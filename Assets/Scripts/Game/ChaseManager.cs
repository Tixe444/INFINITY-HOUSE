using UnityEngine;

namespace InfiniteHaus.Game
{
    /// <summary>
    /// Unified Chase Manager for INFINITE HAUS v5.8.
    /// Integrates chase meter (EnhancedChaseSystem) with physical chaser distance (ChaserSystem).
    /// Maps Comfort/Pressure/Critical bands to chaser behavior.
    /// CPU: <0.2ms | Memory: 4KB | GC: 0B
    /// </summary>
    public class ChaseManager : MonoBehaviour
    {
        #region Configuration
        [Header("Band Configuration")]
        [Tooltip("Comfort band: 0-33%")]
        [SerializeField] private float comfortBandMax = 33f;

        [Tooltip("Pressure band: 34-66%")]
        [SerializeField] private float pressureBandMax = 66f;

        [Header("Distance Mapping")]
        [Tooltip("Base distance in Comfort band (6m)")]
        [SerializeField] private float comfortBaseDistance = 6f;

        [Tooltip("Base distance in Pressure band (4.5m)")]
        [SerializeField] private float pressureBaseDistance = 4.5f;

        [Tooltip("Base distance in Critical band (3m)")]
        [SerializeField] private float criticalBaseDistance = 3f;

        [Header("Penalties & Bonuses")]
        [Tooltip("Meter increase on damage")]
        [SerializeField] private float damagePenalty = 18f;

        [Tooltip("Meter increase on missed jump")]
        [SerializeField] private float missedJumpPenalty = 8f;

        [Tooltip("Meter reduction on perfect landing")]
        [SerializeField] private float perfectLandingBonus = 2f;

        [Tooltip("Distance bonus on 6 perfect actions")]
        [SerializeField] private float perfectStreakBonus = 0.5f;

        [Tooltip("Perfect actions needed for bonus")]
        [SerializeField] private int perfectStreakRequired = 6;

        [Header("Fill Rates")]
        [Tooltip("Meter fill rate in Comfort")]
        [SerializeField] private float comfortFillRate = 0.5f;

        [Tooltip("Meter fill rate in Pressure")]
        [SerializeField] private float pressureFillRate = 1.2f;

        [Tooltip("Meter fill rate in Critical")]
        [SerializeField] private float criticalFillRate = 2f;

        [Header("References")]
        [SerializeField] private GameSessionManager sessionManager;
        [SerializeField] private ChaserSystem chaserSystem;
        #endregion

        #region State
        private int perfectStreak = 0;
        private int currentBand = 0; // 0=Comfort, 1=Pressure, 2=Critical
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            if (sessionManager == null)
                sessionManager = GameSessionManager.Instance;

            if (sessionManager != null)
            {
                sessionManager.OnChaseMeterChanged += OnMeterChanged;
            }
        }

        private void OnDestroy()
        {
            if (sessionManager != null)
            {
                sessionManager.OnChaseMeterChanged -= OnMeterChanged;
            }
        }

        private void Update()
        {
            if (sessionManager == null || !sessionManager.SessionActive) return;

            // Passive meter increase
            float fillRate = GetCurrentFillRate();
            float newMeter = sessionManager.ChaseMeter + fillRate * Time.deltaTime;
            sessionManager.SetChaseMeter(newMeter);
        }
        #endregion

        #region Band Logic
        private void OnMeterChanged(float meter)
        {
            // Update band
            int newBand = CalculateBand(meter);
            if (newBand != currentBand)
            {
                currentBand = newBand;
                sessionManager.SetChaserBand(newBand);
                UpdateChaserDistance();
            }
        }

        private int CalculateBand(float meter)
        {
            if (meter <= comfortBandMax) return 0; // Comfort
            if (meter <= pressureBandMax) return 1; // Pressure
            return 2; // Critical
        }

        private float GetCurrentFillRate()
        {
            return currentBand switch
            {
                0 => comfortFillRate,
                1 => pressureFillRate,
                2 => criticalFillRate,
                _ => comfortFillRate
            };
        }

        private void UpdateChaserDistance()
        {
            float baseDistance = currentBand switch
            {
                0 => comfortBaseDistance,
                1 => pressureBaseDistance,
                2 => criticalBaseDistance,
                _ => comfortBaseDistance
            };

            if (sessionManager != null)
            {
                sessionManager.SetChaserDistance(baseDistance);
            }

            if (chaserSystem != null)
            {
                chaserSystem.SetDistance(baseDistance);
            }
        }
        #endregion

        #region Public API (Game Events)
        public void OnPlayerDamaged()
        {
            if (sessionManager != null)
            {
                float newMeter = sessionManager.ChaseMeter + damagePenalty;
                sessionManager.SetChaseMeter(newMeter);
            }
            ResetPerfectStreak();
        }

        public void OnPlayerMissedJump()
        {
            if (sessionManager != null)
            {
                float newMeter = sessionManager.ChaseMeter + missedJumpPenalty;
                sessionManager.SetChaseMeter(newMeter);
            }
            ResetPerfectStreak();
        }

        public void OnPlayerPerfectLanding()
        {
            if (sessionManager != null)
            {
                float newMeter = sessionManager.ChaseMeter - perfectLandingBonus;
                sessionManager.SetChaseMeter(newMeter);
            }

            perfectStreak++;
            if (perfectStreak >= perfectStreakRequired)
            {
                ApplyPerfectStreakBonus();
                perfectStreak = 0;
            }
        }

        public void OnStyleTierAchieved(int tier)
        {
            // Reduce chase meter on tier up
            float reduction = tier * 10f;
            if (sessionManager != null)
            {
                float newMeter = sessionManager.ChaseMeter - reduction;
                sessionManager.SetChaseMeter(newMeter);
            }
        }

        private void ApplyPerfectStreakBonus()
        {
            // Give distance bonus
            if (chaserSystem != null)
            {
                chaserSystem.IncreaseDistance(perfectStreakBonus);
            }
        }

        private void ResetPerfectStreak()
        {
            perfectStreak = 0;
        }
        #endregion

        #region Public Utilities
        public void ResetChase()
        {
            perfectStreak = 0;
            currentBand = 0;

            if (sessionManager != null)
            {
                sessionManager.SetChaseMeter(0f);
                sessionManager.SetChaserDistance(comfortBaseDistance);
                sessionManager.SetChaserBand(0);
            }

            if (chaserSystem != null)
            {
                chaserSystem.ResetSystem();
            }
        }
        #endregion
    }
}
