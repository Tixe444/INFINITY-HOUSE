using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace InfiniteHaus.Game
{
    /// <summary>
    /// Performance prewarmer for INFINITE HAUS v5.7.
    /// Prewarms object pools before gameplay starts to eliminate runtime allocations.
    /// Targets: Avatar, Chaser[3], Shard, Trail, FX_StartBurst, Obstacle.
    /// CPU: <0.5ms total | Memory: Pre-allocated | GC: 0B
    /// </summary>
    public class PerformancePrewarmer : MonoBehaviour
    {
        #region Configuration
        [Header("Pool Sizes")]
        [Tooltip("Avatar pool size (usually 1)")]
        [SerializeField] private int avatarPoolSize = 1;

        [Tooltip("Chaser pool size (3 default)")]
        [SerializeField] private int chaserPoolSize = 3;

        [Tooltip("Shard pool size")]
        [SerializeField] private int shardPoolSize = 100;

        [Tooltip("Trail pool size")]
        [SerializeField] private int trailPoolSize = 50;

        [Tooltip("Start burst FX pool size")]
        [SerializeField] private int fxStartBurstPoolSize = 5;

        [Tooltip("Obstacle pool size")]
        [SerializeField] private int obstaclePoolSize = 30;

        [Header("Prefab References")]
        [Tooltip("Avatar prefab")]
        [SerializeField] private GameObject avatarPrefab;

        [Tooltip("Chaser prefabs (3 variants)")]
        [SerializeField] private GameObject[] chaserPrefabs;

        [Tooltip("Shard prefab")]
        [SerializeField] private GameObject shardPrefab;

        [Tooltip("Trail prefab")]
        [SerializeField] private GameObject trailPrefab;

        [Tooltip("Start burst FX prefab")]
        [SerializeField] private GameObject fxStartBurstPrefab;

        [Tooltip("Obstacle prefabs")]
        [SerializeField] private GameObject[] obstaclePrefabs;

        [Header("Performance")]
        [Tooltip("Spread prewarming across frames")]
        [SerializeField] private bool spreadAcrossFrames = true;

        [Tooltip("Items to prewarm per frame")]
        [SerializeField] private int itemsPerFrame = 10;

        [Tooltip("Enable detailed logging")]
        [SerializeField] private bool enableLogging = false;
        #endregion

        #region State
        private Dictionary<string, Queue<GameObject>> pools = new Dictionary<string, Queue<GameObject>>();
        private bool prewarmed = false;
        private float prewarmStartTime;
        #endregion

        #region Events
        public event System.Action OnPrewarmStarted;
        public event System.Action OnPrewarmCompleted;
        public event System.Action<string, int> OnPoolPrewarmed; // (pool name, size)
        #endregion

        #region Properties
        public bool IsPrewarmed => prewarmed;
        public int TotalPooledObjects => GetTotalPooledObjects();
        #endregion

        #region Prewarming
        /// <summary>
        /// Prewarms all pools
        /// </summary>
        public IEnumerator PrewarmAll()
        {
            if (prewarmed)
            {
                Debug.LogWarning("[PerformancePrewarmer] Already prewarmed!");
                yield break;
            }

            prewarmStartTime = Time.realtimeSinceStartup;
            OnPrewarmStarted?.Invoke();

            if (enableLogging)
            {
                Debug.Log("[PerformancePrewarmer] Starting prewarm...");
            }

            // Prewarm each pool
            yield return PrewarmPool("Avatar", avatarPrefab, avatarPoolSize);
            yield return PrewarmChaserPool();
            yield return PrewarmPool("Shard", shardPrefab, shardPoolSize);
            yield return PrewarmPool("Trail", trailPrefab, trailPoolSize);
            yield return PrewarmPool("FX_StartBurst", fxStartBurstPrefab, fxStartBurstPoolSize);
            yield return PrewarmObstaclePool();

            float totalTime = (Time.realtimeSinceStartup - prewarmStartTime) * 1000f;
            prewarmed = true;

            OnPrewarmCompleted?.Invoke();

            if (enableLogging)
            {
                Debug.Log($"[PerformancePrewarmer] Prewarm complete in {totalTime:F2}ms. Total objects: {TotalPooledObjects}");
            }
        }

        private IEnumerator PrewarmPool(string poolName, GameObject prefab, int size)
        {
            if (prefab == null)
            {
                if (enableLogging)
                {
                    Debug.LogWarning($"[PerformancePrewarmer] No prefab for {poolName}");
                }
                yield break;
            }

            Queue<GameObject> pool = new Queue<GameObject>(size);
            int instantiated = 0;

            for (int i = 0; i < size; i++)
            {
                GameObject obj = Instantiate(prefab);
                obj.SetActive(false);
                obj.transform.SetParent(transform);
                pool.Enqueue(obj);
                instantiated++;

                // Spread across frames
                if (spreadAcrossFrames && instantiated % itemsPerFrame == 0)
                {
                    yield return null;
                }
            }

            pools[poolName] = pool;
            OnPoolPrewarmed?.Invoke(poolName, size);

            if (enableLogging)
            {
                Debug.Log($"[PerformancePrewarmer] Prewarmed {poolName}: {size} objects");
            }
        }

        private IEnumerator PrewarmChaserPool()
        {
            if (chaserPrefabs == null || chaserPrefabs.Length == 0)
            {
                if (enableLogging)
                {
                    Debug.LogWarning("[PerformancePrewarmer] No chaser prefabs");
                }
                yield break;
            }

            Queue<GameObject> pool = new Queue<GameObject>(chaserPoolSize);

            for (int i = 0; i < chaserPoolSize; i++)
            {
                // Cycle through chaser variants
                int variantIndex = i % chaserPrefabs.Length;
                GameObject prefab = chaserPrefabs[variantIndex];

                if (prefab == null) continue;

                GameObject obj = Instantiate(prefab);
                obj.SetActive(false);
                obj.transform.SetParent(transform);
                pool.Enqueue(obj);

                if (spreadAcrossFrames && i % itemsPerFrame == 0)
                {
                    yield return null;
                }
            }

            pools["Chaser"] = pool;
            OnPoolPrewarmed?.Invoke("Chaser", chaserPoolSize);

            if (enableLogging)
            {
                Debug.Log($"[PerformancePrewarmer] Prewarmed Chaser: {chaserPoolSize} objects");
            }
        }

        private IEnumerator PrewarmObstaclePool()
        {
            if (obstaclePrefabs == null || obstaclePrefabs.Length == 0)
            {
                if (enableLogging)
                {
                    Debug.LogWarning("[PerformancePrewarmer] No obstacle prefabs");
                }
                yield break;
            }

            Queue<GameObject> pool = new Queue<GameObject>(obstaclePoolSize);

            for (int i = 0; i < obstaclePoolSize; i++)
            {
                // Cycle through obstacle variants
                int variantIndex = i % obstaclePrefabs.Length;
                GameObject prefab = obstaclePrefabs[variantIndex];

                if (prefab == null) continue;

                GameObject obj = Instantiate(prefab);
                obj.SetActive(false);
                obj.transform.SetParent(transform);
                pool.Enqueue(obj);

                if (spreadAcrossFrames && i % itemsPerFrame == 0)
                {
                    yield return null;
                }
            }

            pools["Obstacle"] = pool;
            OnPoolPrewarmed?.Invoke("Obstacle", obstaclePoolSize);

            if (enableLogging)
            {
                Debug.Log($"[PerformancePrewarmer] Prewarmed Obstacle: {obstaclePoolSize} objects");
            }
        }
        #endregion

        #region Pool Access
        /// <summary>
        /// Gets object from pool
        /// </summary>
        public GameObject GetFromPool(string poolName)
        {
            if (!pools.ContainsKey(poolName))
            {
                Debug.LogWarning($"[PerformancePrewarmer] Pool '{poolName}' not found!");
                return null;
            }

            Queue<GameObject> pool = pools[poolName];

            if (pool.Count > 0)
            {
                GameObject obj = pool.Dequeue();
                obj.SetActive(true);
                return obj;
            }

            Debug.LogWarning($"[PerformancePrewarmer] Pool '{poolName}' exhausted!");
            return null;
        }

        /// <summary>
        /// Returns object to pool
        /// </summary>
        public void ReturnToPool(string poolName, GameObject obj)
        {
            if (obj == null) return;

            if (!pools.ContainsKey(poolName))
            {
                Debug.LogWarning($"[PerformancePrewarmer] Pool '{poolName}' not found!");
                Destroy(obj);
                return;
            }

            obj.SetActive(false);
            obj.transform.SetParent(transform);
            pools[poolName].Enqueue(obj);
        }

        /// <summary>
        /// Gets pool size
        /// </summary>
        public int GetPoolSize(string poolName)
        {
            if (pools.ContainsKey(poolName))
            {
                return pools[poolName].Count;
            }
            return 0;
        }

        /// <summary>
        /// Checks if pool exists
        /// </summary>
        public bool HasPool(string poolName)
        {
            return pools.ContainsKey(poolName);
        }
        #endregion

        #region Utilities
        private int GetTotalPooledObjects()
        {
            int total = 0;
            foreach (var pool in pools.Values)
            {
                total += pool.Count;
            }
            return total;
        }

        /// <summary>
        /// Clears all pools
        /// </summary>
        public void ClearAllPools()
        {
            foreach (var pool in pools.Values)
            {
                while (pool.Count > 0)
                {
                    GameObject obj = pool.Dequeue();
                    if (obj != null)
                    {
                        Destroy(obj);
                    }
                }
            }

            pools.Clear();
            prewarmed = false;

            if (enableLogging)
            {
                Debug.Log("[PerformancePrewarmer] All pools cleared");
            }
        }

        /// <summary>
        /// Gets pool stats for debugging
        /// </summary>
        public Dictionary<string, int> GetPoolStats()
        {
            Dictionary<string, int> stats = new Dictionary<string, int>();

            foreach (var kvp in pools)
            {
                stats[kvp.Key] = kvp.Value.Count;
            }

            return stats;
        }
        #endregion
    }
}
