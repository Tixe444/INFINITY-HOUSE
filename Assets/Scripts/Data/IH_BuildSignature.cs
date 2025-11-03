using UnityEngine;

namespace InfinityHouse.Build
{
    /// <summary>
    /// Build Signature ScriptableObject for INFINITY HOUSE v6.0.
    /// Stores cryptographic signature for build verification.
    /// Embedded in Resources/ for runtime access.
    /// </summary>
    [CreateAssetMenu(fileName = "IH_BuildSignature", menuName = "Infinity House/Build Signature")]
    public class IH_BuildSignature : ScriptableObject
    {
        [Header("Build Signature (HMAC-SHA256)")]
        [Tooltip("64-char hex signature")]
        public string signature;

        [Header("Build Metadata")]
        [Tooltip("Git commit hash")]
        public string commitHash;

        [Tooltip("Unix timestamp")]
        public string buildTimestamp;

        [Header("Project Identity")]
        [Tooltip("Project identifier")]
        public string projectID = "IH2025";

        [Tooltip("Producer identifier")]
        public string producerID = "TixeStudio";

        /// <summary>
        /// Validates signature format
        /// </summary>
        public bool IsValid()
        {
            if (string.IsNullOrEmpty(signature) || signature.Length != 64)
                return false;

            if (string.IsNullOrEmpty(commitHash))
                return false;

            if (string.IsNullOrEmpty(buildTimestamp))
                return false;

            return true;
        }

        /// <summary>
        /// Gets build date from timestamp
        /// </summary>
        public System.DateTime GetBuildDate()
        {
            if (long.TryParse(buildTimestamp, out long timestamp))
            {
                return System.DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
            }
            return System.DateTime.MinValue;
        }
    }
}
