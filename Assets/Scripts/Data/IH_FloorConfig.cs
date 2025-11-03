using UnityEngine;
using System.Collections.Generic;

namespace InfinityHouse.Data
{
    /// <summary>
    /// Floor Configuration for INFINITE HAUS v5.9.
    /// Manages floor range, biome candidates, difficulty curves, and speed scaling.
    /// Modulare Struktur für Einsteiger.
    /// </summary>
    [CreateAssetMenu(fileName = "IH_FloorConfig", menuName = "Infinity House/Floor Config")]
    public class IH_FloorConfig : ScriptableObject
    {
        [System.Serializable]
        public class BiomeCandidate
        {
            [Tooltip("Biome reference")]
            public IH_BiomeConfig biomeRef;

            [Tooltip("Base selection weight")]
            [Range(0.1f, 10f)]
            public float weight = 1f;

            [Tooltip("Left door bias (-1 to 1, negative favors right)")]
            [Range(-1f, 1f)]
            public float leftDoorBias = 0f;

            [Tooltip("Right door bias (-1 to 1, negative favors left)")]
            [Range(-1f, 1f)]
            public float rightDoorBias = 0f;

            [Tooltip("Minimum floor to enable this biome")]
            public int minFloor = 0;

            [Tooltip("Maximum floor (0 = unlimited)")]
            public int maxFloor = 0;
        }

        #region Floor Range
        [Header("Floor Range")]
        [Tooltip("Minimum floor number")]
        public int floorMin = 1;

        [Tooltip("Maximum floor number (0 = infinite)")]
        public int floorMax = 0;
        #endregion

        #region Biomes
        [Header("Biome Candidates")]
        [Tooltip("Available biomes for this floor range")]
        public List<BiomeCandidate> candidateBiomes = new List<BiomeCandidate>();
        #endregion

        #region Difficulty
        [Header("Difficulty Curve")]
        [Tooltip("Difficulty scaling curve (X=floor, Y=difficulty 0-1)")]
        public AnimationCurve difficultyCurve = AnimationCurve.Linear(0f, 0f, 100f, 1f);

        [Tooltip("Base difficulty multiplier")]
        [Range(0.5f, 2f)]
        public float baseDifficultyMult = 1f;
        #endregion

        #region Speed
        [Header("Run Speed Curve")]
        [Tooltip("Speed scaling curve (X=floor, Y=speed mult)")]
        public AnimationCurve runSpeedCurve = AnimationCurve.Linear(0f, 1f, 100f, 1.5f);

        [Tooltip("Minimum speed multiplier")]
        [Range(0.8f, 1.5f)]
        public float minSpeedMult = 0.9f;

        [Tooltip("Maximum speed multiplier")]
        [Range(0.8f, 2f)]
        public float maxSpeedMult = 1.8f;
        #endregion

        #region Selection Logic
        /// <summary>
        /// Selects a biome for the given floor and door bias
        /// </summary>
        /// <param name="floor">Current floor number</param>
        /// <param name="doorBias">Door choice bias (-1=left, 1=right)</param>
        public IH_BiomeConfig SelectBiome(int floor, float doorBias)
        {
            if (candidateBiomes == null || candidateBiomes.Count == 0)
                return null;

            // Filter valid biomes for this floor
            List<BiomeCandidate> validBiomes = new List<BiomeCandidate>();
            float totalWeight = 0f;

            foreach (var candidate in candidateBiomes)
            {
                if (candidate.biomeRef == null)
                    continue;

                // Check floor range
                if (candidate.minFloor > floor)
                    continue;
                if (candidate.maxFloor > 0 && candidate.maxFloor < floor)
                    continue;

                // Apply door bias to weight
                float biasModifier = 1f;
                if (doorBias < 0f) // Left door chosen
                {
                    biasModifier += candidate.leftDoorBias;
                }
                else if (doorBias > 0f) // Right door chosen
                {
                    biasModifier += candidate.rightDoorBias;
                }

                float adjustedWeight = candidate.weight * Mathf.Max(0.1f, biasModifier);

                validBiomes.Add(candidate);
                totalWeight += adjustedWeight;
            }

            if (validBiomes.Count == 0)
                return null;

            // Weighted random selection
            float random = Random.Range(0f, totalWeight);
            float cumulative = 0f;

            foreach (var candidate in validBiomes)
            {
                float biasModifier = 1f;
                if (doorBias < 0f)
                    biasModifier += candidate.leftDoorBias;
                else if (doorBias > 0f)
                    biasModifier += candidate.rightDoorBias;

                float adjustedWeight = candidate.weight * Mathf.Max(0.1f, biasModifier);
                cumulative += adjustedWeight;

                if (random <= cumulative)
                {
                    return candidate.biomeRef;
                }
            }

            return validBiomes[validBiomes.Count - 1].biomeRef;
        }

        /// <summary>
        /// Gets difficulty for a given floor
        /// </summary>
        public float GetDifficulty(int floor)
        {
            return difficultyCurve.Evaluate(floor) * baseDifficultyMult;
        }

        /// <summary>
        /// Gets speed multiplier for a given floor
        /// </summary>
        public float GetSpeedMultiplier(int floor)
        {
            float speedMult = runSpeedCurve.Evaluate(floor);
            return Mathf.Clamp(speedMult, minSpeedMult, maxSpeedMult);
        }

        /// <summary>
        /// Checks if floor is in range
        /// </summary>
        public bool IsFloorInRange(int floor)
        {
            if (floor < floorMin)
                return false;
            if (floorMax > 0 && floor > floorMax)
                return false;
            return true;
        }
        #endregion

        #region Validation
        private void OnValidate()
        {
            // Remove null biomes
            candidateBiomes.RemoveAll(c => c.biomeRef == null);

            // Ensure min <= max
            if (floorMax > 0 && floorMin > floorMax)
            {
                floorMax = floorMin;
            }
        }
        #endregion
    }
}
