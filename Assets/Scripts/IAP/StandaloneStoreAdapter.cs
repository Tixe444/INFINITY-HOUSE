// ═══════════════════════════════════════════════════════════════════════════════
// INFINITY HOUSE - Made by Mate Makovics
// Standalone Store Adapter (Testing/Development)
// ═══════════════════════════════════════════════════════════════════════════════

using UnityEngine;
using System;
using System.Collections.Generic;

namespace InfinityHouse.IAP
{
    /// <summary>
    /// Standalone store adapter for testing without actual IAP.
    /// Simulates purchase flow for development and testing.
    /// Use for editor testing and platforms without store support.
    /// </summary>
    public class StandaloneStoreAdapter : MonoBehaviour, IPlatformStoreAdapter
    {
        [Header("Simulation Settings")]
        [SerializeField] private float purchaseDelay = 1f;
        [SerializeField] private bool simulateFailures = false;
        [SerializeField] private float failureRate = 0.1f; // 10%

        private bool isInitialized = false;
        private Dictionary<string, PurchaseGateway.IapProduct> productCache;
        private List<string> ownedProducts;

        // Events
        public event Action<List<PurchaseGateway.IapProduct>> OnInitialized;
        public event Action<string> OnInitializeFailed;
        public event Action<string, string> OnPurchaseComplete;
        public event Action<string, string> OnPurchaseFailed;

        // Test prices in USD
        private Dictionary<string, decimal> testPrices = new Dictionary<string, decimal>
        {
            { "diamonds_100", 0.99m },
            { "diamonds_500", 4.99m },
            { "diamonds_1000", 9.99m },
            { "diamonds_2500", 19.99m },
            { "diamonds_5000", 49.99m },
            { "seasonpass_s1", 9.99m },
            { "capsule_starter", 1.99m },
            { "capsule_premium", 2.99m }
        };

        public void Initialize()
        {
            Debug.Log("[StandaloneStoreAdapter] Initializing standalone store (TEST MODE)...");

            productCache = new Dictionary<string, PurchaseGateway.IapProduct>();
            ownedProducts = new List<string>();

            // Load test products
            LoadTestProducts();

            isInitialized = true;

            Debug.Log("[StandaloneStoreAdapter] Initialized with test products");
            OnInitialized?.Invoke(new List<PurchaseGateway.IapProduct>(productCache.Values));
        }

        private void LoadTestProducts()
        {
            string[] productIds = new string[]
            {
                "diamonds_100",
                "diamonds_500",
                "diamonds_1000",
                "diamonds_2500",
                "diamonds_5000",
                "seasonpass_s1",
                "capsule_starter",
                "capsule_premium"
            };

            foreach (string productId in productIds)
            {
                var product = new PurchaseGateway.IapProduct
                {
                    productId = productId,
                    title = GetProductTitle(productId),
                    description = GetProductDescription(productId),
                    priceDecimal = testPrices.ContainsKey(productId) ? testPrices[productId] : 0.99m,
                    localizedPrice = testPrices.ContainsKey(productId) ? $"${testPrices[productId]:F2}" : "$0.99",
                    currencyCode = "USD",
                    type = GetProductType(productId)
                };

                productCache[productId] = product;

                Debug.Log($"[StandaloneStoreAdapter] Test product: {productId} - {product.localizedPrice}");
            }
        }

        private string GetProductTitle(string productId)
        {
            switch (productId)
            {
                case "diamonds_100": return "100 Diamonds (TEST)";
                case "diamonds_500": return "500 Diamonds (TEST)";
                case "diamonds_1000": return "1,000 Diamonds (TEST)";
                case "diamonds_2500": return "2,500 Diamonds (TEST)";
                case "diamonds_5000": return "5,000 Diamonds (TEST)";
                case "seasonpass_s1": return "Season Pass - Season 1 (TEST)";
                case "capsule_starter": return "Starter Cosmetic Capsule (TEST)";
                case "capsule_premium": return "Premium Cosmetic Capsule (TEST)";
                default: return $"{productId} (TEST)";
            }
        }

        private string GetProductDescription(string productId)
        {
            switch (productId)
            {
                case "diamonds_100": return "TEST: Small stack of premium diamonds";
                case "diamonds_500": return "TEST: Medium stack of premium diamonds";
                case "diamonds_1000": return "TEST: Large stack of premium diamonds";
                case "diamonds_2500": return "TEST: Huge stack of premium diamonds";
                case "diamonds_5000": return "TEST: Massive hoard of premium diamonds";
                case "seasonpass_s1": return "TEST: Premium Season Pass";
                case "capsule_starter": return "TEST: Contains 3 random cosmetics";
                case "capsule_premium": return "TEST: Contains 5 random cosmetics";
                default: return "TEST PRODUCT";
            }
        }

        private PurchaseGateway.IapProduct.ProductType GetProductType(string productId)
        {
            if (productId.Contains("seasonpass"))
            {
                return PurchaseGateway.IapProduct.ProductType.Subscription;
            }
            else
            {
                return PurchaseGateway.IapProduct.ProductType.Consumable;
            }
        }

        public void PurchaseProduct(string productId)
        {
            if (!isInitialized)
            {
                Debug.LogError("[StandaloneStoreAdapter] Store not initialized!");
                OnPurchaseFailed?.Invoke(productId, "Store not initialized");
                return;
            }

            if (!productCache.ContainsKey(productId))
            {
                Debug.LogError($"[StandaloneStoreAdapter] Product not found: {productId}");
                OnPurchaseFailed?.Invoke(productId, "Product not found");
                return;
            }

            Debug.Log($"[StandaloneStoreAdapter] Simulating purchase: {productId}");

            // Simulate purchase delay
            StartCoroutine(SimulatePurchaseCoroutine(productId));
        }

        private System.Collections.IEnumerator SimulatePurchaseCoroutine(string productId)
        {
            yield return new WaitForSeconds(purchaseDelay);

            // Simulate random failures if enabled
            if (simulateFailures && UnityEngine.Random.value < failureRate)
            {
                Debug.LogWarning($"[StandaloneStoreAdapter] Simulated purchase failure: {productId}");
                OnPurchaseFailed?.Invoke(productId, "Simulated failure (testing)");
                yield break;
            }

            // Generate test receipt
            string receipt = GenerateTestReceipt(productId);

            // Add to owned products
            if (!ownedProducts.Contains(productId))
            {
                ownedProducts.Add(productId);
            }

            Debug.Log($"[StandaloneStoreAdapter] Purchase complete: {productId}");
            OnPurchaseComplete?.Invoke(productId, receipt);
        }

        private string GenerateTestReceipt(string productId)
        {
            // Generate fake receipt for testing
            var receiptData = new
            {
                productId = productId,
                transactionId = Guid.NewGuid().ToString(),
                purchaseDate = DateTime.UtcNow.ToString("o"),
                platform = "standalone",
                test = true
            };

            return JsonUtility.ToJson(receiptData);
        }

        public void RestorePurchases(Action<bool, List<string>> onComplete)
        {
            if (!isInitialized)
            {
                Debug.LogError("[StandaloneStoreAdapter] Store not initialized!");
                onComplete?.Invoke(false, null);
                return;
            }

            Debug.Log($"[StandaloneStoreAdapter] Restoring {ownedProducts.Count} purchases");
            onComplete?.Invoke(true, new List<string>(ownedProducts));
        }

        /// <summary>
        /// Test utility: Grant product for free (development only)
        /// </summary>
        public void GrantTestProduct(string productId)
        {
            if (productCache.ContainsKey(productId))
            {
                string receipt = GenerateTestReceipt(productId);
                OnPurchaseComplete?.Invoke(productId, receipt);
                Debug.Log($"[StandaloneStoreAdapter] TEST: Granted free product: {productId}");
            }
        }

        /// <summary>
        /// Test utility: Clear all owned products
        /// </summary>
        public void ClearOwnedProducts()
        {
            ownedProducts.Clear();
            Debug.Log("[StandaloneStoreAdapter] TEST: Cleared all owned products");
        }
    }
}
