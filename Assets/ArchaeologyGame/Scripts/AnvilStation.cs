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

    [Header("Debug")]
    [SerializeField] private bool logAnvil = true;

    private int hammerHits = 0;

    // No Start() tag-setting needed. Hammer.cs detects this station via
    // GetComponent<AnvilStation>() / GetComponentInParent<AnvilStation>(),
    // so we never rely on the "AnvilStation" tag.

    public void Hit()
    {
        ArchaeologyGameManager manager = ArchaeologyGameManager.Instance;

        if (logAnvil)
        {
            int ore = manager != null ? manager.GetOreCount() : -1;
            int cost = manager != null ? manager.GetCurrentUpgradeCost() : -1;
            int level = manager != null ? manager.GetUpgradeLevel() : -1;
            Debug.Log($"[AnvilStation] Hit() called. level={level}, ore={ore}, cost={cost}, hammerHits={hammerHits}/{hitsToUpgrade}");
        }

        // Already at max level — nothing to upgrade.
        if (manager != null && manager.IsAtMaxUpgrade())
        {
            if (logAnvil) Debug.Log("[AnvilStation] REJECTED: pickaxe is already at max upgrade.");
            PlayFail();
            return;
        }

        // Not enough ore — white hit, no progress.
        if (manager != null && !manager.HasEnoughOreForUpgrade())
        {
            if (logAnvil) Debug.Log($"[AnvilStation] REJECTED: not enough ore ({manager.GetOreCount()}/{manager.GetCurrentUpgradeCost()}).");
            PlayFail();
            manager.OnUpgradeFailed.Invoke();
            return;
        }

        // Valid hit — count and give feedback.
        hammerHits++;
        if (logAnvil) Debug.Log($"[AnvilStation] VALID hit. hammerHits now {hammerHits}/{hitsToUpgrade}");

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

        if (logAnvil) Debug.Log("[AnvilStation] Threshold reached — calling UpgradePickaxe()");
        bool success = manager.UpgradePickaxe();
        if (logAnvil) Debug.Log($"[AnvilStation] Upgrade result: {(success ? "SUCCESS" : "FAIL")}. Resetting hammer counter.");
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
        else if (logAnvil)
        {
            Debug.LogWarning("[AnvilStation] PlayFail: no failSound assigned.");
        }
    }
}
