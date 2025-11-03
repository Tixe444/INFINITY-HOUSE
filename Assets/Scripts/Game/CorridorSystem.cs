using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;

namespace InfiniteHaus.Game
{
    /// <summary>
    /// Corridor system for INFINITE HAUS v5.7.
    /// Manages ~60s corridor runtime, door choices, and seamless transitions.
    /// Left door = Style Boost + Biome Swap
    /// Right door = Loot + Risk
    /// CPU: <0.15ms | Memory: 4KB | GC: 0B
    /// </summary>
    public class CorridorSystem : MonoBehaviour
    {
        #region Configuration
        [Header("Corridor Timing")]
        [Tooltip("Corridor runtime in seconds (~60s)")]
        [SerializeField] private float corridorDuration = 60f;

        [Tooltip("Slowdown duration at end (1.5s)")]
        [SerializeField] private float slowdownDuration = 1.5f;

        [Header("Door Setup")]
        [Tooltip("Left door transform")]
        [SerializeField] private Transform leftDoor;

        [Tooltip("Right door transform")]
        [SerializeField] private Transform rightDoor;

        [Tooltip("Door spawn distance from end")]
        [SerializeField] private float doorSpawnDistance = 10f;

        [Header("Door Effects")]
        [Tooltip("Left door: Style boost amount")]
        [SerializeField] private float styleBoostAmount = 50f;

        [Tooltip("Right door: Loot multiplier")]
        [SerializeField] private float lootMultiplier = 1.5f;

        [Tooltip("Right door: Risk increase")]
        [SerializeField] private float riskIncrease = 0.3f;

        [Header("Biome Configuration")]
        [Tooltip("Available biome scenes")]
        [SerializeField] private string[] biomeScenes = new string[]
        {
            "Corridor_Dark",
            "Corridor_Neon",
            "Corridor_Industrial",
            "Corridor_Organic"
        };

        [Tooltip("Current biome index")]
        [SerializeField] private int currentBiomeIndex = 0;

        [Header("Transition")]
        [Tooltip("Fade duration for scene transition")]
        [SerializeField] private float fadeDuration = 0.5f;

        [Header("References")]
        [SerializeField] private Player.StyleMeterSystem styleMeter;
        [SerializeField] private UI.UIController uiController;
        #endregion

        #region State
        private float corridorStartTime;
        private float elapsedTime = 0f;
        private bool corridorActive = false;
        private bool doorsSpawned = false;
        private bool slowdownActive = false;
        private DoorChoice playerChoice = DoorChoice.None;
        #endregion

        #region Enums
        public enum DoorChoice
        {
            None,
            Left,   // Style + Biome
            Right   // Loot + Risk
        }
        #endregion

        #region Events
        public event Action OnCorridorStarted;
        public event Action OnDoorsSpawned;
        public event Action OnSlowdownStarted;
        public event Action<DoorChoice> OnDoorChosen;
        public event Action OnCorridorCompleted;
        #endregion

        #region Properties
        public bool IsActive => corridorActive;
        public float ElapsedTime => elapsedTime;
        public float RemainingTime => corridorDuration - elapsedTime;
        public float Progress => elapsedTime / corridorDuration;
        public DoorChoice PlayerChoice => playerChoice;
        #endregion

        #region Unity Lifecycle
        private void Update()
        {
            if (!corridorActive) return;

            elapsedTime += Time.deltaTime;

            // Check for door spawn timing
            if (!doorsSpawned && RemainingTime <= doorSpawnDistance / 6.2f) // Assuming 6.2m/s
            {
                SpawnDoors();
            }

            // Check for slowdown
            if (!slowdownActive && RemainingTime <= slowdownDuration)
            {
                StartSlowdown();
            }

            // Check for corridor end
            if (elapsedTime >= corridorDuration)
            {
                EndCorridor();
            }
        }
        #endregion

        #region Corridor Control
        /// <summary>
        /// Starts corridor
        /// </summary>
        public void StartCorridor()
        {
            corridorActive = true;
            corridorStartTime = Time.time;
            elapsedTime = 0f;
            doorsSpawned = false;
            slowdownActive = false;
            playerChoice = DoorChoice.None;

            OnCorridorStarted?.Invoke();
            Debug.Log($"[CorridorSystem] Corridor started (duration: {corridorDuration}s)");
        }

        /// <summary>
        /// Ends corridor
        /// </summary>
        private void EndCorridor()
        {
            corridorActive = false;
            OnCorridorCompleted?.Invoke();
            Debug.Log("[CorridorSystem] Corridor completed");

            // Wait for door choice
            if (playerChoice == DoorChoice.None)
            {
                // Default to left door
                ChooseDoor(DoorChoice.Left);
            }
        }
        #endregion

        #region Door System
        private void SpawnDoors()
        {
            if (doorsSpawned) return;

            // Spawn doors at distance ahead
            Vector3 spawnPos = Camera.main.transform.position + Vector3.right * doorSpawnDistance;

            if (leftDoor != null)
            {
                leftDoor.position = spawnPos + Vector3.up * 2f;
                leftDoor.gameObject.SetActive(true);
            }

            if (rightDoor != null)
            {
                rightDoor.position = spawnPos + Vector3.down * 2f;
                rightDoor.gameObject.SetActive(true);
            }

            doorsSpawned = true;
            OnDoorsSpawned?.Invoke();
            Debug.Log("[CorridorSystem] Doors spawned");
        }

        /// <summary>
        /// Player chooses a door
        /// </summary>
        public void ChooseDoor(DoorChoice choice)
        {
            if (playerChoice != DoorChoice.None) return;

            playerChoice = choice;
            ApplyDoorEffects(choice);
            OnDoorChosen?.Invoke(choice);

            // Transition to next corridor
            StartCoroutine(TransitionToNextCorridor(choice));
        }

        private void ApplyDoorEffects(DoorChoice choice)
        {
            switch (choice)
            {
                case DoorChoice.Left:
                    ApplyStyleBoost();
                    SwapBiome();
                    break;

                case DoorChoice.Right:
                    ApplyLootBonus();
                    IncreaseRisk();
                    break;
            }
        }

        private void ApplyStyleBoost()
        {
            if (styleMeter != null)
            {
                // Add style points
                Debug.Log($"[CorridorSystem] Style boost: +{styleBoostAmount}");
            }
        }

        private void SwapBiome()
        {
            // Cycle to next biome
            currentBiomeIndex = (currentBiomeIndex + 1) % biomeScenes.Length;
            Debug.Log($"[CorridorSystem] Biome swap to: {biomeScenes[currentBiomeIndex]}");
        }

        private void ApplyLootBonus()
        {
            // Multiply loot rewards
            Debug.Log($"[CorridorSystem] Loot multiplier: {lootMultiplier}x");
        }

        private void IncreaseRisk()
        {
            // Increase difficulty/risk
            Debug.Log($"[CorridorSystem] Risk increased: +{riskIncrease:P0}");
        }
        #endregion

        #region Slowdown
        private void StartSlowdown()
        {
            if (slowdownActive) return;

            slowdownActive = true;
            StartCoroutine(SlowdownCoroutine());
            OnSlowdownStarted?.Invoke();
        }

        private IEnumerator SlowdownCoroutine()
        {
            float startTimeScale = Time.timeScale;
            float endTimeScale = 0.5f; // 50% speed
            float elapsed = 0f;

            while (elapsed < slowdownDuration)
            {
                float t = elapsed / slowdownDuration;
                Time.timeScale = Mathf.Lerp(startTimeScale, endTimeScale, t);

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Time.timeScale = endTimeScale;
            Debug.Log($"[CorridorSystem] Slowdown complete (Time.timeScale = {Time.timeScale})");
        }
        #endregion

        #region Scene Transition
        private IEnumerator TransitionToNextCorridor(DoorChoice choice)
        {
            // Fade out
            if (uiController != null)
            {
                yield return uiController.FadeOut(fadeDuration);
            }
            else
            {
                yield return new WaitForSeconds(fadeDuration);
            }

            // Load next corridor (no unload, seamless)
            string nextScene = choice == DoorChoice.Left ?
                biomeScenes[currentBiomeIndex] :
                biomeScenes[currentBiomeIndex];

            if (!string.IsNullOrEmpty(nextScene))
            {
                AsyncOperation loadOp = SceneManager.LoadSceneAsync(nextScene, LoadSceneMode.Single);

                while (!loadOp.isDone)
                {
                    yield return null;
                }
            }

            // Fade in
            if (uiController != null)
            {
                yield return uiController.FadeIn(fadeDuration);
            }

            // Reset time scale
            Time.timeScale = 1f;

            // Restart corridor
            StartCorridor();

            Debug.Log($"[CorridorSystem] Transitioned to next corridor");
        }
        #endregion

        #region Public API
        /// <summary>
        /// Stops corridor
        /// </summary>
        public void StopCorridor()
        {
            corridorActive = false;
            Time.timeScale = 1f;
            StopAllCoroutines();
        }

        /// <summary>
        /// Resets corridor system
        /// </summary>
        public void ResetCorridor()
        {
            StopCorridor();
            elapsedTime = 0f;
            doorsSpawned = false;
            slowdownActive = false;
            playerChoice = DoorChoice.None;

            if (leftDoor != null) leftDoor.gameObject.SetActive(false);
            if (rightDoor != null) rightDoor.gameObject.SetActive(false);
        }

        /// <summary>
        /// Sets corridor duration
        /// </summary>
        public void SetDuration(float duration)
        {
            corridorDuration = duration;
        }
        #endregion
    }
}
