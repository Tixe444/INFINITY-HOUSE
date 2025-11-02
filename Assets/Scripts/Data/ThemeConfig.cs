using UnityEngine;

namespace InfiniteHaus.Data
{
    /// <summary>
    /// ScriptableObject that defines a complete visual/audio theme for the game.
    /// Allows for easy swapping of entire aesthetic styles.
    /// </summary>
    [CreateAssetMenu(fileName = "New Theme", menuName = "Infinite Haus/Theme Config")]
    public class ThemeConfig : ScriptableObject
    {
        [Header("Theme Identity")]
        [Tooltip("Display name for this theme")]
        public string themeName = "Spooky Cartoon";

        [Tooltip("Short description of the theme")]
        [TextArea(2, 4)]
        public string description = "A creepy-funny pixel art haunted house with purple and orange tones.";

        [Header("Color Palette")]
        [Tooltip("Primary background color")]
        public Color primaryColor = new Color(0.4f, 0.2f, 0.6f); // Purple

        [Tooltip("Secondary accent color")]
        public Color secondaryColor = new Color(1f, 0.5f, 0f); // Orange

        [Tooltip("Ambient light color")]
        public Color ambientColor = new Color(0.8f, 0.7f, 0.9f);

        [Tooltip("UI tint color")]
        public Color uiTintColor = Color.white;

        [Header("Background Layers (Parallax)")]
        [Tooltip("Farthest background layer (slowest movement)")]
        public Sprite backgroundFar;

        [Tooltip("Middle background layer")]
        public Sprite backgroundMid;

        [Tooltip("Closest background layer (fastest movement)")]
        public Sprite backgroundNear;

        [Tooltip("Parallax speed multipliers for each layer")]
        public Vector3 parallaxSpeeds = new Vector3(0.2f, 0.5f, 0.8f);

        [Header("Tilesets & Props")]
        [Tooltip("Floor/wall tileset sprite")]
        public Sprite tileset;

        [Tooltip("Decorative props (candles, eyes, etc.)")]
        public Sprite[] decorativeProps;

        [Tooltip("Door sprite (normal)")]
        public Sprite doorSprite;

        [Tooltip("Door sprite (selected/highlighted)")]
        public Sprite doorHighlightSprite;

        [Header("Hazard Skins")]
        [Tooltip("Spike hazard sprite")]
        public Sprite spikeSprite;

        [Tooltip("Goo puddle sprite")]
        public Sprite gooSprite;

        [Tooltip("Crumbling platform sprite")]
        public Sprite crumblePlatformSprite;

        [Tooltip("Falling ceiling sprite")]
        public Sprite fallingCeilingSprite;

        [Tooltip("Lantern zone sprite")]
        public Sprite lanternSprite;

        [Header("Audio")]
        [Tooltip("Background music for this theme")]
        public AudioClip backgroundMusic;

        [Tooltip("Ambient sound effects (wind, creaks, etc.)")]
        public AudioClip[] ambientSounds;

        [Tooltip("Jump sound effect")]
        public AudioClip jumpSFX;

        [Tooltip("Damage/hurt sound effect")]
        public AudioClip hurtSFX;

        [Tooltip("Door open sound effect")]
        public AudioClip doorOpenSFX;

        [Tooltip("Collectible pickup sound effect")]
        public AudioClip pickupSFX;

        [Header("UI Skin")]
        [Tooltip("Font color for UI text")]
        public Color uiFontColor = Color.white;

        [Tooltip("Health heart sprite (full)")]
        public Sprite heartFullSprite;

        [Tooltip("Health heart sprite (empty)")]
        public Sprite heartEmptySprite;

        [Tooltip("Chase meter fill color")]
        public Color chaseMeterColor = Color.red;

        [Header("Visual Effects")]
        [Tooltip("Fog/mist overlay sprite")]
        public Sprite fogOverlay;

        [Tooltip("Screen overlay color (for vignette/filter)")]
        public Color screenOverlayColor = new Color(0, 0, 0, 0.2f);

        [Tooltip("Particle effect prefab for theme-specific ambiance")]
        public GameObject ambientParticlesPrefab;

        [Tooltip("Enable VHS/scanline effect")]
        public bool enableVHSEffect = false;

        [Tooltip("Enable screen shake on damage")]
        public bool enableScreenShake = true;
    }
}
