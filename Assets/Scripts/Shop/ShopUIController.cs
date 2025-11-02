using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace InfiniteHaus.Shop
{
    /// <summary>
    /// Controls shop UI, displays cosmetics, handles purchase interactions.
    /// Manages scrollable shop grid and item display.
    /// </summary>
    public class ShopUIController : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Main shop panel")]
        [SerializeField] private GameObject shopPanel;

        [Tooltip("Scroll view content container")]
        [SerializeField] private Transform shopItemsContainer;

        [Tooltip("Shop item prefab")]
        [SerializeField] private GameObject shopItemPrefab;

        [Tooltip("Diamond display text")]
        [SerializeField] private TextMeshProUGUI diamondCountText;

        [Tooltip("Selected item detail panel")]
        [SerializeField] private GameObject detailPanel;

        [Tooltip("Detail panel - item name")]
        [SerializeField] private TextMeshProUGUI detailItemName;

        [Tooltip("Detail panel - item description")]
        [SerializeField] private TextMeshProUGUI detailItemDescription;

        [Tooltip("Detail panel - item icon")]
        [SerializeField] private Image detailItemIcon;

        [Tooltip("Detail panel - cost text")]
        [SerializeField] private TextMeshProUGUI detailCostText;

        [Tooltip("Detail panel - purchase button")]
        [SerializeField] private Button purchaseButton;

        [Tooltip("Detail panel - equip button")]
        [SerializeField] private Button equipButton;

        [Tooltip("Detail panel - unequip button")]
        [SerializeField] private Button unequipButton;

        [Tooltip("Close button")]
        [SerializeField] private Button closeButton;

        [Header("Category Filters")]
        [Tooltip("Filter buttons")]
        [SerializeField] private Button allCategoriesButton;
        [SerializeField] private Button skinsButton;
        [SerializeField] private Button trailsButton;
        [SerializeField] private Button uiThemesButton;
        [SerializeField] private Button musicButton;
        [SerializeField] private Button filtersButton;

        [Header("Feedback")]
        [Tooltip("Insufficient funds panel")]
        [SerializeField] private GameObject insufficientFundsPanel;

        [Tooltip("Purchase success effect prefab")]
        [SerializeField] private GameObject purchaseSuccessEffect;

        [Header("Audio")]
        [Tooltip("Purchase sound")]
        [SerializeField] private AudioClip purchaseSFX;

        [Tooltip("Equip sound")]
        [SerializeField] private AudioClip equipSFX;

        [Tooltip("Error sound")]
        [SerializeField] private AudioClip errorSFX;

        // References
        private ShopManager shopManager;
        private DiamondCurrencyManager currencyManager;
        private AudioSource audioSource;

        // State
        private CosmeticItem selectedItem;
        private List<GameObject> spawnedShopItems = new List<GameObject>();
        private CosmeticItem.CosmeticType currentFilter = CosmeticItem.CosmeticType.PlayerSkin;
        private bool showingAllCategories = true;

        private void Awake()
        {
            shopManager = FindObjectOfType<ShopManager>();
            currencyManager = FindObjectOfType<DiamondCurrencyManager>();
            audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            // Setup button listeners
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseShop);
            }

            if (purchaseButton != null)
            {
                purchaseButton.onClick.AddListener(OnPurchaseButtonClicked);
            }

            if (equipButton != null)
            {
                equipButton.onClick.AddListener(OnEquipButtonClicked);
            }

            if (unequipButton != null)
            {
                unequipButton.onClick.AddListener(OnUnequipButtonClicked);
            }

            // Category filter buttons
            if (allCategoriesButton != null)
            {
                allCategoriesButton.onClick.AddListener(() => FilterByCategory(true));
            }

            if (skinsButton != null)
            {
                skinsButton.onClick.AddListener(() => FilterByCategory(false, CosmeticItem.CosmeticType.PlayerSkin));
            }

            if (trailsButton != null)
            {
                trailsButton.onClick.AddListener(() => FilterByCategory(false, CosmeticItem.CosmeticType.SoulTrail));
            }

            if (uiThemesButton != null)
            {
                uiThemesButton.onClick.AddListener(() => FilterByCategory(false, CosmeticItem.CosmeticType.UITheme));
            }

            if (musicButton != null)
            {
                musicButton.onClick.AddListener(() => FilterByCategory(false, CosmeticItem.CosmeticType.BackgroundMusic));
            }

            if (filtersButton != null)
            {
                filtersButton.onClick.AddListener(() => FilterByCategory(false, CosmeticItem.CosmeticType.ScreenFilter));
            }
        }

        private void OnEnable()
        {
            if (currencyManager != null)
            {
                currencyManager.OnDiamondsChanged += UpdateDiamondDisplay;
            }

            if (shopManager != null)
            {
                shopManager.OnCosmeticPurchased += OnCosmeticPurchased;
                shopManager.OnCosmeticEquipped += OnCosmeticEquipped;
                shopManager.OnPurchaseFailed += OnPurchaseFailed;
            }

            RefreshShop();
        }

        private void OnDisable()
        {
            if (currencyManager != null)
            {
                currencyManager.OnDiamondsChanged -= UpdateDiamondDisplay;
            }

            if (shopManager != null)
            {
                shopManager.OnCosmeticPurchased -= OnCosmeticPurchased;
                shopManager.OnCosmeticEquipped -= OnCosmeticEquipped;
                shopManager.OnPurchaseFailed -= OnPurchaseFailed;
            }
        }

        /// <summary>
        /// Opens the shop panel
        /// </summary>
        public void OpenShop()
        {
            if (shopPanel != null)
            {
                shopPanel.SetActive(true);
            }

            RefreshShop();
        }

        /// <summary>
        /// Closes the shop panel
        /// </summary>
        public void CloseShop()
        {
            if (shopPanel != null)
            {
                shopPanel.SetActive(false);
            }

            HideDetailPanel();
        }

        /// <summary>
        /// Refreshes all shop items
        /// </summary>
        public void RefreshShop()
        {
            ClearShopItems();
            PopulateShopItems();
            UpdateDiamondDisplay(currencyManager != null ? currencyManager.CurrentDiamonds : 0);
        }

        /// <summary>
        /// Clears all spawned shop items
        /// </summary>
        private void ClearShopItems()
        {
            foreach (var item in spawnedShopItems)
            {
                if (item != null)
                {
                    Destroy(item);
                }
            }
            spawnedShopItems.Clear();
        }

        /// <summary>
        /// Populates shop with cosmetic items
        /// </summary>
        private void PopulateShopItems()
        {
            if (shopManager == null || shopItemsContainer == null || shopItemPrefab == null) return;

            CosmeticItem[] cosmetics = showingAllCategories ?
                shopManager.GetAllCosmetics() :
                shopManager.GetCosmeticsByType(currentFilter);

            foreach (var cosmetic in cosmetics)
            {
                if (cosmetic == null) continue;

                GameObject itemObj = Instantiate(shopItemPrefab, shopItemsContainer);
                spawnedShopItems.Add(itemObj);

                // Setup item UI
                var itemUI = itemObj.GetComponent<ShopItemUI>();
                if (itemUI != null)
                {
                    itemUI.Setup(cosmetic, shopManager, this);
                }
            }
        }

        /// <summary>
        /// Shows detail panel for selected item
        /// </summary>
        public void ShowDetailPanel(CosmeticItem item)
        {
            if (item == null || detailPanel == null) return;

            selectedItem = item;
            detailPanel.SetActive(true);

            // Update detail panel UI
            if (detailItemName != null)
            {
                detailItemName.text = item.displayName;
                detailItemName.color = item.GetRarityColor();
            }

            if (detailItemDescription != null)
            {
                detailItemDescription.text = item.description;
            }

            if (detailItemIcon != null && item.previewIcon != null)
            {
                detailItemIcon.sprite = item.previewIcon;
            }

            if (detailCostText != null)
            {
                detailCostText.text = item.isFreeUnlock ? "FREE" : $"{item.diamondCost} 💎";
            }

            // Update button states
            bool isOwned = shopManager.IsOwned(item);
            bool isEquipped = shopManager.IsEquipped(item);

            if (purchaseButton != null)
            {
                purchaseButton.gameObject.SetActive(!isOwned);
                purchaseButton.interactable = currencyManager != null && currencyManager.HasEnoughDiamonds(item.diamondCost);
            }

            if (equipButton != null)
            {
                equipButton.gameObject.SetActive(isOwned && !isEquipped);
            }

            if (unequipButton != null)
            {
                unequipButton.gameObject.SetActive(isEquipped);
            }
        }

        /// <summary>
        /// Hides detail panel
        /// </summary>
        public void HideDetailPanel()
        {
            if (detailPanel != null)
            {
                detailPanel.SetActive(false);
            }
            selectedItem = null;
        }

        /// <summary>
        /// Updates diamond count display
        /// </summary>
        private void UpdateDiamondDisplay(int amount)
        {
            if (diamondCountText != null)
            {
                diamondCountText.text = $"💎 {amount}";
            }
        }

        /// <summary>
        /// Filters shop by category
        /// </summary>
        private void FilterByCategory(bool showAll, CosmeticItem.CosmeticType type = CosmeticItem.CosmeticType.PlayerSkin)
        {
            showingAllCategories = showAll;
            if (!showAll)
            {
                currentFilter = type;
            }

            RefreshShop();
        }

        #region Button Handlers

        private void OnPurchaseButtonClicked()
        {
            if (selectedItem == null || shopManager == null) return;

            if (shopManager.TryPurchaseCosmetic(selectedItem))
            {
                // Purchase successful - handled by event
            }
        }

        private void OnEquipButtonClicked()
        {
            if (selectedItem == null || shopManager == null) return;

            shopManager.EquipCosmetic(selectedItem);
        }

        private void OnUnequipButtonClicked()
        {
            if (selectedItem == null || shopManager == null) return;

            shopManager.UnequipCosmetic(selectedItem.type);
            HideDetailPanel();
            RefreshShop();
        }

        #endregion

        #region Event Handlers

        private void OnCosmeticPurchased(CosmeticItem cosmetic)
        {
            PlaySound(purchaseSFX);

            // Show purchase success effect
            if (purchaseSuccessEffect != null)
            {
                Instantiate(purchaseSuccessEffect, transform);
            }

            // Refresh shop to update UI
            RefreshShop();

            // Update detail panel if same item
            if (selectedItem == cosmetic)
            {
                ShowDetailPanel(cosmetic);
            }

            Debug.Log($"Purchase successful: {cosmetic.displayName}");
        }

        private void OnCosmeticEquipped(CosmeticItem cosmetic)
        {
            PlaySound(equipSFX);
            RefreshShop();

            if (selectedItem == cosmetic)
            {
                ShowDetailPanel(cosmetic);
            }
        }

        private void OnPurchaseFailed(string reason)
        {
            PlaySound(errorSFX);

            if (reason == "Insufficient diamonds" && insufficientFundsPanel != null)
            {
                insufficientFundsPanel.SetActive(true);
                Invoke(nameof(HideInsufficientFundsPanel), 2f);
            }

            Debug.LogWarning($"Purchase failed: {reason}");
        }

        private void HideInsufficientFundsPanel()
        {
            if (insufficientFundsPanel != null)
            {
                insufficientFundsPanel.SetActive(false);
            }
        }

        #endregion

        /// <summary>
        /// Plays UI sound effect
        /// </summary>
        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
    }

    /// <summary>
    /// Individual shop item UI component (attach to shop item prefab)
    /// </summary>
    public class ShopItemUI : MonoBehaviour
    {
        [Header("UI Elements")]
        public Image iconImage;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI costText;
        public Image rarityBorder;
        public GameObject ownedIndicator;
        public GameObject equippedIndicator;
        public Button selectButton;

        private CosmeticItem cosmetic;
        private ShopManager shopManager;
        private ShopUIController shopUI;

        public void Setup(CosmeticItem item, ShopManager manager, ShopUIController ui)
        {
            cosmetic = item;
            shopManager = manager;
            shopUI = ui;

            // Set icon
            if (iconImage != null && item.previewIcon != null)
            {
                iconImage.sprite = item.previewIcon;
            }

            // Set name
            if (nameText != null)
            {
                nameText.text = item.displayName;
            }

            // Set cost
            if (costText != null)
            {
                costText.text = item.isFreeUnlock ? "FREE" : $"💎 {item.diamondCost}";
            }

            // Set rarity border color
            if (rarityBorder != null)
            {
                rarityBorder.color = item.GetRarityColor();
            }

            // Show owned/equipped indicators
            bool isOwned = shopManager.IsOwned(item);
            bool isEquipped = shopManager.IsEquipped(item);

            if (ownedIndicator != null)
            {
                ownedIndicator.SetActive(isOwned);
            }

            if (equippedIndicator != null)
            {
                equippedIndicator.SetActive(isEquipped);
            }

            // Setup button
            if (selectButton != null)
            {
                selectButton.onClick.RemoveAllListeners();
                selectButton.onClick.AddListener(OnItemSelected);
            }
        }

        private void OnItemSelected()
        {
            if (shopUI != null && cosmetic != null)
            {
                shopUI.ShowDetailPanel(cosmetic);
            }
        }
    }
}
