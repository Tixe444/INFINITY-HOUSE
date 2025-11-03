// ═══════════════════════════════════════════════════════════════════════════════
// INFINITY HOUSE - Made by Mate Makovics
// Offer Personalizer - Behavioral Targeting & Cohort Segmentation
// ═══════════════════════════════════════════════════════════════════════════════

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using InfinityHouse.Save;

namespace InfinityHouse.Monetization
{
    /// <summary>
    /// Personalizes offers based on player behavior, spending patterns, and engagement.
    /// Implements cohort segmentation and ethical conversion tactics.
    /// Privacy-compliant behavioral analysis.
    /// </summary>
    public class OfferPersonalizer : MonoBehaviour
    {
        [Header("Segmentation")]
        [SerializeField] private int daysForWhaleClassification = 7;
        [SerializeField] private float whaleSpendingThreshold = 50f; // USD

        [Header("Personalization")]
        [SerializeField] private bool enablePersonalizedOffers = true;
        [SerializeField] private bool enableRetentionOffers = true;

        // Singleton
        public static OfferPersonalizer Instance { get; private set; }

        // Player profile
        private PlayerSpendingProfile spendingProfile;
        private PlayerCohort currentCohort;

        // Events
        public event Action<PlayerCohort> OnCohortChanged;
        public event Action<PersonalizedOffer> OnPersonalizedOfferGenerated;

        // Properties
        public PlayerCohort CurrentCohort => currentCohort;
        public bool IsWhale => currentCohort == PlayerCohort.Whale;
        public bool IsMinnow => currentCohort == PlayerCohort.Minnow;
        public bool IsDolphin => currentCohort == PlayerCohort.Dolphin;

        public enum PlayerCohort
        {
            NonSpender,     // Never purchased
            Minnow,         // Small spenders ($0-$5)
            Dolphin,        // Medium spenders ($5-$50)
            Whale           // Big spenders ($50+)
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

            LoadSpendingProfile();
            ClassifyPlayer();
        }

        private void Start()
        {
            // Subscribe to purchase events
            var purchaseGateway = IAP.PurchaseGateway.Instance;
            if (purchaseGateway != null)
            {
                purchaseGateway.OnPurchaseCompleted += HandlePurchaseCompleted;
            }
        }

        #region Profile Management

        private void LoadSpendingProfile()
        {
            string json = PlayerPrefs.GetString("PlayerSpendingProfile", "");

            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    spendingProfile = JsonUtility.FromJson<PlayerSpendingProfile>(json);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[OfferPersonalizer] Failed to load profile: {ex.Message}");
                    CreateNewProfile();
                }
            }
            else
            {
                CreateNewProfile();
            }
        }

        private void CreateNewProfile()
        {
            spendingProfile = new PlayerSpendingProfile
            {
                totalSpent = 0f,
                purchaseCount = 0,
                firstPurchaseDate = DateTime.MinValue,
                lastPurchaseDate = DateTime.MinValue,
                purchaseHistory = new List<PurchaseRecord>(),
                daysSinceLastPurchase = 0,
                averagePurchaseValue = 0f
            };

            SaveProfile();
        }

        private void SaveProfile()
        {
            string json = JsonUtility.ToJson(spendingProfile);
            PlayerPrefs.SetString("PlayerSpendingProfile", json);
            PlayerPrefs.Save();
        }

        private void HandlePurchaseCompleted(IAP.PurchaseGateway.PurchaseResult result)
        {
            // Get product info
            float amount = GetPurchaseAmount(result.productId);

            // Update profile
            spendingProfile.totalSpent += amount;
            spendingProfile.purchaseCount++;
            spendingProfile.lastPurchaseDate = DateTime.UtcNow;

            if (spendingProfile.firstPurchaseDate == DateTime.MinValue)
            {
                spendingProfile.firstPurchaseDate = DateTime.UtcNow;
            }

            // Add to history
            spendingProfile.purchaseHistory.Add(new PurchaseRecord
            {
                productId = result.productId,
                amount = amount,
                date = DateTime.UtcNow
            });

            // Recalculate average
            spendingProfile.averagePurchaseValue = spendingProfile.totalSpent / spendingProfile.purchaseCount;

            SaveProfile();

            // Reclassify
            ClassifyPlayer();

            Debug.Log($"[OfferPersonalizer] Purchase recorded: ${amount:F2}. Total: ${spendingProfile.totalSpent:F2}");
        }

        private float GetPurchaseAmount(string productId)
        {
            // TODO: Get actual price from IAP catalog
            var prices = new Dictionary<string, float>
            {
                { "diamonds_100", 0.99f },
                { "diamonds_500", 4.99f },
                { "diamonds_1000", 9.99f },
                { "diamonds_2500", 19.99f },
                { "diamonds_5000", 49.99f },
                { "seasonpass_s1", 9.99f },
                { "capsule_starter", 1.99f },
                { "capsule_premium", 2.99f }
            };

            return prices.ContainsKey(productId) ? prices[productId] : 0f;
        }

        #endregion

        #region Player Classification

        private void ClassifyPlayer()
        {
            PlayerCohort oldCohort = currentCohort;

            // Calculate recent spending (last X days)
            DateTime cutoff = DateTime.UtcNow.AddDays(-daysForWhaleClassification);
            float recentSpending = spendingProfile.purchaseHistory
                .Where(p => p.date >= cutoff)
                .Sum(p => p.amount);

            // Classify based on total and recent spending
            if (spendingProfile.purchaseCount == 0)
            {
                currentCohort = PlayerCohort.NonSpender;
            }
            else if (recentSpending >= whaleSpendingThreshold || spendingProfile.totalSpent >= whaleSpendingThreshold)
            {
                currentCohort = PlayerCohort.Whale;
            }
            else if (spendingProfile.totalSpent >= 5f)
            {
                currentCohort = PlayerCohort.Dolphin;
            }
            else
            {
                currentCohort = PlayerCohort.Minnow;
            }

            // Fire event if cohort changed
            if (oldCohort != currentCohort)
            {
                OnCohortChanged?.Invoke(currentCohort);
                Debug.Log($"[OfferPersonalizer] Player cohort changed: {oldCohort} → {currentCohort}");
            }

            // Update days since last purchase
            if (spendingProfile.lastPurchaseDate != DateTime.MinValue)
            {
                spendingProfile.daysSinceLastPurchase = (int)(DateTime.UtcNow - spendingProfile.lastPurchaseDate).TotalDays;
            }
        }

        #endregion

        #region Personalized Offers

        /// <summary>
        /// Generates personalized offer based on player profile
        /// </summary>
        public PersonalizedOffer GeneratePersonalizedOffer()
        {
            if (!enablePersonalizedOffers)
            {
                return null;
            }

            PersonalizedOffer offer = null;

            switch (currentCohort)
            {
                case PlayerCohort.NonSpender:
                    offer = GenerateFirstPurchaseOffer();
                    break;

                case PlayerCohort.Minnow:
                    offer = GenerateMinnowOffer();
                    break;

                case PlayerCohort.Dolphin:
                    offer = GenerateDolphinOffer();
                    break;

                case PlayerCohort.Whale:
                    offer = GenerateWhaleOffer();
                    break;
            }

            if (offer != null)
            {
                OnPersonalizedOfferGenerated?.Invoke(offer);
            }

            return offer;
        }

        private PersonalizedOffer GenerateFirstPurchaseOffer()
        {
            // Special first-time buyer offer
            return new PersonalizedOffer
            {
                offerId = Guid.NewGuid().ToString(),
                displayName = "Welcome Pack",
                description = "Get started with this exclusive first-time offer!",
                productId = "diamonds_500",
                discountPercent = 0.50f, // 50% off
                bonusDiamonds = 100,
                targetCohort = PlayerCohort.NonSpender,
                expiresAt = DateTime.UtcNow.AddDays(3)
            };
        }

        private PersonalizedOffer GenerateMinnowOffer()
        {
            // Value-focused offer for small spenders
            return new PersonalizedOffer
            {
                offerId = Guid.NewGuid().ToString(),
                displayName = "Great Value Pack",
                description = "Best value for your diamonds!",
                productId = "diamonds_1000",
                discountPercent = 0.30f,
                bonusDiamonds = 200,
                targetCohort = PlayerCohort.Minnow,
                expiresAt = DateTime.UtcNow.AddDays(1)
            };
        }

        private PersonalizedOffer GenerateDolphinOffer()
        {
            // Mid-tier bundle for regular spenders
            return new PersonalizedOffer
            {
                offerId = Guid.NewGuid().ToString(),
                displayName = "Loyal Player Bundle",
                description = "Exclusive offer for valued players!",
                productId = "diamonds_2500",
                discountPercent = 0.35f,
                bonusDiamonds = 500,
                includedCosmetics = new List<string> { "cosmetic_premium_skin" },
                targetCohort = PlayerCohort.Dolphin,
                expiresAt = DateTime.UtcNow.AddHours(12)
            };
        }

        private PersonalizedOffer GenerateWhaleOffer()
        {
            // Premium exclusive for high spenders
            return new PersonalizedOffer
            {
                offerId = Guid.NewGuid().ToString(),
                displayName = "VIP Exclusive Pack",
                description = "Premium bundle reserved for our most loyal supporters!",
                productId = "diamonds_5000",
                discountPercent = 0.40f,
                bonusDiamonds = 1500,
                includedCosmetics = new List<string> { "cosmetic_exclusive_vip", "cosmetic_legendary_skin" },
                targetCohort = PlayerCohort.Whale,
                expiresAt = DateTime.UtcNow.AddHours(6)
            };
        }

        /// <summary>
        /// Generates retention offer for inactive players
        /// </summary>
        public PersonalizedOffer GenerateRetentionOffer()
        {
            if (!enableRetentionOffers)
            {
                return null;
            }

            // Only offer retention if player hasn't purchased in a while
            if (spendingProfile.daysSinceLastPurchase < 7)
            {
                return null;
            }

            return new PersonalizedOffer
            {
                offerId = Guid.NewGuid().ToString(),
                displayName = "We Miss You!",
                description = "Come back with this special returning player offer!",
                productId = "diamonds_1000",
                discountPercent = 0.60f, // Deep discount for retention
                bonusDiamonds = 300,
                targetCohort = currentCohort,
                expiresAt = DateTime.UtcNow.AddDays(7)
            };
        }

        #endregion

        #region Conversion Psychology

        /// <summary>
        /// Determines if player is at risk of churning
        /// </summary>
        public bool IsChurnRisk()
        {
            // Engaged players who haven't purchased
            if (currentCohort == PlayerCohort.NonSpender)
            {
                // Check engagement (TODO: integrate with analytics)
                return true; // Placeholder
            }

            // Previous spenders who haven't purchased recently
            if (spendingProfile.daysSinceLastPurchase >= 14)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Calculates optimal discount for conversion
        /// </summary>
        public float CalculateOptimalDiscount()
        {
            // Higher discounts for non-spenders and churn risk
            if (currentCohort == PlayerCohort.NonSpender)
            {
                return 0.50f; // 50%
            }

            if (IsChurnRisk())
            {
                return 0.40f; // 40%
            }

            // Standard discounts
            switch (currentCohort)
            {
                case PlayerCohort.Minnow:
                    return 0.30f;
                case PlayerCohort.Dolphin:
                    return 0.25f;
                case PlayerCohort.Whale:
                    return 0.20f;
                default:
                    return 0.30f;
            }
        }

        /// <summary>
        /// Gets recommended products for this player
        /// </summary>
        public List<string> GetRecommendedProducts()
        {
            switch (currentCohort)
            {
                case PlayerCohort.NonSpender:
                    return new List<string> { "diamonds_100", "capsule_starter" };

                case PlayerCohort.Minnow:
                    return new List<string> { "diamonds_500", "diamonds_1000" };

                case PlayerCohort.Dolphin:
                    return new List<string> { "diamonds_1000", "diamonds_2500", "seasonpass_s1" };

                case PlayerCohort.Whale:
                    return new List<string> { "diamonds_2500", "diamonds_5000", "capsule_premium" };

                default:
                    return new List<string>();
            }
        }

        #endregion

        #region Data Structures

        [Serializable]
        public class PlayerSpendingProfile
        {
            public float totalSpent;
            public int purchaseCount;
            public DateTime firstPurchaseDate;
            public DateTime lastPurchaseDate;
            public int daysSinceLastPurchase;
            public float averagePurchaseValue;
            public List<PurchaseRecord> purchaseHistory;
        }

        [Serializable]
        public class PurchaseRecord
        {
            public string productId;
            public float amount;
            public DateTime date;
        }

        [Serializable]
        public class PersonalizedOffer
        {
            public string offerId;
            public string displayName;
            public string description;
            public string productId;
            public float discountPercent;
            public int bonusDiamonds;
            public List<string> includedCosmetics;
            public PlayerCohort targetCohort;
            public DateTime expiresAt;

            public string DiscountLabel => $"{discountPercent * 100:F0}% OFF";
        }

        #endregion

        private void OnDestroy()
        {
            var purchaseGateway = IAP.PurchaseGateway.Instance;
            if (purchaseGateway != null)
            {
                purchaseGateway.OnPurchaseCompleted -= HandlePurchaseCompleted;
            }
        }
    }
}
