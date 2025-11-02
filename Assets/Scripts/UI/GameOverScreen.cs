using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using InfiniteHaus.Collectibles;

namespace InfiniteHaus.UI
{
    /// <summary>
    /// Displays game over / end run screen with stats and rewards.
    /// Shows collected items, distance, score, and permanent upgrades.
    /// </summary>
    public class GameOverScreen : MonoBehaviour
    {
        [Header("UI Elements")]
        [Tooltip("Game over title text")]
        [SerializeField] private TextMeshProUGUI titleText;

        [Tooltip("Final score text")]
        [SerializeField] private TextMeshProUGUI scoreText;

        [Tooltip("High score text")]
        [SerializeField] private TextMeshProUGUI highScoreText;

        [Tooltip("Distance traveled text")]
        [SerializeField] private TextMeshProUGUI distanceText;

        [Header("Collectible Stats")]
        [Tooltip("Soul Shards collected text")]
        [SerializeField] private TextMeshProUGUI soulShardsText;

        [Tooltip("Relics collected text")]
        [SerializeField] private TextMeshProUGUI relicsText;

        [Tooltip("Chase Crystals collected text")]
        [SerializeField] private TextMeshProUGUI chaseCrystalsText;

        [Header("Permanent Progress")]
        [Tooltip("Total relics across all runs")]
        [SerializeField] private TextMeshProUGUI totalRelicsText;

        [Tooltip("HP upgrades earned")]
        [SerializeField] private TextMeshProUGUI hpUpgradesText;

        [Header("Cause of Death")]
        [Tooltip("Death reason text")]
        [SerializeField] private TextMeshProUGUI deathReasonText;

        [Header("Buttons")]
        [Tooltip("Retry button")]
        [SerializeField] private Button retryButton;

        [Tooltip("Main menu button")]
        [SerializeField] private Button mainMenuButton;

        [Tooltip("Quit button")]
        [SerializeField] private Button quitButton;

        [Header("Visual Effects")]
        [Tooltip("Fade in duration")]
        [SerializeField] private float fadeInDuration = 1f;

        [Tooltip("Animate stats on display")]
        [SerializeField] private bool animateStats = true;

        [Header("Audio")]
        [Tooltip("Game over music")]
        [SerializeField] private AudioClip gameOverMusic;

        [Tooltip("New high score sound")]
        [SerializeField] private AudioClip highScoreSound;

        // Components
        private CanvasGroup canvasGroup;
        private AudioSource audioSource;

        // Events
        public System.Action OnRetry;
        public System.Action OnMainMenu;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();

            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            // Setup button listeners
            if (retryButton != null)
            {
                retryButton.onClick.AddListener(HandleRetry);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(HandleMainMenu);
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(HandleQuit);
            }

            // Start hidden
            Hide();
        }

        /// <summary>
        /// Shows game over screen with run stats
        /// </summary>
        public void Show(float distance, int score, RewardManager.RunSummary summary, string deathReason = "Caught by monsters")
        {
            gameObject.SetActive(true);

            // Update texts
            UpdateStats(distance, score, summary, deathReason);

            // Play music
            if (gameOverMusic != null && audioSource != null)
            {
                audioSource.clip = gameOverMusic;
                audioSource.loop = true;
                audioSource.Play();
            }

            // Fade in
            StartCoroutine(FadeInCoroutine());
        }

        /// <summary>
        /// Updates all stat displays
        /// </summary>
        private void UpdateStats(float distance, int score, RewardManager.RunSummary summary, string deathReason)
        {
            // Title
            if (titleText != null)
            {
                titleText.text = "RUN COMPLETE";
            }

            // Distance
            if (distanceText != null)
            {
                distanceText.text = $"Distance: {Mathf.RoundToInt(distance)}m";
            }

            // Score
            if (scoreText != null)
            {
                scoreText.text = $"Score: {score}";
            }

            // High score
            int highScore = PlayerPrefs.GetInt("HighScore", 0);
            bool isNewHighScore = score > highScore;

            if (isNewHighScore)
            {
                PlayerPrefs.SetInt("HighScore", score);
                PlayerPrefs.Save();

                if (highScoreSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(highScoreSound);
                }
            }

            if (highScoreText != null)
            {
                highScoreText.text = isNewHighScore ?
                    $"NEW HIGH SCORE!" :
                    $"High Score: {highScore}";

                highScoreText.color = isNewHighScore ? Color.yellow : Color.white;
            }

            // Collectibles
            if (soulShardsText != null)
            {
                soulShardsText.text = $"Soul Shards: {summary.soulShards}";
            }

            if (relicsText != null)
            {
                relicsText.text = $"Relics: {summary.relics}";
            }

            if (chaseCrystalsText != null)
            {
                chaseCrystalsText.text = $"Chase Crystals: {summary.chaseCrystals}";
            }

            // Permanent progress
            if (totalRelicsText != null)
            {
                totalRelicsText.text = $"Total Relics: {summary.totalRelics}";
            }

            if (hpUpgradesText != null)
            {
                hpUpgradesText.text = $"HP Upgrades: {summary.hpUpgrades}";
            }

            // Death reason
            if (deathReasonText != null)
            {
                deathReasonText.text = deathReason;
            }

            // Animate stats if enabled
            if (animateStats)
            {
                StartCoroutine(AnimateStatsCoroutine());
            }
        }

        /// <summary>
        /// Fades in game over screen
        /// </summary>
        private System.Collections.IEnumerator FadeInCoroutine()
        {
            if (canvasGroup == null) yield break;

            canvasGroup.alpha = 0f;
            float elapsed = 0f;

            while (elapsed < fadeInDuration)
            {
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            canvasGroup.alpha = 1f;
        }

        /// <summary>
        /// Animates stat numbers counting up
        /// </summary>
        private System.Collections.IEnumerator AnimateStatsCoroutine()
        {
            // Simple pulse effect for now
            float elapsed = 0f;
            float duration = 0.5f;

            while (elapsed < duration)
            {
                float scale = Mathf.Lerp(0.8f, 1f, elapsed / duration);

                if (scoreText != null)
                    scoreText.transform.localScale = Vector3.one * scale;

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (scoreText != null)
                scoreText.transform.localScale = Vector3.one;
        }

        /// <summary>
        /// Hides game over screen
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
        }

        /// <summary>
        /// Handles retry button
        /// </summary>
        private void HandleRetry()
        {
            OnRetry?.Invoke();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>
        /// Handles main menu button
        /// </summary>
        private void HandleMainMenu()
        {
            OnMainMenu?.Invoke();
            SceneManager.LoadScene("MainMenu");
        }

        /// <summary>
        /// Handles quit button
        /// </summary>
        private void HandleQuit()
        {
            Application.Quit();

            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #endif
        }
    }
}
