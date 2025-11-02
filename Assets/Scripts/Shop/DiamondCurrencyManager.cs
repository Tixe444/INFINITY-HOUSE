using UnityEngine;
using System;

namespace InfiniteHaus.Shop
{
    /// <summary>
    /// Manages Diamond premium currency (earn, spend, display).
    /// Integrates with save system and IAP (placeholder for future implementation).
    /// </summary>
    public class DiamondCurrencyManager : MonoBehaviour
    {
        [Header("Currency Settings")]
        [Tooltip("Current diamond balance")]
        [SerializeField] private int currentDiamonds = 0;

        [Tooltip("Starting diamonds for new players")]
        [SerializeField] private int startingDiamonds = 50;

        [Header("Debug/Testing")]
        [Tooltip("Enable debug currency commands")]
        [SerializeField] private bool enableDebugCommands = true;

        [Tooltip("Amount to add with debug key (D)")]
        [SerializeField] private int debugAddAmount = 100;

        // Events
        public event Action<int> OnDiamondsChanged; // (new balance)
        public event Action<int> OnDiamondsEarned; // (amount earned)
        public event Action<int> OnDiamondsSpent; // (amount spent)
        public event Action OnInsufficientFunds;

        // Singleton instance
        public static DiamondCurrencyManager Instance { get; private set; }

        // Properties
        public int CurrentDiamonds => currentDiamonds;

        private void Awake()
        {
            // Singleton pattern
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

        private void Start()
        {
            LoadDiamonds();
        }

        private void Update()
        {
            // Debug commands for testing
            if (enableDebugCommands && Debug.isDebugBuild)
            {
                if (Input.GetKeyDown(KeyCode.D))
                {
                    AddDiamonds(debugAddAmount);
                    Debug.Log($"[DEBUG] Added {debugAddAmount} diamonds");
                }

                if (Input.GetKeyDown(KeyCode.R))
                {
                    ResetDiamonds();
                    Debug.Log("[DEBUG] Reset diamonds to starting amount");
                }
            }
        }

        /// <summary>
        /// Loads diamond balance from save system
        /// </summary>
        public void LoadDiamonds()
        {
            var saveData = Save.SaveSystem.LoadGame();

            if (saveData != null)
            {
                currentDiamonds = saveData.diamonds;
            }
            else
            {
                // First time player - give starting diamonds
                currentDiamonds = startingDiamonds;
                SaveDiamonds();
            }

            OnDiamondsChanged?.Invoke(currentDiamonds);
            Debug.Log($"Loaded {currentDiamonds} diamonds");
        }

        /// <summary>
        /// Saves diamond balance to save system
        /// </summary>
        public void SaveDiamonds()
        {
            var saveData = Save.SaveSystem.LoadGame();

            if (saveData != null)
            {
                saveData.diamonds = currentDiamonds;
                Save.SaveSystem.SaveGame(saveData);
                Save.SaveSystem.QuickSave(saveData); // Backup save
            }
        }

        /// <summary>
        /// Adds diamonds to player's balance
        /// </summary>
        public void AddDiamonds(int amount)
        {
            if (amount <= 0) return;

            currentDiamonds += amount;
            OnDiamondsEarned?.Invoke(amount);
            OnDiamondsChanged?.Invoke(currentDiamonds);

            SaveDiamonds();

            Debug.Log($"Earned {amount} diamonds. New balance: {currentDiamonds}");
        }

        /// <summary>
        /// Attempts to spend diamonds. Returns true if successful.
        /// </summary>
        public bool TrySpendDiamonds(int amount)
        {
            if (amount <= 0)
            {
                Debug.LogWarning("Cannot spend 0 or negative diamonds");
                return false;
            }

            if (currentDiamonds < amount)
            {
                OnInsufficientFunds?.Invoke();
                Debug.Log($"Insufficient diamonds. Need {amount}, have {currentDiamonds}");
                return false;
            }

            currentDiamonds -= amount;
            OnDiamondsSpent?.Invoke(amount);
            OnDiamondsChanged?.Invoke(currentDiamonds);

            SaveDiamonds();

            Debug.Log($"Spent {amount} diamonds. New balance: {currentDiamonds}");
            return true;
        }

        /// <summary>
        /// Checks if player has enough diamonds
        /// </summary>
        public bool HasEnoughDiamonds(int amount)
        {
            return currentDiamonds >= amount;
        }

        /// <summary>
        /// Resets diamonds to starting amount (debug/testing)
        /// </summary>
        public void ResetDiamonds()
        {
            currentDiamonds = startingDiamonds;
            OnDiamondsChanged?.Invoke(currentDiamonds);
            SaveDiamonds();
        }

        /// <summary>
        /// Sets diamond amount directly (admin/debug)
        /// </summary>
        public void SetDiamonds(int amount)
        {
            currentDiamonds = Mathf.Max(0, amount);
            OnDiamondsChanged?.Invoke(currentDiamonds);
            SaveDiamonds();
        }

        #region IAP Integration (Placeholder)

        /// <summary>
        /// Placeholder for In-App Purchase integration.
        /// Connect to Unity IAP or your preferred IAP solution.
        /// </summary>
        public void PurchaseDiamondPack(DiamondPack pack)
        {
            // TODO: Integrate with Unity IAP
            // For now, just add diamonds (testing)
            Debug.Log($"[IAP] Purchasing {pack.displayName} for ${pack.realMoneyPrice}");

            // After successful IAP transaction:
            AddDiamonds(pack.diamondAmount);

            // Optional: Add bonus diamonds
            if (pack.bonusDiamonds > 0)
            {
                AddDiamonds(pack.bonusDiamonds);
            }
        }

        /// <summary>
        /// Defines a diamond pack for IAP
        /// </summary>
        [System.Serializable]
        public class DiamondPack
        {
            public string packID;
            public string displayName;
            public int diamondAmount;
            public int bonusDiamonds;
            public float realMoneyPrice; // USD
            public Sprite iconSprite;
        }

        #endregion

        #region Reward Integration

        /// <summary>
        /// Reward diamonds for game milestones
        /// </summary>
        public void RewardForMilestone(MilestoneType milestone)
        {
            int reward = GetMilestoneReward(milestone);
            if (reward > 0)
            {
                AddDiamonds(reward);
                Debug.Log($"Milestone '{milestone}' completed! Earned {reward} diamonds");
            }
        }

        /// <summary>
        /// Gets diamond reward for milestone type
        /// </summary>
        private int GetMilestoneReward(MilestoneType milestone)
        {
            switch (milestone)
            {
                case MilestoneType.FirstRun:
                    return 10;
                case MilestoneType.Reach100m:
                    return 25;
                case MilestoneType.Reach500m:
                    return 50;
                case MilestoneType.Reach1000m:
                    return 100;
                case MilestoneType.Collect100Shards:
                    return 20;
                case MilestoneType.Collect500Shards:
                    return 75;
                case MilestoneType.FirstRelic:
                    return 30;
                case MilestoneType.MaxHPUpgrade:
                    return 50;
                case MilestoneType.Survive5Minutes:
                    return 40;
                case MilestoneType.DailyLogin:
                    return 5;
                default:
                    return 0;
            }
        }

        public enum MilestoneType
        {
            FirstRun,
            Reach100m,
            Reach500m,
            Reach1000m,
            Collect100Shards,
            Collect500Shards,
            FirstRelic,
            MaxHPUpgrade,
            Survive5Minutes,
            DailyLogin
        }

        #endregion

        private void OnApplicationQuit()
        {
            SaveDiamonds();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SaveDiamonds();
            }
        }
    }
}
