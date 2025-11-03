// ═══════════════════════════════════════════════════════════════════════════════
// INFINITY HOUSE - Made by Mate Makovics
// iOS StoreKit IAP Adapter
// ═══════════════════════════════════════════════════════════════════════════════

using UnityEngine;
using System;
using System.Collections.Generic;

#if UNITY_IOS
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Security;
#endif

namespace InfinityHouse.IAP
{
    /// <summary>
    /// iOS App Store adapter using StoreKit.
    /// Handles iOS-specific IAP flow, receipt validation, and restore purchases.
    /// Complies with App Store Review Guidelines.
    /// </summary>
    public class StoreKitAdapter : MonoBehaviour, IPlatformStoreAdapter
    {
        #if UNITY_IOS
        private IStoreController storeController;
        private IExtensionProvider extensionProvider;
        private IAppleExtensions appleExtensions;
        #endif

        private bool isInitialized = false;
        private Dictionary<string, PurchaseGateway.IapProduct> productCache;

        // Events
        public event Action<List<PurchaseGateway.IapProduct>> OnInitialized;
        public event Action<string> OnInitializeFailed;
        public event Action<string, string> OnPurchaseComplete;
        public event Action<string, string> OnPurchaseFailed;

        public void Initialize()
        {
            Debug.Log("[StoreKitAdapter] Initializing iOS StoreKit...");

            #if UNITY_IOS
            productCache = new Dictionary<string, PurchaseGateway.IapProduct>();

            // Create ConfigurationBuilder
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

            // Add products from configuration
            AddProductsToBuilder(builder);

            // Initialize Unity IAP
            UnityPurchasing.Initialize(this, builder);
            #else
            Debug.LogError("[StoreKitAdapter] Not running on iOS platform!");
            OnInitializeFailed?.Invoke("Not running on iOS platform");
            #endif
        }

        #if UNITY_IOS
        private void AddProductsToBuilder(ConfigurationBuilder builder)
        {
            // Load product IDs from configuration
            // In production, load from IapSkuMap.json
            string[] productIds = GetProductIds();

            foreach (string productId in productIds)
            {
                // Determine product type
                ProductType productType = DetermineProductType(productId);
                builder.AddProduct(productId, productType);
            }

            Debug.Log($"[StoreKitAdapter] Added {productIds.Length} products to builder");
        }

        private string[] GetProductIds()
        {
            // TODO: Load from IapSkuMap.json
            // For now, return hardcoded product IDs
            return new string[]
            {
                "com.timeviali.infinityhouse.diamonds_100",
                "com.timeviali.infinityhouse.diamonds_500",
                "com.timeviali.infinityhouse.diamonds_1000",
                "com.timeviali.infinityhouse.diamonds_2500",
                "com.timeviali.infinityhouse.diamonds_5000",
                "com.timeviali.infinityhouse.seasonpass_s1",
                "com.timeviali.infinityhouse.capsule_starter",
                "com.timeviali.infinityhouse.capsule_premium"
            };
        }

        private ProductType DetermineProductType(string productId)
        {
            if (productId.Contains("seasonpass"))
            {
                return ProductType.Subscription;
            }
            else
            {
                return ProductType.Consumable;
            }
        }

        // IStoreListener implementation
        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            Debug.Log("[StoreKitAdapter] StoreKit initialized successfully");

            storeController = controller;
            extensionProvider = extensions;
            appleExtensions = extensions.GetExtension<IAppleExtensions>();

            // Register deferred purchase callback
            appleExtensions.RegisterPurchaseDeferredListener(OnPurchaseDeferred);

            isInitialized = true;

            // Convert products to IapProduct format
            List<PurchaseGateway.IapProduct> products = new List<PurchaseGateway.IapProduct>();

            foreach (var product in controller.products.all)
            {
                if (product.availableToPurchase)
                {
                    var iapProduct = new PurchaseGateway.IapProduct
                    {
                        productId = product.definition.id,
                        title = product.metadata.localizedTitle,
                        description = product.metadata.localizedDescription,
                        priceDecimal = product.metadata.localizedPrice,
                        localizedPrice = product.metadata.localizedPriceString,
                        currencyCode = product.metadata.isoCurrencyCode,
                        type = ConvertProductType(product.definition.type)
                    };

                    products.Add(iapProduct);
                    productCache[iapProduct.productId] = iapProduct;

                    Debug.Log($"[StoreKitAdapter] Product loaded: {iapProduct.productId} - {iapProduct.localizedPrice}");
                }
            }

            OnInitialized?.Invoke(products);
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            Debug.LogError($"[StoreKitAdapter] Initialization failed: {error}");
            OnInitializeFailed?.Invoke(error.ToString());
        }

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            Debug.LogError($"[StoreKitAdapter] Initialization failed: {error} - {message}");
            OnInitializeFailed?.Invoke($"{error}: {message}");
        }

        private PurchaseGateway.IapProduct.ProductType ConvertProductType(ProductType type)
        {
            switch (type)
            {
                case ProductType.Consumable:
                    return PurchaseGateway.IapProduct.ProductType.Consumable;
                case ProductType.NonConsumable:
                    return PurchaseGateway.IapProduct.ProductType.NonConsumable;
                case ProductType.Subscription:
                    return PurchaseGateway.IapProduct.ProductType.Subscription;
                default:
                    return PurchaseGateway.IapProduct.ProductType.Consumable;
            }
        }
        #endif

        public void PurchaseProduct(string productId)
        {
            #if UNITY_IOS
            if (!isInitialized)
            {
                Debug.LogError("[StoreKitAdapter] Store not initialized!");
                OnPurchaseFailed?.Invoke(productId, "Store not initialized");
                return;
            }

            Product product = storeController.products.WithID(productId);

            if (product != null && product.availableToPurchase)
            {
                Debug.Log($"[StoreKitAdapter] Initiating purchase: {productId}");
                storeController.InitiatePurchase(product);
            }
            else
            {
                Debug.LogError($"[StoreKitAdapter] Product not available: {productId}");
                OnPurchaseFailed?.Invoke(productId, "Product not available");
            }
            #else
            OnPurchaseFailed?.Invoke(productId, "Not running on iOS platform");
            #endif
        }

        #if UNITY_IOS
        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            Debug.Log($"[StoreKitAdapter] Processing purchase: {args.purchasedProduct.definition.id}");

            string productId = args.purchasedProduct.definition.id;
            string receipt = args.purchasedProduct.receipt;

            // Validate receipt locally (Apple receipt validation)
            bool validReceipt = ValidateReceipt(receipt);

            if (validReceipt)
            {
                OnPurchaseComplete?.Invoke(productId, receipt);
                return PurchaseProcessingResult.Complete;
            }
            else
            {
                Debug.LogError("[StoreKitAdapter] Receipt validation failed!");
                OnPurchaseFailed?.Invoke(productId, "Invalid receipt");
                return PurchaseProcessingResult.Complete;
            }
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            Debug.LogWarning($"[StoreKitAdapter] Purchase failed: {product.definition.id} - {failureReason}");
            OnPurchaseFailed?.Invoke(product.definition.id, failureReason.ToString());
        }

        private bool ValidateReceipt(string receipt)
        {
            try
            {
                // Use Unity's CrossPlatformValidator for local validation
                var validator = new CrossPlatformValidator(GooglePlayTangle.Data(), AppleTangle.Data(), Application.identifier);
                var result = validator.Validate(receipt);

                Debug.Log("[StoreKitAdapter] Receipt validated successfully");
                return true;
            }
            catch (IAPSecurityException ex)
            {
                Debug.LogError($"[StoreKitAdapter] Receipt validation failed: {ex.Message}");
                return false;
            }
        }

        private void OnPurchaseDeferred(Product product)
        {
            Debug.Log($"[StoreKitAdapter] Purchase deferred (Ask to Buy): {product.definition.id}");
            // Purchase is pending parental approval
            // Don't grant entitlement yet
        }
        #endif

        public void RestorePurchases(Action<bool, List<string>> onComplete)
        {
            #if UNITY_IOS
            if (!isInitialized)
            {
                Debug.LogError("[StoreKitAdapter] Store not initialized!");
                onComplete?.Invoke(false, null);
                return;
            }

            Debug.Log("[StoreKitAdapter] Restoring purchases...");

            // iOS requires restore purchases functionality
            appleExtensions.RestoreTransactions((success, error) =>
            {
                if (success)
                {
                    Debug.Log("[StoreKitAdapter] Purchases restored successfully");

                    // Get restored product IDs
                    List<string> restoredProductIds = new List<string>();
                    // TODO: Track restored products from ProcessPurchase callbacks

                    onComplete?.Invoke(true, restoredProductIds);
                }
                else
                {
                    Debug.LogError($"[StoreKitAdapter] Restore failed: {error}");
                    onComplete?.Invoke(false, null);
                }
            });
            #else
            onComplete?.Invoke(false, null);
            #endif
        }
    }
}
