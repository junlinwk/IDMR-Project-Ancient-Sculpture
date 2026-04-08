using UnityEngine;

/// <summary>
/// Anvil/Upgrade station.
/// Players strike this with the hammer to upgrade the pickaxe.
/// </summary>
public class AnvilStation : MonoBehaviour
{
    [Header("Upgrade Settings")]
    [SerializeField] private int hitsToUpgrade = 5;
    [SerializeField] private AudioClip anvilSound;
    [SerializeField] private ParticleSystem upgradeParticles;

    private int hammerHits = 0;

    private void Start()
    {
        // Ensure this object is tagged for detection
        gameObject.tag = "AnvilStation";
    }

    public void Hit()
    {
        hammerHits++;

        // Play sound and visual feedback
        if (anvilSound != null)
        {
            AudioSource.PlayClipAtPoint(anvilSound, transform.position, 0.8f);
        }

        if (upgradeParticles != null)
        {
            Instantiate(upgradeParticles, transform.position, Quaternion.identity);
        }

        // Check if upgrade threshold is reached
        if (hammerHits >= hitsToUpgrade)
        {
            PerformUpgrade();
        }
    }

    private void PerformUpgrade()
    {
        if (ArchaeologyGameManager.Instance != null)
        {
            ArchaeologyGameManager.Instance.UpgradePickaxe();
        }

        // Reset counter
        hammerHits = 0;

        // Optional: spawn upgrade effect
        if (upgradeParticles != null)
        {
            ParticleSystem particles = Instantiate(upgradeParticles, transform.position, Quaternion.identity);
            particles.Play();
        }
    }
}
