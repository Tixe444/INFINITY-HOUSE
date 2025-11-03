using UnityEngine;
using System;

namespace InfinityHouse.Shop
{
    /// <summary>
    /// ScriptableObject defining a purchasable cosmetic item.
    /// Supports multiple cosmetic types (skins, trails, UI themes, music, filters).
    /// Includes fusion system support and condition-based quality.
    /// </summary>
    [CreateAssetMenu(fileName = "New Cosmetic", menuName = "Infinity House/Shop/Cosmetic Item")]
    public class CosmeticItem : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique ID for this cosmetic (must be unique)")]
        public string cosmeticID = "skin_001";

        [Tooltip("Display name shown in shop")]
        public string displayName = "Ghost Skin";

        [Tooltip("Description of the cosmetic")]
        [TextArea(2, 4)]
        public string description = "A spooky ghost character skin";

        [Header("Type & Category")]
        [Tooltip("Type of cosmetic")]
        public CosmeticType type = CosmeticType.PlayerSkin;

        [Tooltip("Rarity tier (affects visual presentation)")]
        public RarityTier rarity = RarityTier.Common;

        [Header("Pricing")]
        [Tooltip("Cost in Diamonds (premium currency)")]
        [Min(0)]
        public int diamondCost = 100;

        [Tooltip("Is this cosmetic unlockable for free?")]
        public bool isFreeUnlock = false;

        [Tooltip("Required player level to purchase (0 = no requirement)")]
        [Min(0)]
        public int requiredLevel = 0;

        [Header("Fusion & Condition System")]
        [Tooltip("Condition value (0.0-1.0): affects brightness and market value")]
        [Range(0f, 1f)]
        public float conditionValue = 0.5f;

        [Tooltip("Was this item created through fusion?")]
        public bool isFused = false;

        [Tooltip("When was this item fused (UTC)")]
        public DateTime fusionTimestamp;

        [Header("Visual Assets")]
        [Tooltip("Preview icon shown in shop")]
        public Sprite previewIcon;

        [Tooltip("Player skin sprite (for PlayerSkin type)")]
        public Sprite playerSkinSprite;

        [Tooltip("Trail effect prefab (for SoulTrail type)")]
        public GameObject trailEffectPrefab;

        [Tooltip("UI theme config (for UITheme type)")]
        public Data.ThemeConfig uiThemeConfig;

        [Tooltip("Background music clip (for BackgroundMusic type)")]
        public AudioClip backgroundMusicClip;

        [Tooltip("Screen filter material (for ScreenFilter type)")]
        public Material screenFilterMaterial;

        [Header("Animation")]
        [Tooltip("Animator override controller (for animated skins)")]
        public RuntimeAnimatorController animatorOverride;

        [Header("Special Features")]
        [Tooltip("Does this cosmetic have special effects?")]
        public bool hasSpecialEffects = false;

        [Tooltip("Special effect prefab (spawn particles, etc.)")]
        public GameObject specialEffectPrefab;

        [Tooltip("Custom shader for special visuals")]
        public Shader customShader;

        /// <summary>
        /// Cosmetic types supported by the shop system
        /// </summary>
        public enum CosmeticType
        {
            PlayerSkin,         // Character sprite/model
            SoulTrail,          // Trail effect behind player
            UITheme,            // HUD and UI visual theme
            BackgroundMusic,    // Alternate BGM
            ScreenFilter,       // Full-screen visual filter/shader
            DeathAnimation,     // Custom death animation
            JumpEffect,         // Custom jump particle effect
            LandingEffect       // Custom landing particle effect
        }

        /// <summary>
        /// Rarity tiers for visual presentation (6 tiers)
        /// </summary>
        public enum RarityTier
        {
            Common,      // Gray (55%)
            Rare,        // Blue (25%)
            Epic,        // Purple (12%)
            Legendary,   // Gold (6%)
            Mythic,      // Red with violet glow (1.5%)
            Exotic       // Iridescent black (0.1-0.2%)
        }

        /// <summary>
        /// Gets color associated with rarity tier
        /// </summary>
        public Color GetRarityColor()
        {
            switch (rarity)
            {
                case RarityTier.Common:
                    return new Color(0.5f, 0.5f, 0.5f); // Gray
                case RarityTier.Rare:
                    return new Color(0.25f, 0.41f, 0.88f); // Blue
                case RarityTier.Epic:
                    return new Color(0.58f, 0.44f, 0.86f); // Purple
                case RarityTier.Legendary:
                    return new Color(1f, 0.84f, 0f); // Gold
                case RarityTier.Mythic:
                    return new Color(0.86f, 0.08f, 0.24f); // Crimson
                case RarityTier.Exotic:
                    return Color.black; // Iridescent black
                default:
                    return Color.white;
            }
        }

        /// <summary>
        /// Gets condition display string
        /// </summary>
        public string GetConditionDisplay()
        {
            if (conditionValue <= 0.25f) return "Blessed";
            if (conditionValue <= 0.50f) return "Awakened";
            if (conditionValue <= 0.75f) return "Withered";
            return "Cursed";
        }

        /// <summary>
        /// Validates this cosmetic item
        /// </summary>
        private void OnValidate()
        {
            // Ensure ID is set
            if (string.IsNullOrEmpty(cosmeticID))
            {
                cosmeticID = name.ToLower().Replace(" ", "_");
            }

            // Warn if no preview icon
            if (previewIcon == null)
            {
                Debug.LogWarning($"CosmeticItem '{displayName}' has no preview icon assigned!");
            }

            // Type-specific validation
            switch (type)
            {
                case CosmeticType.PlayerSkin:
                    if (playerSkinSprite == null)
                        Debug.LogWarning($"{displayName}: PlayerSkin type requires playerSkinSprite!");
                    break;

                case CosmeticType.SoulTrail:
                    if (trailEffectPrefab == null)
                        Debug.LogWarning($"{displayName}: SoulTrail type requires trailEffectPrefab!");
                    break;

                case CosmeticType.UITheme:
                    if (uiThemeConfig == null)
                        Debug.LogWarning($"{displayName}: UITheme type requires uiThemeConfig!");
                    break;

                case CosmeticType.BackgroundMusic:
                    if (backgroundMusicClip == null)
                        Debug.LogWarning($"{displayName}: BackgroundMusic type requires backgroundMusicClip!");
                    break;

                case CosmeticType.ScreenFilter:
                    if (screenFilterMaterial == null)
                        Debug.LogWarning($"{displayName}: ScreenFilter type requires screenFilterMaterial!");
                    break;
            }
        }
    }
}
