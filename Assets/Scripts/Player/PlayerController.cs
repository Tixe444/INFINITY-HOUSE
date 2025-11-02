using UnityEngine;
using System;

namespace InfiniteHaus.Player
{
    /// <summary>
    /// Main player controller with auto-run, jump physics, coyote time, and jump buffering.
    /// Handles movement and provides animation state hooks.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Base auto-run speed (units per second)")]
        [SerializeField] private float baseSpeed = 5f;

        [Tooltip("Current speed multiplier (modified by hazards, powerups)")]
        [SerializeField] private float speedMultiplier = 1f;

        [Header("Jump Settings")]
        [Tooltip("Initial jump force")]
        [SerializeField] private float jumpForce = 12f;

        [Tooltip("Gravity scale when rising")]
        [SerializeField] private float jumpGravityScale = 2f;

        [Tooltip("Gravity scale when falling (faster fall)")]
        [SerializeField] private float fallGravityScale = 3f;

        [Tooltip("Maximum fall speed")]
        [SerializeField] private float maxFallSpeed = 20f;

        [Tooltip("Extra jump height multiplier when holding button")]
        [SerializeField] private float variableJumpMultiplier = 0.5f;

        [Header("Coyote Time & Jump Buffer")]
        [Tooltip("Time after leaving ground where jump is still allowed (seconds)")]
        [SerializeField] private float coyoteTime = 0.15f;

        [Tooltip("Time to buffer jump input before landing (seconds)")]
        [SerializeField] private float jumpBufferTime = 0.2f;

        [Header("Ground Detection")]
        [Tooltip("Layer mask for ground detection")]
        [SerializeField] private LayerMask groundLayer;

        [Tooltip("Ground check position offset from player center")]
        [SerializeField] private Vector2 groundCheckOffset = new Vector2(0, -0.5f);

        [Tooltip("Ground check box size")]
        [SerializeField] private Vector2 groundCheckSize = new Vector2(0.8f, 0.1f);

        [Header("State")]
        [Tooltip("Is player currently on ground?")]
        [SerializeField] private bool isGrounded;

        [Tooltip("Is player currently alive?")]
        [SerializeField] private bool isAlive = true;

        [Tooltip("Can player control movement?")]
        [SerializeField] private bool canMove = false;

        // Components
        private Rigidbody2D rb;
        private Collider2D col;

        // Jump state
        private float coyoteTimeCounter;
        private float jumpBufferCounter;
        private bool isJumping;
        private bool wasGrounded;

        // Public properties
        public bool IsGrounded => isGrounded;
        public bool IsAlive => isAlive;
        public bool IsJumping => isJumping;
        public bool IsFalling => rb.velocity.y < -0.1f;
        public bool IsRunning => isGrounded && Mathf.Abs(rb.velocity.x) > 0.1f;
        public float CurrentSpeed => baseSpeed * speedMultiplier;

        // Events for animation and game systems
        public event Action OnJump;
        public event Action OnLand;
        public event Action OnHurt;
        public event Action OnDeath;
        public event Action<float> OnSpeedChanged;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();
        }

        private void Start()
        {
            // Configure rigidbody
            rb.gravityScale = fallGravityScale;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        private void Update()
        {
            if (!isAlive) return;

            CheckGroundStatus();
            HandleCoyoteTime();
            HandleJumpBuffer();
            HandleJumpInput();
            UpdateGravity();
        }

        private void FixedUpdate()
        {
            if (!isAlive || !canMove) return;

            HandleAutoRun();
            ClampFallSpeed();
        }

        /// <summary>
        /// Checks if player is on the ground using box cast
        /// </summary>
        private void CheckGroundStatus()
        {
            wasGrounded = isGrounded;

            Vector2 checkPosition = (Vector2)transform.position + groundCheckOffset;
            isGrounded = Physics2D.OverlapBox(checkPosition, groundCheckSize, 0f, groundLayer);

            // Trigger landing event
            if (isGrounded && !wasGrounded)
            {
                isJumping = false;
                OnLand?.Invoke();
            }
        }

        /// <summary>
        /// Handles coyote time counter (grace period after leaving ground)
        /// </summary>
        private void HandleCoyoteTime()
        {
            if (isGrounded)
            {
                coyoteTimeCounter = coyoteTime;
            }
            else
            {
                coyoteTimeCounter -= Time.deltaTime;
            }
        }

        /// <summary>
        /// Handles jump buffer counter (queued jump input)
        /// </summary>
        private void HandleJumpBuffer()
        {
            if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space))
            {
                jumpBufferCounter = jumpBufferTime;
            }
            else
            {
                jumpBufferCounter -= Time.deltaTime;
            }
        }

        /// <summary>
        /// Processes jump input with buffering and coyote time
        /// </summary>
        private void HandleJumpInput()
        {
            if (!canMove) return;

            // Check if jump should execute (buffer + coyote time)
            if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f && !isJumping)
            {
                Jump();
            }

            // Variable jump height (release jump button early = shorter jump)
            if ((Input.GetButtonUp("Jump") || Input.GetKeyUp(KeyCode.Space)) && rb.velocity.y > 0f)
            {
                rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * variableJumpMultiplier);
            }
        }

        /// <summary>
        /// Executes jump
        /// </summary>
        private void Jump()
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            isJumping = true;
            coyoteTimeCounter = 0f;
            jumpBufferCounter = 0f;

            OnJump?.Invoke();
        }

        /// <summary>
        /// Adjusts gravity based on jump state for better feel
        /// </summary>
        private void UpdateGravity()
        {
            if (rb.velocity.y > 0f && isJumping)
            {
                rb.gravityScale = jumpGravityScale;
            }
            else if (rb.velocity.y < 0f)
            {
                rb.gravityScale = fallGravityScale;
            }
        }

        /// <summary>
        /// Handles automatic rightward movement
        /// </summary>
        private void HandleAutoRun()
        {
            float targetSpeed = CurrentSpeed;
            rb.velocity = new Vector2(targetSpeed, rb.velocity.y);
        }

        /// <summary>
        /// Prevents excessive fall speed
        /// </summary>
        private void ClampFallSpeed()
        {
            if (rb.velocity.y < -maxFallSpeed)
            {
                rb.velocity = new Vector2(rb.velocity.x, -maxFallSpeed);
            }
        }

        /// <summary>
        /// Enables player movement (called after countdown)
        /// </summary>
        public void EnableMovement()
        {
            canMove = true;
        }

        /// <summary>
        /// Disables player movement (pause, death, etc.)
        /// </summary>
        public void DisableMovement()
        {
            canMove = false;
        }

        /// <summary>
        /// Sets player speed (used by difficulty scaling and hazards)
        /// </summary>
        public void SetSpeed(float speed)
        {
            baseSpeed = speed;
            OnSpeedChanged?.Invoke(CurrentSpeed);
        }

        /// <summary>
        /// Applies speed multiplier (goo slows, powerups boost)
        /// </summary>
        public void SetSpeedMultiplier(float multiplier)
        {
            speedMultiplier = Mathf.Clamp(multiplier, 0.1f, 3f);
            OnSpeedChanged?.Invoke(CurrentSpeed);
        }

        /// <summary>
        /// Resets speed multiplier to normal
        /// </summary>
        public void ResetSpeedMultiplier()
        {
            speedMultiplier = 1f;
            OnSpeedChanged?.Invoke(CurrentSpeed);
        }

        /// <summary>
        /// Called when player takes damage
        /// </summary>
        public void TriggerHurt()
        {
            if (!isAlive) return;
            OnHurt?.Invoke();
        }

        /// <summary>
        /// Called when player dies
        /// </summary>
        public void TriggerDeath()
        {
            isAlive = false;
            canMove = false;
            rb.velocity = Vector2.zero;
            OnDeath?.Invoke();
        }

        /// <summary>
        /// Resets player state for new game
        /// </summary>
        public void ResetPlayer()
        {
            isAlive = true;
            canMove = false;
            isJumping = false;
            speedMultiplier = 1f;
            rb.velocity = Vector2.zero;
        }

        // Debug visualization
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Vector2 checkPosition = (Vector2)transform.position + groundCheckOffset;
            Gizmos.DrawWireCube(checkPosition, groundCheckSize);
        }
    }
}
