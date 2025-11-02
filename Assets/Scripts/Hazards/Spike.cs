using UnityEngine;

namespace InfiniteHaus.Hazards
{
    /// <summary>
    /// Spike hazard - Deals damage on contact with player.
    /// Simple contact-based damage with optional animation.
    /// </summary>
    public class Spike : HazardBase
    {
        [Header("Spike Settings")]
        [Tooltip("Can spikes be triggered multiple times?")]
        [SerializeField] private bool canTriggerMultipleTimes = true;

        [Tooltip("Cooldown between triggers (seconds)")]
        [SerializeField] private float triggerCooldown = 1f;

        private float lastTriggerTime = -999f;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!isActive) return;

            if (other.CompareTag("Player"))
            {
                // Check cooldown
                if (!canTriggerMultipleTimes && Time.time - lastTriggerTime < triggerCooldown)
                {
                    return;
                }

                lastTriggerTime = Time.time;
                TriggerHazard(other.gameObject);
            }
        }

        public override void Reset()
        {
            base.Reset();
            lastTriggerTime = -999f;
        }
    }
}
