// ═══════════════════════════════════════════════════════════════════════════════
// INFINITY HOUSE - Made by Mate Makovics
// Google Play Billing IAP Adapter
// ═══════════════════════════════════════════════════════════════════════════════

using UnityEngine;
using System;
using System.Collections.Generic;

#if UNITY_ANDROID
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Security;
#endif

namespace InfinityHouse.IAP
{
    /// <summary>
    /// Google Play Billing adapter (v6+).
    /// Handles Android-specific IAP flow, receipt validation, and Play Store compliance.
    /// Supports pending transactions and deferred deep link promo codes.
    /// </summary>
    public class PlayBillingAdapter : MonoBehaviour, IPlatformStoreAdapter
    {
        #if UNITY_ANDROID
        private IStoreController storeController;
        private IExtensionProvider extensionProvider;
        private IGooglePlayStoreExtensions googlePlayExtensions;
        #endif

        private bool isInitialized = false;
        private Dictionary<string, PurchaseGateway.IapProduct> productCache;
        private List<string> pendingRestoredProducts;

        // Events
        public event Action<List<PurchaseGateway.IapProduct>> OnInitialized;
        public event Action<string> OnInitializeFailed;
        public event Action<string, string> OnPurchaseComplete;
        public event Action<string, string> OnPurchaseFailed;

        public void Initialize()
        {
            Debug.Log("[PlayBillingAdapter] Initializing Google Play Billing...");

            #if UNITY_ANDROID
            productCache = new Dictionary<string, PurchaseGateway.IapProduct>();
            pendingRestoredProducts = new List<string>();

            // Create ConfigurationBuilder with Google Play
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

            // Add products from configuration
            AddProductsToBuilder(builder);

            // Initialize Unity IAP
            UnityPurchasing.Initialize(this, builder);
            #else
            Debug.LogError("[PlayBillingAdapter] Not running on Android platform!");
            OnInitializeFailed?.Invoke("Not running on Android platform");
            #endif
        }

        #if UNITY_ANDROID
        private void AddProductsToBuilder(ConfigurationBuilder builder)
        {
            // Load product IDs from configuration
            string[] productIds = GetProductIds();

            foreach (string productId in productIds)
            {
                // Determine product type
                ProductType productType = DetermineProductType(productId);
                builder.AddProduct(productId, productType);
            }

            Debug.Log($"[PlayBillingAdapter] Added {productIds.Length} products to builder");
        }

        private string[] GetProductIds()
        {
            // TODO: Load from IapSkuMap.json
            // For now, return hardcoded product IDs
            return new string[]
            {
                "com.matemakovics.infinityhouse.diamonds_100",
                "com.matemakovics.infinityhouse.diamonds_500",
                "com.matemakovics.infinityhouse.diamonds_1000",
                "com.matemakovics.infinityhouse.diamonds_2500",
                "com.matemakovics.infinityhouse.diamonds_5000",
                "com.matemakovics.infinityhouse.seasonpass_s1",
                "com.matemakovics.infinityhouse.capsule_starter",
                "com.matemakovics.infinityhouse.capsule_premium"
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
            Debug.Log("[PlayBillingAdapter] Google Play Billing initialized successfully");

            storeController = controller;
            extensionProvider = extensions;
            googlePlayExtensions = extensions.GetExtension<IGooglePlayStoreExtensions>();

            // Set up deferred promo purchase listener
            googlePlayExtensions.SetDeferredPurchaseListener(OnDeferredPurchase);

            // Set up deferred promo code listener
            googlePlayExtensions.SetDeferredPromoCodeListener(OnDeferredPromoCode);

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

                    Debug.Log($"[PlayBillingAdapter] Product loaded: {iapProduct.productId} - {iapProduct.localizedPrice}");
                }
            }

            // Fetch existing purchases (Google Play doesn't have explicit "restore")
            FetchExistingPurchases();

            OnInitialized?.Invoke(products);
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            Debug.LogError($"[PlayBillingAdapter] Initialization failed: {error}");
            OnInitializeFailed?.Invoke(error.ToString());
        }

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            Debug.LogError($"[PlayBillingAdapter] Initialization failed: {error} - {message}");
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

        private void FetchExistingPurchases()
        {
            // Google Play Billing automatically tracks owned products
            // Query for existing subscriptions and non-consumed purchases
            foreach (var product in storeController.products.all)
            {
                if (product.hasReceipt)
                {
                    Debug.Log($"[PlayBillingAdapter] Found existing purchase: {product.definition.id}");
                    pendingRestoredProducts.Add(product.definition.id);
                }
            }
        }

        private void OnDeferredPurchase(Product product)
        {
            Debug.Log($"[PlayBillingAdapter] Deferred purchase detected: {product.definition.id}");
            // Process deferred purchase from Play Store promotions
            ProcessPurchase(new PurchaseEventArgs(product));
        }

        private void OnDeferredPromoCode(string promoCode)
        {
            Debug.Log($"[PlayBillingAdapter] Deferred promo code: {promoCode}");
            // Handle promo code deep links
        }
        #endif

        public void PurchaseProduct(string productId)
        {
            #if UNITY_ANDROID
            if (!isInitialized)
            {
                Debug.LogError("[PlayBillingAdapter] Store not initialized!");
                OnPurchaseFailed?.Invoke(productId, "Store not initialized");
                return;
            }

            Product product = storeController.products.WithID(productId);

            if (product != null && product.availableToPurchase)
            {
                Debug.Log($"[PlayBillingAdapter] Initiating purchase: {productId}");
                storeController.InitiatePurchase(product);
            }
            else
            {
                Debug.LogError($"[PlayBillingAdapter] Product not available: {productId}");
                OnPurchaseFailed?.Invoke(productId, "Product not available");
            }
            #else
            OnPurchaseFailed?.Invoke(productId, "Not running on Android platform");
            #endif
        }

        #if UNITY_ANDROID
        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            Debug.Log($"[PlayBillingAdapter] Processing purchase: {args.purchasedProduct.definition.id}");

            string productId = args.purchasedProduct.definition.id;
            string receipt = args.purchasedProduct.receipt;

            // Validate receipt locally (Google Play receipt validation)
            bool validReceipt = ValidateReceipt(receipt);

            if (validReceipt)
            {
                OnPurchaseComplete?.Invoke(productId, receipt);

                // For consumables, confirm consumption
                if (IsConsumable(productId))
                {
                    googlePlayExtensions.ConfirmPurchase(args.purchasedProduct, (result, error) =>
                    {
                        if (result.ResultCode == GooglePurchaseResultCode.Ok)
                        {
                            Debug.Log($"[PlayBillingAdapter] Purchase confirmed: {productId}");
                        }
                        else
                        {
                            Debug.LogError($"[PlayBillingAdapter] Confirmation failed: {error}");
                        }
                    });
                }

                return PurchaseProcessingResult.Complete;
            }
            else
            {
                Debug.LogError("[PlayBillingAdapter] Receipt validation failed!");
                OnPurchaseFailed?.Invoke(productId, "Invalid receipt");
                return PurchaseProcessingResult.Complete;
            }
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            Debug.LogWarning($"[PlayBillingAdapter] Purchase failed: {product.definition.id} - {failureReason}");

            string errorMessage = GetUserFriendlyError(failureReason);
            OnPurchaseFailed?.Invoke(product.definition.id, errorMessage);
        }

        private string GetUserFriendlyError(PurchaseFailureReason reason)
        {
            switch (reason)
            {
                case PurchaseFailureReason.PurchasingUnavailable:
                    return "Purchasing is currently unavailable. Please try again later.";
                case PurchaseFailureReason.ExistingPurchasePending:
                    return "A purchase is already in progress.";
                case PurchaseFailureReason.ProductUnavailable:
                    return "This product is currently unavailable.";
                case PurchaseFailureReason.SignatureInvalid:
                    return "Purchase verification failed. Please contact support.";
                case PurchaseFailureReason.UserCancelled:
                    return "Purchase was cancelled.";
                case PurchaseFailureReason.PaymentDeclined:
                    return "Payment was declined. Please check your payment method.";
                case PurchaseFailureReason.DuplicateTransaction:
                    return "This purchase has already been made.";
                default:
                    return "Purchase failed. Please try again.";
            }
        }

        private bool ValidateReceipt(string receipt)
        {
            try
            {
                // Use Unity's CrossPlatformValidator for local validation
                var validator = new CrossPlatformValidator(GooglePlayTangle.Data(), AppleTangle.Data(), Application.identifier);
                var result = validator.Validate(receipt);

                Debug.Log("[PlayBillingAdapter] Receipt validated successfully");
                return true;
            }
            catch (IAPSecurityException ex)
            {
                Debug.LogError($"[PlayBillingAdapter] Receipt validation failed: {ex.Message}");
                return false;
            }
        }

        private bool IsConsumable(string productId)
        {
            // Check if product is consumable
            Product product = storeController.products.WithID(productId);
            return product != null && product.definition.type == ProductType.Consumable;
        }
        #endif

        public void RestorePurchases(Action<bool, List<string>> onComplete)
        {
            #if UNITY_ANDROID
            if (!isInitialized)
            {
                Debug.LogError("[PlayBillingAdapter] Store not initialized!");
                onComplete?.Invoke(false, null);
                return;
            }

            Debug.Log("[PlayBillingAdapter] Restoring purchases (querying existing)...");

            // Google Play doesn't have explicit "restore" - purchases are always available
            // Return the pending restored products list
            if (pendingRestoredProducts.Count > 0)
            {
                onComplete?.Invoke(true, new List<string>(pendingRestoredProducts));
            }
            else
            {
                onComplete?.Invoke(true, new List<string>());
            }
            #else
            onComplete?.Invoke(false, null);
            #endif
        }
    }
}
