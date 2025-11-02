using UnityEngine;
using UnityEngine.UI;
using InfiniteHaus.Data;

namespace InfiniteHaus.Theme
{
    /// <summary>
    /// Applies theme visuals to individual game objects.
    /// Attach to objects that should change appearance with theme.
    /// </summary>
    public class ThemeApplicator : MonoBehaviour
    {
        [Header("Apply Theme To")]
        [Tooltip("Apply theme to SpriteRenderer")]
        [SerializeField] private bool applyToSpriteRenderer = true;

        [Tooltip("Apply theme to UI Image")]
        [SerializeField] private bool applyToUIImage = false;

        [Tooltip("Apply theme to ParticleSystem")]
        [SerializeField] private bool applyToParticles = false;

        [Header("What to Apply")]
        [Tooltip("Use theme sprites")]
        [SerializeField] private bool useThemeSprites = true;

        [Tooltip("Use theme colors")]
        [SerializeField] private bool useThemeColors = true;

        [Tooltip("Sprite type from theme")]
        [SerializeField] private ThemeSpriteType spriteType = ThemeSpriteType.None;

        [Header("Components")]
        [Tooltip("SpriteRenderer to modify")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Tooltip("UI Image to modify")]
        [SerializeField] private Image uiImage;

        [Tooltip("ParticleSystem to modify")]
        [SerializeField] private ParticleSystem particleSystem;

        public enum ThemeSpriteType
        {
            None,
            BackgroundFar,
            BackgroundMid,
            BackgroundNear,
            Tileset,
            Door,
            DoorHighlight,
            Spike,
            Goo,
            CrumblePlatform,
            FallingCeiling,
            Lantern,
            HeartFull,
            HeartEmpty,
            FogOverlay
        }

        private void Awake()
        {
            // Auto-assign components if not set
            if (applyToSpriteRenderer && spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (applyToUIImage && uiImage == null)
            {
                uiImage = GetComponent<Image>();
            }

            if (applyToParticles && particleSystem == null)
            {
                particleSystem = GetComponent<ParticleSystem>();
            }
        }

        /// <summary>
        /// Applies theme configuration to this object
        /// </summary>
        public void ApplyTheme(ThemeConfig theme)
        {
            if (theme == null)
            {
                Debug.LogWarning("ThemeConfig is null!");
                return;
            }

            // Apply sprites
            if (useThemeSprites)
            {
                ApplyThemeSprite(theme);
            }

            // Apply colors
            if (useThemeColors)
            {
                ApplyThemeColor(theme);
            }

            // Apply particle colors
            if (applyToParticles)
            {
                ApplyParticleColors(theme);
            }
        }

        /// <summary>
        /// Applies sprite from theme based on type
        /// </summary>
        private void ApplyThemeSprite(ThemeConfig theme)
        {
            Sprite sprite = GetSpriteFromTheme(theme);

            if (sprite == null) return;

            if (applyToSpriteRenderer && spriteRenderer != null)
            {
                spriteRenderer.sprite = sprite;
            }

            if (applyToUIImage && uiImage != null)
            {
                uiImage.sprite = sprite;
            }
        }

        /// <summary>
        /// Gets appropriate sprite from theme config
        /// </summary>
        private Sprite GetSpriteFromTheme(ThemeConfig theme)
        {
            switch (spriteType)
            {
                case ThemeSpriteType.BackgroundFar:
                    return theme.backgroundFar;
                case ThemeSpriteType.BackgroundMid:
                    return theme.backgroundMid;
                case ThemeSpriteType.BackgroundNear:
                    return theme.backgroundNear;
                case ThemeSpriteType.Tileset:
                    return theme.tileset;
                case ThemeSpriteType.Door:
                    return theme.doorSprite;
                case ThemeSpriteType.DoorHighlight:
                    return theme.doorHighlightSprite;
                case ThemeSpriteType.Spike:
                    return theme.spikeSprite;
                case ThemeSpriteType.Goo:
                    return theme.gooSprite;
                case ThemeSpriteType.CrumblePlatform:
                    return theme.crumblePlatformSprite;
                case ThemeSpriteType.FallingCeiling:
                    return theme.fallingCeilingSprite;
                case ThemeSpriteType.Lantern:
                    return theme.lanternSprite;
                case ThemeSpriteType.HeartFull:
                    return theme.heartFullSprite;
                case ThemeSpriteType.HeartEmpty:
                    return theme.heartEmptySprite;
                case ThemeSpriteType.FogOverlay:
                    return theme.fogOverlay;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Applies color from theme
        /// </summary>
        private void ApplyThemeColor(ThemeConfig theme)
        {
            Color color = theme.primaryColor;

            if (applyToSpriteRenderer && spriteRenderer != null)
            {
                spriteRenderer.color = color;
            }

            if (applyToUIImage && uiImage != null)
            {
                // Use UI tint for UI elements
                uiImage.color = theme.uiTintColor;
            }
        }

        /// <summary>
        /// Applies theme colors to particle system
        /// </summary>
        private void ApplyParticleColors(ThemeConfig theme)
        {
            if (particleSystem == null) return;

            var main = particleSystem.main;
            main.startColor = new ParticleSystem.MinMaxGradient(theme.secondaryColor);
        }

        /// <summary>
        /// Manually trigger theme update
        /// </summary>
        public void RefreshTheme()
        {
            var themeManager = FindObjectOfType<ThemeManager>();
            if (themeManager != null)
            {
                ThemeConfig currentTheme = themeManager.GetCurrentTheme();
                if (currentTheme != null)
                {
                    ApplyTheme(currentTheme);
                }
            }
        }
    }
}
