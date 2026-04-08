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

    private int currentHits;
    private int hitCount = 0;
    private ArchaeologyGameManager gameManager;

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
            Hit();
        }
    }

    public void Hit()
    {
        hitCount++;

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
