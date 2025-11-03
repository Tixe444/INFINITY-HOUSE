// ═══════════════════════════════════════════════════════════════════════════════
// INFINITY HOUSE - Made by Mate Makovics
// Shard Currency Manager - Utility Currency System
// ═══════════════════════════════════════════════════════════════════════════════

using UnityEngine;
using System;
using InfinityHouse.Save;

namespace InfinityHouse.Economy
{
    /// <summary>
    /// Manages Shards - the utility currency earned through gameplay.
    /// Earned from: duplicate conversions, fusion, runs, events.
    /// Used for: Crystal chests, catalysts, boosts.
    /// Mobile-only currency (no trading).
    /// </summary>
    public class ShardCurrencyManager : MonoBehaviour
    {
        // Singleton
        public static ShardCurrencyManager Instance { get; private set; }

        // Current balance
        private int currentShards = 0;
        private int totalShardsEarned = 0;
        private int totalShardsSpent = 0;

        // Events
        public event Action<int> OnShardsChanged; // new amount
        public event Action<int, string> OnShardsGained; // amount, source
        public event Action<int, string> OnShardsSpent; // amount, item

        // Properties
        public int CurrentShards => currentShards;
        public int TotalEarned => totalShardsEarned;
        public int TotalSpent => totalShardsSpent;

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

            LoadShards();
        }

        #region Core Operations

        /// <summary>
        /// Adds shards to the player's balance
        /// </summary>
        public void AddShards(int amount, string source = "")
        {
            if (amount <= 0) return;

            currentShards += amount;
            totalShardsEarned += amount;

            SaveShards();

            OnShardsGained?.Invoke(amount, source);
            OnShardsChanged?.Invoke(currentShards);

            // Track telemetry
            Analytics.TelemetryEvents.Instance?.TrackCurrencyEarned("shards", amount, source);

            Debug.Log($"[ShardCurrency] +{amount} shards from {source}. Total: {currentShards}");
        }

        /// <summary>
        /// Attempts to spend shards
        /// </summary>
        public bool TrySpendShards(int amount, string item = "")
        {
            if (amount <= 0)
            {
                Debug.LogWarning("[ShardCurrency] Invalid amount");
                return false;
            }

            if (currentShards < amount)
            {
                Debug.LogWarning($"[ShardCurrency] Insufficient shards: {currentShards} < {amount}");
                return false;
            }

            currentShards -= amount;
            totalShardsSpent += amount;

            SaveShards();

            OnShardsSpent?.Invoke(amount, item);
            OnShardsChanged?.Invoke(currentShards);

            // Track telemetry
            Analytics.TelemetryEvents.Instance?.TrackCurrencySpent("shards", amount, item);

            Debug.Log($"[ShardCurrency] -{amount} shards for {item}. Remaining: {currentShards}");
            return true;
        }

        /// <summary>
        /// Checks if player can afford an amount
        /// </summary>
        public bool CanAfford(int amount)
        {
            return currentShards >= amount;
        }

        #endregion

        #region Conversion & Rewards

        /// <summary>
        /// Converts duplicate item to shards based on rarity
        /// </summary>
        public void ConvertDuplicateToShards(RarityManager.RarityTier rarity)
        {
            int shardValue = RarityManager.Instance.GetShardValue(rarity);

            if (shardValue > 0)
            {
                AddShards(shardValue, $"Duplicate {rarity}");

                Debug.Log($"[ShardCurrency] Converted duplicate {rarity} to {shardValue} shards");
            }
        }

        /// <summary>
        /// Awards shards for run completion
        /// </summary>
        public void AwardRunShards(int baseShards, bool isPerfectRun = false, bool isWeekendBonus = false)
        {
            int finalShards = baseShards;

            // Perfect run bonus (200%)
            if (isPerfectRun)
            {
                finalShards = Mathf.RoundToInt(baseShards * 3f);
            }

            // Weekend bonus (+50%)
            if (isWeekendBonus)
            {
                finalShards = Mathf.RoundToInt(finalShards * 1.5f);
            }

            string source = "Run";
            if (isPerfectRun) source += " (Perfect)";
            if (isWeekendBonus) source += " (Weekend)";

            AddShards(finalShards, source);
        }

        #endregion

        #region Persistence

        private void LoadShards()
        {
            SaveData saveData = SaveSystem.LoadGame();

            if (saveData != null)
            {
                // TODO: Add shard fields to SaveData
                currentShards = PlayerPrefs.GetInt("CurrentShards", 100); // Start with 100
                totalShardsEarned = PlayerPrefs.GetInt("TotalShardsEarned", 0);
                totalShardsSpent = PlayerPrefs.GetInt("TotalShardsSpent", 0);
            }
            else
            {
                currentShards = 100; // Starting amount
                totalShardsEarned = 0;
                totalShardsSpent = 0;
            }

            Debug.Log($"[ShardCurrency] Loaded: {currentShards} shards");
        }

        private void SaveShards()
        {
            PlayerPrefs.SetInt("CurrentShards", currentShards);
            PlayerPrefs.SetInt("TotalShardsEarned", totalShardsEarned);
            PlayerPrefs.SetInt("TotalShardsSpent", totalShardsSpent);
            PlayerPrefs.Save();
        }

        #endregion

        #region Debug

        /// <summary>
        /// Grants shards for testing (debug only)
        /// </summary>
        [ContextMenu("Grant 1000 Shards (Debug)")]
        public void DebugGrantShards()
        {
            AddShards(1000, "Debug");
        }

        #endregion
    }
}
