using UnityEngine;

namespace InfiniteHaus.Monsters
{
    /// <summary>
    /// The Shadow - Constant pursuer from the left.
    /// Uses rubberband speed logic to close in when player slows down.
    /// Triggers game over if it catches the player.
    /// </summary>
    public class Shadow : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Base movement speed")]
        [SerializeField] private float baseSpeed = 3f;

        [Tooltip("Maximum speed (when player is far ahead)")]
        [SerializeField] private float maxSpeed = 8f;

        [Tooltip("Minimum speed (when close to player)")]
        [SerializeField] private float minSpeed = 1f;

        [Header("Rubberband Logic")]
        [Tooltip("Target distance behind player")]
        [SerializeField] private float targetDistance = 5f;

        [Tooltip("Distance at which Shadow goes max speed")]
        [SerializeField] private float maxSpeedDistance = 15f;

        [Tooltip("Distance at which Shadow catches player")]
        [SerializeField] private float catchDistance = 1f;

        [Tooltip("How quickly speed adjusts to distance")]
        [SerializeField] private float speedAdjustmentRate = 0.1f;

        [Header("References")]
        [Tooltip("The player transform to chase")]
        [SerializeField] private Transform playerTransform;

        [Header("Visuals")]
        [Tooltip("Sprite renderer for visual effects")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Tooltip("Shadow becomes more visible as aggression increases")]
        [SerializeField] private bool fadeWithAggression = true;

        [Tooltip("Minimum alpha when not aggressive")]
        [SerializeField] private float minAlpha = 0.3f;

        [Tooltip("Maximum alpha when fully aggressive")]
        [SerializeField] private float maxAlpha = 1f;

        // State
        private float currentSpeed;
        private float aggression = 0f; // 0 to 1 from ChaseSystem
        private bool isActive = false;

        // Events
        public System.Action OnPlayerCaught;

        private void Start()
        {
            currentSpeed = baseSpeed;

            if (playerTransform == null)
            {
                playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        private void Update()
        {
            if (!isActive || playerTransform == null) return;

            UpdateSpeed();
            MoveTowardsPlayer();
            UpdateVisuals();
            CheckCatchCondition();
        }

        /// <summary>
        /// Updates Shadow's speed based on distance to player (rubberband logic)
        /// </summary>
        private void UpdateSpeed()
        {
            float distanceToPlayer = playerTransform.position.x - transform.position.x;

            // Calculate target speed based on distance
            float targetSpeed;
            if (distanceToPlayer > maxSpeedDistance)
            {
                // Player is far - go max speed
                targetSpeed = maxSpeed;
            }
            else if (distanceToPlayer < targetDistance)
            {
                // Too close - slow down
                targetSpeed = minSpeed;
            }
            else
            {
                // Interpolate based on distance
                float t = (distanceToPlayer - targetDistance) / (maxSpeedDistance - targetDistance);
                targetSpeed = Mathf.Lerp(baseSpeed, maxSpeed, t);
            }

            // Apply aggression multiplier (chase meter increases speed)
            targetSpeed *= (1f + aggression * 0.5f);

            // Smoothly adjust current speed
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, speedAdjustmentRate);
        }

        /// <summary>
        /// Moves Shadow towards player
        /// </summary>
        private void MoveTowardsPlayer()
        {
            transform.position += Vector3.right * currentSpeed * Time.deltaTime;
        }

        /// <summary>
        /// Updates visual appearance based on aggression
        /// </summary>
        private void UpdateVisuals()
        {
            if (spriteRenderer == null || !fadeWithAggression) return;

            // Fade in Shadow as chase meter increases
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, aggression);
            Color color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
        }

        /// <summary>
        /// Checks if Shadow has caught the player
        /// </summary>
        private void CheckCatchCondition()
        {
            float distanceToPlayer = playerTransform.position.x - transform.position.x;

            if (distanceToPlayer <= catchDistance)
            {
                CatchPlayer();
            }
        }

        /// <summary>
        /// Triggers player caught event
        /// </summary>
        private void CatchPlayer()
        {
            if (!isActive) return;

            isActive = false;
            OnPlayerCaught?.Invoke();
            Debug.Log("Shadow caught the player!");
        }

        /// <summary>
        /// Sets aggression level from ChaseSystem (0 to 1)
        /// </summary>
        public void SetAggression(float level)
        {
            aggression = Mathf.Clamp01(level);
        }

        /// <summary>
        /// Activates Shadow chase
        /// </summary>
        public void Activate()
        {
            isActive = true;
        }

        /// <summary>
        /// Deactivates Shadow chase
        /// </summary>
        public void Deactivate()
        {
            isActive = false;
        }

        /// <summary>
        /// Resets Shadow position for new game
        /// </summary>
        public void ResetPosition(Vector3 startPosition)
        {
            transform.position = startPosition;
            currentSpeed = baseSpeed;
            aggression = 0f;
            isActive = false;
        }

        /// <summary>
        /// Sets base speed (for difficulty scaling)
        /// </summary>
        public void SetBaseSpeed(float speed)
        {
            baseSpeed = speed;
        }

        // Debug visualization
        private void OnDrawGizmosSelected()
        {
            if (playerTransform == null) return;

            // Draw target distance
            Gizmos.color = Color.yellow;
            Vector3 targetPos = playerTransform.position - Vector3.right * targetDistance;
            Gizmos.DrawLine(transform.position, targetPos);

            // Draw catch distance
            Gizmos.color = Color.red;
            Vector3 catchPos = playerTransform.position - Vector3.right * catchDistance;
            Gizmos.DrawWireSphere(catchPos, 0.5f);
        }
    }
}
