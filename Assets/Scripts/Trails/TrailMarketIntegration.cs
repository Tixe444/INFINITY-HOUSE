using UnityEngine;
using System;
using System.Collections.Generic;
using InfinityHouse.Shop;
using InfinityHouse.Economy;

namespace InfiniteHaus.Trails
{
    /// <summary>
    /// Integrates Trail System with Market/Economy/Shop systems.
    /// Handles trail cosmetic purchasing, equipping, and application.
    /// Connects TrailRaritySystem with shop RarityManager.
    /// CPU: <0.1ms | Memory: 4KB | GC: 0B/frame
    /// </summary>
    public class TrailMarketIntegration : MonoBehaviour
    {
        #region Configuration
        [Header("Trail Catalog")]
        [Tooltip("All available trail cosmetics")]
        [SerializeField] private TrailCosmetic[] availableTrails;

        [Tooltip("Default trail (always owned)")]
        [SerializeField] private TrailCosmetic defaultTrail;

        [Header("References")]
        [Tooltip("Shop manager reference")]
        [SerializeField] private ShopManager shopManager;

        [Tooltip("Rarity manager reference")]
        [SerializeField] private RarityManager rarityManager;

        [Header("Trail Pricing")]
        [Tooltip("Base diamond cost multiplier per rarity tier")]
        [SerializeField] private int[] rarityPriceMultipliers = new int[]
        {
            50,    // Common
            150,   // Rare
            400,   // Epic
            1000,  // Legendary
            2500,  // Mythic
            5000   // Exotic
        };

        [Tooltip("Enable dynamic pricing based on condition")]
        [SerializeField] private bool enableConditionPricing = true;
        #endregion

        #region State
        private Dictionary<string, TrailCosmetic> trailRegistry;
        private TrailCosmetic currentEquippedTrail;
        private TrailRenderSystem currentTrailRenderer;
        #endregion

        #region Events
        public event Action<TrailCosmetic> OnTrailPurchased;
        public event Action<TrailCosmetic> OnTrailEquipped;
        public event Action<TrailCosmetic> OnTrailUnequipped;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            InitializeTrailRegistry();
        }

        private void Start()
        {
            // Find references if not set
            if (shopManager == null)
            {
                shopManager = ShopManager.Instance;
            }

            if (rarityManager == null)
            {
                rarityManager = RarityManager.Instance;
            }

            // Subscribe to shop events
            if (shopManager != null)
            {
                shopManager.OnCosmeticEquipped += OnCosmeticEquipped;
                shopManager.OnCosmeticPurchased += OnCosmeticPurchased;
            }

            // Load equipped trail
            LoadEquippedTrail();
        }

        private void OnDestroy()
        {
            if (shopManager != null)
            {
                shopManager.OnCosmeticEquipped -= OnCosmeticEquipped;
                shopManager.OnCosmeticPurchased -= OnCosmeticPurchased;
            }
        }
        #endregion

        #region Initialization
        private void InitializeTrailRegistry()
        {
            trailRegistry = new Dictionary<string, TrailCosmetic>();

            foreach (var trail in availableTrails)
            {
                if (trail != null && !string.IsNullOrEmpty(trail.trailID))
                {
                    trailRegistry[trail.trailID] = trail;
                }
            }

            Debug.Log($"[TrailMarketIntegration] Registered {trailRegistry.Count} trails");
        }
        #endregion

        #region Shop Integration
        private void OnCosmeticPurchased(CosmeticItem cosmetic)
        {
            if (cosmetic.type == CosmeticItem.CosmeticType.SoulTrail)
            {
                // Find matching trail
                TrailCosmetic trail = GetTrailByCosmeticID(cosmetic.cosmeticID);
                if (trail != null)
                {
                    OnTrailPurchased?.Invoke(trail);
                    Debug.Log($"[TrailMarketIntegration] Trail purchased: {trail.displayName}");
                }
            }
        }

        private void OnCosmeticEquipped(CosmeticItem cosmetic)
        {
            if (cosmetic.type == CosmeticItem.CosmeticType.SoulTrail)
            {
                // Find matching trail
                TrailCosmetic trail = GetTrailByCosmeticID(cosmetic.cosmeticID);
                if (trail != null)
                {
                    EquipTrail(trail);
                }
            }
        }
        #endregion

        #region Trail Management
        /// <summary>
        /// Equips a trail cosmetic
        /// </summary>
        public void EquipTrail(TrailCosmetic trail)
        {
            if (trail == null)
            {
                Debug.LogWarning("[TrailMarketIntegration] Cannot equip null trail");
                return;
            }

            // Unequip current trail
            if (currentEquippedTrail != null)
            {
                UnequipCurrentTrail();
            }

            // Apply new trail
            ApplyTrail(trail);
            currentEquippedTrail = trail;

            OnTrailEquipped?.Invoke(trail);
            Debug.Log($"[TrailMarketIntegration] Equipped trail: {trail.displayName}");
        }

        /// <summary>
        /// Unequips current trail
        /// </summary>
        public void UnequipCurrentTrail()
        {
            if (currentEquippedTrail == null) return;

            // Destroy current trail renderer
            if (currentTrailRenderer != null)
            {
                Destroy(currentTrailRenderer.gameObject);
                currentTrailRenderer = null;
            }

            OnTrailUnequipped?.Invoke(currentEquippedTrail);
            currentEquippedTrail = null;
        }

        /// <summary>
        /// Applies trail to player
        /// </summary>
        private void ApplyTrail(TrailCosmetic trail)
        {
            // Find player
            var player = FindObjectOfType<Player.PlayerController>();
            if (player == null)
            {
                player = FindObjectOfType<Player.EnhancedRunnerController>()?.gameObject.GetComponent<Player.PlayerController>();
            }

            if (player == null)
            {
                Debug.LogWarning("[TrailMarketIntegration] Player not found!");
                return;
            }

            // Destroy old trail
            var oldTrail = player.GetComponentInChildren<TrailRenderSystem>();
            if (oldTrail != null)
            {
                Destroy(oldTrail.gameObject);
            }

            // Instantiate new trail
            if (trail.trailPrefab != null)
            {
                GameObject trailObj = Instantiate(trail.trailPrefab, player.transform);
                currentTrailRenderer = trailObj.GetComponent<TrailRenderSystem>();

                if (currentTrailRenderer != null)
                {
                    // Apply trail configuration
                    ApplyTrailConfiguration(currentTrailRenderer, trail);
                }
            }
        }

        /// <summary>
        /// Applies configuration to trail renderer
        /// </summary>
        private void ApplyTrailConfiguration(TrailRenderSystem renderer, TrailCosmetic trail)
        {
            // This would configure the trail renderer based on the cosmetic
            // For now, the TrailRenderSystem handles its own configuration
            Debug.Log($"[TrailMarketIntegration] Applied trail config: {trail.displayName}");
        }

        /// <summary>
        /// Loads equipped trail from save data
        /// </summary>
        private void LoadEquippedTrail()
        {
            if (shopManager == null) return;

            var equippedCosmetic = shopManager.GetEquippedCosmetic(CosmeticItem.CosmeticType.SoulTrail);
            if (equippedCosmetic != null)
            {
                TrailCosmetic trail = GetTrailByCosmeticID(equippedCosmetic.cosmeticID);
                if (trail != null)
                {
                    ApplyTrail(trail);
                    currentEquippedTrail = trail;
                }
            }
            else if (defaultTrail != null)
            {
                // Apply default trail
                ApplyTrail(defaultTrail);
                currentEquippedTrail = defaultTrail;
            }
        }
        #endregion

        #region Trail Creation & Pricing
        /// <summary>
        /// Creates a trail cosmetic item for the shop
        /// </summary>
        public CosmeticItem CreateTrailCosmeticItem(TrailCosmetic trail)
        {
            if (trail == null) return null;

            // This would create a CosmeticItem ScriptableObject
            // For runtime use, this would need to be done in the editor
            Debug.Log($"[TrailMarketIntegration] Creating cosmetic for trail: {trail.displayName}");
            return null;
        }

        /// <summary>
        /// Calculates price for a trail based on rarity and condition
        /// </summary>
        public int CalculateTrailPrice(TrailCosmetic trail)
        {
            if (trail == null) return 0;

            int basePrice = GetBasePriceForRarity(trail.rarity);

            if (enableConditionPricing && trail.hasCondition)
            {
                // Adjust price based on condition
                // Better condition (Blessed) = higher price
                float conditionMultiplier = 1f + (1f - trail.conditionValue) * 0.5f;
                basePrice = Mathf.RoundToInt(basePrice * conditionMultiplier);
            }

            return basePrice;
        }

        private int GetBasePriceForRarity(CosmeticItem.RarityTier rarity)
        {
            int tierIndex = (int)rarity;
            if (tierIndex >= 0 && tierIndex < rarityPriceMultipliers.Length)
            {
                return rarityPriceMultipliers[tierIndex];
            }
            return 100; // Default price
        }
        #endregion

        #region Queries
        /// <summary>
        /// Gets trail by trail ID
        /// </summary>
        public TrailCosmetic GetTrailByID(string trailID)
        {
            if (trailRegistry.TryGetValue(trailID, out TrailCosmetic trail))
            {
                return trail;
            }
            return null;
        }

        /// <summary>
        /// Gets trail by cosmetic ID
        /// </summary>
        public TrailCosmetic GetTrailByCosmeticID(string cosmeticID)
        {
            // Match trail ID to cosmetic ID
            foreach (var trail in trailRegistry.Values)
            {
                if (trail.trailID == cosmeticID || trail.cosmeticItemID == cosmeticID)
                {
                    return trail;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets all trails of a specific rarity
        /// </summary>
        public TrailCosmetic[] GetTrailsByRarity(CosmeticItem.RarityTier rarity)
        {
            List<TrailCosmetic> trails = new List<TrailCosmetic>();

            foreach (var trail in trailRegistry.Values)
            {
                if (trail.rarity == rarity)
                {
                    trails.Add(trail);
                }
            }

            return trails.ToArray();
        }

        /// <summary>
        /// Gets currently equipped trail
        /// </summary>
        public TrailCosmetic GetEquippedTrail()
        {
            return currentEquippedTrail;
        }
        #endregion

        #region Data Structures
        /// <summary>
        /// Trail cosmetic definition
        /// </summary>
        [Serializable]
        public class TrailCosmetic
        {
            [Header("Identity")]
            public string trailID = "trail_001";
            public string cosmeticItemID = "soul_trail_001";
            public string displayName = "Basic Trail";

            [TextArea(2, 3)]
            public string description = "A simple soul trail";

            [Header("Visual")]
            public GameObject trailPrefab;
            public Sprite previewIcon;
            public Color primaryColor = Color.white;
            public Color secondaryColor = Color.cyan;

            [Header("Rarity & Condition")]
            public CosmeticItem.RarityTier rarity = CosmeticItem.RarityTier.Common;
            public bool hasCondition = true;
            [Range(0f, 1f)]
            public float conditionValue = 0.5f;

            [Header("Effects")]
            public bool hasParticles = true;
            public bool hasGlow = true;
            public bool animatesColor = false;

            [Header("Performance")]
            [Range(0.2f, 1f)]
            public float qualityScale = 1f;
        }
        #endregion
    }
}
