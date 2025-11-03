// ═══════════════════════════════════════════════════════════════════════════════
// INFINITY HOUSE v5.5 - Made by Mate Makovics
// Clean HUD Manager - Minimal UI with Full Settings Tab
// ═══════════════════════════════════════════════════════════════════════════════

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

namespace InfinityHouse.Performance
{
    /// <summary>
    /// Clean minimal HUD with responsive Settings Tab.
    /// TL: Shards | TR: Diamonds+Timer | BL: Skin Badge | BR: Event Indicator
    /// Settings: Volume, Vibration, Brightness, Quality, Language, Reset, Accessibility
    /// Target: <16ms HUD latency, zero overlap, persistent settings.
    /// </summary>
    public class CleanHUDManager : MonoBehaviour
    {
        // Singleton
        public static CleanHUDManager Instance { get; private set; }

        [Header("HUD Elements")]
        [SerializeField] private TextMeshProUGUI shardCounterText;
        [SerializeField] private TextMeshProUGUI diamondCounterText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private Image skinBadgeImage;
        [SerializeField] private GameObject eventIndicator;

        [Header("Main Menu")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private Button startButton;
        [SerializeField] private Button shopButton;
        [SerializeField] private Button fusionButton;
        [SerializeField] private Button inventoryButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button marketButton;

        [Header("Settings Panel")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Toggle vibrationToggle;
        [SerializeField] private Slider brightnessSlider;
        [SerializeField] private Slider contrastSlider;
        [SerializeField] private TMP_Dropdown qualityDropdown;
        [SerializeField] private TMP_Dropdown languageDropdown;
        [SerializeField] private Button resetProgressButton;
        [SerializeField] private TMP_Dropdown colorblindFilterDropdown;
        [SerializeField] private Slider fontScaleSlider;

        [Header("HUD Configuration")]
        [SerializeField] private float shardCountAnimSpeed = 0.5f;
        [SerializeField] private bool enableHUDLatencyTracking = true;

        // HUD state
        private int currentShards;
        private int targetShards;
        private int currentDiamonds;
        private float runTimer;
        private bool isRunActive;

        // Settings state (persisted)
        private SettingsData settings;

        // Latency tracking
        private float lastHUDUpdateTime;
        private float averageHUDLatency;

        // Animation
        private float shardAnimT;

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
            // Load settings
            LoadSettings();

            // Setup button listeners
            SetupMainMenuButtons();
            SetupSettingsControls();

            // Apply settings
            ApplyAllSettings();

            // Subscribe to performance manager
            if (PerformanceManager.Instance != null)
            {
                PerformanceManager.Instance.OnRenderTick += UpdateHUD;
            }

            // Hide settings panel
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }

            Debug.Log("[CleanHUDManager] Initialized with clean minimal UI");
        }

        #region HUD Updates

        private void UpdateHUD()
        {
            float updateStartTime = Time.realtimeSinceStartup;

            // Animate shard counter
            if (currentShards != targetShards)
            {
                shardAnimT += Time.deltaTime * shardCountAnimSpeed;
                currentShards = (int)Mathf.Lerp(currentShards, targetShards, shardAnimT);

                if (Mathf.Abs(currentShards - targetShards) < 1)
                {
                    currentShards = targetShards;
                    shardAnimT = 0f;
                }

                UpdateShardDisplay();
            }

            // Update timer
            if (isRunActive)
            {
                runTimer += Time.deltaTime;
                UpdateTimerDisplay();
            }

            // Track latency
            if (enableHUDLatencyTracking)
            {
                float latency = (Time.realtimeSinceStartup - updateStartTime) * 1000f;
                averageHUDLatency = Mathf.Lerp(averageHUDLatency, latency, 0.1f);
            }
        }

        private void UpdateShardDisplay()
        {
            if (shardCounterText != null)
            {
                shardCounterText.text = $"✦ {currentShards}";
            }
        }

        private void UpdateDiamondDisplay()
        {
            if (diamondCounterText != null)
            {
                diamondCounterText.text = $"💎 {currentDiamonds}";
            }
        }

        private void UpdateTimerDisplay()
        {
            if (timerText != null)
            {
                int minutes = (int)(runTimer / 60f);
                int seconds = (int)(runTimer % 60f);
                timerText.text = $"{minutes:00}:{seconds:00}";
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Sets shard count with animation
        /// </summary>
        public void SetShardCount(int amount)
        {
            targetShards = amount;
            shardAnimT = 0f;
        }

        /// <summary>
        /// Adds shards with animation
        /// </summary>
        public void AddShards(int amount)
        {
            targetShards += amount;
            shardAnimT = 0f;

            // Show float number animation
            // TODO: Implement float number FX
        }

        /// <summary>
        /// Sets diamond count
        /// </summary>
        public void SetDiamondCount(int amount)
        {
            currentDiamonds = amount;
            UpdateDiamondDisplay();
        }

        /// <summary>
        /// Starts run timer
        /// </summary>
        public void StartRunTimer()
        {
            runTimer = 0f;
            isRunActive = true;
        }

        /// <summary>
        /// Stops run timer
        /// </summary>
        public void StopRunTimer()
        {
            isRunActive = false;
        }

        /// <summary>
        /// Sets skin badge
        /// </summary>
        public void SetSkinBadge(Sprite badgeSprite)
        {
            if (skinBadgeImage != null)
            {
                skinBadgeImage.sprite = badgeSprite;
            }
        }

        /// <summary>
        /// Shows event indicator
        /// </summary>
        public void ShowEventIndicator(bool show)
        {
            if (eventIndicator != null)
            {
                eventIndicator.SetActive(show);
            }
        }

        #endregion

        #region Main Menu

        private void SetupMainMenuButtons()
        {
            if (startButton != null)
                startButton.onClick.AddListener(OnStartClicked);

            if (shopButton != null)
                shopButton.onClick.AddListener(OnShopClicked);

            if (fusionButton != null)
                fusionButton.onClick.AddListener(OnFusionClicked);

            if (inventoryButton != null)
                inventoryButton.onClick.AddListener(OnInventoryClicked);

            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettingsClicked);

            if (marketButton != null)
                marketButton.onClick.AddListener(OnMarketClicked);
        }

        private void OnStartClicked()
        {
            Debug.Log("[CleanHUDManager] Start game");
            // TODO: Start game
        }

        private void OnShopClicked()
        {
            Debug.Log("[CleanHUDManager] Open shop");
            // TODO: Open shop
        }

        private void OnFusionClicked()
        {
            Debug.Log("[CleanHUDManager] Open fusion");
            // TODO: Open fusion chamber
        }

        private void OnInventoryClicked()
        {
            Debug.Log("[CleanHUDManager] Open inventory");
            // TODO: Open inventory
        }

        private void OnSettingsClicked()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(!settingsPanel.activeSelf);
                TrackSettingsEvent("settings_panel_toggled", settingsPanel.activeSelf ? "opened" : "closed");
            }
        }

        private void OnMarketClicked()
        {
            Debug.Log("[CleanHUDManager] Open market");
            #if UNITY_STANDALONE
            Economy.SteamMarketIntegration.Instance?.OpenMarket();
            #else
            Debug.LogWarning("Market only available on Steam");
            #endif
        }

        #endregion

        #region Settings Panel

        private void SetupSettingsControls()
        {
            // Volume sliders
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
                masterVolumeSlider.value = settings.masterVolume;
            }

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
                musicVolumeSlider.value = settings.musicVolume;
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
                sfxVolumeSlider.value = settings.sfxVolume;
            }

            // Vibration toggle
            if (vibrationToggle != null)
            {
                vibrationToggle.onValueChanged.AddListener(OnVibrationChanged);
                vibrationToggle.isOn = settings.vibrationEnabled;
            }

            // Brightness/Contrast
            if (brightnessSlider != null)
            {
                brightnessSlider.onValueChanged.AddListener(OnBrightnessChanged);
                brightnessSlider.value = settings.brightness;
            }

            if (contrastSlider != null)
            {
                contrastSlider.onValueChanged.AddListener(OnContrastChanged);
                contrastSlider.value = settings.contrast;
            }

            // Quality dropdown
            if (qualityDropdown != null)
            {
                qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
                qualityDropdown.value = settings.qualityLevel;
            }

            // Language dropdown
            if (languageDropdown != null)
            {
                languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
                languageDropdown.value = settings.languageIndex;
            }

            // Reset progress button
            if (resetProgressButton != null)
            {
                resetProgressButton.onClick.AddListener(OnResetProgressClicked);
            }

            // Colorblind filter
            if (colorblindFilterDropdown != null)
            {
                colorblindFilterDropdown.onValueChanged.AddListener(OnColorblindFilterChanged);
                colorblindFilterDropdown.value = settings.colorblindFilterIndex;
            }

            // Font scale
            if (fontScaleSlider != null)
            {
                fontScaleSlider.onValueChanged.AddListener(OnFontScaleChanged);
                fontScaleSlider.value = settings.fontScale;
            }
        }

        #endregion

        #region Settings Handlers

        private void OnMasterVolumeChanged(float value)
        {
            settings.masterVolume = value;
            AudioListener.volume = value;
            SaveSettings();
            TrackSettingsEvent("master_volume", value.ToString("F2"));
        }

        private void OnMusicVolumeChanged(float value)
        {
            settings.musicVolume = value;
            // TODO: Apply to music mixer
            SaveSettings();
            TrackSettingsEvent("music_volume", value.ToString("F2"));
        }

        private void OnSFXVolumeChanged(float value)
        {
            settings.sfxVolume = value;
            // TODO: Apply to SFX mixer
            SaveSettings();
            TrackSettingsEvent("sfx_volume", value.ToString("F2"));
        }

        private void OnVibrationChanged(bool enabled)
        {
            settings.vibrationEnabled = enabled;
            SaveSettings();
            TrackSettingsEvent("vibration", enabled ? "enabled" : "disabled");
        }

        private void OnBrightnessChanged(float value)
        {
            settings.brightness = value;
            ApplyBrightness();
            SaveSettings();
            TrackSettingsEvent("brightness", value.ToString("F2"));
        }

        private void OnContrastChanged(float value)
        {
            settings.contrast = value;
            ApplyContrast();
            SaveSettings();
            TrackSettingsEvent("contrast", value.ToString("F2"));
        }

        private void OnQualityChanged(int index)
        {
            settings.qualityLevel = index;
            QualitySettings.SetQualityLevel(index);
            SaveSettings();
            TrackSettingsEvent("quality", QualitySettings.names[index]);
        }

        private void OnLanguageChanged(int index)
        {
            settings.languageIndex = index;
            // TODO: Apply language
            SaveSettings();
            TrackSettingsEvent("language", index.ToString());
        }

        private void OnResetProgressClicked()
        {
            // Show confirmation dialog
            Debug.LogWarning("[CleanHUDManager] Reset progress requested");
            // TODO: Implement confirmation dialog
        }

        private void OnColorblindFilterChanged(int index)
        {
            settings.colorblindFilterIndex = index;
            ApplyColorblindFilter(index);
            SaveSettings();
            TrackSettingsEvent("colorblind_filter", index.ToString());
        }

        private void OnFontScaleChanged(float value)
        {
            settings.fontScale = value;
            ApplyFontScale();
            SaveSettings();
            TrackSettingsEvent("font_scale", value.ToString("F2"));
        }

        #endregion

        #region Settings Application

        private void ApplyAllSettings()
        {
            AudioListener.volume = settings.masterVolume;
            ApplyBrightness();
            ApplyContrast();
            QualitySettings.SetQualityLevel(settings.qualityLevel);
            ApplyColorblindFilter(settings.colorblindFilterIndex);
            ApplyFontScale();
        }

        private void ApplyBrightness()
        {
            // Apply via post-processing or shader
            // TODO: Implement brightness shader
        }

        private void ApplyContrast()
        {
            // Apply via post-processing or shader
            // TODO: Implement contrast shader
        }

        private void ApplyColorblindFilter(int filterIndex)
        {
            // 0 = None, 1 = Protanopia, 2 = Deuteranopia, 3 = Tritanopia
            // TODO: Implement colorblind filters
        }

        private void ApplyFontScale()
        {
            // Scale all TextMeshPro components
            // TODO: Apply font scaling
        }

        #endregion

        #region Settings Persistence

        private void LoadSettings()
        {
            settings = new SettingsData
            {
                masterVolume = PlayerPrefs.GetFloat("Settings_MasterVolume", 1f),
                musicVolume = PlayerPrefs.GetFloat("Settings_MusicVolume", 0.8f),
                sfxVolume = PlayerPrefs.GetFloat("Settings_SFXVolume", 1f),
                vibrationEnabled = PlayerPrefs.GetInt("Settings_Vibration", 1) == 1,
                brightness = PlayerPrefs.GetFloat("Settings_Brightness", 1f),
                contrast = PlayerPrefs.GetFloat("Settings_Contrast", 1f),
                qualityLevel = PlayerPrefs.GetInt("Settings_Quality", 1),
                languageIndex = PlayerPrefs.GetInt("Settings_Language", 0),
                colorblindFilterIndex = PlayerPrefs.GetInt("Settings_ColorblindFilter", 0),
                fontScale = PlayerPrefs.GetFloat("Settings_FontScale", 1f)
            };

            Debug.Log("[CleanHUDManager] Settings loaded");
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetFloat("Settings_MasterVolume", settings.masterVolume);
            PlayerPrefs.SetFloat("Settings_MusicVolume", settings.musicVolume);
            PlayerPrefs.SetFloat("Settings_SFXVolume", settings.sfxVolume);
            PlayerPrefs.SetInt("Settings_Vibration", settings.vibrationEnabled ? 1 : 0);
            PlayerPrefs.SetFloat("Settings_Brightness", settings.brightness);
            PlayerPrefs.SetFloat("Settings_Contrast", settings.contrast);
            PlayerPrefs.SetInt("Settings_Quality", settings.qualityLevel);
            PlayerPrefs.SetInt("Settings_Language", settings.languageIndex);
            PlayerPrefs.SetInt("Settings_ColorblindFilter", settings.colorblindFilterIndex);
            PlayerPrefs.SetFloat("Settings_FontScale", settings.fontScale);
            PlayerPrefs.Save();

            // TODO: Cloud sync
        }

        #endregion

        #region Telemetry

        private void TrackSettingsEvent(string key, string value)
        {
            Analytics.TelemetryEvents.Instance?.TrackEvent("settings_changed", new Dictionary<string, object>
            {
                { "key", key },
                { "value", value }
            });
        }

        #endregion

        #region Debug

        private void OnGUI()
        {
            if (!enableHUDLatencyTracking) return;

            GUIStyle style = new GUIStyle();
            style.normal.textColor = averageHUDLatency < 16f ? Color.green : Color.red;
            style.fontSize = 16;

            GUI.Label(new Rect(10, 250, 300, 30), $"HUD Latency: {averageHUDLatency:F2}ms (Target: <16ms)", style);
        }

        #endregion

        #region Data Structures

        [Serializable]
        private class SettingsData
        {
            public float masterVolume;
            public float musicVolume;
            public float sfxVolume;
            public bool vibrationEnabled;
            public float brightness;
            public float contrast;
            public int qualityLevel;
            public int languageIndex;
            public int colorblindFilterIndex;
            public float fontScale;
        }

        #endregion

        private void OnDestroy()
        {
            if (PerformanceManager.Instance != null)
            {
                PerformanceManager.Instance.OnRenderTick -= UpdateHUD;
            }
        }
    }
}
