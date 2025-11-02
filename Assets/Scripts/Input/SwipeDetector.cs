using UnityEngine;
using System;

namespace InfiniteHaus.Input
{
    /// <summary>
    /// Detects swipe gestures on mobile devices.
    /// Provides directional swipe events (up, down, left, right).
    /// </summary>
    public class SwipeDetector : MonoBehaviour
    {
        [Header("Swipe Settings")]
        [Tooltip("Minimum swipe distance in pixels")]
        [SerializeField] private float minSwipeDistance = 50f;

        [Tooltip("Maximum swipe duration in seconds")]
        [SerializeField] private float maxSwipeDuration = 1f;

        [Tooltip("Angle tolerance for directional swipes (degrees)")]
        [SerializeField] private float directionThreshold = 30f;

        [Header("Visual Feedback")]
        [Tooltip("Show swipe trail for debugging")]
        [SerializeField] private bool showSwipeTrail = false;

        [Tooltip("Trail line renderer")]
        [SerializeField] private LineRenderer swipeTrail;

        [Header("Touch Areas")]
        [Tooltip("Top-left corner tap area (shop) in screen percentage")]
        [SerializeField] private Vector2 topLeftAreaSize = new Vector2(0.15f, 0.15f);

        [Tooltip("Top-right corner tap area (pause) in screen percentage")]
        [SerializeField] private Vector2 topRightAreaSize = new Vector2(0.15f, 0.15f);

        // Touch tracking
        private Vector2 touchStartPos;
        private float touchStartTime;
        private bool isSwiping = false;

        // Events
        public event Action OnSwipeUp;
        public event Action OnSwipeDown;
        public event Action OnSwipeLeft;
        public event Action OnSwipeRight;
        public event Action OnTapTopLeft;
        public event Action OnTapTopRight;
        public event Action<Vector2> OnSwipeDetected; // (swipe direction vector)

        // Properties
        public bool IsSwiping => isSwiping;

        private void Update()
        {
            if (Application.isMobilePlatform || UnityEngine.Input.touchCount > 0)
            {
                DetectTouchInput();
            }
            else if (showSwipeTrail)
            {
                // Mouse simulation for testing in editor
                DetectMouseInput();
            }
        }

        /// <summary>
        /// Detects touch input on mobile
        /// </summary>
        private void DetectTouchInput()
        {
            if (UnityEngine.Input.touchCount > 0)
            {
                Touch touch = UnityEngine.Input.GetTouch(0);

                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        OnTouchBegan(touch.position);
                        break;

                    case TouchPhase.Moved:
                        OnTouchMoved(touch.position);
                        break;

                    case TouchPhase.Ended:
                        OnTouchEnded(touch.position);
                        break;

                    case TouchPhase.Canceled:
                        OnTouchCanceled();
                        break;
                }
            }
        }

        /// <summary>
        /// Detects mouse input for testing in editor
        /// </summary>
        private void DetectMouseInput()
        {
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                OnTouchBegan(UnityEngine.Input.mousePosition);
            }
            else if (UnityEngine.Input.GetMouseButton(0) && isSwiping)
            {
                OnTouchMoved(UnityEngine.Input.mousePosition);
            }
            else if (UnityEngine.Input.GetMouseButtonUp(0))
            {
                OnTouchEnded(UnityEngine.Input.mousePosition);
            }
        }

        /// <summary>
        /// Called when touch begins
        /// </summary>
        private void OnTouchBegan(Vector2 position)
        {
            touchStartPos = position;
            touchStartTime = Time.time;
            isSwiping = true;

            if (showSwipeTrail && swipeTrail != null)
            {
                swipeTrail.enabled = true;
                swipeTrail.SetPosition(0, Camera.main.ScreenToWorldPoint(new Vector3(position.x, position.y, 10f)));
            }
        }

        /// <summary>
        /// Called when touch moves
        /// </summary>
        private void OnTouchMoved(Vector2 position)
        {
            if (!isSwiping) return;

            if (showSwipeTrail && swipeTrail != null)
            {
                swipeTrail.SetPosition(1, Camera.main.ScreenToWorldPoint(new Vector3(position.x, position.y, 10f)));
            }
        }

        /// <summary>
        /// Called when touch ends
        /// </summary>
        private void OnTouchEnded(Vector2 position)
        {
            if (!isSwiping) return;

            float swipeDuration = Time.time - touchStartTime;
            Vector2 swipeDelta = position - touchStartPos;
            float swipeDistance = swipeDelta.magnitude;

            // Check for corner taps (short duration, small movement)
            if (swipeDuration < 0.3f && swipeDistance < minSwipeDistance * 0.5f)
            {
                CheckCornerTaps(position);
            }
            // Check for swipe
            else if (swipeDistance >= minSwipeDistance && swipeDuration <= maxSwipeDuration)
            {
                ProcessSwipe(swipeDelta);
            }

            OnTouchCanceled();
        }

        /// <summary>
        /// Called when touch is canceled
        /// </summary>
        private void OnTouchCanceled()
        {
            isSwiping = false;

            if (showSwipeTrail && swipeTrail != null)
            {
                swipeTrail.enabled = false;
            }
        }

        /// <summary>
        /// Processes swipe direction and triggers appropriate event
        /// </summary>
        private void ProcessSwipe(Vector2 swipeDelta)
        {
            Vector2 normalizedSwipe = swipeDelta.normalized;
            float angle = Mathf.Atan2(normalizedSwipe.y, normalizedSwipe.x) * Mathf.Rad2Deg;

            OnSwipeDetected?.Invoke(normalizedSwipe);

            // Determine primary direction
            if (Mathf.Abs(normalizedSwipe.x) > Mathf.Abs(normalizedSwipe.y))
            {
                // Horizontal swipe
                if (normalizedSwipe.x > 0)
                {
                    OnSwipeRight?.Invoke();
                    Debug.Log("Swipe Right detected");
                }
                else
                {
                    OnSwipeLeft?.Invoke();
                    Debug.Log("Swipe Left detected");
                }
            }
            else
            {
                // Vertical swipe
                if (normalizedSwipe.y > 0)
                {
                    OnSwipeUp?.Invoke();
                    Debug.Log("Swipe Up detected");
                }
                else
                {
                    OnSwipeDown?.Invoke();
                    Debug.Log("Swipe Down detected");
                }
            }
        }

        /// <summary>
        /// Checks for corner tap gestures
        /// </summary>
        private void CheckCornerTaps(Vector2 position)
        {
            // Normalize to screen space (0-1)
            Vector2 normalizedPos = new Vector2(
                position.x / Screen.width,
                position.y / Screen.height
            );

            // Check top-left (shop button)
            if (normalizedPos.x <= topLeftAreaSize.x &&
                normalizedPos.y >= (1f - topLeftAreaSize.y))
            {
                OnTapTopLeft?.Invoke();
                Debug.Log("Tap Top-Left detected (Shop)");
            }
            // Check top-right (pause button)
            else if (normalizedPos.x >= (1f - topRightAreaSize.x) &&
                     normalizedPos.y >= (1f - topRightAreaSize.y))
            {
                OnTapTopRight?.Invoke();
                Debug.Log("Tap Top-Right detected (Pause)");
            }
        }

        /// <summary>
        /// Enables/disables swipe detection
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            this.enabled = enabled;

            if (!enabled)
            {
                OnTouchCanceled();
            }
        }

        // Debug visualization
        private void OnGUI()
        {
            if (!showSwipeTrail || !Application.isEditor) return;

            // Draw corner tap areas
            GUI.color = new Color(1f, 1f, 0f, 0.3f);

            // Top-left
            GUI.Box(new Rect(0, 0, Screen.width * topLeftAreaSize.x, Screen.height * topLeftAreaSize.y), "SHOP");

            // Top-right
            GUI.Box(new Rect(
                Screen.width * (1f - topRightAreaSize.x),
                0,
                Screen.width * topRightAreaSize.x,
                Screen.height * topRightAreaSize.y
            ), "PAUSE");

            GUI.color = Color.white;
        }
    }
}
