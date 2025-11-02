using UnityEngine;
using InfiniteHaus.Data;

namespace InfiniteHaus.Theme
{
    /// <summary>
    /// Manages theme switching and applies ThemeConfig to the game.
    /// Handles visual/audio theme changes at runtime.
    /// </summary>
    public class ThemeManager : MonoBehaviour
    {
        [Header("Theme Configuration")]
        [Tooltip("Available themes")]
        [SerializeField] private ThemeConfig[] availableThemes;

        [Tooltip("Current active theme")]
        [SerializeField] private ThemeConfig currentTheme;

        [Tooltip("Default theme to use on startup")]
        [SerializeField] private int defaultThemeIndex = 0;

        [Header("Theme Switching")]
        [Tooltip("Auto-switch themes every N doors")]
        [SerializeField] private bool autoSwitchThemes = false;

        [Tooltip("Doors between theme switches")]
        [SerializeField] private int doorsPerThemeSwitch = 5;

        [Header("References")]
        [Tooltip("Camera for background rendering")]
        [SerializeField] private Camera mainCamera;

        [Tooltip("Audio source for background music")]
        [SerializeField] private AudioSource musicSource;

        [Tooltip("Audio source for ambient sounds")]
        [SerializeField] private AudioSource ambientSource;

        // State
        private int doorCounter = 0;

        // Events
        public System.Action<ThemeConfig> OnThemeChanged;

        private void Start()
        {
            // Setup audio sources
            SetupAudioSources();

            // Load saved theme preference
            int savedThemeIndex = PlayerPrefs.GetInt("LastThemeIndex", defaultThemeIndex);
            LoadTheme(savedThemeIndex);
        }

        /// <summary>
        /// Loads and applies a theme by index
        /// </summary>
        public void LoadTheme(int themeIndex)
        {
            if (availableThemes == null || availableThemes.Length == 0)
            {
                Debug.LogError("No themes available!");
                return;
            }

            themeIndex = Mathf.Clamp(themeIndex, 0, availableThemes.Length - 1);
            currentTheme = availableThemes[themeIndex];

            if (currentTheme != null)
            {
                ApplyTheme(currentTheme);
                PlayerPrefs.SetInt("LastThemeIndex", themeIndex);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// Loads and applies a specific theme config
        /// </summary>
        public void LoadTheme(ThemeConfig theme)
        {
            if (theme == null)
            {
                Debug.LogError("Theme is null!");
                return;
            }

            currentTheme = theme;
            ApplyTheme(theme);
        }

        /// <summary>
        /// Applies theme to all game systems
        /// </summary>
        private void ApplyTheme(ThemeConfig theme)
        {
            Debug.Log($"Applying theme: {theme.themeName}");

            // Apply camera background color
            if (mainCamera != null)
            {
                mainCamera.backgroundColor = theme.primaryColor;
            }

            // Apply ambient lighting
            RenderSettings.ambientLight = theme.ambientColor;

            // Apply background music
            if (musicSource != null && theme.backgroundMusic != null)
            {
                musicSource.clip = theme.backgroundMusic;
                musicSource.loop = true;
                musicSource.Play();
            }

            // Apply ambient sounds
            if (ambientSource != null && theme.ambientSounds != null && theme.ambientSounds.Length > 0)
            {
                AudioClip randomAmbient = theme.ambientSounds[Random.Range(0, theme.ambientSounds.Length)];
                ambientSource.clip = randomAmbient;
                ambientSource.loop = true;
                ambientSource.Play();
            }

            // Notify all theme applicators
            ThemeApplicator[] applicators = FindObjectsOfType<ThemeApplicator>();
            foreach (var applicator in applicators)
            {
                applicator.ApplyTheme(theme);
            }

            // Notify UI
            var uiController = FindObjectOfType<UI.UIController>();
            if (uiController != null)
            {
                uiController.UpdateThemeName(theme.themeName);
            }

            // Trigger event
            OnThemeChanged?.Invoke(theme);
        }

        /// <summary>
        /// Switches to next theme in list
        /// </summary>
        public void SwitchToNextTheme()
        {
            if (availableThemes == null || availableThemes.Length == 0) return;

            int currentIndex = System.Array.IndexOf(availableThemes, currentTheme);
            int nextIndex = (currentIndex + 1) % availableThemes.Length;

            LoadTheme(nextIndex);
        }

        /// <summary>
        /// Switches to random theme
        /// </summary>
        public void SwitchToRandomTheme()
        {
            if (availableThemes == null || availableThemes.Length == 0) return;

            int randomIndex = Random.Range(0, availableThemes.Length);
            LoadTheme(randomIndex);
        }

        /// <summary>
        /// Called when player passes through a door
        /// </summary>
        public void OnDoorPassed()
        {
            if (!autoSwitchThemes) return;

            doorCounter++;

            if (doorCounter >= doorsPerThemeSwitch)
            {
                doorCounter = 0;
                SwitchToRandomTheme();
            }
        }

        /// <summary>
        /// Setup audio sources if not assigned
        /// </summary>
        private void SetupAudioSources()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (musicSource == null)
            {
                GameObject musicObj = new GameObject("MusicSource");
                musicObj.transform.parent = transform;
                musicSource = musicObj.AddComponent<AudioSource>();
                musicSource.playOnAwake = false;
                musicSource.spatialBlend = 0f; // 2D sound
            }

            if (ambientSource == null)
            {
                GameObject ambientObj = new GameObject("AmbientSource");
                ambientObj.transform.parent = transform;
                ambientSource = ambientObj.AddComponent<AudioSource>();
                ambientSource.playOnAwake = false;
                ambientSource.spatialBlend = 0f; // 2D sound
                ambientSource.volume = 0.5f; // Quieter than music
            }
        }

        /// <summary>
        /// Gets current theme config
        /// </summary>
        public ThemeConfig GetCurrentTheme()
        {
            return currentTheme;
        }

        /// <summary>
        /// Gets all available themes
        /// </summary>
        public ThemeConfig[] GetAvailableThemes()
        {
            return availableThemes;
        }

        /// <summary>
        /// Sets music volume
        /// </summary>
        public void SetMusicVolume(float volume)
        {
            if (musicSource != null)
            {
                musicSource.volume = Mathf.Clamp01(volume);
            }
        }

        /// <summary>
        /// Sets ambient volume
        /// </summary>
        public void SetAmbientVolume(float volume)
        {
            if (ambientSource != null)
            {
                ambientSource.volume = Mathf.Clamp01(volume);
            }
        }
    }
}
