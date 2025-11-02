using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace InfiniteHaus.UI
{
    /// <summary>
    /// Handles the "Ready → Go" countdown sequence at the start of a run.
    /// Includes fade-in effects and delays player input until complete.
    /// </summary>
    public class StartCountdown : MonoBehaviour
    {
        [Header("UI Elements")]
        [Tooltip("Countdown text display")]
        [SerializeField] private TextMeshProUGUI countdownText;

        [Tooltip("Fade overlay (black screen)")]
        [SerializeField] private CanvasGroup fadeOverlay;

        [Tooltip("Vignette effect overlay")]
        [SerializeField] private Image vignetteOverlay;

        [Header("Countdown Settings")]
        [Tooltip("Duration to show 'Ready' (seconds)")]
        [SerializeField] private float readyDuration = 1f;

        [Tooltip("Duration to show 'Go' (seconds)")]
        [SerializeField] private float goDuration = 0.5f;

        [Tooltip("Fade in duration (seconds)")]
        [SerializeField] private float fadeInDuration = 1f;

        [Header("Visual Effects")]
        [Tooltip("Ready text color")]
        [SerializeField] private Color readyColor = Color.yellow;

        [Tooltip("Go text color")]
        [SerializeField] private Color goColor = Color.green;

        [Tooltip("Text scale animation")]
        [SerializeField] private AnimationCurve scaleAnimation = AnimationCurve.EaseInOut(0, 1, 1, 1.5f);

        [Tooltip("Enable screen shake on 'Go'")]
        [SerializeField] private bool enableShakeOnGo = true;

        [Header("Audio")]
        [Tooltip("Ready sound effect")]
        [SerializeField] private AudioClip readySound;

        [Tooltip("Go sound effect")]
        [SerializeField] private AudioClip goSound;

        [Tooltip("Background music to start")]
        [SerializeField] private AudioClip backgroundMusic;

        // Components
        private AudioSource audioSource;

        // Events
        public System.Action OnCountdownComplete;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        /// <summary>
        /// Starts the countdown sequence
        /// </summary>
        public void StartCountdownSequence()
        {
            StartCoroutine(CountdownCoroutine());
        }

        /// <summary>
        /// Main countdown coroutine
        /// </summary>
        private IEnumerator CountdownCoroutine()
        {
            // Fade in from black
            yield return StartCoroutine(FadeInSequence());

            // Show "Ready"
            yield return StartCoroutine(ShowReadySequence());

            // Show "Go"
            yield return StartCoroutine(ShowGoSequence());

            // Fade out countdown text
            yield return StartCoroutine(FadeOutText());

            // Start background music
            if (backgroundMusic != null && audioSource != null)
            {
                audioSource.clip = backgroundMusic;
                audioSource.loop = true;
                audioSource.Play();
            }

            // Trigger completion event
            OnCountdownComplete?.Invoke();

            // Hide this UI
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Fade in from black screen
        /// </summary>
        private IEnumerator FadeInSequence()
        {
            if (fadeOverlay == null) yield break;

            fadeOverlay.alpha = 1f;
            float elapsed = 0f;

            while (elapsed < fadeInDuration)
            {
                fadeOverlay.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeInDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            fadeOverlay.alpha = 0f;

            // Also fade vignette
            if (vignetteOverlay != null)
            {
                Color color = vignetteOverlay.color;
                color.a = 0.3f;
                vignetteOverlay.color = color;
            }
        }

        /// <summary>
        /// Shows "Ready" text with animation
        /// </summary>
        private IEnumerator ShowReadySequence()
        {
            if (countdownText == null) yield break;

            countdownText.text = "READY";
            countdownText.color = readyColor;
            countdownText.gameObject.SetActive(true);

            // Play sound
            if (readySound != null && audioSource != null)
            {
                audioSource.PlayOneShot(readySound);
            }

            // Scale animation
            float elapsed = 0f;
            Vector3 originalScale = Vector3.one;

            while (elapsed < readyDuration)
            {
                float scale = scaleAnimation.Evaluate(elapsed / readyDuration);
                countdownText.transform.localScale = originalScale * scale;
                elapsed += Time.deltaTime;
                yield return null;
            }

            countdownText.transform.localScale = originalScale;
        }

        /// <summary>
        /// Shows "Go" text with animation
        /// </summary>
        private IEnumerator ShowGoSequence()
        {
            if (countdownText == null) yield break;

            countdownText.text = "GO!";
            countdownText.color = goColor;

            // Play sound
            if (goSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(goSound);
            }

            // Screen shake effect
            if (enableShakeOnGo)
            {
                StartCoroutine(ScreenShakeEffect());
            }

            // Scale animation
            float elapsed = 0f;
            Vector3 originalScale = Vector3.one;

            while (elapsed < goDuration)
            {
                float scale = scaleAnimation.Evaluate(elapsed / goDuration) * 1.2f;
                countdownText.transform.localScale = originalScale * scale;
                elapsed += Time.deltaTime;
                yield return null;
            }

            countdownText.transform.localScale = originalScale;
        }

        /// <summary>
        /// Fades out countdown text
        /// </summary>
        private IEnumerator FadeOutText()
        {
            if (countdownText == null) yield break;

            Color startColor = countdownText.color;
            float elapsed = 0f;
            float fadeDuration = 0.3f;

            while (elapsed < fadeDuration)
            {
                Color color = startColor;
                color.a = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                countdownText.color = color;
                elapsed += Time.deltaTime;
                yield return null;
            }

            countdownText.gameObject.SetActive(false);
        }

        /// <summary>
        /// Screen shake effect on "Go"
        /// </summary>
        private IEnumerator ScreenShakeEffect()
        {
            Vector3 originalPosition = countdownText.transform.position;
            float shakeIntensity = 10f;
            float shakeDuration = 0.2f;
            float elapsed = 0f;

            while (elapsed < shakeDuration)
            {
                float x = Random.Range(-shakeIntensity, shakeIntensity);
                float y = Random.Range(-shakeIntensity, shakeIntensity);

                countdownText.transform.position = originalPosition + new Vector3(x, y, 0);

                elapsed += Time.deltaTime;
                yield return null;
            }

            countdownText.transform.position = originalPosition;
        }

        /// <summary>
        /// Resets countdown for replay
        /// </summary>
        public void Reset()
        {
            gameObject.SetActive(true);

            if (countdownText != null)
            {
                countdownText.transform.localScale = Vector3.one;
                countdownText.color = Color.white;
                countdownText.gameObject.SetActive(false);
            }

            if (fadeOverlay != null)
            {
                fadeOverlay.alpha = 1f;
            }

            StopAllCoroutines();
        }
    }
}
