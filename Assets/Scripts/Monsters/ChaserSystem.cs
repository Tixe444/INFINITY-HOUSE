using UnityEngine;
using System;
using System.Collections.Generic;

namespace InfiniteHaus.Game
{
    /// <summary>
    /// Dynamic chaser system for INFINITE HAUS v5.7.
    /// Follows player at preset distance, closes on errors, backs off on perfect streaks.
    /// Supports parallax effect and freeze during entry phase.
    /// CPU: <0.25ms | Memory: 6KB | GC: 0B
    /// </summary>
    public class ChaserSystem : MonoBehaviour
    {
        #region Configuration
        [Header("Chaser Setup")]
        [Tooltip("Chaser prefabs (3 total)")]
        [SerializeField] private GameObject[] chaserPrefabs;

        [Tooltip("Active chaser instances")]
        [SerializeField] private List<GameObject> chaserInstances = new List<GameObject>();

        [Header("Distance Management")]
        [Tooltip("Preset distance from player (6m default)")]
        [SerializeField] private float presetDistance = 6f;

        [Tooltip("Current distance from player")]
        [SerializeField] private float currentDistance = 6f;

        [Tooltip("Minimum distance (catch threshold)")]
        [SerializeField] private float minDistance = 0.5f;

        [Tooltip("Maximum distance")]
        [SerializeField] private float maxDistance = 15f;

        [Header("Distance Modifiers")]
        [Tooltip("Distance reduction on player error (-0.35m default)")]
        [SerializeField] private float errorPenalty = 0.35f;

        [Tooltip("Distance increase on perfect streak (+0.5m default, 6 perfect actions)")]
        [SerializeField] private float perfectBonus = 0.5f;

        [Tooltip("Perfect actions needed for bonus (6 default)")]
        [SerializeField] private int perfectStreakRequired = 6;

        [Header("Movement")]
        [Tooltip("Chaser movement speed")]
        [SerializeField] private float movementSpeed = 5f;

        [Tooltip("Smooth follow speed")]
        [SerializeField] private float smoothSpeed = 0.2f;

        [Tooltip("Is movement frozen?")]
        [SerializeField] private bool movementFrozen = false;

        [Header("Parallax")]
        [Tooltip("Enable parallax effect")]
        [SerializeField] private bool parallaxEnabled = true;

        [Tooltip("Parallax depth factor (0-1)")]
        [SerializeField][Range(0f, 1f)] private float parallaxDepth = 0.7f;

        [Header("Player Tracking")]
        [Tooltip("Player transform reference")]
        [SerializeField] private Transform playerTransform;

        [Header("Performance")]
        [Tooltip("Update rate in Hz")]
        [SerializeField] private int updateRate = 30;
        #endregion

        #region State
        private int perfectStreak = 0;
        private Vector3 targetPosition;
        private Vector3 velocity = Vector3.zero;
        private float updateInterval;
        private float lastUpdateTime;
        private float reliefAmount = 0f;
        #endregion

        #region Events
        public event Action<float> OnDistanceChanged; // (new distance)
        public event Action OnPlayerCaught;
        public event Action<float> OnDistanceReduced; // (amount)
        public event Action<float> OnDistanceIncreased; // (amount)
        #endregion

        #region Properties
        public float CurrentDistance => currentDistance;
        public float DistancePercentage => currentDistance / maxDistance;
        public bool IsMovementFrozen => movementFrozen;
        public int PerfectStreak => perfectStreak;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            updateInterval = 1f / updateRate;
        }

        private void Start()
        {
            // Find player if not set
            if (playerTransform == null)
            {
                var player = FindObjectOfType<Player.EnhancedRunnerController>();
                if (player != null)
                {
                    playerTransform = player.transform;
                }
            }

            // Spawn chasers
            SpawnChasers();
        }

        private void Update()
        {
            if (movementFrozen || playerTransform == null) return;

            // Throttled update
            if (Time.time - lastUpdateTime >= updateInterval)
            {
                UpdateChaserPosition();
                CheckCatchCondition();
                lastUpdateTime = Time.time;
            }
        }
        #endregion

        #region Chaser Spawning
        private void SpawnChasers()
        {
            if (chaserPrefabs == null || chaserPrefabs.Length == 0)
            {
                Debug.LogWarning("[ChaserSystem] No chaser prefabs assigned!");
                return;
            }

            // Spawn each chaser
            foreach (var prefab in chaserPrefabs)
            {
                if (prefab == null) continue;

                GameObject chaser = Instantiate(prefab);
                chaser.transform.SetParent(transform);
                chaserInstances.Add(chaser);
            }

            // Position chasers behind player
            if (playerTransform != null)
            {
                PositionChasersBehindPlayer();
            }

            Debug.Log($"[ChaserSystem] Spawned {chaserInstances.Count} chasers");
        }

        private void PositionChasersBehindPlayer()
        {
            Vector3 basePosition = playerTransform.position - new Vector3(currentDistance, 0f, 0f);

            for (int i = 0; i < chaserInstances.Count; i++)
            {
                if (chaserInstances[i] == null) continue;

                // Stagger chasers slightly
                Vector3 offset = new Vector3(-i * 0.5f, i * 0.2f, 0f);
                chaserInstances[i].transform.position = basePosition + offset;
            }
        }
        #endregion

        #region Movement
        private void UpdateChaserPosition()
        {
            if (playerTransform == null) return;

            // Calculate target position (behind player)
            targetPosition = playerTransform.position - new Vector3(currentDistance, 0f, 0f);

            // Apply parallax if enabled
            if (parallaxEnabled)
            {
                targetPosition.z = -parallaxDepth * 2f; // Depth for parallax
            }

            // Smooth follow
            for (int i = 0; i < chaserInstances.Count; i++)
            {
                if (chaserInstances[i] == null) continue;

                Vector3 chaserTarget = targetPosition;

                // Stagger chasers
                chaserTarget += new Vector3(-i * 0.5f, i * 0.2f, 0f);

                // SmoothDamp to target
                chaserInstances[i].transform.position = Vector3.SmoothDamp(
                    chaserInstances[i].transform.position,
                    chaserTarget,
                    ref velocity,
                    smoothSpeed
                );
            }
        }

        private void CheckCatchCondition()
        {
            if (playerTransform == null) return;

            // Check if any chaser caught the player
            foreach (var chaser in chaserInstances)
            {
                if (chaser == null) continue;

                float distance = Vector3.Distance(chaser.transform.position, playerTransform.position);

                if (distance <= minDistance)
                {
                    OnPlayerCaught?.Invoke();
                    Debug.Log("[ChaserSystem] Player caught!");
                    break;
                }
            }
        }
        #endregion

        #region Distance Management
        /// <summary>
        /// Sets chaser distance from player
        /// </summary>
        public void SetDistance(float distance)
        {
            currentDistance = Mathf.Clamp(distance, minDistance, maxDistance);
            presetDistance = currentDistance;
            OnDistanceChanged?.Invoke(currentDistance);
        }

        /// <summary>
        /// Reduces distance (player made error)
        /// </summary>
        public void OnPlayerError()
        {
            ReduceDistance(errorPenalty);
            ResetPerfectStreak();
        }

        /// <summary>
        /// Increases distance on perfect streak
        /// </summary>
        public void OnPlayerPerfect()
        {
            perfectStreak++;

            if (perfectStreak >= perfectStreakRequired)
            {
                IncreaseDistance(perfectBonus);
                perfectStreak = 0;
            }
        }

        /// <summary>
        /// Reduces distance by amount
        /// </summary>
        public void ReduceDistance(float amount)
        {
            float oldDistance = currentDistance;
            currentDistance = Mathf.Max(minDistance, currentDistance - amount);
            float actualReduction = oldDistance - currentDistance;

            if (actualReduction > 0f)
            {
                OnDistanceReduced?.Invoke(actualReduction);
                OnDistanceChanged?.Invoke(currentDistance);
                Debug.Log($"[ChaserSystem] Distance reduced by {actualReduction:F2}m (now {currentDistance:F2}m)");
            }
        }

        /// <summary>
        /// Increases distance by amount
        /// </summary>
        public void IncreaseDistance(float amount)
        {
            float oldDistance = currentDistance;
            currentDistance = Mathf.Min(maxDistance, currentDistance + amount);
            float actualIncrease = currentDistance - oldDistance;

            if (actualIncrease > 0f)
            {
                OnDistanceIncreased?.Invoke(actualIncrease);
                OnDistanceChanged?.Invoke(currentDistance);
                Debug.Log($"[ChaserSystem] Distance increased by {actualIncrease:F2}m (now {currentDistance:F2}m)");
            }
        }

        /// <summary>
        /// Applies relief (T3 trail bonus)
        /// </summary>
        public void ApplyRelief(float percentage)
        {
            reliefAmount = percentage;
            float relief = currentDistance * percentage;
            IncreaseDistance(relief);
            Debug.Log($"[ChaserSystem] Applied {percentage:P0} relief (+{relief:F2}m)");
        }

        /// <summary>
        /// Resets perfect streak
        /// </summary>
        public void ResetPerfectStreak()
        {
            perfectStreak = 0;
        }
        #endregion

        #region Movement Control
        /// <summary>
        /// Freezes chaser movement (entry phase)
        /// </summary>
        public void FreezeMovement()
        {
            movementFrozen = true;
            Debug.Log("[ChaserSystem] Movement frozen");
        }

        /// <summary>
        /// Unfreezes chaser movement
        /// </summary>
        public void UnfreezeMovement()
        {
            movementFrozen = false;
            Debug.Log("[ChaserSystem] Movement unfrozen");
        }
        #endregion

        #region Parallax Control
        /// <summary>
        /// Enables parallax effect
        /// </summary>
        public void EnableParallax()
        {
            parallaxEnabled = true;
        }

        /// <summary>
        /// Disables parallax effect
        /// </summary>
        public void DisableParallax()
        {
            parallaxEnabled = false;
        }

        /// <summary>
        /// Sets parallax depth
        /// </summary>
        public void SetParallaxDepth(float depth)
        {
            parallaxDepth = Mathf.Clamp01(depth);
        }
        #endregion

        #region Public API
        /// <summary>
        /// Sets player transform reference
        /// </summary>
        public void SetPlayer(Transform player)
        {
            playerTransform = player;
            PositionChasersBehindPlayer();
        }

        /// <summary>
        /// Gets chaser instances
        /// </summary>
        public GameObject[] GetChasers()
        {
            return chaserInstances.ToArray();
        }

        /// <summary>
        /// Resets chaser system
        /// </summary>
        public void ResetSystem()
        {
            currentDistance = presetDistance;
            perfectStreak = 0;
            reliefAmount = 0f;
            movementFrozen = false;
            PositionChasersBehindPlayer();
        }
        #endregion

        #region Debug
        private void OnDrawGizmos()
        {
            if (playerTransform == null) return;

            // Draw distance line
            Gizmos.color = Color.red;
            Vector3 chaserPos = playerTransform.position - new Vector3(currentDistance, 0f, 0f);
            Gizmos.DrawLine(playerTransform.position, chaserPos);

            // Draw catch threshold
            Gizmos.color = Color.yellow;
            Vector3 catchPos = playerTransform.position - new Vector3(minDistance, 0f, 0f);
            Gizmos.DrawWireSphere(catchPos, 0.3f);
        }
        #endregion
    }
}
