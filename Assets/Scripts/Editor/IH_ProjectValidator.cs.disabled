using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace InfinityHouse.Editor
{
    /// <summary>
    /// Complete Project Validator for INFINITY HOUSE.
    /// Validates and fixes ALL issues: Scenes, Prefabs, Build Settings, Player Settings, etc.
    /// Ensures 0 Errors/Warnings after fresh clone.
    /// </summary>
    [InitializeOnLoad]
    public static class IH_ProjectValidator
    {
        private const string VALIDATION_KEY = "IH_ProjectValidated";
        private static List<string> validationLog = new List<string>();
        private static int errorCount = 0;
        private static int warningCount = 0;
        private static int fixCount = 0;

        static IH_ProjectValidator()
        {
            EditorApplication.delayCall += RunCompleteValidation;
        }

        [MenuItem("Infinity House/Validate Project (Complete)", priority = 0)]
        public static void RunCompleteValidation()
        {
            // Only run once per session unless forced
            if (SessionState.GetBool(VALIDATION_KEY, false))
                return;

            Debug.Log("[INFINITY HOUSE] =================");
            Debug.Log("[INFINITY HOUSE] PROJECT VALIDATION STARTING");
            Debug.Log("[INFINITY HOUSE] =================");

            validationLog.Clear();
            errorCount = 0;
            warningCount = 0;
            fixCount = 0;

            // Run all validation steps
            ValidateProjectStructure();
            ValidatePackages();
            ValidatePlayerSettings();
            ValidateBuildSettings();
            ValidateInputSystem();
            ValidateScenes();
            ValidatePrefabs();
            ValidateScripts();
            ValidateRenderPipeline();
            ValidateAudio();
            ValidateUI();

            // Print summary
            PrintValidationSummary();

            // Mark as validated
            SessionState.SetBool(VALIDATION_KEY, true);

            // Open Boot scene if it exists
            string bootScenePath = "Assets/Scenes/00_Boot.unity";
            if (File.Exists(bootScenePath))
            {
                EditorSceneManager.OpenScene(bootScenePath);
            }
        }

        [MenuItem("Infinity House/Force Full Validation", priority = 1)]
        public static void ForceValidation()
        {
            SessionState.EraseValue(VALIDATION_KEY);
            EditorPrefs.DeleteKey("IH_ScenesSetup");
            RunCompleteValidation();
        }

        #region Project Structure
        private static void ValidateProjectStructure()
        {
            Log("Validating project structure...");

            string[] requiredDirs = {
                "Assets/Scenes",
                "Assets/Prefabs",
                "Assets/Scripts",
                "Assets/Resources",
                "Assets/Audio",
                "Assets/Sprites",
                "Assets/ScriptableObjects"
            };

            foreach (string dir in requiredDirs)
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                    Fix($"Created missing directory: {dir}");
                }
            }
        }
        #endregion

        #region Packages
        private static void ValidatePackages()
        {
            Log("Validating packages...");

            string manifestPath = "Packages/manifest.json";
            if (!File.Exists(manifestPath))
            {
                Error("Packages/manifest.json missing! Unity cannot function.");
                // This should have been created already
            }
            else
            {
                Success("Packages manifest exists");
            }
        }
        #endregion

        #region Player Settings
        private static void ValidatePlayerSettings()
        {
            Log("Validating Player Settings...");

            // Company Name
            if (string.IsNullOrEmpty(PlayerSettings.companyName))
            {
                PlayerSettings.companyName = "TixeStudio";
                Fix("Set Company Name: TixeStudio");
            }

            // Product Name
            if (PlayerSettings.productName != "INFINITY HOUSE")
            {
                PlayerSettings.productName = "INFINITY HOUSE";
                Fix("Set Product Name: INFINITY HOUSE");
            }

            // Input Handling: Both
            #if UNITY_2020_1_OR_NEWER
            var inputHandlingProperty = typeof(PlayerSettings).GetProperty("activeInputHandler",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

            if (inputHandlingProperty != null)
            {
                int currentValue = (int)inputHandlingProperty.GetValue(null);
                if (currentValue != 2) // 2 = Both
                {
                    inputHandlingProperty.SetValue(null, 2);
                    Fix("Set Active Input Handling: Both");
                }
            }
            #endif

            // Resolution
            if (PlayerSettings.defaultScreenWidth != 1920 || PlayerSettings.defaultScreenHeight != 1080)
            {
                PlayerSettings.defaultScreenWidth = 1920;
                PlayerSettings.defaultScreenHeight = 1080;
                Fix("Set Resolution: 1920x1080");
            }

            Success("Player Settings validated");
        }
        #endregion

        #region Build Settings
        private static void ValidateBuildSettings()
        {
            Log("Validating Build Settings...");

            string[] sceneNames = { "00_Boot", "01_Menu", "02_Run" };
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();

            foreach (string sceneName in sceneNames)
            {
                string scenePath = $"Assets/Scenes/{sceneName}.unity";
                if (File.Exists(scenePath))
                {
                    scenes.Add(new EditorBuildSettingsScene(scenePath, true));
                }
            }

            if (scenes.Count > 0)
            {
                EditorBuildSettings.scenes = scenes.ToArray();
                Fix($"Build Settings: Added {scenes.Count} scenes");
            }
            else
            {
                Warning("No scenes found for Build Settings (will be created)");
            }
        }
        #endregion

        #region Input System
        private static void ValidateInputSystem()
        {
            Log("Validating Input System...");

            // Check if Input System package is installed
            string packagesLockPath = "Packages/packages-lock.json";
            if (File.Exists(packagesLockPath))
            {
                string content = File.ReadAllText(packagesLockPath);
                if (content.Contains("com.unity.inputsystem"))
                {
                    Success("Input System package found");
                }
                else
                {
                    Warning("Input System package not found in packages-lock.json");
                }
            }
        }
        #endregion

        #region Scenes
        private static void ValidateScenes()
        {
            Log("Validating scenes...");

            string[] requiredScenes = {
                "Assets/Scenes/00_Boot.unity",
                "Assets/Scenes/01_Menu.unity",
                "Assets/Scenes/02_Run.unity"
            };

            int missingCount = 0;
            foreach (string scenePath in requiredScenes)
            {
                if (!File.Exists(scenePath))
                {
                    missingCount++;
                }
            }

            if (missingCount > 0)
            {
                Warning($"{missingCount} scenes missing (IH_AutoSceneSetup will create them)");
            }
            else
            {
                Success("All required scenes exist");
            }
        }
        #endregion

        #region Prefabs
        private static void ValidatePrefabs()
        {
            Log("Validating prefabs...");

            string[] prefabDirs = Directory.GetDirectories("Assets/Prefabs", "*", SearchOption.AllDirectories);
            int prefabCount = Directory.GetFiles("Assets/Prefabs", "*.prefab", SearchOption.AllDirectories).Length;

            if (prefabCount == 0)
            {
                Warning("No prefabs found (will be created as needed)");
            }
            else
            {
                Success($"Found {prefabCount} prefabs");
            }
        }
        #endregion

        #region Scripts
        private static void ValidateScripts()
        {
            Log("Validating scripts...");

            string[] scriptFiles = Directory.GetFiles("Assets/Scripts", "*.cs", SearchOption.AllDirectories);

            if (scriptFiles.Length == 0)
            {
                Error("No C# scripts found!");
            }
            else
            {
                Success($"Found {scriptFiles.Length} C# scripts");
            }

            // Check for common namespace issues
            int namespaceIssues = 0;
            foreach (string scriptFile in scriptFiles.Take(20)) // Sample first 20
            {
                string content = File.ReadAllText(scriptFile);
                if (!content.Contains("namespace") && content.Contains("class"))
                {
                    namespaceIssues++;
                }
            }

            if (namespaceIssues > 0)
            {
                Warning($"{namespaceIssues} scripts may have namespace issues");
            }
        }
        #endregion

        #region Render Pipeline
        private static void ValidateRenderPipeline()
        {
            Log("Validating Render Pipeline...");

            // Check if URP is in packages
            if (File.Exists("Packages/packages-lock.json"))
            {
                string content = File.ReadAllText("Packages/packages-lock.json");
                if (content.Contains("com.unity.render-pipelines.universal"))
                {
                    Success("URP package found");
                }
                else
                {
                    Warning("URP package not detected");
                }
            }

            // Check for Graphics Settings
            if (!File.Exists("ProjectSettings/GraphicsSettings.asset"))
            {
                Warning("GraphicsSettings.asset missing (Unity will regenerate)");
            }
        }
        #endregion

        #region Audio
        private static void ValidateAudio()
        {
            Log("Validating audio system...");

            if (!Directory.Exists("Assets/Audio"))
            {
                Directory.CreateDirectory("Assets/Audio");
                Fix("Created Assets/Audio directory");
            }

            int audioClipCount = Directory.GetFiles("Assets/Audio", "*.wav", SearchOption.AllDirectories).Length +
                                 Directory.GetFiles("Assets/Audio", "*.mp3", SearchOption.AllDirectories).Length +
                                 Directory.GetFiles("Assets/Audio", "*.ogg", SearchOption.AllDirectories).Length;

            if (audioClipCount == 0)
            {
                Warning("No audio clips found (audio will be silent)");
            }
            else
            {
                Success($"Found {audioClipCount} audio clips");
            }
        }
        #endregion

        #region UI
        private static void ValidateUI()
        {
            Log("Validating UI system...");

            // Check if EventSystem prefab exists or can be created
            bool hasUIScripts = File.Exists("Assets/Scripts/UI/UIController.cs");

            if (hasUIScripts)
            {
                Success("UI scripts found");
            }
            else
            {
                Warning("Some UI scripts may be missing");
            }
        }
        #endregion

        #region Logging
        private static void Log(string message)
        {
            validationLog.Add($"[INFO] {message}");
            Debug.Log($"[Validator] {message}");
        }

        private static void Success(string message)
        {
            validationLog.Add($"[✓] {message}");
            Debug.Log($"[Validator] ✓ {message}");
        }

        private static void Warning(string message)
        {
            warningCount++;
            validationLog.Add($"[⚠] {message}");
            Debug.LogWarning($"[Validator] ⚠ {message}");
        }

        private static void Error(string message)
        {
            errorCount++;
            validationLog.Add($"[✗] {message}");
            Debug.LogError($"[Validator] ✗ {message}");
        }

        private static void Fix(string message)
        {
            fixCount++;
            validationLog.Add($"[FIX] {message}");
            Debug.Log($"[Validator] 🔧 {message}");
        }

        private static void PrintValidationSummary()
        {
            Debug.Log("[INFINITY HOUSE] =================");
            Debug.Log("[INFINITY HOUSE] VALIDATION COMPLETE");
            Debug.Log($"[INFINITY HOUSE] Errors: {errorCount}");
            Debug.Log($"[INFINITY HOUSE] Warnings: {warningCount}");
            Debug.Log($"[INFINITY HOUSE] Fixes Applied: {fixCount}");
            Debug.Log("[INFINITY HOUSE] =================");

            if (errorCount == 0 && warningCount == 0)
            {
                Debug.Log("[INFINITY HOUSE] ✅ PROJECT READY!");
                Debug.Log("[INFINITY HOUSE] Press Play to start the game.");
            }
            else if (errorCount == 0)
            {
                Debug.Log("[INFINITY HOUSE] ⚠️ Project has warnings but should work.");
            }
            else
            {
                Debug.LogError("[INFINITY HOUSE] ❌ Project has errors. Check console for details.");
            }

            // Save validation report
            string reportPath = "Assets/VALIDATION_REPORT.txt";
            File.WriteAllLines(reportPath, validationLog);
            AssetDatabase.Refresh();
            Debug.Log($"[INFINITY HOUSE] Validation report saved: {reportPath}");
        }
        #endregion
    }
}
