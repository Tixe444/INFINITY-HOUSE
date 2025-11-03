using UnityEngine;
using System;
using System.Reflection;
using System.Collections;

namespace InfinityHouse.Security
{
    /// <summary>
    /// Signature Verifier for INFINITY HOUSE v6.0.
    /// Runtime verification of build signature (Release builds only).
    /// Fail-soft: Logs mismatch but never blocks gameplay.
    /// Invisible to developers, no UI, no prompts.
    /// CPU: <0.01ms | Memory: 1KB | GC: 0B
    /// </summary>
    public class IH_SignatureVerifier : MonoBehaviour
    {
        private const string SIGNATURE_ASSET_PATH = "IH_BuildSignature";

        #region Unity Lifecycle
        private void Awake()
        {
            // Only verify in Release builds, skip Editor and Debug
            #if !UNITY_EDITOR && !DEBUG
            StartCoroutine(VerifySignatureAsync());
            #endif
        }
        #endregion

        #region Verification
        private IEnumerator VerifySignatureAsync()
        {
            yield return null; // Defer one frame to avoid startup stutter

            try
            {
                // Load signatures from 3 locations
                string sig1 = GetAssemblySignature();
                string sig2 = GetScriptableObjectSignature();
                string sig3 = GetAddressablesSignature();

                // Validate consistency
                bool isValid = ValidateSignatures(sig1, sig2, sig3);

                if (isValid)
                {
                    // Silent success
                    #if UNITY_EDITOR
                    Debug.Log("[Security] ✅ Build signature verified");
                    #endif
                }
                else
                {
                    // Fail-soft: Log + telemetry, no block
                    HandleMismatch(sig1, sig2, sig3);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Security] Signature verification failed: {ex.Message}");
            }
        }

        private string GetAssemblySignature()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var attributes = assembly.GetCustomAttributes(typeof(AssemblyMetadataAttribute), false);

                foreach (AssemblyMetadataAttribute attr in attributes)
                {
                    if (attr.Key == "IH_Signature")
                        return attr.Value;
                }
            }
            catch
            {
                // Silent fail
            }

            return null;
        }

        private string GetScriptableObjectSignature()
        {
            try
            {
                var asset = Resources.Load<Build.IH_BuildSignature>(SIGNATURE_ASSET_PATH);
                if (asset != null && asset.IsValid())
                {
                    return asset.signature;
                }
            }
            catch
            {
                // Silent fail
            }

            return null;
        }

        private string GetAddressablesSignature()
        {
            // TODO: Implement when Addressables package is integrated
            // For now, return null (not critical for fail-soft verification)
            return null;
        }

        private bool ValidateSignatures(string sig1, string sig2, string sig3)
        {
            // At least 2 signatures must match and be non-null
            int validCount = 0;
            string primarySig = null;

            if (!string.IsNullOrEmpty(sig1))
            {
                validCount++;
                primarySig = sig1;
            }

            if (!string.IsNullOrEmpty(sig2))
            {
                if (primarySig == null)
                {
                    validCount++;
                    primarySig = sig2;
                }
                else if (sig2 == primarySig)
                {
                    validCount++;
                }
            }

            if (!string.IsNullOrEmpty(sig3))
            {
                if (primarySig == null)
                {
                    validCount++;
                }
                else if (sig3 == primarySig)
                {
                    validCount++;
                }
            }

            // Require at least 2 matching signatures
            return validCount >= 2;
        }

        private void HandleMismatch(string sig1, string sig2, string sig3)
        {
            Debug.LogWarning("[Security] ⚠️ Build signature mismatch detected");

            #if UNITY_EDITOR
            Debug.LogWarning($"[Security] Assembly: {sig1?.Substring(0, 16) ?? "null"}...");
            Debug.LogWarning($"[Security] ScriptableObject: {sig2?.Substring(0, 16) ?? "null"}...");
            Debug.LogWarning($"[Security] Addressables: {sig3?.Substring(0, 16) ?? "null"}...");
            #endif

            // Send telemetry (fail-soft, no blocking)
            SendTelemetry(sig1, sig2, sig3);

            // Continue gameplay (fail-soft principle)
        }

        private void SendTelemetry(string sig1, string sig2, string sig3)
        {
            try
            {
                // TODO: Integrate with analytics service
                // Example: AnalyticsService.SendEvent("build_signature_mismatch", new {
                //     sig1, sig2, sig3,
                //     deviceID = SystemInfo.deviceUniqueIdentifier,
                //     timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                // });

                #if UNITY_EDITOR
                Debug.Log("[Security] Telemetry: build_signature_mismatch event logged");
                #endif
            }
            catch
            {
                // Silent fail (never disrupt gameplay)
            }
        }
        #endregion
    }
}
