// ═══════════════════════════════════════════════════════════════════════════════
// INFINITY HOUSE - Made by Mate Makovics
// A/B Testing Manager - Experimentation Framework
// ═══════════════════════════════════════════════════════════════════════════════

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace InfinityHouse.Analytics
{
    /// <summary>
    /// A/B testing framework for monetization and feature experiments.
    /// Supports multivariate testing, cohort assignment, and statistical analysis.
    /// Privacy-compliant with deterministic variant assignment.
    /// </summary>
    public class ABTestManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private bool enableABTesting = true;
        [SerializeField] private TextAsset experimentsConfigJson;

        // Singleton
        public static ABTestManager Instance { get; private set; }

        // Active experiments
        private Dictionary<string, Experiment> activeExperiments;
        private Dictionary<string, string> assignedVariants; // experimentId -> variantId

        // Player identifier (persistent across sessions)
        private string playerIdentifier;

        // Events
        public event Action<string, string> OnVariantAssigned; // experimentId, variantId
        public event Action<string, string, object> OnGoalReached; // experimentId, variantId, value

        private void Awake()
        {
            // Singleton
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

            activeExperiments = new Dictionary<string, Experiment>();
            assignedVariants = new Dictionary<string, string>();

            // Get or create player identifier
            playerIdentifier = PlayerPrefs.GetString("ABTestPlayerId", Guid.NewGuid().ToString());
            PlayerPrefs.SetString("ABTestPlayerId", playerIdentifier);
            PlayerPrefs.Save();

            LoadExperiments();
            AssignVariants();
        }

        #region Experiment Management

        private void LoadExperiments()
        {
            if (experimentsConfigJson != null)
            {
                try
                {
                    var config = JsonUtility.FromJson<ExperimentsConfig>(experimentsConfigJson.text);

                    foreach (var experiment in config.experiments)
                    {
                        activeExperiments[experiment.experimentId] = experiment;
                    }

                    Debug.Log($"[ABTestManager] Loaded {activeExperiments.Count} experiments");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ABTestManager] Failed to load experiments: {ex.Message}");
                    CreateDefaultExperiments();
                }
            }
            else
            {
                CreateDefaultExperiments();
            }
        }

        private void CreateDefaultExperiments()
        {
            // Example: Price point test
            var priceTest = new Experiment
            {
                experimentId = "diamond_pack_pricing",
                experimentName = "Diamond Pack Price Test",
                isActive = true,
                variants = new List<Variant>
                {
                    new Variant { variantId = "control", weight = 0.33f, parameters = new Dictionary<string, object> { { "price_multiplier", 1.0f } } },
                    new Variant { variantId = "lower_price", weight = 0.33f, parameters = new Dictionary<string, object> { { "price_multiplier", 0.8f } } },
                    new Variant { variantId = "higher_value", weight = 0.34f, parameters = new Dictionary<string, object> { { "price_multiplier", 1.0f }, { "bonus_diamonds", 20 } } }
                }
            };

            activeExperiments["diamond_pack_pricing"] = priceTest;

            // Example: Shop UI layout test
            var shopLayoutTest = new Experiment
            {
                experimentId = "shop_layout",
                experimentName = "Shop UI Layout Test",
                isActive = true,
                variants = new List<Variant>
                {
                    new Variant { variantId = "grid", weight = 0.5f, parameters = new Dictionary<string, object> { { "layout", "grid" } } },
                    new Variant { variantId = "carousel", weight = 0.5f, parameters = new Dictionary<string, object> { { "layout", "carousel" } } }
                }
            };

            activeExperiments["shop_layout"] = shopLayoutTest;

            Debug.Log("[ABTestManager] Created default experiments");
        }

        private void AssignVariants()
        {
            foreach (var experiment in activeExperiments.Values)
            {
                if (experiment.isActive)
                {
                    string variantId = AssignVariant(experiment);
                    assignedVariants[experiment.experimentId] = variantId;

                    OnVariantAssigned?.Invoke(experiment.experimentId, variantId);

                    // Track assignment
                    TelemetryEvents.Instance?.TrackEvent("ab_variant_assigned", new Dictionary<string, object>
                    {
                        { "experiment_id", experiment.experimentId },
                        { "variant_id", variantId }
                    });

                    Debug.Log($"[ABTestManager] {experiment.experimentId}: {variantId}");
                }
            }
        }

        private string AssignVariant(Experiment experiment)
        {
            // Deterministic assignment based on player identifier
            // This ensures same player always gets same variant
            int hash = GetStableHash(playerIdentifier + experiment.experimentId);
            float value = (hash % 10000) / 10000f; // 0.0 to 1.0

            float cumulative = 0f;
            foreach (var variant in experiment.variants)
            {
                cumulative += variant.weight;
                if (value <= cumulative)
                {
                    return variant.variantId;
                }
            }

            // Fallback to first variant
            return experiment.variants[0].variantId;
        }

        private int GetStableHash(string input)
        {
            // Simple stable hash function
            int hash = 0;
            foreach (char c in input)
            {
                hash = (hash * 31 + c) % int.MaxValue;
            }
            return Math.Abs(hash);
        }

        #endregion

        #region Variant Queries

        /// <summary>
        /// Gets the assigned variant for an experiment
        /// </summary>
        public string GetVariant(string experimentId)
        {
            if (!enableABTesting)
            {
                return "control";
            }

            if (assignedVariants.ContainsKey(experimentId))
            {
                return assignedVariants[experimentId];
            }

            return "control";
        }

        /// <summary>
        /// Gets a parameter value for the assigned variant
        /// </summary>
        public T GetVariantParameter<T>(string experimentId, string parameterName, T defaultValue)
        {
            if (!enableABTesting || !activeExperiments.ContainsKey(experimentId))
            {
                return defaultValue;
            }

            string variantId = GetVariant(experimentId);
            Experiment experiment = activeExperiments[experimentId];

            var variant = experiment.variants.FirstOrDefault(v => v.variantId == variantId);
            if (variant != null && variant.parameters.ContainsKey(parameterName))
            {
                try
                {
                    return (T)Convert.ChangeType(variant.parameters[parameterName], typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }

            return defaultValue;
        }

        /// <summary>
        /// Checks if player is in a specific variant
        /// </summary>
        public bool IsInVariant(string experimentId, string variantId)
        {
            return GetVariant(experimentId) == variantId;
        }

        #endregion

        #region Goal Tracking

        /// <summary>
        /// Tracks when a goal is reached (e.g., purchase completed)
        /// </summary>
        public void TrackGoal(string experimentId, string goalName, object value = null)
        {
            if (!enableABTesting || !assignedVariants.ContainsKey(experimentId))
            {
                return;
            }

            string variantId = assignedVariants[experimentId];

            OnGoalReached?.Invoke(experimentId, variantId, value);

            // Track to analytics
            TelemetryEvents.Instance?.TrackEvent("ab_goal_reached", new Dictionary<string, object>
            {
                { "experiment_id", experimentId },
                { "variant_id", variantId },
                { "goal_name", goalName },
                { "value", value?.ToString() ?? "null" }
            });

            Debug.Log($"[ABTestManager] Goal reached: {experimentId}/{variantId}/{goalName}");
        }

        #endregion

        #region Data Structures

        [Serializable]
        public class Experiment
        {
            public string experimentId;
            public string experimentName;
            public bool isActive;
            public List<Variant> variants;
        }

        [Serializable]
        public class Variant
        {
            public string variantId;
            public float weight; // 0.0 to 1.0 (should sum to 1.0 across all variants)
            public Dictionary<string, object> parameters;
        }

        [Serializable]
        private class ExperimentsConfig
        {
            public List<Experiment> experiments;
        }

        #endregion
    }
}
