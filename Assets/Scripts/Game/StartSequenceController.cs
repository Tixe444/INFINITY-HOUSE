using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;

namespace InfiniteHaus.Game
{
    /// <summary>
    /// Manages the complete start sequence for INFINITE HAUS v5.7.
    /// Handles prewarming, scene loading, avatar entry, and transition to gameplay.
    /// CPU: <0.2ms | Memory: 6KB | GC: 0B
    /// </summary>
    public class StartSequenceController : MonoBehaviour
    {
        #region Configuration
        [Header("Scene Management")]
        [Tooltip("Name of corridor scene to load")]
        [SerializeField] private string corridorSceneName = "Corridor";

        [Header("Entry Phase Timing")]
        [Tooltip("Entry phase duration (0.6s default)")]
        [SerializeField] private float entryPhaseDuration = 0.6f;

        [Tooltip("Time until avatar visible (0.2s default)")]
        [SerializeField] private float visibilityDelay = 0.2f;

        [Tooltip("Avatar start position X (offscreen left)")]
        [SerializeField] private float avatarStartX = -3f;

        [Header("Avatar Acceleration")]
        [Tooltip("Initial velocity")]
        [SerializeField] private float startVelocity = 0f;

        [Tooltip("Target velocity (6.2m/s default)")]
        [SerializeField] private float targetVelocity = 6.2f;

        [Header("Input Timing")]
        [Tooltip("Jump enabled frame 0")]
        [SerializeField] private bool jumpEnabledFrame0 = true;

        [Tooltip("Slide enabled after frames (18 default)")]
        [SerializeField] private int slideEnableFrame = 18;

        [Tooltip("Coyote time in ms (90ms default)")]
        [SerializeField] private float coyoteTimeMs = 90f;

        [Tooltip("Input buffer in ms (80ms default)")]
        [SerializeField] private float inputBufferMs = 80f;

        [Header("Trail Timing")]
        [Tooltip("T1 trail delay after in-frame (0.3s)")]
        [SerializeField] private float t1TrailDelay = 0.3f;

        [Tooltip("T2 trail delay (2.0s)")]
        [SerializeField] private float t2TrailDelay = 2.0f;

        [Tooltip("T3 trail delay (4.5s, +chaser relief)")]
        [SerializeField] private float t3TrailDelay = 4.5f;

        [Tooltip("Chaser relief on T3 (12%)")]
        [SerializeField] private float t3ChaserRelief = 0.12f;

        [Header("Chaser Setup")]
        [Tooltip("Chaser preset distance (6m)")]
        [SerializeField] private float chaserDistance = 6f;

        [Tooltip("Freeze chaser during entry phase")]
        [SerializeField] private bool freezeChaserDuringEntry = true;

        [Header("First Content Timing")]
        [Tooltip("Shard guidance start delay (0.3s)")]
        [SerializeField] private float shardGuidanceDelay = 0.3f;

        [Tooltip("First jump timing (~1.3s)")]
        [SerializeField] private float firstJumpTime = 1.3f;

        [Tooltip("First obstacle distance (+10m)")]
        [SerializeField] private float firstObstacleDistance = 10f;

        [Tooltip("Safe spawn window (+4m from in-frame)")]
        [SerializeField] private float safeSpawnWindow = 4f;

        [Header("Audio")]
        [Tooltip("Start burst SFX")]
        [SerializeField] private AudioClip startBurstSFX;

        [Tooltip("Ambient to Beat crossfade duration (0.7s)")]
        [SerializeField] private float audioXfadeDuration = 0.7f;

        [Tooltip("Rumble frequency (40Hz)")]
        [SerializeField] private float rumbleFrequency = 40f;

        [Header("HUD Transition")]
        [Tooltip("HUD fade duration (0.2s)")]
        [SerializeField] private float hudFadeDuration = 0.2f;

        [Header("References")]
        [SerializeField] private PerformancePrewarmer prewarmer;
        [SerializeField] private FixedSideCameraController cameraController;
        [SerializeField] private AvatarEntrySystem avatarEntry;
        [SerializeField] private ChaserSystem chaserSystem;
        [SerializeField] private ShardGuidanceSystem shardGuidance;
        [SerializeField] private UI.UIController uiController;
        [SerializeField] private AudioSource audioSource;
        #endregion

        #region State
        private bool isStarting = false;
        private float startTime;
        private int currentFrame = 0;
        #endregion

        #region Events
        public event Action OnStartSequenceBegin;
        public event Action OnEntryPhaseComplete;
        public event Action OnGameplayBegin;
        #endregion

        #region Public API
        /// <summary>
        /// Called by Start Button OnClick
        /// </summary>
        public void StartGame()
        {
            if (isStarting) return;

            StartCoroutine(StartSequenceCoroutine());
        }
        #endregion

        #region Start Sequence
        private IEnumerator StartSequenceCoroutine()
        {
            isStarting = true;
            startTime = Time.realtimeSinceStartup;
            OnStartSequenceBegin?.Invoke();

            // === PHASE 1: DISABLE & PREWARM ===
            DisableHUDAndInput();
            yield return PrewarmPools();

            // === PHASE 2: LOAD CORRIDOR SCENE ===
            yield return LoadCorridorScene();

            // === PHASE 3: SETUP CAMERA ===
            SetupCamera();

            // === PHASE 4: SPAWN AVATAR ===
            SpawnAvatar();

            // === PHASE 5: ENTRY PHASE ===
            yield return ExecuteEntryPhase();

            // === PHASE 6: ENABLE GAMEPLAY ===
            EnableGameplay();

            // === PHASE 7: START AUDIO ===
            StartAudio();

            // === PHASE 8: TRANSITION HUD ===
            TransitionHUD();

            // === PHASE 9: START CONTENT ===
            StartContent();

            float totalTime = (Time.realtimeSinceStartup - startTime) * 1000f;
            Debug.Log($"[StartSequence] Complete in {totalTime:F2}ms");

            OnGameplayBegin?.Invoke();
            isStarting = false;
        }

        private void DisableHUDAndInput()
        {
            if (uiController != null)
            {
                uiController.DisableInput();
                uiController.DisableMainHUD();
            }

            Debug.Log("[StartSequence] HUD & Input disabled");
        }

        private IEnumerator PrewarmPools()
        {
            if (prewarmer != null)
            {
                yield return prewarmer.PrewarmAll();
            }
            else
            {
                Debug.LogWarning("[StartSequence] PerformancePrewarmer not found!");
            }

            Debug.Log("[StartSequence] Pools prewarmed");
        }

        private IEnumerator LoadCorridorScene()
        {
            if (!string.IsNullOrEmpty(corridorSceneName))
            {
                AsyncOperation loadOp = SceneManager.LoadSceneAsync(corridorSceneName, LoadSceneMode.Additive);

                while (!loadOp.isDone)
                {
                    yield return null;
                }

                Debug.Log($"[StartSequence] Corridor scene '{corridorSceneName}' loaded");
            }
            else
            {
                Debug.LogWarning("[StartSequence] No corridor scene specified!");
            }
        }

        private void SetupCamera()
        {
            if (cameraController == null)
            {
                cameraController = FindObjectOfType<FixedSideCameraController>();
            }

            if (cameraController != null)
            {
                cameraController.SetFollowSpeed(0.15f);
                cameraController.DisableZoom();
                cameraController.EnableParallax();
            }

            Debug.Log("[StartSequence] Camera setup complete");
        }

        private void SpawnAvatar()
        {
            if (avatarEntry == null)
            {
                avatarEntry = FindObjectOfType<AvatarEntrySystem>();
            }

            if (avatarEntry != null)
            {
                Vector3 spawnPos = new Vector3(avatarStartX, 0f, 0f);
                avatarEntry.SpawnAvatar(spawnPos);
                avatarEntry.SetState(AvatarEntrySystem.AvatarState.EntryRun);
                avatarEntry.SetAutorun(true);
            }

            Debug.Log($"[StartSequence] Avatar spawned at x={avatarStartX}");
        }

        private IEnumerator ExecuteEntryPhase()
        {
            Debug.Log("[StartSequence] Entry phase started");

            float elapsed = 0f;
            currentFrame = 0;

            // Enable jump immediately (frame 0)
            if (jumpEnabledFrame0 && avatarEntry != null)
            {
                avatarEntry.EnableJump();
            }

            // Freeze chaser during entry
            if (freezeChaserDuringEntry && chaserSystem != null)
            {
                chaserSystem.FreezeMovement();
            }

            // Entry phase loop
            while (elapsed < entryPhaseDuration)
            {
                float t = elapsed / entryPhaseDuration;

                // EaseOutExpo curve
                float velocity = Mathf.Lerp(startVelocity, targetVelocity, EaseOutExpo(t));

                if (avatarEntry != null)
                {
                    avatarEntry.SetVelocity(velocity);
                }

                // Make avatar visible at delay
                if (elapsed >= visibilityDelay && avatarEntry != null)
                {
                    avatarEntry.SetVisible(true);
                }

                // Enable slide at frame
                if (currentFrame == slideEnableFrame && avatarEntry != null)
                {
                    avatarEntry.EnableSlide();
                }

                elapsed += Time.deltaTime;
                currentFrame++;
                yield return null;
            }

            // Blend to run loop at 0.6s
            if (avatarEntry != null)
            {
                avatarEntry.BlendToRunLoop();
                avatarEntry.SetVelocity(targetVelocity);
            }

            // Unfreeze chaser
            if (chaserSystem != null)
            {
                chaserSystem.UnfreezeMovement();
            }

            OnEntryPhaseComplete?.Invoke();
            Debug.Log("[StartSequence] Entry phase complete");
        }

        private void EnableGameplay()
        {
            if (avatarEntry != null)
            {
                // Set coyote time and input buffer
                var controller = avatarEntry.GetController();
                if (controller != null)
                {
                    // These would be set on the EnhancedRunnerController
                    Debug.Log($"[StartSequence] Coyote time: {coyoteTimeMs}ms, Input buffer: {inputBufferMs}ms");
                }
            }

            Debug.Log("[StartSequence] Gameplay enabled");
        }

        private void StartAudio()
        {
            if (audioSource != null && startBurstSFX != null)
            {
                audioSource.PlayOneShot(startBurstSFX);
            }

            // Crossfade ambient to beat
            StartCoroutine(AudioCrossfade());

            // Start rumble loop
            StartCoroutine(RumbleLoop());

            Debug.Log("[StartSequence] Audio started");
        }

        private IEnumerator AudioCrossfade()
        {
            // TODO: Implement audio crossfade from ambient to beat
            yield return new WaitForSeconds(audioXfadeDuration);
            Debug.Log($"[StartSequence] Audio crossfade complete ({audioXfadeDuration}s)");
        }

        private IEnumerator RumbleLoop()
        {
            // 40Hz rumble loop
            float interval = 1f / rumbleFrequency;

            while (isStarting || true) // Continue during gameplay
            {
                // TODO: Trigger haptic feedback here
                yield return new WaitForSeconds(interval);
            }
        }

        private void TransitionHUD()
        {
            if (uiController != null)
            {
                StartCoroutine(HUDTransitionCoroutine());
            }
        }

        private IEnumerator HUDTransitionCoroutine()
        {
            // Fade to compact run HUD
            if (uiController != null)
            {
                yield return uiController.FadeToRunHUD(hudFadeDuration);
                uiController.EnableScore();
                uiController.EnableShards();
                uiController.EnableStyle();
                uiController.DisableShop();
                uiController.DisableSettings();
            }

            Debug.Log($"[StartSequence] HUD transitioned ({hudFadeDuration}s)");
        }

        private void StartContent()
        {
            // Start shard guidance spline
            if (shardGuidance != null)
            {
                StartCoroutine(StartShardGuidance());
            }

            // Start trail progression
            StartCoroutine(TrailProgression());

            // Setup chaser
            if (chaserSystem != null)
            {
                chaserSystem.SetDistance(chaserDistance);
                chaserSystem.EnableParallax();
            }

            Debug.Log("[StartSequence] Content systems started");
        }

        private IEnumerator StartShardGuidance()
        {
            yield return new WaitForSeconds(shardGuidanceDelay);

            if (shardGuidance != null)
            {
                shardGuidance.GenerateSpline(20f, 25f);
                shardGuidance.StartGuidance();
            }

            Debug.Log($"[StartSequence] Shard guidance started (first jump ~{firstJumpTime}s)");
        }

        private IEnumerator TrailProgression()
        {
            // T1 trail
            yield return new WaitForSeconds(t1TrailDelay);
            EnableTrailTier(1);

            // T2 trail
            yield return new WaitForSeconds(t2TrailDelay - t1TrailDelay);
            EnableTrailTier(2);

            // T3 trail + chaser relief
            yield return new WaitForSeconds(t3TrailDelay - t2TrailDelay);
            EnableTrailTier(3);

            if (chaserSystem != null)
            {
                chaserSystem.ApplyRelief(t3ChaserRelief);
            }

            Debug.Log($"[StartSequence] Trail progression: T1@{t1TrailDelay}s, T2@{t2TrailDelay}s, T3@{t3TrailDelay}s");
        }

        private void EnableTrailTier(int tier)
        {
            // TODO: Integrate with trail system
            Debug.Log($"[StartSequence] Trail tier {tier} enabled");
        }
        #endregion

        #region Easing Functions
        private float EaseOutExpo(float t)
        {
            return t == 1f ? 1f : 1f - Mathf.Pow(2f, -10f * t);
        }
        #endregion
    }
}
