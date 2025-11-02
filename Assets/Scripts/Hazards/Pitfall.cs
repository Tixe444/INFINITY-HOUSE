using UnityEngine;

namespace InfiniteHaus.Hazards
{
    /// <summary>
    /// Pitfall hazard - Instantly kills player on contact.
    /// Used for bottomless pits and fatal drops.
    /// </summary>
    public class Pitfall : HazardBase
    {
        [Header("Pitfall Settings")]
        [Tooltip("Instantly kill player (ignores health)")]
        [SerializeField] private bool instantKill = true;

        [Tooltip("Visual effect on player fall")]
        [SerializeField] private ParticleSystem fallEffect;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!isActive) return;

            if (other.CompareTag("Player"))
            {
                TriggerPitfall(other.gameObject);
            }
        }

        /// <summary>
        /// Triggers pitfall death
        /// </summary>
        private void TriggerPitfall(GameObject player)
        {
            var playerHealth = player.GetComponent<InfiniteHaus.Player.PlayerHealth>();

            if (playerHealth != null)
            {
                if (instantKill)
                {
                    playerHealth.InstantKill();
                }
                else
                {
                    playerHealth.TakeDamage(damage);
                }
            }

            // Trigger chase system
            var chaseSystem = FindObjectOfType<Monsters.ChaseSystem>();
            if (chaseSystem != null)
            {
                chaseSystem.OnPlayerMissedJump();
            }

            // Play effects
            if (fallEffect != null)
            {
                Instantiate(fallEffect, player.transform.position, Quaternion.identity);
            }

            PlayEffects();
            OnHazardTriggered?.Invoke(player);

            Debug.Log("Player fell into pitfall!");
        }
    }
}
