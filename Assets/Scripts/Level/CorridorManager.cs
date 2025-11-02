using UnityEngine;
using System.Collections.Generic;
using InfiniteHaus.Data;

namespace InfiniteHaus.Level
{
    /// <summary>
    /// Manages procedural corridor generation, door placement, and level progression.
    /// Handles infinite level generation by spawning chunks ahead and despawning behind player.
    /// </summary>
    public class CorridorManager : MonoBehaviour
    {
        [Header("Corridor Sets")]
        [Tooltip("Array of corridor sets for different difficulty levels")]
        [SerializeField] private CorridorSet[] corridorSets;

        [Tooltip("Current corridor set index")]
        [SerializeField] private int currentSetIndex = 0;

        [Header("Generation Settings")]
        [Tooltip("Number of chunks to keep active ahead of player")]
        [SerializeField] private int chunksAhead = 3;

        [Tooltip("Number of chunks to keep active behind player")]
        [SerializeField] private int chunksBehind = 1;

        [Tooltip("Door count per chunk end")]
        [SerializeField] private int doorsPerChunk = 2;

        [Header("Prefabs")]
        [Tooltip("Door prefab")]
        [SerializeField] private GameObject doorPrefab;

        [Tooltip("Hazard prefabs")]
        [SerializeField] private GameObject[] hazardPrefabs;

        [Tooltip("Collectible prefabs")]
        [SerializeField] private GameObject[] collectiblePrefabs;

        [Header("Difficulty Scaling")]
        [Tooltip("Distance traveled to increase difficulty")]
        [SerializeField] private float distancePerDifficultyIncrease = 100f;

        [Tooltip("Current difficulty multiplier")]
        [SerializeField] private float currentDifficultyMultiplier = 1f;

        [Header("References")]
        [Tooltip("Player transform for tracking position")]
        [SerializeField] private Transform playerTransform;

        // Runtime state
        private List<GameObject> activeChunks = new List<GameObject>();
        private Vector3 nextSpawnPosition = Vector3.zero;
        private float totalDistanceTraveled = 0f;
        private int chunksGenerated = 0;

        // Events
        public System.Action<int> OnChunkGenerated;
        public System.Action<float> OnDifficultyIncreased;
        public System.Action<float> OnDistanceChanged;

        // Properties
        public float TotalDistance => totalDistanceTraveled;
        public int ChunksGenerated => chunksGenerated;

        private void Start()
        {
            if (playerTransform == null)
            {
                playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
            }

            // Generate initial chunks
            for (int i = 0; i < chunksAhead; i++)
            {
                GenerateNextChunk();
            }
        }

        private void Update()
        {
            if (playerTransform == null) return;

            UpdateDistanceTraveled();
            UpdateChunkGeneration();
            UpdateDifficulty();
        }

        /// <summary>
        /// Updates total distance traveled
        /// </summary>
        private void UpdateDistanceTraveled()
        {
            float currentDistance = playerTransform.position.x;

            if (currentDistance > totalDistanceTraveled)
            {
                totalDistanceTraveled = currentDistance;
                OnDistanceChanged?.Invoke(totalDistanceTraveled);
            }
        }

        /// <summary>
        /// Updates chunk generation/despawning based on player position
        /// </summary>
        private void UpdateChunkGeneration()
        {
            // Generate new chunks ahead
            while (ShouldGenerateAhead())
            {
                GenerateNextChunk();
            }

            // Despawn chunks behind player
            DespawnOldChunks();
        }

        /// <summary>
        /// Checks if we should generate more chunks ahead
        /// </summary>
        private bool ShouldGenerateAhead()
        {
            if (activeChunks.Count == 0) return true;

            // Find furthest chunk
            float furthestChunkX = float.MinValue;
            foreach (var chunk in activeChunks)
            {
                if (chunk != null)
                {
                    float chunkX = chunk.transform.position.x;
                    if (chunkX > furthestChunkX)
                    {
                        furthestChunkX = chunkX;
                    }
                }
            }

            // Generate if player is getting close to last chunk
            float playerX = playerTransform.position.x;
            float distanceToFurthest = furthestChunkX - playerX;

            return distanceToFurthest < (chunksAhead * 20f); // Assuming ~20 units per chunk
        }

        /// <summary>
        /// Generates the next corridor chunk
        /// </summary>
        private void GenerateNextChunk()
        {
            // Get current corridor set
            CorridorSet currentSet = GetCurrentCorridorSet();
            if (currentSet == null)
            {
                Debug.LogError("No corridor set available!");
                return;
            }

            // Get random corridor prefab from set
            GameObject corridorPrefab = currentSet.GetRandomCorridor(chunksGenerated > 5);
            if (corridorPrefab == null)
            {
                Debug.LogError("Corridor prefab is null!");
                return;
            }

            // Instantiate chunk
            GameObject newChunk = Instantiate(corridorPrefab, nextSpawnPosition, Quaternion.identity, transform);
            activeChunks.Add(newChunk);

            // Setup chunk
            CorridorChunk chunkScript = newChunk.GetComponent<CorridorChunk>();
            if (chunkScript != null)
            {
                chunkScript.Activate();

                // Spawn hazards
                int hazardCount = GetHazardCount();
                chunkScript.SpawnHazards(hazardPrefabs, hazardCount);

                // Spawn collectibles
                int collectibleCount = GetCollectibleCount();
                chunkScript.SpawnCollectibles(collectiblePrefabs, collectibleCount);

                // Spawn doors
                if (doorPrefab != null)
                {
                    GameObject[] doors = chunkScript.SpawnDoors(doorPrefab, doorsPerChunk);

                    // Setup door events
                    if (doors != null)
                    {
                        foreach (var door in doors)
                        {
                            if (door != null)
                            {
                                var doorHandler = door.GetComponent<DoorHandler>();
                                if (doorHandler != null)
                                {
                                    doorHandler.OnDoorChosen += OnDoorChosen;
                                }
                            }
                        }
                    }
                }

                // Update next spawn position
                nextSpawnPosition = chunkScript.EndPosition;
            }
            else
            {
                // Fallback if no chunk script
                nextSpawnPosition += Vector3.right * 20f;
            }

            chunksGenerated++;
            OnChunkGenerated?.Invoke(chunksGenerated);

            Debug.Log($"Generated chunk #{chunksGenerated} at {nextSpawnPosition}");
        }

        /// <summary>
        /// Despawns chunks that are too far behind player
        /// </summary>
        private void DespawnOldChunks()
        {
            if (playerTransform == null || activeChunks.Count <= chunksAhead) return;

            float playerX = playerTransform.position.x;
            List<GameObject> chunksToRemove = new List<GameObject>();

            foreach (var chunk in activeChunks)
            {
                if (chunk == null) continue;

                float chunkX = chunk.transform.position.x;
                float distanceBehind = playerX - chunkX;

                // Despawn if too far behind
                if (distanceBehind > (chunksBehind * 30f))
                {
                    chunksToRemove.Add(chunk);
                }
            }

            // Remove chunks
            foreach (var chunk in chunksToRemove)
            {
                activeChunks.Remove(chunk);
                Destroy(chunk);
            }
        }

        /// <summary>
        /// Called when player chooses a door
        /// </summary>
        private void OnDoorChosen(int doorIndex, DoorHandler.DoorType doorType)
        {
            Debug.Log($"Player chose door {doorIndex} - Type: {doorType}");

            // Adjust difficulty based on door type
            float difficultyMod = 1f;
            switch (doorType)
            {
                case DoorHandler.DoorType.Easy:
                    difficultyMod = 0.7f;
                    break;
                case DoorHandler.DoorType.Hard:
                    difficultyMod = 1.5f;
                    break;
                case DoorHandler.DoorType.Mystery:
                    difficultyMod = Random.Range(0.5f, 2f);
                    break;
            }

            currentDifficultyMultiplier *= difficultyMod;
        }

        /// <summary>
        /// Gets current corridor set based on difficulty
        /// </summary>
        private CorridorSet GetCurrentCorridorSet()
        {
            if (corridorSets == null || corridorSets.Length == 0)
            {
                Debug.LogError("No corridor sets assigned!");
                return null;
            }

            // Return set based on current index
            currentSetIndex = Mathf.Clamp(currentSetIndex, 0, corridorSets.Length - 1);
            return corridorSets[currentSetIndex];
        }

        /// <summary>
        /// Updates difficulty based on distance
        /// </summary>
        private void UpdateDifficulty()
        {
            int newSetIndex = Mathf.FloorToInt(totalDistanceTraveled / distancePerDifficultyIncrease);
            newSetIndex = Mathf.Clamp(newSetIndex, 0, corridorSets.Length - 1);

            if (newSetIndex != currentSetIndex)
            {
                currentSetIndex = newSetIndex;
                currentDifficultyMultiplier += 0.2f;
                OnDifficultyIncreased?.Invoke(currentDifficultyMultiplier);

                Debug.Log($"Difficulty increased! New level: {currentSetIndex}, Multiplier: {currentDifficultyMultiplier}");
            }
        }

        /// <summary>
        /// Gets hazard count based on difficulty
        /// </summary>
        private int GetHazardCount()
        {
            CorridorSet currentSet = GetCurrentCorridorSet();
            if (currentSet != null && currentSet.difficultyProfile != null)
            {
                return Mathf.RoundToInt(currentSet.difficultyProfile.hazardsPerChunk * currentDifficultyMultiplier);
            }

            return 2;
        }

        /// <summary>
        /// Gets collectible count based on difficulty
        /// </summary>
        private int GetCollectibleCount()
        {
            CorridorSet currentSet = GetCurrentCorridorSet();
            if (currentSet != null && currentSet.difficultyProfile != null)
            {
                return currentSet.difficultyProfile.soulShardsPerChunk;
            }

            return 2;
        }

        /// <summary>
        /// Resets corridor manager for new game
        /// </summary>
        public void ResetLevel()
        {
            // Clear all chunks
            foreach (var chunk in activeChunks)
            {
                if (chunk != null) Destroy(chunk);
            }

            activeChunks.Clear();
            nextSpawnPosition = Vector3.zero;
            totalDistanceTraveled = 0f;
            chunksGenerated = 0;
            currentSetIndex = 0;
            currentDifficultyMultiplier = 1f;

            // Generate initial chunks
            for (int i = 0; i < chunksAhead; i++)
            {
                GenerateNextChunk();
            }
        }
    }
}
