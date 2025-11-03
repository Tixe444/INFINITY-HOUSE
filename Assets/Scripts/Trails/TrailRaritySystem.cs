// ═══════════════════════════════════════════════════════════════════════════════
// INFINITY HOUSE v5.6 - Made by Mate Makovics
// Trail Rarity System - Unique Progression Framework
// ═══════════════════════════════════════════════════════════════════════════════

using UnityEngine;
using System;
using System.Collections.Generic;

namespace InfinityHouse.Trails
{
    /// <summary>
    /// Custom rarity system for Trail cosmetics.
    /// 5-Tier progression: Whisper → Echo → Resonance → Radiance → Transcendence
    /// 8 Trail Families with visual evolution per tier.
    /// Integrated with existing economy, separate from skin rarities.
    /// </summary>
    public class TrailRaritySystem : MonoBehaviour
    {
        public static TrailRaritySystem Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private TextAsset trailConfigJson;

        // Rarity definitions
        private Dictionary<TrailRarity, RarityData> rarityData;
        private Dictionary<TrailFamily, FamilyData> familyData;

        // Economy values
        public enum TrailRarity
        {
            Whisper = 0,        // 40% drop - Subtle, single-layer
            Echo = 1,           // 30% drop - Visible, simple animation
            Resonance = 2,      // 18% drop - Strong, multi-layer
            Radiance = 3,       // 9% drop  - Glowing, particle accents
            Transcendence = 4   // 3% drop  - Reality-bending, max effects
        }

        public enum TrailFamily
        {
            Ghost,          // Transparent wisps, fading opacity
            Ember,          // Fire particles, heat distortion
            Glitch,         // Digital artifacts, scanlines
            Ectoplasm,      // Slime drips, viscous flow
            Lightning,      // Electric arcs, spark bursts
            Rune,           // Floating symbols, arcane glyphs
            Afterimage,     // Speed clones, motion blur
            PixelFray       // Retro pixelation, bit degradation
        }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            InitializeRarities();
            InitializeFamilies();
        }

        #region Initialization

        private void InitializeRarities()
        {
            rarityData = new Dictionary<TrailRarity, RarityData>
            {
                [TrailRarity.Whisper] = new RarityData
                {
                    tier = TrailRarity.Whisper,
                    displayName = "Whisper",
                    dropRate = 0.40f,
                    dustValue = 5,
                    craftCost = 0,
                    diamondCost = 50,
                    segmentCount = 16,
                    widthMultiplier = 0.8f,
                    intensityMultiplier = 0.6f,
                    particleCount = 0,
                    glowStrength = 0f,
                    color = new Color(0.7f, 0.7f, 0.8f, 0.5f)
                },

                [TrailRarity.Echo] = new RarityData
                {
                    tier = TrailRarity.Echo,
                    displayName = "Echo",
                    dropRate = 0.30f,
                    dustValue = 15,
                    craftCost = 50,
                    diamondCost = 150,
                    segmentCount = 20,
                    widthMultiplier = 1.0f,
                    intensityMultiplier = 0.8f,
                    particleCount = 2,
                    glowStrength = 0.2f,
                    color = new Color(0.6f, 0.8f, 1f, 0.7f)
                },

                [TrailRarity.Resonance] = new RarityData
                {
                    tier = TrailRarity.Resonance,
                    displayName = "Resonance",
                    dropRate = 0.18f,
                    dustValue = 40,
                    craftCost = 150,
                    diamondCost = 400,
                    segmentCount = 24,
                    widthMultiplier = 1.2f,
                    intensityMultiplier = 1.0f,
                    particleCount = 4,
                    glowStrength = 0.5f,
                    color = new Color(0.9f, 0.6f, 1f, 0.85f)
                },

                [TrailRarity.Radiance] = new RarityData
                {
                    tier = TrailRarity.Radiance,
                    displayName = "Radiance",
                    dropRate = 0.09f,
                    dustValue = 120,
                    craftCost = 500,
                    diamondCost = 1000,
                    segmentCount = 28,
                    widthMultiplier = 1.4f,
                    intensityMultiplier = 1.3f,
                    particleCount = 6,
                    glowStrength = 0.8f,
                    color = new Color(1f, 0.9f, 0.4f, 1f)
                },

                [TrailRarity.Transcendence] = new RarityData
                {
                    tier = TrailRarity.Transcendence,
                    displayName = "Transcendence",
                    dropRate = 0.03f,
                    dustValue = 500,
                    craftCost = 2000,
                    diamondCost = 2500,
                    segmentCount = 32,
                    widthMultiplier = 1.6f,
                    intensityMultiplier = 1.6f,
                    particleCount = 10,
                    glowStrength = 1.2f,
                    color = new Color(0.5f, 1f, 1f, 1f)
                }
            };

            Debug.Log("[TrailRaritySystem] Initialized 5 rarity tiers");
        }

        private void InitializeFamilies()
        {
            familyData = new Dictionary<TrailFamily, FamilyData>
            {
                [TrailFamily.Ghost] = new FamilyData
                {
                    family = TrailFamily.Ghost,
                    displayName = "Ghost",
                    description = "Ethereal wisps that fade between dimensions",
                    baseColor = new Color(0.8f, 0.9f, 1f),
                    secondaryColor = new Color(0.5f, 0.6f, 0.8f),
                    animationSpeed = 1.2f,
                    theme = "Cartoon Spooky"
                },

                [TrailFamily.Ember] = new FamilyData
                {
                    family = TrailFamily.Ember,
                    displayName = "Ember",
                    description = "Burning embers with heat distortion",
                    baseColor = new Color(1f, 0.5f, 0.2f),
                    secondaryColor = new Color(1f, 0.8f, 0.1f),
                    animationSpeed = 2.0f,
                    theme = "Cartoon Spooky"
                },

                [TrailFamily.Glitch] = new FamilyData
                {
                    family = TrailFamily.Glitch,
                    displayName = "Glitch",
                    description = "Digital artifacts and reality tears",
                    baseColor = new Color(0f, 1f, 0.5f),
                    secondaryColor = new Color(1f, 0f, 0.5f),
                    animationSpeed = 3.0f,
                    theme = "Digital Horror"
                },

                [TrailFamily.Ectoplasm] = new FamilyData
                {
                    family = TrailFamily.Ectoplasm,
                    displayName = "Ectoplasm",
                    description = "Viscous ghostly substance with drips",
                    baseColor = new Color(0.3f, 1f, 0.3f),
                    secondaryColor = new Color(0.1f, 0.6f, 0.1f),
                    animationSpeed = 0.8f,
                    theme = "Cartoon Spooky"
                },

                [TrailFamily.Lightning] = new FamilyData
                {
                    family = TrailFamily.Lightning,
                    displayName = "Lightning",
                    description = "Electric arcs with spark bursts",
                    baseColor = new Color(0.5f, 0.5f, 1f),
                    secondaryColor = new Color(1f, 1f, 1f),
                    animationSpeed = 4.0f,
                    theme = "Electro Storm"
                },

                [TrailFamily.Rune] = new FamilyData
                {
                    family = TrailFamily.Rune,
                    displayName = "Rune",
                    description = "Arcane symbols with mystical energy",
                    baseColor = new Color(0.8f, 0.3f, 1f),
                    secondaryColor = new Color(1f, 0.7f, 1f),
                    animationSpeed = 1.0f,
                    theme = "Mystic Arcane"
                },

                [TrailFamily.Afterimage] = new FamilyData
                {
                    family = TrailFamily.Afterimage,
                    displayName = "Afterimage",
                    description = "Speed clones with motion blur",
                    baseColor = new Color(0.6f, 0.6f, 1f),
                    secondaryColor = new Color(0.3f, 0.3f, 0.6f),
                    animationSpeed = 5.0f,
                    theme = "Speed Force"
                },

                [TrailFamily.PixelFray] = new FamilyData
                {
                    family = TrailFamily.PixelFray,
                    displayName = "Pixel Fray",
                    description = "Retro pixelation with bit degradation",
                    baseColor = new Color(1f, 0f, 1f),
                    secondaryColor = new Color(0f, 1f, 1f),
                    animationSpeed = 2.5f,
                    theme = "Retro Digital"
                }
            };

            Debug.Log("[TrailRaritySystem] Initialized 8 trail families");
        }

        #endregion

        #region Query Methods

        public RarityData GetRarityData(TrailRarity rarity)
        {
            return rarityData.ContainsKey(rarity) ? rarityData[rarity] : null;
        }

        public FamilyData GetFamilyData(TrailFamily family)
        {
            return familyData.ContainsKey(family) ? familyData[family] : null;
        }

        public TrailRarity RollRarity()
        {
            float roll = UnityEngine.Random.value;
            float cumulative = 0f;

            // Roll from highest to lowest for excitement
            cumulative += rarityData[TrailRarity.Transcendence].dropRate;
            if (roll <= cumulative) return TrailRarity.Transcendence;

            cumulative += rarityData[TrailRarity.Radiance].dropRate;
            if (roll <= cumulative) return TrailRarity.Radiance;

            cumulative += rarityData[TrailRarity.Resonance].dropRate;
            if (roll <= cumulative) return TrailRarity.Resonance;

            cumulative += rarityData[TrailRarity.Echo].dropRate;
            if (roll <= cumulative) return TrailRarity.Echo;

            return TrailRarity.Whisper;
        }

        public Color GetRarityColor(TrailRarity rarity)
        {
            return rarityData.ContainsKey(rarity) ? rarityData[rarity].color : Color.white;
        }

        public int GetDustValue(TrailRarity rarity)
        {
            return rarityData.ContainsKey(rarity) ? rarityData[rarity].dustValue : 0;
        }

        #endregion

        #region Data Structures

        [Serializable]
        public class RarityData
        {
            public TrailRarity tier;
            public string displayName;
            public float dropRate;
            public int dustValue;          // Duplicate conversion
            public int craftCost;          // Dust cost to craft
            public int diamondCost;        // Direct purchase
            public int segmentCount;       // Trail segments
            public float widthMultiplier;  // Visual width
            public float intensityMultiplier; // Effect intensity
            public int particleCount;      // Extra particles
            public float glowStrength;     // Glow effect
            public Color color;            // UI color
        }

        [Serializable]
        public class FamilyData
        {
            public TrailFamily family;
            public string displayName;
            public string description;
            public Color baseColor;
            public Color secondaryColor;
            public float animationSpeed;
            public string theme;
        }

        #endregion
    }
}
