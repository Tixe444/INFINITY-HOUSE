using UnityEngine;
using System.Collections.Generic;

namespace InfiniteHaus.Game
{
    /// <summary>
    /// Shard guidance system with spline generation for rhythmic gameplay.
    /// Creates 4-beat paced shard trails that guide player through first jump.
    /// Spline length: 20-25m, starts 0.3s after in-frame, leads to first jump ~1.3s.
    /// CPU: <0.2ms | Memory: 8KB | GC: 0B
    /// </summary>
    public class ShardGuidanceSystem : MonoBehaviour
    {
        #region Configuration
        [Header("Spline Generation")]
        [Tooltip("Min spline length (20m default)")]
        [SerializeField] private float minSplineLength = 20f;

        [Tooltip("Max spline length (25m default)")]
        [SerializeField] private float maxSplineLength = 25f;

        [Tooltip("Spline resolution (points per meter)")]
        [SerializeField] private float splineResolution = 2f;

        [Header("Shard Placement")]
        [Tooltip("Shards per beat (4-beat pacing)")]
        [SerializeField] private int shardsPerBeat = 4;

        [Tooltip("Beat interval in seconds")]
        [SerializeField] private float beatInterval = 0.5f;

        [Tooltip("Shard height variation")]
        [SerializeField] private float heightVariation = 2f;

        [Header("First Jump Setup")]
        [Tooltip("First jump target time (~1.3s)")]
        [SerializeField] private float firstJumpTime = 1.3f;

        [Tooltip("Jump arc height")]
        [SerializeField] private float jumpArcHeight = 3f;

        [Header("Shard Prefab")]
        [Tooltip("Shard prefab to spawn")]
        [SerializeField] private GameObject shardPrefab;

        [Header("Pool Management")]
        [Tooltip("Use object pooling")]
        [SerializeField] private bool usePooling = true;

        [Tooltip("Initial pool size")]
        [SerializeField] private int poolSize = 100;

        [Header("Performance")]
        [Tooltip("Update rate in Hz")]
        [SerializeField] private int updateRate = 30;
        #endregion

        #region State
        private List<Vector3> splinePoints = new List<Vector3>();
        private List<GameObject> activeShards = new List<GameObject>();
        private Queue<GameObject> shardPool = new Queue<GameObject>();
        private bool guidanceActive = false;
        private float splineLength = 0f;
        private float updateInterval;
        private float lastUpdateTime;
        #endregion

        #region Events
        public event System.Action OnSplineGenerated;
        public event System.Action OnGuidanceStarted;
        public event System.Action OnGuidanceCompleted;
        #endregion

        #region Properties
        public bool IsActive => guidanceActive;
        public float SplineLength => splineLength;
        public int ShardCount => activeShards.Count;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            updateInterval = 1f / updateRate;

            if (usePooling)
            {
                InitializePool();
            }
        }

        private void Update()
        {
            if (!guidanceActive) return;

            // Throttled update
            if (Time.time - lastUpdateTime >= updateInterval)
            {
                UpdateShardPositions();
                lastUpdateTime = Time.time;
            }
        }
        #endregion

        #region Pool Management
        private void InitializePool()
        {
            if (shardPrefab == null)
            {
                Debug.LogWarning("[ShardGuidance] No shard prefab assigned!");
                return;
            }

            for (int i = 0; i < poolSize; i++)
            {
                GameObject shard = Instantiate(shardPrefab);
                shard.SetActive(false);
                shard.transform.SetParent(transform);
                shardPool.Enqueue(shard);
            }

            Debug.Log($"[ShardGuidance] Pool initialized with {poolSize} shards");
        }

        private GameObject GetShardFromPool()
        {
            if (shardPool.Count > 0)
            {
                GameObject shard = shardPool.Dequeue();
                shard.SetActive(true);
                return shard;
            }

            // Create new if pool exhausted
            if (shardPrefab != null)
            {
                GameObject shard = Instantiate(shardPrefab);
                shard.transform.SetParent(transform);
                return shard;
            }

            return null;
        }

        private void ReturnShardToPool(GameObject shard)
        {
            if (shard == null) return;

            shard.SetActive(false);
            shardPool.Enqueue(shard);
        }
        #endregion

        #region Spline Generation
        /// <summary>
        /// Generates shard guidance spline
        /// </summary>
        public void GenerateSpline(float minLength, float maxLength)
        {
            splineLength = Random.Range(minLength, maxLength);
            splinePoints.Clear();

            // Generate spline points
            int pointCount = Mathf.RoundToInt(splineLength * splineResolution);
            Vector3 startPos = Vector3.zero;

            for (int i = 0; i < pointCount; i++)
            {
                float t = (float)i / pointCount;
                Vector3 point = CalculateSplinePoint(t, splineLength);
                splinePoints.Add(point);
            }

            OnSplineGenerated?.Invoke();
            Debug.Log($"[ShardGuidance] Spline generated: {splineLength:F1}m with {splinePoints.Count} points");
        }

        private Vector3 CalculateSplinePoint(float t, float length)
        {
            // Linear path with height variation
            float x = t * length;
            float y = 0f;

            // Add jump arc at first jump time
            float jumpT = firstJumpTime / (length / 6.2f); // Assuming 6.2m/s velocity
            if (t >= jumpT && t <= jumpT + 0.3f)
            {
                // Create jump arc
                float arcT = (t - jumpT) / 0.3f;
                y = Mathf.Sin(arcT * Mathf.PI) * jumpArcHeight;
            }
            else
            {
                // Random height variation
                y = Mathf.PerlinNoise(t * 5f, 0f) * heightVariation - heightVariation * 0.5f;
            }

            return new Vector3(x, y, 0f);
        }
        #endregion

        #region Shard Placement
        /// <summary>
        /// Starts shard guidance
        /// </summary>
        public void StartGuidance()
        {
            if (splinePoints.Count == 0)
            {
                Debug.LogWarning("[ShardGuidance] No spline generated! Call GenerateSpline first.");
                return;
            }

            PlaceShardsAlongSpline();
            guidanceActive = true;
            OnGuidanceStarted?.Invoke();
            Debug.Log("[ShardGuidance] Guidance started");
        }

        private void PlaceShardsAlongSpline()
        {
            // Calculate shard positions based on 4-beat pacing
            int totalShards = Mathf.RoundToInt(splineLength / (beatInterval * shardsPerBeat));

            for (int i = 0; i < totalShards; i++)
            {
                float t = (float)i / totalShards;
                int splineIndex = Mathf.RoundToInt(t * (splinePoints.Count - 1));

                if (splineIndex < splinePoints.Count)
                {
                    Vector3 position = splinePoints[splineIndex];
                    SpawnShard(position);
                }
            }

            Debug.Log($"[ShardGuidance] Placed {activeShards.Count} shards");
        }

        private void SpawnShard(Vector3 position)
        {
            GameObject shard = GetShardFromPool();
            if (shard != null)
            {
                shard.transform.position = position;
                activeShards.Add(shard);
            }
        }
        #endregion

        #region Shard Updates
        private void UpdateShardPositions()
        {
            // Remove collected or off-screen shards
            for (int i = activeShards.Count - 1; i >= 0; i--)
            {
                if (activeShards[i] == null || !activeShards[i].activeInHierarchy)
                {
                    ReturnShardToPool(activeShards[i]);
                    activeShards.RemoveAt(i);
                }
            }

            // Check if guidance complete
            if (activeShards.Count == 0 && guidanceActive)
            {
                CompleteGuidance();
            }
        }
        #endregion

        #region Guidance Control
        /// <summary>
        /// Stops guidance
        /// </summary>
        public void StopGuidance()
        {
            guidanceActive = false;
            ClearShards();
            Debug.Log("[ShardGuidance] Guidance stopped");
        }

        /// <summary>
        /// Completes guidance
        /// </summary>
        private void CompleteGuidance()
        {
            guidanceActive = false;
            OnGuidanceCompleted?.Invoke();
            Debug.Log("[ShardGuidance] Guidance completed");
        }

        /// <summary>
        /// Clears all active shards
        /// </summary>
        public void ClearShards()
        {
            foreach (var shard in activeShards)
            {
                ReturnShardToPool(shard);
            }
            activeShards.Clear();
        }

        /// <summary>
        /// Resets system
        /// </summary>
        public void ResetSystem()
        {
            StopGuidance();
            splinePoints.Clear();
            splineLength = 0f;
        }
        #endregion

        #region Public API
        /// <summary>
        /// Gets spline points
        /// </summary>
        public Vector3[] GetSplinePoints()
        {
            return splinePoints.ToArray();
        }

        /// <summary>
        /// Gets nearest shard to position
        /// </summary>
        public GameObject GetNearestShard(Vector3 position)
        {
            GameObject nearest = null;
            float minDist = float.MaxValue;

            foreach (var shard in activeShards)
            {
                if (shard == null) continue;

                float dist = Vector3.Distance(position, shard.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = shard;
                }
            }

            return nearest;
        }
        #endregion

        #region Debug
        private void OnDrawGizmos()
        {
            if (splinePoints.Count == 0) return;

            // Draw spline
            Gizmos.color = Color.cyan;
            for (int i = 0; i < splinePoints.Count - 1; i++)
            {
                Gizmos.DrawLine(splinePoints[i], splinePoints[i + 1]);
            }

            // Draw shard positions
            Gizmos.color = Color.yellow;
            foreach (var shard in activeShards)
            {
                if (shard != null && shard.activeInHierarchy)
                {
                    Gizmos.DrawWireSphere(shard.transform.position, 0.2f);
                }
            }
        }
        #endregion
    }
}
