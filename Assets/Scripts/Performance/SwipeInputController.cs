// ═══════════════════════════════════════════════════════════════════════════════
// INFINITY HOUSE v5.5 - Made by Mate Makovics
// Buttonless Swipe Input Controller - Ultra-Responsive Controls
// ═══════════════════════════════════════════════════════════════════════════════

using UnityEngine;
using System;
using System.Collections.Generic;

namespace InfinityHouse.Performance
{
    /// <summary>
    /// Buttonless swipe and keyboard input with <80ms latency.
    /// Mobile: SwipeUp=jump, SwipeDown=slide, SwipeLeft/Right=doors, TapTR=pause, TapTL=shop
    /// PC: W=jump, S=slide, A/D=doors, Esc=pause, Tab/I=shop
    /// Features: 120ms buffer, 120ms coyote time, 10px deadzone, 22.5° angle snap.
    /// </summary>
    public class SwipeInputController : MonoBehaviour
    {
        // Singleton
        public static SwipeInputController Instance { get; private set; }

        [Header("Input Configuration")]
        [SerializeField] private float swipeDeadZone = 10f; // pixels
        [SerializeField] private float swipeAngleSnap = 22.5f; // degrees
        [SerializeField] private float inputBufferTime = 0.12f; // 120ms
        [SerializeField] private float coyoteTime = 0.12f; // 120ms

        [Header("Tap Zones (Normalized Screen)")]
        [SerializeField] private Rect topRightZone = new Rect(0.75f, 0.75f, 0.25f, 0.25f);
        [SerializeField] private Rect topLeftZone = new Rect(0f, 0.75f, 0.25f, 0.25f);

        [Header("Input Latency Tracking")]
        [SerializeField] private bool trackLatency = true;
        [SerializeField] private int latencySampleSize = 60;

        // Touch tracking
        private Vector2 touchStartPos;
        private float touchStartTime;
        private bool isTouching;

        // Input buffering
        private Dictionary<InputAction, float> inputBuffer;
        private float lastGroundedTime;

        // Latency tracking
        private Queue<float> latencySamples;
        private float averageLatency;

        // Events
        public event Action OnJump;
        public event Action OnSlide;
        public event Action OnDoorLeft;
        public event Action OnDoorRight;
        public event Action OnPause;
        public event Action OnShop;

        public enum InputAction
        {
            Jump,
            Slide,
            DoorLeft,
            DoorRight
        }

        // Properties
        public float AverageInputLatency => averageLatency;
        public bool IsBuffering => inputBuffer.Count > 0;

        private void Awake()
        {
            // Singleton
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            Initialize();
        }

        private void Initialize()
        {
            inputBuffer = new Dictionary<InputAction, float>();
            latencySamples = new Queue<float>(latencySampleSize);

            // Subscribe to performance manager
            if (PerformanceManager.Instance != null)
            {
                PerformanceManager.Instance.OnPreTick += ProcessInput;
            }

            Debug.Log("[SwipeInputController] Initialized buttonless input system");
        }

        /// <summary>
        /// Process input (called by PerformanceManager PreTick)
        /// </summary>
        private void ProcessInput()
        {
            float inputStartTime = Time.realtimeSinceStartup;

            #if UNITY_IOS || UNITY_ANDROID || UNITY_EDITOR
            ProcessTouchInput();
            #endif

            #if UNITY_STANDALONE || UNITY_EDITOR
            ProcessKeyboardInput();
            #endif

            ProcessInputBuffer();

            if (trackLatency)
            {
                float latency = (Time.realtimeSinceStartup - inputStartTime) * 1000f;
                TrackLatency(latency);
            }
        }

        #region Touch Input

        private void ProcessTouchInput()
        {
            if (Input.touchCount == 0 && !Input.GetMouseButton(0)) return;

            Vector2 touchPos = Input.touchCount > 0 ? Input.GetTouch(0).position : (Vector2)Input.mousePosition;

            // Touch start
            if ((Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) || Input.GetMouseButtonDown(0))
            {
                touchStartPos = touchPos;
                touchStartTime = Time.time;
                isTouching = true;

                // Check tap zones
                CheckTapZones(touchPos);
            }

            // Touch move/end
            if (isTouching && ((Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended) || Input.GetMouseButtonUp(0)))
            {
                DetectSwipe(touchPos);
                isTouching = false;
            }
        }

        private void CheckTapZones(Vector2 touchPos)
        {
            // Normalize touch position
            Vector2 normalizedPos = new Vector2(touchPos.x / Screen.width, touchPos.y / Screen.height);

            // Check top-right (pause)
            if (topRightZone.Contains(normalizedPos))
            {
                TriggerInput(InputAction.Jump, "tap_pause");
                OnPause?.Invoke();
                TrackInputEvent("pause", "tap", 0f);
            }

            // Check top-left (shop)
            if (topLeftZone.Contains(normalizedPos))
            {
                TriggerInput(InputAction.Jump, "tap_shop");
                OnShop?.Invoke();
                TrackInputEvent("shop", "tap", 0f);
            }
        }

        private void DetectSwipe(Vector2 touchEndPos)
        {
            Vector2 swipeDelta = touchEndPos - touchStartPos;
            float swipeDistance = swipeDelta.magnitude;

            // Check dead zone
            if (swipeDistance < swipeDeadZone)
            {
                TrackInputEvent("swipe_miss", "too_short", swipeDistance);
                return;
            }

            // Calculate swipe angle
            float swipeAngle = Mathf.Atan2(swipeDelta.y, swipeDelta.x) * Mathf.Rad2Deg;

            // Snap to cardinal directions with angle snap tolerance
            InputAction? action = GetSwipeAction(swipeAngle);

            if (action.HasValue)
            {
                float swipeTime = Time.time - touchStartTime;
                TriggerInput(action.Value, "swipe");
                TrackInputEvent(action.Value.ToString(), "swipe", swipeTime * 1000f);
            }
            else
            {
                TrackInputEvent("swipe_miss", "invalid_angle", swipeAngle);
            }
        }

        private InputAction? GetSwipeAction(float angle)
        {
            // Normalize angle to 0-360
            if (angle < 0) angle += 360;

            // Up: 45-135 degrees
            if (angle >= 90 - swipeAngleSnap && angle <= 90 + swipeAngleSnap)
            {
                OnJump?.Invoke();
                return InputAction.Jump;
            }

            // Down: 225-315 degrees
            if (angle >= 270 - swipeAngleSnap && angle <= 270 + swipeAngleSnap)
            {
                OnSlide?.Invoke();
                return InputAction.Slide;
            }

            // Left: 135-225 degrees
            if (angle >= 180 - swipeAngleSnap && angle <= 180 + swipeAngleSnap)
            {
                OnDoorLeft?.Invoke();
                return InputAction.DoorLeft;
            }

            // Right: 315-45 degrees
            if ((angle >= 0 && angle <= swipeAngleSnap) || (angle >= 360 - swipeAngleSnap && angle <= 360))
            {
                OnDoorRight?.Invoke();
                return InputAction.DoorRight;
            }

            return null;
        }

        #endregion

        #region Keyboard Input

        private void ProcessKeyboardInput()
        {
            // Jump: W or Space
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space))
            {
                TriggerInput(InputAction.Jump, "keyboard");
                OnJump?.Invoke();
                TrackInputEvent("jump", "keyboard", 0f);
            }

            // Slide: S
            if (Input.GetKeyDown(KeyCode.S))
            {
                TriggerInput(InputAction.Slide, "keyboard");
                OnSlide?.Invoke();
                TrackInputEvent("slide", "keyboard", 0f);
            }

            // Door Left: A
            if (Input.GetKeyDown(KeyCode.A))
            {
                TriggerInput(InputAction.DoorLeft, "keyboard");
                OnDoorLeft?.Invoke();
                TrackInputEvent("door_left", "keyboard", 0f);
            }

            // Door Right: D
            if (Input.GetKeyDown(KeyCode.D))
            {
                TriggerInput(InputAction.DoorRight, "keyboard");
                OnDoorRight?.Invoke();
                TrackInputEvent("door_right", "keyboard", 0f);
            }

            // Pause: Escape
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                OnPause?.Invoke();
                TrackInputEvent("pause", "keyboard", 0f);
            }

            // Shop: Tab or I
            if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.I))
            {
                OnShop?.Invoke();
                TrackInputEvent("shop", "keyboard", 0f);
            }
        }

        #endregion

        #region Input Buffering

        private void TriggerInput(InputAction action, string source)
        {
            // Add to buffer
            inputBuffer[action] = Time.time;
        }

        private void ProcessInputBuffer()
        {
            // Remove expired buffered inputs
            List<InputAction> expiredActions = new List<InputAction>();

            foreach (var kvp in inputBuffer)
            {
                if (Time.time - kvp.Value > inputBufferTime)
                {
                    expiredActions.Add(kvp.Key);
                }
            }

            foreach (var action in expiredActions)
            {
                inputBuffer.Remove(action);
            }
        }

        /// <summary>
        /// Checks if input action is buffered
        /// </summary>
        public bool IsInputBuffered(InputAction action)
        {
            return inputBuffer.ContainsKey(action);
        }

        /// <summary>
        /// Consumes buffered input
        /// </summary>
        public bool ConsumeBufferedInput(InputAction action)
        {
            if (inputBuffer.ContainsKey(action))
            {
                inputBuffer.Remove(action);
                return true;
            }
            return false;
        }

        #endregion

        #region Coyote Time

        /// <summary>
        /// Updates grounded state for coyote time
        /// </summary>
        public void UpdateGroundedState(bool isGrounded)
        {
            if (isGrounded)
            {
                lastGroundedTime = Time.time;
            }
        }

        /// <summary>
        /// Checks if coyote time is active
        /// </summary>
        public bool IsCoyoteTimeActive()
        {
            return Time.time - lastGroundedTime <= coyoteTime;
        }

        #endregion

        #region Latency Tracking

        private void TrackLatency(float latencyMs)
        {
            latencySamples.Enqueue(latencyMs);

            if (latencySamples.Count > latencySampleSize)
            {
                latencySamples.Dequeue();
            }

            // Calculate average
            float sum = 0f;
            foreach (float sample in latencySamples)
            {
                sum += sample;
            }
            averageLatency = sum / latencySamples.Count;
        }

        private void TrackInputEvent(string actionType, string inputType, float durationMs)
        {
            Analytics.TelemetryEvents.Instance?.TrackEvent("input_action", new Dictionary<string, object>
            {
                { "type", actionType },
                { "input_type", inputType },
                { "latency_ms", averageLatency },
                { "duration_ms", durationMs }
            });
        }

        #endregion

        #region Debug

        private void OnGUI()
        {
            if (!trackLatency) return;

            GUIStyle style = new GUIStyle();
            style.normal.textColor = averageLatency < 80f ? Color.green : (averageLatency < 120f ? Color.yellow : Color.red);
            style.fontSize = 16;

            GUI.Label(new Rect(10, 190, 300, 30), $"Input Latency: {averageLatency:F1}ms (Target: <80ms)", style);
            if (inputBuffer.Count > 0)
            {
                GUI.Label(new Rect(10, 220, 300, 30), $"Buffered Inputs: {inputBuffer.Count}", style);
            }
        }

        #endregion

        private void OnDestroy()
        {
            if (PerformanceManager.Instance != null)
            {
                PerformanceManager.Instance.OnPreTick -= ProcessInput;
            }
        }
    }
}
