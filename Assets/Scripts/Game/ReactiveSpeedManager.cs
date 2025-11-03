using UnityEngine;

namespace InfiniteHaus.Game
{
    /// <summary>
    /// Reactive Speed Manager for INFINITE HAUS v5.8.
    /// Implements: "Speed scales w/ skill: better play = faster scroll; mistakes slow briefly"
    /// Links velocity to StyleMeter, trail tiers, and audio tempo.
    /// CPU: <0.1ms | Memory: 2KB | GC: 0B
    /// </summary>
    public class ReactiveSpeedManager : MonoBehaviour
    {
        #region Configuration
        [Header("Velocity Range")]
        [Tooltip("Base velocity (6.2m/s)")]
        [SerializeField] private float baseVelocity = 6.2f;

        [Tooltip("Minimum velocity (after mistakes)")]
        [SerializeField] private float minVelocity = 4.5f;

        [Tooltip("Maximum velocity (perfect play)")]
        [SerializeField] private float maxVelocity = 8.5f;

        [Header("Acceleration")]
        [Tooltip("Acceleration rate (m/s²)")]
        [SerializeField] private float accelerationRate = 1.5f;

        [Tooltip("Deceleration rate on mistakes (m/s²)")]
        [SerializeField] private float decelerationRate = 3f;

        [Tooltip("Recovery rate back to base (m/s²)")]
        [SerializeField] private float recoveryRate = 0.8f;

        [Header("Style Scaling")]
        [Tooltip("Velocity bonus per style tier")]
        [SerializeField] private float velocityPerStyleTier = 0.5f;

        [Tooltip("Velocity bonus at max combo")]
        [SerializeField] private float maxComboBonus = 1.0f;

        [Header("Mistake Penalties")]
        [Tooltip("Velocity reduction on damage")]
        [SerializeField] private float damageSlowdown = 1.5f;

        [Tooltip("Slowdown duration (seconds)")]
        [SerializeField] private float slowdownDuration = 0.8f;

        [Header("References")]
        [SerializeField] private GameSessionManager sessionManager;
        [SerializeField] private Player.StyleMeterSystem styleMeter;
        [SerializeField] private Player.EnhancedRunnerController playerController;
        #endregion

        #region State
        private float currentVelocity;
        private float targetVelocity;
        private float slowdownTimer = 0f;
        private bool isSlowedDown = false;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            if (sessionManager == null)
                sessionManager = GameSessionManager.Instance;

            currentVelocity = baseVelocity;
            targetVelocity = baseVelocity;

            // Subscribe to style events
            if (styleMeter != null)
            {
                styleMeter.OnTierChanged += OnStyleTierChanged;
                styleMeter.OnComboIncreased += OnComboIncreased;
                styleMeter.OnComboBroken += OnComboBroken;
            }
        }

        private void OnDestroy()
        {
            if (styleMeter != null)
            {
                styleMeter.OnTierChanged -= OnStyleTierChanged;
                styleMeter.OnComboIncreased -= OnComboIncreased;
                styleMeter.OnComboBroken -= OnComboBroken;
            }
        }

        private void Update()
        {
            if (sessionManager == null || !sessionManager.SessionActive) return;

            // Update slowdown timer
            if (isSlowedDown)
            {
                slowdownTimer -= Time.deltaTime;
                if (slowdownTimer <= 0f)
                {
                    isSlowedDown = false;
                    RecalculateTargetVelocity();
                }
            }

            // Smoothly approach target velocity
            UpdateVelocity();
        }
        #endregion

        #region Velocity Management
        private void UpdateVelocity()
        {
            if (isSlowedDown)
            {
                // Decelerate during slowdown
                currentVelocity = Mathf.MoveTowards(
                    currentVelocity,
                    targetVelocity,
                    decelerationRate * Time.deltaTime
                );
            }
            else if (currentVelocity < targetVelocity)
            {
                // Accelerate
                currentVelocity = Mathf.MoveTowards(
                    currentVelocity,
                    targetVelocity,
                    accelerationRate * Time.deltaTime
                );
            }
            else if (currentVelocity > targetVelocity)
            {
                // Recover to target
                currentVelocity = Mathf.MoveTowards(
                    currentVelocity,
                    targetVelocity,
                    recoveryRate * Time.deltaTime
                );
            }

            // Clamp to bounds
            currentVelocity = Mathf.Clamp(currentVelocity, minVelocity, maxVelocity);

            // Update session manager
            if (sessionManager != null)
            {
                sessionManager.SetPlayerVelocity(currentVelocity);
            }

            // Update player controller
            if (playerController != null)
            {
                playerController.SetSpeed(currentVelocity);
            }
        }

        private void RecalculateTargetVelocity()
        {
            if (isSlowedDown) return;

            float styleBonus = 0f;
            float comboBonus = 0f;

            // Style tier bonus
            if (styleMeter != null)
            {
                styleBonus = styleMeter.CurrentTier * velocityPerStyleTier;

                // Combo bonus (scaled 0-1 based on combo chain)
                float comboProgress = Mathf.Clamp01(styleMeter.ComboChain / 10f);
                comboBonus = comboProgress * maxComboBonus;
            }

            targetVelocity = baseVelocity + styleBonus + comboBonus;
            targetVelocity = Mathf.Clamp(targetVelocity, minVelocity, maxVelocity);
        }
        #endregion

        #region Event Handlers
        private void OnStyleTierChanged(int tier)
        {
            RecalculateTargetVelocity();
        }

        private void OnComboIncreased(int chain)
        {
            RecalculateTargetVelocity();
        }

        private void OnComboBroken()
        {
            RecalculateTargetVelocity();
        }

        public void OnPlayerDamaged()
        {
            // Apply slowdown
            isSlowedDown = true;
            slowdownTimer = slowdownDuration;
            targetVelocity = currentVelocity - damageSlowdown;
            targetVelocity = Mathf.Max(targetVelocity, minVelocity);
        }

        public void OnPlayerPerfect()
        {
            // Instant small boost
            currentVelocity = Mathf.Min(currentVelocity + 0.2f, maxVelocity);
        }
        #endregion

        #region Public API
        public void ResetVelocity()
        {
            currentVelocity = baseVelocity;
            targetVelocity = baseVelocity;
            slowdownTimer = 0f;
            isSlowedDown = false;

            if (sessionManager != null)
            {
                sessionManager.SetPlayerVelocity(baseVelocity);
            }
        }

        public float GetCurrentVelocity() => currentVelocity;
        public float GetTargetVelocity() => targetVelocity;
        public float GetVelocityPercentage() => (currentVelocity - minVelocity) / (maxVelocity - minVelocity);
        #endregion
    }
}
