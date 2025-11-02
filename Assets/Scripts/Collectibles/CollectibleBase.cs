using UnityEngine;

namespace InfiniteHaus.Collectibles
{
    /// <summary>
    /// Base class for all collectibles (Soul Shards, Relics, Chase Crystals).
    /// Handles collection, visual/audio feedback, and reward manager integration.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public abstract class CollectibleBase : MonoBehaviour
    {
        [Header("Collectible Settings")]
        [Tooltip("Is this collectible currently active?")]
        [SerializeField] protected bool isActive = true;

        [Tooltip("Destroy after collection?")]
        [SerializeField] protected bool destroyOnCollect = true;

        [Header("Visual/Audio")]
        [Tooltip("Sprite renderer")]
        [SerializeField] protected SpriteRenderer spriteRenderer;

        [Tooltip("Collection particle effect")]
        [SerializeField] protected ParticleSystem collectParticles;

        [Tooltip("Collection sound effect")]
        [SerializeField] protected AudioClip collectSound;

        [Header("Animation")]
        [Tooltip("Float/bob animation")]
        [SerializeField] protected bool enableFloatAnimation = true;

        [Tooltip("Float height")]
        [SerializeField] protected float floatHeight = 0.3f;

        [Tooltip("Float speed")]
        [SerializeField] protected float floatSpeed = 2f;

        [Tooltip("Rotate collectible")]
        [SerializeField] protected bool enableRotation = false;

        [Tooltip("Rotation speed")]
        [SerializeField] protected float rotationSpeed = 90f;

        // Components
        protected Collider2D collectibleCollider;

        // Animation
        private Vector3 startPosition;
        private float timeOffset;

        // Events
        public System.Action<CollectibleBase> OnCollected;

        protected virtual void Awake()
        {
            collectibleCollider = GetComponent<Collider2D>();
            collectibleCollider.isTrigger = true;

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            startPosition = transform.position;
            timeOffset = Random.Range(0f, 100f); // Random start offset for variety
        }

        protected virtual void Update()
        {
            if (!isActive) return;

            if (enableFloatAnimation)
            {
                AnimateFloat();
            }

            if (enableRotation)
            {
                AnimateRotation();
            }
        }

        /// <summary>
        /// Floating animation
        /// </summary>
        private void AnimateFloat()
        {
            float newY = startPosition.y + Mathf.Sin((Time.time + timeOffset) * floatSpeed) * floatHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }

        /// <summary>
        /// Rotation animation
        /// </summary>
        private void AnimateRotation()
        {
            transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!isActive) return;

            if (other.CompareTag("Player"))
            {
                Collect(other.gameObject);
            }
        }

        /// <summary>
        /// Handles collection logic (override in derived classes)
        /// </summary>
        protected virtual void Collect(GameObject player)
        {
            if (!isActive) return;

            isActive = false;
            OnCollected?.Invoke(this);

            PlayCollectionEffects();

            if (destroyOnCollect)
            {
                Destroy(gameObject, 0.1f); // Small delay for audio
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Plays collection visual/audio effects
        /// </summary>
        protected virtual void PlayCollectionEffects()
        {
            if (collectParticles != null)
            {
                ParticleSystem particles = Instantiate(collectParticles, transform.position, Quaternion.identity);
                Destroy(particles.gameObject, particles.main.duration);
            }

            if (collectSound != null)
            {
                AudioSource.PlayClipAtPoint(collectSound, transform.position);
            }

            // Hide sprite immediately
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = false;
            }
        }

        /// <summary>
        /// Reactivates collectible
        /// </summary>
        public virtual void Reactivate()
        {
            isActive = true;
            gameObject.SetActive(true);

            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
            }
        }

        /// <summary>
        /// Sets the sprite for this collectible (theme system)
        /// </summary>
        public void SetSprite(Sprite sprite)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = sprite;
            }
        }
    }
}
