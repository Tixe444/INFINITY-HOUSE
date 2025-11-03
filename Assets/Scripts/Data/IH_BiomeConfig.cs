using UnityEngine;
using UnityEngine.AddressableAssets;

namespace InfinityHouse.Data
{
    /// <summary>
    /// Biome Configuration ScriptableObject for INFINITY HOUSE v6.0.
    /// Defines biome-specific parameters: visuals, movement feel, obstacles, difficulty.
    /// v6.0: Added parameter delta validation (max 30% shift per transition)
    /// Modulare Struktur für Einsteiger - alle Biome-Settings an einem Ort.
    /// </summary>
    [CreateAssetMenu(fileName = "IH_BiomeConfig", menuName = "Infinity House/Biome Config")]
    public class IH_BiomeConfig : ScriptableObject
    {
        #region Identity
        [Header("Biome Identity")]
        [Tooltip("Unique biome ID (e.g., 'dark_pixel', 'neon_city')")]
        public string biomeID = "dark_pixel";

        [Tooltip("Display name")]
        public string biomeName = "Dark Pixel";

        [Tooltip("Biome description")]
        [TextArea(2, 4)]
        public string description = "A dark, pixelated corridor filled with danger.";
        #endregion

        #region Visuals
        [Header("Visuals & Audio")]
        [Tooltip("Background color")]
        public Color backgroundColor = Color.black;

        [Tooltip("Music track reference (Addressable)")]
        public AssetReferenceT<AudioClip> musicRef;

        [Tooltip("Parallax layer set (Addressable)")]
        public AssetReference parallaxSetRef;

        [Tooltip("Ambient SFX")]
        public AssetReferenceT<AudioClip> ambientSFX;

        [Tooltip("Lighting intensity (0-1)")]
        [Range(0f, 1f)]
        public float lightingIntensity = 0.5f;
        #endregion

        #region Obstacles
        [Header("Obstacles & Hazards")]
        [Tooltip("Obstacle set for this biome")]
        public IH_ObstacleSet obstacleSet;

        [Tooltip("Shard frequency (shards per 10m)")]
        [Range(0f, 10f)]
        public float shardFrequency = 3f;
        #endregion

        #region Movement Parameters
        [Header("Movement Feel Parameters")]
        [Tooltip("Speed multiplier (0.8-1.5)")]
        [Range(0.8f, 1.5f)]
        public float speedMultMin = 1.0f;

        [Tooltip("Speed multiplier max")]
        [Range(0.8f, 1.5f)]
        public float speedMultMax = 1.2f;

        [Tooltip("Gravity multiplier (0.8-1.5)")]
        [Range(0.8f, 1.5f)]
        public float gravityMult = 1.0f;

        [Tooltip("Air control multiplier (0.3-1.0)")]
        [Range(0.3f, 1.0f)]
        public float airControlMult = 0.5f;

        [Tooltip("Ground friction multiplier (0.5-2.0)")]
        [Range(0.5f, 2.0f)]
        public float frictionMult = 1.0f;

        [Tooltip("Coyote time in ms (80-120)")]
        [Range(80f, 120f)]
        public float jumpCoyoteMs = 90f;

        [Tooltip("Jump buffer in ms (80-120)")]
        [Range(80f, 120f)]
        public float jumpBufferMs = 80f;
        #endregion

        #region Transition
        [Header("Transition Settings")]
        [Tooltip("Transition duration (0.6-1.2s)")]
        [Range(0.6f, 1.2f)]
        public float transitionDurationS = 0.8f;

        [Tooltip("Light blend curve")]
        public AnimationCurve lightBlendCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("Transition SFX")]
        public AssetReferenceT<AudioClip> transitionSFX;

        [Tooltip("Use smooth parameter interpolation")]
        public bool useSmoothParamInterpolation = true;
        #endregion

        #region Difficulty
        [Header("Difficulty Modifiers")]
        [Tooltip("Base difficulty (1.0 = normal)")]
        [Range(0.5f, 2.0f)]
        public float baseDifficulty = 1.0f;

        [Tooltip("Difficulty scaling per floor")]
        [Range(0f, 0.2f)]
        public float difficultyScalingPerFloor = 0.05f;
        #endregion

        #region Helper Methods
        /// <summary>
        /// Gets movement parameters as struct
        /// </summary>
        public IH_MoveParams GetMoveParams()
        {
            return new IH_MoveParams
            {
                gravityMult = gravityMult,
                airControlMult = airControlMult,
                frictionMult = frictionMult,
                speedMult = Random.Range(speedMultMin, speedMultMax),
                coyoteTimeMs = jumpCoyoteMs,
                jumpBufferMs = jumpBufferMs
            };
        }

        /// <summary>
        /// Validates biome configuration (v6.0: Added parameter constraints)
        /// </summary>
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(biomeID))
            {
                biomeID = name.ToLower().Replace(" ", "_");
            }

            // Ensure min <= max for speed multipliers
            if (speedMultMin > speedMultMax)
            {
                speedMultMax = speedMultMin;
            }

            // v6.0: Clamp parameters to safe ranges (prevent extreme shifts)
            gravityMult = Mathf.Clamp(gravityMult, 0.75f, 1.25f); // ±25% max
            airControlMult = Mathf.Clamp(airControlMult, 0.35f, 0.8f); // Safe range
            frictionMult = Mathf.Clamp(frictionMult, 0.75f, 1.25f); // ±25% max
            speedMultMin = Mathf.Clamp(speedMultMin, 0.9f, 1.3f); // ±30% max
            speedMultMax = Mathf.Clamp(speedMultMax, 0.9f, 1.3f);
            jumpCoyoteMs = Mathf.Clamp(jumpCoyoteMs, 80f, 120f); // Android/iOS precision
            jumpBufferMs = Mathf.Clamp(jumpBufferMs, 75f, 110f);

            // Transition duration safe range
            transitionDurationS = Mathf.Clamp(transitionDurationS, 0.6f, 1.2f);
        }
        #endregion
    }

    /// <summary>
    /// Movement parameters struct (value type, no GC)
    /// </summary>
    [System.Serializable]
    public struct IH_MoveParams
    {
        public float gravityMult;
        public float airControlMult;
        public float frictionMult;
        public float speedMult;
        public float coyoteTimeMs;
        public float jumpBufferMs;

        /// <summary>
        /// Lerps between two param sets
        /// </summary>
        public static IH_MoveParams Lerp(IH_MoveParams a, IH_MoveParams b, float t)
        {
            return new IH_MoveParams
            {
                gravityMult = Mathf.Lerp(a.gravityMult, b.gravityMult, t),
                airControlMult = Mathf.Lerp(a.airControlMult, b.airControlMult, t),
                frictionMult = Mathf.Lerp(a.frictionMult, b.frictionMult, t),
                speedMult = Mathf.Lerp(a.speedMult, b.speedMult, t),
                coyoteTimeMs = Mathf.Lerp(a.coyoteTimeMs, b.coyoteTimeMs, t),
                jumpBufferMs = Mathf.Lerp(a.jumpBufferMs, b.jumpBufferMs, t)
            };
        }
    }
}
