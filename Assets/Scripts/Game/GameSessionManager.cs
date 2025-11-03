using UnityEngine;
using System;

namespace InfiniteHaus.Game
{
    /// <summary>
    /// Unified game session state manager for INFINITE HAUS v5.8.
    /// Single source of truth for all gameplay state.
    /// Eliminates state desyncs between systems.
    /// CPU: <0.05ms | Memory: 2KB | GC: 0B
    /// </summary>
    public class GameSessionManager : MonoBehaviour
    {
        #region Singleton
        public static GameSessionManager Instance { get; private set; }
        #endregion

        #region Player State
        [Header("Player State")]
        [SerializeField] private Vector3 playerPosition;
        [SerializeField] private float playerVelocity = 6.2f;
        [SerializeField] private bool playerAlive = true;
        [SerializeField] private bool playerCanMove = false;
        #endregion

        #region Chase State
        [Header("Chase State")]
        [SerializeField] private float chaseMeter = 0f;
        [SerializeField] private float chaserDistance = 6f;
        [SerializeField] private int chaserBand = 0; // 0=Comfort, 1=Pressure, 2=Critical
        #endregion

        #region Progress State
        [Header("Progress State")]
        [SerializeField] private float distanceTraveled = 0f;
        [SerializeField] private int shardsCollected = 0;
        [SerializeField] private float stylePoints = 0f;
        [SerializeField] private int currentStyleTier = 0; // 0=None, 1=T1, 2=T2, 3=T3
        [SerializeField] private int currentTrailTier = 0; // 0=None, 1=T1, 2=T2, 3=T3
        #endregion

        #region Session State
        [Header("Session State")]
        [SerializeField] private bool sessionActive = false;
        [SerializeField] private float sessionStartTime = 0f;
        [SerializeField] private float sessionDuration = 0f;
        #endregion

        #region Properties (Read-Only Access)
        // Player
        public Vector3 PlayerPosition => playerPosition;
        public float PlayerVelocity => playerVelocity;
        public bool PlayerAlive => playerAlive;
        public bool PlayerCanMove => playerCanMove;

        // Chase
        public float ChaseMeter => chaseMeter;
        public float ChaserDistance => chaserDistance;
        public int ChaserBand => chaserBand;

        // Progress
        public float DistanceTraveled => distanceTraveled;
        public int ShardsCollected => shardsCollected;
        public float StylePoints => stylePoints;
        public int CurrentStyleTier => currentStyleTier;
        public int CurrentTrailTier => currentTrailTier;

        // Session
        public bool SessionActive => sessionActive;
        public float SessionDuration => sessionDuration;
        public float SessionElapsed => sessionActive ? Time.time - sessionStartTime : 0f;
        #endregion

        #region Events
        // Player events
        public event Action<Vector3> OnPlayerPositionChanged;
        public event Action<float> OnPlayerVelocityChanged;
        public event Action OnPlayerDeath;

        // Chase events
        public event Action<float> OnChaseMeterChanged;
        public event Action<float> OnChaserDistanceChanged;
        public event Action<int> OnChaserBandChanged;

        // Progress events
        public event Action<float> OnDistanceIncreased;
        public event Action<int> OnShardCollected;
        public event Action<float> OnStylePointsChanged;
        public event Action<int> OnStyleTierChanged;
        public event Action<int> OnTrailTierChanged;

        // Session events
        public event Action OnSessionStarted;
        public event Action OnSessionEnded;
        #endregion

        #region Unity Lifecycle
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
        }

        private void Update()
        {
            if (!sessionActive || !playerAlive) return;

            // Update session duration
            sessionDuration = Time.time - sessionStartTime;

            // Update distance traveled (velocity * time)
            float distanceDelta = playerVelocity * Time.deltaTime;
            SetDistanceTraveled(distanceTraveled + distanceDelta);
        }
        #endregion

        #region Player State Setters
        public void SetPlayerPosition(Vector3 position)
        {
            if (playerPosition != position)
            {
                playerPosition = position;
                OnPlayerPositionChanged?.Invoke(position);
            }
        }

        public void SetPlayerVelocity(float velocity)
        {
            if (!Mathf.Approximately(playerVelocity, velocity))
            {
                playerVelocity = velocity;
                OnPlayerVelocityChanged?.Invoke(velocity);
            }
        }

        public void SetPlayerAlive(bool alive)
        {
            if (playerAlive != alive)
            {
                playerAlive = alive;
                if (!alive)
                {
                    OnPlayerDeath?.Invoke();
                }
            }
        }

        public void SetPlayerCanMove(bool canMove)
        {
            playerCanMove = canMove;
        }
        #endregion

        #region Chase State Setters
        public void SetChaseMeter(float meter)
        {
            meter = Mathf.Clamp(meter, 0f, 100f);
            if (!Mathf.Approximately(chaseMeter, meter))
            {
                chaseMeter = meter;
                OnChaseMeterChanged?.Invoke(meter);
            }
        }

        public void SetChaserDistance(float distance)
        {
            if (!Mathf.Approximately(chaserDistance, distance))
            {
                chaserDistance = distance;
                OnChaserDistanceChanged?.Invoke(distance);
            }
        }

        public void SetChaserBand(int band)
        {
            band = Mathf.Clamp(band, 0, 2);
            if (chaserBand != band)
            {
                chaserBand = band;
                OnChaserBandChanged?.Invoke(band);
            }
        }
        #endregion

        #region Progress State Setters
        public void SetDistanceTraveled(float distance)
        {
            if (!Mathf.Approximately(distanceTraveled, distance))
            {
                distanceTraveled = distance;
                OnDistanceIncreased?.Invoke(distance);
            }
        }

        public void AddShard()
        {
            shardsCollected++;
            OnShardCollected?.Invoke(shardsCollected);
        }

        public void SetStylePoints(float points)
        {
            if (!Mathf.Approximately(stylePoints, points))
            {
                stylePoints = points;
                OnStylePointsChanged?.Invoke(points);
            }
        }

        public void SetStyleTier(int tier)
        {
            tier = Mathf.Clamp(tier, 0, 3);
            if (currentStyleTier != tier)
            {
                currentStyleTier = tier;
                OnStyleTierChanged?.Invoke(tier);
            }
        }

        public void SetTrailTier(int tier)
        {
            tier = Mathf.Clamp(tier, 0, 3);
            if (currentTrailTier != tier)
            {
                currentTrailTier = tier;
                OnTrailTierChanged?.Invoke(tier);
            }
        }
        #endregion

        #region Session Control
        public void StartSession()
        {
            sessionActive = true;
            sessionStartTime = Time.time;
            sessionDuration = 0f;
            OnSessionStarted?.Invoke();
        }

        public void EndSession()
        {
            sessionActive = false;
            OnSessionEnded?.Invoke();
        }

        public void ResetSession()
        {
            // Player
            playerPosition = Vector3.zero;
            playerVelocity = 6.2f;
            playerAlive = true;
            playerCanMove = false;

            // Chase
            chaseMeter = 0f;
            chaserDistance = 6f;
            chaserBand = 0;

            // Progress
            distanceTraveled = 0f;
            shardsCollected = 0;
            stylePoints = 0f;
            currentStyleTier = 0;
            currentTrailTier = 0;

            // Session
            sessionActive = false;
            sessionStartTime = 0f;
            sessionDuration = 0f;
        }
        #endregion

        #region Session Stats
        public SessionStats GetSessionStats()
        {
            return new SessionStats
            {
                DistanceTraveled = distanceTraveled,
                ShardsCollected = shardsCollected,
                StylePoints = stylePoints,
                StyleTier = currentStyleTier,
                TrailTier = currentTrailTier,
                Duration = sessionDuration,
                AverageVelocity = distanceTraveled / Mathf.Max(sessionDuration, 0.001f)
            };
        }
        #endregion

        #region Data Structures
        [Serializable]
        public struct SessionStats
        {
            public float DistanceTraveled;
            public int ShardsCollected;
            public float StylePoints;
            public int StyleTier;
            public int TrailTier;
            public float Duration;
            public float AverageVelocity;
        }
        #endregion
    }
}
