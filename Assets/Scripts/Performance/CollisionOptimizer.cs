// ═══════════════════════════════════════════════════════════════════════════════
// INFINITY HOUSE v5.5 - Made by Mate Makovics
// Collision Optimizer - Raycast Culling & Physics Optimization
// ═══════════════════════════════════════════════════════════════════════════════

using UnityEngine;

namespace InfinityHouse.Performance
{
    /// <summary>
    /// Raycast culling and physics optimization.
    /// Target: -40% collision checks, 0.02s physics step, solver iterations 6.
    /// </summary>
    public class CollisionOptimizer : MonoBehaviour
    {
        public static CollisionOptimizer Instance { get; private set; }

        [Header("Physics Configuration")]
        [SerializeField] private float fixedTimestep = 0.02f;
        [SerializeField] private int solverIterations = 6;
        [SerializeField] private int velocityIterations = 1;

        [Header("Culling")]
        [SerializeField] private float cullingDistance = 30f;
        [SerializeField] private LayerMask collisionLayers;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            OptimizePhysics();
        }

        private void OptimizePhysics()
        {
            Time.fixedDeltaTime = fixedTimestep;
            Physics2D.defaultSolverIterations = solverIterations;
            Physics2D.defaultSolverVelocityIterations = velocityIterations;

            // Auto sleeping
            Physics2D.autoSyncTransforms = false;
            Physics2D.reuseCollisionCallbacks = true;

            Debug.Log("[CollisionOptimizer] Physics optimized");
        }

        public bool ShouldCheckCollision(Vector3 objectPos, Vector3 playerPos)
        {
            return Vector3.Distance(objectPos, playerPos) < cullingDistance;
        }
    }
}
