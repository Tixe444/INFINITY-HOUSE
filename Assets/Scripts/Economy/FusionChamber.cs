// ═══════════════════════════════════════════════════════════════════════════════
// INFINITY HOUSE - Made by Mate Makovics
// Fusion Chamber - 5:1 Item Fusion System
// ═══════════════════════════════════════════════════════════════════════════════

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using InfinityHouse.Shop;

namespace InfinityHouse.Economy
{
    /// <summary>
    /// Fusion Chamber: Combines 5 items of same rarity into 1 of next tier.
    /// 100% success rate. Catalyst provides 10% chance for tier skip.
    /// Exotics cannot be fused. Only unequipped items can be used.
    /// Result is marked with fused=true and timestamp.
    /// </summary>
    public class FusionChamber : MonoBehaviour
    {
        // Singleton
        public static FusionChamber Instance { get; private set; }

        // Configuration
        [Header("Fusion Settings")]
        [SerializeField] private int itemsRequired = 5;
        [SerializeField] private float catalystSkipChance = 0.1f; // 10%
        [SerializeField] private bool enableFusion = true;

        // Current fusion state
        private List<CosmeticItem> fusionSlots;
        private bool hasCatalyst = false;

        // Events
        public event Action<CosmeticItem> OnItemAddedToFusion;
        public event Action<CosmeticItem> OnItemRemovedFromFusion;
        public event Action<FusionResult> OnFusionComplete;
        public event Action<string> OnFusionFailed;

        // Properties
        public int SlotsUsed => fusionSlots.Count;
        public int SlotsRemaining => itemsRequired - fusionSlots.Count;
        public bool IsReady => fusionSlots.Count == itemsRequired;
        public bool HasCatalyst => hasCatalyst;

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

            fusionSlots = new List<CosmeticItem>();
        }

        #region Fusion Slot Management

        /// <summary>
        /// Adds an item to fusion slots
        /// </summary>
        public bool AddItemToFusion(CosmeticItem item)
        {
            if (!enableFusion)
            {
                Debug.LogWarning("[FusionChamber] Fusion is disabled");
                return false;
            }

            // Platform check
            #if !UNITY_STANDALONE_WIN && !UNITY_STANDALONE_OSX && !UNITY_STANDALONE_LINUX && !UNITY_EDITOR
            // Mobile platforms can fuse
            #endif

            // Validation
            if (fusionSlots.Count >= itemsRequired)
            {
                Debug.LogWarning("[FusionChamber] All fusion slots are full");
                return false;
            }

            if (item == null)
            {
                Debug.LogWarning("[FusionChamber] Item is null");
                return false;
            }

            // Check if item is fusible
            if (!CanFuseItem(item))
            {
                return false;
            }

            // Check rarity consistency
            if (fusionSlots.Count > 0)
            {
                if (fusionSlots[0].rarity != item.rarity)
                {
                    Debug.LogWarning("[FusionChamber] All items must be same rarity");
                    return false;
                }
            }

            // Check if item is equipped
            if (ShopManager.Instance.IsEquipped(item))
            {
                Debug.LogWarning("[FusionChamber] Cannot fuse equipped items");
                return false;
            }

            // Check ownership
            if (!ShopManager.Instance.OwnsCosmetic(item))
            {
                Debug.LogWarning("[FusionChamber] Player doesn't own this item");
                return false;
            }

            // Add to slots
            fusionSlots.Add(item);
            OnItemAddedToFusion?.Invoke(item);

            Debug.Log($"[FusionChamber] Added {item.cosmeticId} to fusion ({SlotsUsed}/{itemsRequired})");
            return true;
        }

        /// <summary>
        /// Removes an item from fusion slots
        /// </summary>
        public bool RemoveItemFromFusion(CosmeticItem item)
        {
            if (fusionSlots.Contains(item))
            {
                fusionSlots.Remove(item);
                OnItemRemovedFromFusion?.Invoke(item);

                Debug.Log($"[FusionChamber] Removed {item.cosmeticId} from fusion");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Clears all fusion slots
        /// </summary>
        public void ClearFusion()
        {
            fusionSlots.Clear();
            hasCatalyst = false;

            Debug.Log("[FusionChamber] Fusion cleared");
        }

        /// <summary>
        /// Adds a catalyst to the fusion
        /// </summary>
        public void AddCatalyst()
        {
            hasCatalyst = true;
            Debug.Log("[FusionChamber] Catalyst added - 10% chance for tier skip!");
        }

        #endregion

        #region Fusion Execution

        /// <summary>
        /// Executes the fusion
        /// </summary>
        public FusionResult ExecuteFusion()
        {
            if (!IsReady)
            {
                Debug.LogWarning("[FusionChamber] Fusion not ready - need all slots filled");
                OnFusionFailed?.Invoke("Not enough items");
                return null;
            }

            // Get input rarity
            RarityManager.RarityTier inputRarity = fusionSlots[0].rarity;

            // Calculate output rarity
            RarityManager.RarityTier outputRarity = CalculateOutputRarity(inputRarity);

            // Calculate fused condition
            List<float> inputConditions = fusionSlots.Select(item => item.conditionValue).ToList();
            float fusedCondition = RarityManager.Instance.CalculateFusedCondition(inputConditions);

            // Remove input items from inventory
            foreach (var item in fusionSlots)
            {
                ShopManager.Instance.RemoveCosmetic(item);
            }

            // Grant fusion result
            FusionResult result = new FusionResult
            {
                inputItems = new List<CosmeticItem>(fusionSlots),
                inputRarity = inputRarity,
                outputRarity = outputRarity,
                fusedCondition = fusedCondition,
                usedCatalyst = hasCatalyst,
                tierSkipped = outputRarity > RarityManager.Instance.GetNextFusionTier(inputRarity),
                timestamp = DateTime.UtcNow
            };

            // Create fused item
            CosmeticItem fusedItem = CreateFusedItem(outputRarity, fusedCondition);
            result.outputItem = fusedItem;

            // Grant to player
            ShopManager.Instance.GrantCosmetic(fusedItem);

            // Clear fusion
            ClearFusion();

            // Fire event
            OnFusionComplete?.Invoke(result);

            // Track telemetry
            Analytics.TelemetryEvents.Instance?.TrackEvent("fusion_commit", new Dictionary<string, object>
            {
                { "input_rarity", inputRarity.ToString() },
                { "output_rarity", outputRarity.ToString() },
                { "catalyst_used", hasCatalyst },
                { "tier_skipped", result.tierSkipped }
            });

            Debug.Log($"[FusionChamber] Fusion complete: {inputRarity} → {outputRarity} (Condition: {fusedCondition:F2})");

            return result;
        }

        private RarityManager.RarityTier CalculateOutputRarity(RarityManager.RarityTier inputRarity)
        {
            // Check for tier skip with catalyst
            if (hasCatalyst && UnityEngine.Random.value <= catalystSkipChance)
            {
                // Skip one tier!
                RarityManager.RarityTier nextTier = RarityManager.Instance.GetNextFusionTier(inputRarity);
                RarityManager.RarityTier skippedTier = RarityManager.Instance.GetNextFusionTier(nextTier);

                Debug.Log($"[FusionChamber] CATALYST ACTIVATED! Tier skip: {inputRarity} → {skippedTier}");
                return skippedTier;
            }

            // Normal fusion
            return RarityManager.Instance.GetNextFusionTier(inputRarity);
        }

        private CosmeticItem CreateFusedItem(RarityManager.RarityTier rarity, float condition)
        {
            // Get random cosmetic of target rarity
            var allCosmetics = ShopManager.Instance.GetAllCosmetics();
            var targetRarityCosmetics = allCosmetics.Where(c => c.rarity == rarity).ToList();

            if (targetRarityCosmetics.Count == 0)
            {
                Debug.LogError($"[FusionChamber] No cosmetics found for rarity {rarity}");
                return null;
            }

            // Select random
            CosmeticItem template = targetRarityCosmetics[UnityEngine.Random.Range(0, targetRarityCosmetics.Count)];

            // Create fused instance
            CosmeticItem fusedItem = ScriptableObject.CreateInstance<CosmeticItem>();
            fusedItem.cosmeticId = template.cosmeticId;
            fusedItem.cosmeticType = template.cosmeticType;
            fusedItem.rarity = rarity;
            fusedItem.conditionValue = condition;
            fusedItem.isFused = true;
            fusedItem.fusionTimestamp = DateTime.UtcNow;

            return fusedItem;
        }

        #endregion

        #region Validation

        private bool CanFuseItem(CosmeticItem item)
        {
            // Check if exotic
            if (item.rarity == RarityManager.RarityTier.Exotic)
            {
                Debug.LogWarning("[FusionChamber] Exotics cannot be fused");
                OnFusionFailed?.Invoke("Exotics cannot be fused");
                return false;
            }

            // Check if rarity is fusible
            if (!RarityManager.Instance.CanFuse(item.rarity))
            {
                Debug.LogWarning($"[FusionChamber] {item.rarity} items cannot be fused");
                OnFusionFailed?.Invoke($"{item.rarity} items cannot be fused");
                return false;
            }

            // Check if already at max tier
            if (item.rarity == RarityManager.RarityTier.Mythic)
            {
                // Mythic can still be fused (just results in another Mythic)
                // But check if we want to allow this
            }

            return true;
        }

        /// <summary>
        /// Gets list of fusible items from inventory
        /// </summary>
        public List<CosmeticItem> GetFusibleItems()
        {
            var ownedItems = ShopManager.Instance.GetOwnedCosmetics();

            return ownedItems.Where(item =>
                item.rarity != RarityManager.RarityTier.Exotic &&
                !ShopManager.Instance.IsEquipped(item) &&
                RarityManager.Instance.CanFuse(item.rarity)
            ).ToList();
        }

        /// <summary>
        /// Gets fusible items of specific rarity
        /// </summary>
        public List<CosmeticItem> GetFusibleItemsByRarity(RarityManager.RarityTier rarity)
        {
            return GetFusibleItems().Where(item => item.rarity == rarity).ToList();
        }

        #endregion

        #region Data Structures

        [Serializable]
        public class FusionResult
        {
            public List<CosmeticItem> inputItems;
            public RarityManager.RarityTier inputRarity;
            public CosmeticItem outputItem;
            public RarityManager.RarityTier outputRarity;
            public float fusedCondition;
            public bool usedCatalyst;
            public bool tierSkipped;
            public DateTime timestamp;
        }

        #endregion
    }
}
