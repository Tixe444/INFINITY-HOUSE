// ═══════════════════════════════════════════════════════════════════════════════
// INFINITY HOUSE - Made by Mate Makovics
// Set Completion System - Collection Progress Tracking
// ═══════════════════════════════════════════════════════════════════════════════

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using InfinityHouse.Shop;

namespace InfinityHouse.Monetization
{
    /// <summary>
    /// Tracks cosmetic collection sets and completion progress.
    /// Incentivizes completing themed collections with bonus rewards.
    /// Drives conversion through "almost complete" psychology.
    /// </summary>
    public class SetCompletionSystem : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private bool enableSetBonuses = true;
        [SerializeField] private int completionBonusDiamonds = 500;

        // Singleton
        public static SetCompletionSystem Instance { get; private set; }

        // Cosmetic sets
        private Dictionary<string, CosmeticSet> cosmeticSets;
        private Dictionary<string, SetProgress> playerProgress;

        // Events
        public event Action<CosmeticSet, float> OnSetProgressChanged; // set, completion %
        public event Action<CosmeticSet> OnSetCompleted;
        public event Action<CosmeticSet, int> OnSetBonusAwarded; // set, diamond amount

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

            InitializeSets();
            LoadProgress();
        }

        private void Start()
        {
            // Subscribe to shop events
            var shopManager = ShopManager.Instance;
            if (shopManager != null)
            {
                shopManager.OnCosmeticPurchased += HandleCosmeticAcquired;
                shopManager.OnCosmeticGranted += HandleCosmeticAcquired;
            }
        }

        #region Initialization

        private void InitializeSets()
        {
            cosmeticSets = new Dictionary<string, CosmeticSet>();

            // Define themed sets
            CreateSet("halloween", "Halloween Collection", new string[]
            {
                "skin_ghost", "skin_vampire", "skin_witch",
                "trail_pumpkin", "trail_spooky",
                "theme_halloween"
            });

            CreateSet("neon", "Neon Dreams", new string[]
            {
                "skin_neon_pink", "skin_neon_blue",
                "trail_neon_particles", "trail_neon_glow",
                "theme_neon", "filter_neon"
            });

            CreateSet("vintage", "Vintage Noir", new string[]
            {
                "skin_noir", "skin_sepia",
                "trail_film_grain", "theme_vintage",
                "filter_blackwhite", "music_jazz"
            });

            CreateSet("cosmic", "Cosmic Voyager", new string[]
            {
                "skin_stardust", "skin_galaxy",
                "trail_stars", "trail_nebula",
                "theme_space", "filter_cosmic"
            });

            CreateSet("season1_premium", "Season 1 Premium", new string[]
            {
                // Season pass exclusive cosmetics
                "skin_premium_s1", "trail_premium_s1",
                "theme_premium_s1", "filter_premium_s1"
            });

            Debug.Log($"[SetCompletionSystem] Initialized {cosmeticSets.Count} cosmetic sets");
        }

        private void CreateSet(string setId, string displayName, string[] cosmeticIds)
        {
            var set = new CosmeticSet
            {
                setId = setId,
                displayName = displayName,
                cosmeticIds = new List<string>(cosmeticIds),
                bonusDiamonds = completionBonusDiamonds
            };

            cosmeticSets[setId] = set;
        }

        private void LoadProgress()
        {
            playerProgress = new Dictionary<string, SetProgress>();

            foreach (var set in cosmeticSets.Values)
            {
                var progress = new SetProgress
                {
                    setId = set.setId,
                    ownedCosmetics = new List<string>(),
                    isCompleted = false,
                    bonusClaimed = false
                };

                playerProgress[set.setId] = progress;
            }

            // Load owned cosmetics from ShopManager
            UpdateAllProgress();
        }

        #endregion

        #region Progress Tracking

        private void HandleCosmeticAcquired(CosmeticItem cosmetic)
        {
            // Check which sets contain this cosmetic
            foreach (var set in cosmeticSets.Values)
            {
                if (set.cosmeticIds.Contains(cosmetic.cosmeticId))
                {
                    UpdateSetProgress(set, cosmetic.cosmeticId);
                }
            }
        }

        private void UpdateSetProgress(CosmeticSet set, string cosmeticId)
        {
            SetProgress progress = playerProgress[set.setId];

            // Add to owned if not already
            if (!progress.ownedCosmetics.Contains(cosmeticId))
            {
                progress.ownedCosmetics.Add(cosmeticId);

                float completionPercent = (float)progress.ownedCosmetics.Count / set.cosmeticIds.Count;
                OnSetProgressChanged?.Invoke(set, completionPercent);

                Debug.Log($"[SetCompletionSystem] {set.displayName}: {completionPercent * 100:F0}% complete");

                // Check for completion
                if (!progress.isCompleted && progress.ownedCosmetics.Count >= set.cosmeticIds.Count)
                {
                    CompleteSet(set);
                }
            }
        }

        private void CompleteSet(CosmeticSet set)
        {
            SetProgress progress = playerProgress[set.setId];
            progress.isCompleted = true;

            OnSetCompleted?.Invoke(set);

            // Award completion bonus
            if (enableSetBonuses && !progress.bonusClaimed)
            {
                AwardSetBonus(set);
            }

            Debug.Log($"[SetCompletionSystem] SET COMPLETE: {set.displayName}!");
        }

        private void AwardSetBonus(CosmeticSet set)
        {
            SetProgress progress = playerProgress[set.setId];

            if (progress.bonusClaimed)
            {
                Debug.LogWarning($"[SetCompletionSystem] Bonus already claimed for {set.setId}");
                return;
            }

            // Grant bonus diamonds
            DiamondCurrencyManager.Instance?.AddDiamonds(set.bonusDiamonds, $"Set completion: {set.displayName}");

            progress.bonusClaimed = true;

            OnSetBonusAwarded?.Invoke(set, set.bonusDiamonds);
            Debug.Log($"[SetCompletionSystem] Awarded {set.bonusDiamonds} diamonds for completing {set.displayName}");
        }

        private void UpdateAllProgress()
        {
            var shopManager = ShopManager.Instance;
            if (shopManager == null) return;

            List<string> ownedCosmetics = shopManager.GetOwnedCosmeticIds();

            foreach (var set in cosmeticSets.Values)
            {
                SetProgress progress = playerProgress[set.setId];
                progress.ownedCosmetics.Clear();

                foreach (string cosmeticId in set.cosmeticIds)
                {
                    if (ownedCosmetics.Contains(cosmeticId))
                    {
                        progress.ownedCosmetics.Add(cosmeticId);
                    }
                }

                // Check completion
                if (progress.ownedCosmetics.Count >= set.cosmeticIds.Count)
                {
                    progress.isCompleted = true;
                }
            }
        }

        #endregion

        #region Query Methods

        /// <summary>
        /// Gets all available sets
        /// </summary>
        public List<CosmeticSet> GetAllSets()
        {
            return new List<CosmeticSet>(cosmeticSets.Values);
        }

        /// <summary>
        /// Gets progress for a specific set
        /// </summary>
        public SetProgress GetSetProgress(string setId)
        {
            if (playerProgress.ContainsKey(setId))
            {
                return playerProgress[setId];
            }
            return null;
        }

        /// <summary>
        /// Gets completion percentage for a set
        /// </summary>
        public float GetCompletionPercentage(string setId)
        {
            if (!cosmeticSets.ContainsKey(setId) || !playerProgress.ContainsKey(setId))
            {
                return 0f;
            }

            CosmeticSet set = cosmeticSets[setId];
            SetProgress progress = playerProgress[setId];

            return (float)progress.ownedCosmetics.Count / set.cosmeticIds.Count;
        }

        /// <summary>
        /// Checks if a set is complete
        /// </summary>
        public bool IsSetComplete(string setId)
        {
            if (!playerProgress.ContainsKey(setId))
            {
                return false;
            }

            return playerProgress[setId].isCompleted;
        }

        /// <summary>
        /// Gets missing cosmetics for a set
        /// </summary>
        public List<string> GetMissingCosmetics(string setId)
        {
            if (!cosmeticSets.ContainsKey(setId) || !playerProgress.ContainsKey(setId))
            {
                return new List<string>();
            }

            CosmeticSet set = cosmeticSets[setId];
            SetProgress progress = playerProgress[setId];

            return set.cosmeticIds.Where(id => !progress.ownedCosmetics.Contains(id)).ToList();
        }

        /// <summary>
        /// Gets sets that are almost complete (for conversion psychology)
        /// </summary>
        public List<CosmeticSet> GetNearlyCompleteSets(float threshold = 0.75f)
        {
            List<CosmeticSet> nearlycomplete = new List<CosmeticSet>();

            foreach (var set in cosmeticSets.Values)
            {
                float completion = GetCompletionPercentage(set.setId);
                if (completion >= threshold && completion < 1.0f)
                {
                    nearlycomplete.Add(set);
                }
            }

            return nearlycomplete.OrderByDescending(s => GetCompletionPercentage(s.setId)).ToList();
        }

        #endregion

        #region Data Structures

        [Serializable]
        public class CosmeticSet
        {
            public string setId;
            public string displayName;
            public List<string> cosmeticIds;
            public int bonusDiamonds;
            public string iconSpritePath; // Optional
        }

        [Serializable]
        public class SetProgress
        {
            public string setId;
            public List<string> ownedCosmetics;
            public bool isCompleted;
            public bool bonusClaimed;

            public int OwnedCount => ownedCosmetics.Count;
        }

        #endregion

        private void OnDestroy()
        {
            var shopManager = ShopManager.Instance;
            if (shopManager != null)
            {
                shopManager.OnCosmeticPurchased -= HandleCosmeticAcquired;
                shopManager.OnCosmeticGranted -= HandleCosmeticAcquired;
            }
        }
    }
}
