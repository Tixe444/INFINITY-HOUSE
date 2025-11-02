using UnityEngine;
using System.Collections;

namespace InfiniteHaus.Hazards
{
    /// <summary>
    /// Crumbling platform - Falls after player steps on it.
    /// Provides brief warning before collapsing.
    /// </summary>
    public class CrumblingPlatform : HazardBase
    {
        [Header("Crumble Settings")]
        [Tooltip("Delay before platform starts falling (seconds)")]
        [SerializeField] private float crumbleDelay = 0.3f;

        [Tooltip("Fall speed")]
        [SerializeField] private float fallSpeed = 5f;

        [Tooltip("Visual shake intensity during warning")]
        [SerializeField] private float shakeIntensity = 0.1f;

        [Tooltip("Destroy platform after falling this distance")]
        [SerializeField] private float destroyDistance = 10f;

        [Header("Respawn")]
        [Tooltip("Does platform respawn?")]
        [SerializeField] private bool canRespawn = false;

        [Tooltip("Respawn delay (seconds)")]
        [SerializeField] private float respawnDelay = 5f;

        private bool isCrumbling = false;
        private bool hasFallen = false;
        private Vector3 originalPosition;
        private Rigidbody2D rb;

        protected override void Awake()
        {
            base.Awake();
            rb = GetComponent<Rigidbody2D>();

            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody2D>();
            }

            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;

            originalPosition = transform.position;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!isActive || isCrumbling || hasFallen) return;

            if (collision.gameObject.CompareTag("Player"))
            {
                StartCoroutine(CrumbleSequence());
            }
        }

        /// <summary>
        /// Executes crumble sequence: shake → fall → destroy
        /// </summary>
        private IEnumerator CrumbleSequence()
        {
            isCrumbling = true;

            // Shake warning
            yield return StartCoroutine(ShakeWarning());

            // Start falling
            hasFallen = true;
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = fallSpeed;

            // Disable collision after falling starts
            if (hazardCollider != null)
            {
                hazardCollider.enabled = false;
            }

            // Wait until platform has fallen enough, then destroy or respawn
            yield return new WaitForSeconds(destroyDistance / fallSpeed);

            if (canRespawn)
            {
                yield return new WaitForSeconds(respawnDelay);
                Respawn();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Shake effect before falling
        /// </summary>
        private IEnumerator ShakeWarning()
        {
            float elapsed = 0f;
            Vector3 startPos = transform.position;

            while (elapsed < crumbleDelay)
            {
                float offsetX = Random.Range(-shakeIntensity, shakeIntensity);
                float offsetY = Random.Range(-shakeIntensity, shakeIntensity);
                transform.position = startPos + new Vector3(offsetX, offsetY, 0);

                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.position = startPos;
        }

        /// <summary>
        /// Respawns platform at original position
        /// </summary>
        private void Respawn()
        {
            transform.position = originalPosition;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.velocity = Vector2.zero;

            if (hazardCollider != null)
            {
                hazardCollider.enabled = true;
            }

            isCrumbling = false;
            hasFallen = false;
            isActive = true;
        }

        public override void Reset()
        {
            base.Reset();
            StopAllCoroutines();
            Respawn();
        }
    }
}
