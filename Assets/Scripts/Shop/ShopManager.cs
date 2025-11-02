using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace InfiniteHaus.Shop
{
    /// <summary>
    /// Manages shop logic, cosmetic ownership, and equipped items.
    /// Handles purchasing, equipping, and applying cosmetics.
    /// </summary>
    public class ShopManager : MonoBehaviour
    {
        [Header("Cosmetic Catalog")]
        [Tooltip("All available cosmetic items in the game")]
        [SerializeField] private CosmeticItem[] availableCosmetics;

        [Header("Default Cosmetics")]
        [Tooltip("Default player skin (always equipped initially)")]
        [SerializeField] private CosmeticItem defaultPlayerSkin;

        [Tooltip("Default trail effect")]
        [SerializeField] private CosmeticItem defaultTrail;

        [Header("References")]
        [Tooltip("Diamond currency manager")]
        [SerializeField] private DiamondCurrencyManager currencyManager;

        // Runtime data
        private HashSet<string> ownedCosmetics = new HashSet<string>();
        private Dictionary<CosmeticItem.CosmeticType, string> equippedCosmetics = new Dictionary<CosmeticItem.CosmeticType, string>();

        // Events
        public event Action<CosmeticItem> OnCosmeticPurchased;
        public event Action<CosmeticItem> OnCosmeticEquipped;
        public event Action<CosmeticItem> OnCosmeticUnequipped;
        public event Action<string> OnPurchaseFailed; // (reason)

        // Singleton
        public static ShopManager Instance { get; private set; }

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
        }

        private void Start()
        {
            if (currencyManager == null)
            {
                currencyManager = FindObjectOfType<DiamondCurrencyManager>();
            }

            LoadShopData();
            EquipDefaultCosmetics();
        }

        /// <summary>
        /// Loads owned and equipped cosmetics from save system
        /// </summary>
        public void LoadShopData()
        {
            var saveData = Save.SaveSystem.LoadGame();

            if (saveData != null)
            {
                // Load owned cosmetics
                ownedCosmetics.Clear();
                if (saveData.ownedCosmeticIDs != null)
                {
                    foreach (string id in saveData.ownedCosmeticIDs)
                    {
                        ownedCosmetics.Add(id);
                    }
                }

                // Load equipped cosmetics
                equippedCosmetics.Clear();
                if (saveData.equippedCosmetics != null)
                {
                    foreach (var kvp in saveData.equippedCosmetics)
                    {
                        if (Enum.TryParse(kvp.Key, out CosmeticItem.CosmeticType type))
                        {
                            equippedCosmetics[type] = kvp.Value;
                        }
                    }
                }

                Debug.Log($"Loaded {ownedCosmetics.Count} owned cosmetics, {equippedCosmetics.Count} equipped");
            }
            else
            {
                // First time - own default cosmetics
                if (defaultPlayerSkin != null)
                {
                    ownedCosmetics.Add(defaultPlayerSkin.cosmeticID);
                }

                if (defaultTrail != null)
                {
                    ownedCosmetics.Add(defaultTrail.cosmeticID);
                }

                SaveShopData();
            }
        }

        /// <summary>
        /// Saves owned and equipped cosmetics to save system
        /// </summary>
        public void SaveShopData()
        {
            var saveData = Save.SaveSystem.LoadGame();

            if (saveData != null)
            {
                // Save owned cosmetics
                saveData.ownedCosmeticIDs = ownedCosmetics.ToArray();

                // Save equipped cosmetics
                saveData.equippedCosmetics = new Dictionary<string, string>();
                foreach (var kvp in equippedCosmetics)
                {
                    saveData.equippedCosmetics[kvp.Key.ToString()] = kvp.Value;
                }

                Save.SaveSystem.SaveGame(saveData);
                Save.SaveSystem.QuickSave(saveData);
            }
        }

        /// <summary>
        /// Attempts to purchase a cosmetic item
        /// </summary>
        public bool TryPurchaseCosmetic(CosmeticItem cosmetic)
        {
            if (cosmetic == null)
            {
                OnPurchaseFailed?.Invoke("Invalid cosmetic");
                return false;
            }

            // Check if already owned
            if (IsOwned(cosmetic))
            {
                OnPurchaseFailed?.Invoke("Already owned");
                return false;
            }

            // Check level requirement
            if (cosmetic.requiredLevel > 0)
            {
                // TODO: Implement player level system
                // For now, allow all purchases
            }

            // Check if free unlock
            if (cosmetic.isFreeUnlock)
            {
                UnlockCosmetic(cosmetic);
                return true;
            }

            // Try to spend diamonds
            if (currencyManager != null)
            {
                if (currencyManager.TrySpendDiamonds(cosmetic.diamondCost))
                {
                    UnlockCosmetic(cosmetic);
                    return true;
                }
                else
                {
                    OnPurchaseFailed?.Invoke("Insufficient diamonds");
                    return false;
                }
            }
            else
            {
                Debug.LogError("DiamondCurrencyManager not found!");
                OnPurchaseFailed?.Invoke("System error");
                return false;
            }
        }

        /// <summary>
        /// Unlocks a cosmetic without payment (rewards, free items)
        /// </summary>
        public void UnlockCosmetic(CosmeticItem cosmetic)
        {
            if (cosmetic == null || IsOwned(cosmetic)) return;

            ownedCosmetics.Add(cosmetic.cosmeticID);
            OnCosmeticPurchased?.Invoke(cosmetic);
            SaveShopData();

            Debug.Log($"Unlocked cosmetic: {cosmetic.displayName}");
        }

        /// <summary>
        /// Equips a cosmetic item (must be owned)
        /// </summary>
        public bool EquipCosmetic(CosmeticItem cosmetic)
        {
            if (cosmetic == null)
            {
                Debug.LogWarning("Cannot equip null cosmetic");
                return false;
            }

            if (!IsOwned(cosmetic))
            {
                Debug.LogWarning($"Cannot equip {cosmetic.displayName} - not owned");
                return false;
            }

            // Equip the cosmetic
            equippedCosmetics[cosmetic.type] = cosmetic.cosmeticID;
            OnCosmeticEquipped?.Invoke(cosmetic);
            SaveShopData();

            // Apply cosmetic to game
            ApplyCosmetic(cosmetic);

            Debug.Log($"Equipped: {cosmetic.displayName}");
            return true;
        }

        /// <summary>
        /// Unequips a cosmetic type
        /// </summary>
        public void UnequipCosmetic(CosmeticItem.CosmeticType type)
        {
            if (equippedCosmetics.ContainsKey(type))
            {
                string cosmeticID = equippedCosmetics[type];
                CosmeticItem cosmetic = GetCosmeticByID(cosmeticID);

                equippedCosmetics.Remove(type);
                OnCosmeticUnequipped?.Invoke(cosmetic);
                SaveShopData();

                // Revert to default
                RevertToDefault(type);
            }
        }

        /// <summary>
        /// Applies cosmetic effects to the game
        /// </summary>
        private void ApplyCosmetic(CosmeticItem cosmetic)
        {
            switch (cosmetic.type)
            {
                case CosmeticItem.CosmeticType.PlayerSkin:
                    ApplyPlayerSkin(cosmetic);
                    break;

                case CosmeticItem.CosmeticType.SoulTrail:
                    ApplySoulTrail(cosmetic);
                    break;

                case CosmeticItem.CosmeticType.UITheme:
                    ApplyUITheme(cosmetic);
                    break;

                case CosmeticItem.CosmeticType.BackgroundMusic:
                    ApplyBackgroundMusic(cosmetic);
                    break;

                case CosmeticItem.CosmeticType.ScreenFilter:
                    ApplyScreenFilter(cosmetic);
                    break;
            }
        }

        #region Cosmetic Application

        private void ApplyPlayerSkin(CosmeticItem cosmetic)
        {
            var player = FindObjectOfType<Player.PlayerController>();
            if (player != null && cosmetic.playerSkinSprite != null)
            {
                var spriteRenderer = player.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    spriteRenderer.sprite = cosmetic.playerSkinSprite;
                }

                // Apply animator override if available
                if (cosmetic.animatorOverride != null)
                {
                    var animator = player.GetComponent<Animator>();
                    if (animator != null)
                    {
                        animator.runtimeAnimatorController = cosmetic.animatorOverride;
                    }
                }
            }
        }

        private void ApplySoulTrail(CosmeticItem cosmetic)
        {
            var player = FindObjectOfType<Player.PlayerController>();
            if (player != null && cosmetic.trailEffectPrefab != null)
            {
                // Remove old trail
                var oldTrail = player.GetComponentInChildren<TrailRenderer>();
                if (oldTrail != null)
                {
                    Destroy(oldTrail.gameObject);
                }

                // Instantiate new trail
                Instantiate(cosmetic.trailEffectPrefab, player.transform);
            }
        }

        private void ApplyUITheme(CosmeticItem cosmetic)
        {
            var themeManager = FindObjectOfType<Theme.ThemeManager>();
            if (themeManager != null && cosmetic.uiThemeConfig != null)
            {
                themeManager.LoadTheme(cosmetic.uiThemeConfig);
            }
        }

        private void ApplyBackgroundMusic(CosmeticItem cosmetic)
        {
            var themeManager = FindObjectOfType<Theme.ThemeManager>();
            if (themeManager != null && cosmetic.backgroundMusicClip != null)
            {
                // Apply custom BGM
                // This would need ThemeManager extension to support custom BGM
                Debug.Log($"Applied custom BGM: {cosmetic.displayName}");
            }
        }

        private void ApplyScreenFilter(CosmeticItem cosmetic)
        {
            if (cosmetic.screenFilterMaterial != null)
            {
                // Apply post-processing or camera filter
                var mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    // TODO: Implement screen filter system
                    Debug.Log($"Applied screen filter: {cosmetic.displayName}");
                }
            }
        }

        private void RevertToDefault(CosmeticItem.CosmeticType type)
        {
            switch (type)
            {
                case CosmeticItem.CosmeticType.PlayerSkin:
                    if (defaultPlayerSkin != null)
                    {
                        ApplyPlayerSkin(defaultPlayerSkin);
                    }
                    break;

                case CosmeticItem.CosmeticType.SoulTrail:
                    if (defaultTrail != null)
                    {
                        ApplySoulTrail(defaultTrail);
                    }
                    break;
            }
        }

        #endregion

        #region Queries

        /// <summary>
        /// Checks if player owns a cosmetic
        /// </summary>
        public bool IsOwned(CosmeticItem cosmetic)
        {
            return cosmetic != null && ownedCosmetics.Contains(cosmetic.cosmeticID);
        }

        /// <summary>
        /// Checks if a cosmetic is currently equipped
        /// </summary>
        public bool IsEquipped(CosmeticItem cosmetic)
        {
            if (cosmetic == null) return false;

            return equippedCosmetics.TryGetValue(cosmetic.type, out string equippedID) &&
                   equippedID == cosmetic.cosmeticID;
        }

        /// <summary>
        /// Gets cosmetic by ID
        /// </summary>
        public CosmeticItem GetCosmeticByID(string cosmeticID)
        {
            return Array.Find(availableCosmetics, c => c.cosmeticID == cosmeticID);
        }

        /// <summary>
        /// Gets all cosmetics of a specific type
        /// </summary>
        public CosmeticItem[] GetCosmeticsByType(CosmeticItem.CosmeticType type)
        {
            return availableCosmetics.Where(c => c.type == type).ToArray();
        }

        /// <summary>
        /// Gets all available cosmetics
        /// </summary>
        public CosmeticItem[] GetAllCosmetics()
        {
            return availableCosmetics;
        }

        /// <summary>
        /// Gets equipped cosmetic of a specific type
        /// </summary>
        public CosmeticItem GetEquippedCosmetic(CosmeticItem.CosmeticType type)
        {
            if (equippedCosmetics.TryGetValue(type, out string cosmeticID))
            {
                return GetCosmeticByID(cosmeticID);
            }
            return null;
        }

        #endregion

        /// <summary>
        /// Equips default cosmetics on first load
        /// </summary>
        private void EquipDefaultCosmetics()
        {
            if (defaultPlayerSkin != null && !equippedCosmetics.ContainsKey(CosmeticItem.CosmeticType.PlayerSkin))
            {
                EquipCosmetic(defaultPlayerSkin);
            }

            if (defaultTrail != null && !equippedCosmetics.ContainsKey(CosmeticItem.CosmeticType.SoulTrail))
            {
                EquipCosmetic(defaultTrail);
            }
        }

        private void OnApplicationQuit()
        {
            SaveShopData();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SaveShopData();
            }
        }
    }
}
