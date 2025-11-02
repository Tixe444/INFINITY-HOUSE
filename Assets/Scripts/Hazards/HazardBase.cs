using UnityEngine;

namespace InfiniteHaus.Hazards
{
    /// <summary>
    /// Base class for all hazards (spikes, goo, crumbling platforms, etc.)
    /// Provides common functionality for damage, detection, and visual effects.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public abstract class HazardBase : MonoBehaviour
    {
        [Header("Hazard Settings")]
        [Tooltip("Damage dealt to player")]
        [SerializeField] protected int damage = 1;

        [Tooltip("Is this hazard currently active?")]
        [SerializeField] protected bool isActive = true;

        [Tooltip("Chase meter increase when triggered")]
        [SerializeField] protected float chaseIncrease = 5f;

        [Header("Visual/Audio")]
        [Tooltip("Sprite renderer for hazard visuals")]
        [SerializeField] protected SpriteRenderer spriteRenderer;

        [Tooltip("Particle effect when triggered")]
        [SerializeField] protected ParticleSystem triggerParticles;

        [Tooltip("Sound effect when triggered")]
        [SerializeField] protected AudioClip triggerSound;

        // Components
        protected Collider2D hazardCollider;

        // Events
        public System.Action<GameObject> OnHazardTriggered;

        protected virtual void Awake()
        {
            hazardCollider = GetComponent<Collider2D>();

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        /// <summary>
        /// Activates this hazard
        /// </summary>
        public virtual void Activate()
        {
            isActive = true;

            if (hazardCollider != null)
            {
                hazardCollider.enabled = true;
            }
        }

        /// <summary>
        /// Deactivates this hazard
        /// </summary>
        public virtual void Deactivate()
        {
            isActive = false;

            if (hazardCollider != null)
            {
                hazardCollider.enabled = false;
            }
        }

        /// <summary>
        /// Sets the damage value
        /// </summary>
        public void SetDamage(int damageAmount)
        {
            damage = damageAmount;
        }

        /// <summary>
        /// Triggers hazard effect (damage, visuals, audio)
        /// </summary>
        protected virtual void TriggerHazard(GameObject player)
        {
            if (!isActive) return;

            OnHazardTriggered?.Invoke(player);

            // Apply damage
            var playerHealth = player.GetComponent<InfiniteHaus.Player.PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }

            // Increase chase meter
            var chaseSystem = FindObjectOfType<Monsters.ChaseSystem>();
            if (chaseSystem != null)
            {
                chaseSystem.OnPlayerDamaged();
            }

            // Play visual/audio effects
            PlayEffects();
        }

        /// <summary>
        /// Plays visual and audio effects
        /// </summary>
        protected virtual void PlayEffects()
        {
            if (triggerParticles != null)
            {
                triggerParticles.Play();
            }

            if (triggerSound != null)
            {
                AudioSource.PlayClipAtPoint(triggerSound, transform.position);
            }
        }

        /// <summary>
        /// Sets the sprite for this hazard (theme system)
        /// </summary>
        public void SetSprite(Sprite sprite)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = sprite;
            }
        }

        /// <summary>
        /// Resets hazard to initial state
        /// </summary>
        public virtual void Reset()
        {
            isActive = true;

            if (hazardCollider != null)
            {
                hazardCollider.enabled = true;
            }
        }
    }
}
