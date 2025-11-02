using UnityEngine;

namespace InfiniteHaus.Level
{
    /// <summary>
    /// Represents a single corridor chunk in the procedural level.
    /// Contains spawn points for hazards, collectibles, and doors.
    /// </summary>
    public class CorridorChunk : MonoBehaviour
    {
        [Header("Chunk Identity")]
        [Tooltip("Unique identifier for this chunk type")]
        public string chunkID = "corridor_01";

        [Tooltip("Difficulty level of this chunk")]
        [Range(1, 10)]
        public int difficultyLevel = 1;

        [Header("Dimensions")]
        [Tooltip("Length of this corridor chunk")]
        public float chunkLength = 20f;

        [Tooltip("Start position (local)")]
        public Transform startPoint;

        [Tooltip("End position (local)")]
        public Transform endPoint;

        [Header("Spawn Points")]
        [Tooltip("Spawn points for hazards")]
        public Transform[] hazardSpawnPoints;

        [Tooltip("Spawn points for collectibles")]
        public Transform[] collectibleSpawnPoints;

        [Tooltip("Spawn points for doors (end of chunk)")]
        public Transform[] doorSpawnPoints;

        [Header("Props & Decoration")]
        [Tooltip("Decorative props in this chunk")]
        public GameObject[] decorativeProps;

        [Tooltip("Background elements")]
        public GameObject[] backgroundElements;

        // Runtime state
        private bool isActive = false;
        private GameObject[] spawnedHazards;
        private GameObject[] spawnedCollectibles;
        private GameObject[] spawnedDoors;

        /// <summary>
        /// Gets the world position of chunk start
        /// </summary>
        public Vector3 StartPosition => startPoint != null ? startPoint.position : transform.position;

        /// <summary>
        /// Gets the world position of chunk end
        /// </summary>
        public Vector3 EndPosition => endPoint != null ? endPoint.position : transform.position + Vector3.right * chunkLength;

        private void Awake()
        {
            // Auto-setup if start/end points not assigned
            if (startPoint == null)
            {
                GameObject startObj = new GameObject("StartPoint");
                startObj.transform.parent = transform;
                startObj.transform.localPosition = Vector3.zero;
                startPoint = startObj.transform;
            }

            if (endPoint == null)
            {
                GameObject endObj = new GameObject("EndPoint");
                endObj.transform.parent = transform;
                endObj.transform.localPosition = Vector3.right * chunkLength;
                endPoint = endObj.transform;
            }
        }

        /// <summary>
        /// Activates this corridor chunk
        /// </summary>
        public void Activate()
        {
            isActive = true;
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Deactivates this corridor chunk
        /// </summary>
        public void Deactivate()
        {
            isActive = false;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Spawns hazards at designated spawn points
        /// </summary>
        public void SpawnHazards(GameObject[] hazardPrefabs, int count)
        {
            if (hazardSpawnPoints == null || hazardSpawnPoints.Length == 0) return;

            spawnedHazards = new GameObject[count];

            // Randomly select spawn points
            for (int i = 0; i < count && i < hazardSpawnPoints.Length; i++)
            {
                if (hazardPrefabs.Length == 0) continue;

                GameObject hazardPrefab = hazardPrefabs[Random.Range(0, hazardPrefabs.Length)];
                Transform spawnPoint = hazardSpawnPoints[Random.Range(0, hazardSpawnPoints.Length)];

                if (hazardPrefab != null && spawnPoint != null)
                {
                    spawnedHazards[i] = Instantiate(hazardPrefab, spawnPoint.position, Quaternion.identity, transform);
                }
            }
        }

        /// <summary>
        /// Spawns collectibles at designated spawn points
        /// </summary>
        public void SpawnCollectibles(GameObject[] collectiblePrefabs, int count)
        {
            if (collectibleSpawnPoints == null || collectibleSpawnPoints.Length == 0) return;

            spawnedCollectibles = new GameObject[count];

            for (int i = 0; i < count && i < collectibleSpawnPoints.Length; i++)
            {
                if (collectiblePrefabs.Length == 0) continue;

                GameObject collectiblePrefab = collectiblePrefabs[Random.Range(0, collectiblePrefabs.Length)];
                Transform spawnPoint = collectibleSpawnPoints[Random.Range(0, collectibleSpawnPoints.Length)];

                if (collectiblePrefab != null && spawnPoint != null)
                {
                    spawnedCollectibles[i] = Instantiate(collectiblePrefab, spawnPoint.position, Quaternion.identity, transform);
                }
            }
        }

        /// <summary>
        /// Spawns doors at end of chunk
        /// </summary>
        public GameObject[] SpawnDoors(GameObject doorPrefab, int doorCount)
        {
            if (doorSpawnPoints == null || doorSpawnPoints.Length == 0)
            {
                Debug.LogWarning($"Chunk {chunkID} has no door spawn points!");
                return null;
            }

            spawnedDoors = new GameObject[doorCount];

            for (int i = 0; i < doorCount && i < doorSpawnPoints.Length; i++)
            {
                Transform spawnPoint = doorSpawnPoints[i];

                if (doorPrefab != null && spawnPoint != null)
                {
                    spawnedDoors[i] = Instantiate(doorPrefab, spawnPoint.position, Quaternion.identity, transform);

                    // Configure door
                    var doorHandler = spawnedDoors[i].GetComponent<DoorHandler>();
                    if (doorHandler != null)
                    {
                        doorHandler.SetDoorIndex(i);
                    }
                }
            }

            return spawnedDoors;
        }

        /// <summary>
        /// Cleans up spawned objects
        /// </summary>
        public void Cleanup()
        {
            if (spawnedHazards != null)
            {
                foreach (var hazard in spawnedHazards)
                {
                    if (hazard != null) Destroy(hazard);
                }
            }

            if (spawnedCollectibles != null)
            {
                foreach (var collectible in spawnedCollectibles)
                {
                    if (collectible != null) Destroy(collectible);
                }
            }

            if (spawnedDoors != null)
            {
                foreach (var door in spawnedDoors)
                {
                    if (door != null) Destroy(door);
                }
            }
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        // Debug visualization
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;

            Vector3 start = startPoint != null ? startPoint.position : transform.position;
            Vector3 end = endPoint != null ? endPoint.position : transform.position + Vector3.right * chunkLength;

            // Draw chunk boundaries
            Gizmos.DrawLine(start, end);
            Gizmos.DrawWireSphere(start, 0.5f);
            Gizmos.DrawWireSphere(end, 0.5f);

            // Draw spawn points
            if (hazardSpawnPoints != null)
            {
                Gizmos.color = Color.red;
                foreach (var point in hazardSpawnPoints)
                {
                    if (point != null) Gizmos.DrawWireSphere(point.position, 0.3f);
                }
            }

            if (collectibleSpawnPoints != null)
            {
                Gizmos.color = Color.yellow;
                foreach (var point in collectibleSpawnPoints)
                {
                    if (point != null) Gizmos.DrawWireSphere(point.position, 0.3f);
                }
            }

            if (doorSpawnPoints != null)
            {
                Gizmos.color = Color.blue;
                foreach (var point in doorSpawnPoints)
                {
                    if (point != null) Gizmos.DrawWireCube(point.position, Vector3.one * 0.5f);
                }
            }
        }
    }
}
