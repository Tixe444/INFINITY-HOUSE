// ═══════════════════════════════════════════════════════════════════════════════
// INFINITY HOUSE v5.5 - Made by Mate Makovics
// Universal Object Pool Manager - Zero Allocation Pooling
// ═══════════════════════════════════════════════════════════════════════════════

using UnityEngine;
using System;
using System.Collections.Generic;

namespace InfinityHouse.Performance
{
    /// <summary>
    /// Universal object pooling system for zero GC allocation.
    /// Pools: Shards, FX, Hazards, Doors, Projectiles.
    /// Target: >90% reuse rate, <1ms pool operations.
    /// </summary>
    public class ObjectPoolManager : MonoBehaviour
    {
        // Singleton
        public static ObjectPoolManager Instance { get; private set; }

        [Header("Pool Configuration")]
        [SerializeField] private int initialPoolSize = 50;
        [SerializeField] private int maxPoolSize = 200;
        [SerializeField] private bool allowGrowth = true;

        [Header("Prefab Registry")]
        [SerializeField] private PoolPrefab[] poolPrefabs;

        // Pools
        private Dictionary<string, Pool> pools;
        private Dictionary<GameObject, string> activeObjects; // Track which pool an object belongs to

        // Statistics
        private int totalGets;
        private int totalReturns;
        private int totalCreations;
        private float reuseRate;

        // Properties
        public float ReuseRate => reuseRate;
        public int TotalPooledObjects => GetTotalPooledCount();

        [Serializable]
        public class PoolPrefab
        {
            public string poolId;
            public GameObject prefab;
            public int preloadCount = 10;
            public PoolType type;
        }

        public enum PoolType
        {
            Shard,
            FX,
            Hazard,
            Door,
            Projectile,
            UI,
            Audio
        }

        private class Pool
        {
            public string poolId;
            public GameObject prefab;
            public Queue<GameObject> available;
            public HashSet<GameObject> active;
            public Transform container;
            public int totalCreated;
            public int totalGets;
            public int totalReturns;

            public Pool(string id, GameObject prefabTemplate, Transform parent)
            {
                poolId = id;
                prefab = prefabTemplate;
                available = new Queue<GameObject>();
                active = new HashSet<GameObject>();

                // Create container
                GameObject containerObj = new GameObject($"Pool_{id}");
                containerObj.transform.SetParent(parent);
                container = containerObj.transform;

                totalCreated = 0;
                totalGets = 0;
                totalReturns = 0;
            }
        }

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
            pools = new Dictionary<string, Pool>();
            activeObjects = new Dictionary<GameObject, string>();

            // Register all prefabs
            foreach (var poolPrefab in poolPrefabs)
            {
                RegisterPool(poolPrefab.poolId, poolPrefab.prefab, poolPrefab.preloadCount);
            }

            Debug.Log($"[ObjectPoolManager] Initialized with {pools.Count} pools");
        }

        #region Pool Registration

        /// <summary>
        /// Registers a new pool
        /// </summary>
        public void RegisterPool(string poolId, GameObject prefab, int preloadCount = 10)
        {
            if (pools.ContainsKey(poolId))
            {
                Debug.LogWarning($"[ObjectPoolManager] Pool '{poolId}' already registered");
                return;
            }

            Pool pool = new Pool(poolId, prefab, transform);
            pools[poolId] = pool;

            // Preload objects
            for (int i = 0; i < preloadCount; i++)
            {
                GameObject obj = CreateNewObject(pool);
                ReturnToPool(obj);
            }

            Debug.Log($"[ObjectPoolManager] Registered pool '{poolId}' with {preloadCount} preloaded objects");
        }

        #endregion

        #region Get/Return

        /// <summary>
        /// Gets an object from the pool
        /// </summary>
        public GameObject Get(string poolId, Vector3 position, Quaternion rotation)
        {
            if (!pools.ContainsKey(poolId))
            {
                Debug.LogError($"[ObjectPoolManager] Pool '{poolId}' not found!");
                return null;
            }

            Pool pool = pools[poolId];
            GameObject obj;

            // Get from pool or create new
            if (pool.available.Count > 0)
            {
                obj = pool.available.Dequeue();
            }
            else
            {
                if (pool.totalCreated >= maxPoolSize && !allowGrowth)
                {
                    Debug.LogWarning($"[ObjectPoolManager] Pool '{poolId}' at max size!");
                    return null;
                }

                obj = CreateNewObject(pool);
                totalCreations++;
            }

            // Activate and position
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);

            // Track
            pool.active.Add(obj);
            activeObjects[obj] = poolId;
            pool.totalGets++;
            totalGets++;

            // Notify pooled object
            IPoolable poolable = obj.GetComponent<IPoolable>();
            poolable?.OnSpawn();

            return obj;
        }

        /// <summary>
        /// Returns an object to the pool
        /// </summary>
        public void Return(GameObject obj)
        {
            if (obj == null) return;

            if (!activeObjects.ContainsKey(obj))
            {
                Debug.LogWarning($"[ObjectPoolManager] Object {obj.name} not tracked in pool!");
                return;
            }

            string poolId = activeObjects[obj];
            Pool pool = pools[poolId];

            // Notify pooled object
            IPoolable poolable = obj.GetComponent<IPoolable>();
            poolable?.OnDespawn();

            // Deactivate and return to pool
            obj.SetActive(false);
            obj.transform.SetParent(pool.container);

            pool.active.Remove(obj);
            pool.available.Enqueue(obj);
            activeObjects.Remove(obj);

            pool.totalReturns++;
            totalReturns++;

            // Update reuse rate
            reuseRate = totalReturns > 0 ? (float)totalReturns / (totalGets + 1) : 0f;
        }

        /// <summary>
        /// Returns object to pool after delay
        /// </summary>
        public void ReturnDelayed(GameObject obj, float delay)
        {
            StartCoroutine(ReturnAfterDelay(obj, delay));
        }

        private System.Collections.IEnumerator ReturnAfterDelay(GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            Return(obj);
        }

        #endregion

        #region Helper Methods

        private GameObject CreateNewObject(Pool pool)
        {
            GameObject obj = Instantiate(pool.prefab, pool.container);
            obj.name = $"{pool.poolId}_{pool.totalCreated}";
            obj.SetActive(false);

            // Add poolable component if missing
            if (obj.GetComponent<IPoolable>() == null)
            {
                obj.AddComponent<PoolableObject>();
            }

            pool.totalCreated++;
            return obj;
        }

        private void ReturnToPool(GameObject obj)
        {
            obj.SetActive(false);
        }

        private int GetTotalPooledCount()
        {
            int count = 0;
            foreach (var pool in pools.Values)
            {
                count += pool.totalCreated;
            }
            return count;
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Clears a specific pool
        /// </summary>
        public void ClearPool(string poolId)
        {
            if (!pools.ContainsKey(poolId)) return;

            Pool pool = pools[poolId];

            // Return all active objects
            List<GameObject> activeList = new List<GameObject>(pool.active);
            foreach (var obj in activeList)
            {
                Return(obj);
            }

            Debug.Log($"[ObjectPoolManager] Cleared pool '{poolId}'");
        }

        /// <summary>
        /// Clears all pools
        /// </summary>
        public void ClearAllPools()
        {
            foreach (var poolId in new List<string>(pools.Keys))
            {
                ClearPool(poolId);
            }

            Debug.Log("[ObjectPoolManager] All pools cleared");
        }

        #endregion

        #region Statistics

        /// <summary>
        /// Gets pool statistics
        /// </summary>
        public PoolStats GetPoolStats(string poolId)
        {
            if (!pools.ContainsKey(poolId))
            {
                return new PoolStats();
            }

            Pool pool = pools[poolId];

            return new PoolStats
            {
                poolId = poolId,
                totalCreated = pool.totalCreated,
                availableCount = pool.available.Count,
                activeCount = pool.active.Count,
                totalGets = pool.totalGets,
                totalReturns = pool.totalReturns,
                reuseRate = pool.totalReturns > 0 ? (float)pool.totalReturns / pool.totalGets : 0f
            };
        }

        /// <summary>
        /// Gets global statistics
        /// </summary>
        public GlobalPoolStats GetGlobalStats()
        {
            return new GlobalPoolStats
            {
                totalPools = pools.Count,
                totalObjects = GetTotalPooledCount(),
                totalGets = totalGets,
                totalReturns = totalReturns,
                totalCreations = totalCreations,
                globalReuseRate = reuseRate
            };
        }

        #endregion

        #region Data Structures

        [Serializable]
        public struct PoolStats
        {
            public string poolId;
            public int totalCreated;
            public int availableCount;
            public int activeCount;
            public int totalGets;
            public int totalReturns;
            public float reuseRate;
        }

        [Serializable]
        public struct GlobalPoolStats
        {
            public int totalPools;
            public int totalObjects;
            public int totalGets;
            public int totalReturns;
            public int totalCreations;
            public float globalReuseRate;
        }

        #endregion

        #region Debug

        private void OnGUI()
        {
            if (!Application.isPlaying) return;

            GUIStyle style = new GUIStyle();
            style.normal.textColor = reuseRate >= 0.9f ? Color.green : (reuseRate >= 0.7f ? Color.yellow : Color.red);
            style.fontSize = 16;

            int yOffset = 130;
            GUI.Label(new Rect(10, yOffset, 300, 30), $"Pool Reuse: {reuseRate * 100:F1}% (Target: >90%)", style);
            GUI.Label(new Rect(10, yOffset + 30, 300, 30), $"Pooled Objects: {GetTotalPooledCount()}", style);
        }

        #endregion
    }

    /// <summary>
    /// Interface for poolable objects
    /// </summary>
    public interface IPoolable
    {
        void OnSpawn();
        void OnDespawn();
    }

    /// <summary>
    /// Default poolable component
    /// </summary>
    public class PoolableObject : MonoBehaviour, IPoolable
    {
        public virtual void OnSpawn()
        {
            // Override in derived classes
        }

        public virtual void OnDespawn()
        {
            // Override in derived classes
        }
    }
}
