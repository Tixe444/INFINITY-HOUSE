using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;

namespace InfinityHouse.Editor
{
    /// <summary>
    /// Auto Scene Setup for INFINITY HOUSE.
    /// Automatically creates missing scenes when Unity opens.
    /// Ensures out-of-the-box playability.
    /// </summary>
    [InitializeOnLoad]
    public static class IH_AutoSceneSetup
    {
        private const string SETUP_KEY = "IH_ScenesSetup";
        private const string SCENES_PATH = "Assets/Scenes";

        static IH_AutoSceneSetup()
        {
            // Run setup on Unity startup
            EditorApplication.delayCall += CheckAndSetupScenes;
        }

        private static void CheckAndSetupScenes()
        {
            // Only run once per project
            if (EditorPrefs.GetBool(SETUP_KEY, false))
                return;

            Debug.Log("[INFINITY HOUSE] First-time setup: Creating scenes...");

            // Ensure Scenes directory exists
            if (!Directory.Exists(SCENES_PATH))
            {
                Directory.CreateDirectory(SCENES_PATH);
                AssetDatabase.Refresh();
            }

            // Create scenes if missing
            CreateBootScene();
            CreateMainScene();
            CreateMenuScene();

            // Configure Build Settings
            ConfigureBuildSettings();

            // Configure Player Settings
            ConfigurePlayerSettings();

            // Mark setup as complete
            EditorPrefs.SetBool(SETUP_KEY, true);

            Debug.Log("[INFINITY HOUSE] ✅ Setup complete! Press Play to start the game.");

            // Open Boot scene
            EditorSceneManager.OpenScene($"{SCENES_PATH}/00_Boot.unity");
        }

        [MenuItem("Infinity House/Reset Scene Setup")]
        public static void ResetSetup()
        {
            EditorPrefs.DeleteKey(SETUP_KEY);
            Debug.Log("[INFINITY HOUSE] Setup reset. Restart Unity to regenerate scenes.");
        }

        [MenuItem("Infinity House/Force Scene Setup")]
        public static void ForceSetup()
        {
            EditorPrefs.DeleteKey(SETUP_KEY);
            CheckAndSetupScenes();
        }

        private static void CreateBootScene()
        {
            string scenePath = $"{SCENES_PATH}/00_Boot.unity";

            if (File.Exists(scenePath))
            {
                Debug.Log($"[Setup] Boot scene already exists: {scenePath}");
                return;
            }

            // Create new scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Create essential GameObjects
            CreateBootSceneObjects();

            // Save scene
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[Setup] ✅ Created Boot scene: {scenePath}");
        }

        private static void CreateBootSceneObjects()
        {
            // 1. Main Camera
            GameObject camera = new GameObject("Main Camera");
            Camera cam = camera.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.orthographic = true;
            cam.orthographicSize = 5;
            camera.tag = "MainCamera";

            // 2. GameManager (DontDestroyOnLoad)
            GameObject gameManager = new GameObject("GameManager");
            gameManager.AddComponent<Core.GameManager>();

            // 3. IH_EconomyService
            GameObject economyService = new GameObject("IH_EconomyService");
            economyService.AddComponent<Economy.IH_EconomyService>();

            // 4. IH_SignatureVerifier (Security)
            GameObject security = new GameObject("IH_SignatureVerifier");
            security.AddComponent<Security.IH_SignatureVerifier>();

            // 5. EventSystem for UI
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            // 6. Canvas for loading screen
            GameObject canvas = new GameObject("Canvas");
            Canvas c = canvas.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            GameObject loadingText = new GameObject("LoadingText");
            loadingText.transform.SetParent(canvas.transform);
            var text = loadingText.AddComponent<UnityEngine.UI.Text>();
            text.text = "INFINITY HOUSE\n\nPress Play to Start";
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 24;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;

            RectTransform rt = loadingText.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            Debug.Log("[Setup] Boot scene objects created");
        }

        private static void CreateMainScene()
        {
            string scenePath = $"{SCENES_PATH}/02_Run.unity";

            if (File.Exists(scenePath))
            {
                Debug.Log($"[Setup] Main scene already exists: {scenePath}");
                return;
            }

            // Create new scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Create gameplay objects
            CreateMainSceneObjects();

            // Save scene
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[Setup] ✅ Created Main scene: {scenePath}");
        }

        private static void CreateMainSceneObjects()
        {
            // 1. Main Camera (2D)
            GameObject camera = new GameObject("Main Camera");
            Camera cam = camera.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.1f, 0.1f, 0.15f); // Dark pixel background
            cam.orthographic = true;
            cam.orthographicSize = 5;
            camera.tag = "MainCamera";
            camera.AddComponent<Camera.FixedSideCameraController>();

            // 2. Player
            GameObject player = new GameObject("Player");
            player.transform.position = new Vector3(-8, 0, 0);
            player.tag = "Player";

            // Add Rigidbody2D
            var rb = player.AddComponent<Rigidbody2D>();
            rb.gravityScale = 2.5f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // Add Collider
            var col = player.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.8f, 1.6f);

            // Add Visual (simple sprite)
            var sr = player.AddComponent<SpriteRenderer>();
            sr.color = Color.cyan;
            sr.sprite = CreateSquareSprite();

            // Add EnhancedRunnerController
            player.AddComponent<Player.EnhancedRunnerController>();
            player.AddComponent<Player.StyleMeterSystem>();

            // 3. Ground (for testing)
            GameObject ground = new GameObject("Ground");
            ground.transform.position = new Vector3(0, -3, 0);
            ground.tag = "Ground";
            ground.layer = LayerMask.NameToLayer("Default");

            var groundCol = ground.AddComponent<BoxCollider2D>();
            groundCol.size = new Vector2(50, 1);

            var groundSprite = ground.AddComponent<SpriteRenderer>();
            groundSprite.color = Color.gray;
            groundSprite.sprite = CreateSquareSprite();
            groundSprite.drawMode = SpriteDrawMode.Tiled;
            groundSprite.size = new Vector2(50, 1);

            // 4. Game Systems
            GameObject gameSystems = new GameObject("GameSystems");

            GameObject sessionManager = new GameObject("GameSessionManager");
            sessionManager.transform.SetParent(gameSystems.transform);
            sessionManager.AddComponent<Game.GameSessionManager>();

            GameObject biomeManager = new GameObject("IH_BiomeManager");
            biomeManager.transform.SetParent(gameSystems.transform);
            biomeManager.AddComponent<Game.IH_BiomeManager>();

            GameObject corridorSystem = new GameObject("CorridorSystem");
            corridorSystem.transform.SetParent(gameSystems.transform);
            corridorSystem.AddComponent<Game.CorridorSystem>();

            GameObject chaseManager = new GameObject("ChaseManager");
            chaseManager.transform.SetParent(gameSystems.transform);
            chaseManager.AddComponent<Game.ChaseManager>();

            // 5. Performance Validator
            GameObject perfValidator = new GameObject("IH_PerformanceValidator");
            perfValidator.AddComponent<Performance.IH_PerformanceValidator>();

            // 6. UI Canvas
            GameObject canvas = new GameObject("Canvas");
            Canvas c = canvas.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // HUD
            GameObject hud = new GameObject("HUD");
            hud.transform.SetParent(canvas.transform);

            GameObject distanceText = new GameObject("DistanceText");
            distanceText.transform.SetParent(hud.transform);
            var distText = distanceText.AddComponent<UnityEngine.UI.Text>();
            distText.text = "Distance: 0m";
            distText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            distText.fontSize = 20;
            distText.color = Color.white;

            RectTransform distRT = distanceText.GetComponent<RectTransform>();
            distRT.anchorMin = new Vector2(0, 1);
            distRT.anchorMax = new Vector2(0, 1);
            distRT.pivot = new Vector2(0, 1);
            distRT.anchoredPosition = new Vector2(20, -20);
            distRT.sizeDelta = new Vector2(200, 30);

            Debug.Log("[Setup] Main scene objects created");
        }

        private static void CreateMenuScene()
        {
            string scenePath = $"{SCENES_PATH}/01_Menu.unity";

            if (File.Exists(scenePath))
            {
                Debug.Log($"[Setup] Menu scene already exists: {scenePath}");
                return;
            }

            // Create new scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Create menu objects
            CreateMenuSceneObjects();

            // Save scene
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[Setup] ✅ Created Menu scene: {scenePath}");
        }

        private static void CreateMenuSceneObjects()
        {
            // Camera
            GameObject camera = new GameObject("Main Camera");
            Camera cam = camera.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.orthographic = true;
            camera.tag = "MainCamera";

            // Canvas
            GameObject canvas = new GameObject("Canvas");
            Canvas c = canvas.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // Title
            GameObject title = new GameObject("Title");
            title.transform.SetParent(canvas.transform);
            var titleText = title.AddComponent<UnityEngine.UI.Text>();
            titleText.text = "INFINITY HOUSE";
            titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            titleText.fontSize = 48;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = Color.white;

            RectTransform titleRT = title.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0.5f, 0.7f);
            titleRT.anchorMax = new Vector2(0.5f, 0.7f);
            titleRT.pivot = new Vector2(0.5f, 0.5f);
            titleRT.sizeDelta = new Vector2(400, 60);

            // Play Button
            GameObject playButton = new GameObject("PlayButton");
            playButton.transform.SetParent(canvas.transform);

            var buttonImg = playButton.AddComponent<UnityEngine.UI.Image>();
            buttonImg.color = new Color(0.2f, 0.6f, 1f);

            var button = playButton.AddComponent<UnityEngine.UI.Button>();

            RectTransform buttonRT = playButton.GetComponent<RectTransform>();
            buttonRT.anchorMin = new Vector2(0.5f, 0.4f);
            buttonRT.anchorMax = new Vector2(0.5f, 0.4f);
            buttonRT.pivot = new Vector2(0.5f, 0.5f);
            buttonRT.sizeDelta = new Vector2(200, 60);

            GameObject buttonText = new GameObject("Text");
            buttonText.transform.SetParent(playButton.transform);
            var btnText = buttonText.AddComponent<UnityEngine.UI.Text>();
            btnText.text = "PLAY";
            btnText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            btnText.fontSize = 24;
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.color = Color.white;

            RectTransform btnTextRT = buttonText.GetComponent<RectTransform>();
            btnTextRT.anchorMin = Vector2.zero;
            btnTextRT.anchorMax = Vector2.one;
            btnTextRT.sizeDelta = Vector2.zero;

            // EventSystem
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            Debug.Log("[Setup] Menu scene objects created");
        }

        private static void ConfigureBuildSettings()
        {
            // Get current build settings
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>();

            // Add scenes in order
            scenes.Add(new EditorBuildSettingsScene($"{SCENES_PATH}/00_Boot.unity", true));
            scenes.Add(new EditorBuildSettingsScene($"{SCENES_PATH}/01_Menu.unity", true));
            scenes.Add(new EditorBuildSettingsScene($"{SCENES_PATH}/02_Run.unity", true));

            EditorBuildSettings.scenes = scenes.ToArray();

            Debug.Log("[Setup] Build Settings configured");
        }

        private static void ConfigurePlayerSettings()
        {
            // Active Input Handling: Both
            #if UNITY_2020_1_OR_NEWER
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
            #endif

            // Company and Product Name
            PlayerSettings.companyName = "TixeStudio";
            PlayerSettings.productName = "INFINITY HOUSE";

            // Default Screen Size
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.defaultIsNativeResolution = false;
            PlayerSettings.runInBackground = true;

            Debug.Log("[Setup] Player Settings configured");
        }

        private static Sprite CreateSquareSprite()
        {
            // Create a simple 1x1 white texture
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
        }
    }
}
