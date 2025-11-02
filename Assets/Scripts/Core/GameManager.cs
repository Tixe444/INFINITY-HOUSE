using UnityEngine;
using InfiniteHaus.Player;
using InfiniteHaus.Monsters;
using InfiniteHaus.Collectibles;
using InfiniteHaus.Level;
using InfiniteHaus.UI;
using InfiniteHaus.Theme;
using InfiniteHaus.Save;

namespace InfiniteHaus.Core
{
    /// <summary>
    /// Central game manager that coordinates all major systems.
    /// Handles game state, initialization, and system integration.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("System References")]
        [Tooltip("Player controller")]
        [SerializeField] private PlayerController playerController;

        [Tooltip("Player health")]
        [SerializeField] private PlayerHealth playerHealth;

        [Tooltip("Chase system")]
        [SerializeField] private ChaseSystem chaseSystem;

        [Tooltip("Reward manager")]
        [SerializeField] private RewardManager rewardManager;

        [Tooltip("Corridor manager")]
        [SerializeField] private CorridorManager corridorManager;

        [Tooltip("UI controller")]
        [SerializeField] private UIController uiController;

        [Tooltip("Theme manager")]
        [SerializeField] private ThemeManager themeManager;

        [Tooltip("Settings manager")]
        [SerializeField] private SettingsManager settingsManager;

        [Header("Game State")]
        [Tooltip("Current game state")]
        [SerializeField] private GameState currentState = GameState.Menu;

        [Tooltip("Time since game started")]
        [SerializeField] private float gameTime = 0f;

        public enum GameState
        {
            Menu,
            Countdown,
            Playing,
            Paused,
            GameOver
        }

        // Properties
        public GameState CurrentState => currentState;
        public float GameTime => gameTime;

        // Events
        public System.Action<GameState> OnGameStateChanged;

        private void Awake()
        {
            // Auto-find components if not assigned
            FindComponents();
        }

        private void Start()
        {
            InitializeGame();
        }

        private void Update()
        {
            if (currentState == GameState.Playing)
            {
                gameTime += Time.deltaTime;
            }
        }

        /// <summary>
        /// Initializes all game systems
        /// </summary>
        private void InitializeGame()
        {
            // Load settings
            if (settingsManager != null)
            {
                settingsManager.LoadSettings();
                settingsManager.ApplySettings();
            }

            // Subscribe to events
            SubscribeToEvents();

            // Start new game
            StartNewGame();
        }

        /// <summary>
        /// Subscribes to all system events
        /// </summary>
        private void SubscribeToEvents()
        {
            // Player events
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged += HandleHealthChanged;
                playerHealth.OnDeath += HandlePlayerDeath;
            }

            // Chase system events
            if (chaseSystem != null)
            {
                chaseSystem.OnChaseMeterChanged += HandleChaseMeterChanged;
                chaseSystem.OnPlayerCaught += HandlePlayerCaught;
            }

            // Reward manager events
            if (rewardManager != null)
            {
                rewardManager.OnSoulShardCollected += HandleSoulShardCollected;
                rewardManager.OnRelicCollected += HandleRelicCollected;
                rewardManager.OnChaseCrystalCollected += HandleChaseCrystalCollected;
                rewardManager.OnHPUpgradeEarned += HandleHPUpgrade;
            }

            // Corridor manager events
            if (corridorManager != null)
            {
                corridorManager.OnDistanceChanged += HandleDistanceChanged;
            }

            // UI events
            if (uiController != null)
            {
                uiController.OnGameStarted += HandleGameStarted;
            }
        }

        /// <summary>
        /// Starts a new game
        /// </summary>
        public void StartNewGame()
        {
            Debug.Log("Starting new game...");

            // Reset game time
            gameTime = 0f;

            // Reset player
            if (playerController != null)
            {
                playerController.ResetPlayer();
            }

            if (playerHealth != null)
            {
                playerHealth.ResetHealth();
            }

            // Reset chase system
            if (chaseSystem != null)
            {
                chaseSystem.ResetChase();
            }

            // Reset reward manager
            if (rewardManager != null)
            {
                rewardManager.StartNewRun();
            }

            // Reset level
            if (corridorManager != null)
            {
                corridorManager.ResetLevel();
            }

            // Start UI countdown
            if (uiController != null)
            {
                uiController.StartGame();
            }

            ChangeState(GameState.Countdown);
        }

        /// <summary>
        /// Called when countdown completes and gameplay begins
        /// </summary>
        private void HandleGameStarted()
        {
            Debug.Log("Gameplay started!");

            // Enable player movement
            if (playerController != null)
            {
                playerController.EnableMovement();
            }

            // Start chase system
            if (chaseSystem != null)
            {
                chaseSystem.StartChase();
            }

            ChangeState(GameState.Playing);
        }

        /// <summary>
        /// Changes game state
        /// </summary>
        private void ChangeState(GameState newState)
        {
            if (currentState == newState) return;

            currentState = newState;
            OnGameStateChanged?.Invoke(newState);

            Debug.Log($"Game state changed to: {newState}");
        }

        #region Event Handlers

        private void HandleHealthChanged(int current, int max)
        {
            if (uiController != null)
            {
                uiController.UpdateHealth(current, max);
            }
        }

        private void HandleChaseMeterChanged(float value)
        {
            if (uiController != null)
            {
                uiController.UpdateChaseMeter(value);
            }
        }

        private void HandleSoulShardCollected(int count)
        {
            if (uiController != null)
            {
                uiController.UpdateSoulShards(count);
            }
        }

        private void HandleRelicCollected(int current, int required)
        {
            if (uiController != null)
            {
                uiController.UpdateRelicTracker(current, required);
            }
        }

        private void HandleChaseCrystalCollected()
        {
            if (rewardManager != null && uiController != null)
            {
                uiController.UpdateChaseCrystals(rewardManager.CurrentRunChaseCrystals);
            }
        }

        private void HandleHPUpgrade(int newMaxHP)
        {
            Debug.Log($"HP Upgrade earned! New max HP: {newMaxHP}");

            if (uiController != null)
            {
                uiController.FlashMilestone(Color.green);
            }
        }

        private void HandleDistanceChanged(float distance)
        {
            if (uiController != null)
            {
                uiController.UpdateDistance(distance);
            }
        }

        private void HandlePlayerDeath()
        {
            Debug.Log("Player died!");
            EndGame("Killed by hazard");
        }

        private void HandlePlayerCaught()
        {
            Debug.Log("Player caught by monsters!");
            EndGame("Caught by monsters");
        }

        #endregion

        /// <summary>
        /// Ends the current game
        /// </summary>
        public void EndGame(string deathReason = "Unknown")
        {
            if (currentState == GameState.GameOver) return;

            Debug.Log($"Game Over: {deathReason}");

            ChangeState(GameState.GameOver);

            // Stop player
            if (playerController != null)
            {
                playerController.DisableMovement();
            }

            // Stop chase
            if (chaseSystem != null)
            {
                chaseSystem.StopChase();
            }

            // Complete run and get stats
            float distance = corridorManager != null ? corridorManager.TotalDistance : 0f;

            if (rewardManager != null)
            {
                rewardManager.CompleteRun(distance);
            }

            // Show game over screen
            if (uiController != null && rewardManager != null)
            {
                var summary = rewardManager.GetRunSummary();
                uiController.ShowGameOver(distance, summary.score, summary, deathReason);
            }

            // Save progress
            SaveProgress(distance);
        }

        /// <summary>
        /// Saves game progress
        /// </summary>
        private void SaveProgress(float distance)
        {
            SaveData saveData = settingsManager != null ? settingsManager.GetSettings() : SaveSystem.LoadGame();

            if (saveData == null) return;

            // Update statistics
            saveData.totalRuns++;

            if (rewardManager != null)
            {
                int finalScore = rewardManager.CurrentRunScore;

                if (finalScore > saveData.highScore)
                {
                    saveData.highScore = finalScore;
                }

                if (distance > saveData.bestDistance)
                {
                    saveData.bestDistance = distance;
                }

                saveData.totalRelicsCollected = rewardManager.TotalRelicsCollected;
                saveData.permanentHPUpgrades = rewardManager.PermanentHPUpgrades;
                saveData.totalSoulShardsCollected += rewardManager.CurrentRunSoulShards;
                saveData.totalChaseCrystalsCollected += rewardManager.CurrentRunChaseCrystals;
                saveData.totalDistanceTraveled += distance;
            }

            // Save to disk
            SaveSystem.SaveGame(saveData);
            SaveSystem.QuickSave(saveData);

            Debug.Log("Progress saved");
        }

        /// <summary>
        /// Finds required components automatically
        /// </summary>
        private void FindComponents()
        {
            if (playerController == null)
            {
                playerController = FindObjectOfType<PlayerController>();
            }

            if (playerHealth == null)
            {
                playerHealth = FindObjectOfType<PlayerHealth>();
            }

            if (chaseSystem == null)
            {
                chaseSystem = FindObjectOfType<ChaseSystem>();
            }

            if (rewardManager == null)
            {
                rewardManager = FindObjectOfType<RewardManager>();
            }

            if (corridorManager == null)
            {
                corridorManager = FindObjectOfType<CorridorManager>();
            }

            if (uiController == null)
            {
                uiController = FindObjectOfType<UIController>();
            }

            if (themeManager == null)
            {
                themeManager = FindObjectOfType<ThemeManager>();
            }

            if (settingsManager == null)
            {
                settingsManager = FindObjectOfType<SettingsManager>();
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged -= HandleHealthChanged;
                playerHealth.OnDeath -= HandlePlayerDeath;
            }

            if (chaseSystem != null)
            {
                chaseSystem.OnChaseMeterChanged -= HandleChaseMeterChanged;
                chaseSystem.OnPlayerCaught -= HandlePlayerCaught;
            }

            if (rewardManager != null)
            {
                rewardManager.OnSoulShardCollected -= HandleSoulShardCollected;
                rewardManager.OnRelicCollected -= HandleRelicCollected;
                rewardManager.OnChaseCrystalCollected -= HandleChaseCrystalCollected;
                rewardManager.OnHPUpgradeEarned -= HandleHPUpgrade;
            }

            if (corridorManager != null)
            {
                corridorManager.OnDistanceChanged -= HandleDistanceChanged;
            }

            if (uiController != null)
            {
                uiController.OnGameStarted -= HandleGameStarted;
            }
        }
    }
}
