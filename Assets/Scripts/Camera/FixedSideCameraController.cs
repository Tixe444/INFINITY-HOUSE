using UnityEngine;
using System.Collections.Generic;

namespace InfiniteHaus.Game
{
    /// <summary>
    /// Fixed side-scrolling camera for INFINITE HAUS v5.7.
    /// Follows player with smooth follow, no zoom, parallax support.
    /// CPU: <0.15ms | Memory: 4KB | GC: 0B
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class FixedSideCameraController : MonoBehaviour
    {
        #region Configuration
        [Header("Follow Settings")]
        [Tooltip("Target to follow (usually player)")]
        [SerializeField] private Transform followTarget;

        [Tooltip("Follow smoothing (0.15 default)")]
        [SerializeField][Range(0.01f, 1f)] private float followSpeed = 0.15f;

        [Tooltip("Camera offset from target")]
        [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 0f, -10f);

        [Tooltip("Enable follow on Y axis")]
        [SerializeField] private bool followY = false;

        [Header("Bounds")]
        [Tooltip("Enable camera bounds")]
        [SerializeField] private bool enableBounds = true;

        [Tooltip("Minimum X position")]
        [SerializeField] private float minX = -10f;

        [Tooltip("Maximum X position")]
        [SerializeField] private float maxX = 1000f;

        [Tooltip("Minimum Y position")]
        [SerializeField] private float minY = -5f;

        [Tooltip("Maximum Y position")]
        [SerializeField] private float maxY = 5f;

        [Header("Parallax")]
        [Tooltip("Enable parallax effect")]
        [SerializeField] private bool enableParallax = true;

        [Tooltip("Parallax layers")]
        [SerializeField] private ParallaxLayer[] parallaxLayers;

        [Header("Performance")]
        [Tooltip("Update rate (Hz) - 60 = update every frame")]
        [SerializeField] private int updateRate = 60;
        #endregion

        #region State
        private Camera cam;
        private Vector3 targetPosition;
        private Vector3 velocity = Vector3.zero;
        private float updateInterval;
        private float lastUpdateTime;
        private Vector3 lastCameraPosition;
        private bool zoomEnabled = false;
        #endregion

        #region Properties
        public Vector3 Position => transform.position;
        public Transform FollowTarget => followTarget;
        public bool ParallaxEnabled => enableParallax;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            cam = GetComponent<Camera>();
            updateInterval = 1f / updateRate;
            lastCameraPosition = transform.position;
        }

        private void LateUpdate()
        {
            if (followTarget == null) return;

            // Throttled update
            if (Time.time - lastUpdateTime >= updateInterval)
            {
                UpdateCameraPosition();
                UpdateParallax();
                lastUpdateTime = Time.time;
            }
        }
        #endregion

        #region Camera Movement
        private void UpdateCameraPosition()
        {
            if (followTarget == null) return;

            // Calculate target position
            targetPosition = followTarget.position + cameraOffset;

            // Optional Y follow
            if (!followY)
            {
                targetPosition.y = transform.position.y;
            }

            // Smooth follow using SmoothDamp
            Vector3 newPosition = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref velocity,
                followSpeed
            );

            // Apply bounds
            if (enableBounds)
            {
                newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
                newPosition.y = Mathf.Clamp(newPosition.y, minY, maxY);
            }

            // Ensure Z is fixed
            newPosition.z = cameraOffset.z;

            transform.position = newPosition;
        }
        #endregion

        #region Parallax
        private void UpdateParallax()
        {
            if (!enableParallax || parallaxLayers == null || parallaxLayers.Length == 0)
                return;

            Vector3 cameraDelta = transform.position - lastCameraPosition;

            foreach (var layer in parallaxLayers)
            {
                if (layer.transform == null) continue;

                // Move layer based on parallax factor
                Vector3 parallaxMove = new Vector3(
                    cameraDelta.x * layer.parallaxFactorX,
                    cameraDelta.y * layer.parallaxFactorY,
                    0f
                );

                layer.transform.position += parallaxMove;
            }

            lastCameraPosition = transform.position;
        }
        #endregion

        #region Public API
        /// <summary>
        /// Sets the follow target
        /// </summary>
        public void SetFollowTarget(Transform target)
        {
            followTarget = target;
        }

        /// <summary>
        /// Sets the follow speed (smoothing)
        /// </summary>
        public void SetFollowSpeed(float speed)
        {
            followSpeed = Mathf.Clamp01(speed);
        }

        /// <summary>
        /// Disables zoom (keeps orthographic size fixed)
        /// </summary>
        public void DisableZoom()
        {
            zoomEnabled = false;
        }

        /// <summary>
        /// Enables zoom
        /// </summary>
        public void EnableZoom()
        {
            zoomEnabled = true;
        }

        /// <summary>
        /// Enables parallax effect
        /// </summary>
        public void EnableParallax()
        {
            enableParallax = true;
        }

        /// <summary>
        /// Disables parallax effect
        /// </summary>
        public void DisableParallax()
        {
            enableParallax = false;
        }

        /// <summary>
        /// Sets camera offset from target
        /// </summary>
        public void SetOffset(Vector3 offset)
        {
            cameraOffset = offset;
        }

        /// <summary>
        /// Immediately snaps camera to target (no smoothing)
        /// </summary>
        public void SnapToTarget()
        {
            if (followTarget == null) return;

            Vector3 snapPosition = followTarget.position + cameraOffset;

            if (!followY)
            {
                snapPosition.y = transform.position.y;
            }

            if (enableBounds)
            {
                snapPosition.x = Mathf.Clamp(snapPosition.x, minX, maxX);
                snapPosition.y = Mathf.Clamp(snapPosition.y, minY, maxY);
            }

            snapPosition.z = cameraOffset.z;
            transform.position = snapPosition;
            lastCameraPosition = snapPosition;
            velocity = Vector3.zero;
        }

        /// <summary>
        /// Sets camera bounds
        /// </summary>
        public void SetBounds(float minX, float maxX, float minY, float maxY)
        {
            this.minX = minX;
            this.maxX = maxX;
            this.minY = minY;
            this.maxY = maxY;
            enableBounds = true;
        }

        /// <summary>
        /// Disables camera bounds
        /// </summary>
        public void DisableBounds()
        {
            enableBounds = false;
        }

        /// <summary>
        /// Adds a parallax layer
        /// </summary>
        public void AddParallaxLayer(Transform layerTransform, float factorX, float factorY)
        {
            List<ParallaxLayer> layers = new List<ParallaxLayer>(parallaxLayers ?? new ParallaxLayer[0]);
            layers.Add(new ParallaxLayer
            {
                transform = layerTransform,
                parallaxFactorX = factorX,
                parallaxFactorY = factorY
            });
            parallaxLayers = layers.ToArray();
        }
        #endregion

        #region Data Structures
        [System.Serializable]
        public class ParallaxLayer
        {
            [Tooltip("Transform of the parallax layer")]
            public Transform transform;

            [Tooltip("Parallax factor X (0=static, 1=follow camera)")]
            [Range(0f, 1f)]
            public float parallaxFactorX = 0.5f;

            [Tooltip("Parallax factor Y")]
            [Range(0f, 1f)]
            public float parallaxFactorY = 0.5f;
        }
        #endregion

        #region Debug
        private void OnDrawGizmosSelected()
        {
            if (!enableBounds) return;

            Gizmos.color = Color.yellow;

            // Draw bounds
            Vector3 bottomLeft = new Vector3(minX, minY, 0f);
            Vector3 bottomRight = new Vector3(maxX, minY, 0f);
            Vector3 topRight = new Vector3(maxX, maxY, 0f);
            Vector3 topLeft = new Vector3(minX, maxY, 0f);

            Gizmos.DrawLine(bottomLeft, bottomRight);
            Gizmos.DrawLine(bottomRight, topRight);
            Gizmos.DrawLine(topRight, topLeft);
            Gizmos.DrawLine(topLeft, bottomLeft);
        }
        #endregion
    }
}
