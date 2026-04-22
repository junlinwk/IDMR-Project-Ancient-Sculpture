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
    [SerializeField] private int oreDropCount = 1;

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

        feedback = Object.FindFirstObjectByType<FeedbackManager>();
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

    private void Destroy()
    {
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

        for (int i = 0; i < oreDropCount; i++)
        {
            GameObject ore = Instantiate(ironOrePrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            Rigidbody rb = ore.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Apply random force to scatter ore
                Vector3 randomDirection = Random.insideUnitSphere;
                randomDirection.y = Mathf.Abs(randomDirection.y); // Always upward component
                rb.linearVelocity = randomDirection * 3f;
            }
        }
    }
}
