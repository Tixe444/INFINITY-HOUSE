using UnityEngine;

namespace InfiniteHaus.Player
{
    /// <summary>
    /// Manages player animations based on controller state.
    /// Subscribes to PlayerController events and updates Animator parameters.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(PlayerController))]
    public class PlayerAnimator : MonoBehaviour
    {
        [Header("Animation Settings")]
        [Tooltip("Minimum speed to trigger run animation")]
        [SerializeField] private float runSpeedThreshold = 0.1f;

        [Tooltip("Enable debug logging for animation states")]
        [SerializeField] private bool debugMode = false;

        // Animator parameter names (must match animator controller)
        private static readonly string ANIM_IS_RUNNING = "IsRunning";
        private static readonly string ANIM_IS_JUMPING = "IsJumping";
        private static readonly string ANIM_IS_FALLING = "IsFalling";
        private static readonly string ANIM_IS_GROUNDED = "IsGrounded";
        private static readonly string ANIM_SPEED = "Speed";
        private static readonly string ANIM_TRIGGER_HURT = "Hurt";
        private static readonly string ANIM_TRIGGER_DEATH = "Death";
        private static readonly string ANIM_TRIGGER_JUMP = "Jump";
        private static readonly string ANIM_TRIGGER_LAND = "Land";

        // Components
        private Animator animator;
        private PlayerController playerController;
        private PlayerHealth playerHealth;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            playerController = GetComponent<PlayerController>();
            playerHealth = GetComponent<PlayerHealth>();
        }

        private void OnEnable()
        {
            // Subscribe to player events
            if (playerController != null)
            {
                playerController.OnJump += HandleJump;
                playerController.OnLand += HandleLand;
                playerController.OnHurt += HandleHurt;
                playerController.OnDeath += HandleDeath;
            }

            if (playerHealth != null)
            {
                playerHealth.OnDeath += HandleDeath;
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from player events
            if (playerController != null)
            {
                playerController.OnJump -= HandleJump;
                playerController.OnLand -= HandleLand;
                playerController.OnHurt -= HandleHurt;
                playerController.OnDeath -= HandleDeath;
            }

            if (playerHealth != null)
            {
                playerHealth.OnDeath -= HandleDeath;
            }
        }

        private void Update()
        {
            if (animator == null || playerController == null) return;

            UpdateAnimatorParameters();
        }

        /// <summary>
        /// Updates animator parameters based on player state
        /// </summary>
        private void UpdateAnimatorParameters()
        {
            // Boolean states
            animator.SetBool(ANIM_IS_GROUNDED, playerController.IsGrounded);
            animator.SetBool(ANIM_IS_JUMPING, playerController.IsJumping);
            animator.SetBool(ANIM_IS_FALLING, playerController.IsFalling);
            animator.SetBool(ANIM_IS_RUNNING, playerController.IsRunning);

            // Speed parameter (for blend trees)
            animator.SetFloat(ANIM_SPEED, playerController.CurrentSpeed);

            if (debugMode)
            {
                LogAnimationState();
            }
        }

        /// <summary>
        /// Handles jump animation trigger
        /// </summary>
        private void HandleJump()
        {
            animator.SetTrigger(ANIM_TRIGGER_JUMP);

            if (debugMode)
            {
                Debug.Log("Animation: Jump triggered");
            }
        }

        /// <summary>
        /// Handles landing animation trigger
        /// </summary>
        private void HandleLand()
        {
            animator.SetTrigger(ANIM_TRIGGER_LAND);

            if (debugMode)
            {
                Debug.Log("Animation: Land triggered");
            }
        }

        /// <summary>
        /// Handles hurt animation trigger
        /// </summary>
        private void HandleHurt()
        {
            animator.SetTrigger(ANIM_TRIGGER_HURT);

            if (debugMode)
            {
                Debug.Log("Animation: Hurt triggered");
            }
        }

        /// <summary>
        /// Handles death animation trigger
        /// </summary>
        private void HandleDeath()
        {
            animator.SetTrigger(ANIM_TRIGGER_DEATH);

            if (debugMode)
            {
                Debug.Log("Animation: Death triggered");
            }
        }

        /// <summary>
        /// Debug logging for animation state
        /// </summary>
        private void LogAnimationState()
        {
            Debug.Log($"Anim State - Grounded: {playerController.IsGrounded}, " +
                      $"Jumping: {playerController.IsJumping}, " +
                      $"Falling: {playerController.IsFalling}, " +
                      $"Running: {playerController.IsRunning}, " +
                      $"Speed: {playerController.CurrentSpeed:F2}");
        }

        /// <summary>
        /// Manually trigger an animation state (for debugging)
        /// </summary>
        public void TriggerAnimation(string triggerName)
        {
            animator.SetTrigger(triggerName);
        }

        /// <summary>
        /// Resets all animator triggers and states
        /// </summary>
        public void ResetAnimator()
        {
            animator.Rebind();
            animator.Update(0f);
        }
    }
}
