using UnityEngine;

namespace InfiniteHaus.Hazards
{
    /// <summary>
    /// Lantern zone - Safe area that reduces Chase Meter.
    /// Provides temporary respite from monster pursuit.
    /// </summary>
    public class LanternZone : MonoBehaviour
    {
        [Header("Lantern Settings")]
        [Tooltip("Chase meter reduction amount")]
        [SerializeField] private float chaseReduction = 20f;

        [Tooltip("Can only be used once per game?")]
        [SerializeField] private bool singleUse = true;

        [Tooltip("Visual glow intensity")]
        [SerializeField][Range(0f, 2f)] private float glowIntensity = 1.5f;

        [Header("References")]
        [Tooltip("Sprite renderer for lantern")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Tooltip("Light component for glow effect")]
        [SerializeField] private Light2D lightComponent;

        [Tooltip("Particle effect when active")]
        [SerializeField] private ParticleSystem glowParticles;

        [Header("Audio")]
        [Tooltip("Sound when player enters lantern zone")]
        [SerializeField] private AudioClip enterSound;

        private bool hasBeenUsed = false;

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (lightComponent == null)
            {
                lightComponent = GetComponent<Light2D>();
            }
        }

        private void Start()
        {
            if (glowParticles != null)
            {
                glowParticles.Play();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (singleUse && hasBeenUsed) return;

            if (other.CompareTag("Player"))
            {
                ActivateLantern();
            }
        }

        /// <summary>
        /// Activates lantern effect
        /// </summary>
        private void ActivateLantern()
        {
            if (singleUse && hasBeenUsed) return;

            hasBeenUsed = true;

            // Reduce chase meter
            var chaseSystem = FindObjectOfType<Monsters.ChaseSystem>();
            if (chaseSystem != null)
            {
                chaseSystem.OnLanternZoneEntered();
            }

            // Play effects
            if (enterSound != null)
            {
                AudioSource.PlayClipAtPoint(enterSound, transform.position);
            }

            // Visual feedback
            if (singleUse)
            {
                DeactivateVisuals();
            }

            Debug.Log($"Lantern activated! Chase meter reduced by {chaseReduction}");
        }

        /// <summary>
        /// Deactivates visual elements after use
        /// </summary>
        private void DeactivateVisuals()
        {
            if (spriteRenderer != null)
            {
                Color color = spriteRenderer.color;
                color.a *= 0.3f; // Dim the lantern
                spriteRenderer.color = color;
            }

            if (lightComponent != null)
            {
                lightComponent.intensity *= 0.2f;
            }

            if (glowParticles != null)
            {
                glowParticles.Stop();
            }
        }

        /// <summary>
        /// Resets lantern for reuse
        /// </summary>
        public void Reset()
        {
            hasBeenUsed = false;

            if (spriteRenderer != null)
            {
                Color color = spriteRenderer.color;
                color.a = 1f;
                spriteRenderer.color = color;
            }

            if (lightComponent != null)
            {
                lightComponent.intensity = glowIntensity;
            }

            if (glowParticles != null)
            {
                glowParticles.Play();
            }
        }

        /// <summary>
        /// Sets the chase reduction amount
        /// </summary>
        public void SetChaseReduction(float amount)
        {
            chaseReduction = amount;
        }
    }
}
