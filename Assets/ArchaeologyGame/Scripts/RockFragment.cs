using UnityEngine;

public class RockFragment : MonoBehaviour
{
    [Header("Hit Settings")]
    [SerializeField] private int baseHits = 3;
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
            rb.useGravity = false;
        }

        feedback = Object.FindFirstObjectByType<FeedbackManager>();
    }

    public void Initialize(ArchaeologyGameManager manager)
    {
        gameManager = manager;
        currentHits = baseHits;
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
        // Each upgrade reduces hits needed by 1, minimum 1
        currentHits = Mathf.Max(1, baseHits - upgradeLevel);
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
