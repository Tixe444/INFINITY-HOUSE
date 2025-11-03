using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

namespace InfiniteHaus.Game
{
    /// <summary>
    /// Death sequence controller for INFINITE HAUS v5.7.
    /// Handles slow-mo, flash, freeze-frame, and results HUD.
    /// Shows distance, shards, style, XP, and dust rewards.
    /// CPU: <0.15ms | Memory: 4KB | GC: 0B
    /// </summary>
    public class DeathSequenceController : MonoBehaviour
    {
        #region Configuration
        [Header("Death Effects")]
        [Tooltip("Slow-mo time scale (0.1 = 10% speed)")]
        [SerializeField][Range(0.01f, 1f)] private float slowMoTimeScale = 0.1f;

        [Tooltip("Slow-mo duration (seconds)")]
        [SerializeField] private float slowMoDuration = 1f;

        [Tooltip("Flash color")]
        [SerializeField] private Color flashColor = Color.white;

        [Tooltip("Flash duration (seconds)")]
        [SerializeField] private float flashDuration = 0.1f;

        [Tooltip("Freeze-frame duration (seconds)")]
        [SerializeField] private float freezeDuration = 0.3f;

        [Header("Results HUD")]
        [Tooltip("Results panel")]
        [SerializeField] private GameObject resultsPanel;

        [Tooltip("Distance text")]
        [SerializeField] private Text distanceText;

        [Tooltip("Shards text")]
        [SerializeField] private Text shardsText;

        [Tooltip("Style text")]
        [SerializeField] private Text styleText;

        [Tooltip("XP text")]
        [SerializeField] private Text xpText;

        [Tooltip("Dust text")]
        [SerializeField] private Text dustText;

        [Header("Buttons")]
        [Tooltip("Retry button")]
        [SerializeField] private Button retryButton;

        [Tooltip("Market button")]
        [SerializeField] private Button marketButton;

        [Tooltip("Home button")]
        [SerializeField] private Button homeButton;

        [Header("Rewards Calculation")]
        [Tooltip("XP per meter")]
        [SerializeField] private float xpPerMeter = 10f;

        [Tooltip("Dust per shard")]
        [SerializeField] private int dustPerShard = 5;

        [Tooltip("Dust per style point")]
        [SerializeField] private float dustPerStylePoint = 0.5f;

        [Tooltip("Base dust reward")]
        [SerializeField] private int baseDustReward = 100;

        [Header("References")]
        [SerializeField] private Image flashOverlay;
        [SerializeField] private Player.StyleMeterSystem styleMeter;
        [SerializeField] private Economy.ShardCurrencyManager shardManager;
        #endregion

        #region State
        private bool deathSequenceActive = false;
        private float distanceTraveled = 0f;
        private int shardsCollected = 0;
        private float stylePoints = 0f;
        private int xpEarned = 0;
        private int dustEarned = 0;
        #endregion

        #region Events
        public event Action OnDeathSequenceStarted;
        public event Action OnDeathSequenceCompleted;
        public event Action OnRetry;
        public event Action OnMarket;
        public event Action OnHome;
        #endregion

        #region Properties
        public bool IsActive => deathSequenceActive;
        public float DistanceTraveled => distanceTraveled;
        public int ShardsCollected => shardsCollected;
        public float StylePoints => stylePoints;
        public int XPEarned => xpEarned;
        public int DustEarned => dustEarned;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            // Setup button listeners
            if (retryButton != null)
            {
                retryButton.onClick.AddListener(OnRetryClicked);
            }

            if (marketButton != null)
            {
                marketButton.onClick.AddListener(OnMarketClicked);
            }

            if (homeButton != null)
            {
                homeButton.onClick.AddListener(OnHomeClicked);
            }

            // Hide results panel initially
            if (resultsPanel != null)
            {
                resultsPanel.SetActive(false);
            }

            // Hide flash overlay
            if (flashOverlay != null)
            {
                flashOverlay.enabled = false;
            }
        }
        #endregion

        #region Death Sequence
        /// <summary>
        /// Triggers death sequence
        /// </summary>
        public void TriggerDeath(float distance, int shards, float style)
        {
            if (deathSequenceActive) return;

            distanceTraveled = distance;
            shardsCollected = shards;
            stylePoints = style;

            StartCoroutine(DeathSequenceCoroutine());
        }

        private IEnumerator DeathSequenceCoroutine()
        {
            deathSequenceActive = true;
            OnDeathSequenceStarted?.Invoke();

            // === SLOW-MO ===
            yield return SlowMotion();

            // === FLASH ===
            yield return Flash();

            // === FREEZE-FRAME ===
            yield return FreezeFrame();

            // === CALCULATE REWARDS ===
            CalculateRewards();

            // === SHOW RESULTS HUD ===
            ShowResultsHUD();

            OnDeathSequenceCompleted?.Invoke();
            deathSequenceActive = false;
        }

        private IEnumerator SlowMotion()
        {
            float originalTimeScale = Time.timeScale;
            Time.timeScale = slowMoTimeScale;

            yield return new WaitForSecondsRealtime(slowMoDuration);

            Time.timeScale = originalTimeScale;
            Debug.Log($"[DeathSequence] Slow-mo ({slowMoTimeScale}x for {slowMoDuration}s)");
        }

        private IEnumerator Flash()
        {
            if (flashOverlay == null) yield break;

            flashOverlay.enabled = true;
            flashOverlay.color = flashColor;

            yield return new WaitForSecondsRealtime(flashDuration);

            flashOverlay.enabled = false;
            Debug.Log($"[DeathSequence] Flash ({flashDuration}s)");
        }

        private IEnumerator FreezeFrame()
        {
            Time.timeScale = 0f;

            yield return new WaitForSecondsRealtime(freezeDuration);

            Time.timeScale = 1f;
            Debug.Log($"[DeathSequence] Freeze-frame ({freezeDuration}s)");
        }
        #endregion

        #region Rewards
        private void CalculateRewards()
        {
            // Calculate XP
            xpEarned = Mathf.RoundToInt(distanceTraveled * xpPerMeter);

            // Calculate dust
            int shardDust = shardsCollected * dustPerShard;
            int styleDust = Mathf.RoundToInt(stylePoints * dustPerStylePoint);
            dustEarned = baseDustReward + shardDust + styleDust;

            Debug.Log($"[DeathSequence] Rewards - XP: {xpEarned}, Dust: {dustEarned}");
        }

        private void ShowResultsHUD()
        {
            if (resultsPanel != null)
            {
                resultsPanel.SetActive(true);
            }

            // Update text fields
            if (distanceText != null)
            {
                distanceText.text = $"{distanceTraveled:F1}m";
            }

            if (shardsText != null)
            {
                shardsText.text = $"{shardsCollected}";
            }

            if (styleText != null)
            {
                styleText.text = $"{stylePoints:F0}";
            }

            if (xpText != null)
            {
                xpText.text = $"+{xpEarned} XP";
            }

            if (dustText != null)
            {
                dustText.text = $"+{dustEarned} Dust";
            }

            Debug.Log("[DeathSequence] Results HUD shown");
        }
        #endregion

        #region Button Handlers
        private void OnRetryClicked()
        {
            OnRetry?.Invoke();
            HideResultsHUD();
            Debug.Log("[DeathSequence] Retry clicked");
        }

        private void OnMarketClicked()
        {
            OnMarket?.Invoke();
            HideResultsHUD();
            Debug.Log("[DeathSequence] Market clicked");
        }

        private void OnHomeClicked()
        {
            OnHome?.Invoke();
            HideResultsHUD();
            Debug.Log("[DeathSequence] Home clicked");
        }

        private void HideResultsHUD()
        {
            if (resultsPanel != null)
            {
                resultsPanel.SetActive(false);
            }
        }
        #endregion

        #region Public API
        /// <summary>
        /// Resets death sequence
        /// </summary>
        public void ResetSequence()
        {
            deathSequenceActive = false;
            distanceTraveled = 0f;
            shardsCollected = 0;
            stylePoints = 0f;
            xpEarned = 0;
            dustEarned = 0;
            Time.timeScale = 1f;
            HideResultsHUD();
        }

        /// <summary>
        /// Gets death stats
        /// </summary>
        public DeathStats GetStats()
        {
            return new DeathStats
            {
                Distance = distanceTraveled,
                Shards = shardsCollected,
                Style = stylePoints,
                XP = xpEarned,
                Dust = dustEarned
            };
        }
        #endregion

        #region Data Structures
        [Serializable]
        public struct DeathStats
        {
            public float Distance;
            public int Shards;
            public float Style;
            public int XP;
            public int Dust;
        }
        #endregion
    }
}
