using UnityEngine;

namespace InfiniteHaus.Collectibles
{
    /// <summary>
    /// Rare Relic collectible - Grants permanent health upgrades.
    /// Collecting 3 in a run increases max HP by 1.
    /// Stored between sessions.
    /// </summary>
    public class Relic : CollectibleBase
    {
        [Header("Relic Settings")]
        [Tooltip("Glow color (purple for rarity)")]
        [SerializeField] private Color glowColor = new Color(0.8f, 0.3f, 1f);

        [Tooltip("Relic rarity tier")]
        [SerializeField] private RelicTier tier = RelicTier.Common;

        [Header("Spawn Conditions")]
        [Tooltip("Only spawn after difficult jumps?")]
        [SerializeField] private bool requireDifficultJump = true;

        // Events
        public System.Action OnRelicCollected;

        public enum RelicTier
        {
            Common,
            Rare,
            Epic,
            Legendary
        }

        protected override void Awake()
        {
            base.Awake();

            // Apply glow color based on tier
            if (spriteRenderer != null)
            {
                spriteRenderer.color = GetTierColor();
            }

            // Relics always rotate
            enableRotation = true;
        }

        protected override void Collect(GameObject player)
        {
            // Add relic to reward manager
            var rewardManager = FindObjectOfType<RewardManager>();
            if (rewardManager != null)
            {
                rewardManager.CollectRelic();
            }

            OnRelicCollected?.Invoke();

            Debug.Log($"Collected {tier} Relic! Progress toward permanent HP upgrade.");

            base.Collect(player);
        }

        /// <summary>
        /// Gets color based on rarity tier
        /// </summary>
        private Color GetTierColor()
        {
            switch (tier)
            {
                case RelicTier.Common:
                    return new Color(0.7f, 0.7f, 0.7f); // Gray
                case RelicTier.Rare:
                    return new Color(0.3f, 0.5f, 1f); // Blue
                case RelicTier.Epic:
                    return new Color(0.8f, 0.3f, 1f); // Purple
                case RelicTier.Legendary:
                    return new Color(1f, 0.7f, 0.2f); // Gold
                default:
                    return glowColor;
            }
        }

        /// <summary>
        /// Sets the relic tier
        /// </summary>
        public void SetTier(RelicTier newTier)
        {
            tier = newTier;

            if (spriteRenderer != null)
            {
                spriteRenderer.color = GetTierColor();
            }
        }
    }
}
