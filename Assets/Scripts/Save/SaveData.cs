using System;

namespace InfiniteHaus.Save
{
    /// <summary>
    /// Serializable data structure for save game information.
    /// Contains all persistent player data across sessions.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        // Game Progress
        public int highScore;
        public float bestDistance;
        public int totalRuns;
        public int totalDeaths;

        // Permanent Upgrades
        public int totalRelicsCollected;
        public int permanentHPUpgrades;
        public int[] unlockedCosmetics; // IDs of unlocked cosmetic items

        // Settings
        public float masterVolume = 1f;
        public float musicVolume = 1f;
        public float sfxVolume = 1f;
        public bool fullscreen = true;
        public int resolutionWidth = 1920;
        public int resolutionHeight = 1080;

        // Theme Preferences
        public int lastThemeIndex;
        public bool randomThemePerRun;

        // Input Bindings (optional - for rebindable controls)
        public string jumpKey = "Space";
        public string leftDoorKey = "LeftArrow";
        public string rightDoorKey = "RightArrow";
        public string pauseKey = "Escape";

        // Statistics
        public int totalSoulShardsCollected;
        public int totalChaseCrystalsCollected;
        public float totalDistanceTraveled;
        public int totalDoorsOpened;

        // Timestamps
        public string lastPlayedDate;
        public string firstPlayedDate;

        /// <summary>
        /// Creates a new default save data
        /// </summary>
        public static SaveData CreateDefault()
        {
            SaveData data = new SaveData
            {
                highScore = 0,
                bestDistance = 0f,
                totalRuns = 0,
                totalDeaths = 0,
                totalRelicsCollected = 0,
                permanentHPUpgrades = 0,
                unlockedCosmetics = new int[0],
                masterVolume = 1f,
                musicVolume = 0.8f,
                sfxVolume = 1f,
                fullscreen = true,
                resolutionWidth = 1920,
                resolutionHeight = 1080,
                lastThemeIndex = 0,
                randomThemePerRun = false,
                jumpKey = "Space",
                leftDoorKey = "LeftArrow",
                rightDoorKey = "RightArrow",
                pauseKey = "Escape",
                totalSoulShardsCollected = 0,
                totalChaseCrystalsCollected = 0,
                totalDistanceTraveled = 0f,
                totalDoorsOpened = 0,
                firstPlayedDate = DateTime.Now.ToString(),
                lastPlayedDate = DateTime.Now.ToString()
            };

            return data;
        }

        /// <summary>
        /// Updates last played timestamp
        /// </summary>
        public void UpdateLastPlayed()
        {
            lastPlayedDate = DateTime.Now.ToString();
        }

        /// <summary>
        /// Checks if a cosmetic is unlocked
        /// </summary>
        public bool IsCosmeticUnlocked(int cosmeticID)
        {
            if (unlockedCosmetics == null) return false;
            return Array.Exists(unlockedCosmetics, id => id == cosmeticID);
        }

        /// <summary>
        /// Unlocks a cosmetic item
        /// </summary>
        public void UnlockCosmetic(int cosmeticID)
        {
            if (IsCosmeticUnlocked(cosmeticID)) return;

            int[] newArray = new int[unlockedCosmetics.Length + 1];
            Array.Copy(unlockedCosmetics, newArray, unlockedCosmetics.Length);
            newArray[unlockedCosmetics.Length] = cosmeticID;
            unlockedCosmetics = newArray;
        }
    }
}
