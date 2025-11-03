using UnityEngine;
using System.Collections.Generic;

namespace InfinityHouse.Data
{
    /// <summary>
    /// Obstacle Set for biome-specific obstacles in INFINITE HAUS v5.9.
    /// Weighted list of obstacle prefabs with gap ranges.
    /// Modulare Struktur für Einsteiger.
    /// </summary>
    [CreateAssetMenu(fileName = "IH_ObstacleSet", menuName = "Infinity House/Obstacle Set")]
    public class IH_ObstacleSet : ScriptableObject
    {
        [System.Serializable]
        public class ObstacleEntry
        {
            [Tooltip("Obstacle prefab")]
            public GameObject prefab;

            [Tooltip("Spawn weight (higher = more common)")]
            [Range(0.1f, 10f)]
            public float weight = 1f;

            [Tooltip("Minimum gap before this obstacle (meters)")]
            [Range(2f, 15f)]
            public float gapMin = 3f;

            [Tooltip("Maximum gap before this obstacle (meters)")]
            [Range(2f, 15f)]
            public float gapMax = 6f;

            [Tooltip("Difficulty requirement (0-1, 0=easy)")]
            [Range(0f, 1f)]
            public float difficultyMin = 0f;
        }

        [Header("Obstacle Entries")]
        [Tooltip("List of obstacles with weights")]
        public List<ObstacleEntry> obstacles = new List<ObstacleEntry>();

        [Header("Set Configuration")]
        [Tooltip("Set name")]
        public string setName = "Default Set";

        [Tooltip("Enable hazard combinations")]
        public bool allowCombinations = false;

        [Tooltip("Combination chance (0-1)")]
        [Range(0f, 0.5f)]
        public float combinationChance = 0.1f;

        #region Selection Logic
        /// <summary>
        /// Selects a random obstacle based on weights and difficulty
        /// </summary>
        public ObstacleEntry SelectObstacle(float difficulty)
        {
            if (obstacles == null || obstacles.Count == 0)
                return null;

            // Filter by difficulty
            List<ObstacleEntry> validObstacles = new List<ObstacleEntry>();
            float totalWeight = 0f;

            foreach (var entry in obstacles)
            {
                if (entry.difficultyMin <= difficulty && entry.prefab != null)
                {
                    validObstacles.Add(entry);
                    totalWeight += entry.weight;
                }
            }

            if (validObstacles.Count == 0)
                return null;

            // Weighted random selection
            float random = Random.Range(0f, totalWeight);
            float cumulative = 0f;

            foreach (var entry in validObstacles)
            {
                cumulative += entry.weight;
                if (random <= cumulative)
                {
                    return entry;
                }
            }

            return validObstacles[validObstacles.Count - 1];
        }

        /// <summary>
        /// Gets random gap distance for an obstacle
        /// </summary>
        public float GetRandomGap(ObstacleEntry entry)
        {
            if (entry == null)
                return 5f;

            return Random.Range(entry.gapMin, entry.gapMax);
        }

        /// <summary>
        /// Checks if combination should spawn
        /// </summary>
        public bool ShouldSpawnCombination()
        {
            return allowCombinations && Random.value < combinationChance;
        }
        #endregion

        #region Validation
        private void OnValidate()
        {
            // Remove null entries
            obstacles.RemoveAll(e => e.prefab == null);

            // Ensure min <= max for gaps
            foreach (var entry in obstacles)
            {
                if (entry.gapMin > entry.gapMax)
                {
                    entry.gapMax = entry.gapMin;
                }
            }
        }
        #endregion
    }
}
