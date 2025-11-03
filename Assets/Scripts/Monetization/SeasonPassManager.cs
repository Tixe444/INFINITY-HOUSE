// ═══════════════════════════════════════════════════════════════════════════════
// INFINITY HOUSE - Made by Mate Makovics
// Season Pass Manager - Free & Premium Progression
// ═══════════════════════════════════════════════════════════════════════════════

using UnityEngine;
using System;
using System.Collections.Generic;
using InfinityHouse.Save;
using InfinityHouse.Shop;

namespace InfinityHouse.Monetization
{
    /// <summary>
    /// Season Pass system with free and premium reward tracks.
    /// Includes quests, milestones, and time-limited progression.
    /// Compliant with battle pass best practices.
    /// </summary>
    public class SeasonPassManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private TextAsset seasonConfigJson;
        [SerializeField] private int maxTier = 50;
        [SerializeField] private int xpPerTier = 1000;

        [Header("Quest Generation")]
        [SerializeField] private int dailyQuestCount = 3;
        [SerializeField] private int weeklyQuestCount = 3;

        // Singleton
        public static SeasonPassManager Instance { get; private set; }

        // Current season data
        private SeasonConfig currentSeason;
        private SeasonProgress playerProgress;
        private bool hasPremiumPass = false;

        // Quests
        private List<Quest> activeQuests;
        private DateTime lastQuestRefresh;

        // Events
        public event Action<int> OnXpGained; // xp amount
        public event Action<int> OnTierUnlocked; // tier number
        public event Action<SeasonReward> OnRewardClaimed;
        public event Action OnPremiumPassUnlocked;
        public event Action<Quest> OnQuestCompleted;
        public event Action<Quest> OnQuestProgressed;

        // Properties
        public int CurrentTier => playerProgress != null ? playerProgress.currentTier : 0;
        public int CurrentXp => playerProgress != null ? playerProgress.currentXp : 0;
        public int XpToNextTier => xpPerTier - (CurrentXp % xpPerTier);
        public bool HasPremiumPass => hasPremiumPass;
        public float SeasonProgress => (float)CurrentTier / maxTier;
        public TimeSpan TimeRemaining => currentSeason != null ? currentSeason.endDate - DateTime.UtcNow : TimeSpan.Zero;

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

            activeQuests = new List<Quest>();
        }

        private void Start()
        {
            LoadSeasonConfig();
            LoadPlayerProgress();
            RefreshQuests();
        }

        #region Configuration

        private void LoadSeasonConfig()
        {
            if (seasonConfigJson != null)
            {
                try
                {
                    currentSeason = JsonUtility.FromJson<SeasonConfig>(seasonConfigJson.text);
                    Debug.Log($"[SeasonPassManager] Loaded season: {currentSeason.seasonId}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SeasonPassManager] Failed to load season config: {ex.Message}");
                    CreateDefaultSeason();
                }
            }
            else
            {
                CreateDefaultSeason();
            }
        }

        private void CreateDefaultSeason()
        {
            currentSeason = new SeasonConfig
            {
                seasonId = "s1",
                seasonName = "Season 1: Haunted Halls",
                startDate = DateTime.UtcNow,
                endDate = DateTime.UtcNow.AddDays(90),
                maxTier = maxTier,
                xpPerTier = xpPerTier
            };

            Debug.Log("[SeasonPassManager] Created default season config");
        }

        #endregion

        #region Player Progress

        private void LoadPlayerProgress()
        {
            SaveData saveData = SaveSystem.LoadGame();

            // TODO: Add SeasonProgress to SaveData
            // For now, use PlayerPrefs
            string progressJson = PlayerPrefs.GetString($"SeasonProgress_{currentSeason.seasonId}", "");

            if (!string.IsNullOrEmpty(progressJson))
            {
                try
                {
                    playerProgress = JsonUtility.FromJson<SeasonProgress>(progressJson);
                    hasPremiumPass = playerProgress.hasPremiumPass;

                    Debug.Log($"[SeasonPassManager] Loaded progress: Tier {playerProgress.currentTier}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SeasonPassManager] Failed to load progress: {ex.Message}");
                    CreateNewProgress();
                }
            }
            else
            {
                CreateNewProgress();
            }
        }

        private void CreateNewProgress()
        {
            playerProgress = new SeasonProgress
            {
                seasonId = currentSeason.seasonId,
                currentTier = 0,
                currentXp = 0,
                hasPremiumPass = false,
                claimedFreeRewards = new List<int>(),
                claimedPremiumRewards = new List<int>()
            };

            SaveProgress();
            Debug.Log("[SeasonPassManager] Created new season progress");
        }

        private void SaveProgress()
        {
            string json = JsonUtility.ToJson(playerProgress);
            PlayerPrefs.SetString($"SeasonProgress_{currentSeason.seasonId}", json);
            PlayerPrefs.Save();
        }

        #endregion

        #region XP and Progression

        /// <summary>
        /// Awards XP to the player
        /// </summary>
        public void AwardXp(int amount, string source = "")
        {
            if (amount <= 0) return;

            int oldTier = playerProgress.currentTier;
            playerProgress.currentXp += amount;

            // Check for tier ups
            while (playerProgress.currentXp >= xpPerTier && playerProgress.currentTier < maxTier)
            {
                playerProgress.currentXp -= xpPerTier;
                playerProgress.currentTier++;

                Debug.Log($"[SeasonPassManager] Tier up! Now tier {playerProgress.currentTier}");
                OnTierUnlocked?.Invoke(playerProgress.currentTier);
            }

            // Cap at max tier
            if (playerProgress.currentTier >= maxTier)
            {
                playerProgress.currentTier = maxTier;
                playerProgress.currentXp = 0;
            }

            SaveProgress();

            OnXpGained?.Invoke(amount);
            Debug.Log($"[SeasonPassManager] Awarded {amount} XP from {source}. Total: {playerProgress.currentXp}/{xpPerTier}");
        }

        /// <summary>
        /// Claims a reward for a specific tier
        /// </summary>
        public bool ClaimReward(int tier, bool isPremium)
        {
            // Check if tier is unlocked
            if (tier > playerProgress.currentTier)
            {
                Debug.LogWarning($"[SeasonPassManager] Tier {tier} not yet unlocked!");
                return false;
            }

            // Check if premium pass is required
            if (isPremium && !hasPremiumPass)
            {
                Debug.LogWarning("[SeasonPassManager] Premium pass required!");
                return false;
            }

            // Check if already claimed
            List<int> claimedList = isPremium ? playerProgress.claimedPremiumRewards : playerProgress.claimedFreeRewards;
            if (claimedList.Contains(tier))
            {
                Debug.LogWarning($"[SeasonPassManager] Reward already claimed: Tier {tier}");
                return false;
            }

            // Get reward
            SeasonReward reward = GetRewardForTier(tier, isPremium);
            if (reward == null)
            {
                Debug.LogError($"[SeasonPassManager] No reward defined for tier {tier}");
                return false;
            }

            // Grant reward
            GrantReward(reward);

            // Mark as claimed
            claimedList.Add(tier);
            SaveProgress();

            OnRewardClaimed?.Invoke(reward);
            Debug.Log($"[SeasonPassManager] Claimed {(isPremium ? "premium" : "free")} reward: Tier {tier}");

            return true;
        }

        private SeasonReward GetRewardForTier(int tier, bool isPremium)
        {
            // TODO: Load from season config
            // For now, generate procedural rewards
            if (isPremium)
            {
                return new SeasonReward
                {
                    tier = tier,
                    rewardType = SeasonReward.RewardType.Diamonds,
                    amount = 50 * tier,
                    cosmeticId = tier % 10 == 0 ? $"premium_skin_{tier}" : null
                };
            }
            else
            {
                return new SeasonReward
                {
                    tier = tier,
                    rewardType = SeasonReward.RewardType.Diamonds,
                    amount = 25 * tier,
                    cosmeticId = tier % 5 == 0 ? $"free_skin_{tier}" : null
                };
            }
        }

        private void GrantReward(SeasonReward reward)
        {
            switch (reward.rewardType)
            {
                case SeasonReward.RewardType.Diamonds:
                    DiamondCurrencyManager.Instance?.AddDiamonds(reward.amount, "Season Pass");
                    break;

                case SeasonReward.RewardType.Cosmetic:
                    if (!string.IsNullOrEmpty(reward.cosmeticId))
                    {
                        var shopManager = ShopManager.Instance;
                        var cosmetic = shopManager?.GetCosmeticById(reward.cosmeticId);
                        if (cosmetic != null)
                        {
                            shopManager.GrantCosmetic(cosmetic);
                        }
                    }
                    break;

                case SeasonReward.RewardType.Capsule:
                    // TODO: Grant capsule
                    break;
            }
        }

        #endregion

        #region Premium Pass

        /// <summary>
        /// Unlocks the premium pass for the current season
        /// </summary>
        public void UnlockPremiumPass(string seasonId)
        {
            if (seasonId != currentSeason.seasonId)
            {
                Debug.LogWarning($"[SeasonPassManager] Season ID mismatch: {seasonId} vs {currentSeason.seasonId}");
                return;
            }

            if (hasPremiumPass)
            {
                Debug.LogWarning("[SeasonPassManager] Premium pass already unlocked!");
                return;
            }

            hasPremiumPass = true;
            playerProgress.hasPremiumPass = true;
            SaveProgress();

            OnPremiumPassUnlocked?.Invoke();
            Debug.Log("[SeasonPassManager] Premium pass unlocked!");
        }

        /// <summary>
        /// Checks if a specific tier's premium reward has been claimed
        /// </summary>
        public bool IsPremiumRewardClaimed(int tier)
        {
            return playerProgress.claimedPremiumRewards.Contains(tier);
        }

        /// <summary>
        /// Checks if a specific tier's free reward has been claimed
        /// </summary>
        public bool IsFreeRewardClaimed(int tier)
        {
            return playerProgress.claimedFreeRewards.Contains(tier);
        }

        #endregion

        #region Quests

        private void RefreshQuests()
        {
            DateTime now = DateTime.UtcNow;

            // Check if daily refresh needed
            if (ShouldRefreshDaily(now))
            {
                GenerateDailyQuests();
                lastQuestRefresh = now;
            }

            // Check if weekly refresh needed
            if (ShouldRefreshWeekly(now))
            {
                GenerateWeeklyQuests();
            }
        }

        private bool ShouldRefreshDaily(DateTime now)
        {
            return lastQuestRefresh.Date < now.Date;
        }

        private bool ShouldRefreshWeekly(DateTime now)
        {
            // Check if new week (Monday)
            DayOfWeek today = now.DayOfWeek;
            DayOfWeek lastRefresh = lastQuestRefresh.DayOfWeek;

            return today == DayOfWeek.Monday && lastRefresh != DayOfWeek.Monday;
        }

        private void GenerateDailyQuests()
        {
            // Remove old daily quests
            activeQuests.RemoveAll(q => q.questType == Quest.QuestType.Daily);

            // Generate new daily quests
            for (int i = 0; i < dailyQuestCount; i++)
            {
                Quest quest = GenerateRandomQuest(Quest.QuestType.Daily);
                activeQuests.Add(quest);
            }

            Debug.Log($"[SeasonPassManager] Generated {dailyQuestCount} daily quests");
        }

        private void GenerateWeeklyQuests()
        {
            // Remove old weekly quests
            activeQuests.RemoveAll(q => q.questType == Quest.QuestType.Weekly);

            // Generate new weekly quests
            for (int i = 0; i < weeklyQuestCount; i++)
            {
                Quest quest = GenerateRandomQuest(Quest.QuestType.Weekly);
                activeQuests.Add(quest);
            }

            Debug.Log($"[SeasonPassManager] Generated {weeklyQuestCount} weekly quests");
        }

        private Quest GenerateRandomQuest(Quest.QuestType type)
        {
            // Generate random quest objectives
            string[] objectives = new string[]
            {
                "Complete {0} runs",
                "Travel {0} meters",
                "Collect {0} Soul Shards",
                "Open {0} doors",
                "Survive {0} monster encounters"
            };

            int targetValue = type == Quest.QuestType.Daily ? UnityEngine.Random.Range(3, 10) : UnityEngine.Random.Range(20, 50);
            string objective = string.Format(objectives[UnityEngine.Random.Range(0, objectives.Length)], targetValue);

            int xpReward = type == Quest.QuestType.Daily ? 200 : 1000;

            return new Quest
            {
                questId = Guid.NewGuid().ToString(),
                questType = type,
                description = objective,
                targetValue = targetValue,
                currentProgress = 0,
                xpReward = xpReward,
                isCompleted = false
            };
        }

        /// <summary>
        /// Updates quest progress
        /// </summary>
        public void UpdateQuestProgress(string questObjective, int amount)
        {
            foreach (Quest quest in activeQuests)
            {
                if (quest.isCompleted) continue;

                if (quest.description.Contains(questObjective))
                {
                    quest.currentProgress += amount;
                    OnQuestProgressed?.Invoke(quest);

                    if (quest.currentProgress >= quest.targetValue)
                    {
                        CompleteQuest(quest);
                    }
                }
            }
        }

        private void CompleteQuest(Quest quest)
        {
            quest.isCompleted = true;
            AwardXp(quest.xpReward, $"Quest: {quest.description}");

            OnQuestCompleted?.Invoke(quest);
            Debug.Log($"[SeasonPassManager] Quest completed: {quest.description} (+{quest.xpReward} XP)");
        }

        /// <summary>
        /// Gets all active quests
        /// </summary>
        public List<Quest> GetActiveQuests()
        {
            return new List<Quest>(activeQuests);
        }

        #endregion

        #region Data Structures

        [Serializable]
        public class SeasonConfig
        {
            public string seasonId;
            public string seasonName;
            public DateTime startDate;
            public DateTime endDate;
            public int maxTier;
            public int xpPerTier;
        }

        [Serializable]
        public class SeasonProgress
        {
            public string seasonId;
            public int currentTier;
            public int currentXp;
            public bool hasPremiumPass;
            public List<int> claimedFreeRewards;
            public List<int> claimedPremiumRewards;
        }

        [Serializable]
        public class SeasonReward
        {
            public int tier;
            public RewardType rewardType;
            public int amount;
            public string cosmeticId;

            public enum RewardType
            {
                Diamonds,
                Cosmetic,
                Capsule,
                Currency
            }
        }

        [Serializable]
        public class Quest
        {
            public string questId;
            public QuestType questType;
            public string description;
            public int targetValue;
            public int currentProgress;
            public int xpReward;
            public bool isCompleted;

            public enum QuestType
            {
                Daily,
                Weekly,
                Seasonal
            }

            public float ProgressPercentage => (float)currentProgress / targetValue;
        }

        #endregion
    }
}
