using UnityEngine;

namespace InfiniteHaus.Shop
{
    /// <summary>
    /// ScriptableObject defining a purchasable cosmetic item.
    /// Supports multiple cosmetic types (skins, trails, UI themes, music, filters).
    /// </summary>
    [CreateAssetMenu(fileName = "New Cosmetic", menuName = "Infinite Haus/Shop/Cosmetic Item")]
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
        /// Rarity tiers for visual presentation
        /// </summary>
        public enum RarityTier
        {
            Common,      // White/Gray
            Rare,        // Blue
            Epic,        // Purple
            Legendary,   // Gold
            Mythic       // Red/Rainbow
        }

        /// <summary>
        /// Gets color associated with rarity tier
        /// </summary>
        public Color GetRarityColor()
        {
            switch (rarity)
            {
                case RarityTier.Common:
                    return new Color(0.7f, 0.7f, 0.7f); // Gray
                case RarityTier.Rare:
                    return new Color(0.2f, 0.5f, 1f); // Blue
                case RarityTier.Epic:
                    return new Color(0.7f, 0.3f, 1f); // Purple
                case RarityTier.Legendary:
                    return new Color(1f, 0.8f, 0.2f); // Gold
                case RarityTier.Mythic:
                    return new Color(1f, 0.2f, 0.4f); // Red
                default:
                    return Color.white;
            }
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
