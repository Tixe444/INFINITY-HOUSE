using UnityEngine;
using System.Collections;

namespace InfiniteHaus.Monsters
{
    /// <summary>
    /// The Crawler - Ambushes from ceilings or walls in tight spots.
    /// Applies slow debuff to player when it strikes.
    /// </summary>
    public class Crawler : MonoBehaviour
    {
        [Header("Ambush Settings")]
        [Tooltip("Base ambush frequency (seconds)")]
        [SerializeField] private float ambushFrequency = 20f;

        [Tooltip("Warning duration before ambush (seconds)")]
        [SerializeField] private float warningDuration = 1.5f;

        [Tooltip("How long Crawler remains visible (seconds)")]
        [SerializeField] private float attackDuration = 2f;

        [Header("Ambush Locations")]
        [Tooltip("Ceiling ambush prefab position offset")]
        [SerializeField] private Vector2 ceilingOffset = new Vector2(0, 3f);

        [Tooltip("Wall ambush prefab position offset")]
        [SerializeField] private Vector2 wallOffset = new Vector2(-2f, 1f);

        [Header("Debuff Settings")]
        [Tooltip("Speed multiplier applied to player (0.5 = 50% speed)")]
        [SerializeField][Range(0.1f, 1f)] private float slowMultiplier = 0.5f;

        [Tooltip("Slow debuff duration (seconds)")]
        [SerializeField] private float slowDuration = 3f;

        [Tooltip("Trigger radius to hit player")]
        [SerializeField] private float hitRadius = 1.5f;

        [Header("References")]
        [Tooltip("Player transform")]
        [SerializeField] private Transform playerTransform;

        [Tooltip("Sprite renderer for Crawler")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Visuals")]
        [Tooltip("Warning color (flashing)")]
        [SerializeField] private Color warningColor = Color.yellow;

        [Tooltip("Attack color")]
        [SerializeField] private Color attackColor = Color.red;

        // State
        private float aggression = 0f;
        private bool isActive = false;
        private bool isAmbushing = false;
        private AmbushType currentAmbushType;

        // Coroutines
        private Coroutine ambushRoutine;

        // Events
        public System.Action<float> OnPlayerSlowed; // (slow duration)

        private enum AmbushType
        {
            Ceiling,
            Wall
        }

        private void Start()
        {
            if (playerTransform == null)
            {
                playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            // Start hidden
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = false;
            }
        }

        /// <summary>
        /// Sets aggression level from ChaseSystem (0 to 1)
        /// </summary>
        public void SetAggression(float level)
        {
            aggression = Mathf.Clamp01(level);

            // Higher aggression = more frequent ambushes
            float adjustedFrequency = ambushFrequency * (1f - aggression * 0.5f);

            // Restart ambush timer if active
            if (isActive && !isAmbushing)
            {
                StopAmbushRoutine();
                ambushRoutine = StartCoroutine(AmbushTimerCoroutine(adjustedFrequency));
            }
        }

        /// <summary>
        /// Activates Crawler ambush system
        /// </summary>
        public void Activate()
        {
            isActive = true;
            float adjustedFrequency = ambushFrequency * (1f - aggression * 0.5f);
            ambushRoutine = StartCoroutine(AmbushTimerCoroutine(adjustedFrequency));
        }

        /// <summary>
        /// Deactivates Crawler
        /// </summary>
        public void Deactivate()
        {
            isActive = false;
            StopAmbushRoutine();
            HideCrawler();
        }

        /// <summary>
        /// Ambush timer coroutine
        /// </summary>
        private IEnumerator AmbushTimerCoroutine(float frequency)
        {
            yield return new WaitForSeconds(frequency);

            if (isActive)
            {
                StartCoroutine(AmbushSequence());
            }
        }

        /// <summary>
        /// Executes full ambush sequence
        /// </summary>
        private IEnumerator AmbushSequence()
        {
            isAmbushing = true;

            // Choose random ambush type
            currentAmbushType = Random.value > 0.5f ? AmbushType.Ceiling : AmbushType.Wall;

            // Position Crawler near player
            PositionCrawler();

            // Show warning
            yield return StartCoroutine(WarningPhase());

            // Attack phase
            yield return StartCoroutine(AttackPhase());

            // Hide and reset
            HideCrawler();
            isAmbushing = false;

            // Schedule next ambush
            if (isActive)
            {
                float adjustedFrequency = ambushFrequency * (1f - aggression * 0.5f);
                ambushRoutine = StartCoroutine(AmbushTimerCoroutine(adjustedFrequency));
            }
        }

        /// <summary>
        /// Warning phase - flashing indicator before attack
        /// </summary>
        private IEnumerator WarningPhase()
        {
            if (spriteRenderer == null) yield break;

            spriteRenderer.enabled = true;
            float elapsed = 0f;
            bool isVisible = true;

            while (elapsed < warningDuration)
            {
                // Flash effect
                spriteRenderer.color = warningColor;
                spriteRenderer.enabled = isVisible;
                isVisible = !isVisible;

                yield return new WaitForSeconds(0.2f);
                elapsed += 0.2f;
            }

            spriteRenderer.enabled = true;
        }

        /// <summary>
        /// Attack phase - Crawler strikes
        /// </summary>
        private IEnumerator AttackPhase()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = attackColor;
                spriteRenderer.enabled = true;
            }

            float elapsed = 0f;
            bool hasHitPlayer = false;

            while (elapsed < attackDuration)
            {
                // Check if player is in range
                if (!hasHitPlayer && playerTransform != null)
                {
                    float distance = Vector2.Distance(transform.position, playerTransform.position);
                    if (distance <= hitRadius)
                    {
                        HitPlayer();
                        hasHitPlayer = true;
                    }
                }

                yield return null;
                elapsed += Time.deltaTime;
            }
        }

        /// <summary>
        /// Positions Crawler based on ambush type
        /// </summary>
        private void PositionCrawler()
        {
            if (playerTransform == null) return;

            Vector2 offset = currentAmbushType == AmbushType.Ceiling ? ceilingOffset : wallOffset;
            transform.position = (Vector2)playerTransform.position + offset;
        }

        /// <summary>
        /// Applies slow debuff to player
        /// </summary>
        private void HitPlayer()
        {
            OnPlayerSlowed?.Invoke(slowDuration);

            // Apply slow to player
            var playerController = playerTransform.GetComponent<InfiniteHaus.Player.PlayerController>();
            if (playerController != null)
            {
                StartCoroutine(ApplySlowDebuff(playerController));
            }

            Debug.Log("Crawler hit player! Applying slow debuff.");
        }

        /// <summary>
        /// Applies and removes slow debuff
        /// </summary>
        private IEnumerator ApplySlowDebuff(InfiniteHaus.Player.PlayerController player)
        {
            player.SetSpeedMultiplier(slowMultiplier);
            yield return new WaitForSeconds(slowDuration);
            player.ResetSpeedMultiplier();
        }

        /// <summary>
        /// Hides Crawler sprite
        /// </summary>
        private void HideCrawler()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = false;
            }
        }

        /// <summary>
        /// Stops ambush routine
        /// </summary>
        private void StopAmbushRoutine()
        {
            if (ambushRoutine != null)
            {
                StopCoroutine(ambushRoutine);
                ambushRoutine = null;
            }
        }

        /// <summary>
        /// Resets Crawler for new game
        /// </summary>
        public void Reset()
        {
            isActive = false;
            isAmbushing = false;
            aggression = 0f;
            StopAllCoroutines();
            HideCrawler();
        }

        /// <summary>
        /// Sets ambush frequency (for difficulty scaling)
        /// </summary>
        public void SetAmbushFrequency(float frequency)
        {
            ambushFrequency = frequency;
        }

        // Debug visualization
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, hitRadius);
        }
    }
}
