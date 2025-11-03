// ═══════════════════════════════════════════════════════════════════════════════
// INFINITY HOUSE - Made by Mate Makovics
// Telemetry Events - Purchase Funnel & Player Behavior Tracking
// ═══════════════════════════════════════════════════════════════════════════════

using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace InfinityHouse.Analytics
{
    /// <summary>
    /// Telemetry event tracking system for analytics and optimization.
    /// Tracks purchase funnel, player behavior, and monetization metrics.
    /// Privacy-compliant with opt-in/opt-out support.
    /// </summary>
    public class TelemetryEvents : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private string analyticsEndpoint = "https://api.yourgame.com/analytics";
        [SerializeField] private bool enableTelemetry = true;
        [SerializeField] private bool logEventsLocally = true;

        [Header("Batching")]
        [SerializeField] private int batchSize = 10;
        [SerializeField] private float batchInterval = 30f; // seconds

        // Singleton
        public static TelemetryEvents Instance { get; private set; }

        // Event queue
        private List<AnalyticsEvent> eventQueue;
        private string sessionId;
        private DateTime sessionStart;

        // Privacy
        private bool userConsentGiven = true; // Default true, should prompt user

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

            eventQueue = new List<AnalyticsEvent>();
            sessionId = Guid.NewGuid().ToString();
            sessionStart = DateTime.UtcNow;

            // Load user consent
            userConsentGiven = PlayerPrefs.GetInt("AnalyticsConsent", 1) == 1;
        }

        private void Start()
        {
            // Track session start
            TrackSessionStart();

            // Start batch sender
            InvokeRepeating(nameof(FlushEventQueue), batchInterval, batchInterval);
        }

        private void OnApplicationQuit()
        {
            // Track session end
            TrackSessionEnd();

            // Flush remaining events
            FlushEventQueue();
        }

        #region Privacy Controls

        /// <summary>
        /// Sets user consent for analytics tracking
        /// </summary>
        public void SetUserConsent(bool consent)
        {
            userConsentGiven = consent;
            PlayerPrefs.SetInt("AnalyticsConsent", consent ? 1 : 0);
            PlayerPrefs.Save();

            Debug.Log($"[TelemetryEvents] Analytics consent: {consent}");

            if (consent)
            {
                TrackEvent("analytics_consent_granted", new Dictionary<string, object>());
            }
        }

        /// <summary>
        /// Checks if user has given consent
        /// </summary>
        public bool HasUserConsent()
        {
            return userConsentGiven;
        }

        #endregion

        #region Core Tracking

        /// <summary>
        /// Tracks a generic event
        /// </summary>
        public void TrackEvent(string eventName, Dictionary<string, object> parameters = null)
        {
            if (!enableTelemetry || !userConsentGiven)
            {
                return;
            }

            var analyticsEvent = new AnalyticsEvent
            {
                eventName = eventName,
                timestamp = DateTime.UtcNow,
                sessionId = sessionId,
                parameters = parameters ?? new Dictionary<string, object>()
            };

            eventQueue.Add(analyticsEvent);

            if (logEventsLocally)
            {
                Debug.Log($"[Analytics] {eventName}: {JsonUtility.ToJson(analyticsEvent)}");
            }

            // Flush if batch size reached
            if (eventQueue.Count >= batchSize)
            {
                FlushEventQueue();
            }
        }

        private async void FlushEventQueue()
        {
            if (eventQueue.Count == 0 || !enableTelemetry)
            {
                return;
            }

            // Prepare batch
            var batch = new AnalyticsBatch
            {
                sessionId = sessionId,
                events = new List<AnalyticsEvent>(eventQueue)
            };

            string json = JsonUtility.ToJson(batch);

            // Clear queue
            eventQueue.Clear();

            // Send to server
            try
            {
                using (UnityWebRequest request = new UnityWebRequest(analyticsEndpoint, "POST"))
                {
                    byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
                    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type", "application/json");

                    var operation = request.SendWebRequest();

                    while (!operation.isDone)
                    {
                        await System.Threading.Tasks.Task.Yield();
                    }

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        Debug.Log($"[TelemetryEvents] Flushed {batch.events.Count} events");
                    }
                    else
                    {
                        Debug.LogWarning($"[TelemetryEvents] Failed to send events: {request.error}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TelemetryEvents] Error sending events: {ex.Message}");
            }
        }

        #endregion

        #region Session Tracking

        private void TrackSessionStart()
        {
            TrackEvent("session_start", new Dictionary<string, object>
            {
                { "platform", Application.platform.ToString() },
                { "version", Application.version },
                { "device_model", SystemInfo.deviceModel },
                { "os", SystemInfo.operatingSystem }
            });
        }

        private void TrackSessionEnd()
        {
            TimeSpan sessionDuration = DateTime.UtcNow - sessionStart;

            TrackEvent("session_end", new Dictionary<string, object>
            {
                { "duration_seconds", (int)sessionDuration.TotalSeconds }
            });
        }

        #endregion

        #region Purchase Funnel Tracking

        /// <summary>
        /// Tracks when player views a shop item
        /// </summary>
        public void TrackShopItemViewed(string productId, string location)
        {
            TrackEvent("shop_item_viewed", new Dictionary<string, object>
            {
                { "product_id", productId },
                { "location", location }
            });
        }

        /// <summary>
        /// Tracks when player initiates a purchase
        /// </summary>
        public void TrackPurchaseInitiated(string productId, float price, string currency)
        {
            TrackEvent("purchase_initiated", new Dictionary<string, object>
            {
                { "product_id", productId },
                { "price", price },
                { "currency", currency }
            });
        }

        /// <summary>
        /// Tracks successful purchase
        /// </summary>
        public void TrackPurchaseCompleted(string productId, float price, string currency, string transactionId)
        {
            TrackEvent("purchase_completed", new Dictionary<string, object>
            {
                { "product_id", productId },
                { "price", price },
                { "currency", currency },
                { "transaction_id", transactionId },
                { "revenue", price } // For revenue tracking
            });
        }

        /// <summary>
        /// Tracks failed/cancelled purchase
        /// </summary>
        public void TrackPurchaseFailed(string productId, string reason)
        {
            TrackEvent("purchase_failed", new Dictionary<string, object>
            {
                { "product_id", productId },
                { "reason", reason }
            });
        }

        /// <summary>
        /// Tracks when player views purchase options
        /// </summary>
        public void TrackShopOpened(string source)
        {
            TrackEvent("shop_opened", new Dictionary<string, object>
            {
                { "source", source } // e.g., "main_menu", "hud_button", "level_end"
            });
        }

        /// <summary>
        /// Tracks shop exit
        /// </summary>
        public void TrackShopClosed(float timeSpent)
        {
            TrackEvent("shop_closed", new Dictionary<string, object>
            {
                { "time_spent_seconds", timeSpent }
            });
        }

        #endregion

        #region Monetization Tracking

        /// <summary>
        /// Tracks capsule opening
        /// </summary>
        public void TrackCapsuleOpened(string capsuleId, List<string> contents)
        {
            TrackEvent("capsule_opened", new Dictionary<string, object>
            {
                { "capsule_id", capsuleId },
                { "item_count", contents.Count }
            });
        }

        /// <summary>
        /// Tracks season pass purchase
        /// </summary>
        public void TrackSeasonPassPurchased(string seasonId, float price)
        {
            TrackEvent("season_pass_purchased", new Dictionary<string, object>
            {
                { "season_id", seasonId },
                { "price", price }
            });
        }

        /// <summary>
        /// Tracks offer interaction
        /// </summary>
        public void TrackOfferViewed(string offerId, string offerType)
        {
            TrackEvent("offer_viewed", new Dictionary<string, object>
            {
                { "offer_id", offerId },
                { "offer_type", offerType }
            });
        }

        /// <summary>
        /// Tracks offer purchase
        /// </summary>
        public void TrackOfferPurchased(string offerId, float discountPercent, float price)
        {
            TrackEvent("offer_purchased", new Dictionary<string, object>
            {
                { "offer_id", offerId },
                { "discount_percent", discountPercent },
                { "price", price }
            });
        }

        #endregion

        #region Gameplay Tracking

        /// <summary>
        /// Tracks level completion
        /// </summary>
        public void TrackLevelCompleted(int level, float duration, int score)
        {
            TrackEvent("level_completed", new Dictionary<string, object>
            {
                { "level", level },
                { "duration_seconds", duration },
                { "score", score }
            });
        }

        /// <summary>
        /// Tracks level failure
        /// </summary>
        public void TrackLevelFailed(int level, string reason)
        {
            TrackEvent("level_failed", new Dictionary<string, object>
            {
                { "level", level },
                { "reason", reason }
            });
        }

        /// <summary>
        /// Tracks currency earned
        /// </summary>
        public void TrackCurrencyEarned(string currencyType, int amount, string source)
        {
            TrackEvent("currency_earned", new Dictionary<string, object>
            {
                { "currency_type", currencyType },
                { "amount", amount },
                { "source", source }
            });
        }

        /// <summary>
        /// Tracks currency spent
        /// </summary>
        public void TrackCurrencySpent(string currencyType, int amount, string item)
        {
            TrackEvent("currency_spent", new Dictionary<string, object>
            {
                { "currency_type", currencyType },
                { "amount", amount },
                { "item", item }
            });
        }

        #endregion

        #region Retention Tracking

        /// <summary>
        /// Tracks daily login
        /// </summary>
        public void TrackDailyLogin(int dayStreak)
        {
            TrackEvent("daily_login", new Dictionary<string, object>
            {
                { "day_streak", dayStreak }
            });
        }

        /// <summary>
        /// Tracks feature usage
        /// </summary>
        public void TrackFeatureUsed(string featureName)
        {
            TrackEvent("feature_used", new Dictionary<string, object>
            {
                { "feature", featureName }
            });
        }

        #endregion

        #region Data Structures

        [Serializable]
        private class AnalyticsEvent
        {
            public string eventName;
            public DateTime timestamp;
            public string sessionId;
            public Dictionary<string, object> parameters;
        }

        [Serializable]
        private class AnalyticsBatch
        {
            public string sessionId;
            public List<AnalyticsEvent> events;
        }

        #endregion
    }
}
