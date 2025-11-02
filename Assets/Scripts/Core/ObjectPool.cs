using UnityEngine;
using System.Collections.Generic;

namespace InfiniteHaus.Core
{
    /// <summary>
    /// Generic object pooling system for performance optimization.
    /// Reduces garbage collection and instantiation overhead.
    /// Especially important for mobile platforms.
    /// </summary>
    public class ObjectPool : MonoBehaviour
    {
        [Header("Pool Settings")]
        [Tooltip("Prefab to pool")]
        [SerializeField] private GameObject prefab;

        [Tooltip("Initial pool size")]
        [SerializeField] private int initialSize = 10;

        [Tooltip("Can pool grow if needed?")]
        [SerializeField] private bool canGrow = true;

        [Tooltip("Maximum pool size (0 = unlimited)")]
        [SerializeField] private int maxSize = 50;

        [Header("Auto Return")]
        [Tooltip("Automatically return objects after delay")]
        [SerializeField] private bool autoReturn = false;

        [Tooltip("Auto return delay (seconds)")]
        [SerializeField] private float autoReturnDelay = 5f;

        // Pool storage
        private Queue<GameObject> availableObjects = new Queue<GameObject>();
        private HashSet<GameObject> activeObjects = new HashSet<GameObject>();

        // Statistics
        private int totalCreated = 0;
        private int peakActive = 0;

        // Properties
        public int TotalCreated => totalCreated;
        public int Available => availableObjects.Count;
        public int Active => activeObjects.Count;
        public int PeakActive => peakActive;

        private void Start()
        {
            InitializePool();
        }

        /// <summary>
        /// Initializes the pool with initial objects
        /// </summary>
        private void InitializePool()
        {
            if (prefab == null)
            {
                Debug.LogError("ObjectPool: No prefab assigned!");
                return;
            }

            for (int i = 0; i < initialSize; i++)
            {
                CreateNewObject();
            }

            Debug.Log($"ObjectPool initialized: {prefab.name} x{initialSize}");
        }

        /// <summary>
        /// Creates a new pooled object
        /// </summary>
        private GameObject CreateNewObject()
        {
            GameObject obj = Instantiate(prefab, transform);
            obj.SetActive(false);
            availableObjects.Enqueue(obj);
            totalCreated++;
            return obj;
        }

        /// <summary>
        /// Gets an object from the pool
        /// </summary>
        public GameObject Get()
        {
            GameObject obj;

            // Try to get from available pool
            if (availableObjects.Count > 0)
            {
                obj = availableObjects.Dequeue();
            }
            // Create new if pool can grow
            else if (canGrow && (maxSize == 0 || totalCreated < maxSize))
            {
                obj = CreateNewObject();
            }
            // Pool exhausted and can't grow
            else
            {
                Debug.LogWarning($"ObjectPool exhausted: {prefab.name}");
                return null;
            }

            // Activate and track
            obj.SetActive(true);
            activeObjects.Add(obj);

            // Update statistics
            if (activeObjects.Count > peakActive)
            {
                peakActive = activeObjects.Count;
            }

            // Auto return if enabled
            if (autoReturn)
            {
                StartCoroutine(AutoReturnCoroutine(obj));
            }

            return obj;
        }

        /// <summary>
        /// Gets an object at a specific position and rotation
        /// </summary>
        public GameObject Get(Vector3 position, Quaternion rotation)
        {
            GameObject obj = Get();
            if (obj != null)
            {
                obj.transform.position = position;
                obj.transform.rotation = rotation;
            }
            return obj;
        }

        /// <summary>
        /// Returns an object to the pool
        /// </summary>
        public void Return(GameObject obj)
        {
            if (obj == null) return;

            if (!activeObjects.Contains(obj))
            {
                Debug.LogWarning($"ObjectPool: Trying to return object not from this pool!");
                return;
            }

            // Deactivate and return to pool
            obj.SetActive(false);
            obj.transform.SetParent(transform);
            activeObjects.Remove(obj);
            availableObjects.Enqueue(obj);
        }

        /// <summary>
        /// Returns all active objects to pool
        /// </summary>
        public void ReturnAll()
        {
            GameObject[] objectsToReturn = new GameObject[activeObjects.Count];
            activeObjects.CopyTo(objectsToReturn);

            foreach (var obj in objectsToReturn)
            {
                Return(obj);
            }
        }

        /// <summary>
        /// Auto-return coroutine
        /// </summary>
        private System.Collections.IEnumerator AutoReturnCoroutine(GameObject obj)
        {
            yield return new WaitForSeconds(autoReturnDelay);

            if (obj != null && activeObjects.Contains(obj))
            {
                Return(obj);
            }
        }

        /// <summary>
        /// Preloads additional objects into the pool
        /// </summary>
        public void Preload(int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (maxSize > 0 && totalCreated >= maxSize) break;
                CreateNewObject();
            }

            Debug.Log($"ObjectPool preloaded {count} objects. Total: {totalCreated}");
        }

        /// <summary>
        /// Clears the pool
        /// </summary>
        public void Clear()
        {
            ReturnAll();

            foreach (var obj in availableObjects)
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }

            availableObjects.Clear();
            activeObjects.Clear();
            totalCreated = 0;
            peakActive = 0;
        }

        /// <summary>
        /// Gets pool statistics
        /// </summary>
        public string GetStatistics()
        {
            return $"Pool '{prefab.name}': Total={totalCreated}, Available={availableObjects.Count}, Active={activeObjects.Count}, Peak={peakActive}";
        }

        private void OnDestroy()
        {
            Clear();
        }

        // Debug info
        #if UNITY_EDITOR
        private void OnValidate()
        {
            if (maxSize > 0 && initialSize > maxSize)
            {
                Debug.LogWarning($"ObjectPool: Initial size ({initialSize}) exceeds max size ({maxSize})");
            }
        }
        #endif
    }

    /// <summary>
    /// Helper component to automatically return pooled objects
    /// </summary>
    public class PooledObject : MonoBehaviour
    {
        private ObjectPool pool;
        private float lifetime = 0f;
        private bool hasLifetime = false;

        public void SetPool(ObjectPool objectPool)
        {
            pool = objectPool;
        }

        public void SetLifetime(float time)
        {
            lifetime = time;
            hasLifetime = true;
        }

        private void OnEnable()
        {
            if (hasLifetime && lifetime > 0f)
            {
                Invoke(nameof(ReturnToPool), lifetime);
            }
        }

        private void OnDisable()
        {
            CancelInvoke();
        }

        public void ReturnToPool()
        {
            if (pool != null)
            {
                pool.Return(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
