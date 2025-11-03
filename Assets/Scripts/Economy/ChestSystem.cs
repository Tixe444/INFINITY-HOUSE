// ═══════════════════════════════════════════════════════════════════════════════
// INFINITY HOUSE - Made by Mate Makovics
// Chest System - 5-Tier Chest Opening System
// ═══════════════════════════════════════════════════════════════════════════════

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using InfinityHouse.Shop;

namespace InfinityHouse.Economy
{
    /// <summary>
    /// Manages 5 types of chests: Basic, Crystal, Mythic, Eternal Vault, Obsidian Vault.
    /// Basic: Free, Common-Rare. Crystal: Shards/Diamonds, Rare-Epic. Mythic: Diamonds, Epic-Legendary-Mythic.
    /// Eternal Vault: Event-only, Legendary-Mythic-Exotic. Obsidian Vault: Rare/Premium, guarantees Legendary+.
    /// Exotics only drop from Eternal/Obsidian Vaults or Diamond shop.
    /// </summary>
    public class ChestSystem : MonoBehaviour
    {
        // Singleton
        public static ChestSystem Instance { get; private set; }

        // Configuration
        [Header("Configuration")]
        [SerializeField] private TextAsset economyConfigJson;
        [SerializeField] private bool enableRemoteConfig = false;

        // Chest definitions
        private Dictionary<ChestType, ChestData> chestDefinitions;

        // Events
        public event Action<ChestType, List<CosmeticItem>> OnChestOpened;
        public event Action<ChestType, CosmeticItem> OnRareItemReceived;
        public event Action<string> OnChestOpenFailed;

        public enum ChestType
        {
            Basic,          // Free, Common-Rare
            Crystal,        // Shards/Diamonds, Rare-Epic
            Mythic,         // Diamonds, Epic-Legendary-Mythic
            EternalVault,   // Event, Legendary-Mythic-Exotic
            ObsidianVault   // Premium, Legendary+ guaranteed
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

            InitializeChests();
        }

        #region Initialization

        private void InitializeChests()
        {
            chestDefinitions = new Dictionary<ChestType, ChestData>();

            // Basic Chest - Gray-blue, free after runs
            chestDefinitions[ChestType.Basic] = new ChestData
            {
                chestType = ChestType.Basic,
                displayName = "Basic Chest",
                description = "Free chest with Common-Rare items",
                itemCount = 1,
                costShards = 0,
                costDiamonds = 0,
                isFree = true,
                visualTheme = "gray_blue",
                rarityWeights = new Dictionary<RarityManager.RarityTier, float>
                {
                    { RarityManager.RarityTier.Common, 0.75f },
                    { RarityManager.RarityTier.Rare, 0.25f }
                }
            };

            // Crystal Chest - Crystalline, costs Shards or Diamonds
            chestDefinitions[ChestType.Crystal] = new ChestData
            {
                chestType = ChestType.Crystal,
                displayName = "Crystal Chest",
                description = "Rare-Epic items",
                itemCount = 3,
                costShards = 500,
                costDiamonds = 50,
                isFree = false,
                visualTheme = "crystalline",
                rarityWeights = new Dictionary<RarityManager.RarityTier, float>
                {
                    { RarityManager.RarityTier.Rare, 0.60f },
                    { RarityManager.RarityTier.Epic, 0.35f },
                    { RarityManager.RarityTier.Legendary, 0.05f }
                }
            };

            // Mythic Chest - Gold glow, Diamonds only
            chestDefinitions[ChestType.Mythic] = new ChestData
            {
                chestType = ChestType.Mythic,
                displayName = "Mythic Chest",
                description = "Epic-Legendary-Mythic items",
                itemCount = 5,
                costShards = 0,
                costDiamonds = 200,
                isFree = false,
                visualTheme = "gold_glow",
                rarityWeights = new Dictionary<RarityManager.RarityTier, float>
                {
                    { RarityManager.RarityTier.Epic, 0.50f },
                    { RarityManager.RarityTier.Legendary, 0.40f },
                    { RarityManager.RarityTier.Mythic, 0.10f }
                }
            };

            // Eternal Vault - Black metal with burning runes, event-only
            chestDefinitions[ChestType.EternalVault] = new ChestData
            {
                chestType = ChestType.EternalVault,
                displayName = "Eternal Vault",
                description = "Legendary-Mythic-Exotic items - EVENT ONLY",
                itemCount = 7,
                costShards = 0,
                costDiamonds = 0,
                isFree = false,
                requiresEvent = true,
                visualTheme = "black_metal_runes",
                rarityWeights = new Dictionary<RarityManager.RarityTier, float>
                {
                    { RarityManager.RarityTier.Legendary, 0.50f },
                    { RarityManager.RarityTier.Mythic, 0.45f },
                    { RarityManager.RarityTier.Exotic, 0.05f }
                }
            };

            // Obsidian Vault - Black with gold particles, premium
            chestDefinitions[ChestType.ObsidianVault] = new ChestData
            {
                chestType = ChestType.ObsidianVault,
                displayName = "Obsidian Vault",
                description = "Guaranteed Legendary+ items",
                itemCount = 5,
                costShards = 0,
                costDiamonds = 500,
                isFree = false,
                guaranteedMinRarity = RarityManager.RarityTier.Legendary,
                visualTheme = "black_gold_particles",
                rarityWeights = new Dictionary<RarityManager.RarityTier, float>
                {
                    { RarityManager.RarityTier.Legendary, 0.70f },
                    { RarityManager.RarityTier.Mythic, 0.25f },
                    { RarityManager.RarityTier.Exotic, 0.05f }
                }
            };

            Debug.Log("[ChestSystem] Initialized 5 chest types");
        }

        #endregion

        #region Chest Opening

        /// <summary>
        /// Opens a chest
        /// </summary>
        public List<CosmeticItem> OpenChest(ChestType chestType, bool useDiamonds = false)
        {
            if (!chestDefinitions.ContainsKey(chestType))
            {
                Debug.LogError($"[ChestSystem] Unknown chest type: {chestType}");
                OnChestOpenFailed?.Invoke("Unknown chest type");
                return null;
            }

            ChestData chest = chestDefinitions[chestType];

            // Validate purchase
            if (!ValidateChestPurchase(chest, useDiamonds))
            {
                return null;
            }

            // Charge currency
            if (!chest.isFree)
            {
                if (useDiamonds && chest.costDiamonds > 0)
                {
                    Shop.DiamondCurrencyManager.Instance.TrySpendDiamonds(chest.costDiamonds, chest.displayName);
                }
                else if (!useDiamonds && chest.costShards > 0)
                {
                    ShardCurrencyManager.Instance.TrySpendShards(chest.costShards, chest.displayName);
                }
            }

            // Generate contents
            List<CosmeticItem> contents = GenerateChestContents(chest);

            // Fire events
            OnChestOpened?.Invoke(chestType, contents);

            // Check for rare items
            foreach (var item in contents)
            {
                if (item.rarity >= RarityManager.RarityTier.Legendary)
                {
                    OnRareItemReceived?.Invoke(chestType, item);
                }
            }

            // Track telemetry
            Analytics.TelemetryEvents.Instance?.TrackEvent("chest_open", new Dictionary<string, object>
            {
                { "chest_type", chestType.ToString() },
                { "item_count", contents.Count },
                { "payment_method", useDiamonds ? "diamonds" : (chest.isFree ? "free" : "shards") }
            });

            Debug.Log($"[ChestSystem] Opened {chestType}: {contents.Count} items");

            return contents;
        }

        private bool ValidateChestPurchase(ChestData chest, bool useDiamonds)
        {
            // Check if event-only
            if (chest.requiresEvent)
            {
                // TODO: Check if event is active
                bool eventActive = false; // Placeholder
                if (!eventActive)
                {
                    Debug.LogWarning($"[ChestSystem] {chest.displayName} requires an active event");
                    OnChestOpenFailed?.Invoke("Event required");
                    return false;
                }
            }

            // Check currency
            if (!chest.isFree)
            {
                if (useDiamonds)
                {
                    if (chest.costDiamonds == 0)
                    {
                        Debug.LogWarning($"[ChestSystem] {chest.displayName} cannot be purchased with Diamonds");
                        OnChestOpenFailed?.Invoke("Cannot use Diamonds");
                        return false;
                    }

                    if (!Shop.DiamondCurrencyManager.Instance.CanAfford(chest.costDiamonds))
                    {
                        Debug.LogWarning($"[ChestSystem] Insufficient Diamonds: {chest.costDiamonds}");
                        OnChestOpenFailed?.Invoke("Insufficient Diamonds");
                        return false;
                    }
                }
                else
                {
                    if (chest.costShards == 0)
                    {
                        Debug.LogWarning($"[ChestSystem] {chest.displayName} cannot be purchased with Shards");
                        OnChestOpenFailed?.Invoke("Cannot use Shards");
                        return false;
                    }

                    if (!ShardCurrencyManager.Instance.CanAfford(chest.costShards))
                    {
                        Debug.LogWarning($"[ChestSystem] Insufficient Shards: {chest.costShards}");
                        OnChestOpenFailed?.Invoke("Insufficient Shards");
                        return false;
                    }
                }
            }

            return true;
        }

        private List<CosmeticItem> GenerateChestContents(ChestData chest)
        {
            List<CosmeticItem> contents = new List<CosmeticItem>();

            for (int i = 0; i < chest.itemCount; i++)
            {
                // Roll rarity
                RarityManager.RarityTier rarity = RollChestRarity(chest);

                // Apply guaranteed min rarity on last item
                if (i == chest.itemCount - 1 && chest.guaranteedMinRarity != RarityManager.RarityTier.Common)
                {
                    bool hasMinRarity = contents.Any(item => item.rarity >= chest.guaranteedMinRarity);
                    if (!hasMinRarity)
                    {
                        rarity = chest.guaranteedMinRarity;
                    }
                }

                // Roll condition
                float condition = RarityManager.Instance.RollCondition();

                // Create cosmetic
                CosmeticItem item = CreateRandomCosmetic(rarity, condition);

                if (item != null)
                {
                    contents.Add(item);

                    // Grant to player or convert to shards if duplicate
                    GrantOrConvertItem(item);
                }
            }

            return contents;
        }

        private RarityManager.RarityTier RollChestRarity(ChestData chest)
        {
            float roll = UnityEngine.Random.value;
            float cumulative = 0f;

            // Order by rarity (descending for excitement)
            var sortedWeights = chest.rarityWeights.OrderByDescending(x => (int)x.Key);

            foreach (var kvp in sortedWeights)
            {
                cumulative += kvp.Value;
                if (roll <= cumulative)
                {
                    return kvp.Key;
                }
            }

            // Fallback
            return RarityManager.RarityTier.Common;
        }

        private CosmeticItem CreateRandomCosmetic(RarityManager.RarityTier rarity, float condition)
        {
            // Get all cosmetics of target rarity
            var allCosmetics = ShopManager.Instance.GetAllCosmetics();
            var targetCosmetics = allCosmetics.Where(c => c.rarity == rarity).ToList();

            if (targetCosmetics.Count == 0)
            {
                Debug.LogWarning($"[ChestSystem] No cosmetics found for rarity {rarity}");
                return null;
            }

            // Select random
            CosmeticItem template = targetCosmetics[UnityEngine.Random.Range(0, targetCosmetics.Count)];

            // Create instance with condition
            CosmeticItem item = ScriptableObject.CreateInstance<CosmeticItem>();
            item.cosmeticId = template.cosmeticId;
            item.cosmeticType = template.cosmeticType;
            item.rarity = rarity;
            item.conditionValue = condition;
            item.isFused = false;

            return item;
        }

        private void GrantOrConvertItem(CosmeticItem item)
        {
            // Check if already owned
            if (ShopManager.Instance.OwnsCosmetic(item))
            {
                // Convert to shards (mobile only)
                #if UNITY_IOS || UNITY_ANDROID
                ShardCurrencyManager.Instance.ConvertDuplicateToShards(item.rarity);
                Analytics.TelemetryEvents.Instance?.TrackEvent("duplicate_converted", new Dictionary<string, object>
                {
                    { "rarity", item.rarity.ToString() },
                    { "shard_value", RarityManager.Instance.GetShardValue(item.rarity) }
                });
                #endif
            }
            else
            {
                // Grant new item
                ShopManager.Instance.GrantCosmetic(item);
            }
        }

        #endregion

        #region Query Methods

        /// <summary>
        /// Gets chest data for a type
        /// </summary>
        public ChestData GetChestData(ChestType chestType)
        {
            return chestDefinitions.ContainsKey(chestType) ? chestDefinitions[chestType] : null;
        }

        /// <summary>
        /// Checks if player can afford a chest
        /// </summary>
        public bool CanAffordChest(ChestType chestType, bool useDiamonds)
        {
            if (!chestDefinitions.ContainsKey(chestType)) return false;

            ChestData chest = chestDefinitions[chestType];

            if (chest.isFree) return true;

            if (useDiamonds)
            {
                return Shop.DiamondCurrencyManager.Instance.CanAfford(chest.costDiamonds);
            }
            else
            {
                return ShardCurrencyManager.Instance.CanAfford(chest.costShards);
            }
        }

        #endregion

        #region Data Structures

        [Serializable]
        public class ChestData
        {
            public ChestType chestType;
            public string displayName;
            public string description;
            public int itemCount;
            public int costShards;
            public int costDiamonds;
            public bool isFree;
            public bool requiresEvent;
            public RarityManager.RarityTier guaranteedMinRarity;
            public string visualTheme;
            public Dictionary<RarityManager.RarityTier, float> rarityWeights;
        }

        #endregion
    }
}
