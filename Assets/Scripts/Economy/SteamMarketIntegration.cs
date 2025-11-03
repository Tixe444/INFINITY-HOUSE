// ═══════════════════════════════════════════════════════════════════════════════
// INFINITY HOUSE - Made by Mate Makovics
// Steam Community Market Integration
// ═══════════════════════════════════════════════════════════════════════════════

using UnityEngine;
using System;
using System.Collections.Generic;

#if STEAMWORKS_NET
using Steamworks;
#endif

namespace InfinityHouse.Economy
{
    /// <summary>
    /// Steam Community Market integration for trading cosmetic items.
    /// PC ONLY - allows buying/selling skins on Steam Community Market.
    /// All skins are tradeable on Steam. Exotics ONLY tradeable on Steam.
    /// Condition affects market value (+10-15% for Blessed).
    /// NO direct trades - only Steam Community Market.
    /// </summary>
    public class SteamMarketIntegration : MonoBehaviour
    {
        // Singleton
        public static SteamMarketIntegration Instance { get; private set; }

        #if STEAMWORKS_NET
        // Steam inventory
        private SteamInventoryResult_t currentInventory;
        private bool inventoryLoaded = false;
        #endif

        // Platform check
        private bool isSteamPlatform = false;

        // Events
        public event Action OnInventoryLoaded;
        public event Action<string, float> OnItemListed; // itemId, price
        public event Action<string> OnItemSold;
        public event Action<string, string> OnMarketError; // operation, error

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

            // Check if on Steam platform
            #if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX || UNITY_EDITOR
            isSteamPlatform = true;
            #endif
        }

        private void Start()
        {
            #if STEAMWORKS_NET
            if (isSteamPlatform && SteamManager.Initialized)
            {
                LoadInventory();
            }
            #endif
        }

        #region Inventory Management

        /// <summary>
        /// Loads Steam inventory
        /// </summary>
        public void LoadInventory()
        {
            #if STEAMWORKS_NET
            if (!isSteamPlatform)
            {
                Debug.LogWarning("[SteamMarket] Not on Steam platform");
                return;
            }

            if (!SteamManager.Initialized)
            {
                Debug.LogError("[SteamMarket] Steam not initialized");
                return;
            }

            Debug.Log("[SteamMarket] Loading Steam inventory...");

            // Request full inventory
            if (SteamInventory.GetAllItems(out currentInventory))
            {
                // Inventory request submitted - wait for callback
                Debug.Log("[SteamMarket] Inventory request submitted");
            }
            else
            {
                Debug.LogError("[SteamMarket] Failed to request inventory");
            }
            #else
            Debug.LogWarning("[SteamMarket] Steamworks.NET not available");
            #endif
        }

        #if STEAMWORKS_NET
        private void OnInventoryResultReady(SteamInventoryResultReady_t callback)
        {
            if (callback.m_result == EResult.k_EResultOK)
            {
                currentInventory = callback.m_handle;
                inventoryLoaded = true;

                Debug.Log("[SteamMarket] Inventory loaded successfully");
                OnInventoryLoaded?.Invoke();

                // Track telemetry
                Analytics.TelemetryEvents.Instance?.TrackEvent("steam_inventory_loaded", new Dictionary<string, object>());
            }
            else
            {
                Debug.LogError($"[SteamMarket] Inventory load failed: {callback.m_result}");
            }
        }
        #endif

        #endregion

        #region Market Operations

        /// <summary>
        /// Opens Steam Community Market in overlay
        /// </summary>
        public void OpenMarket()
        {
            if (!isSteamPlatform)
            {
                OnMarketError?.Invoke("open_market", "Not on Steam platform");
                return;
            }

            #if STEAMWORKS_NET
            if (!SteamManager.Initialized)
            {
                OnMarketError?.Invoke("open_market", "Steam not initialized");
                return;
            }

            // Open Steam Overlay to Community Market
            string marketUrl = "https://steamcommunity.com/market/search?appid=" + SteamUtils.GetAppID().m_AppId;
            SteamFriends.ActivateGameOverlayToWebPage(marketUrl);

            Debug.Log("[SteamMarket] Opening Steam Community Market");

            // Track telemetry
            Analytics.TelemetryEvents.Instance?.TrackEvent("market_view_pc", new Dictionary<string, object>());
            #endif
        }

        /// <summary>
        /// Opens market listing for a specific item
        /// </summary>
        public void ViewItemOnMarket(string itemId)
        {
            if (!isSteamPlatform)
            {
                OnMarketError?.Invoke("view_item", "Not on Steam platform");
                return;
            }

            #if STEAMWORKS_NET
            if (!SteamManager.Initialized)
            {
                OnMarketError?.Invoke("view_item", "Steam not initialized");
                return;
            }

            // Construct market URL for specific item
            string itemName = GetSteamMarketName(itemId);
            string marketUrl = $"https://steamcommunity.com/market/listings/{SteamUtils.GetAppID().m_AppId}/{itemName}";

            SteamFriends.ActivateGameOverlayToWebPage(marketUrl);

            Debug.Log($"[SteamMarket] Viewing {itemName} on market");

            // Track telemetry
            Analytics.TelemetryEvents.Instance?.TrackEvent("item_view_market", new Dictionary<string, object>
            {
                { "item_id", itemId }
            });
            #endif
        }

        /// <summary>
        /// Checks if an item can be traded on Steam Market
        /// </summary>
        public bool IsMarketable(Shop.CosmeticItem item)
        {
            if (!isSteamPlatform) return false;

            // All skins are marketable on Steam
            // Exotics are ONLY marketable on Steam (not mobile)
            return true;
        }

        /// <summary>
        /// Calculates estimated market value for an item
        /// </summary>
        public float GetEstimatedMarketValue(Shop.CosmeticItem item)
        {
            // Base value by rarity
            float baseValue = GetBaseMarketValue(item.rarity);

            // Apply condition multiplier
            float conditionMultiplier = RarityManager.Instance.GetConditionMarketMultiplier(item.conditionValue);

            // Final value
            float estimatedValue = baseValue * conditionMultiplier;

            return estimatedValue;
        }

        private float GetBaseMarketValue(RarityManager.RarityTier rarity)
        {
            // Estimated Steam market prices in USD
            switch (rarity)
            {
                case RarityManager.RarityTier.Common:
                    return 0.10f;
                case RarityManager.RarityTier.Rare:
                    return 0.25f;
                case RarityManager.RarityTier.Epic:
                    return 0.75f;
                case RarityManager.RarityTier.Legendary:
                    return 2.50f;
                case RarityManager.RarityTier.Mythic:
                    return 10.00f;
                case RarityManager.RarityTier.Exotic:
                    return 50.00f;
                default:
                    return 0.10f;
            }
        }

        private string GetSteamMarketName(string itemId)
        {
            // Format item name for Steam Market
            // Example: "Infinity House - Ghost Skin (Blessed)"
            return $"Infinity House - {itemId}";
        }

        #endregion

        #region Item Trading

        /// <summary>
        /// Prepares item for market listing
        /// NOTE: Actual listing happens via Steam Community Market website
        /// </summary>
        public void PrepareForMarketListing(Shop.CosmeticItem item, float price)
        {
            if (!isSteamPlatform)
            {
                OnMarketError?.Invoke("list_item", "Not on Steam platform");
                return;
            }

            if (!IsMarketable(item))
            {
                OnMarketError?.Invoke("list_item", "Item not marketable");
                return;
            }

            // Check if item is equipped
            if (Shop.ShopManager.Instance.IsEquipped(item))
            {
                OnMarketError?.Invoke("list_item", "Cannot list equipped items");
                return;
            }

            Debug.Log($"[SteamMarket] Item ready for listing: {item.cosmeticId} at ${price:F2}");

            // Open market to complete listing
            ViewItemOnMarket(item.cosmeticId);

            OnItemListed?.Invoke(item.cosmeticId, price);

            // Track telemetry
            Analytics.TelemetryEvents.Instance?.TrackEvent("item_sold_steam", new Dictionary<string, object>
            {
                { "item_id", item.cosmeticId },
                { "rarity", item.rarity.ToString() },
                { "condition", item.conditionValue },
                { "estimated_price", price }
            });
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets formatted condition tag for Steam Market
        /// </summary>
        public string GetConditionTag(float condition)
        {
            RarityManager.ConditionTier tier = RarityManager.Instance.GetConditionTier(condition);
            var conditionData = RarityManager.Instance.GetConditionData(tier);

            return $"{conditionData.displayName} ({condition:F2})";
        }

        /// <summary>
        /// Checks if Steam Market is available
        /// </summary>
        public bool IsMarketAvailable()
        {
            #if STEAMWORKS_NET
            return isSteamPlatform && SteamManager.Initialized;
            #else
            return false;
            #endif
        }

        #endregion

        #region Debug

        [ContextMenu("Open Steam Market")]
        public void DebugOpenMarket()
        {
            OpenMarket();
        }

        #endregion
    }
}
