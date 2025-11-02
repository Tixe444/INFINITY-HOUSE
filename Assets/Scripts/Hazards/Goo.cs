using UnityEngine;
using System.Collections;

namespace InfiniteHaus.Hazards
{
    /// <summary>
    /// Goo puddle hazard - Slows player movement and weakens jump.
    /// Applies temporary debuffs while player is in contact.
    /// </summary>
    public class Goo : HazardBase
    {
        [Header("Goo Settings")]
        [Tooltip("Speed multiplier when in goo (0.5 = 50% speed)")]
        [SerializeField][Range(0.1f, 1f)] private float speedMultiplier = 0.5f;

        [Tooltip("Jump force multiplier when in goo")]
        [SerializeField][Range(0.1f, 1f)] private float jumpMultiplier = 0.7f;

        [Tooltip("Does goo deal damage?")]
        [SerializeField] private bool dealsDamage = false;

        [Tooltip("Damage interval if damage is enabled (seconds)")]
        [SerializeField] private float damageInterval = 2f;

        private InfiniteHaus.Player.PlayerController affectedPlayer;
        private Coroutine damageRoutine;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!isActive) return;

            if (other.CompareTag("Player"))
            {
                ApplySlowEffect(other.gameObject);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                RemoveSlowEffect(other.gameObject);
            }
        }

        /// <summary>
        /// Applies slow effect to player
        /// </summary>
        private void ApplySlowEffect(GameObject player)
        {
            affectedPlayer = player.GetComponent<InfiniteHaus.Player.PlayerController>();

            if (affectedPlayer != null)
            {
                affectedPlayer.SetSpeedMultiplier(speedMultiplier);
            }

            // Start damage over time if enabled
            if (dealsDamage)
            {
                damageRoutine = StartCoroutine(DamageOverTimeCoroutine(player));
            }

            PlayEffects();
        }

        /// <summary>
        /// Removes slow effect from player
        /// </summary>
        private void RemoveSlowEffect(GameObject player)
        {
            if (affectedPlayer != null)
            {
                affectedPlayer.ResetSpeedMultiplier();
                affectedPlayer = null;
            }

            // Stop damage routine
            if (damageRoutine != null)
            {
                StopCoroutine(damageRoutine);
                damageRoutine = null;
            }
        }

        /// <summary>
        /// Deals damage periodically while player is in goo
        /// </summary>
        private IEnumerator DamageOverTimeCoroutine(GameObject player)
        {
            while (true)
            {
                yield return new WaitForSeconds(damageInterval);
                TriggerHazard(player);
            }
        }

        private void OnDisable()
        {
            // Clean up if disabled while player is affected
            if (affectedPlayer != null)
            {
                affectedPlayer.ResetSpeedMultiplier();
                affectedPlayer = null;
            }
        }

        public override void Reset()
        {
            base.Reset();

            if (affectedPlayer != null)
            {
                affectedPlayer.ResetSpeedMultiplier();
                affectedPlayer = null;
            }

            if (damageRoutine != null)
            {
                StopCoroutine(damageRoutine);
                damageRoutine = null;
            }
        }
    }
}
