using UnityEngine;

public class RockFragment : MonoBehaviour
{
    [Header("Hit Settings")]
    [Tooltip("Fallback hits when Hits Per Level is empty. Reduces linearly by upgrade level.")]
    [SerializeField] private int baseHits = 3;
    [Tooltip("Hits required at each upgrade level. Index 0 = Lv0, 1 = Lv1, etc. " +
             "If set, overrides Base Hits entirely. Levels beyond the array length use the last value.")]
    [SerializeField] private int[] hitsPerLevel = new int[] { 5, 4, 2, 1 };
    [SerializeField] private GameObject ironOrePrefab;
    [SerializeField] private int oreDropCount = 3;
    [Tooltip("Horizontal scatter speed. Higher = ores fly further apart from each other.")]
    [SerializeField] private float oreScatterSpeed = 1.2f;
    [Tooltip("Vertical offset above the rock where ore is spawned.")]
    [SerializeField] private float oreSpawnHeightOffset = 2.0f;
    [Tooltip("Upward burst velocity added on spawn. Small positive value helps each ore arc visibly outward.")]
    [SerializeField] private float oreUpwardBurst = 1.5f;
    [Tooltip("Random radius (metres) around the spawn point so multiple ores don't stack on top of each other.")]
    [SerializeField] private float oreSpawnJitter = 0.15f;

    [Header("Visual Feedback")]
    [SerializeField] private ParticleSystem hitParticles;
    [SerializeField] private AudioClip hitSound;

    [Header("Debris Settings")]
    [SerializeField] private GameObject debrisParticlePrefab; // Reference to Debries prefab
    [SerializeField] private float dropShakeIntensity = 0.04f;
    [SerializeField] private float dropShakeDuration = 0.18f;
    [SerializeField] private bool logRockHits = true;

    private int currentHits;
    private int hitCount = 0;
    private ArchaeologyGameManager gameManager;
    private FeedbackManager feedback;
    private Rigidbody rb;
    private Vector3 _lockedPosition;
    private Quaternion _lockedRotation;
    private bool _positionLocked = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Keep rock fragments in place until they are explicitly hit or destroyed.
            // FreezeAll constraints prevent overlapping fragments from shoving each
            // other apart on scene start, while keeping the Rigidbody dynamic so that
            // OnCollisionEnter still fires when a Kinematic pickaxe (held via
            // OVRGrabbable) strikes the rock. Kinematic-vs-Kinematic collisions would
            // be silently dropped.
            rb.useGravity = false;
            rb.isKinematic = false;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        // Capture the spawn pose as the immutable anchor. LateUpdate will force-clamp
        // the transform back here every frame regardless of what physics does.
        _lockedPosition = transform.position;
        _lockedRotation = transform.rotation;
        _positionLocked = true;

        feedback = Object.FindFirstObjectByType<FeedbackManager>();
    }

    private void LateUpdate()
    {
        // Hard-lock position/rotation so nothing (not even forces that bypass
        // FreezeAll) can displace the rock until it is destroyed via Hit().
        if (_positionLocked)
        {
            if (transform.position != _lockedPosition)
            {
                transform.position = _lockedPosition;
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                }
            }
            if (transform.rotation != _lockedRotation)
            {
                transform.rotation = _lockedRotation;
                if (rb != null)
                {
                    rb.angularVelocity = Vector3.zero;
                }
            }
        }
    }

    public void Initialize(ArchaeologyGameManager manager)
    {
        gameManager = manager;
        currentHits = GetHitsForLevel(0);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Check if this is a pickaxe hit
        if (collision.gameObject.CompareTag("Pickaxe"))
        {
            if (logRockHits)
            {
                Debug.Log($"{nameof(RockFragment)} on {gameObject.name} was hit by {collision.gameObject.name}.");
            }
            Hit();
        }
    }

    public void Hit()
    {
        hitCount++;

        if (logRockHits)
        {
            Debug.Log($"{nameof(RockFragment)} hit count: {hitCount}/{currentHits} on {gameObject.name}.");
        }

        // Play hit feedback
        if (hitParticles != null)
        {
            Instantiate(hitParticles, transform.position, Quaternion.identity);
        }

        if (hitSound != null)
        {
            AudioSource.PlayClipAtPoint(hitSound, transform.position, 0.7f);
        }

        // Check if fragment is destroyed
        if (hitCount >= currentHits)
        {
            Destroy();
        }
    }

    public void OnUpgrade(int upgradeLevel)
    {
        currentHits = GetHitsForLevel(upgradeLevel);
    }

    private int GetHitsForLevel(int level)
    {
        if (hitsPerLevel != null && hitsPerLevel.Length > 0)
        {
            int idx = Mathf.Clamp(level, 0, hitsPerLevel.Length - 1);
            return Mathf.Max(1, hitsPerLevel[idx]);
        }
        // Fallback: linear reduction from baseHits
        return Mathf.Max(1, baseHits - level);
    }

    /// <summary>
    /// Public entry point so other systems (e.g. weak-point colliders) can trigger
    /// the full destruction sequence — ore spawn, debris, manager notification,
    /// and GameObject removal — without re-implementing the logic.
    /// </summary>
    public void TriggerDestruction()
    {
        if (_positionLocked == false)
        {
            // Already in the destruction sequence; avoid double-invoking.
            return;
        }
        Destroy();
    }

    private void Destroy()
    {
        // Release the position lock so anything in the destruction sequence
        // (particles, debris) can behave naturally.
        _positionLocked = false;

        if (logRockHits)
        {
            Debug.Log($"{nameof(RockFragment)} destroyed on {gameObject.name}.");
        }

        if (feedback != null)
        {
            feedback.PlayRockDropFeedback(transform.position, dropShakeIntensity, dropShakeDuration);
        }

        // Spawn ore
        SpawnOre();

        // Spawn debris scatter effect
        if (debrisParticlePrefab != null)
        {
            GameObject debrisInstance = Instantiate(debrisParticlePrefab, transform.position, Quaternion.identity);
            Debries debrisScript = debrisInstance.GetComponent<Debries>();
            if (debrisScript != null)
            {
                debrisScript.ScatterDebries();
            }
        }

        // Register destruction and remove this fragment
        if (gameManager != null)
        {
            gameManager.RegisterFragmentDestroyed();
        }

        Destroy(gameObject);
    }

    private void SpawnOre()
    {
        if (ironOrePrefab == null)
        {
            return;
        }

        Collider[] rockColliders = GetComponentsInChildren<Collider>();
        Vector3 basePos = transform.position + Vector3.up * oreSpawnHeightOffset;

        for (int i = 0; i < oreDropCount; i++)
        {
            // Small random horizontal jitter so multiple ores don't spawn stacked.
            Vector2 jitter2D = Random.insideUnitCircle * oreSpawnJitter;
            Vector3 spawnPos = basePos + new Vector3(jitter2D.x, 0f, jitter2D.y);

            GameObject ore = Instantiate(ironOrePrefab, spawnPos, Quaternion.identity);

            // Ignore collision between the spawned ore and this rock (or its children)
            // so the ore doesn't get violently ejected if it spawns inside the rock's collider.
            Collider[] oreColliders = ore.GetComponentsInChildren<Collider>();
            foreach (Collider oc in oreColliders)
            {
                foreach (Collider rc in rockColliders)
                {
                    if (oc != null && rc != null)
                    {
                        Physics.IgnoreCollision(oc, rc, true);
                    }
                }
            }

            Rigidbody rb = ore.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Each ore gets its own outward direction so they scatter in different
                // directions instead of all drifting the same way.
                Vector2 dir2D = Random.insideUnitCircle.normalized;
                if (dir2D == Vector2.zero) dir2D = Vector2.right;
                Vector3 lateral = new Vector3(dir2D.x, 0f, dir2D.y);
                rb.linearVelocity = lateral * oreScatterSpeed + Vector3.up * oreUpwardBurst;
            }
        }
    }
}
