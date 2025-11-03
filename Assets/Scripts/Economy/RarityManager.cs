// ═══════════════════════════════════════════════════════════════════════════════
// INFINITY HOUSE - Made by Mate Makovics
// Rarity Manager - 6-Tier Rarity System with Condition
// ═══════════════════════════════════════════════════════════════════════════════

using UnityEngine;
using System;
using System.Collections.Generic;

namespace InfinityHouse.Economy
{
    /// <summary>
    /// Manages 6-tier rarity system: Common, Rare, Epic, Legendary, Mythic, Exotic.
    /// Includes Condition system (Blessed, Awakened, Withered, Cursed).
    /// Handles visual effects, drop rates, and shard values.
    /// </summary>
    public class RarityManager : MonoBehaviour
    {
        // Singleton
        public static RarityManager Instance { get; private set; }

        // Rarity definitions
        private Dictionary<RarityTier, RarityData> rarityData;
        private Dictionary<ConditionTier, ConditionData> conditionData;

        // Configuration
        [Header("Configuration")]
        [SerializeField] private TextAsset economyConfigJson;
        [SerializeField] private bool enableConditionSystem = true;

        public enum RarityTier
        {
            Common = 0,
            Rare = 1,
            Epic = 2,
            Legendary = 3,
            Mythic = 4,
            Exotic = 5
        }

        public enum ConditionTier
        {
            Blessed,    // 0.00-0.25
            Awakened,   // 0.25-0.50
            Withered,   // 0.50-0.75
            Cursed      // 0.75-1.00
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

            InitializeRarityData();
            InitializeConditionData();
        }

        #region Initialization

        private void InitializeRarityData()
        {
            rarityData = new Dictionary<RarityTier, RarityData>();

            // Common - Gray (55%)
            rarityData[RarityTier.Common] = new RarityData
            {
                tier = RarityTier.Common,
                displayName = "Common",
                color = new Color(0.5f, 0.5f, 0.5f), // Gray
                dropRate = 0.55f,
                shardValue = 25,
                fusionCost = 5,
                canFuse = true
            };

            // Rare - Blue (25%)
            rarityData[RarityTier.Rare] = new RarityData
            {
                tier = RarityTier.Rare,
                displayName = "Rare",
                color = new Color(0.25f, 0.41f, 0.88f), // Blue
                dropRate = 0.25f,
                shardValue = 75,
                fusionCost = 5,
                canFuse = true
            };

            // Epic - Purple (12%)
            rarityData[RarityTier.Epic] = new RarityData
            {
                tier = RarityTier.Epic,
                displayName = "Epic",
                color = new Color(0.58f, 0.44f, 0.86f), // Purple
                dropRate = 0.12f,
                shardValue = 150,
                fusionCost = 5,
                canFuse = true
            };

            // Legendary - Gold (6%)
            rarityData[RarityTier.Legendary] = new RarityData
            {
                tier = RarityTier.Legendary,
                displayName = "Legendary",
                color = new Color(1f, 0.84f, 0f), // Gold
                dropRate = 0.06f,
                shardValue = 400,
                fusionCost = 5,
                canFuse = true
            };

            // Mythic - Red with violet glow (1.5%)
            rarityData[RarityTier.Mythic] = new RarityData
            {
                tier = RarityTier.Mythic,
                displayName = "Mythic",
                color = new Color(0.86f, 0.08f, 0.24f), // Crimson
                glowColor = new Color(1f, 0.08f, 0.58f), // Hot Pink glow
                dropRate = 0.015f,
                shardValue = 1000,
                fusionCost = 5,
                canFuse = true,
                hasGlow = true
            };

            // Exotic - Iridescent Black (0.1-0.2%)
            rarityData[RarityTier.Exotic] = new RarityData
            {
                tier = RarityTier.Exotic,
                displayName = "Exotic",
                color = Color.black,
                reflectionColors = new Color[]
                {
                    new Color(0f, 1f, 1f), // Cyan
                    new Color(1f, 0f, 1f)  // Magenta
                },
                dropRate = 0.002f,
                shardValue = 2500,
                fusionCost = 0,
                canFuse = false,
                hasSpecialEffect = true,
                specialEffect = "iridescent"
            };

            Debug.Log("[RarityManager] Initialized 6 rarity tiers");
        }

        private void InitializeConditionData()
        {
            conditionData = new Dictionary<ConditionTier, ConditionData>();

            // Blessed (0.00-0.25) - Bright, clear
            conditionData[ConditionTier.Blessed] = new ConditionData
            {
                tier = ConditionTier.Blessed,
                displayName = "Blessed",
                minValue = 0f,
                maxValue = 0.25f,
                brightnessMultiplier = 1.3f,
                marketValueMultiplier = 1.15f
            };

            // Awakened (0.25-0.50) - Clean, vibrant
            conditionData[ConditionTier.Awakened] = new ConditionData
            {
                tier = ConditionTier.Awakened,
                displayName = "Awakened",
                minValue = 0.25f,
                maxValue = 0.5f,
                brightnessMultiplier = 1.1f,
                marketValueMultiplier = 1.05f
            };

            // Withered (0.50-0.75) - Dull, slightly worn
            conditionData[ConditionTier.Withered] = new ConditionData
            {
                tier = ConditionTier.Withered,
                displayName = "Withered",
                minValue = 0.5f,
                maxValue = 0.75f,
                brightnessMultiplier = 0.9f,
                marketValueMultiplier = 0.95f
            };

            // Cursed (0.75-1.00) - Dark, flickering
            conditionData[ConditionTier.Cursed] = new ConditionData
            {
                tier = ConditionTier.Cursed,
                displayName = "Cursed",
                minValue = 0.75f,
                maxValue = 1f,
                brightnessMultiplier = 0.7f,
                marketValueMultiplier = 0.9f
            };

            Debug.Log("[RarityManager] Initialized condition system");
        }

        #endregion

        #region Rarity Queries

        /// <summary>
        /// Gets rarity data for a tier
        /// </summary>
        public RarityData GetRarityData(RarityTier tier)
        {
            return rarityData.ContainsKey(tier) ? rarityData[tier] : null;
        }

        /// <summary>
        /// Rolls a random rarity based on drop rates
        /// </summary>
        public RarityTier RollRarity(bool includeExotic = false)
        {
            float roll = UnityEngine.Random.value;
            float cumulative = 0f;

            // Roll from highest to lowest for excitement
            if (includeExotic)
            {
                cumulative += rarityData[RarityTier.Exotic].dropRate;
                if (roll <= cumulative) return RarityTier.Exotic;
            }

            cumulative += rarityData[RarityTier.Mythic].dropRate;
            if (roll <= cumulative) return RarityTier.Mythic;

            cumulative += rarityData[RarityTier.Legendary].dropRate;
            if (roll <= cumulative) return RarityTier.Legendary;

            cumulative += rarityData[RarityTier.Epic].dropRate;
            if (roll <= cumulative) return RarityTier.Epic;

            cumulative += rarityData[RarityTier.Rare].dropRate;
            if (roll <= cumulative) return RarityTier.Rare;

            return RarityTier.Common;
        }

        /// <summary>
        /// Gets color for a rarity tier
        /// </summary>
        public Color GetRarityColor(RarityTier tier)
        {
            return rarityData.ContainsKey(tier) ? rarityData[tier].color : Color.white;
        }

        /// <summary>
        /// Gets shard value for a rarity tier
        /// </summary>
        public int GetShardValue(RarityTier tier)
        {
            return rarityData.ContainsKey(tier) ? rarityData[tier].shardValue : 0;
        }

        /// <summary>
        /// Checks if rarity can be fused
        /// </summary>
        public bool CanFuse(RarityTier tier)
        {
            return rarityData.ContainsKey(tier) && rarityData[tier].canFuse;
        }

        /// <summary>
        /// Gets next tier in fusion chain
        /// </summary>
        public RarityTier GetNextFusionTier(RarityTier current)
        {
            if (current == RarityTier.Exotic) return RarityTier.Exotic; // Cannot fuse

            int nextTier = (int)current + 1;
            if (nextTier > (int)RarityTier.Mythic) return RarityTier.Mythic; // Cap at Mythic

            return (RarityTier)nextTier;
        }

        #endregion

        #region Condition System

        /// <summary>
        /// Rolls a random condition value (0.0-1.0)
        /// </summary>
        public float RollCondition()
        {
            if (!enableConditionSystem) return 0.5f;

            return UnityEngine.Random.value;
        }

        /// <summary>
        /// Gets condition tier from value
        /// </summary>
        public ConditionTier GetConditionTier(float value)
        {
            foreach (var kvp in conditionData)
            {
                if (value >= kvp.Value.minValue && value < kvp.Value.maxValue)
                {
                    return kvp.Key;
                }
            }

            return ConditionTier.Cursed; // Fallback
        }

        /// <summary>
        /// Gets condition data
        /// </summary>
        public ConditionData GetConditionData(ConditionTier tier)
        {
            return conditionData.ContainsKey(tier) ? conditionData[tier] : null;
        }

        /// <summary>
        /// Calculates fused condition from multiple items
        /// </summary>
        public float CalculateFusedCondition(List<float> inputConditions)
        {
            if (inputConditions == null || inputConditions.Count == 0)
            {
                return RollCondition();
            }

            // Average + random variance
            float average = 0f;
            foreach (float condition in inputConditions)
            {
                average += condition;
            }
            average /= inputConditions.Count;

            // Apply variance
            float variance = UnityEngine.Random.Range(-0.05f, 0.05f);
            float result = Mathf.Clamp01(average + variance);

            return result;
        }

        /// <summary>
        /// Gets display string for condition
        /// </summary>
        public string GetConditionDisplay(float value)
        {
            ConditionTier tier = GetConditionTier(value);
            string tierName = conditionData[tier].displayName;
            return $"{tierName} ({value:F2})";
        }

        /// <summary>
        /// Gets brightness multiplier for condition
        /// </summary>
        public float GetConditionBrightness(float value)
        {
            ConditionTier tier = GetConditionTier(value);
            return conditionData[tier].brightnessMultiplier;
        }

        /// <summary>
        /// Gets market value multiplier for condition
        /// </summary>
        public float GetConditionMarketMultiplier(float value)
        {
            ConditionTier tier = GetConditionTier(value);
            return conditionData[tier].marketValueMultiplier;
        }

        #endregion

        #region Data Structures

        [Serializable]
        public class RarityData
        {
            public RarityTier tier;
            public string displayName;
            public Color color;
            public Color glowColor;
            public Color[] reflectionColors;
            public float dropRate;
            public int shardValue;
            public int fusionCost;
            public bool canFuse;
            public bool hasGlow;
            public bool hasSpecialEffect;
            public string specialEffect;
        }

        [Serializable]
        public class ConditionData
        {
            public ConditionTier tier;
            public string displayName;
            public float minValue;
            public float maxValue;
            public float brightnessMultiplier;
            public float marketValueMultiplier;
        }

        #endregion
    }
}
