using UnityEngine;
using System;
using System.Collections;

namespace InfiniteHaus.Player
{
    /// <summary>
    /// Manages player health, damage, invincibility frames, and death.
    /// Integrates with PlayerController for hurt/death states.
    /// </summary>
    public class PlayerHealth : MonoBehaviour
    {
        [Header("Health Settings")]
        [Tooltip("Maximum health points")]
        [SerializeField] private int maxHealth = 3;

        [Tooltip("Current health points")]
        [SerializeField] private int currentHealth;

        [Header("Invincibility")]
        [Tooltip("Invincibility duration after taking damage (seconds)")]
        [SerializeField] private float invincibilityDuration = 0.8f;

        [Tooltip("Is player currently invincible?")]
        [SerializeField] private bool isInvincible = false;

        [Header("Visual Feedback")]
        [Tooltip("Sprite renderer for flash effect")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Tooltip("Flash color when damaged")]
        [SerializeField] private Color flashColor = Color.red;

        [Tooltip("Number of flashes during invincibility")]
        [SerializeField] private int flashCount = 5;

        // Components
        private PlayerController playerController;

        // Events
        public event Action<int, int> OnHealthChanged; // (current, max)
        public event Action<int> OnDamageTaken; // (damage amount)
        public event Action OnDeath;
        public event Action<int> OnHealthIncreased; // (new max health)

        // Properties
        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public bool IsInvincible => isInvincible;
        public float HealthPercentage => (float)currentHealth / maxHealth;

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        private void Start()
        {
            currentHealth = maxHealth;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        /// <summary>
        /// Applies damage to the player
        /// </summary>
        public void TakeDamage(int damage)
        {
            if (isInvincible || currentHealth <= 0) return;

            currentHealth = Mathf.Max(0, currentHealth - damage);
            OnDamageTaken?.Invoke(damage);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth > 0)
            {
                // Trigger hurt state
                playerController?.TriggerHurt();
                StartCoroutine(InvincibilityCoroutine());
            }
            else
            {
                // Player died
                Die();
            }
        }

        /// <summary>
        /// Heals the player
        /// </summary>
        public void Heal(int amount)
        {
            if (currentHealth >= maxHealth) return;

            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        /// <summary>
        /// Increases maximum health (from Relic collectibles)
        /// </summary>
        public void IncreaseMaxHealth(int amount)
        {
            maxHealth += amount;
            currentHealth += amount; // Also heal by the same amount
            OnHealthIncreased?.Invoke(maxHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        /// <summary>
        /// Handles player death
        /// </summary>
        private void Die()
        {
            playerController?.TriggerDeath();
            OnDeath?.Invoke();
        }

        /// <summary>
        /// Invincibility coroutine with visual feedback
        /// </summary>
        private IEnumerator InvincibilityCoroutine()
        {
            isInvincible = true;
            Color originalColor = spriteRenderer.color;
            float flashInterval = invincibilityDuration / (flashCount * 2);

            for (int i = 0; i < flashCount; i++)
            {
                // Flash to damage color
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = flashColor;
                }
                yield return new WaitForSeconds(flashInterval);

                // Flash back to original
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = originalColor;
                }
                yield return new WaitForSeconds(flashInterval);
            }

            // Ensure sprite is back to normal
            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
            }

            isInvincible = false;
        }

        /// <summary>
        /// Resets health to full for new game
        /// </summary>
        public void ResetHealth()
        {
            currentHealth = maxHealth;
            isInvincible = false;
            StopAllCoroutines();

            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
            }

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        /// <summary>
        /// Sets invincibility duration (for difficulty scaling)
        /// </summary>
        public void SetInvincibilityDuration(float duration)
        {
            invincibilityDuration = duration;
        }

        /// <summary>
        /// Instantly kills player (for pitfalls)
        /// </summary>
        public void InstantKill()
        {
            currentHealth = 0;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            Die();
        }
    }
}
