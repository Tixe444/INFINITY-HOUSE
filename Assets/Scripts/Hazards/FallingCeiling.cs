using UnityEngine;
using System.Collections;

namespace InfiniteHaus.Hazards
{
    /// <summary>
    /// Falling ceiling hazard - Triggered when player enters trigger zone.
    /// Falls from ceiling and deals damage on impact.
    /// </summary>
    public class FallingCeiling : HazardBase
    {
        [Header("Falling Settings")]
        [Tooltip("Fall speed")]
        [SerializeField] private float fallSpeed = 10f;

        [Tooltip("Warning duration before falling (seconds)")]
        [SerializeField] private float warningDuration = 0.5f;

        [Tooltip("Distance to fall before stopping")]
        [SerializeField] private float fallDistance = 5f;

        [Tooltip("Flash color during warning")]
        [SerializeField] private Color warningColor = Color.red;

        [Header("Trigger Zone")]
        [Tooltip("Trigger collider that activates ceiling")]
        [SerializeField] private Collider2D triggerZone;

        private bool hasTriggered = false;
        private bool isFalling = false;
        private Vector3 startPosition;
        private Color originalColor;

        protected override void Awake()
        {
            base.Awake();
            startPosition = transform.position;

            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
            }

            // Setup trigger zone
            if (triggerZone == null)
            {
                GameObject triggerObj = new GameObject("TriggerZone");
                triggerObj.transform.parent = transform;
                triggerObj.transform.localPosition = Vector3.down * (fallDistance / 2f);

                triggerZone = triggerObj.AddComponent<BoxCollider2D>();
                triggerZone.isTrigger = true;
                ((BoxCollider2D)triggerZone).size = new Vector2(2f, fallDistance);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!isActive || hasTriggered || isFalling) return;

            if (other.CompareTag("Player"))
            {
                StartCoroutine(FallSequence());
            }
        }

        /// <summary>
        /// Executes falling sequence: warning → fall → damage
        /// </summary>
        private IEnumerator FallSequence()
        {
            hasTriggered = true;

            // Warning flash
            yield return StartCoroutine(WarningFlash());

            // Fall
            isFalling = true;
            Vector3 targetPosition = startPosition + Vector3.down * fallDistance;

            while (Vector3.Distance(transform.position, targetPosition) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetPosition,
                    fallSpeed * Time.deltaTime
                );
                yield return null;
            }

            transform.position = targetPosition;
            isFalling = false;

            PlayEffects();
        }

        /// <summary>
        /// Warning flash before falling
        /// </summary>
        private IEnumerator WarningFlash()
        {
            if (spriteRenderer == null) yield break;

            float elapsed = 0f;
            bool isWarningColor = false;

            while (elapsed < warningDuration)
            {
                spriteRenderer.color = isWarningColor ? warningColor : originalColor;
                isWarningColor = !isWarningColor;

                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }

            spriteRenderer.color = originalColor;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!isFalling) return;

            if (collision.gameObject.CompareTag("Player"))
            {
                TriggerHazard(collision.gameObject);
            }
        }

        public override void Reset()
        {
            base.Reset();
            StopAllCoroutines();

            transform.position = startPosition;
            hasTriggered = false;
            isFalling = false;

            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
            }
        }
    }
}
