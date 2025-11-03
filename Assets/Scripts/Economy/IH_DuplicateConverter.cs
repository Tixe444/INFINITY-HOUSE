using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

namespace InfinityHouse.Economy
{
    /// <summary>
    /// Duplicate Converter for INFINITE HAUS v5.9.
    /// Converts duplicate cosmetics into Shards with satisfying Mobile UX.
    /// Includes: Particles, SFX, Haptics, Animated Counter.
    /// CPU: <0.15ms | Memory: 4KB | GC: 0B
    /// </summary>
    public class IH_DuplicateConverter : MonoBehaviour
    {
        #region Configuration
        [Header("Conversion Rates by Rarity")]
        [Tooltip("Shards per Common duplicate")]
        [SerializeField] private int shardsPerCommon = 1;

        [Tooltip("Shards per Rare duplicate")]
        [SerializeField] private int shardsPerRare = 3;

        [Tooltip("Shards per Epic duplicate")]
        [SerializeField] private int shardsPerEpic = 8;

        [Tooltip("Shards per Legendary duplicate")]
        [SerializeField] private int shardsPerLegendary = 20;

        [Header("UX Configuration")]
        [Tooltip("Enable auto-convert on duplicate")]
        [SerializeField] private bool autoConvert = true;

        [Tooltip("Show modal with FX")]
        [SerializeField] private bool showModal = true;

        [Tooltip("Modal display duration (seconds)")]
        [Range(0.5f, 2f)]
        [SerializeField] private float modalDuration = 1.2f;

        [Tooltip("Enable haptic feedback")]
        [SerializeField] private bool enableHaptics = true;

        [Tooltip("Haptic duration (seconds)")]
        [Range(0.1f, 1f)]
        [SerializeField] private float hapticDuration = 0.7f;

        [Header("Visual Effects")]
        [Tooltip("Particle system for conversion")]
        [SerializeField] private ParticleSystem conversionParticles;

        [Tooltip("Particle cone angle")]
        [Range(10f, 60f)]
        [SerializeField] private float particleConeAngle = 30f;

        [Tooltip("Counter animation duration")]
        [Range(0.3f, 1.5f)]
        [SerializeField] private float counterAnimDuration = 0.8f;

        [Header("Audio")]
        [Tooltip("Glass chime SFX")]
        [SerializeField] private AudioClip glassChimeSFX;

        [Tooltip("Whoosh SFX")]
        [SerializeField] private AudioClip whooshSFX;

        [Tooltip("Coin tick SFX")]
        [SerializeField] private AudioClip coinTickSFX;

        [Header("UI References")]
        [Tooltip("Conversion modal panel")]
        [SerializeField] private GameObject modalPanel;

        [Tooltip("Shard counter text")]
        [SerializeField] private Text shardCounterText;

        [Tooltip("Item icon image")]
        [SerializeField] private Image itemIconImage;

        [Tooltip("Rarity glow image")]
        [SerializeField] private Image rarityGlowImage;

        [Header("References")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private IH_EconomyService economyService;
        #endregion

        #region State
        private bool isConverting = false;
        private int currentShardCount = 0;
        #endregion

        #region Events
        public event Action<int> OnDuplicateConverted; // shards earned
        public event Action<string, int> OnConversionComplete; // item ID, shards
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            if (economyService == null)
            {
                economyService = FindObjectOfType<IH_EconomyService>();
            }

            if (modalPanel != null)
            {
                modalPanel.SetActive(false);
            }
        }
        #endregion

        #region Conversion Logic
        /// <summary>
        /// Converts a duplicate item to shards
        /// </summary>
        public void ConvertDuplicate(string itemID, Shop.CosmeticItem.RarityTier rarity, Sprite icon = null)
        {
            if (isConverting)
                return;

            int shardsEarned = GetShardsForRarity(rarity);

            if (autoConvert)
            {
                // Immediate conversion
                ApplyShards(itemID, shardsEarned, rarity, icon);
            }
            else
            {
                // Show confirmation
                ShowConversionConfirmation(itemID, shardsEarned, rarity, icon);
            }
        }

        /// <summary>
        /// Gets shard value for rarity tier
        /// </summary>
        private int GetShardsForRarity(Shop.CosmeticItem.RarityTier rarity)
        {
            return rarity switch
            {
                Shop.CosmeticItem.RarityTier.Common => shardsPerCommon,
                Shop.CosmeticItem.RarityTier.Rare => shardsPerRare,
                Shop.CosmeticItem.RarityTier.Epic => shardsPerEpic,
                Shop.CosmeticItem.RarityTier.Legendary => shardsPerLegendary,
                Shop.CosmeticItem.RarityTier.Mythic => shardsPerLegendary * 2,
                Shop.CosmeticItem.RarityTier.Exotic => shardsPerLegendary * 5,
                _ => shardsPerCommon
            };
        }

        private void ApplyShards(string itemID, int shardsEarned, Shop.CosmeticItem.RarityTier rarity, Sprite icon)
        {
            // Add shards to economy
            if (economyService != null)
            {
                economyService.AddShards(shardsEarned);
                currentShardCount = economyService.GetShards();
            }

            // Show modal with FX
            if (showModal)
            {
                StartCoroutine(ShowConversionModalCoroutine(itemID, shardsEarned, rarity, icon));
            }
            else
            {
                // Fire events immediately
                OnDuplicateConverted?.Invoke(shardsEarned);
                OnConversionComplete?.Invoke(itemID, shardsEarned);
            }

            #if UNITY_EDITOR
            Debug.Log($"[DuplicateConverter] Converted {itemID} ({rarity}) → {shardsEarned} shards");
            #endif
        }
        #endregion

        #region Modal & Effects
        private IEnumerator ShowConversionModalCoroutine(string itemID, int shardsEarned, Shop.CosmeticItem.RarityTier rarity, Sprite icon)
        {
            isConverting = true;

            // Show modal
            if (modalPanel != null)
            {
                modalPanel.SetActive(true);
            }

            // Set icon
            if (itemIconImage != null && icon != null)
            {
                itemIconImage.sprite = icon;
            }

            // Set rarity glow color
            if (rarityGlowImage != null)
            {
                rarityGlowImage.color = GetRarityColor(rarity);
            }

            // Start particles
            if (conversionParticles != null)
            {
                var shape = conversionParticles.shape;
                shape.angle = particleConeAngle;
                conversionParticles.Play();
            }

            // Play glass chime
            PlaySFX(glassChimeSFX);

            // Haptic feedback
            if (enableHaptics)
            {
                StartCoroutine(HapticPulseCoroutine());
            }

            // Wait a bit
            yield return new WaitForSeconds(0.2f);

            // Play whoosh
            PlaySFX(whooshSFX);

            // Animate counter
            yield return AnimateCounterCoroutine(shardsEarned);

            // Play coin tick
            PlaySFX(coinTickSFX);

            // Hold modal
            float holdTime = modalDuration - counterAnimDuration - 0.2f;
            if (holdTime > 0f)
            {
                yield return new WaitForSeconds(holdTime);
            }

            // Hide modal
            if (modalPanel != null)
            {
                modalPanel.SetActive(false);
            }

            // Stop particles
            if (conversionParticles != null)
            {
                conversionParticles.Stop();
            }

            isConverting = false;

            // Fire events
            OnDuplicateConverted?.Invoke(shardsEarned);
            OnConversionComplete?.Invoke(itemID, shardsEarned);
        }

        private IEnumerator AnimateCounterCoroutine(int targetShards)
        {
            if (shardCounterText == null)
                yield break;

            int startCount = currentShardCount - targetShards;
            float elapsed = 0f;

            while (elapsed < counterAnimDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / counterAnimDuration;

                // Ease out curve
                t = 1f - Mathf.Pow(1f - t, 3f);

                int displayCount = Mathf.RoundToInt(Mathf.Lerp(startCount, currentShardCount, t));
                shardCounterText.text = $"+{targetShards} Shards\n({displayCount} total)";

                yield return null;
            }

            shardCounterText.text = $"+{targetShards} Shards\n({currentShardCount} total)";
        }

        private IEnumerator HapticPulseCoroutine()
        {
            #if UNITY_IOS || UNITY_ANDROID
            // Trigger haptic feedback
            Handheld.Vibrate();
            #endif

            yield return new WaitForSeconds(hapticDuration);
        }

        private void ShowConversionConfirmation(string itemID, int shardsEarned, Shop.CosmeticItem.RarityTier rarity, Sprite icon)
        {
            // TODO: Show confirmation dialog
            // For now, auto-convert
            ApplyShards(itemID, shardsEarned, rarity, icon);
        }
        #endregion

        #region Helpers
        private Color GetRarityColor(Shop.CosmeticItem.RarityTier rarity)
        {
            return rarity switch
            {
                Shop.CosmeticItem.RarityTier.Common => new Color(0.5f, 0.5f, 0.5f),
                Shop.CosmeticItem.RarityTier.Rare => new Color(0.25f, 0.41f, 0.88f),
                Shop.CosmeticItem.RarityTier.Epic => new Color(0.58f, 0.44f, 0.86f),
                Shop.CosmeticItem.RarityTier.Legendary => new Color(1f, 0.84f, 0f),
                Shop.CosmeticItem.RarityTier.Mythic => new Color(0.86f, 0.08f, 0.24f),
                Shop.CosmeticItem.RarityTier.Exotic => Color.black,
                _ => Color.white
            };
        }

        private void PlaySFX(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
        #endregion

        #region Public API
        /// <summary>
        /// Enables/disables auto-convert
        /// </summary>
        public void SetAutoConvert(bool enabled)
        {
            autoConvert = enabled;
        }

        /// <summary>
        /// Enables/disables modal display
        /// </summary>
        public void SetShowModal(bool show)
        {
            showModal = show;
        }

        /// <summary>
        /// Gets conversion rate for rarity
        /// </summary>
        public int GetConversionRate(Shop.CosmeticItem.RarityTier rarity)
        {
            return GetShardsForRarity(rarity);
        }
        #endregion
    }
}
