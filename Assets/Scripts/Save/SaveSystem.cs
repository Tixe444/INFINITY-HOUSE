using UnityEngine;
using System.IO;

namespace InfiniteHaus.Save
{
    /// <summary>
    /// Handles saving and loading game data to/from disk.
    /// Uses JSON serialization with PlayerPrefs fallback.
    /// </summary>
    public static class SaveSystem
    {
        private static readonly string SAVE_FOLDER = "InfiniteHausSaves";
        private static readonly string SAVE_FILE = "savegame.json";

        /// <summary>
        /// Gets the full path to save file
        /// </summary>
        private static string GetSavePath()
        {
            string folderPath = Path.Combine(Application.persistentDataPath, SAVE_FOLDER);

            // Create folder if it doesn't exist
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            return Path.Combine(folderPath, SAVE_FILE);
        }

        /// <summary>
        /// Saves game data to disk
        /// </summary>
        public static void SaveGame(SaveData data)
        {
            if (data == null)
            {
                Debug.LogError("Cannot save null SaveData!");
                return;
            }

            try
            {
                // Update timestamp
                data.UpdateLastPlayed();

                // Serialize to JSON
                string json = JsonUtility.ToJson(data, true);

                // Write to file
                string savePath = GetSavePath();
                File.WriteAllText(savePath, json);

                Debug.Log($"Game saved successfully to: {savePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save game: {e.Message}");
            }
        }

        /// <summary>
        /// Loads game data from disk
        /// </summary>
        public static SaveData LoadGame()
        {
            string savePath = GetSavePath();

            if (!File.Exists(savePath))
            {
                Debug.Log("No save file found, creating new save data");
                return SaveData.CreateDefault();
            }

            try
            {
                // Read from file
                string json = File.ReadAllText(savePath);

                // Deserialize from JSON
                SaveData data = JsonUtility.FromJson<SaveData>(json);

                Debug.Log($"Game loaded successfully from: {savePath}");
                return data;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load game: {e.Message}");
                return SaveData.CreateDefault();
            }
        }

        /// <summary>
        /// Checks if save file exists
        /// </summary>
        public static bool SaveExists()
        {
            return File.Exists(GetSavePath());
        }

        /// <summary>
        /// Deletes save file
        /// </summary>
        public static void DeleteSave()
        {
            string savePath = GetSavePath();

            if (File.Exists(savePath))
            {
                try
                {
                    File.Delete(savePath);
                    Debug.Log("Save file deleted");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to delete save: {e.Message}");
                }
            }
        }

        /// <summary>
        /// Quick save using PlayerPrefs (backup method)
        /// </summary>
        public static void QuickSave(SaveData data)
        {
            if (data == null) return;

            PlayerPrefs.SetInt("HighScore", data.highScore);
            PlayerPrefs.SetFloat("BestDistance", data.bestDistance);
            PlayerPrefs.SetInt("TotalRelics", data.totalRelicsCollected);
            PlayerPrefs.SetInt("HPUpgrades", data.permanentHPUpgrades);
            PlayerPrefs.SetFloat("MasterVolume", data.masterVolume);
            PlayerPrefs.SetFloat("MusicVolume", data.musicVolume);
            PlayerPrefs.SetFloat("SFXVolume", data.sfxVolume);
            PlayerPrefs.SetInt("LastThemeIndex", data.lastThemeIndex);
            PlayerPrefs.Save();

            Debug.Log("Quick save completed");
        }

        /// <summary>
        /// Quick load using PlayerPrefs (backup method)
        /// </summary>
        public static SaveData QuickLoad()
        {
            SaveData data = SaveData.CreateDefault();

            if (PlayerPrefs.HasKey("HighScore"))
            {
                data.highScore = PlayerPrefs.GetInt("HighScore", 0);
                data.bestDistance = PlayerPrefs.GetFloat("BestDistance", 0f);
                data.totalRelicsCollected = PlayerPrefs.GetInt("TotalRelics", 0);
                data.permanentHPUpgrades = PlayerPrefs.GetInt("HPUpgrades", 0);
                data.masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
                data.musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
                data.sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
                data.lastThemeIndex = PlayerPrefs.GetInt("LastThemeIndex", 0);

                Debug.Log("Quick load completed");
            }

            return data;
        }

        /// <summary>
        /// Gets save file info for debug/display
        /// </summary>
        public static string GetSaveInfo()
        {
            string savePath = GetSavePath();

            if (!File.Exists(savePath))
            {
                return "No save file found";
            }

            FileInfo info = new FileInfo(savePath);
            return $"Save File: {info.Name}\n" +
                   $"Size: {info.Length} bytes\n" +
                   $"Last Modified: {info.LastWriteTime}\n" +
                   $"Path: {savePath}";
        }
    }
}
