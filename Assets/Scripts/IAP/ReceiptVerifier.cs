// ═══════════════════════════════════════════════════════════════════════════════
// INFINITY HOUSE - Made by Mate Makovics
// Receipt Verification Service
// ═══════════════════════════════════════════════════════════════════════════════

using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Threading.Tasks;

namespace InfinityHouse.IAP
{
    /// <summary>
    /// Server-side receipt verification service.
    /// Validates receipts with Apple/Google/Steam to prevent fraud.
    /// Required for production IAP compliance.
    /// </summary>
    public class ReceiptVerifier : MonoBehaviour
    {
        [Header("Verification Settings")]
        [SerializeField] private bool requireVerification = true;
        [SerializeField] private int timeoutSeconds = 10;
        [SerializeField] private int maxRetries = 3;

        private string verificationUrl;

        /// <summary>
        /// Sets the server verification URL
        /// </summary>
        public void SetVerificationUrl(string url)
        {
            verificationUrl = url;
            Debug.Log($"[ReceiptVerifier] Verification URL set: {url}");
        }

        /// <summary>
        /// Verifies a receipt with the backend server
        /// </summary>
        public async Task<bool> VerifyReceipt(string receipt, PurchaseGateway.StorePlatform platform)
        {
            if (!requireVerification)
            {
                Debug.LogWarning("[ReceiptVerifier] Verification disabled - INSECURE FOR PRODUCTION!");
                return true;
            }

            if (string.IsNullOrEmpty(verificationUrl))
            {
                Debug.LogError("[ReceiptVerifier] No verification URL set!");
                return false;
            }

            Debug.Log($"[ReceiptVerifier] Verifying {platform} receipt...");

            int attempts = 0;
            while (attempts < maxRetries)
            {
                attempts++;

                try
                {
                    bool result = await SendVerificationRequest(receipt, platform);
                    if (result)
                    {
                        Debug.Log("[ReceiptVerifier] Receipt verified successfully");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[ReceiptVerifier] Verification attempt {attempts} failed: {ex.Message}");
                }

                if (attempts < maxRetries)
                {
                    await Task.Delay(1000 * attempts); // Exponential backoff
                }
            }

            Debug.LogError("[ReceiptVerifier] Receipt verification failed after all retries");
            return false;
        }

        private async Task<bool> SendVerificationRequest(string receipt, PurchaseGateway.StorePlatform platform)
        {
            // Prepare request payload
            var payload = new VerificationRequest
            {
                receipt = receipt,
                platform = platform.ToString(),
                deviceId = SystemInfo.deviceUniqueIdentifier,
                timestamp = DateTime.UtcNow.ToString("o")
            };

            string jsonPayload = JsonUtility.ToJson(payload);

            // Create web request
            using (UnityWebRequest request = new UnityWebRequest(verificationUrl, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = timeoutSeconds;

                // Send request
                var operation = request.SendWebRequest();

                // Wait for completion
                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                // Check result
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string responseText = request.downloadHandler.text;
                    var response = JsonUtility.FromJson<VerificationResponse>(responseText);

                    if (response.valid)
                    {
                        Debug.Log($"[ReceiptVerifier] Receipt valid: {response.productId}");
                        return true;
                    }
                    else
                    {
                        Debug.LogWarning($"[ReceiptVerifier] Invalid receipt: {response.reason}");
                        return false;
                    }
                }
                else
                {
                    Debug.LogError($"[ReceiptVerifier] Request failed: {request.error}");
                    throw new Exception(request.error);
                }
            }
        }

        /// <summary>
        /// Verifies a receipt locally (fallback for offline mode)
        /// WARNING: Less secure than server-side verification
        /// </summary>
        public bool VerifyReceiptLocally(string receipt)
        {
            Debug.LogWarning("[ReceiptVerifier] Using LOCAL verification - not recommended for production!");

            if (string.IsNullOrEmpty(receipt))
            {
                return false;
            }

            try
            {
                // Basic validation: Check if receipt is valid JSON
                var testParse = JsonUtility.FromJson<ReceiptData>(receipt);
                return testParse != null;
            }
            catch
            {
                return false;
            }
        }

        #region Data Structures

        [Serializable]
        private class VerificationRequest
        {
            public string receipt;
            public string platform;
            public string deviceId;
            public string timestamp;
        }

        [Serializable]
        private class VerificationResponse
        {
            public bool valid;
            public string productId;
            public string transactionId;
            public string reason;
        }

        [Serializable]
        private class ReceiptData
        {
            public string transactionId;
            public string productId;
            public string purchaseDate;
        }

        #endregion
    }
}
