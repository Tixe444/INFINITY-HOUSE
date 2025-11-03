// ═══════════════════════════════════════════════════════════════════════════════
// INFINITY HOUSE - Made by Mate Makovics
// Entitlement Service - Grants purchased items
// ═══════════════════════════════════════════════════════════════════════════════

using UnityEngine;
using System;
using System.Collections.Generic;
using InfinityHouse.Shop;
using InfinityHouse.Save;

namespace InfinityHouse.IAP
{
    /// <summary>
    /// Entitlement service that grants purchased items to the player.
    /// Handles diamond packs, cosmetics, season pass, and capsules.
    /// Prevents double-granting and tracks purchase history.
    /// </summary>
    public class EntitlementService : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private bool preventDuplicateGrants = true;
        [SerializeField] private bool saveAfterGrant = true;

        // Granted transaction tracking
        private HashSet<string> grantedTransactions;
        private const string GRANTED_TRANSACTIONS_KEY = "GrantedTransactions";

        // Events
        public event Action<string, int> OnDiamondsGranted; // productId, amount
        public event Action<string> OnCosmeticGranted; // cosmeticId
        public event Action<string> OnSeasonPassGranted; // seasonId
        public event Action<string, List<string>> OnCapsuleOpened; // capsuleId, cosmeticIds

        private void Awake()
        {
            LoadGrantedTransactions();
        }

        /// <summary>
        /// Grants entitlement for a purchased product
        /// </summary>
        public bool GrantEntitlement(string productId, string receipt)
        {
            // Extract transaction ID from receipt
            string transactionId = ExtractTransactionId(receipt);

            // Check if already granted
            if (preventDuplicateGrants && IsAlreadyGranted(transactionId))
            {
                Debug.LogWarning($"[EntitlementService] Transaction already granted: {transactionId}");
                return false;
            }

            Debug.Log($"[EntitlementService] Granting entitlement: {productId}");

            // Grant based on product type
            bool success = ProcessEntitlement(productId, receipt);

            if (success)
            {
                // Mark as granted
                MarkAsGranted(transactionId);

                // Save progress
                if (saveAfterGrant)
                {
                    SaveSystem.QuickSave(SaveSystem.LoadGame());
                }

                Debug.Log($"[EntitlementService] Successfully granted: {productId}");
            }
            else
            {
                Debug.LogError($"[EntitlementService] Failed to grant: {productId}");
            }

            return success;
        }

        /// <summary>
        /// Processes restored purchases
        /// </summary>
        public void ProcessRestoredPurchases(List<string> productIds)
        {
            Debug.Log($"[EntitlementService] Processing {productIds.Count} restored purchases");

            foreach (string productId in productIds)
            {
                // For non-consumables only (season pass, permanent unlocks)
                if (IsNonConsumable(productId))
                {
                    RestoreNonConsumable(productId);
                }
            }
        }

        private bool ProcessEntitlement(string productId, string receipt)
        {
            // Diamond packs
            if (productId.Contains("diamonds_"))
            {
                return GrantDiamondPack(productId);
            }
            // Season pass
            else if (productId.Contains("seasonpass"))
            {
                return GrantSeasonPass(productId);
            }
            // Capsules
            else if (productId.Contains("capsule"))
            {
                return GrantCapsule(productId);
            }
            // Direct cosmetic purchase
            else if (productId.StartsWith("cosmetic_"))
            {
                return GrantCosmetic(productId);
            }
            else
            {
                Debug.LogWarning($"[EntitlementService] Unknown product type: {productId}");
                return false;
            }
        }

        #region Grant Implementations

        private bool GrantDiamondPack(string productId)
        {
            // Extract diamond amount from product ID
            int diamondAmount = GetDiamondAmount(productId);

            if (diamondAmount <= 0)
            {
                Debug.LogError($"[EntitlementService] Invalid diamond amount: {productId}");
                return false;
            }

            // Grant diamonds via currency manager
            var currencyManager = DiamondCurrencyManager.Instance;
            if (currencyManager != null)
            {
                currencyManager.AddDiamonds(diamondAmount, $"IAP: {productId}");
                OnDiamondsGranted?.Invoke(productId, diamondAmount);

                Debug.Log($"[EntitlementService] Granted {diamondAmount} diamonds");
                return true;
            }
            else
            {
                Debug.LogError("[EntitlementService] DiamondCurrencyManager not found!");
                return false;
            }
        }

        private int GetDiamondAmount(string productId)
        {
            // Parse diamond amount from product ID
            // Expected format: "diamonds_100", "diamonds_500", etc.
            string[] parts = productId.Split('_');
            if (parts.Length >= 2 && int.TryParse(parts[1], out int amount))
            {
                return amount;
            }
            return 0;
        }

        private bool GrantSeasonPass(string productId)
        {
            // Extract season ID
            string seasonId = productId.Replace("seasonpass_", "");

            // Grant season pass via SeasonPassManager
            var seasonPassManager = FindObjectOfType<SeasonPassManager>();
            if (seasonPassManager != null)
            {
                seasonPassManager.UnlockPremiumPass(seasonId);
                OnSeasonPassGranted?.Invoke(seasonId);

                Debug.Log($"[EntitlementService] Granted season pass: {seasonId}");
                return true;
            }
            else
            {
                Debug.LogWarning("[EntitlementService] SeasonPassManager not found - will grant when available");
                // Store for later grant
                StoreDelayedEntitlement(productId);
                return true;
            }
        }

        private bool GrantCapsule(string productId)
        {
            // Open capsule via CapsuleManager
            var capsuleManager = FindObjectOfType<CapsuleManager>();
            if (capsuleManager != null)
            {
                List<string> cosmeticIds = capsuleManager.OpenCapsule(productId);
                OnCapsuleOpened?.Invoke(productId, cosmeticIds);

                Debug.Log($"[EntitlementService] Opened capsule: {productId} ({cosmeticIds.Count} items)");
                return true;
            }
            else
            {
                Debug.LogError("[EntitlementService] CapsuleManager not found!");
                return false;
            }
        }

        private bool GrantCosmetic(string productId)
        {
            // Extract cosmetic ID
            string cosmeticId = productId.Replace("cosmetic_", "");

            // Grant via ShopManager
            var shopManager = ShopManager.Instance;
            if (shopManager != null)
            {
                // Find cosmetic item
                var cosmetic = shopManager.GetCosmeticById(cosmeticId);
                if (cosmetic != null)
                {
                    shopManager.GrantCosmetic(cosmetic);
                    OnCosmeticGranted?.Invoke(cosmeticId);

                    Debug.Log($"[EntitlementService] Granted cosmetic: {cosmeticId}");
                    return true;
                }
                else
                {
                    Debug.LogError($"[EntitlementService] Cosmetic not found: {cosmeticId}");
                    return false;
                }
            }
            else
            {
                Debug.LogError("[EntitlementService] ShopManager not found!");
                return false;
            }
        }

        #endregion

        #region Non-Consumable Restoration

        private bool IsNonConsumable(string productId)
        {
            return productId.Contains("seasonpass");
        }

        private void RestoreNonConsumable(string productId)
        {
            Debug.Log($"[EntitlementService] Restoring non-consumable: {productId}");

            if (productId.Contains("seasonpass"))
            {
                GrantSeasonPass(productId);
            }
        }

        #endregion

        #region Transaction Tracking

        private string ExtractTransactionId(string receipt)
        {
            try
            {
                // Parse receipt JSON to extract transaction ID
                var receiptData = JsonUtility.FromJson<ReceiptData>(receipt);
                return receiptData.transactionId ?? Guid.NewGuid().ToString();
            }
            catch
            {
                // Fallback: Use receipt hash as transaction ID
                return receipt.GetHashCode().ToString();
            }
        }

        private bool IsAlreadyGranted(string transactionId)
        {
            return grantedTransactions.Contains(transactionId);
        }

        private void MarkAsGranted(string transactionId)
        {
            grantedTransactions.Add(transactionId);
            SaveGrantedTransactions();
        }

        private void LoadGrantedTransactions()
        {
            grantedTransactions = new HashSet<string>();

            string json = PlayerPrefs.GetString(GRANTED_TRANSACTIONS_KEY, "");
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var data = JsonUtility.FromJson<TransactionList>(json);
                    foreach (string txId in data.transactions)
                    {
                        grantedTransactions.Add(txId);
                    }
                    Debug.Log($"[EntitlementService] Loaded {grantedTransactions.Count} granted transactions");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[EntitlementService] Failed to load granted transactions: {ex.Message}");
                }
            }
        }

        private void SaveGrantedTransactions()
        {
            var data = new TransactionList
            {
                transactions = new List<string>(grantedTransactions)
            };

            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(GRANTED_TRANSACTIONS_KEY, json);
            PlayerPrefs.Save();
        }

        #endregion

        #region Delayed Entitlements

        private void StoreDelayedEntitlement(string productId)
        {
            // Store entitlement to grant later (e.g., when SeasonPassManager loads)
            List<string> delayed = LoadDelayedEntitlements();
            delayed.Add(productId);
            SaveDelayedEntitlements(delayed);
        }

        private List<string> LoadDelayedEntitlements()
        {
            string json = PlayerPrefs.GetString("DelayedEntitlements", "");
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var data = JsonUtility.FromJson<ProductList>(json);
                    return new List<string>(data.products);
                }
                catch
                {
                    return new List<string>();
                }
            }
            return new List<string>();
        }

        private void SaveDelayedEntitlements(List<string> products)
        {
            var data = new ProductList { products = products };
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString("DelayedEntitlements", json);
            PlayerPrefs.Save();
        }

        #endregion

        #region Data Structures

        [Serializable]
        private class ReceiptData
        {
            public string transactionId;
            public string productId;
            public string purchaseDate;
        }

        [Serializable]
        private class TransactionList
        {
            public List<string> transactions;
        }

        [Serializable]
        private class ProductList
        {
            public List<string> products;
        }

        #endregion
    }
}
