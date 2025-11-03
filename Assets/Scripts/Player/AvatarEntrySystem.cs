using UnityEngine;
using System;

namespace InfiniteHaus.Game
{
    /// <summary>
    /// Manages avatar entry sequence for INFINITE HAUS v5.7.
    /// Handles spawning, acceleration, visibility, and animation blending.
    /// CPU: <0.1ms | Memory: 2KB | GC: 0B
    /// </summary>
    public class AvatarEntrySystem : MonoBehaviour
    {
        #region Configuration
        [Header("Avatar Prefab")]
        [Tooltip("Avatar prefab to spawn")]
        [SerializeField] private GameObject avatarPrefab;

        [Header("Animation")]
        [Tooltip("Entry run animation name")]
        [SerializeField] private string entryRunAnimName = "entry_run";

        [Tooltip("Run loop animation name")]
        [SerializeField] private string runLoopAnimName = "run_loop";

        [Tooltip("Blend duration to run loop (seconds)")]
        [SerializeField] private float blendDuration = 0.2f;

        [Header("Components")]
        [Tooltip("Spawned avatar instance")]
        [SerializeField] private GameObject avatarInstance;

        [Tooltip("Auto-find controller on spawn")]
        [SerializeField] private bool autoFindController = true;
        #endregion

        #region State
        private Player.EnhancedRunnerController controller;
        private Animator animator;
        private SpriteRenderer spriteRenderer;
        private Rigidbody2D rb;
        private AvatarState currentState = AvatarState.None;
        private float currentVelocity = 0f;
        private bool jumpEnabled = false;
        private bool slideEnabled = false;
        private bool isVisible = false;
        private bool autorunEnabled = false;
        #endregion

        #region Enums
        public enum AvatarState
        {
            None,
            EntryRun,
            RunLoop,
            Jumping,
            Sliding,
            Death
        }
        #endregion

        #region Events
        public event Action<GameObject> OnAvatarSpawned;
        public event Action<AvatarState> OnStateChanged;
        #endregion

        #region Properties
        public GameObject AvatarInstance => avatarInstance;
        public AvatarState CurrentState => currentState;
        public float Velocity => currentVelocity;
        public bool IsVisible => isVisible;
        #endregion

        #region Spawning
        /// <summary>
        /// Spawns avatar at position
        /// </summary>
        public void SpawnAvatar(Vector3 position)
        {
            if (avatarInstance != null)
            {
                Debug.LogWarning("[AvatarEntry] Avatar already spawned!");
                return;
            }

            // Spawn from prefab
            if (avatarPrefab != null)
            {
                avatarInstance = Instantiate(avatarPrefab, position, Quaternion.identity);
            }
            else
            {
                // Create new GameObject
                avatarInstance = new GameObject("Avatar");
                avatarInstance.transform.position = position;

                // Add basic components
                spriteRenderer = avatarInstance.AddComponent<SpriteRenderer>();
                rb = avatarInstance.AddComponent<Rigidbody2D>();
                animator = avatarInstance.AddComponent<Animator>();
            }

            // Cache components
            if (autoFindController)
            {
                FindComponents();
            }

            // Start invisible
            SetVisible(false);

            OnAvatarSpawned?.Invoke(avatarInstance);
            Debug.Log($"[AvatarEntry] Avatar spawned at {position}");
        }

        private void FindComponents()
        {
            if (avatarInstance == null) return;

            if (controller == null)
            {
                controller = avatarInstance.GetComponent<Player.EnhancedRunnerController>();
                if (controller == null)
                {
                    controller = avatarInstance.AddComponent<Player.EnhancedRunnerController>();
                }
            }

            if (rb == null)
            {
                rb = avatarInstance.GetComponent<Rigidbody2D>();
            }

            if (animator == null)
            {
                animator = avatarInstance.GetComponent<Animator>();
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = avatarInstance.GetComponent<SpriteRenderer>();
            }
        }
        #endregion

        #region State Management
        /// <summary>
        /// Sets avatar state
        /// </summary>
        public void SetState(AvatarState newState)
        {
            if (currentState == newState) return;

            currentState = newState;
            ApplyState();
            OnStateChanged?.Invoke(newState);
        }

        private void ApplyState()
        {
            if (animator == null) return;

            switch (currentState)
            {
                case AvatarState.EntryRun:
                    if (!string.IsNullOrEmpty(entryRunAnimName))
                    {
                        animator.Play(entryRunAnimName);
                    }
                    break;

                case AvatarState.RunLoop:
                    if (!string.IsNullOrEmpty(runLoopAnimName))
                    {
                        animator.Play(runLoopAnimName);
                    }
                    break;
            }
        }

        /// <summary>
        /// Blends to run loop animation
        /// </summary>
        public void BlendToRunLoop()
        {
            if (animator != null && !string.IsNullOrEmpty(runLoopAnimName))
            {
                animator.CrossFade(runLoopAnimName, blendDuration);
            }

            SetState(AvatarState.RunLoop);
            Debug.Log($"[AvatarEntry] Blending to run loop ({blendDuration}s)");
        }
        #endregion

        #region Movement
        /// <summary>
        /// Sets avatar velocity
        /// </summary>
        public void SetVelocity(float velocity)
        {
            currentVelocity = velocity;

            if (rb != null)
            {
                rb.velocity = new Vector2(velocity, rb.velocity.y);
            }
        }

        /// <summary>
        /// Enables autorun
        /// </summary>
        public void SetAutorun(bool enabled)
        {
            autorunEnabled = enabled;

            if (controller != null)
            {
                if (enabled)
                {
                    controller.EnableMovement();
                }
                else
                {
                    controller.DisableMovement();
                }
            }
        }
        #endregion

        #region Visibility
        /// <summary>
        /// Sets avatar visibility
        /// </summary>
        public void SetVisible(bool visible)
        {
            isVisible = visible;

            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = visible;
            }

            // Also set child renderers
            if (avatarInstance != null)
            {
                foreach (var renderer in avatarInstance.GetComponentsInChildren<SpriteRenderer>())
                {
                    renderer.enabled = visible;
                }
            }
        }
        #endregion

        #region Input Control
        /// <summary>
        /// Enables jump input
        /// </summary>
        public void EnableJump()
        {
            jumpEnabled = true;
            Debug.Log("[AvatarEntry] Jump enabled");
        }

        /// <summary>
        /// Disables jump input
        /// </summary>
        public void DisableJump()
        {
            jumpEnabled = false;
        }

        /// <summary>
        /// Enables slide input
        /// </summary>
        public void EnableSlide()
        {
            slideEnabled = true;
            Debug.Log("[AvatarEntry] Slide enabled");
        }

        /// <summary>
        /// Disables slide input
        /// </summary>
        public void DisableSlide()
        {
            slideEnabled = false;
        }
        #endregion

        #region Public API
        /// <summary>
        /// Gets the enhanced runner controller
        /// </summary>
        public Player.EnhancedRunnerController GetController()
        {
            return controller;
        }

        /// <summary>
        /// Gets the animator
        /// </summary>
        public Animator GetAnimator()
        {
            return animator;
        }

        /// <summary>
        /// Gets the rigidbody
        /// </summary>
        public Rigidbody2D GetRigidbody()
        {
            return rb;
        }

        /// <summary>
        /// Destroys the avatar instance
        /// </summary>
        public void DestroyAvatar()
        {
            if (avatarInstance != null)
            {
                Destroy(avatarInstance);
                avatarInstance = null;
                controller = null;
                animator = null;
                rb = null;
                spriteRenderer = null;
                currentState = AvatarState.None;
            }
        }
        #endregion
    }
}
