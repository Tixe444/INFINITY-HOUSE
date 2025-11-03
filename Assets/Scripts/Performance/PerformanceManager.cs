// ═══════════════════════════════════════════════════════════════════════════════
// INFINITY HOUSE v5.5 - Made by Mate Makovics
// Performance Manager - Unified Tick Loop & FPS Optimization
// ═══════════════════════════════════════════════════════════════════════════════

using UnityEngine;
using System;
using System.Collections.Generic;

namespace InfinityHouse.Performance
{
    /// <summary>
    /// Unified Tick Loop for ultra-fluid performance.
    /// Manages frame timing, FPS limiting, and execution order.
    /// Target: 60 FPS mobile, 90 FPS desktop, <16ms p95 frame time.
    /// Zero GC allocations during gameplay.
    /// </summary>
    public class PerformanceManager : MonoBehaviour
    {
        // Singleton
        public static PerformanceManager Instance { get; private set; }

        [Header("FPS Targeting")]
        [SerializeField] private int targetFPSMobile = 60;
        [SerializeField] private int targetFPSDesktop = 90;
        [SerializeField] private bool dynamicFPSLimiter = true;

        [Header("Frame Timing")]
        [SerializeField] private float maxDeltaTime = 0.033f; // 33ms cap
        [SerializeField] private bool clampDeltaTime = true;

        [Header("Performance Monitoring")]
        [SerializeField] private bool enableProfiling = true;
        [SerializeField] private int profilingSampleSize = 300; // 5 seconds at 60fps

        // Tick delegates for ordered execution
        public event Action OnPreTick;      // Input pre-polling
        public event Action OnPhysicsTick;  // Physics updates
        public event Action OnLogicTick;    // Game logic
        public event Action OnRenderTick;   // Visual updates
        public event Action OnPostTick;     // Cleanup, telemetry

        // Performance metrics
        private float currentFPS;
        private float averageFrameTime;
        private float peakFrameTime;
        private List<float> frameTimeSamples;
        private int currentSampleIndex;

        // Memory tracking
        private long lastHeapSize;
        private int gcCollectionCount;

        // Frame timing
        private float lastFrameTime;
        private float deltaTime;
        private float unscaledDeltaTime;

        // Properties
        public float CurrentFPS => currentFPS;
        public float AverageFrameTime => averageFrameTime;
        public float DeltaTime => deltaTime;
        public float UnscaledDeltaTime => unscaledDeltaTime;
        public int TargetFPS => Application.isMobilePlatform ? targetFPSMobile : targetFPSDesktop;

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

            Initialize();
        }

        private void Initialize()
        {
            // Set target framerate
            Application.targetFrameRate = TargetFPS;

            // Initialize profiling
            frameTimeSamples = new List<float>(profilingSampleSize);
            for (int i = 0; i < profilingSampleSize; i++)
            {
                frameTimeSamples.Add(0f);
            }
            currentSampleIndex = 0;

            // QualitySettings optimizations
            QualitySettings.vSyncCount = 0; // Disable VSync for manual control

            #if UNITY_IOS || UNITY_ANDROID
            // Mobile optimizations
            QualitySettings.pixelLightCount = 1;
            QualitySettings.shadowDistance = 0;
            QualitySettings.shadows = ShadowQuality.Disable;
            #endif

            lastHeapSize = GC.GetTotalMemory(false);
            gcCollectionCount = GC.CollectionCount(0);

            Debug.Log($"[PerformanceManager] Initialized. Target FPS: {TargetFPS}");
        }

        private void Update()
        {
            // Start frame profiling
            float frameStartTime = Time.realtimeSinceStartup;

            // Calculate delta time
            unscaledDeltaTime = Time.unscaledDeltaTime;
            deltaTime = clampDeltaTime ? Mathf.Min(unscaledDeltaTime, maxDeltaTime) : unscaledDeltaTime;

            // === UNIFIED TICK LOOP ===
            // Ordered execution for predictable performance

            // 1. PRE-TICK: Input polling (highest priority)
            OnPreTick?.Invoke();

            // 2. PHYSICS TICK: Physics calculations
            OnPhysicsTick?.Invoke();

            // 3. LOGIC TICK: Game logic, AI, spawning
            OnLogicTick?.Invoke();

            // 4. RENDER TICK: Visual updates, animations, FX
            OnRenderTick?.Invoke();

            // 5. POST-TICK: Cleanup, telemetry, analytics
            OnPostTick?.Invoke();

            // End frame profiling
            float frameEndTime = Time.realtimeSinceStartup;
            float frameTime = (frameEndTime - frameStartTime) * 1000f; // Convert to ms

            UpdatePerformanceMetrics(frameTime);
        }

        private void UpdatePerformanceMetrics(float frameTime)
        {
            // Update FPS
            currentFPS = 1f / Time.unscaledDeltaTime;

            // Track frame time
            frameTimeSamples[currentSampleIndex] = frameTime;
            currentSampleIndex = (currentSampleIndex + 1) % profilingSampleSize;

            // Calculate average
            float sum = 0f;
            float peak = 0f;
            foreach (float sample in frameTimeSamples)
            {
                sum += sample;
                if (sample > peak) peak = sample;
            }
            averageFrameTime = sum / profilingSampleSize;
            peakFrameTime = peak;

            // Check memory
            if (enableProfiling && Time.frameCount % 300 == 0) // Every 5 seconds
            {
                CheckMemory();
            }
        }

        private void CheckMemory()
        {
            long currentHeap = GC.GetTotalMemory(false);
            long heapDelta = currentHeap - lastHeapSize;
            int currentGCCount = GC.CollectionCount(0);
            int gcDelta = currentGCCount - gcCollectionCount;

            if (gcDelta > 0)
            {
                Debug.LogWarning($"[PerformanceManager] GC Collection detected! Count: {gcDelta}, Heap: {currentHeap / 1024 / 1024}MB");

                // Track telemetry
                Analytics.TelemetryEvents.Instance?.TrackEvent("performance_gc_spike", new Dictionary<string, object>
                {
                    { "gc_count", gcDelta },
                    { "heap_mb", currentHeap / 1024 / 1024 },
                    { "heap_delta_mb", heapDelta / 1024 / 1024 }
                });
            }

            lastHeapSize = currentHeap;
            gcCollectionCount = currentGCCount;
        }

        #region Public API

        /// <summary>
        /// Gets p95 frame time (95th percentile)
        /// </summary>
        public float GetP95FrameTime()
        {
            List<float> sortedSamples = new List<float>(frameTimeSamples);
            sortedSamples.Sort();
            int p95Index = (int)(sortedSamples.Count * 0.95f);
            return sortedSamples[p95Index];
        }

        /// <summary>
        /// Gets performance report
        /// </summary>
        public PerformanceReport GetPerformanceReport()
        {
            return new PerformanceReport
            {
                currentFPS = currentFPS,
                averageFrameTime = averageFrameTime,
                p95FrameTime = GetP95FrameTime(),
                peakFrameTime = peakFrameTime,
                heapSizeMB = GC.GetTotalMemory(false) / 1024f / 1024f,
                gcCollectionCount = GC.CollectionCount(0)
            };
        }

        /// <summary>
        /// Sets target FPS dynamically
        /// </summary>
        public void SetTargetFPS(int fps)
        {
            Application.targetFrameRate = fps;
            Debug.Log($"[PerformanceManager] Target FPS set to {fps}");
        }

        /// <summary>
        /// Forces garbage collection (use sparingly!)
        /// </summary>
        public void ForceGC()
        {
            Debug.LogWarning("[PerformanceManager] Forcing GC...");
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        #endregion

        #region Debug

        private void OnGUI()
        {
            if (!enableProfiling) return;

            // Performance overlay
            GUIStyle style = new GUIStyle();
            style.normal.textColor = currentFPS >= TargetFPS * 0.9f ? Color.green : Color.red;
            style.fontSize = 20;

            GUI.Label(new Rect(10, 10, 300, 30), $"FPS: {currentFPS:F1} / {TargetFPS}", style);
            GUI.Label(new Rect(10, 40, 300, 30), $"Frame: {averageFrameTime:F2}ms (P95: {GetP95FrameTime():F2}ms)", style);
            GUI.Label(new Rect(10, 70, 300, 30), $"Heap: {GC.GetTotalMemory(false) / 1024 / 1024}MB", style);
        }

        #endregion

        #region Data Structures

        [Serializable]
        public struct PerformanceReport
        {
            public float currentFPS;
            public float averageFrameTime;
            public float p95FrameTime;
            public float peakFrameTime;
            public float heapSizeMB;
            public int gcCollectionCount;
        }

        #endregion
    }
}
