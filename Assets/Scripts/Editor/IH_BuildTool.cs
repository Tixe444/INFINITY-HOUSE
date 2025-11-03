using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Diagnostics;

namespace InfinityHouse.Build
{
    /// <summary>
    /// Build Tool for INFINITY HOUSE v6.0 Security Layer.
    /// Generates cryptographic build signatures (HMAC-SHA256) for IP protection.
    /// Invisible to developers, fail-soft verification, no workflow disruption.
    /// </summary>
    public static class IH_BuildTool
    {
        private const string PROJECT_ID = "IH2025";
        private const string PRODUCER_ID = "TixeStudio";
        private const string SECRET_FILE = ".ih_secret";
        private const string ENV_VAR_KEY = "INFINITY_HOUSE_KEY";

        [MenuItem("Infinity House/Generate Build Signature")]
        public static void GenerateBuildSignature()
        {
            try
            {
                // Get build metadata
                string commitHash = GetGitCommitHash();
                string timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

                // Get signing key
                string key = GetSigningKey();
                if (string.IsNullOrEmpty(key))
                {
                    UnityEngine.Debug.LogError("[BuildTool] No signing key found! Set INFINITY_HOUSE_KEY env var or create .ih_secret file.");
                    return;
                }

                // Compute signature
                string payload = $"{commitHash}|{timestamp}|{PROJECT_ID}|{PRODUCER_ID}";
                string signature = ComputeHMAC(payload, key);

                // Embed signature in 3 locations
                EmbedInAssemblyInfo(signature, commitHash, timestamp);
                EmbedInScriptableObject(signature, commitHash, timestamp);
                EmbedInAddressables(signature);

                UnityEngine.Debug.Log($"[BuildTool] ✅ Signature generated: {signature.Substring(0, 16)}...");
                UnityEngine.Debug.Log($"[BuildTool] Commit: {commitHash} | Timestamp: {timestamp}");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[BuildTool] Signature generation failed: {ex.Message}");
            }
        }

        private static string GetGitCommitHash()
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = "rev-parse HEAD",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = Application.dataPath
                    }
                };

                process.Start();
                string hash = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();

                return string.IsNullOrEmpty(hash) ? "unknown" : hash;
            }
            catch
            {
                return "unknown";
            }
        }

        private static string GetSigningKey()
        {
            // Priority 1: Environment variable
            string envKey = Environment.GetEnvironmentVariable(ENV_VAR_KEY);
            if (!string.IsNullOrEmpty(envKey))
                return envKey;

            // Priority 2: Secret file
            string secretPath = Path.Combine(Application.dataPath, "..", SECRET_FILE);
            if (File.Exists(secretPath))
            {
                return File.ReadAllText(secretPath).Trim();
            }

            return null;
        }

        private static string ComputeHMAC(string payload, string key)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key)))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }

        private static void EmbedInAssemblyInfo(string signature, string commit, string timestamp)
        {
            string assemblyPath = Path.Combine(Application.dataPath, "Scripts", "AssemblyInfo.cs");

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("using System.Reflection;");
            sb.AppendLine();
            sb.AppendLine($"[assembly: AssemblyMetadata(\"IH_Signature\", \"{signature}\")]");
            sb.AppendLine($"[assembly: AssemblyMetadata(\"IH_Commit\", \"{commit}\")]");
            sb.AppendLine($"[assembly: AssemblyMetadata(\"IH_Timestamp\", \"{timestamp}\")]");
            sb.AppendLine($"[assembly: AssemblyMetadata(\"IH_Project\", \"{PROJECT_ID}\")]");
            sb.AppendLine($"[assembly: AssemblyMetadata(\"IH_Producer\", \"{PRODUCER_ID}\")]");

            Directory.CreateDirectory(Path.GetDirectoryName(assemblyPath));
            File.WriteAllText(assemblyPath, sb.ToString());

            AssetDatabase.Refresh();
        }

        private static void EmbedInScriptableObject(string signature, string commit, string timestamp)
        {
            string assetPath = "Assets/Resources/IH_BuildSignature.asset";

            var asset = AssetDatabase.LoadAssetAtPath<IH_BuildSignature>(assetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<IH_BuildSignature>();
                Directory.CreateDirectory(Path.GetDirectoryName(assetPath));
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            asset.signature = signature;
            asset.commitHash = commit;
            asset.buildTimestamp = timestamp;
            asset.projectID = PROJECT_ID;
            asset.producerID = PRODUCER_ID;

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }

        private static void EmbedInAddressables(string signature)
        {
            // Note: Addressables injection requires Addressables package
            // For now, we'll just log. Full implementation in IH_AddressablesInjector.cs
            UnityEngine.Debug.Log("[BuildTool] Addressables signature injection ready (requires package)");
        }

        [MenuItem("Infinity House/Generate Signing Key")]
        public static void GenerateSigningKey()
        {
            string secretPath = Path.Combine(Application.dataPath, "..", SECRET_FILE);

            if (File.Exists(secretPath))
            {
                if (!EditorUtility.DisplayDialog("Key Exists",
                    "A signing key already exists. Regenerate?",
                    "Yes", "No"))
                {
                    return;
                }
            }

            // Generate 32-char random key
            byte[] keyBytes = new byte[16];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(keyBytes);
            }
            string key = BitConverter.ToString(keyBytes).Replace("-", "").ToLower();

            File.WriteAllText(secretPath, key);

            UnityEngine.Debug.Log($"[BuildTool] ✅ Signing key generated: {SECRET_FILE}");
            UnityEngine.Debug.Log("[BuildTool] ⚠️ Add .ih_secret to .gitignore!");
        }
    }
}
