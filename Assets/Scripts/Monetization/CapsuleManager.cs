// ═══════════════════════════════════════════════════════════════════════════════
// INFINITY HOUSE - Made by Mate Makovics
// Capsule Manager - Randomized Cosmetic Packs with Odds Disclosure
// ═══════════════════════════════════════════════════════════════════════════════

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using InfinityHouse.Shop;

namespace InfinityHouse.Monetization
{
    /// <summary>
    /// Capsule/Loot box system with transparent odds disclosure.
    /// Compliant with Apple, Google, and international regulations.
    /// Features duplicate protection and pity systems.
    /// </summary>
    public class CapsuleManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private TextAsset oddsDisclosureJson;
        [SerializeField] private bool enableDuplicateProtection = true;
        [SerializeField] private bool enablePitySystem = true;

        [Header("Pity System")]
        [SerializeField] private int guaranteedEpicAfter = 10; // Opens
        [SerializeField] private int guaranteedLegendaryAfter = 30; // Opens

        // Singleton
        public static CapsuleManager Instance { get; private set; }

        // Capsule definitions
        private Dictionary<string, CapsuleType> capsuleTypes;
        private Dictionary<CosmeticItem.RarityTier, float> baseOdds;

        // Player state
        private int opensSinceEpic = 0;
        private int opensSinceLegendary = 0;
        private List<string> ownedCosmetics;

        // Events
        public event Action<string, List<CosmeticItem>> OnCapsuleOpened; // capsuleId, contents
        public event Action<CapsuleType> OnCapsulePreview; // Show odds before purchase
        public event Action<CosmeticItem> OnNewCosmeticUnlocked;
        public event Action<CosmeticItem> OnDuplicateReceived;

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

            InitializeCapsules();
            LoadPlayerState();
        }

        #region Initialization

        private void InitializeCapsules()
        {
            capsuleTypes = new Dictionary<string, CapsuleType>();

            // Load odds from JSON if available
            if (oddsDisclosureJson != null)
            {
                LoadOddsFromJson();
            }
            else
            {
                CreateDefaultCapsules();
            }

            // Base rarity odds (example - adjust for balance)
            baseOdds = new Dictionary<CosmeticItem.RarityTier, float>
            {
                { CosmeticItem.RarityTier.Common, 0.50f },      // 50%
                { CosmeticItem.RarityTier.Rare, 0.30f },        // 30%
                { CosmeticItem.RarityTier.Epic, 0.15f },        // 15%
                { CosmeticItem.RarityTier.Legendary, 0.04f },   // 4%
                { CosmeticItem.RarityTier.Mythic, 0.01f }       // 1%
            };
        }

        private void LoadOddsFromJson()
        {
            try
            {
                var config = JsonUtility.FromJson<OddsDisclosureConfig>(oddsDisclosureJson.text);

                foreach (var capsule in config.capsules)
                {
                    capsuleTypes[capsule.capsuleId] = capsule;
                }

                Debug.Log($"[CapsuleManager] Loaded {capsuleTypes.Count} capsule types from JSON");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CapsuleManager] Failed to load odds JSON: {ex.Message}");
                CreateDefaultCapsules();
            }
        }

        private void CreateDefaultCapsules()
        {
            // Starter Capsule (Common-Rare)
            capsuleTypes["capsule_starter"] = new CapsuleType
            {
                capsuleId = "capsule_starter",
                displayName = "Starter Capsule",
                description = "Contains 3 cosmetics (Common-Rare)",
                itemCount = 3,
                guaranteedMinRarity = CosmeticItem.RarityTier.Common,
                rarityWeights = new Dictionary<CosmeticItem.RarityTier, float>
                {
                    { CosmeticItem.RarityTier.Common, 0.70f },
                    { CosmeticItem.RarityTier.Rare, 0.30f }
                }
            };

            // Premium Capsule (Rare-Legendary)
            capsuleTypes["capsule_premium"] = new CapsuleType
            {
                capsuleId = "capsule_premium",
                displayName = "Premium Capsule",
                description = "Contains 5 cosmetics (Rare-Legendary guaranteed)",
                itemCount = 5,
                guaranteedMinRarity = CosmeticItem.RarityTier.Rare,
                rarityWeights = new Dictionary<CosmeticItem.RarityTier, float>
                {
                    { CosmeticItem.RarityTier.Rare, 0.60f },
                    { CosmeticItem.RarityTier.Epic, 0.30f },
                    { CosmeticItem.RarityTier.Legendary, 0.10f }
                }
            };

            Debug.Log("[CapsuleManager] Created default capsule types");
        }

        private void LoadPlayerState()
        {
            ownedCosmetics = new List<string>();

            // Load from ShopManager
            var shopManager = ShopManager.Instance;
            if (shopManager != null)
            {
                ownedCosmetics = shopManager.GetOwnedCosmeticIds();
            }

            // Load pity counters
            opensSinceEpic = PlayerPrefs.GetInt("CapsuleOpensSinceEpic", 0);
            opensSinceLegendary = PlayerPrefs.GetInt("CapsuleOpensSinceLegendary", 0);
        }

        #endregion

        #region Capsule Opening

        /// <summary>
        /// Opens a capsule and returns the contents
        /// </summary>
        public List<string> OpenCapsule(string capsuleId)
        {
            if (!capsuleTypes.ContainsKey(capsuleId))
            {
                Debug.LogError($"[CapsuleManager] Unknown capsule type: {capsuleId}");
                return new List<string>();
            }

            CapsuleType capsule = capsuleTypes[capsuleId];
            List<CosmeticItem> contents = GenerateCapsuleContents(capsule);

            // Grant items
            List<string> cosmeticIds = new List<string>();
            foreach (var cosmetic in contents)
            {
                GrantCosmetic(cosmetic);
                cosmeticIds.Add(cosmetic.cosmeticId);
            }

            // Update pity counters
            UpdatePityCounters(contents);

            // Fire events
            OnCapsuleOpened?.Invoke(capsuleId, contents);

            Debug.Log($"[CapsuleManager] Opened {capsuleId}: {contents.Count} items");

            return cosmeticIds;
        }

        private List<CosmeticItem> GenerateCapsuleContents(CapsuleType capsule)
        {
            List<CosmeticItem> contents = new List<CosmeticItem>();
            var availableCosmetics = GetAvailableCosmetics();

            // Apply pity system
            bool guaranteeEpic = enablePitySystem && opensSinceEpic >= guaranteedEpicAfter;
            bool guaranteeLegendary = enablePitySystem && opensSinceLegendary >= guaranteedLegendaryAfter;

            for (int i = 0; i < capsule.itemCount; i++)
            {
                CosmeticItem.RarityTier targetRarity;

                // First item: apply guarantees
                if (i == 0)
                {
                    if (guaranteeLegendary)
                    {
                        targetRarity = CosmeticItem.RarityTier.Legendary;
                    }
                    else if (guaranteeEpic)
                    {
                        targetRarity = CosmeticItem.RarityTier.Epic;
                    }
                    else
                    {
                        targetRarity = RollRarity(capsule);
                    }
                }
                // Last item: guaranteed minimum rarity
                else if (i == capsule.itemCount - 1)
                {
                    // Ensure at least one item meets minimum rarity
                    bool hasMinRarity = contents.Any(c => c.rarity >= capsule.guaranteedMinRarity);
                    if (!hasMinRarity)
                    {
                        targetRarity = capsule.guaranteedMinRarity;
                    }
                    else
                    {
                        targetRarity = RollRarity(capsule);
                    }
                }
                else
                {
                    targetRarity = RollRarity(capsule);
                }

                // Select cosmetic
                CosmeticItem cosmetic = SelectCosmeticOfRarity(availableCosmetics, targetRarity);
                if (cosmetic != null)
                {
                    contents.Add(cosmetic);

                    // Remove from available if duplicate protection enabled
                    if (enableDuplicateProtection)
                    {
                        availableCosmetics.Remove(cosmetic);
                    }
                }
            }

            return contents;
        }

        private CosmeticItem.RarityTier RollRarity(CapsuleType capsule)
        {
            float roll = UnityEngine.Random.value;
            float cumulative = 0f;

            foreach (var kvp in capsule.rarityWeights.OrderBy(x => x.Value))
            {
                cumulative += kvp.Value;
                if (roll <= cumulative)
                {
                    return kvp.Key;
                }
            }

            // Fallback
            return CosmeticItem.RarityTier.Common;
        }

        private List<CosmeticItem> GetAvailableCosmetics()
        {
            var shopManager = ShopManager.Instance;
            if (shopManager == null)
            {
                Debug.LogError("[CapsuleManager] ShopManager not found!");
                return new List<CosmeticItem>();
            }

            List<CosmeticItem> available = shopManager.GetAllCosmetics();

            // If duplicate protection enabled, remove owned cosmetics
            if (enableDuplicateProtection)
            {
                available = available.Where(c => !ownedCosmetics.Contains(c.cosmeticId)).ToList();
            }

            return available;
        }

        private CosmeticItem SelectCosmeticOfRarity(List<CosmeticItem> pool, CosmeticItem.RarityTier rarity)
        {
            // Filter by rarity
            var filtered = pool.Where(c => c.rarity == rarity).ToList();

            if (filtered.Count == 0)
            {
                // Fallback: select from entire pool
                Debug.LogWarning($"[CapsuleManager] No cosmetics of rarity {rarity} available");
                return pool.Count > 0 ? pool[UnityEngine.Random.Range(0, pool.Count)] : null;
            }

            return filtered[UnityEngine.Random.Range(0, filtered.Count)];
        }

        private void GrantCosmetic(CosmeticItem cosmetic)
        {
            var shopManager = ShopManager.Instance;
            if (shopManager == null) return;

            bool isNew = !ownedCosmetics.Contains(cosmetic.cosmeticId);

            // Grant cosmetic
            shopManager.GrantCosmetic(cosmetic);
            ownedCosmetics.Add(cosmetic.cosmeticId);

            // Fire appropriate event
            if (isNew)
            {
                OnNewCosmeticUnlocked?.Invoke(cosmetic);
            }
            else
            {
                OnDuplicateReceived?.Invoke(cosmetic);

                // Convert duplicate to currency (e.g., 20% of diamond cost)
                int refund = Mathf.RoundToInt(cosmetic.diamondCost * 0.2f);
                DiamondCurrencyManager.Instance?.AddDiamonds(refund, "Duplicate cosmetic");
            }
        }

        private void UpdatePityCounters(List<CosmeticItem> contents)
        {
            bool hasEpic = contents.Any(c => c.rarity >= CosmeticItem.RarityTier.Epic);
            bool hasLegendary = contents.Any(c => c.rarity >= CosmeticItem.RarityTier.Legendary);

            if (hasLegendary)
            {
                opensSinceLegendary = 0;
                opensSinceEpic = 0;
            }
            else if (hasEpic)
            {
                opensSinceEpic = 0;
                opensSinceLegendary++;
            }
            else
            {
                opensSinceEpic++;
                opensSinceLegendary++;
            }

            SavePityCounters();
        }

        private void SavePityCounters()
        {
            PlayerPrefs.SetInt("CapsuleOpensSinceEpic", opensSinceEpic);
            PlayerPrefs.SetInt("CapsuleOpensSinceLegendary", opensSinceLegendary);
            PlayerPrefs.Save();
        }

        #endregion

        #region Odds Disclosure

        /// <summary>
        /// Shows detailed odds disclosure for a capsule (required by regulations)
        /// </summary>
        public void ShowOddsDisclosure(string capsuleId)
        {
            if (!capsuleTypes.ContainsKey(capsuleId))
            {
                Debug.LogError($"[CapsuleManager] Unknown capsule: {capsuleId}");
                return;
            }

            CapsuleType capsule = capsuleTypes[capsuleId];
            OnCapsulePreview?.Invoke(capsule);

            // Log odds for transparency
            Debug.Log($"=== CAPSULE ODDS: {capsule.displayName} ===");
            Debug.Log($"Contains: {capsule.itemCount} items");
            Debug.Log($"Guaranteed minimum: {capsule.guaranteedMinRarity}");
            Debug.Log("Drop rates:");

            foreach (var kvp in capsule.rarityWeights.OrderByDescending(x => x.Value))
            {
                Debug.Log($"  {kvp.Key}: {kvp.Value * 100:F2}%");
            }

            if (enablePitySystem)
            {
                Debug.Log($"Pity System:");
                Debug.Log($"  Epic guaranteed after {guaranteedEpicAfter} opens (current: {opensSinceEpic})");
                Debug.Log($"  Legendary guaranteed after {guaranteedLegendaryAfter} opens (current: {opensSinceLegendary})");
            }

            Debug.Log("=====================================");
        }

        /// <summary>
        /// Gets formatted odds disclosure string for UI (required by Apple/Google)
        /// </summary>
        public string GetOddsDisclosureText(string capsuleId)
        {
            if (!capsuleTypes.ContainsKey(capsuleId))
            {
                return "Odds information unavailable.";
            }

            CapsuleType capsule = capsuleTypes[capsuleId];
            string text = $"{capsule.displayName}\n\n";
            text += $"Contains: {capsule.itemCount} cosmetic items\n";
            text += $"Guaranteed: At least one {capsule.guaranteedMinRarity} or better\n\n";
            text += "Drop Rates:\n";

            foreach (var kvp in capsule.rarityWeights.OrderByDescending(x => (int)x.Key))
            {
                text += $"• {kvp.Key}: {kvp.Value * 100:F2}%\n";
            }

            if (enablePitySystem)
            {
                text += $"\nPity System:\n";
                text += $"• Epic or better guaranteed after {guaranteedEpicAfter} opens without one\n";
                text += $"• Legendary or better guaranteed after {guaranteedLegendaryAfter} opens without one\n";
            }

            if (enableDuplicateProtection)
            {
                text += "\nDuplicate Protection:\n";
                text += "• Duplicate cosmetics are converted to Diamonds (20% refund)\n";
            }

            return text;
        }

        #endregion

        #region Data Structures

        [Serializable]
        public class CapsuleType
        {
            public string capsuleId;
            public string displayName;
            public string description;
            public int itemCount;
            public CosmeticItem.RarityTier guaranteedMinRarity;
            public Dictionary<CosmeticItem.RarityTier, float> rarityWeights;
        }

        [Serializable]
        private class OddsDisclosureConfig
        {
            public List<CapsuleType> capsules;
        }

        #endregion
    }
}
