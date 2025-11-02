using UnityEngine;

namespace InfiniteHaus.UI
{
    /// <summary>
    /// Main UI controller that coordinates all UI systems.
    /// Manages transitions between HUD, countdown, pause, and game over screens.
    /// </summary>
    public class UIController : MonoBehaviour
    {
        [Header("UI Screens")]
        [Tooltip("HUD Manager reference")]
        [SerializeField] private HUDManager hudManager;

        [Tooltip("Start countdown screen")]
        [SerializeField] private StartCountdown startCountdown;

        [Tooltip("Game over screen")]
        [SerializeField] private GameOverScreen gameOverScreen;

        [Tooltip("Pause menu panel")]
        [SerializeField] private GameObject pauseMenu;

        [Header("State")]
        [Tooltip("Is game currently paused?")]
        [SerializeField] private bool isPaused = false;

        // Events
        public System.Action OnGameStarted;
        public System.Action OnGamePaused;
        public System.Action OnGameResumed;

        // Properties
        public HUDManager HUD => hudManager;
        public bool IsPaused => isPaused;

        private void Awake()
        {
            // Subscribe to countdown completion
            if (startCountdown != null)
            {
                startCountdown.OnCountdownComplete += HandleCountdownComplete;
            }

            // Initial state
            if (pauseMenu != null)
            {
                pauseMenu.SetActive(false);
            }
        }

        private void Update()
        {
            // Handle pause input
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
            {
                TogglePause();
            }
        }

        /// <summary>
        /// Starts the game with countdown sequence
        /// </summary>
        public void StartGame()
        {
            // Hide all screens
            if (gameOverScreen != null)
            {
                gameOverScreen.Hide();
            }

            if (pauseMenu != null)
            {
                pauseMenu.SetActive(false);
            }

            // Show HUD
            if (hudManager != null)
            {
                hudManager.Show();
            }

            // Start countdown
            if (startCountdown != null)
            {
                startCountdown.Reset();
                startCountdown.StartCountdownSequence();
            }
            else
            {
                // If no countdown, start immediately
                HandleCountdownComplete();
            }
        }

        /// <summary>
        /// Called when countdown completes
        /// </summary>
        private void HandleCountdownComplete()
        {
            OnGameStarted?.Invoke();
            Debug.Log("Game started!");
        }

        /// <summary>
        /// Shows game over screen with stats
        /// </summary>
        public void ShowGameOver(float distance, int score, Collectibles.RewardManager.RunSummary summary, string deathReason = "Caught by monsters")
        {
            // Hide HUD
            if (hudManager != null)
            {
                hudManager.Hide();
            }

            // Show game over screen
            if (gameOverScreen != null)
            {
                gameOverScreen.Show(distance, score, summary, deathReason);
            }

            Debug.Log("Game over displayed");
        }

        /// <summary>
        /// Toggles pause state
        /// </summary>
        public void TogglePause()
        {
            if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }

        /// <summary>
        /// Pauses the game
        /// </summary>
        public void Pause()
        {
            isPaused = true;
            Time.timeScale = 0f;

            if (pauseMenu != null)
            {
                pauseMenu.SetActive(true);
            }

            OnGamePaused?.Invoke();
            Debug.Log("Game paused");
        }

        /// <summary>
        /// Resumes the game
        /// </summary>
        public void Resume()
        {
            isPaused = false;
            Time.timeScale = 1f;

            if (pauseMenu != null)
            {
                pauseMenu.SetActive(false);
            }

            OnGameResumed?.Invoke();
            Debug.Log("Game resumed");
        }

        /// <summary>
        /// Updates HUD chase meter
        /// </summary>
        public void UpdateChaseMeter(float value)
        {
            if (hudManager != null)
            {
                hudManager.UpdateChaseMeter(value);
            }
        }

        /// <summary>
        /// Updates HUD health display
        /// </summary>
        public void UpdateHealth(int current, int max)
        {
            if (hudManager != null)
            {
                hudManager.UpdateHealth(current, max);
            }
        }

        /// <summary>
        /// Updates HUD distance counter
        /// </summary>
        public void UpdateDistance(float distance)
        {
            if (hudManager != null)
            {
                hudManager.UpdateDistance(distance);
            }
        }

        /// <summary>
        /// Updates HUD soul shard count
        /// </summary>
        public void UpdateSoulShards(int count)
        {
            if (hudManager != null)
            {
                hudManager.UpdateSoulShards(count);
            }
        }

        /// <summary>
        /// Updates HUD relic tracker
        /// </summary>
        public void UpdateRelicTracker(int current, int required)
        {
            if (hudManager != null)
            {
                hudManager.UpdateRelicTracker(current, required);
            }
        }

        /// <summary>
        /// Updates HUD chase crystal count
        /// </summary>
        public void UpdateChaseCrystals(int count)
        {
            if (hudManager != null)
            {
                hudManager.UpdateChaseCrystals(count);
            }
        }

        /// <summary>
        /// Updates theme name display
        /// </summary>
        public void UpdateThemeName(string themeName)
        {
            if (hudManager != null)
            {
                hudManager.UpdateThemeName(themeName);
            }
        }

        /// <summary>
        /// Flashes screen for milestone
        /// </summary>
        public void FlashMilestone(Color color)
        {
            if (hudManager != null)
            {
                hudManager.FlashMilestone(color);
            }
        }
    }
}
