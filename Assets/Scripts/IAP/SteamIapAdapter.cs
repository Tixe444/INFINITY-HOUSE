// ═══════════════════════════════════════════════════════════════════════════════
// INFINITY HOUSE - Made by Mate Makovics
// Steam Microtransactions IAP Adapter
// ═══════════════════════════════════════════════════════════════════════════════

using UnityEngine;
using System;
using System.Collections.Generic;

#if STEAMWORKS_NET
using Steamworks;
#endif

namespace InfinityHouse.IAP
{
    /// <summary>
    /// Steam Microtransactions adapter.
    /// Handles Steam-specific IAP flow via Steamworks.NET.
    /// Supports Steam Inventory Service and Wallet transactions.
    /// </summary>
    public class SteamIapAdapter : MonoBehaviour, IPlatformStoreAdapter
    {
        #if STEAMWORKS_NET
        private bool steamInitialized = false;
        private Dictionary<string, SteamItemDef> itemDefs;
        private Callback<MicroTxnAuthorizationResponse_t> authCallback;
        #endif

        private bool isInitialized = false;
        private Dictionary<string, PurchaseGateway.IapProduct> productCache;

        // Events
        public event Action<List<PurchaseGateway.IapProduct>> OnInitialized;
        public event Action<string> OnInitializeFailed;
        public event Action<string, string> OnPurchaseComplete;
        public event Action<string, string> OnPurchaseFailed;

        // Price mapping (USD cents)
        private Dictionary<string, int> priceMap = new Dictionary<string, int>
        {
            { "diamonds_100", 99 },      // $0.99
            { "diamonds_500", 499 },     // $4.99
            { "diamonds_1000", 999 },    // $9.99
            { "diamonds_2500", 1999 },   // $19.99
            { "diamonds_5000", 4999 },   // $49.99
            { "seasonpass_s1", 999 },    // $9.99
            { "capsule_starter", 199 },  // $1.99
            { "capsule_premium", 299 }   // $2.99
        };

        public void Initialize()
        {
            Debug.Log("[SteamIapAdapter] Initializing Steam Microtransactions...");

            #if STEAMWORKS_NET
            // Check if Steam is initialized
            if (!SteamManager.Initialized)
            {
                Debug.LogError("[SteamIapAdapter] Steam is not initialized!");
                OnInitializeFailed?.Invoke("Steam not initialized");
                return;
            }

            steamInitialized = true;
            productCache = new Dictionary<string, PurchaseGateway.IapProduct>();
            itemDefs = new Dictionary<string, SteamItemDef>();

            // Register Microtransaction callback
            authCallback = Callback<MicroTxnAuthorizationResponse_t>.Create(OnMicroTxnAuthorizationResponse);

            // Load product catalog
            LoadProductCatalog();

            isInitialized = true;
            Debug.Log("[SteamIapAdapter] Initialization complete");

            OnInitialized?.Invoke(new List<PurchaseGateway.IapProduct>(productCache.Values));
            #else
            Debug.LogError("[SteamIapAdapter] Steamworks.NET not available!");
            OnInitializeFailed?.Invoke("Steamworks.NET not available");
            #endif
        }

        #if STEAMWORKS_NET
        private void LoadProductCatalog()
        {
            // Load products from configuration
            string[] productIds = GetProductIds();

            foreach (string productId in productIds)
            {
                // Create IapProduct
                var product = new PurchaseGateway.IapProduct
                {
                    productId = productId,
                    title = GetProductTitle(productId),
                    description = GetProductDescription(productId),
                    priceDecimal = GetProductPrice(productId),
                    localizedPrice = GetLocalizedPrice(productId),
                    currencyCode = "USD", // Steam uses user's currency
                    type = GetProductType(productId)
                };

                productCache[productId] = product;

                Debug.Log($"[SteamIapAdapter] Product loaded: {productId} - {product.localizedPrice}");
            }
        }

        private string[] GetProductIds()
        {
            return new string[]
            {
                "diamonds_100",
                "diamonds_500",
                "diamonds_1000",
                "diamonds_2500",
                "diamonds_5000",
                "seasonpass_s1",
                "capsule_starter",
                "capsule_premium"
            };
        }

        private string GetProductTitle(string productId)
        {
            // TODO: Load from localization
            switch (productId)
            {
                case "diamonds_100": return "100 Diamonds";
                case "diamonds_500": return "500 Diamonds";
                case "diamonds_1000": return "1,000 Diamonds";
                case "diamonds_2500": return "2,500 Diamonds";
                case "diamonds_5000": return "5,000 Diamonds";
                case "seasonpass_s1": return "Season Pass - Season 1";
                case "capsule_starter": return "Starter Cosmetic Capsule";
                case "capsule_premium": return "Premium Cosmetic Capsule";
                default: return productId;
            }
        }

        private string GetProductDescription(string productId)
        {
            // TODO: Load from localization
            switch (productId)
            {
                case "diamonds_100": return "Small stack of premium diamonds";
                case "diamonds_500": return "Medium stack of premium diamonds";
                case "diamonds_1000": return "Large stack of premium diamonds (BEST VALUE)";
                case "diamonds_2500": return "Huge stack of premium diamonds";
                case "diamonds_5000": return "Massive hoard of premium diamonds";
                case "seasonpass_s1": return "Premium Season Pass with exclusive cosmetics";
                case "capsule_starter": return "Contains 3 random cosmetics (Common-Rare)";
                case "capsule_premium": return "Contains 5 random cosmetics (Rare-Legendary)";
                default: return "";
            }
        }

        private decimal GetProductPrice(string productId)
        {
            if (priceMap.ContainsKey(productId))
            {
                return priceMap[productId] / 100m;
            }
            return 0m;
        }

        private string GetLocalizedPrice(string productId)
        {
            if (priceMap.ContainsKey(productId))
            {
                return $"${priceMap[productId] / 100m:F2}";
            }
            return "$0.00";
        }

        private PurchaseGateway.IapProduct.ProductType GetProductType(string productId)
        {
            if (productId.Contains("seasonpass"))
            {
                return PurchaseGateway.IapProduct.ProductType.Subscription;
            }
            else
            {
                return PurchaseGateway.IapProduct.ProductType.Consumable;
            }
        }

        private void OnMicroTxnAuthorizationResponse(MicroTxnAuthorizationResponse_t callback)
        {
            Debug.Log($"[SteamIapAdapter] Microtransaction response: AppID={callback.m_unAppID}, OrderID={callback.m_ulOrderID}, Authorized={callback.m_bAuthorized}");

            if (callback.m_bAuthorized == 1)
            {
                // Transaction authorized - finalize purchase
                // Note: Actual item grant should be handled server-side via Steam Web API
                // This is just client confirmation

                // Extract product ID from order (in real implementation, track pending orders)
                string productId = "unknown"; // TODO: Track order ID -> product ID mapping

                // Generate receipt (order ID)
                string receipt = callback.m_ulOrderID.ToString();

                OnPurchaseComplete?.Invoke(productId, receipt);
                Debug.Log($"[SteamIapAdapter] Purchase authorized: {productId}");
            }
            else
            {
                Debug.LogWarning("[SteamIapAdapter] Transaction not authorized");
                OnPurchaseFailed?.Invoke("unknown", "Transaction not authorized");
            }
        }
        #endif

        public void PurchaseProduct(string productId)
        {
            #if STEAMWORKS_NET
            if (!isInitialized || !steamInitialized)
            {
                Debug.LogError("[SteamIapAdapter] Store not initialized!");
                OnPurchaseFailed?.Invoke(productId, "Store not initialized");
                return;
            }

            if (!productCache.ContainsKey(productId))
            {
                Debug.LogError($"[SteamIapAdapter] Product not found: {productId}");
                OnPurchaseFailed?.Invoke(productId, "Product not found");
                return;
            }

            Debug.Log($"[SteamIapAdapter] Initiating Steam Overlay purchase: {productId}");

            // Open Steam Overlay to store page
            // Note: For actual microtransactions, you need to:
            // 1. Call your backend to initiate purchase via Steam Web API
            // 2. Backend calls InitTxn
            // 3. Backend returns transaction ID
            // 4. Client opens Steam Overlay with that transaction

            // For now, open Steam Overlay as fallback
            string storeUrl = $"https://store.steampowered.com/app/{GetSteamAppId()}/";
            SteamFriends.ActivateGameOverlayToWebPage(storeUrl);

            // In production, implement proper Microtransaction API flow:
            // StartPurchaseRequest(productId);

            Debug.LogWarning("[SteamIapAdapter] Note: Full Steam Microtransaction flow requires server-side implementation");
            OnPurchaseFailed?.Invoke(productId, "Steam purchases require server integration");
            #else
            OnPurchaseFailed?.Invoke(productId, "Steamworks.NET not available");
            #endif
        }

        #if STEAMWORKS_NET
        private uint GetSteamAppId()
        {
            return SteamUtils.GetAppID().m_AppId;
        }

        // Note: Full implementation requires server-side Steam Web API integration
        // See: https://partner.steamgames.com/doc/features/microtransactions
        private void StartPurchaseRequest(string productId)
        {
            // This would call your backend API:
            // POST /api/steam/initiate-purchase
            // Body: { productId, steamId }
            // Response: { transactionId }

            // Then open Steam Overlay with transaction:
            // SteamFriends.ActivateGameOverlayToWebPage($"steam://url/...");
        }
        #endif

        public void RestorePurchases(Action<bool, List<string>> onComplete)
        {
            #if STEAMWORKS_NET
            if (!isInitialized)
            {
                Debug.LogError("[SteamIapAdapter] Store not initialized!");
                onComplete?.Invoke(false, null);
                return;
            }

            Debug.Log("[SteamIapAdapter] Restoring purchases from Steam Cloud...");

            // Steam doesn't have explicit "restore purchases"
            // Purchases are tied to Steam account and handled server-side
            // Query your backend for user's purchases

            // For now, return empty list
            onComplete?.Invoke(true, new List<string>());
            #else
            onComplete?.Invoke(false, null);
            #endif
        }

        private void OnDestroy()
        {
            #if STEAMWORKS_NET
            // Cleanup callbacks
            if (authCallback != null)
            {
                authCallback.Dispose();
            }
            #endif
        }
    }
}
