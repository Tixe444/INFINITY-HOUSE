using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System;
using System.Collections;
using InfinityHouse.Data;

namespace InfinityHouse.Game
{
    /// <summary>
    /// Biome Manager for INFINITE HAUS v5.9.
    /// Manages biome selection, smooth transitions, and movement parameter interpolation.
    /// ZIEL: Absolut smooth, momentum-preserving transitions zwischen Biomes.
    /// CPU: <0.2ms | Memory: 8KB | GC: 0B
    /// </summary>
    public class IH_BiomeManager : MonoBehaviour
    {
        #region Configuration
        [Header("Current State")]
        [SerializeField] private IH_BiomeConfig currentBiome;
        [SerializeField] private IH_BiomeConfig nextBiome;
        [SerializeField] private bool isTransitioning = false;

        [Header("Floor Configuration")]
        [SerializeField] private IH_FloorConfig floorConfig;
        [SerializeField] private int currentFloor = 1;

        [Header("Transition")]
        [SerializeField] private float transitionProgress = 0f;
        [SerializeField] private float transitionDuration = 0.8f;

        [Header("References")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private IIH_MoveTuner moveTuner;
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource ambientSource;
        #endregion

        #region State
        private IH_MoveParams currentMoveParams;
        private IH_MoveParams targetMoveParams;
        private Color currentBgColor;
        private Color targetBgColor;
        private float currentLightIntensity;
        private float targetLightIntensity;

        // Addressables handles
        private AsyncOperationHandle<AudioClip> musicHandle;
        private AsyncOperationHandle<AudioClip> ambientHandle;
        private bool hasLoadedAssets = false;
        #endregion

        #region Events
        public event Action<IH_BiomeConfig> OnBiomeChanged;
        public event Action<float> OnTransitionProgress; // 0-1
        public event Action OnTransitionComplete;
        #endregion

        #region Properties
        public IH_BiomeConfig CurrentBiome => currentBiome;
        public bool IsTransitioning => isTransitioning;
        public float TransitionProgress => transitionProgress;
        public int CurrentFloor => currentFloor;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            if (currentBiome != null)
            {
                InitializeBiome(currentBiome);
            }
        }

        private void OnDestroy()
        {
            // Release Addressables
            if (hasLoadedAssets)
            {
                if (musicHandle.IsValid())
                    Addressables.Release(musicHandle);
                if (ambientHandle.IsValid())
                    Addressables.Release(ambientHandle);
            }
        }
        #endregion

        #region Biome Selection
        /// <summary>
        /// Selects next biome based on floor and door choice
        /// </summary>
        /// <param name="doorBias">-1 = left door, 1 = right door</param>
        public void SelectNextBiome(float doorBias)
        {
            if (floorConfig == null)
            {
                Debug.LogWarning("[BiomeManager] No FloorConfig assigned!");
                return;
            }

            currentFloor++;

            IH_BiomeConfig selected = floorConfig.SelectBiome(currentFloor, doorBias);

            if (selected != null)
            {
                TransitionToBiome(selected);
            }
            else
            {
                Debug.LogWarning($"[BiomeManager] No valid biome for floor {currentFloor}");
            }
        }

        /// <summary>
        /// Manually set next biome
        /// </summary>
        public void TransitionToBiome(IH_BiomeConfig biome)
        {
            if (biome == null || isTransitioning)
                return;

            nextBiome = biome;
            StartCoroutine(TransitionCoroutine());
        }
        #endregion

        #region Transition Logic
        private IEnumerator TransitionCoroutine()
        {
            isTransitioning = true;
            transitionProgress = 0f;

            // Get transition duration from new biome
            transitionDuration = nextBiome.transitionDurationS;

            // Setup target parameters
            targetMoveParams = nextBiome.GetMoveParams();
            targetBgColor = nextBiome.backgroundColor;
            targetLightIntensity = nextBiome.lightingIntensity;

            // Store current parameters
            if (currentBiome != null)
            {
                currentMoveParams = currentBiome.GetMoveParams();
                currentBgColor = currentBiome.backgroundColor;
                currentLightIntensity = currentBiome.lightingIntensity;
            }

            // Start music/ambient crossfade
            StartCoroutine(CrossfadeAudio());

            // WICHTIG: Momentum bleibt erhalten während Transition!
            float elapsed = 0f;

            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                transitionProgress = elapsed / transitionDuration;

                // Evaluate curve if available
                float t = transitionProgress;
                if (nextBiome.useSmoothParamInterpolation && nextBiome.lightBlendCurve != null)
                {
                    t = nextBiome.lightBlendCurve.Evaluate(transitionProgress);
                }

                // Interpolate movement parameters (SMOOTH!)
                IH_MoveParams lerpedParams = IH_MoveParams.Lerp(currentMoveParams, targetMoveParams, t);
                ApplyMovementParams(lerpedParams);

                // Interpolate visuals
                InterpolateVisuals(t);

                OnTransitionProgress?.Invoke(transitionProgress);

                yield return null;
            }

            // Finalize transition
            ApplyMovementParams(targetMoveParams);
            InterpolateVisuals(1f);

            // Swap biomes
            currentBiome = nextBiome;
            nextBiome = null;

            isTransitioning = false;
            transitionProgress = 1f;

            OnBiomeChanged?.Invoke(currentBiome);
            OnTransitionComplete?.Invoke();

            #if UNITY_EDITOR
            Debug.Log($"[BiomeManager] Transition complete: {currentBiome.biomeName}");
            #endif
        }

        private void ApplyMovementParams(IH_MoveParams params)
        {
            if (moveTuner != null)
            {
                moveTuner.ApplyBiomeParams(in params);
            }
        }

        private void InterpolateVisuals(float t)
        {
            // Background color
            if (mainCamera != null)
            {
                mainCamera.backgroundColor = Color.Lerp(currentBgColor, targetBgColor, t);
            }

            // Lighting intensity (würde hier mit Light2D arbeiten)
            // TODO: Apply to Light2D global light

            // Parallax würde hier auch interpoliert werden
            // TODO: Parallax layer interpolation
        }

        private IEnumerator CrossfadeAudio()
        {
            if (musicSource == null || nextBiome.musicRef == null)
                yield break;

            // Fade out current
            float startVolume = musicSource.volume;
            float elapsed = 0f;
            float fadeDuration = transitionDuration * 0.5f;

            while (elapsed < fadeDuration && musicSource.volume > 0f)
            {
                elapsed += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeDuration);
                yield return null;
            }

            musicSource.Stop();

            // Load and play new music
            musicHandle = nextBiome.musicRef.LoadAssetAsync<AudioClip>();
            yield return musicHandle;

            if (musicHandle.Status == AsyncOperationStatus.Succeeded)
            {
                musicSource.clip = musicHandle.Result;
                musicSource.Play();

                // Fade in
                elapsed = 0f;
                while (elapsed < fadeDuration && musicSource.volume < startVolume)
                {
                    elapsed += Time.deltaTime;
                    musicSource.volume = Mathf.Lerp(0f, startVolume, elapsed / fadeDuration);
                    yield return null;
                }

                musicSource.volume = startVolume;
                hasLoadedAssets = true;
            }
        }
        #endregion

        #region Initialization
        private void InitializeBiome(IH_BiomeConfig biome)
        {
            currentBiome = biome;
            currentMoveParams = biome.GetMoveParams();
            ApplyMovementParams(currentMoveParams);

            // Apply visuals immediately
            if (mainCamera != null)
            {
                mainCamera.backgroundColor = biome.backgroundColor;
            }

            #if UNITY_EDITOR
            Debug.Log($"[BiomeManager] Initialized: {biome.biomeName}");
            #endif
        }
        #endregion

        #region Public API
        /// <summary>
        /// Gets current difficulty for spawning
        /// </summary>
        public float GetCurrentDifficulty()
        {
            if (floorConfig == null)
                return 0.5f;

            return floorConfig.GetDifficulty(currentFloor);
        }

        /// <summary>
        /// Gets current speed multiplier
        /// </summary>
        public float GetCurrentSpeedMultiplier()
        {
            if (floorConfig == null)
                return 1f;

            return floorConfig.GetSpeedMultiplier(currentFloor);
        }

        /// <summary>
        /// Resets to floor 1
        /// </summary>
        public void ResetToFloor1()
        {
            currentFloor = 1;
            transitionProgress = 0f;
            isTransitioning = false;

            if (floorConfig != null && floorConfig.candidateBiomes.Count > 0)
            {
                IH_BiomeConfig firstBiome = floorConfig.candidateBiomes[0].biomeRef;
                if (firstBiome != null)
                {
                    InitializeBiome(firstBiome);
                }
            }
        }

        /// <summary>
        /// Sets move tuner (player controller)
        /// </summary>
        public void SetMoveTuner(IIH_MoveTuner tuner)
        {
            moveTuner = tuner;
            if (currentBiome != null)
            {
                ApplyMovementParams(currentMoveParams);
            }
        }
        #endregion
    }

    /// <summary>
    /// Interface for objects that can receive biome movement parameters
    /// </summary>
    public interface IIH_MoveTuner
    {
        void ApplyBiomeParams(in IH_MoveParams params);
    }
}
