// ═══════════════════════════════════════════════════════════════════════════════
// INFINITY HOUSE - Made by Mate Makovics
// Cross-Platform IAP Purchase Gateway
// ═══════════════════════════════════════════════════════════════════════════════

using UnityEngine;
using System;
using System.Collections.Generic;

namespace InfinityHouse.IAP
{
    /// <summary>
    /// Unified IAP facade for Steam, Google Play, and App Store.
    /// Handles purchase flow, receipt verification, and entitlements.
    /// Platform-agnostic interface for all in-app purchases.
    /// </summary>
    public class PurchaseGateway : MonoBehaviour
    {
        [Header("Platform Detection")]
        [SerializeField] private bool forcePlatform = false;
        [SerializeField] private StorePlatform debugPlatform = StorePlatform.Steam;

        [Header("Configuration")]
        [SerializeField] private TextAsset iapSkuMapJson;
        [SerializeField] private string serverVerificationUrl = "https://api.yourgame.com/verify";
        [SerializeField] private bool requireServerVerification = true;

        [Header("Security")]
        [SerializeField] private bool allowOfflinePurchases = false;
        [SerializeField] private int maxRetryAttempts = 3;
        [SerializeField] private float retryDelaySeconds = 2f;

        // Singleton
        public static PurchaseGateway Instance { get; private set; }

        // Platform adapters
        private IPlatformStoreAdapter currentAdapter;
        private ReceiptVerifier receiptVerifier;
        private EntitlementService entitlementService;

        // State
        private StorePlatform currentPlatform;
        private bool isInitialized = false;
        private Dictionary<string, IapProduct> productCatalog;
        private Queue<PurchaseRequest> pendingPurchases;

        // Events
        public event Action<StorePlatform> OnStoreInitialized;
        public event Action<IapProduct> OnPurchaseStarted;
        public event Action<PurchaseResult> OnPurchaseCompleted;
        public event Action<string> OnPurchaseFailed;
        public event Action<List<string>> OnPurchasesRestored;
        public event Action<IapProduct, decimal> OnProductPriceLoaded;

        // Properties
        public bool IsInitialized => isInitialized;
        public StorePlatform CurrentPlatform => currentPlatform;
        public bool CanMakePurchases => isInitialized && currentAdapter != null;

        public enum StorePlatform
        {
            Steam,
            GooglePlay,
            AppStore,
            Standalone  // For testing without store
        }

        private void Awake()
        {
            // Singleton
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            pendingPurchases = new Queue<PurchaseRequest>();
            productCatalog = new Dictionary<string, IapProduct>();

            InitializeComponents();
        }

        private void Start()
        {
            DetectPlatform();
            InitializeStore();
        }

        /// <summary>
        /// Initializes supporting components
        /// </summary>
        private void InitializeComponents()
        {
            // Add receipt verifier
            receiptVerifier = gameObject.AddComponent<ReceiptVerifier>();
            receiptVerifier.SetVerificationUrl(serverVerificationUrl);

            // Add entitlement service
            entitlementService = gameObject.AddComponent<EntitlementService>();
        }

        /// <summary>
        /// Detects current platform
        /// </summary>
        private void DetectPlatform()
        {
            if (forcePlatform)
            {
                currentPlatform = debugPlatform;
                Debug.Log($"[PurchaseGateway] Forced platform: {currentPlatform}");
                return;
            }

            #if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX
                // Check for Steam
                #if STEAMWORKS_NET
                    currentPlatform = StorePlatform.Steam;
                #else
                    currentPlatform = StorePlatform.Standalone;
                #endif
            #elif UNITY_IOS
                currentPlatform = StorePlatform.AppStore;
            #elif UNITY_ANDROID
                currentPlatform = StorePlatform.GooglePlay;
            #else
                currentPlatform = StorePlatform.Standalone;
            #endif

            Debug.Log($"[PurchaseGateway] Detected platform: {currentPlatform}");
        }

        /// <summary>
        /// Initializes the appropriate store adapter
        /// </summary>
        private void InitializeStore()
        {
            Debug.Log($"[PurchaseGateway] Initializing {currentPlatform} store...");

            switch (currentPlatform)
            {
                case StorePlatform.Steam:
                    currentAdapter = gameObject.AddComponent<SteamIapAdapter>();
                    break;

                case StorePlatform.GooglePlay:
                    currentAdapter = gameObject.AddComponent<PlayBillingAdapter>();
                    break;

                case StorePlatform.AppStore:
                    currentAdapter = gameObject.AddComponent<StoreKitAdapter>();
                    break;

                case StorePlatform.Standalone:
                    currentAdapter = gameObject.AddComponent<StandaloneStoreAdapter>();
                    break;
            }

            if (currentAdapter != null)
            {
                currentAdapter.OnInitialized += HandleStoreInitialized;
                currentAdapter.OnInitializeFailed += HandleStoreInitializeFailed;
                currentAdapter.OnPurchaseComplete += HandlePurchaseComplete;
                currentAdapter.OnPurchaseFailed += HandlePurchaseFailed;

                currentAdapter.Initialize();
            }
            else
            {
                Debug.LogError("[PurchaseGateway] Failed to create store adapter!");
            }
        }

        #region Purchase Flow

        /// <summary>
        /// Initiates a purchase for a product
        /// </summary>
        public void PurchaseProduct(string productId, Action<bool> onComplete = null)
        {
            if (!CanMakePurchases)
            {
                Debug.LogWarning("[PurchaseGateway] Store not initialized!");
                onComplete?.Invoke(false);
                OnPurchaseFailed?.Invoke("Store not initialized");
                return;
            }

            if (!productCatalog.ContainsKey(productId))
            {
                Debug.LogWarning($"[PurchaseGateway] Product not found: {productId}");
                onComplete?.Invoke(false);
                OnPurchaseFailed?.Invoke($"Product not found: {productId}");
                return;
            }

            IapProduct product = productCatalog[productId];
            OnPurchaseStarted?.Invoke(product);

            // Create purchase request
            PurchaseRequest request = new PurchaseRequest
            {
                productId = productId,
                requestTime = DateTime.UtcNow,
                callback = onComplete
            };

            pendingPurchases.Enqueue(request);

            // Initiate platform purchase
            currentAdapter.PurchaseProduct(productId);

            Debug.Log($"[PurchaseGateway] Purchase initiated: {productId}");
        }

        /// <summary>
        /// Restores previous purchases (required by iOS)
        /// </summary>
        public void RestorePurchases(Action<bool> onComplete = null)
        {
            if (!CanMakePurchases)
            {
                Debug.LogWarning("[PurchaseGateway] Store not initialized!");
                onComplete?.Invoke(false);
                return;
            }

            Debug.Log("[PurchaseGateway] Restoring purchases...");
            currentAdapter.RestorePurchases((success, productIds) =>
            {
                if (success)
                {
                    OnPurchasesRestored?.Invoke(productIds);
                    entitlementService.ProcessRestoredPurchases(productIds);
                }
                onComplete?.Invoke(success);
            });
        }

        /// <summary>
        /// Gets the localized price for a product
        /// </summary>
        public string GetProductPrice(string productId)
        {
            if (productCatalog.ContainsKey(productId))
            {
                return productCatalog[productId].localizedPrice;
            }
            return "$?.??";
        }

        /// <summary>
        /// Checks if a product is available
        /// </summary>
        public bool IsProductAvailable(string productId)
        {
            return productCatalog.ContainsKey(productId);
        }

        /// <summary>
        /// Gets all available products
        /// </summary>
        public List<IapProduct> GetAllProducts()
        {
            return new List<IapProduct>(productCatalog.Values);
        }

        #endregion

        #region Store Adapter Callbacks

        private void HandleStoreInitialized(List<IapProduct> products)
        {
            Debug.Log($"[PurchaseGateway] Store initialized with {products.Count} products");

            // Populate catalog
            productCatalog.Clear();
            foreach (var product in products)
            {
                productCatalog[product.productId] = product;
                OnProductPriceLoaded?.Invoke(product, product.priceDecimal);
            }

            isInitialized = true;
            OnStoreInitialized?.Invoke(currentPlatform);
        }

        private void HandleStoreInitializeFailed(string error)
        {
            Debug.LogError($"[PurchaseGateway] Store initialization failed: {error}");
            isInitialized = false;
        }

        private async void HandlePurchaseComplete(string productId, string receipt)
        {
            Debug.Log($"[PurchaseGateway] Purchase completed: {productId}");

            // Get pending request
            PurchaseRequest request = null;
            if (pendingPurchases.Count > 0)
            {
                request = pendingPurchases.Dequeue();
            }

            // Verify receipt if required
            if (requireServerVerification)
            {
                bool verified = await receiptVerifier.VerifyReceipt(receipt, currentPlatform);

                if (!verified)
                {
                    Debug.LogError("[PurchaseGateway] Receipt verification failed!");
                    request?.callback?.Invoke(false);
                    OnPurchaseFailed?.Invoke("Receipt verification failed");
                    return;
                }
            }

            // Grant entitlement
            bool granted = entitlementService.GrantEntitlement(productId, receipt);

            if (granted)
            {
                PurchaseResult result = new PurchaseResult
                {
                    productId = productId,
                    success = true,
                    receipt = receipt,
                    purchaseTime = DateTime.UtcNow
                };

                OnPurchaseCompleted?.Invoke(result);
                request?.callback?.Invoke(true);

                Debug.Log($"[PurchaseGateway] Entitlement granted: {productId}");
            }
            else
            {
                Debug.LogError($"[PurchaseGateway] Failed to grant entitlement: {productId}");
                request?.callback?.Invoke(false);
                OnPurchaseFailed?.Invoke("Failed to grant entitlement");
            }
        }

        private void HandlePurchaseFailed(string productId, string error)
        {
            Debug.LogWarning($"[PurchaseGateway] Purchase failed: {productId} - {error}");

            // Get pending request
            PurchaseRequest request = null;
            if (pendingPurchases.Count > 0)
            {
                request = pendingPurchases.Dequeue();
            }

            request?.callback?.Invoke(false);
            OnPurchaseFailed?.Invoke(error);
        }

        #endregion

        #region Data Structures

        [Serializable]
        public class IapProduct
        {
            public string productId;
            public string title;
            public string description;
            public decimal priceDecimal;
            public string localizedPrice;
            public string currencyCode;
            public ProductType type;

            public enum ProductType
            {
                Consumable,         // Diamonds, currency packs
                NonConsumable,      // Permanent unlocks
                Subscription        // Season pass
            }
        }

        [Serializable]
        public class PurchaseResult
        {
            public string productId;
            public bool success;
            public string receipt;
            public DateTime purchaseTime;
        }

        private class PurchaseRequest
        {
            public string productId;
            public DateTime requestTime;
            public Action<bool> callback;
        }

        #endregion

        private void OnDestroy()
        {
            if (currentAdapter != null)
            {
                currentAdapter.OnInitialized -= HandleStoreInitialized;
                currentAdapter.OnInitializeFailed -= HandleStoreInitializeFailed;
                currentAdapter.OnPurchaseComplete -= HandlePurchaseComplete;
                currentAdapter.OnPurchaseFailed -= HandlePurchaseFailed;
            }
        }
    }

    /// <summary>
    /// Interface for platform-specific store adapters
    /// </summary>
    public interface IPlatformStoreAdapter
    {
        event Action<List<PurchaseGateway.IapProduct>> OnInitialized;
        event Action<string> OnInitializeFailed;
        event Action<string, string> OnPurchaseComplete; // productId, receipt
        event Action<string, string> OnPurchaseFailed; // productId, error

        void Initialize();
        void PurchaseProduct(string productId);
        void RestorePurchases(Action<bool, List<string>> onComplete);
    }
}
