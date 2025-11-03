// ═══════════════════════════════════════════════════════════════════════════════
// INFINITY HOUSE v5.5 - Made by Mate Makovics
// Shard-Line Procedural Spawning System
// ═══════════════════════════════════════════════════════════════════════════════

using UnityEngine;
using System;
using System.Collections.Generic;

namespace InfinityHouse.Performance
{
    /// <summary>
    /// Procedural shard path spawning (line/wave/stairs/cluster/gap).
    /// Pooled, in-view spawning, off-screen despawn.
    /// Target: 40-70% pickup rate, <1.2s spawn→pickup, <0.5ms frame impact.
    /// </summary>
    public class ShardLineSystem : MonoBehaviour
    {
        public static ShardLineSystem Instance { get; private set; }

        [Header("Spawn Configuration")]
        [SerializeField] private GameObject shardPrefab;
        [SerializeField] private float spawnDistanceAhead = 20f;
        [SerializeField] private float despawnDistanceBehind = 10f;

        [Header("Pattern Weights")]
        [SerializeField] private float lineWeight = 0.3f;
        [SerializeField] private float waveWeight = 0.25f;
        [SerializeField] private float stairsWeight = 0.2f;
        [SerializeField] private float clusterWeight = 0.15f;
        [SerializeField] private float gapWeight = 0.1f;

        [Header("Rates & Limits")]
        [SerializeField] private int baseShardsPerChunk = 15;
        [SerializeField] private int maxShardsPerChunk = 30;
        [SerializeField] private float perfectRunBonus = 2f;
        [SerializeField] private float weekendBonus = 0.5f;

        private List<GameObject> activeShards;
        private Transform playerTransform;
        private float lastSpawnX;
        private int shardsSpawnedThisChunk;

        public enum PatternType { Line, Wave, Stairs, Cluster, Gap }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            activeShards = new List<GameObject>();
        }

        private void Start()
        {
            if (PerformanceManager.Instance != null)
                PerformanceManager.Instance.OnLogicTick += UpdateShardSpawning;
        }

        private void UpdateShardSpawning()
        {
            if (playerTransform == null) return;

            float playerX = playerTransform.position.x;

            // Spawn ahead
            if (playerX + spawnDistanceAhead > lastSpawnX)
            {
                SpawnShardPattern();
            }

            // Despawn behind
            DespawnOutOfViewShards(playerX);
        }

        private void SpawnShardPattern()
        {
            if (shardsSpawnedThisChunk >= maxShardsPerChunk) return;

            PatternType pattern = RollPattern();
            int shardCount = GetShardCountForPattern(pattern);

            Vector3 spawnPos = new Vector3(lastSpawnX + 5f, 2f, 0f);

            switch (pattern)
            {
                case PatternType.Line:
                    SpawnLine(spawnPos, shardCount);
                    break;
                case PatternType.Wave:
                    SpawnWave(spawnPos, shardCount);
                    break;
                case PatternType.Stairs:
                    SpawnStairs(spawnPos, shardCount);
                    break;
                case PatternType.Cluster:
                    SpawnCluster(spawnPos, shardCount);
                    break;
                case PatternType.Gap:
                    // Skip spawning (gap)
                    break;
            }

            shardsSpawnedThisChunk += shardCount;
            lastSpawnX += 10f;
        }

        private PatternType RollPattern()
        {
            float roll = UnityEngine.Random.value;
            float cumulative = 0f;

            if ((cumulative += lineWeight) >= roll) return PatternType.Line;
            if ((cumulative += waveWeight) >= roll) return PatternType.Wave;
            if ((cumulative += stairsWeight) >= roll) return PatternType.Stairs;
            if ((cumulative += clusterWeight) >= roll) return PatternType.Cluster;
            return PatternType.Gap;
        }

        private int GetShardCountForPattern(PatternType pattern)
        {
            switch (pattern)
            {
                case PatternType.Line: return 8;
                case PatternType.Wave: return 10;
                case PatternType.Stairs: return 6;
                case PatternType.Cluster: return 12;
                default: return 0;
            }
        }

        private void SpawnLine(Vector3 start, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 pos = start + new Vector3(i * 1f, 0f, 0f);
                SpawnShard(pos);
            }
        }

        private void SpawnWave(Vector3 start, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float y = Mathf.Sin(i * 0.5f) * 2f;
                Vector3 pos = start + new Vector3(i * 1f, y, 0f);
                SpawnShard(pos);
            }
        }

        private void SpawnStairs(Vector3 start, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 pos = start + new Vector3(i * 1f, i * 0.5f, 0f);
                SpawnShard(pos);
            }
        }

        private void SpawnCluster(Vector3 center, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 offset = UnityEngine.Random.insideUnitCircle * 2f;
                Vector3 pos = center + new Vector3(offset.x, offset.y, 0f);
                SpawnShard(pos);
            }
        }

        private void SpawnShard(Vector3 position)
        {
            GameObject shard = ObjectPoolManager.Instance.Get("shard", position, Quaternion.identity);
            if (shard != null) activeShards.Add(shard);
        }

        private void DespawnOutOfViewShards(float playerX)
        {
            for (int i = activeShards.Count - 1; i >= 0; i--)
            {
                if (activeShards[i].transform.position.x < playerX - despawnDistanceBehind)
                {
                    ObjectPoolManager.Instance.Return(activeShards[i]);
                    activeShards.RemoveAt(i);
                }
            }
        }

        public void SetPlayerTransform(Transform player) => playerTransform = player;
        public void ResetChunk() { shardsSpawnedThisChunk = 0; lastSpawnX = 0f; }
    }
}
