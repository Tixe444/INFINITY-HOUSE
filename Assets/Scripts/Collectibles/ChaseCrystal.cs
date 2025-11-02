using UnityEngine;

namespace InfiniteHaus.Collectibles
{
    /// <summary>
    /// Chase Crystal collectible - Temporarily freezes Chase Meter.
    /// Provides strategic relief from monster pursuit.
    /// </summary>
    public class ChaseCrystal : CollectibleBase
    {
        [Header("Chase Crystal Settings")]
        [Tooltip("Duration to freeze chase meter (seconds)")]
        [SerializeField] private float freezeDuration = 3f;

        [Tooltip("Chase meter reduction amount")]
        [SerializeField] private float chaseReduction = 30f;

        [Tooltip("Crystal glow color")]
        [SerializeField] private Color glowColor = new Color(0.2f, 1f, 1f);

        [Header("Visual Effects")]
        [Tooltip("Pulsing glow effect")]
        [SerializeField] private bool enablePulse = true;

        [Tooltip("Pulse speed")]
        [SerializeField] private float pulseSpeed = 3f;

        [Tooltip("Pulse intensity")]
        [SerializeField][Range(0f, 1f)] private float pulseIntensity = 0.3f;

        private Color baseColor;

        // Events
        public System.Action<float> OnChaseFrozen;

        protected override void Awake()
        {
            base.Awake();

            baseColor = glowColor;

            if (spriteRenderer != null)
            {
                spriteRenderer.color = glowColor;
            }

            // Crystals float and rotate
            enableFloatAnimation = true;
            enableRotation = true;
        }

        protected override void Update()
        {
            base.Update();

            if (enablePulse && isActive)
            {
                AnimatePulse();
            }
        }

        /// <summary>
        /// Pulsing glow animation
        /// </summary>
        private void AnimatePulse()
        {
            if (spriteRenderer == null) return;

            float pulse = Mathf.Sin(Time.time * pulseSpeed) * pulseIntensity;
            Color pulseColor = baseColor * (1f + pulse);
            spriteRenderer.color = pulseColor;
        }

        protected override void Collect(GameObject player)
        {
            // Freeze chase meter via chase system
            var chaseSystem = FindObjectOfType<Monsters.ChaseSystem>();
            if (chaseSystem != null)
            {
                chaseSystem.OnChaseCrystalCollected(freezeDuration);
            }

            // Track in reward manager
            var rewardManager = FindObjectOfType<RewardManager>();
            if (rewardManager != null)
            {
                rewardManager.CollectChaseCrystal();
            }

            OnChaseFrozen?.Invoke(freezeDuration);

            Debug.Log($"Collected Chase Crystal! Chase meter frozen for {freezeDuration}s");

            base.Collect(player);
        }

        /// <summary>
        /// Sets freeze duration
        /// </summary>
        public void SetFreezeDuration(float duration)
        {
            freezeDuration = duration;
        }

        /// <summary>
        /// Sets chase reduction amount
        /// </summary>
        public void SetChaseReduction(float amount)
        {
            chaseReduction = amount;
        }
    }
}
