using UnityEngine;

namespace InfiniteHaus.Data
{
    /// <summary>
    /// ScriptableObject that defines difficulty parameters for different stages of the game.
    /// Allows designers to tune challenge progression without code changes.
    /// </summary>
    [CreateAssetMenu(fileName = "New Difficulty Profile", menuName = "Infinite Haus/Difficulty Profile")]
    public class DifficultyProfile : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Name of this difficulty tier")]
        public string difficultyName = "Easy";

        [Tooltip("Minimum distance to trigger this difficulty")]
        public float minDistance = 0f;

        [Header("Movement & Speed")]
        [Tooltip("Player auto-run speed")]
        [Range(1f, 15f)]
        public float playerSpeed = 5f;

        [Tooltip("Scroll speed multiplier for environment")]
        [Range(0.5f, 3f)]
        public float scrollSpeedMultiplier = 1f;

        [Header("Chase Parameters")]
        [Tooltip("Base speed of The Shadow pursuer")]
        [Range(1f, 10f)]
        public float shadowBaseSpeed = 3f;

        [Tooltip("How quickly Chase Meter fills per second")]
        [Range(0f, 10f)]
        public float chaseMeterFillRate = 1f;

        [Tooltip("Chase Meter penalty for taking damage")]
        [Range(0f, 50f)]
        public float chasePenaltyOnDamage = 15f;

        [Tooltip("Frequency of Crawler ambushes (seconds)")]
        [Range(5f, 60f)]
        public float crawlerAmbushFrequency = 20f;

        [Tooltip("Chance of Mimic appearing as door (0-1)")]
        [Range(0f, 1f)]
        public float mimicSpawnChance = 0.1f;

        [Header("Hazard Density")]
        [Tooltip("Number of hazards per corridor chunk")]
        [Range(0, 10)]
        public int hazardsPerChunk = 2;

        [Tooltip("Chance of trap being active (0-1)")]
        [Range(0f, 1f)]
        public float trapActiveChance = 0.7f;

        [Header("Rewards & Balance")]
        [Tooltip("Soul Shard spawn rate per chunk")]
        [Range(0, 5)]
        public int soulShardsPerChunk = 2;

        [Tooltip("Chance of Rare Relic spawning (0-1)")]
        [Range(0f, 1f)]
        public float relicSpawnChance = 0.05f;

        [Tooltip("Chase Crystal spawn rate (per 5 chunks)")]
        [Range(0, 3)]
        public int chaseCrystalsPerSection = 1;

        [Header("Level Generation")]
        [Tooltip("Length of corridor chunks (Unity units)")]
        [Range(10f, 50f)]
        public float corridorLength = 20f;

        [Tooltip("Number of door choices at end of chunk")]
        [Range(2, 4)]
        public int doorChoiceCount = 2;

        [Header("Combat & Damage")]
        [Tooltip("Base damage from hazards")]
        [Range(1, 3)]
        public int baseDamage = 1;

        [Tooltip("Player invincibility duration after hit (seconds)")]
        [Range(0.3f, 2f)]
        public float invincibilityDuration = 0.8f;
    }
}
