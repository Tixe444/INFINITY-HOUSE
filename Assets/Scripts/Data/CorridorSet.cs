using UnityEngine;

namespace InfiniteHaus.Data
{
    /// <summary>
    /// ScriptableObject that holds a collection of corridor chunk prefabs for a specific difficulty tier.
    /// Allows level designers to create themed corridor variations.
    /// </summary>
    [CreateAssetMenu(fileName = "New Corridor Set", menuName = "Infinite Haus/Corridor Set")]
    public class CorridorSet : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Name of this corridor set")]
        public string setName = "Easy Corridors";

        [Tooltip("Associated difficulty profile")]
        public DifficultyProfile difficultyProfile;

        [Header("Corridor Prefabs")]
        [Tooltip("Array of corridor chunk prefabs to randomly select from")]
        public GameObject[] corridorPrefabs;

        [Tooltip("Weights for each corridor (higher = more likely). Leave empty for equal weights.")]
        public float[] spawnWeights;

        [Header("Special Corridors")]
        [Tooltip("Boss/challenge corridor prefabs (rare spawns)")]
        public GameObject[] specialCorridors;

        [Tooltip("Chance of spawning a special corridor (0-1)")]
        [Range(0f, 1f)]
        public float specialCorridorChance = 0.1f;

        [Header("Corridor Metadata")]
        [Tooltip("Average length of corridors in this set (Unity units)")]
        public float averageLength = 20f;

        [Tooltip("Minimum safe spacing between hazards")]
        public float hazardSpacing = 3f;

        /// <summary>
        /// Returns a random corridor prefab based on weights
        /// </summary>
        public GameObject GetRandomCorridor(bool allowSpecial = true)
        {
            // Check for special corridor spawn
            if (allowSpecial && specialCorridors != null && specialCorridors.Length > 0)
            {
                if (Random.value < specialCorridorChance)
                {
                    return specialCorridors[Random.Range(0, specialCorridors.Length)];
                }
            }

            if (corridorPrefabs == null || corridorPrefabs.Length == 0)
            {
                Debug.LogError($"CorridorSet '{setName}' has no corridor prefabs!");
                return null;
            }

            // Use weighted selection if weights are provided
            if (spawnWeights != null && spawnWeights.Length == corridorPrefabs.Length)
            {
                float totalWeight = 0f;
                foreach (float weight in spawnWeights)
                {
                    totalWeight += weight;
                }

                float randomValue = Random.value * totalWeight;
                float cumulativeWeight = 0f;

                for (int i = 0; i < corridorPrefabs.Length; i++)
                {
                    cumulativeWeight += spawnWeights[i];
                    if (randomValue <= cumulativeWeight)
                    {
                        return corridorPrefabs[i];
                    }
                }
            }

            // Fallback to random selection
            return corridorPrefabs[Random.Range(0, corridorPrefabs.Length)];
        }

        /// <summary>
        /// Validates the corridor set configuration
        /// </summary>
        private void OnValidate()
        {
            if (spawnWeights != null && spawnWeights.Length > 0)
            {
                if (spawnWeights.Length != corridorPrefabs.Length)
                {
                    Debug.LogWarning($"CorridorSet '{setName}': Spawn weights count ({spawnWeights.Length}) doesn't match corridor prefabs count ({corridorPrefabs.Length})");
                }
            }
        }
    }
}
