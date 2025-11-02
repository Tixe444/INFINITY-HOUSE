using UnityEngine;
using System;

namespace InfiniteHaus.Collectibles
{
    /// <summary>
    /// Manages all collectibles and rewards during a run.
    /// Tracks Soul Shards, Relics, Chase Crystals, and permanent upgrades.
    /// Integrates with Save System for persistence.
    /// </summary>
    public class RewardManager : MonoBehaviour
    {
        [Header("Current Run Stats")]
        [Tooltip("Soul Shards collected this run")]
        [SerializeField] private int currentRunSoulShards = 0;

        [Tooltip("Relics collected this run")]
        [SerializeField] private int currentRunRelics = 0;

        [Tooltip("Chase Crystals collected this run")]
        [SerializeField] private int currentRunChaseCrystals = 0;

        [Tooltip("Total score this run")]
        [SerializeField] private int currentRunScore = 0;

        [Header("Permanent Upgrades")]
        [Tooltip("Total relics collected across all runs")]
        [SerializeField] private int totalRelicsCollected = 0;

        [Tooltip("Number of permanent HP upgrades earned")]
        [SerializeField] private int permanentHPUpgrades = 0;

        [Header("Relic System")]
        [Tooltip("Relics needed for one HP upgrade")]
        [SerializeField] private int relicsPerHPUpgrade = 3;

        [Header("Scoring")]
        [Tooltip("Points per Soul Shard")]
        [SerializeField] private int soulShardPointValue = 10;

        [Tooltip("Score multiplier for distance traveled")]
        [SerializeField] private float distanceScoreMultiplier = 1f;

        // Events
        public event Action<int> OnSoulShardCollected; // (total count)
        public event Action<int, int> OnRelicCollected; // (current count, required count)
        public event Action OnChaseCrystalCollected;
        public event Action<int> OnScoreChanged; // (new score)
        public event Action<int> OnHPUpgradeEarned; // (new max HP)
        public event Action<int, int, int> OnRunCompleted; // (shards, relics, crystals)

        // Properties
        public int CurrentRunSoulShards => currentRunSoulShards;
        public int CurrentRunRelics => currentRunRelics;
        public int CurrentRunChaseCrystals => currentRunChaseCrystals;
        public int CurrentRunScore => currentRunScore;
        public int TotalRelicsCollected => totalRelicsCollected;
        public int PermanentHPUpgrades => permanentHPUpgrades;
        public int RelicsNeededForUpgrade => relicsPerHPUpgrade - (currentRunRelics % relicsPerHPUpgrade);

        private void Start()
        {
            LoadPermanentProgress();
        }

        /// <summary>
        /// Called when player collects a Soul Shard
        /// </summary>
        public void CollectSoulShard(int pointValue)
        {
            currentRunSoulShards++;
            AddScore(pointValue);

            OnSoulShardCollected?.Invoke(currentRunSoulShards);

            Debug.Log($"Soul Shards: {currentRunSoulShards} | Score: {currentRunScore}");
        }

        /// <summary>
        /// Called when player collects a Relic
        /// </summary>
        public void CollectRelic()
        {
            currentRunRelics++;
            totalRelicsCollected++;

            OnRelicCollected?.Invoke(currentRunRelics, relicsPerHPUpgrade);

            // Check if player earned HP upgrade
            if (currentRunRelics >= relicsPerHPUpgrade && currentRunRelics % relicsPerHPUpgrade == 0)
            {
                GrantHPUpgrade();
            }

            Debug.Log($"Relics: {currentRunRelics}/{relicsPerHPUpgrade} | Total: {totalRelicsCollected}");
        }

        /// <summary>
        /// Called when player collects a Chase Crystal
        /// </summary>
        public void CollectChaseCrystal()
        {
            currentRunChaseCrystals++;
            OnChaseCrystalCollected?.Invoke();

            Debug.Log($"Chase Crystals: {currentRunChaseCrystals}");
        }

        /// <summary>
        /// Adds score to current run
        /// </summary>
        public void AddScore(int points)
        {
            currentRunScore += points;
            OnScoreChanged?.Invoke(currentRunScore);
        }

        /// <summary>
        /// Adds distance-based score
        /// </summary>
        public void AddDistanceScore(float distance)
        {
            int distancePoints = Mathf.FloorToInt(distance * distanceScoreMultiplier);
            AddScore(distancePoints);
        }

        /// <summary>
        /// Grants permanent HP upgrade
        /// </summary>
        private void GrantHPUpgrade()
        {
            permanentHPUpgrades++;

            // Apply to player health
            var playerHealth = FindObjectOfType<Player.PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.IncreaseMaxHealth(1);
            }

            OnHPUpgradeEarned?.Invoke(permanentHPUpgrades);

            Debug.Log($"HP UPGRADE EARNED! Total upgrades: {permanentHPUpgrades}");

            SavePermanentProgress();
        }

        /// <summary>
        /// Calculates final score for end of run
        /// </summary>
        public int CalculateFinalScore(float distanceTraveled)
        {
            int finalScore = currentRunScore;

            // Add distance bonus
            finalScore += Mathf.FloorToInt(distanceTraveled * distanceScoreMultiplier);

            // Add collectible bonuses
            finalScore += currentRunRelics * 100; // Relics worth 100 points each
            finalScore += currentRunChaseCrystals * 50; // Crystals worth 50 points each

            return finalScore;
        }

        /// <summary>
        /// Completes the run and triggers end screen
        /// </summary>
        public void CompleteRun(float distanceTraveled)
        {
            int finalScore = CalculateFinalScore(distanceTraveled);
            currentRunScore = finalScore;

            OnRunCompleted?.Invoke(currentRunSoulShards, currentRunRelics, currentRunChaseCrystals);

            SavePermanentProgress();

            Debug.Log($"RUN COMPLETED | Score: {finalScore} | Shards: {currentRunSoulShards} | Relics: {currentRunRelics}");
        }

        /// <summary>
        /// Resets current run stats for new game
        /// </summary>
        public void StartNewRun()
        {
            currentRunSoulShards = 0;
            currentRunRelics = 0;
            currentRunChaseCrystals = 0;
            currentRunScore = 0;

            OnScoreChanged?.Invoke(0);
            OnSoulShardCollected?.Invoke(0);

            Debug.Log("New run started - stats reset");
        }

        /// <summary>
        /// Loads permanent progress from save system
        /// </summary>
        private void LoadPermanentProgress()
        {
            totalRelicsCollected = PlayerPrefs.GetInt("TotalRelics", 0);
            permanentHPUpgrades = PlayerPrefs.GetInt("HPUpgrades", 0);

            Debug.Log($"Loaded progress - Total Relics: {totalRelicsCollected}, HP Upgrades: {permanentHPUpgrades}");
        }

        /// <summary>
        /// Saves permanent progress to save system
        /// </summary>
        private void SavePermanentProgress()
        {
            PlayerPrefs.SetInt("TotalRelics", totalRelicsCollected);
            PlayerPrefs.SetInt("HPUpgrades", permanentHPUpgrades);
            PlayerPrefs.Save();

            Debug.Log("Progress saved");
        }

        /// <summary>
        /// Gets summary of current run for UI
        /// </summary>
        public RunSummary GetRunSummary()
        {
            return new RunSummary
            {
                soulShards = currentRunSoulShards,
                relics = currentRunRelics,
                chaseCrystals = currentRunChaseCrystals,
                score = currentRunScore,
                totalRelics = totalRelicsCollected,
                hpUpgrades = permanentHPUpgrades
            };
        }

        [System.Serializable]
        public struct RunSummary
        {
            public int soulShards;
            public int relics;
            public int chaseCrystals;
            public int score;
            public int totalRelics;
            public int hpUpgrades;
        }
    }
}
