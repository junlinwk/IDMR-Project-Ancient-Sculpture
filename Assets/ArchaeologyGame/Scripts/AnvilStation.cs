using UnityEngine;

/// <summary>
/// Anvil/Upgrade station.
/// Players strike this with the hammer to upgrade the pickaxe.
/// Each upgrade costs iron ore (configured on ArchaeologyGameManager);
/// hits without enough ore play a fail sound and do not count toward progress.
/// </summary>
public class AnvilStation : MonoBehaviour
{
    [Header("Upgrade Settings")]
    [Tooltip("Number of hammer strikes required to trigger an upgrade (after ore cost is met).")]
    [SerializeField] private int hitsToUpgrade = 5;

    [Header("Audio")]
    [SerializeField] private AudioClip anvilSound;
    [Tooltip("Played when the hammer strikes but ore is insufficient or pickaxe is maxed.")]
    [SerializeField] private AudioClip failSound;

    [Header("Visual")]
    [Tooltip("Particle effect spawned on each successful hammer strike.")]
    [SerializeField] private ParticleSystem hitParticles;
    [Tooltip("Particle effect spawned when the upgrade actually triggers.")]
    [SerializeField] private ParticleSystem upgradeParticles;

    private int hammerHits = 0;

    private void Start()
    {
        // Ensure this object is tagged for detection
        gameObject.tag = "AnvilStation";
    }

    public void Hit()
    {
        ArchaeologyGameManager manager = ArchaeologyGameManager.Instance;

        // Already at max level — nothing to upgrade.
        if (manager != null && manager.IsAtMaxUpgrade())
        {
            PlayFail();
            return;
        }

        // Not enough ore — white hit, no progress.
        if (manager != null && !manager.HasEnoughOreForUpgrade())
        {
            PlayFail();
            manager.OnUpgradeFailed.Invoke();
            return;
        }

        // Valid hit — count and give feedback.
        hammerHits++;

        if (anvilSound != null)
        {
            AudioSource.PlayClipAtPoint(anvilSound, transform.position, 0.8f);
        }

        if (hitParticles != null)
        {
            Instantiate(hitParticles, transform.position, Quaternion.identity);
        }

        if (hammerHits >= hitsToUpgrade)
        {
            PerformUpgrade();
        }
    }

    private void PerformUpgrade()
    {
        ArchaeologyGameManager manager = ArchaeologyGameManager.Instance;
        if (manager == null)
        {
            hammerHits = 0;
            return;
        }

        bool success = manager.UpgradePickaxe();
        hammerHits = 0;

        if (success && upgradeParticles != null)
        {
            ParticleSystem particles = Instantiate(upgradeParticles, transform.position, Quaternion.identity);
            particles.Play();
        }
    }

    private void PlayFail()
    {
        if (failSound != null)
        {
            AudioSource.PlayClipAtPoint(failSound, transform.position, 0.8f);
        }
    }
}
