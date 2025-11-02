using UnityEngine;
using UnityEngine.SceneManagement;

namespace InfiniteHaus.Core
{
    /// <summary>
    /// Bootstrap scene initializer.
    /// Loads persistent systems and transitions to main menu or game.
    /// Should be attached to a GameObject in the Boot scene.
    /// </summary>
    public class Bootstrap : MonoBehaviour
    {
        [Header("Boot Settings")]
        [Tooltip("Auto-load main menu after boot")]
        [SerializeField] private bool autoLoadMainMenu = true;

        [Tooltip("Scene to load after boot")]
        [SerializeField] private string menuSceneName = "MainMenu";

        [Tooltip("Delay before loading next scene (seconds)")]
        [SerializeField] private float loadDelay = 1f;

        [Header("Splash/Loading")]
        [Tooltip("Show loading screen")]
        [SerializeField] private bool showLoadingScreen = false;

        [Tooltip("Loading screen UI")]
        [SerializeField] private GameObject loadingScreen;

        private void Start()
        {
            Debug.Log("=== INFINITE HAUS - Boot Sequence ===");

            // Initialize persistent systems
            InitializeSystems();

            // Load next scene
            if (autoLoadMainMenu)
            {
                Invoke(nameof(LoadNextScene), loadDelay);
            }
        }

        /// <summary>
        /// Initializes all persistent game systems
        /// </summary>
        private void InitializeSystems()
        {
            Debug.Log("Initializing game systems...");

            // Set target frame rate
            Application.targetFrameRate = 60;

            // Set quality settings
            QualitySettings.vSyncCount = 0; // Disable VSync for consistent frame rate

            // Initialize save system
            var saveData = Save.SaveSystem.LoadGame();
            if (saveData != null)
            {
                Debug.Log($"Save data loaded - High Score: {saveData.highScore}, Relics: {saveData.totalRelicsCollected}");
            }

            // Set up audio
            AudioListener.volume = saveData?.masterVolume ?? 1f;

            Debug.Log("Systems initialized successfully");
        }

        /// <summary>
        /// Loads the next scene
        /// </summary>
        private void LoadNextScene()
        {
            if (showLoadingScreen && loadingScreen != null)
            {
                loadingScreen.SetActive(true);
            }

            Debug.Log($"Loading scene: {menuSceneName}");
            SceneManager.LoadScene(menuSceneName);
        }

        /// <summary>
        /// Loads game scene directly (skip menu)
        /// </summary>
        public void LoadGameDirectly()
        {
            SceneManager.LoadScene("Game");
        }
    }
}
