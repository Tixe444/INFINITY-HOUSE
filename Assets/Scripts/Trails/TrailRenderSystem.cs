// ═══════════════════════════════════════════════════════════════════════════════
// INFINITY HOUSE v5.6 - Made by Mate Makovics
// Trail Render System - High-Performance Trail Rendering
// ═══════════════════════════════════════════════════════════════════════════════

using UnityEngine;
using System.Collections.Generic;

namespace InfinityHouse.Trails
{
    /// <summary>
    /// Ultra-optimized trail rendering: ≤0.3ms CPU, ≤0.5ms GPU, 1-2 drawcalls.
    /// Ribbon mesh with gradient atlas, auto-degradation on low FPS.
    /// </summary>
    public class TrailRenderSystem : MonoBehaviour
    {
        public static TrailRenderSystem Instance { get; private set; }

        [Header("Performance Budget")]
        [SerializeField] private float maxCPUMs = 0.3f;
        [SerializeField] private float maxGPUMs = 0.5f;
        [SerializeField] private int maxDrawcalls = 2;

        [Header("Trail Configuration")]
        [SerializeField] private Material trailMaterial;
        [SerializeField] private Texture2D gradientAtlas;
        [SerializeField] private int maxSegments = 24;
        [SerializeField] private float updateHz = 45f;
        [SerializeField] private float baseWidth = 0.03f; // 3px at 100 units
        [SerializeField] private float peakWidth = 0.05f; // 5px at T3

        [Header("Degradation")]
        [SerializeField] private float degradeFPS1 = 58f;
        [SerializeField] private float degradeFPS2 = 55f;
        [SerializeField] private float degradeFPS3 = 50f;

        // Active trail
        private TrailData currentTrail;
        private List<Vector3> positions;
        private Mesh trailMesh;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;

        // Performance tracking
        private float lastUpdateTime;
        private float cpuTime;
        private float gpuTime;
        private int drawcalls;
        private int currentSegments;

        // Degradation state
        private float segmentMultiplier = 1f;
        private bool glintEnabled = true;
        private float widthMultiplier = 1f;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            Initialize();
        }

        private void Initialize()
        {
            positions = new List<Vector3>(maxSegments);

            // Create mesh components
            GameObject trailObj = new GameObject("TrailRenderer");
            trailObj.transform.SetParent(transform);
            meshFilter = trailObj.AddComponent<MeshFilter>();
            meshRenderer = trailObj.AddComponent<MeshRenderer>();

            trailMesh = new Mesh();
            trailMesh.MarkDynamic();
            meshFilter.mesh = trailMesh;

            if (trailMaterial != null)
                meshRenderer.material = trailMaterial;

            // Subscribe to performance manager
            if (Performance.PerformanceManager.Instance != null)
                Performance.PerformanceManager.Instance.OnRenderTick += UpdateTrail;

            Debug.Log("[TrailRenderSystem] Initialized with performance budgets");
        }

        #region Trail Update

        private void UpdateTrail()
        {
            if (currentTrail == null) return;

            float currentTime = Time.time;
            if (currentTime - lastUpdateTime < 1f / updateHz) return;

            float startTime = Time.realtimeSinceStartup;

            // Add new position
            Vector3 playerPos = GetPlayerPosition();
            if (positions.Count == 0 || Vector3.Distance(playerPos, positions[positions.Count - 1]) > 0.1f)
            {
                positions.Add(playerPos);

                // Remove old segments
                currentSegments = Mathf.Min(positions.Count, Mathf.RoundToInt(maxSegments * segmentMultiplier));
                while (positions.Count > currentSegments)
                {
                    positions.RemoveAt(0);
                }
            }

            // Rebuild mesh
            if (positions.Count >= 2)
            {
                RebuildMesh();
            }

            lastUpdateTime = currentTime;
            cpuTime = (Time.realtimeSinceStartup - startTime) * 1000f;

            // Check budget
            if (cpuTime > maxCPUMs)
            {
                Debug.LogWarning($"[TrailRenderSystem] CPU budget exceeded: {cpuTime:F2}ms > {maxCPUMs}ms");
                Analytics.TelemetryEvents.Instance?.TrackEvent("trail_budget_exceeded", new Dictionary<string, object>
                {
                    { "cpu_ms", cpuTime },
                    { "segments", currentSegments }
                });
            }

            // Apply degradation based on FPS
            ApplyDegradation();
        }

        private void RebuildMesh()
        {
            int segmentCount = positions.Count;
            if (segmentCount < 2) return;

            Vector3[] vertices = new Vector3[segmentCount * 2];
            Vector2[] uvs = new Vector2[segmentCount * 2];
            int[] triangles = new int[(segmentCount - 1) * 6];

            float width = GetCurrentWidth();

            for (int i = 0; i < segmentCount; i++)
            {
                Vector3 pos = positions[i];
                Vector3 forward = i < segmentCount - 1 ? (positions[i + 1] - pos).normalized : (pos - positions[i - 1]).normalized;
                Vector3 right = Vector3.Cross(forward, Vector3.forward).normalized;

                float widthAtSegment = width * Mathf.Lerp(0.5f, 1f, (float)i / segmentCount);

                vertices[i * 2] = pos - right * widthAtSegment;
                vertices[i * 2 + 1] = pos + right * widthAtSegment;

                float t = (float)i / (segmentCount - 1);
                uvs[i * 2] = new Vector2(t, 0f);
                uvs[i * 2 + 1] = new Vector2(t, 1f);
            }

            for (int i = 0; i < segmentCount - 1; i++)
            {
                int triIndex = i * 6;
                int vertIndex = i * 2;

                triangles[triIndex] = vertIndex;
                triangles[triIndex + 1] = vertIndex + 2;
                triangles[triIndex + 2] = vertIndex + 1;

                triangles[triIndex + 3] = vertIndex + 1;
                triangles[triIndex + 4] = vertIndex + 2;
                triangles[triIndex + 5] = vertIndex + 3;
            }

            trailMesh.Clear();
            trailMesh.vertices = vertices;
            trailMesh.uv = uvs;
            trailMesh.triangles = triangles;
            trailMesh.RecalculateNormals();
            trailMesh.RecalculateBounds();

            drawcalls = 1; // Single mesh, single material
        }

        #endregion

        #region Degradation

        private void ApplyDegradation()
        {
            float currentFPS = Performance.PerformanceManager.Instance?.CurrentFPS ?? 60f;

            if (currentFPS < degradeFPS3)
            {
                // Severe degradation
                segmentMultiplier = 0.6f;
                glintEnabled = false;
                widthMultiplier = 0.7f;
            }
            else if (currentFPS < degradeFPS2)
            {
                // Medium degradation
                segmentMultiplier = 0.8f;
                glintEnabled = false;
                widthMultiplier = 0.85f;
            }
            else if (currentFPS < degradeFPS1)
            {
                // Light degradation
                segmentMultiplier = 0.8f;
                widthMultiplier = 1f;
            }
            else
            {
                // No degradation
                segmentMultiplier = 1f;
                glintEnabled = true;
                widthMultiplier = 1f;
            }
        }

        private float GetCurrentWidth()
        {
            float tierMultiplier = 1f;
            if (StyleMeterSystem.Instance != null)
            {
                switch (StyleMeterSystem.Instance.CurrentTier)
                {
                    case StyleMeterSystem.StyleTier.Tier1:
                        tierMultiplier = 1f;
                        break;
                    case StyleMeterSystem.StyleTier.Tier2:
                        tierMultiplier = 1.2f;
                        break;
                    case StyleMeterSystem.StyleTier.Tier3:
                        tierMultiplier = baseWidth / peakWidth; // Use peak width
                        break;
                }
            }

            return baseWidth * tierMultiplier * widthMultiplier;
        }

        #endregion

        #region Public API

        public void SetTrail(TrailData trail)
        {
            currentTrail = trail;
            positions.Clear();

            if (trail != null && trailMaterial != null)
            {
                // Apply trail colors to material
                trailMaterial.SetColor("_BaseColor", trail.baseColor);
                trailMaterial.SetColor("_SecondaryColor", trail.secondaryColor);
            }
        }

        public void ClearTrail()
        {
            currentTrail = null;
            positions.Clear();
            if (trailMesh != null) trailMesh.Clear();
        }

        public PerformanceStats GetPerformanceStats()
        {
            return new PerformanceStats
            {
                cpuMs = cpuTime,
                gpuMs = gpuTime,
                drawcalls = drawcalls,
                segmentsLive = currentSegments,
                segmentMultiplier = segmentMultiplier
            };
        }

        #endregion

        #region Helper Methods

        private Vector3 GetPlayerPosition()
        {
            // TODO: Get actual player position
            return transform.position;
        }

        #endregion

        #region Data Structures

        [System.Serializable]
        public class TrailData
        {
            public TrailRaritySystem.TrailRarity rarity;
            public TrailRaritySystem.TrailFamily family;
            public Color baseColor;
            public Color secondaryColor;
            public float animationSpeed;
        }

        [System.Serializable]
        public struct PerformanceStats
        {
            public float cpuMs;
            public float gpuMs;
            public int drawcalls;
            public int segmentsLive;
            public float segmentMultiplier;
        }

        #endregion

        private void OnDestroy()
        {
            if (Performance.PerformanceManager.Instance != null)
                Performance.PerformanceManager.Instance.OnRenderTick -= UpdateTrail;

            if (trailMesh != null)
                Destroy(trailMesh);
        }
    }
}
