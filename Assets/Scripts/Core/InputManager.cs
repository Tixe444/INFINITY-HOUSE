using UnityEngine;

namespace InfiniteHaus.Core
{
    /// <summary>
    /// Centralized input manager for the game.
    /// Provides input state queries and supports key rebinding.
    /// For Unity's new Input System integration, this can be extended later.
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        [Header("Input Settings")]
        [Tooltip("Enable input processing")]
        [SerializeField] private bool inputEnabled = true;

        [Header("Key Bindings")]
        [Tooltip("Jump key")]
        public KeyCode jumpKey = KeyCode.Space;

        [Tooltip("Alternative jump key")]
        public KeyCode jumpKeyAlt = KeyCode.W;

        [Tooltip("Left door selection key")]
        public KeyCode leftDoorKey = KeyCode.LeftArrow;

        [Tooltip("Right door selection key")]
        public KeyCode rightDoorKey = KeyCode.RightArrow;

        [Tooltip("Pause key")]
        public KeyCode pauseKey = KeyCode.Escape;

        [Tooltip("Pause alternative key")]
        public KeyCode pauseKeyAlt = KeyCode.P;

        // Singleton instance
        public static InputManager Instance { get; private set; }

        private void Awake()
        {
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            LoadKeyBindings();
        }

        /// <summary>
        /// Loads key bindings from save data
        /// </summary>
        private void LoadKeyBindings()
        {
            var saveData = Save.SaveSystem.QuickLoad();

            if (saveData != null)
            {
                // Parse saved key bindings
                if (!string.IsNullOrEmpty(saveData.jumpKey))
                {
                    if (System.Enum.TryParse(saveData.jumpKey, out KeyCode key))
                    {
                        jumpKey = key;
                    }
                }

                if (!string.IsNullOrEmpty(saveData.pauseKey))
                {
                    if (System.Enum.TryParse(saveData.pauseKey, out KeyCode key))
                    {
                        pauseKey = key;
                    }
                }

                // Additional bindings can be loaded similarly
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

        #region Input Queries

        /// <summary>
        /// Returns true if jump button is pressed this frame
        /// </summary>
        public bool GetJumpDown()
        {
            if (!inputEnabled) return false;
            return Input.GetKeyDown(jumpKey) || Input.GetKeyDown(jumpKeyAlt);
        }

        /// <summary>
        /// Returns true if jump button is held
        /// </summary>
        public bool GetJump()
        {
            if (!inputEnabled) return false;
            return Input.GetKey(jumpKey) || Input.GetKey(jumpKeyAlt);
        }

        /// <summary>
        /// Returns true if jump button is released this frame
        /// </summary>
        public bool GetJumpUp()
        {
            if (!inputEnabled) return false;
            return Input.GetKeyUp(jumpKey) || Input.GetKeyUp(jumpKeyAlt);
        }

        /// <summary>
        /// Returns true if left door key is pressed
        /// </summary>
        public bool GetLeftDoorDown()
        {
            if (!inputEnabled) return false;
            return Input.GetKeyDown(leftDoorKey);
        }

        /// <summary>
        /// Returns true if right door key is pressed
        /// </summary>
        public bool GetRightDoorDown()
        {
            if (!inputEnabled) return false;
            return Input.GetKeyDown(rightDoorKey);
        }

        /// <summary>
        /// Returns true if pause key is pressed
        /// </summary>
        public bool GetPauseDown()
        {
            if (!inputEnabled) return false;
            return Input.GetKeyDown(pauseKey) || Input.GetKeyDown(pauseKeyAlt);
        }

        /// <summary>
        /// Gets horizontal input (-1 to 1)
        /// </summary>
        public float GetHorizontal()
        {
            if (!inputEnabled) return 0f;
            return Input.GetAxis("Horizontal");
        }

        /// <summary>
        /// Gets vertical input (-1 to 1)
        /// </summary>
        public float GetVertical()
        {
            if (!inputEnabled) return 0f;
            return Input.GetAxis("Vertical");
        }

        #endregion

        #region Input Control

        /// <summary>
        /// Enables input processing
        /// </summary>
        public void EnableInput()
        {
            inputEnabled = true;
        }

        /// <summary>
        /// Disables input processing
        /// </summary>
        public void DisableInput()
        {
            inputEnabled = false;
        }

        /// <summary>
        /// Toggles input enabled state
        /// </summary>
        public void ToggleInput()
        {
            inputEnabled = !inputEnabled;
        }

        #endregion

        #region Key Rebinding

        /// <summary>
        /// Rebinds the jump key
        /// </summary>
        public void RebindJump(KeyCode newKey)
        {
            jumpKey = newKey;
            SaveKeyBindings();
        }

        /// <summary>
        /// Rebinds the pause key
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
            jumpKey = KeyCode.Space;
            jumpKeyAlt = KeyCode.W;
            leftDoorKey = KeyCode.LeftArrow;
            rightDoorKey = KeyCode.RightArrow;
            pauseKey = KeyCode.Escape;
            pauseKeyAlt = KeyCode.P;

            SaveKeyBindings();
        }

        #endregion

        private void OnApplicationQuit()
        {
            SaveKeyBindings();
        }
    }
}
