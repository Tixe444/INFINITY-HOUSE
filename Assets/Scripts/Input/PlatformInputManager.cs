using UnityEngine;
using System;

namespace InfiniteHaus.Input
{
    /// <summary>
    /// Unified cross-platform input manager.
    /// Automatically switches between keyboard (PC/Mac) and touch/gesture (iOS/Android) input.
    /// Abstracts input into game actions.
    /// </summary>
    public class PlatformInputManager : MonoBehaviour
    {
        [Header("Platform Detection")]
        [Tooltip("Force desktop input (for testing touch on PC)")]
        [SerializeField] private bool forceDesktopInput = false;

        [Tooltip("Force mobile input (for testing mobile on PC)")]
        [SerializeField] private bool forceMobileInput = false;

        [Header("Desktop Key Bindings")]
        [Tooltip("Jump key")]
        public KeyCode jumpKey = KeyCode.W;

        [Tooltip("Left door/choice key")]
        public KeyCode leftDoorKey = KeyCode.A;

        [Tooltip("Right door/choice key")]
        public KeyCode rightDoorKey = KeyCode.D;

        [Tooltip("Pause key")]
        public KeyCode pauseKey = KeyCode.Escape;

        [Tooltip("Shop key")]
        public KeyCode shopKey = KeyCode.Tab;

        [Tooltip("Alternative shop key")]
        public KeyCode shopKeyAlt = KeyCode.I;

        [Header("Mobile Gesture Settings")]
        [Tooltip("Swipe detector component")]
        [SerializeField] private SwipeDetector swipeDetector;

        [Header("Input State")]
        [Tooltip("Is input currently enabled?")]
        [SerializeField] private bool inputEnabled = true;

        // Platform state
        private InputPlatform currentPlatform;
        private bool isMobilePlatform;

        // Singleton
        public static PlatformInputManager Instance { get; private set; }

        // Events for game actions
        public event Action OnJumpPressed;
        public event Action OnJumpReleased;
        public event Action OnLeftDoorSelected;
        public event Action OnRightDoorSelected;
        public event Action OnPausePressed;
        public event Action OnShopPressed;

        // Properties
        public bool InputEnabled => inputEnabled;
        public InputPlatform CurrentPlatform => currentPlatform;
        public bool IsMobile => isMobilePlatform;

        public enum InputPlatform
        {
            Desktop,    // PC/Mac with keyboard
            Mobile      // iOS/Android with touch
        }

        private void Awake()
        {
            // Singleton
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            DetectPlatform();
            InitializeInput();
        }

        private void OnEnable()
        {
            if (swipeDetector != null)
            {
                swipeDetector.OnSwipeUp += HandleSwipeUp;
                swipeDetector.OnSwipeLeft += HandleSwipeLeft;
                swipeDetector.OnSwipeRight += HandleSwipeRight;
                swipeDetector.OnTapTopLeft += HandleTapTopLeft;
                swipeDetector.OnTapTopRight += HandleTapTopRight;
            }
        }

        private void OnDisable()
        {
            if (swipeDetector != null)
            {
                swipeDetector.OnSwipeUp -= HandleSwipeUp;
                swipeDetector.OnSwipeLeft -= HandleSwipeLeft;
                swipeDetector.OnSwipeRight -= HandleSwipeRight;
                swipeDetector.OnTapTopLeft -= HandleTapTopLeft;
                swipeDetector.OnTapTopRight -= HandleTapTopRight;
            }
        }

        private void Update()
        {
            if (!inputEnabled) return;

            if (currentPlatform == InputPlatform.Desktop)
            {
                HandleDesktopInput();
            }
            // Mobile input handled via SwipeDetector events
        }

        /// <summary>
        /// Detects current platform
        /// </summary>
        private void DetectPlatform()
        {
            if (forceDesktopInput)
            {
                currentPlatform = InputPlatform.Desktop;
                isMobilePlatform = false;
            }
            else if (forceMobileInput)
            {
                currentPlatform = InputPlatform.Mobile;
                isMobilePlatform = true;
            }
            else
            {
                #if UNITY_IOS || UNITY_ANDROID
                currentPlatform = InputPlatform.Mobile;
                isMobilePlatform = true;
                #else
                currentPlatform = InputPlatform.Desktop;
                isMobilePlatform = false;
                #endif
            }

            Debug.Log($"Input Platform: {currentPlatform}");
        }

        /// <summary>
        /// Initializes platform-specific input
        /// </summary>
        private void InitializeInput()
        {
            if (currentPlatform == InputPlatform.Mobile)
            {
                // Setup swipe detector
                if (swipeDetector == null)
                {
                    swipeDetector = gameObject.AddComponent<SwipeDetector>();
                }

                swipeDetector.SetEnabled(true);
            }
            else
            {
                // Disable swipe detector on desktop
                if (swipeDetector != null)
                {
                    swipeDetector.SetEnabled(false);
                }
            }

            LoadKeyBindings();
        }

        /// <summary>
        /// Handles desktop keyboard input
        /// </summary>
        private void HandleDesktopInput()
        {
            // Jump
            if (UnityEngine.Input.GetKeyDown(jumpKey))
            {
                OnJumpPressed?.Invoke();
            }

            if (UnityEngine.Input.GetKeyUp(jumpKey))
            {
                OnJumpReleased?.Invoke();
            }

            // Door selection
            if (UnityEngine.Input.GetKeyDown(leftDoorKey))
            {
                OnLeftDoorSelected?.Invoke();
            }

            if (UnityEngine.Input.GetKeyDown(rightDoorKey))
            {
                OnRightDoorSelected?.Invoke();
            }

            // Pause
            if (UnityEngine.Input.GetKeyDown(pauseKey))
            {
                OnPausePressed?.Invoke();
            }

            // Shop
            if (UnityEngine.Input.GetKeyDown(shopKey) || UnityEngine.Input.GetKeyDown(shopKeyAlt))
            {
                OnShopPressed?.Invoke();
            }
        }

        #region Mobile Gesture Handlers

        private void HandleSwipeUp()
        {
            if (!inputEnabled) return;
            OnJumpPressed?.Invoke();
        }

        private void HandleSwipeLeft()
        {
            if (!inputEnabled) return;
            OnLeftDoorSelected?.Invoke();
        }

        private void HandleSwipeRight()
        {
            if (!inputEnabled) return;
            OnRightDoorSelected?.Invoke();
        }

        private void HandleTapTopLeft()
        {
            // Shop button
            OnShopPressed?.Invoke();
        }

        private void HandleTapTopRight()
        {
            // Pause button
            OnPausePressed?.Invoke();
        }

        #endregion

        #region Input Control

        /// <summary>
        /// Enables input processing
        /// </summary>
        public void EnableInput()
        {
            inputEnabled = true;

            if (swipeDetector != null && currentPlatform == InputPlatform.Mobile)
            {
                swipeDetector.SetEnabled(true);
            }
        }

        /// <summary>
        /// Disables input processing
        /// </summary>
        public void DisableInput()
        {
            inputEnabled = false;

            if (swipeDetector != null)
            {
                swipeDetector.SetEnabled(false);
            }
        }

        /// <summary>
        /// Toggles input enabled state
        /// </summary>
        public void ToggleInput()
        {
            if (inputEnabled)
            {
                DisableInput();
            }
            else
            {
                EnableInput();
            }
        }

        #endregion

        #region Input Queries (for direct polling)

        /// <summary>
        /// Returns true if jump is pressed this frame
        /// </summary>
        public bool GetJumpDown()
        {
            if (!inputEnabled) return false;

            if (currentPlatform == InputPlatform.Desktop)
            {
                return UnityEngine.Input.GetKeyDown(jumpKey);
            }

            // Mobile uses events, not polling
            return false;
        }

        /// <summary>
        /// Returns true if jump is held
        /// </summary>
        public bool GetJump()
        {
            if (!inputEnabled) return false;

            if (currentPlatform == InputPlatform.Desktop)
            {
                return UnityEngine.Input.GetKey(jumpKey);
            }

            return false;
        }

        /// <summary>
        /// Returns true if jump is released this frame
        /// </summary>
        public bool GetJumpUp()
        {
            if (!inputEnabled) return false;

            if (currentPlatform == InputPlatform.Desktop)
            {
                return UnityEngine.Input.GetKeyUp(jumpKey);
            }

            return false;
        }

        #endregion

        #region Key Rebinding

        /// <summary>
        /// Loads key bindings from save data
        /// </summary>
        private void LoadKeyBindings()
        {
            var saveData = Save.SaveSystem.QuickLoad();

            if (saveData != null)
            {
                if (!string.IsNullOrEmpty(saveData.jumpKey))
                {
                    if (Enum.TryParse(saveData.jumpKey, out KeyCode key))
                    {
                        jumpKey = key;
                    }
                }

                if (!string.IsNullOrEmpty(saveData.pauseKey))
                {
                    if (Enum.TryParse(saveData.pauseKey, out KeyCode key))
                    {
                        pauseKey = key;
                    }
                }

                if (!string.IsNullOrEmpty(saveData.leftDoorKey))
                {
                    if (Enum.TryParse(saveData.leftDoorKey, out KeyCode key))
                    {
                        leftDoorKey = key;
                    }
                }

                if (!string.IsNullOrEmpty(saveData.rightDoorKey))
                {
                    if (Enum.TryParse(saveData.rightDoorKey, out KeyCode key))
                    {
                        rightDoorKey = key;
                    }
                }
            }
        }

        /// <summary>
        /// Saves current key bindings
        /// </summary>
        private void SaveKeyBindings()
        {
            var saveData = Save.SaveSystem.LoadGame();

            if (saveData != null)
            {
                saveData.jumpKey = jumpKey.ToString();
                saveData.pauseKey = pauseKey.ToString();
                saveData.leftDoorKey = leftDoorKey.ToString();
                saveData.rightDoorKey = rightDoorKey.ToString();

                Save.SaveSystem.QuickSave(saveData);
            }
        }

        /// <summary>
        /// Rebinds jump key
        /// </summary>
        public void RebindJump(KeyCode newKey)
        {
            jumpKey = newKey;
            SaveKeyBindings();
        }

        /// <summary>
        /// Rebinds pause key
        /// </summary>
        public void RebindPause(KeyCode newKey)
        {
            pauseKey = newKey;
            SaveKeyBindings();
        }

        /// <summary>
        /// Rebinds left door key
        /// </summary>
        public void RebindLeftDoor(KeyCode newKey)
        {
            leftDoorKey = newKey;
            SaveKeyBindings();
        }

        /// <summary>
        /// Rebinds right door key
        /// </summary>
        public void RebindRightDoor(KeyCode newKey)
        {
            rightDoorKey = newKey;
            SaveKeyBindings();
        }

        /// <summary>
        /// Resets all key bindings to default
        /// </summary>
        public void ResetToDefaults()
        {
            jumpKey = KeyCode.W;
            leftDoorKey = KeyCode.A;
            rightDoorKey = KeyCode.D;
            pauseKey = KeyCode.Escape;
            shopKey = KeyCode.Tab;
            shopKeyAlt = KeyCode.I;

            SaveKeyBindings();
        }

        #endregion

        private void OnApplicationQuit()
        {
            SaveKeyBindings();
        }
    }
}
