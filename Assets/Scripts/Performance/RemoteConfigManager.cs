// ═══════════════════════════════════════════════════════════════════════════════
// INFINITY HOUSE v5.5 - Made by Mate Makovics
// Remote Config Manager - Hot-Adjustable Parameters
// ═══════════════════════════════════════════════════════════════════════════════

using UnityEngine;
using System.Collections.Generic;

namespace InfinityHouse.Performance
{
    /// <summary>
    /// Remote configuration for hot-adjustable parameters.
    /// Keys: input.*, hud.*, shardLine.*, settings.*, perf.*, difficulty.*
    /// </summary>
    public class RemoteConfigManager : MonoBehaviour
    {
        public static RemoteConfigManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private bool useRemoteConfig = false;
        [SerializeField] private float fetchIntervalSeconds = 300f;

        // Default values
        private Dictionary<string, object> config;
        private float lastFetchTime;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            InitializeDefaults();
        }

        private void InitializeDefaults()
        {
            config = new Dictionary<string, object>
            {
                // Input
                { "input.swipeDeadZone", 10f },
                { "input.bufferTime", 0.12f },
                { "input.coyoteTime", 0.12f },

                // HUD
                { "hud.shardAnimSpeed", 0.5f },
                { "hud.latencyTracking", true },

                // Shard Line
                { "shardLine.enabled", true },
                { "shardLine.baseShardsPerChunk", 15 },
                { "shardLine.maxPerChunk", 30 },
                { "shardLine.perfectRunBonus", 2f },
                { "shardLine.weekendBonus", 0.5f },

                // Performance
                { "perf.targetFPSMobile", 60 },
                { "perf.targetFPSDesktop", 90 },
                { "perf.maxDeltaTime", 0.033f },

                // Difficulty
                { "difficulty.chaseIncreaseRate", 0.1f },
                { "difficulty.spawnDistanceBase", 20f }
            };
        }

        public T GetValue<T>(string key, T defaultValue)
        {
            if (config.ContainsKey(key))
            {
                try { return (T)config[key]; }
                catch { return defaultValue; }
            }
            return defaultValue;
        }

        public void SetValue(string key, object value)
        {
            config[key] = value;
        }
    }
}
