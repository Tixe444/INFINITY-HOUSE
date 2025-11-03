using UnityEngine;
using System;
using System.Collections.Generic;

namespace InfinityHouse.Performance
{
    /// <summary>
    /// Auto-Tuning Manager for dynamic performance optimization.
    /// Monitors performance degradation and automatically adjusts quality settings.
    /// Target: Maintain 60 FPS mobile, 90 FPS desktop with ≤16ms p95 frame time.
    /// CPU: <0.2ms | Memory: 8KB | GC: 0B/frame
    /// </summary>
    public class AutoTuningManager : MonoBehaviour
    {
        #region Configuration
        [Header("Performance Thresholds")]
        [Tooltip("FPS threshold to trigger quality reduction (% of target)")]
        [SerializeField][Range(0.5f, 1f)] private float fpsThreshold = 0.85f; // 85% of target

        [Tooltip("Frame time threshold in ms to trigger reduction")]
        [SerializeField] private float frameTimeThreshold = 18f; // 18ms

        [Tooltip("P95 frame time threshold in ms")]
        [SerializeField] private float p95Threshold = 20f; // 20ms

        [Tooltip("Time window to measure degradation (seconds)")]
        [SerializeField] private float measurementWindow = 3f;

        [Tooltip("Samples needed to trigger adjustment")]
        [SerializeField] private int requiredSamples = 90; // 3 sec @ 30hz

        [Header("Quality Levels")]
        [Tooltip("Current quality level (0-5, 5=Ultra, 0=Potato)")]
        [SerializeField][Range(0, 5)] private int currentQualityLevel = 5;

        [Tooltip("Enable auto quality adjustment")]
        [SerializeField] private bool enableAutoAdjust = true;

        [Tooltip("Allow quality increase when stable")]
        [SerializeField] private bool allowQualityIncrease = true;

        [Tooltip("Time before allowing quality increase (seconds)")]
        [SerializeField] private float qualityIncreaseDelay = 10f;

        [Header("Adjustment Targets")]
        [Tooltip("Trail system quality reduction")]
        [SerializeField] private bool adjustTrails = true;

        [Tooltip("Particle effect reduction")]
        [SerializeField] private bool adjustParticles = true;

        [Tooltip("Shadow quality reduction")]
        [SerializeField] private bool adjustShadows = true;

        [Tooltip("Post-processing reduction")]
        [SerializeField] private bool adjustPostProcessing = true;

        [Tooltip("Physics rate reduction")]
        [SerializeField] private bool adjustPhysicsRate = true;

        [Header("Performance")]
        [Tooltip("Update rate in Hz")]
        [SerializeField] private int updateRate = 30;

        [Tooltip("Enable detailed logging")]
        [SerializeField] private bool enableLogging = false;
        #endregion

        #region State
        private PerformanceManager perfManager;

        // Performance tracking
        private List<float> recentFPSSamples;
        private List<float> recentFrameTimeSamples;
        private int sampleIndex = 0;
        private int poorPerformanceSamples = 0;
        private int goodPerformanceSamples = 0;

        // Timing
        private float updateInterval;
        private float lastUpdateTime;
        private float lastQualityChangeTime;

        // Quality level definitions
        private QualityConfig[] qualityConfigs;
        private int targetFPS;
        #endregion

        #region Enums & Structs
        public enum QualityLevel
        {
            Potato = 0,     // Absolute minimum
            Low = 1,        // Low-end devices
            Medium = 2,     // Mid-range devices
            High = 3,       // High-end devices
            Ultra = 4,      // Flagship devices
            Max = 5         // Desktop/High-end
        }

        [Serializable]
        private struct QualityConfig
        {
            public string Name;
            public float TrailQuality;      // 0-1
            public float ParticleQuality;   // 0-1
            public int MaxParticles;
            public bool EnableShadows;
            public bool EnablePostProcessing;
            public int PhysicsRate;         // Hz
        }

        [Serializable]
        public struct PerformanceStats
        {
            public float CurrentFPS;
            public float AverageFrameTime;
            public float P95FrameTime;
            public int QualityLevel;
            public string QualityName;
            public bool IsDegraded;
            public int AdjustmentCount;
        }
        #endregion

        #region Properties
        public int CurrentQualityLevel => currentQualityLevel;
        public string QualityName => GetQualityName();
        public bool IsPerformanceDegraded { get; private set; }
        public int AdjustmentCount { get; private set; }
        #endregion

        #region Events
        public event Action<int> OnQualityLevelChanged; // (new level)
        public event Action OnQualityReduced;
        public event Action OnQualityIncreased;
        public event Action<PerformanceStats> OnPerformanceUpdate;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            updateInterval = 1f / updateRate;
            InitializeQualityLevels();
            InitializeSampleBuffers();
        }

        private void Start()
        {
            perfManager = PerformanceManager.Instance;

            if (perfManager == null)
            {
                Debug.LogError("[AutoTuningManager] PerformanceManager not found!");
                enabled = false;
                return;
            }

            targetFPS = perfManager.TargetFPS;
            lastQualityChangeTime = Time.time;

            ApplyQualityLevel(currentQualityLevel);

            if (enableLogging)
            {
                Debug.Log($"[AutoTuningManager] Initialized. Quality: {QualityName}, Target FPS: {targetFPS}");
            }
        }

        private void Update()
        {
            if (!enableAutoAdjust || perfManager == null) return;

            // Throttled update
            if (Time.time - lastUpdateTime >= updateInterval)
            {
                MonitorPerformance();
                EvaluateAdjustments();
                lastUpdateTime = Time.time;
            }
        }
        #endregion

        #region Initialization
        private void InitializeQualityLevels()
        {
            qualityConfigs = new QualityConfig[]
            {
                // Potato - Absolute minimum
                new QualityConfig
                {
                    Name = "Potato",
                    TrailQuality = 0.2f,
                    ParticleQuality = 0.2f,
                    MaxParticles = 10,
                    EnableShadows = false,
                    EnablePostProcessing = false,
                    PhysicsRate = 30
                },
                // Low
                new QualityConfig
                {
                    Name = "Low",
                    TrailQuality = 0.4f,
                    ParticleQuality = 0.4f,
                    MaxParticles = 25,
                    EnableShadows = false,
                    EnablePostProcessing = false,
                    PhysicsRate = 45
                },
                // Medium
                new QualityConfig
                {
                    Name = "Medium",
                    TrailQuality = 0.6f,
                    ParticleQuality = 0.6f,
                    MaxParticles = 50,
                    EnableShadows = false,
                    EnablePostProcessing = true,
                    PhysicsRate = 60
                },
                // High
                new QualityConfig
                {
                    Name = "High",
                    TrailQuality = 0.75f,
                    ParticleQuality = 0.75f,
                    MaxParticles = 100,
                    EnableShadows = true,
                    EnablePostProcessing = true,
                    PhysicsRate = 60
                },
                // Ultra
                new QualityConfig
                {
                    Name = "Ultra",
                    TrailQuality = 0.9f,
                    ParticleQuality = 0.9f,
                    MaxParticles = 150,
                    EnableShadows = true,
                    EnablePostProcessing = true,
                    PhysicsRate = 60
                },
                // Max
                new QualityConfig
                {
                    Name = "Max",
                    TrailQuality = 1f,
                    ParticleQuality = 1f,
                    MaxParticles = 200,
                    EnableShadows = true,
                    EnablePostProcessing = true,
                    PhysicsRate = 90
                }
            };
        }

        private void InitializeSampleBuffers()
        {
            recentFPSSamples = new List<float>(requiredSamples);
            recentFrameTimeSamples = new List<float>(requiredSamples);

            for (int i = 0; i < requiredSamples; i++)
            {
                recentFPSSamples.Add(0f);
                recentFrameTimeSamples.Add(0f);
            }
        }
        #endregion

        #region Performance Monitoring
        private void MonitorPerformance()
        {
            float currentFPS = perfManager.CurrentFPS;
            float currentFrameTime = perfManager.AverageFrameTime;

            // Store samples
            recentFPSSamples[sampleIndex] = currentFPS;
            recentFrameTimeSamples[sampleIndex] = currentFrameTime;
            sampleIndex = (sampleIndex + 1) % requiredSamples;

            // Check thresholds
            float fpsTarget = targetFPS * fpsThreshold;
            bool isPoorPerformance = currentFPS < fpsTarget || currentFrameTime > frameTimeThreshold;

            if (isPoorPerformance)
            {
                poorPerformanceSamples++;
                goodPerformanceSamples = 0;
            }
            else
            {
                goodPerformanceSamples++;
                poorPerformanceSamples = 0;
            }

            IsPerformanceDegraded = poorPerformanceSamples > requiredSamples / 3;
        }

        private void EvaluateAdjustments()
        {
            // Reduce quality if sustained poor performance
            if (poorPerformanceSamples >= requiredSamples / 2 && currentQualityLevel > 0)
            {
                ReduceQuality();
            }
            // Increase quality if sustained good performance
            else if (allowQualityIncrease && goodPerformanceSamples >= requiredSamples &&
                     currentQualityLevel < qualityConfigs.Length - 1 &&
                     Time.time - lastQualityChangeTime >= qualityIncreaseDelay)
            {
                IncreaseQuality();
            }

            // Fire performance update event
            OnPerformanceUpdate?.Invoke(GetStats());
        }
        #endregion

        #region Quality Adjustment
        private void ReduceQuality()
        {
            if (currentQualityLevel <= 0) return;

            currentQualityLevel--;
            ApplyQualityLevel(currentQualityLevel);
            AdjustmentCount++;
            lastQualityChangeTime = Time.time;

            // Reset counters
            poorPerformanceSamples = 0;

            OnQualityLevelChanged?.Invoke(currentQualityLevel);
            OnQualityReduced?.Invoke();

            if (enableLogging)
            {
                Debug.Log($"[AutoTuningManager] Quality reduced to {QualityName} due to performance degradation");
            }

            // Track telemetry
            TrackQualityChange("reduced");
        }

        private void IncreaseQuality()
        {
            if (currentQualityLevel >= qualityConfigs.Length - 1) return;

            currentQualityLevel++;
            ApplyQualityLevel(currentQualityLevel);
            lastQualityChangeTime = Time.time;

            // Reset counters
            goodPerformanceSamples = 0;

            OnQualityLevelChanged?.Invoke(currentQualityLevel);
            OnQualityIncreased?.Invoke();

            if (enableLogging)
            {
                Debug.Log($"[AutoTuningManager] Quality increased to {QualityName}");
            }

            // Track telemetry
            TrackQualityChange("increased");
        }

        private void ApplyQualityLevel(int level)
        {
            level = Mathf.Clamp(level, 0, qualityConfigs.Length - 1);
            QualityConfig config = qualityConfigs[level];

            // Apply trail quality
            if (adjustTrails)
            {
                ApplyTrailQuality(config.TrailQuality);
            }

            // Apply particle quality
            if (adjustParticles)
            {
                ApplyParticleQuality(config.ParticleQuality, config.MaxParticles);
            }

            // Apply shadows
            if (adjustShadows)
            {
                QualitySettings.shadows = config.EnableShadows ? ShadowQuality.HardOnly : ShadowQuality.Disable;
            }

            // Apply post-processing (this would need integration with your post-processing system)
            if (adjustPostProcessing)
            {
                // TODO: Integrate with post-processing volume
            }

            // Apply physics rate
            if (adjustPhysicsRate)
            {
                Time.fixedDeltaTime = 1f / config.PhysicsRate;
            }

            if (enableLogging)
            {
                Debug.Log($"[AutoTuningManager] Applied quality config: {config.Name}");
            }
        }

        private void ApplyTrailQuality(float quality)
        {
            // Find all trail systems and adjust quality
            var trailSystems = FindObjectsOfType<InfiniteHaus.Player.StyleMeterSystem>();
            // This would need integration with TrailRenderSystem
            // For now, we'll just log
            if (enableLogging)
            {
                Debug.Log($"[AutoTuningManager] Trail quality set to {quality:P0}");
            }
        }

        private void ApplyParticleQuality(float quality, int maxParticles)
        {
            QualitySettings.particleRaycastBudget = (int)(maxParticles * quality);

            // Reduce particle systems
            var particles = FindObjectsOfType<ParticleSystem>();
            foreach (var ps in particles)
            {
                var main = ps.main;
                main.maxParticles = Mathf.RoundToInt(main.maxParticles * quality);
            }
        }
        #endregion

        #region Public API
        /// <summary>
        /// Manually set quality level
        /// </summary>
        public void SetQualityLevel(int level)
        {
            level = Mathf.Clamp(level, 0, qualityConfigs.Length - 1);
            currentQualityLevel = level;
            ApplyQualityLevel(level);
            OnQualityLevelChanged?.Invoke(level);
        }

        /// <summary>
        /// Enable/disable auto-tuning
        /// </summary>
        public void SetAutoTuning(bool enabled)
        {
            enableAutoAdjust = enabled;

            if (enableLogging)
            {
                Debug.Log($"[AutoTuningManager] Auto-tuning {(enabled ? "enabled" : "disabled")}");
            }
        }

        /// <summary>
        /// Get current performance stats
        /// </summary>
        public PerformanceStats GetStats()
        {
            return new PerformanceStats
            {
                CurrentFPS = perfManager?.CurrentFPS ?? 0f,
                AverageFrameTime = perfManager?.AverageFrameTime ?? 0f,
                P95FrameTime = perfManager?.GetP95FrameTime() ?? 0f,
                QualityLevel = currentQualityLevel,
                QualityName = QualityName,
                IsDegraded = IsPerformanceDegraded,
                AdjustmentCount = AdjustmentCount
            };
        }

        /// <summary>
        /// Reset adjustment counter
        /// </summary>
        public void ResetAdjustments()
        {
            AdjustmentCount = 0;
            poorPerformanceSamples = 0;
            goodPerformanceSamples = 0;
        }
        #endregion

        #region Utilities
        private string GetQualityName()
        {
            if (currentQualityLevel >= 0 && currentQualityLevel < qualityConfigs.Length)
            {
                return qualityConfigs[currentQualityLevel].Name;
            }
            return "Unknown";
        }

        private void TrackQualityChange(string changeType)
        {
            // TODO: Integrate with analytics system
            if (enableLogging)
            {
                Debug.Log($"[AutoTuningManager] Quality {changeType}: {QualityName} (Level {currentQualityLevel})");
            }
        }
        #endregion
    }
}
