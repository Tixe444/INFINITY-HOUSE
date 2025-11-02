using UnityEngine;
using UnityEngine.Audio;

namespace InfiniteHaus.Save
{
    /// <summary>
    /// Manages game settings (audio, video, input).
    /// Applies settings from SaveData and handles runtime changes.
    /// </summary>
    public class SettingsManager : MonoBehaviour
    {
        [Header("Audio")]
        [Tooltip("Audio mixer for volume control")]
        [SerializeField] private AudioMixer audioMixer;

        [Tooltip("Master volume slider range")]
        [SerializeField] private Vector2 volumeRange = new Vector2(-80f, 0f);

        [Header("Current Settings")]
        [SerializeField] private SaveData currentSettings;

        // Events
        public System.Action<SaveData> OnSettingsChanged;

        private void Start()
        {
            LoadSettings();
            ApplySettings();
        }

        /// <summary>
        /// Loads settings from save system
        /// </summary>
        public void LoadSettings()
        {
            currentSettings = SaveSystem.LoadGame();
            Debug.Log("Settings loaded");
        }

        /// <summary>
        /// Saves current settings
        /// </summary>
        public void SaveSettings()
        {
            if (currentSettings == null)
            {
                currentSettings = SaveData.CreateDefault();
            }

            SaveSystem.SaveGame(currentSettings);
            SaveSystem.QuickSave(currentSettings); // Backup save
            Debug.Log("Settings saved");
        }

        /// <summary>
        /// Applies all settings to game
        /// </summary>
        public void ApplySettings()
        {
            if (currentSettings == null) return;

            ApplyAudioSettings();
            ApplyVideoSettings();

            OnSettingsChanged?.Invoke(currentSettings);
        }

        /// <summary>
        /// Applies audio settings
        /// </summary>
        private void ApplyAudioSettings()
        {
            SetMasterVolume(currentSettings.masterVolume);
            SetMusicVolume(currentSettings.musicVolume);
            SetSFXVolume(currentSettings.sfxVolume);
        }

        /// <summary>
        /// Applies video settings
        /// </summary>
        private void ApplyVideoSettings()
        {
            // Set resolution
            Screen.SetResolution(
                currentSettings.resolutionWidth,
                currentSettings.resolutionHeight,
                currentSettings.fullscreen
            );

            Debug.Log($"Resolution set to {currentSettings.resolutionWidth}x{currentSettings.resolutionHeight}, Fullscreen: {currentSettings.fullscreen}");
        }

        #region Audio Settings

        /// <summary>
        /// Sets master volume (0-1)
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            currentSettings.masterVolume = volume;

            if (audioMixer != null)
            {
                float dbValue = Mathf.Lerp(volumeRange.x, volumeRange.y, volume);
                audioMixer.SetFloat("MasterVolume", dbValue);
            }
            else
            {
                AudioListener.volume = volume;
            }
        }

        /// <summary>
        /// Sets music volume (0-1)
        /// </summary>
        public void SetMusicVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            currentSettings.musicVolume = volume;

            if (audioMixer != null)
            {
                float dbValue = Mathf.Lerp(volumeRange.x, volumeRange.y, volume);
                audioMixer.SetFloat("MusicVolume", dbValue);
            }
        }

        /// <summary>
        /// Sets SFX volume (0-1)
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            currentSettings.sfxVolume = volume;

            if (audioMixer != null)
            {
                float dbValue = Mathf.Lerp(volumeRange.x, volumeRange.y, volume);
                audioMixer.SetFloat("SFXVolume", dbValue);
            }
        }

        #endregion

        #region Video Settings

        /// <summary>
        /// Sets fullscreen mode
        /// </summary>
        public void SetFullscreen(bool fullscreen)
        {
            currentSettings.fullscreen = fullscreen;
            Screen.fullScreen = fullscreen;
        }

        /// <summary>
        /// Sets resolution
        /// </summary>
        public void SetResolution(int width, int height, bool fullscreen)
        {
            currentSettings.resolutionWidth = width;
            currentSettings.resolutionHeight = height;
            currentSettings.fullscreen = fullscreen;

            Screen.SetResolution(width, height, fullscreen);
        }

        /// <summary>
        /// Sets resolution from preset index
        /// </summary>
        public void SetResolutionPreset(int presetIndex)
        {
            Resolution[] resolutions = Screen.resolutions;

            if (presetIndex >= 0 && presetIndex < resolutions.Length)
            {
                Resolution res = resolutions[presetIndex];
                SetResolution(res.width, res.height, currentSettings.fullscreen);
            }
        }

        #endregion

        #region Input Settings

        /// <summary>
        /// Sets jump key binding
        /// </summary>
        public void SetJumpKey(string key)
        {
            currentSettings.jumpKey = key;
        }

        /// <summary>
        /// Sets left door key binding
        /// </summary>
        public void SetLeftDoorKey(string key)
        {
            currentSettings.leftDoorKey = key;
        }

        /// <summary>
        /// Sets right door key binding
        /// </summary>
        public void SetRightDoorKey(string key)
        {
            currentSettings.rightDoorKey = key;
        }

        /// <summary>
        /// Sets pause key binding
        /// </summary>
        public void SetPauseKey(string key)
        {
            currentSettings.pauseKey = key;
        }

        #endregion

        /// <summary>
        /// Gets current settings data
        /// </summary>
        public SaveData GetSettings()
        {
            return currentSettings;
        }

        /// <summary>
        /// Resets settings to default
        /// </summary>
        public void ResetToDefaults()
        {
            currentSettings = SaveData.CreateDefault();
            ApplySettings();
            SaveSettings();

            Debug.Log("Settings reset to defaults");
        }

        private void OnApplicationQuit()
        {
            SaveSettings();
        }
    }
}
