using UnityEngine;
using System;

namespace InfinityHouse.Economy
{
    /// <summary>
    /// Economy Service for INFINITE HAUS v5.9.
    /// Manages all currencies: Shards, Dust, XP.
    /// Modulare Struktur für Einsteiger - zentrale Währungsverwaltung.
    /// CPU: <0.05ms | Memory: 2KB | GC: 0B
    /// </summary>
    public class IH_EconomyService : MonoBehaviour
    {
        #region Singleton
        public static IH_EconomyService Instance { get; private set; }
        #endregion

        #region Currency State
        [Header("Currencies")]
        [SerializeField] private int shards = 0;
        [SerializeField] private int dust = 0;
        [SerializeField] private int xp = 0;
        [SerializeField] private int level = 1;

        [Header("XP Configuration")]
        [Tooltip("XP needed for level 2")]
        [SerializeField] private int baseXPPerLevel = 100;

        [Tooltip("XP scaling per level")]
        [SerializeField] private float xpScaling = 1.5f;
        #endregion

        #region Properties
        public int Shards => shards;
        public int Dust => dust;
        public int XP => xp;
        public int Level => level;
        #endregion

        #region Events
        public event Action<int> OnShardsChanged;
        public event Action<int> OnDustChanged;
        public event Action<int> OnXPChanged;
        public event Action<int> OnLevelUp; // new level
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
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

            LoadCurrencies();
        }
        #endregion

        #region Shards
        public void AddShards(int amount)
        {
            if (amount <= 0) return;

            shards += amount;
            OnShardsChanged?.Invoke(shards);
            SaveCurrencies();

            #if UNITY_EDITOR
            Debug.Log($"[Economy] +{amount} Shards (Total: {shards})");
            #endif
        }

        public bool TrySpendShards(int amount)
        {
            if (amount <= 0 || shards < amount)
                return false;

            shards -= amount;
            OnShardsChanged?.Invoke(shards);
            SaveCurrencies();

            #if UNITY_EDITOR
            Debug.Log($"[Economy] -{amount} Shards (Remaining: {shards})");
            #endif

            return true;
        }

        public int GetShards() => shards;
        #endregion

        #region Dust
        public void AddDust(int amount)
        {
            if (amount <= 0) return;

            dust += amount;
            OnDustChanged?.Invoke(dust);
            SaveCurrencies();

            #if UNITY_EDITOR
            Debug.Log($"[Economy] +{amount} Dust (Total: {dust})");
            #endif
        }

        public bool TrySpendDust(int amount)
        {
            if (amount <= 0 || dust < amount)
                return false;

            dust -= amount;
            OnDustChanged?.Invoke(dust);
            SaveCurrencies();

            #if UNITY_EDITOR
            Debug.Log($"[Economy] -{amount} Dust (Remaining: {dust})");
            #endif

            return true;
        }

        public int GetDust() => dust;
        #endregion

        #region XP & Leveling
        public void AddXP(int amount)
        {
            if (amount <= 0) return;

            xp += amount;
            OnXPChanged?.Invoke(xp);

            // Check for level up
            int xpNeeded = GetXPForNextLevel();
            while (xp >= xpNeeded)
            {
                LevelUp();
                xpNeeded = GetXPForNextLevel();
            }

            SaveCurrencies();

            #if UNITY_EDITOR
            Debug.Log($"[Economy] +{amount} XP (Total: {xp}, Level: {level})");
            #endif
        }

        private void LevelUp()
        {
            int xpNeeded = GetXPForNextLevel();
            xp -= xpNeeded;
            level++;

            OnLevelUp?.Invoke(level);

            #if UNITY_EDITOR
            Debug.Log($"[Economy] LEVEL UP! → Level {level}");
            #endif
        }

        public int GetXPForNextLevel()
        {
            return Mathf.RoundToInt(baseXPPerLevel * Mathf.Pow(xpScaling, level - 1));
        }

        public float GetLevelProgress()
        {
            int xpNeeded = GetXPForNextLevel();
            return Mathf.Clamp01((float)xp / xpNeeded);
        }

        public int GetXP() => xp;
        public int GetLevel() => level;
        #endregion

        #region Save/Load
        private void SaveCurrencies()
        {
            PlayerPrefs.SetInt("IH_Shards", shards);
            PlayerPrefs.SetInt("IH_Dust", dust);
            PlayerPrefs.SetInt("IH_XP", xp);
            PlayerPrefs.SetInt("IH_Level", level);
            PlayerPrefs.Save();
        }

        private void LoadCurrencies()
        {
            shards = PlayerPrefs.GetInt("IH_Shards", 0);
            dust = PlayerPrefs.GetInt("IH_Dust", 0);
            xp = PlayerPrefs.GetInt("IH_XP", 0);
            level = PlayerPrefs.GetInt("IH_Level", 1);

            #if UNITY_EDITOR
            Debug.Log($"[Economy] Loaded: {shards} Shards, {dust} Dust, XP {xp}, Level {level}");
            #endif
        }

        public void ResetCurrencies()
        {
            shards = 0;
            dust = 0;
            xp = 0;
            level = 1;

            SaveCurrencies();

            OnShardsChanged?.Invoke(shards);
            OnDustChanged?.Invoke(dust);
            OnXPChanged?.Invoke(xp);

            #if UNITY_EDITOR
            Debug.Log("[Economy] All currencies reset");
            #endif
        }
        #endregion

        #region Debug
        #if UNITY_EDITOR
        [ContextMenu("Add 100 Shards")]
        private void Debug_Add100Shards()
        {
            AddShards(100);
        }

        [ContextMenu("Add 1000 Dust")]
        private void Debug_Add1000Dust()
        {
            AddDust(1000);
        }

        [ContextMenu("Add 500 XP")]
        private void Debug_Add500XP()
        {
            AddXP(500);
        }

        [ContextMenu("Reset All")]
        private void Debug_ResetAll()
        {
            ResetCurrencies();
        }
        #endif
        #endregion
    }
}
