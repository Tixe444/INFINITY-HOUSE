using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace InfiniteHaus.UI
{
    /// <summary>
    /// Manages the in-game HUD display.
    /// Shows Chase Meter, HP hearts, distance, collectibles, and current theme.
    /// </summary>
    public class HUDManager : MonoBehaviour
    {
        [Header("Chase Meter")]
        [Tooltip("Chase meter fill image")]
        [SerializeField] private Image chaseMeterFill;

        [Tooltip("Chase meter text (percentage)")]
        [SerializeField] private TextMeshProUGUI chaseMeterText;

        [Tooltip("Chase meter warning color (>75%)")]
        [SerializeField] private Color warningColor = Color.red;

        [Tooltip("Chase meter normal color")]
        [SerializeField] private Color normalColor = new Color(1f, 0.5f, 0f);

        [Header("Health Display")]
        [Tooltip("Parent container for heart icons")]
        [SerializeField] private Transform heartsContainer;

        [Tooltip("Heart icon prefab")]
        [SerializeField] private GameObject heartIconPrefab;

        [Tooltip("Full heart sprite")]
        [SerializeField] private Sprite heartFullSprite;

        [Tooltip("Empty heart sprite")]
        [SerializeField] private Sprite heartEmptySprite;

        [Header("Stats Display")]
        [Tooltip("Distance counter text")]
        [SerializeField] private TextMeshProUGUI distanceText;

        [Tooltip("Soul Shard count text")]
        [SerializeField] private TextMeshProUGUI soulShardText;

        [Tooltip("Relic tracker text (e.g. \"2/3\")")]
        [SerializeField] private TextMeshProUGUI relicTrackerText;

        [Tooltip("Chase Crystal count text")]
        [SerializeField] private TextMeshProUGUI chaseCrystalText;

        [Header("Theme Display")]
        [Tooltip("Current theme label")]
        [SerializeField] private TextMeshProUGUI themeNameText;

        [Header("Visual Effects")]
        [Tooltip("Flash effect for milestones")]
        [SerializeField] private CanvasGroup flashOverlay;

        [Tooltip("Pulse animation for warnings")]
        [SerializeField] private bool enablePulseWarning = true;

        // Heart icons
        private Image[] heartIcons;

        // State
        private float currentChaseMeter = 0f;
        private bool isWarningActive = false;

        private void Start()
        {
            InitializeHearts(3); // Default 3 hearts
            UpdateAllUI();
        }

        private void Update()
        {
            if (enablePulseWarning && isWarningActive)
            {
                PulseChaseMeter();
            }
        }

        /// <summary>
        /// Updates chase meter display
        /// </summary>
        public void UpdateChaseMeter(float value)
        {
            currentChaseMeter = Mathf.Clamp(value, 0f, 100f);

            if (chaseMeterFill != null)
            {
                chaseMeterFill.fillAmount = currentChaseMeter / 100f;

                // Change color based on danger level
                if (currentChaseMeter >= 75f)
                {
                    chaseMeterFill.color = warningColor;
                    isWarningActive = true;
                }
                else
                {
                    chaseMeterFill.color = normalColor;
                    isWarningActive = false;
                }
            }

            if (chaseMeterText != null)
            {
                chaseMeterText.text = $"{Mathf.RoundToInt(currentChaseMeter)}%";
            }
        }

        /// <summary>
        /// Pulse animation for chase meter when in danger
        /// </summary>
        private void PulseChaseMeter()
        {
            if (chaseMeterFill == null) return;

            float pulse = Mathf.PingPong(Time.time * 3f, 1f);
            Color color = Color.Lerp(warningColor, Color.white, pulse * 0.3f);
            chaseMeterFill.color = color;
        }

        /// <summary>
        /// Initializes heart icons for health display
        /// </summary>
        public void InitializeHearts(int maxHealth)
        {
            // Clear existing hearts
            if (heartsContainer != null)
            {
                foreach (Transform child in heartsContainer)
                {
                    Destroy(child.gameObject);
                }
            }

            // Create new hearts
            heartIcons = new Image[maxHealth];

            for (int i = 0; i < maxHealth; i++)
            {
                if (heartIconPrefab != null && heartsContainer != null)
                {
                    GameObject heartObj = Instantiate(heartIconPrefab, heartsContainer);
                    heartIcons[i] = heartObj.GetComponent<Image>();

                    if (heartIcons[i] != null && heartFullSprite != null)
                    {
                        heartIcons[i].sprite = heartFullSprite;
                    }
                }
            }
        }

        /// <summary>
        /// Updates health display
        /// </summary>
        public void UpdateHealth(int currentHealth, int maxHealth)
        {
            // Re-initialize if max health changed
            if (heartIcons == null || heartIcons.Length != maxHealth)
            {
                InitializeHearts(maxHealth);
            }

            // Update heart sprites
            for (int i = 0; i < heartIcons.Length; i++)
            {
                if (heartIcons[i] != null)
                {
                    heartIcons[i].sprite = (i < currentHealth) ? heartFullSprite : heartEmptySprite;
                }
            }
        }

        /// <summary>
        /// Updates distance counter
        /// </summary>
        public void UpdateDistance(float distance)
        {
            if (distanceText != null)
            {
                distanceText.text = $"{Mathf.RoundToInt(distance)}m";
            }
        }

        /// <summary>
        /// Updates Soul Shard count
        /// </summary>
        public void UpdateSoulShards(int count)
        {
            if (soulShardText != null)
            {
                soulShardText.text = $"x{count}";
            }
        }

        /// <summary>
        /// Updates Relic tracker (e.g. "2/3")
        /// </summary>
        public void UpdateRelicTracker(int current, int required)
        {
            if (relicTrackerText != null)
            {
                relicTrackerText.text = $"{current}/{required}";

                // Highlight when close to upgrade
                if (current >= required)
                {
                    relicTrackerText.color = Color.yellow;
                }
                else
                {
                    relicTrackerText.color = Color.white;
                }
            }
        }

        /// <summary>
        /// Updates Chase Crystal count
        /// </summary>
        public void UpdateChaseCrystals(int count)
        {
            if (chaseCrystalText != null)
            {
                chaseCrystalText.text = $"x{count}";
            }
        }

        /// <summary>
        /// Updates theme name display
        /// </summary>
        public void UpdateThemeName(string themeName)
        {
            if (themeNameText != null)
            {
                themeNameText.text = themeName;
            }
        }

        /// <summary>
        /// Flashes screen for milestone events
        /// </summary>
        public void FlashMilestone(Color color)
        {
            if (flashOverlay != null)
            {
                StartCoroutine(FlashCoroutine(color));
            }
        }

        private System.Collections.IEnumerator FlashCoroutine(Color color)
        {
            if (flashOverlay == null) yield break;

            Image flashImage = flashOverlay.GetComponent<Image>();
            if (flashImage != null)
            {
                flashImage.color = color;
            }

            // Fade in
            float elapsed = 0f;
            while (elapsed < 0.2f)
            {
                flashOverlay.alpha = Mathf.Lerp(0f, 0.5f, elapsed / 0.2f);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Fade out
            elapsed = 0f;
            while (elapsed < 0.3f)
            {
                flashOverlay.alpha = Mathf.Lerp(0.5f, 0f, elapsed / 0.3f);
                elapsed += Time.deltaTime;
                yield return null;
            }

            flashOverlay.alpha = 0f;
        }

        /// <summary>
        /// Updates all UI elements (called on initialization)
        /// </summary>
        private void UpdateAllUI()
        {
            UpdateChaseMeter(0f);
            UpdateHealth(3, 3);
            UpdateDistance(0f);
            UpdateSoulShards(0);
            UpdateRelicTracker(0, 3);
            UpdateChaseCrystals(0);
        }

        /// <summary>
        /// Shows HUD
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Hides HUD
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
