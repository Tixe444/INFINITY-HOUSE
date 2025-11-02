using UnityEngine;
using System;

namespace InfiniteHaus.Monsters
{
    /// <summary>
    /// Manages the Chase Meter and coordinates monster behavior.
    /// Chase Meter fills from player mistakes and over time.
    /// At 100%, player is caught and game ends.
    /// </summary>
    public class ChaseSystem : MonoBehaviour
    {
        [Header("Chase Meter Settings")]
        [Tooltip("Current chase meter value (0-100)")]
        [SerializeField][Range(0f, 100f)] private float chaseMeter = 0f;

        [Tooltip("Base fill rate per second")]
        [SerializeField] private float baseFillRate = 1f;

        [Tooltip("Chase meter penalty when player takes damage")]
        [SerializeField] private float damageChaseIncrease = 15f;

        [Tooltip("Chase meter penalty when player misses a jump")]
        [SerializeField] private float missedJumpPenalty = 5f;

        [Header("Chase Reduction")]
        [Tooltip("Amount reduced when entering lantern zone")]
        [SerializeField] private float lanternZoneReduction = 20f;

        [Tooltip("Amount reduced when collecting Chase Crystal")]
        [SerializeField] private float chaseCrystalReduction = 30f;

        [Header("Monster References")]
        [Tooltip("The Shadow monster (constant pursuer)")]
        [SerializeField] private Shadow shadowMonster;

        [Tooltip("The Crawler monster (ambush)")]
        [SerializeField] private Crawler crawlerMonster;

        [Tooltip("The Mimic monster (trap disguise)")]
        [SerializeField] private Mimic mimicMonster;

        [Header("Game State")]
        [Tooltip("Is chase system active?")]
        [SerializeField] private bool isActive = false;

        [Tooltip("Is chase meter frozen?")]
        [SerializeField] private bool isFrozen = false;

        // Events
        public event Action<float> OnChaseMeterChanged; // (current value 0-100)
        public event Action OnPlayerCaught; // Triggered at 100%
        public event Action<float> OnChaseMeterReduced; // (amount reduced)
        public event Action OnChaseFrozen; // Chase Crystal effect
        public event Action OnChaseUnfrozen;

        // Properties
        public float ChaseMeter => chaseMeter;
        public float ChasePercentage => chaseMeter / 100f;
        public bool IsActive => isActive;
        public bool IsFrozen => isFrozen;

        private void Update()
        {
            if (!isActive || isFrozen) return;

            // Gradually increase chase meter
            IncreaseChaseMeter(baseFillRate * Time.deltaTime);

            // Check for catch condition
            if (chaseMeter >= 100f)
            {
                CatchPlayer();
            }
        }

        /// <summary>
        /// Increases chase meter by specified amount
        /// </summary>
        public void IncreaseChaseMeter(float amount)
        {
            if (isFrozen) return;

            chaseMeter = Mathf.Clamp(chaseMeter + amount, 0f, 100f);
            OnChaseMeterChanged?.Invoke(chaseMeter);

            // Update monster aggression based on chase level
            UpdateMonsterAggression();
        }

        /// <summary>
        /// Reduces chase meter by specified amount
        /// </summary>
        public void ReduceChaseMeter(float amount)
        {
            chaseMeter = Mathf.Clamp(chaseMeter - amount, 0f, 100f);
            OnChaseMeterChanged?.Invoke(chaseMeter);
            OnChaseMeterReduced?.Invoke(amount);

            UpdateMonsterAggression();
        }

        /// <summary>
        /// Called when player takes damage
        /// </summary>
        public void OnPlayerDamaged()
        {
            IncreaseChaseMeter(damageChaseIncrease);
        }

        /// <summary>
        /// Called when player misses a jump (falls into gap)
        /// </summary>
        public void OnPlayerMissedJump()
        {
            IncreaseChaseMeter(missedJumpPenalty);
        }

        /// <summary>
        /// Called when player enters a lantern zone
        /// </summary>
        public void OnLanternZoneEntered()
        {
            ReduceChaseMeter(lanternZoneReduction);
        }

        /// <summary>
        /// Called when player collects a Chase Crystal
        /// </summary>
        public void OnChaseCrystalCollected(float freezeDuration)
        {
            ReduceChaseMeter(chaseCrystalReduction);
            StartCoroutine(FreezeChaseMeterCoroutine(freezeDuration));
        }

        /// <summary>
        /// Freezes chase meter for specified duration
        /// </summary>
        private System.Collections.IEnumerator FreezeChaseMeterCoroutine(float duration)
        {
            isFrozen = true;
            OnChaseFrozen?.Invoke();

            yield return new WaitForSeconds(duration);

            isFrozen = false;
            OnChaseUnfrozen?.Invoke();
        }

        /// <summary>
        /// Updates monster aggression based on chase meter level
        /// </summary>
        private void UpdateMonsterAggression()
        {
            float intensity = ChasePercentage;

            if (shadowMonster != null)
            {
                shadowMonster.SetAggression(intensity);
            }

            if (crawlerMonster != null)
            {
                crawlerMonster.SetAggression(intensity);
            }

            if (mimicMonster != null)
            {
                mimicMonster.SetAggression(intensity);
            }
        }

        /// <summary>
        /// Triggers player caught condition
        /// </summary>
        private void CatchPlayer()
        {
            if (!isActive) return;

            isActive = false;
            chaseMeter = 100f;
            OnPlayerCaught?.Invoke();

            Debug.Log("PLAYER CAUGHT! Chase Meter reached 100%");
        }

        /// <summary>
        /// Starts the chase system
        /// </summary>
        public void StartChase()
        {
            isActive = true;
            chaseMeter = 0f;
            isFrozen = false;
            OnChaseMeterChanged?.Invoke(chaseMeter);
        }

        /// <summary>
        /// Stops the chase system
        /// </summary>
        public void StopChase()
        {
            isActive = false;
        }

        /// <summary>
        /// Resets chase system for new game
        /// </summary>
        public void ResetChase()
        {
            chaseMeter = 0f;
            isActive = false;
            isFrozen = false;
            StopAllCoroutines();
            OnChaseMeterChanged?.Invoke(chaseMeter);
        }

        /// <summary>
        /// Sets the base fill rate (for difficulty scaling)
        /// </summary>
        public void SetFillRate(float rate)
        {
            baseFillRate = rate;
        }

        /// <summary>
        /// Sets damage chase penalty (for difficulty scaling)
        /// </summary>
        public void SetDamagePenalty(float penalty)
        {
            damageChaseIncrease = penalty;
        }
    }
}
