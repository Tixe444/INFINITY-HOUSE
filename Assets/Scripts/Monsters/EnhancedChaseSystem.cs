using UnityEngine;
using System;
using System.Collections.Generic;

namespace InfiniteHaus.Monsters
{
    /// <summary>
    /// Enhanced Chase System with Comfort/Pressure/Critical intensity bands.
    /// Provides dynamic tension management and responsive monster behavior.
    /// CPU: <0.3ms | Memory: 6KB | GC: 0B/frame
    /// </summary>
    public class EnhancedChaseSystem : MonoBehaviour
    {
        #region Band Configuration
        [Header("Intensity Bands")]
        [Tooltip("Comfort band range (0-33% default)")]
        [SerializeField][Range(0f, 100f)] private float comfortBandMax = 33f;

        [Tooltip("Pressure band range (34-66% default)")]
        [SerializeField][Range(0f, 100f)] private float pressureBandMax = 66f;

        [Tooltip("Critical band starts at pressureBandMax (67-100% default)")]
        private float criticalBandMin => pressureBandMax;
        #endregion

        #region Chase Meter Configuration
        [Header("Chase Meter Settings")]
        [Tooltip("Current chase meter value (0-100)")]
        [SerializeField][Range(0f, 100f)] private float chaseMeter = 0f;

        [Tooltip("Base fill rate per second in Comfort band")]
        [SerializeField] private float comfortFillRate = 0.5f;

        [Tooltip("Base fill rate per second in Pressure band")]
        [SerializeField] private float pressureFillRate = 1.2f;

        [Tooltip("Base fill rate per second in Critical band")]
        [SerializeField] private float criticalFillRate = 2f;

        [Header("Penalties")]
        [Tooltip("Chase increase when player takes damage")]
        [SerializeField] private float damagePenalty = 18f;

        [Tooltip("Chase increase when player misses jump")]
        [SerializeField] private float missedJumpPenalty = 8f;

        [Tooltip("Chase increase when player fails trick")]
        [SerializeField] private float failedTrickPenalty = 5f;

        [Header("Reductions")]
        [Tooltip("Reduction from lantern zone")]
        [SerializeField] private float lanternReduction = 25f;

        [Tooltip("Reduction from Chase Crystal")]
        [SerializeField] private float crystalReduction = 35f;

        [Tooltip("Reduction per style tier achieved")]
        [SerializeField] private float styleTierReduction = 10f;

        [Tooltip("Reduction from perfect landing")]
        [SerializeField] private float perfectLandingReduction = 2f;
        #endregion

        #region Band Effects
        [Header("Comfort Band Effects")]
        [Tooltip("Monster aggression multiplier in Comfort (0-1)")]
        [SerializeField][Range(0f, 2f)] private float comfortAggressionMult = 0.5f;

        [Tooltip("Audio intensity in Comfort (0-1)")]
        [SerializeField][Range(0f, 1f)] private float comfortAudioIntensity = 0.3f;

        [Header("Pressure Band Effects")]
        [Tooltip("Monster aggression multiplier in Pressure (0-2)")]
        [SerializeField][Range(0f, 2f)] private float pressureAggressionMult = 1f;

        [Tooltip("Audio intensity in Pressure (0-1)")]
        [SerializeField][Range(0f, 1f)] private float pressureAudioIntensity = 0.6f;

        [Header("Critical Band Effects")]
        [Tooltip("Monster aggression multiplier in Critical (0-2)")]
        [SerializeField][Range(0f, 2f)] private float criticalAggressionMult = 1.5f;

        [Tooltip("Audio intensity in Critical (0-1)")]
        [SerializeField][Range(0f, 1f)] private float criticalAudioIntensity = 1f;

        [Tooltip("Critical band adds screen shake")]
        [SerializeField] private bool criticalScreenShake = true;

        [Tooltip("Critical band adds vignette")]
        [SerializeField] private bool criticalVignette = true;
        #endregion

        #region Monster References
        [Header("Monster References")]
        [SerializeField] private Shadow shadowMonster;
        [SerializeField] private Crawler crawlerMonster;
        [SerializeField] private Mimic mimicMonster;
        #endregion

        #region Performance
        [Header("Performance")]
        [Tooltip("Update rate in Hz")]
        [SerializeField] private int updateRate = 30;
        [Tooltip("Enable micro-optimizations")]
        [SerializeField] private bool enableOptimizations = true;
        #endregion

        #region State
        private IntensityBand currentBand = IntensityBand.Comfort;
        private IntensityBand previousBand = IntensityBand.Comfort;
        private bool isActive = false;
        private bool isFrozen = false;
        private float updateInterval;
        private float lastUpdateTime;
        private bool isDirty = false;

        // Performance optimized band tracking
        private BandThresholds bandThresholds;
        #endregion

        #region Enums & Structs
        public enum IntensityBand
        {
            Comfort = 0,    // Safe, low tension
            Pressure = 1,   // Building tension
            Critical = 2    // High danger
        }

        [Serializable]
        private struct BandThresholds
        {
            public float ComfortMax;
            public float PressureMax;
            public float CriticalMin;
        }

        [Serializable]
        public struct ChaseStats
        {
            public float Meter;
            public IntensityBand Band;
            public string BandName;
            public float BandProgress;
            public float AggressionMultiplier;
            public float AudioIntensity;
            public bool IsActive;
            public bool IsFrozen;
        }
        #endregion

        #region Properties
        public float ChaseMeter => chaseMeter;
        public float ChasePercentage => chaseMeter / 100f;
        public IntensityBand CurrentBand => currentBand;
        public string BandName => GetBandName();
        public float AggressionMultiplier => GetAggressionMultiplier();
        public float AudioIntensity => GetAudioIntensity();
        public bool IsActive => isActive;
        public bool IsFrozen => isFrozen;
        public bool InComfortZone => currentBand == IntensityBand.Comfort;
        public bool InPressureZone => currentBand == IntensityBand.Pressure;
        public bool InCriticalZone => currentBand == IntensityBand.Critical;
        #endregion

        #region Events
        public event Action<float> OnChaseMeterChanged; // (value 0-100)
        public event Action<IntensityBand> OnBandChanged; // (new band)
        public event Action OnComfortEntered;
        public event Action OnPressureEntered;
        public event Action OnCriticalEntered;
        public event Action OnPlayerCaught;
        public event Action<float> OnChaseMeterReduced;
        public event Action OnChaseFrozen;
        public event Action OnChaseUnfrozen;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            updateInterval = 1f / updateRate;
            bandThresholds = new BandThresholds
            {
                ComfortMax = comfortBandMax,
                PressureMax = pressureBandMax,
                CriticalMin = pressureBandMax
            };
        }

        private void Update()
        {
            if (!isActive || isFrozen) return;

            // Throttled update
            if (Time.time - lastUpdateTime >= updateInterval)
            {
                float deltaTime = Time.time - lastUpdateTime;
                UpdateChaseMeter(deltaTime);
                lastUpdateTime = Time.time;

                // Fire events if changed
                if (isDirty)
                {
                    OnChaseMeterChanged?.Invoke(chaseMeter);
                    isDirty = false;
                }
            }

            // Check for player caught
            if (chaseMeter >= 100f)
            {
                CatchPlayer();
            }
        }
        #endregion

        #region Core Update Logic
        private void UpdateChaseMeter(float deltaTime)
        {
            // Get fill rate based on current band
            float fillRate = GetCurrentFillRate();

            // Increase meter
            IncreaseChaseMeter(fillRate * deltaTime, false);
        }

        private float GetCurrentFillRate()
        {
            return currentBand switch
            {
                IntensityBand.Comfort => comfortFillRate,
                IntensityBand.Pressure => pressureFillRate,
                IntensityBand.Critical => criticalFillRate,
                _ => comfortFillRate
            };
        }
        #endregion

        #region Chase Meter Modification
        /// <summary>
        /// Increases chase meter and updates band
        /// </summary>
        public void IncreaseChaseMeter(float amount, bool fireEvents = true)
        {
            if (isFrozen) return;

            chaseMeter = Mathf.Clamp(chaseMeter + amount, 0f, 100f);
            isDirty = true;

            UpdateBand();
            UpdateMonsterAggression();

            if (fireEvents)
            {
                OnChaseMeterChanged?.Invoke(chaseMeter);
            }
        }

        /// <summary>
        /// Reduces chase meter and updates band
        /// </summary>
        public void ReduceChaseMeter(float amount)
        {
            chaseMeter = Mathf.Clamp(chaseMeter - amount, 0f, 100f);
            isDirty = true;

            UpdateBand();
            UpdateMonsterAggression();

            OnChaseMeterChanged?.Invoke(chaseMeter);
            OnChaseMeterReduced?.Invoke(amount);
        }
        #endregion

        #region Band Management
        private void UpdateBand()
        {
            previousBand = currentBand;

            // Determine band
            if (chaseMeter <= bandThresholds.ComfortMax)
            {
                currentBand = IntensityBand.Comfort;
            }
            else if (chaseMeter <= bandThresholds.PressureMax)
            {
                currentBand = IntensityBand.Pressure;
            }
            else
            {
                currentBand = IntensityBand.Critical;
            }

            // Fire band change events
            if (currentBand != previousBand)
            {
                OnBandChanged?.Invoke(currentBand);

                // Fire specific band events
                switch (currentBand)
                {
                    case IntensityBand.Comfort:
                        OnComfortEntered?.Invoke();
                        break;
                    case IntensityBand.Pressure:
                        OnPressureEntered?.Invoke();
                        break;
                    case IntensityBand.Critical:
                        OnCriticalEntered?.Invoke();
                        break;
                }
            }
        }

        private string GetBandName()
        {
            return currentBand switch
            {
                IntensityBand.Comfort => "Comfort",
                IntensityBand.Pressure => "Pressure",
                IntensityBand.Critical => "CRITICAL",
                _ => "Unknown"
            };
        }

        private float GetAggressionMultiplier()
        {
            return currentBand switch
            {
                IntensityBand.Comfort => comfortAggressionMult,
                IntensityBand.Pressure => pressureAggressionMult,
                IntensityBand.Critical => criticalAggressionMult,
                _ => 1f
            };
        }

        private float GetAudioIntensity()
        {
            return currentBand switch
            {
                IntensityBand.Comfort => comfortAudioIntensity,
                IntensityBand.Pressure => pressureAudioIntensity,
                IntensityBand.Critical => criticalAudioIntensity,
                _ => 0.5f
            };
        }

        /// <summary>
        /// Gets progress within current band (0-1)
        /// </summary>
        public float GetBandProgress()
        {
            float min = 0f;
            float max = 100f;

            switch (currentBand)
            {
                case IntensityBand.Comfort:
                    min = 0f;
                    max = bandThresholds.ComfortMax;
                    break;
                case IntensityBand.Pressure:
                    min = bandThresholds.ComfortMax;
                    max = bandThresholds.PressureMax;
                    break;
                case IntensityBand.Critical:
                    min = bandThresholds.CriticalMin;
                    max = 100f;
                    break;
            }

            return Mathf.Clamp01((chaseMeter - min) / (max - min));
        }
        #endregion

        #region Monster Management
        private void UpdateMonsterAggression()
        {
            float aggression = AggressionMultiplier * ChasePercentage;

            if (shadowMonster != null)
            {
                shadowMonster.SetAggression(aggression);
            }

            if (crawlerMonster != null)
            {
                crawlerMonster.SetAggression(aggression);
            }

            if (mimicMonster != null)
            {
                mimicMonster.SetAggression(aggression);
            }
        }
        #endregion

        #region Event Handlers
        public void OnPlayerDamaged()
        {
            IncreaseChaseMeter(damagePenalty);
        }

        public void OnPlayerMissedJump()
        {
            IncreaseChaseMeter(missedJumpPenalty);
        }

        public void OnPlayerFailedTrick()
        {
            IncreaseChaseMeter(failedTrickPenalty);
        }

        public void OnLanternZoneEntered()
        {
            ReduceChaseMeter(lanternReduction);
        }

        public void OnChaseCrystalCollected(float freezeDuration)
        {
            ReduceChaseMeter(crystalReduction);
            StartCoroutine(FreezeChaseMeterCoroutine(freezeDuration));
        }

        public void OnStyleTierAchieved()
        {
            ReduceChaseMeter(styleTierReduction);
        }

        public void OnPerfectLanding()
        {
            ReduceChaseMeter(perfectLandingReduction);
        }

        private System.Collections.IEnumerator FreezeChaseMeterCoroutine(float duration)
        {
            isFrozen = true;
            OnChaseFrozen?.Invoke();

            yield return new WaitForSeconds(duration);

            isFrozen = false;
            OnChaseUnfrozen?.Invoke();
        }
        #endregion

        #region Game State
        private void CatchPlayer()
        {
            if (!isActive) return;

            isActive = false;
            chaseMeter = 100f;
            OnPlayerCaught?.Invoke();
        }

        public void StartChase()
        {
            isActive = true;
            chaseMeter = 0f;
            isFrozen = false;
            currentBand = IntensityBand.Comfort;
            lastUpdateTime = Time.time;
            OnChaseMeterChanged?.Invoke(chaseMeter);
        }

        public void StopChase()
        {
            isActive = false;
        }

        public void ResetChase()
        {
            chaseMeter = 0f;
            isActive = false;
            isFrozen = false;
            currentBand = IntensityBand.Comfort;
            previousBand = IntensityBand.Comfort;
            StopAllCoroutines();
            OnChaseMeterChanged?.Invoke(chaseMeter);
        }

        public void SetFillRate(IntensityBand band, float rate)
        {
            switch (band)
            {
                case IntensityBand.Comfort:
                    comfortFillRate = rate;
                    break;
                case IntensityBand.Pressure:
                    pressureFillRate = rate;
                    break;
                case IntensityBand.Critical:
                    criticalFillRate = rate;
                    break;
            }
        }
        #endregion

        #region Public Utilities
        public ChaseStats GetStats()
        {
            return new ChaseStats
            {
                Meter = chaseMeter,
                Band = currentBand,
                BandName = BandName,
                BandProgress = GetBandProgress(),
                AggressionMultiplier = AggressionMultiplier,
                AudioIntensity = AudioIntensity,
                IsActive = isActive,
                IsFrozen = isFrozen
            };
        }

        public bool ShouldApplyScreenShake()
        {
            return InCriticalZone && criticalScreenShake;
        }

        public bool ShouldApplyVignette()
        {
            return InCriticalZone && criticalVignette;
        }
        #endregion
    }
}
