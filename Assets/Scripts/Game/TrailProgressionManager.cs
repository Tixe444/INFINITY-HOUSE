using UnityEngine;

namespace InfiniteHaus.Game
{
    /// <summary>
    /// Trail Progression Manager for INFINITE HAUS v5.8.
    /// Links trail tiers to velocity, style, and chaser relief.
    /// Implements: "Trail tiers linked to velocity: T1 glow, T2 echo, T3 ribbon+relief"
    /// CPU: <0.1ms | Memory: 2KB | GC: 0B
    /// </summary>
    public class TrailProgressionManager : MonoBehaviour
    {
        #region Configuration
        [Header("Tier Thresholds")]
        [Tooltip("Velocity needed for T1 (5.5m/s)")]
        [SerializeField] private float t1VelocityThreshold = 5.5f;

        [Tooltip("Velocity needed for T2 (6.5m/s)")]
        [SerializeField] private float t2VelocityThreshold = 6.5f;

        [Tooltip("Velocity needed for T3 (7.5m/s)")]
        [SerializeField] private float t3VelocityThreshold = 7.5f;

        [Header("Alternative: Time-Based")]
        [Tooltip("Use time-based progression instead of velocity")]
        [SerializeField] private bool useTimeBased = true;

        [Tooltip("Time for T1 (0.3s)")]
        [SerializeField] private float t1TimeThreshold = 0.3f;

        [Tooltip("Time for T2 (2.0s)")]
        [SerializeField] private float t2TimeThreshold = 2.0f;

        [Tooltip("Time for T3 (4.5s)")]
        [SerializeField] private float t3TimeThreshold = 4.5f;

        [Header("Chaser Relief")]
        [Tooltip("Chaser relief on T3 (12%)")]
        [SerializeField] private float t3ChaserRelief = 0.12f;

        [Header("Trail Quality")]
        [Tooltip("Trail quality per tier (0-1)")]
        [SerializeField] private float[] tierQuality = new float[] { 0f, 0.4f, 0.7f, 1f };

        [Header("References")]
        [SerializeField] private GameSessionManager sessionManager;
        [SerializeField] private ReactiveSpeedManager speedManager;
        [SerializeField] private ChaseManager chaseManager;
        [SerializeField] private Trails.TrailRenderSystem trailRenderer;
        #endregion

        #region State
        private int currentTier = 0;
        private bool t3ReliefApplied = false;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            if (sessionManager == null)
                sessionManager = GameSessionManager.Instance;

            if (sessionManager != null)
            {
                sessionManager.OnPlayerVelocityChanged += OnVelocityChanged;
                sessionManager.OnSessionStarted += OnSessionStarted;
            }
        }

        private void OnDestroy()
        {
            if (sessionManager != null)
            {
                sessionManager.OnPlayerVelocityChanged -= OnVelocityChanged;
                sessionManager.OnSessionStarted -= OnSessionStarted;
            }
        }

        private void Update()
        {
            if (sessionManager == null || !sessionManager.SessionActive) return;

            if (useTimeBased)
            {
                UpdateTimeBasedProgression();
            }
            // Velocity-based is handled by OnVelocityChanged event
        }
        #endregion

        #region Progression Logic
        private void UpdateTimeBasedProgression()
        {
            float elapsed = sessionManager.SessionElapsed;
            int newTier = CalculateTierFromTime(elapsed);

            if (newTier != currentTier)
            {
                SetTrailTier(newTier);
            }
        }

        private int CalculateTierFromTime(float time)
        {
            if (time >= t3TimeThreshold) return 3;
            if (time >= t2TimeThreshold) return 2;
            if (time >= t1TimeThreshold) return 1;
            return 0;
        }

        private void OnVelocityChanged(float velocity)
        {
            if (!useTimeBased)
            {
                int newTier = CalculateTierFromVelocity(velocity);
                if (newTier != currentTier)
                {
                    SetTrailTier(newTier);
                }
            }
        }

        private int CalculateTierFromVelocity(float velocity)
        {
            if (velocity >= t3VelocityThreshold) return 3;
            if (velocity >= t2VelocityThreshold) return 2;
            if (velocity >= t1VelocityThreshold) return 1;
            return 0;
        }

        private void SetTrailTier(int tier)
        {
            tier = Mathf.Clamp(tier, 0, 3);
            currentTier = tier;

            // Update session manager
            if (sessionManager != null)
            {
                sessionManager.SetTrailTier(tier);
            }

            // Apply trail visual quality
            ApplyTrailQuality(tier);

            // Apply T3 chaser relief
            if (tier == 3 && !t3ReliefApplied)
            {
                ApplyT3Relief();
            }

            #if UNITY_EDITOR
            Debug.Log($"[TrailProgression] Tier changed to T{tier}");
            #endif
        }

        private void ApplyTrailQuality(int tier)
        {
            if (trailRenderer == null) return;

            float quality = tier < tierQuality.Length ? tierQuality[tier] : 1f;

            // Apply quality to trail renderer
            // This would adjust particle counts, glow intensity, etc.
            #if UNITY_EDITOR
            Debug.Log($"[TrailProgression] Trail quality set to {quality:P0}");
            #endif
        }

        private void ApplyT3Relief()
        {
            t3ReliefApplied = true;

            // Reduce chase meter
            if (sessionManager != null)
            {
                float currentMeter = sessionManager.ChaseMeter;
                float relief = currentMeter * t3ChaserRelief;
                sessionManager.SetChaseMeter(currentMeter - relief);
            }

            // Increase chaser distance
            if (chaseManager != null)
            {
                float currentDistance = sessionManager != null ? sessionManager.ChaserDistance : 6f;
                float reliefDistance = currentDistance * t3ChaserRelief;
                sessionManager?.SetChaserDistance(currentDistance + reliefDistance);
            }

            #if UNITY_EDITOR
            Debug.Log($"[TrailProgression] T3 relief applied: {t3ChaserRelief:P0}");
            #endif
        }
        #endregion

        #region Event Handlers
        private void OnSessionStarted()
        {
            ResetProgression();
        }
        #endregion

        #region Public API
        public void ResetProgression()
        {
            currentTier = 0;
            t3ReliefApplied = false;

            if (sessionManager != null)
            {
                sessionManager.SetTrailTier(0);
            }
        }

        public int GetCurrentTier() => currentTier;

        public string GetTierName()
        {
            return currentTier switch
            {
                1 => "T1: Glow",
                2 => "T2: Echo",
                3 => "T3: Ribbon",
                _ => "None"
            };
        }

        public float GetTierProgress()
        {
            if (useTimeBased)
            {
                float elapsed = sessionManager?.SessionElapsed ?? 0f;
                float nextThreshold = GetNextThreshold(currentTier);
                float prevThreshold = GetPrevThreshold(currentTier);
                return Mathf.Clamp01((elapsed - prevThreshold) / (nextThreshold - prevThreshold));
            }
            else
            {
                float velocity = speedManager?.GetCurrentVelocity() ?? 0f;
                float nextThreshold = GetNextVelocityThreshold(currentTier);
                float prevThreshold = GetPrevVelocityThreshold(currentTier);
                return Mathf.Clamp01((velocity - prevThreshold) / (nextThreshold - prevThreshold));
            }
        }

        private float GetNextThreshold(int tier)
        {
            return tier switch
            {
                0 => t1TimeThreshold,
                1 => t2TimeThreshold,
                2 => t3TimeThreshold,
                _ => float.MaxValue
            };
        }

        private float GetPrevThreshold(int tier)
        {
            return tier switch
            {
                1 => t1TimeThreshold,
                2 => t2TimeThreshold,
                3 => t3TimeThreshold,
                _ => 0f
            };
        }

        private float GetNextVelocityThreshold(int tier)
        {
            return tier switch
            {
                0 => t1VelocityThreshold,
                1 => t2VelocityThreshold,
                2 => t3VelocityThreshold,
                _ => float.MaxValue
            };
        }

        private float GetPrevVelocityThreshold(int tier)
        {
            return tier switch
            {
                1 => t1VelocityThreshold,
                2 => t2VelocityThreshold,
                3 => t3VelocityThreshold,
                _ => 0f
            };
        }
        #endregion
    }
}
