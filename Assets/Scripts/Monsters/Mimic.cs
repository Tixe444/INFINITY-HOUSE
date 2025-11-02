using UnityEngine;

namespace InfiniteHaus.Monsters
{
    /// <summary>
    /// The Mimic - Pretends to be a door or object.
    /// Triggers traps or deals damage when player interacts.
    /// Can disguise itself as normal game objects.
    /// </summary>
    public class Mimic : MonoBehaviour
    {
        [Header("Disguise Settings")]
        [Tooltip("Sprite when disguised as door")]
        [SerializeField] private Sprite doorDisguiseSprite;

        [Tooltip("Sprite when disguised as collectible")]
        [SerializeField] private Sprite collectibleDisguiseSprite;

        [Tooltip("Sprite when revealed (attack form)")]
        [SerializeField] private Sprite revealedSprite;

        [Tooltip("Current disguise type")]
        [SerializeField] private DisguiseType currentDisguise = DisguiseType.Door;

        [Header("Damage Settings")]
        [Tooltip("Damage dealt when revealed")]
        [SerializeField] private int damage = 1;

        [Tooltip("Chase meter increase when triggered")]
        [SerializeField] private float chaseIncrease = 10f;

        [Header("References")]
        [Tooltip("Sprite renderer for Mimic")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Tooltip("Collider for trigger detection")]
        [SerializeField] private Collider2D mimicCollider;

        [Header("Audio")]
        [Tooltip("Sound when revealed")]
        [SerializeField] private AudioClip revealSound;

        // State
        private float aggression = 0f;
        private bool isRevealed = false;
        private bool isActive = false;

        // Events
        public System.Action<int> OnPlayerDamaged; // (damage amount)

        public enum DisguiseType
        {
            Door,
            Collectible,
            Platform
        }

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (mimicCollider == null)
            {
                mimicCollider = GetComponent<Collider2D>();
            }
        }

        private void Start()
        {
            ApplyDisguise();
        }

        /// <summary>
        /// Sets the disguise type
        /// </summary>
        public void SetDisguise(DisguiseType type)
        {
            currentDisguise = type;
            ApplyDisguise();
        }

        /// <summary>
        /// Applies visual disguise
        /// </summary>
        private void ApplyDisguise()
        {
            if (spriteRenderer == null) return;

            switch (currentDisguise)
            {
                case DisguiseType.Door:
                    spriteRenderer.sprite = doorDisguiseSprite;
                    break;
                case DisguiseType.Collectible:
                    spriteRenderer.sprite = collectibleDisguiseSprite;
                    break;
                case DisguiseType.Platform:
                    // Keep default sprite or set platform sprite
                    break;
            }

            isRevealed = false;
        }

        /// <summary>
        /// Reveals Mimic and triggers attack
        /// </summary>
        private void Reveal()
        {
            if (isRevealed) return;

            isRevealed = true;

            // Change sprite to revealed form
            if (spriteRenderer != null && revealedSprite != null)
            {
                spriteRenderer.sprite = revealedSprite;
            }

            // Play reveal sound
            if (revealSound != null)
            {
                AudioSource.PlayClipAtPoint(revealSound, transform.position);
            }

            Debug.Log("Mimic revealed!");
        }

        /// <summary>
        /// Triggered when player touches Mimic
        /// </summary>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!isActive || isRevealed) return;

            if (other.CompareTag("Player"))
            {
                Reveal();
                AttackPlayer(other.gameObject);
            }
        }

        /// <summary>
        /// Damages player and increases chase meter
        /// </summary>
        private void AttackPlayer(GameObject player)
        {
            var playerHealth = player.GetComponent<InfiniteHaus.Player.PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
                OnPlayerDamaged?.Invoke(damage);
            }

            // Increase chase meter
            var chaseSystem = FindObjectOfType<ChaseSystem>();
            if (chaseSystem != null)
            {
                chaseSystem.IncreaseChaseMeter(chaseIncrease);
            }

            // Disable after attack
            Deactivate();
        }

        /// <summary>
        /// Sets aggression level from ChaseSystem (0 to 1)
        /// Doesn't affect Mimic behavior directly, but can be used for spawn chance
        /// </summary>
        public void SetAggression(float level)
        {
            aggression = Mathf.Clamp01(level);
        }

        /// <summary>
        /// Activates this Mimic
        /// </summary>
        public void Activate()
        {
            isActive = true;
            isRevealed = false;

            if (mimicCollider != null)
            {
                mimicCollider.enabled = true;
            }

            ApplyDisguise();
        }

        /// <summary>
        /// Deactivates this Mimic
        /// </summary>
        public void Deactivate()
        {
            isActive = false;

            if (mimicCollider != null)
            {
                mimicCollider.enabled = false;
            }
        }

        /// <summary>
        /// Resets Mimic for reuse
        /// </summary>
        public void Reset()
        {
            isActive = false;
            isRevealed = false;
            aggression = 0f;
            ApplyDisguise();
        }

        /// <summary>
        /// Creates a Mimic disguised as a door at specified position
        /// </summary>
        public static GameObject CreateMimicDoor(GameObject mimicPrefab, Vector3 position)
        {
            GameObject mimic = Instantiate(mimicPrefab, position, Quaternion.identity);
            Mimic mimicComponent = mimic.GetComponent<Mimic>();

            if (mimicComponent != null)
            {
                mimicComponent.SetDisguise(DisguiseType.Door);
                mimicComponent.Activate();
            }

            return mimic;
        }

        /// <summary>
        /// Determines if Mimic should spawn based on aggression and chance
        /// </summary>
        public static bool ShouldSpawnMimic(float baseChance, float aggression)
        {
            float adjustedChance = baseChance * (1f + aggression);
            return Random.value < adjustedChance;
        }
    }
}
