using UnityEngine;

namespace InfiniteHaus.Collectibles
{
    /// <summary>
    /// Soul Shard collectible - Grants points and unlocks cosmetics.
    /// Primary collectible type found throughout levels.
    /// </summary>
    public class SoulShard : CollectibleBase
    {
        [Header("Soul Shard Settings")]
        [Tooltip("Points awarded for collecting this shard")]
        [SerializeField] private int pointValue = 10;

        [Tooltip("Glow color")]
        [SerializeField] private Color glowColor = new Color(0.5f, 0.8f, 1f);

        // Events
        public System.Action<int> OnPointsAwarded;

        protected override void Awake()
        {
            base.Awake();

            // Apply glow color to sprite
            if (spriteRenderer != null)
            {
                spriteRenderer.color = glowColor;
            }
        }

        protected override void Collect(GameObject player)
        {
            // Award points
            var rewardManager = FindObjectOfType<RewardManager>();
            if (rewardManager != null)
            {
                rewardManager.CollectSoulShard(pointValue);
            }

            OnPointsAwarded?.Invoke(pointValue);

            Debug.Log($"Collected Soul Shard! +{pointValue} points");

            base.Collect(player);
        }

        /// <summary>
        /// Sets the point value for this shard
        /// </summary>
        public void SetPointValue(int value)
        {
            pointValue = value;
        }
    }
}
