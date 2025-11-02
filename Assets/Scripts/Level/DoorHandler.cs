using UnityEngine;
using System;

namespace InfiniteHaus.Level
{
    /// <summary>
    /// Handles door selection at the end of corridor chunks.
    /// Players choose left or right door to determine next corridor.
    /// </summary>
    public class DoorHandler : MonoBehaviour
    {
        [Header("Door Settings")]
        [Tooltip("Door index (0 = left, 1 = right, etc.)")]
        [SerializeField] private int doorIndex = 0;

        [Tooltip("Door type affects next corridor difficulty")]
        [SerializeField] private DoorType doorType = DoorType.Normal;

        [Tooltip("Is this door currently selectable?")]
        [SerializeField] private bool isSelectable = true;

        [Header("Visual Feedback")]
        [Tooltip("Sprite renderer")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Tooltip("Normal door sprite")]
        [SerializeField] private Sprite normalSprite;

        [Tooltip("Highlighted door sprite")]
        [SerializeField] private Sprite highlightSprite;

        [Tooltip("Locked door sprite")]
        [SerializeField] private Sprite lockedSprite;

        [Tooltip("Highlight color")]
        [SerializeField] private Color highlightColor = Color.yellow;

        [Header("Door Indicator")]
        [Tooltip("Visual indicator (arrow, glow, etc.)")]
        [SerializeField] private GameObject selectionIndicator;

        [Header("Audio")]
        [Tooltip("Door selection sound")]
        [SerializeField] private AudioClip selectSound;

        [Tooltip("Door open sound")]
        [SerializeField] private AudioClip openSound;

        [Tooltip("Door locked sound")]
        [SerializeField] private AudioClip lockedSound;

        // State
        private bool isHighlighted = false;
        private bool hasBeenChosen = false;
        private Color originalColor;

        // Events
        public event Action<int, DoorType> OnDoorChosen;

        public enum DoorType
        {
            Normal,      // Standard difficulty progression
            Easy,        // Easier corridor (fewer hazards)
            Hard,        // Harder corridor (more hazards, better rewards)
            Mystery,     // Random difficulty
            Safe,        // No hazards (rare)
            Locked       // Requires key/condition
        }

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
            }

            if (selectionIndicator != null)
            {
                selectionIndicator.SetActive(false);
            }
        }

        private void Start()
        {
            UpdateDoorVisuals();
        }

        /// <summary>
        /// Highlights this door (player is near)
        /// </summary>
        public void Highlight()
        {
            if (!isSelectable || hasBeenChosen) return;

            isHighlighted = true;

            if (spriteRenderer != null)
            {
                if (highlightSprite != null)
                {
                    spriteRenderer.sprite = highlightSprite;
                }
                else
                {
                    spriteRenderer.color = highlightColor;
                }
            }

            if (selectionIndicator != null)
            {
                selectionIndicator.SetActive(true);
            }
        }

        /// <summary>
        /// Removes highlight from this door
        /// </summary>
        public void Unhighlight()
        {
            isHighlighted = false;

            if (spriteRenderer != null)
            {
                if (normalSprite != null)
                {
                    spriteRenderer.sprite = normalSprite;
                }
                else
                {
                    spriteRenderer.color = originalColor;
                }
            }

            if (selectionIndicator != null)
            {
                selectionIndicator.SetActive(false);
            }
        }

        /// <summary>
        /// Chooses this door (player input)
        /// </summary>
        public void Choose()
        {
            if (!isSelectable || hasBeenChosen)
            {
                PlayLockedSound();
                return;
            }

            hasBeenChosen = true;

            // Play sounds
            PlaySelectSound();
            PlayOpenSound();

            // Trigger event
            OnDoorChosen?.Invoke(doorIndex, doorType);

            Debug.Log($"Door {doorIndex} chosen - Type: {doorType}");
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                Highlight();

                // Auto-choose if player passes through
                Choose();
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                Unhighlight();
            }
        }

        /// <summary>
        /// Sets door index
        /// </summary>
        public void SetDoorIndex(int index)
        {
            doorIndex = index;
        }

        /// <summary>
        /// Sets door type
        /// </summary>
        public void SetDoorType(DoorType type)
        {
            doorType = type;
            UpdateDoorVisuals();
        }

        /// <summary>
        /// Locks this door
        /// </summary>
        public void Lock()
        {
            isSelectable = false;
            UpdateDoorVisuals();
        }

        /// <summary>
        /// Unlocks this door
        /// </summary>
        public void Unlock()
        {
            isSelectable = true;
            UpdateDoorVisuals();
        }

        /// <summary>
        /// Updates door visuals based on type and state
        /// </summary>
        private void UpdateDoorVisuals()
        {
            if (spriteRenderer == null) return;

            if (!isSelectable && lockedSprite != null)
            {
                spriteRenderer.sprite = lockedSprite;
            }
            else if (normalSprite != null)
            {
                spriteRenderer.sprite = normalSprite;
            }

            // Color coding by door type
            switch (doorType)
            {
                case DoorType.Easy:
                    spriteRenderer.color = new Color(0.5f, 1f, 0.5f); // Green tint
                    break;
                case DoorType.Hard:
                    spriteRenderer.color = new Color(1f, 0.5f, 0.5f); // Red tint
                    break;
                case DoorType.Mystery:
                    spriteRenderer.color = new Color(0.8f, 0.5f, 1f); // Purple tint
                    break;
                case DoorType.Safe:
                    spriteRenderer.color = new Color(0.5f, 0.8f, 1f); // Blue tint
                    break;
                case DoorType.Locked:
                    spriteRenderer.color = Color.gray;
                    break;
                default:
                    spriteRenderer.color = originalColor;
                    break;
            }
        }

        private void PlaySelectSound()
        {
            if (selectSound != null)
            {
                AudioSource.PlayClipAtPoint(selectSound, transform.position);
            }
        }

        private void PlayOpenSound()
        {
            if (openSound != null)
            {
                AudioSource.PlayClipAtPoint(openSound, transform.position);
            }
        }

        private void PlayLockedSound()
        {
            if (lockedSound != null)
            {
                AudioSource.PlayClipAtPoint(lockedSound, transform.position);
            }
        }

        /// <summary>
        /// Gets door difficulty modifier
        /// </summary>
        public float GetDifficultyModifier()
        {
            switch (doorType)
            {
                case DoorType.Easy: return 0.7f;
                case DoorType.Hard: return 1.5f;
                case DoorType.Mystery: return UnityEngine.Random.Range(0.5f, 2f);
                case DoorType.Safe: return 0f;
                default: return 1f;
            }
        }
    }
}
