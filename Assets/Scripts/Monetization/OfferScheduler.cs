// ═══════════════════════════════════════════════════════════════════════════════
// INFINITY HOUSE - Made by Mate Makovics
// Offer Scheduler - Time-Limited Offers & Rotations
// ═══════════════════════════════════════════════════════════════════════════════

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace InfinityHouse.Monetization
{
    /// <summary>
    /// Manages time-limited offers, daily deals, and featured rotations.
    /// Uses server time to prevent client-side time manipulation.
    /// Implements scarcity psychology and FOMO (Fear of Missing Out).
    /// </summary>
    public class OfferScheduler : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private string serverTimeUrl = "https://worldtimeapi.org/api/timezone/Etc/UTC";
        [SerializeField] private bool useServerTime = true;

        [Header("Rotation Timing")]
        [SerializeField] private int dailyOfferCount = 2;
        [SerializeField] private int weeklyOfferCount = 1;
        [SerializeField] private int featuredRotationHours = 6;

        // Singleton
        public static OfferScheduler Instance { get; private set; }

        // Server time sync
        private DateTime serverTime;
        private bool serverTimeSynced = false;
        private float timeSinceSync = 0f;

        // Active offers
        private List<TimedOffer> activeOffers;
        private TimedOffer featuredOffer;

        // Events
        public event Action<TimedOffer> OnNewOfferAvailable;
        public event Action<TimedOffer> OnOfferExpired;
        public event Action<TimedOffer> OnOfferPurchased;
        public event Action<TimedOffer> OnFeaturedOfferChanged;

        // Properties
        public DateTime CurrentTime => serverTimeSynced ? serverTime.AddSeconds(timeSinceSync) : DateTime.UtcNow;
        public bool IsServerTimeSynced => serverTimeSynced;

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

            activeOffers = new List<TimedOffer>();
        }

        private void Start()
        {
            if (useServerTime)
            {
                SyncServerTime();
            }
            else
            {
                serverTime = DateTime.UtcNow;
                serverTimeSynced = true;
            }

            RefreshOffers();
            InvokeRepeating(nameof(CheckOfferExpirations), 1f, 60f); // Check every minute
        }

        private void Update()
        {
            if (serverTimeSynced)
            {
                timeSinceSync += Time.deltaTime;
            }
        }

        #region Server Time Sync

        private async void SyncServerTime()
        {
            Debug.Log("[OfferScheduler] Syncing with server time...");

            try
            {
                var request = UnityEngine.Networking.UnityWebRequest.Get(serverTimeUrl);
                var operation = request.SendWebRequest();

                while (!operation.isDone)
                {
                    await System.Threading.Tasks.Task.Yield();
                }

                if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    string json = request.downloadHandler.text;
                    var timeData = JsonUtility.FromJson<ServerTimeResponse>(json);

                    if (DateTime.TryParse(timeData.datetime, out DateTime parsedTime))
                    {
                        serverTime = parsedTime;
                        serverTimeSynced = true;
                        timeSinceSync = 0f;

                        Debug.Log($"[OfferScheduler] Server time synced: {serverTime}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[OfferScheduler] Time sync failed: {request.error}");
                    FallbackToLocalTime();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OfferScheduler] Time sync error: {ex.Message}");
                FallbackToLocalTime();
            }
        }

        private void FallbackToLocalTime()
        {
            Debug.LogWarning("[OfferScheduler] Using local time (WARNING: susceptible to time cheats)");
            serverTime = DateTime.UtcNow;
            serverTimeSynced = true;
        }

        [Serializable]
        private class ServerTimeResponse
        {
            public string datetime;
        }

        #endregion

        #region Offer Management

        private void RefreshOffers()
        {
            DateTime now = CurrentTime;

            // Generate daily offers
            if (ShouldRefreshDaily(now))
            {
                GenerateDailyOffers();
            }

            // Generate weekly offers
            if (ShouldRefreshWeekly(now))
            {
                GenerateWeeklyOffers();
            }

            // Rotate featured offer
            if (ShouldRotateFeatured(now))
            {
                RotateFeaturedOffer();
            }
        }

        private bool ShouldRefreshDaily(DateTime now)
        {
            string lastDailyKey = "LastDailyOfferRefresh";
            string lastDailyStr = PlayerPrefs.GetString(lastDailyKey, "");

            if (string.IsNullOrEmpty(lastDailyStr))
            {
                return true;
            }

            DateTime lastDaily = DateTime.Parse(lastDailyStr);
            return now.Date > lastDaily.Date;
        }

        private bool ShouldRefreshWeekly(DateTime now)
        {
            string lastWeeklyKey = "LastWeeklyOfferRefresh";
            string lastWeeklyStr = PlayerPrefs.GetString(lastWeeklyKey, "");

            if (string.IsNullOrEmpty(lastWeeklyStr))
            {
                return true;
            }

            DateTime lastWeekly = DateTime.Parse(lastWeeklyStr);

            // Check if new week (Monday)
            return now.DayOfWeek == DayOfWeek.Monday && lastWeekly.DayOfWeek != DayOfWeek.Monday;
        }

        private bool ShouldRotateFeatured(DateTime now)
        {
            if (featuredOffer == null) return true;

            return now >= featuredOffer.expiresAt;
        }

        private void GenerateDailyOffers()
        {
            DateTime now = CurrentTime;

            // Remove old daily offers
            activeOffers.RemoveAll(o => o.offerType == TimedOffer.OfferType.Daily);

            // Generate new daily offers
            for (int i = 0; i < dailyOfferCount; i++)
            {
                TimedOffer offer = CreateRandomOffer(TimedOffer.OfferType.Daily);
                offer.expiresAt = now.Date.AddDays(1); // Expires at midnight
                activeOffers.Add(offer);

                OnNewOfferAvailable?.Invoke(offer);
            }

            PlayerPrefs.SetString("LastDailyOfferRefresh", now.ToString());
            PlayerPrefs.Save();

            Debug.Log($"[OfferScheduler] Generated {dailyOfferCount} daily offers");
        }

        private void GenerateWeeklyOffers()
        {
            DateTime now = CurrentTime;

            // Remove old weekly offers
            activeOffers.RemoveAll(o => o.offerType == TimedOffer.OfferType.Weekly);

            // Generate new weekly offers
            for (int i = 0; i < weeklyOfferCount; i++)
            {
                TimedOffer offer = CreateRandomOffer(TimedOffer.OfferType.Weekly);

                // Expires next Monday
                int daysUntilMonday = ((int)DayOfWeek.Monday - (int)now.DayOfWeek + 7) % 7;
                if (daysUntilMonday == 0) daysUntilMonday = 7;
                offer.expiresAt = now.Date.AddDays(daysUntilMonday);

                activeOffers.Add(offer);
                OnNewOfferAvailable?.Invoke(offer);
            }

            PlayerPrefs.SetString("LastWeeklyOfferRefresh", now.ToString());
            PlayerPrefs.Save();

            Debug.Log($"[OfferScheduler] Generated {weeklyOfferCount} weekly offers");
        }

        private void RotateFeaturedOffer()
        {
            DateTime now = CurrentTime;

            featuredOffer = CreateRandomOffer(TimedOffer.OfferType.Featured);
            featuredOffer.expiresAt = now.AddHours(featuredRotationHours);

            OnFeaturedOfferChanged?.Invoke(featuredOffer);

            Debug.Log($"[OfferScheduler] New featured offer: {featuredOffer.displayName} (expires in {featuredRotationHours}h)");
        }

        private TimedOffer CreateRandomOffer(TimedOffer.OfferType type)
        {
            // Generate offers with attractive discounts
            var offerTemplates = new[]
            {
                new { name = "Diamond Rush", productId = "diamonds_1000", discount = 0.30f },
                new { name = "Starter Pack", productId = "capsule_starter", discount = 0.50f },
                new { name = "Premium Capsule Deal", productId = "capsule_premium", discount = 0.25f },
                new { name = "Diamond Mega Pack", productId = "diamonds_2500", discount = 0.40f },
                new { name = "Season Pass Special", productId = "seasonpass_s1", discount = 0.20f }
            };

            var template = offerTemplates[UnityEngine.Random.Range(0, offerTemplates.Length)];

            return new TimedOffer
            {
                offerId = Guid.NewGuid().ToString(),
                displayName = template.name,
                offerType = type,
                productId = template.productId,
                discountPercent = template.discount,
                originalPrice = GetProductPrice(template.productId),
                expiresAt = DateTime.UtcNow, // Will be set by caller
                isPurchased = false
            };
        }

        private float GetProductPrice(string productId)
        {
            // TODO: Get from IAP catalog
            // Placeholder prices
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

            return prices.ContainsKey(productId) ? prices[productId] : 0.99f;
        }

        private void CheckOfferExpirations()
        {
            DateTime now = CurrentTime;

            // Check active offers
            var expired = activeOffers.Where(o => now >= o.expiresAt && !o.isPurchased).ToList();

            foreach (var offer in expired)
            {
                activeOffers.Remove(offer);
                OnOfferExpired?.Invoke(offer);
                Debug.Log($"[OfferScheduler] Offer expired: {offer.displayName}");
            }

            // Refresh if needed
            RefreshOffers();
        }

        /// <summary>
        /// Purchases an offer
        /// </summary>
        public void PurchaseOffer(string offerId)
        {
            TimedOffer offer = activeOffers.FirstOrDefault(o => o.offerId == offerId);

            if (offer == null)
            {
                offer = featuredOffer?.offerId == offerId ? featuredOffer : null;
            }

            if (offer == null)
            {
                Debug.LogWarning($"[OfferScheduler] Offer not found: {offerId}");
                return;
            }

            if (offer.isPurchased)
            {
                Debug.LogWarning($"[OfferScheduler] Offer already purchased: {offerId}");
                return;
            }

            offer.isPurchased = true;
            OnOfferPurchased?.Invoke(offer);

            Debug.Log($"[OfferScheduler] Offer purchased: {offer.displayName}");
        }

        #endregion

        #region Query Methods

        /// <summary>
        /// Gets all active offers
        /// </summary>
        public List<TimedOffer> GetActiveOffers()
        {
            return activeOffers.Where(o => !o.isPurchased && CurrentTime < o.expiresAt).ToList();
        }

        /// <summary>
        /// Gets daily offers
        /// </summary>
        public List<TimedOffer> GetDailyOffers()
        {
            return activeOffers.Where(o => o.offerType == TimedOffer.OfferType.Daily && !o.isPurchased).ToList();
        }

        /// <summary>
        /// Gets weekly offers
        /// </summary>
        public List<TimedOffer> GetWeeklyOffers()
        {
            return activeOffers.Where(o => o.offerType == TimedOffer.OfferType.Weekly && !o.isPurchased).ToList();
        }

        /// <summary>
        /// Gets current featured offer
        /// </summary>
        public TimedOffer GetFeaturedOffer()
        {
            return featuredOffer;
        }

        /// <summary>
        /// Gets time remaining for an offer
        /// </summary>
        public TimeSpan GetTimeRemaining(TimedOffer offer)
        {
            TimeSpan remaining = offer.expiresAt - CurrentTime;
            return remaining.TotalSeconds > 0 ? remaining : TimeSpan.Zero;
        }

        #endregion

        #region Data Structures

        [Serializable]
        public class TimedOffer
        {
            public string offerId;
            public string displayName;
            public OfferType offerType;
            public string productId;
            public float discountPercent;
            public float originalPrice;
            public DateTime expiresAt;
            public bool isPurchased;

            public float DiscountedPrice => originalPrice * (1f - discountPercent);
            public string DiscountLabel => $"{discountPercent * 100:F0}% OFF";

            public enum OfferType
            {
                Daily,
                Weekly,
                Featured,
                Flash
            }
        }

        #endregion
    }
}
