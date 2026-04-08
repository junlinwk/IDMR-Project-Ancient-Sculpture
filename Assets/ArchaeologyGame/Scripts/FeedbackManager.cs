using System.Collections;
using UnityEngine;

/// <summary>
/// Central manager for all audio, particle, and haptic feedback in the game.
/// Provides a unified interface for triggering visual and tactile effects.
///
/// Usage: Attach to a GameObject in the scene (e.g., an empty "FeedbackManager" object).
/// Configure audio clips and particle prefabs in the Inspector.
/// </summary>
public class FeedbackManager : MonoBehaviour
{
    [Header("Particle Effects")]
    public GameObject hitParticlePrefab;
    public GameObject deathParticlePrefab;
    public GameObject muzzleFlashPrefab;

    [Header("Audio Clips")]
    public AudioClip shootSFX;
    public AudioClip swordSwingSFX;
    public AudioClip impactSFX;
    public AudioClip enemyDeathSFX;
    public AudioClip playerHitSFX;

    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.spatialBlend = 0f; // 2D sound for UI feedback
    }

    /// <summary>
    /// Play a hit effect: particle + impact sound.
    /// </summary>
    public void PlayHitEffect(Vector3 position)
    {
        if (hitParticlePrefab != null)
        {
            GameObject effect = Instantiate(hitParticlePrefab, position, Quaternion.identity);
            Destroy(effect, 2f);
        }
        if (impactSFX != null)
        {
            audioSource.PlayOneShot(impactSFX);
        }
    }

    /// <summary>
    /// Play a death effect: particle + death sound.
    /// </summary>
    public void PlayDeathEffect(Vector3 position)
    {
        if (deathParticlePrefab != null)
        {
            GameObject effect = Instantiate(deathParticlePrefab, position, Quaternion.identity);
            Destroy(effect, 3f);
        }
        if (enemyDeathSFX != null)
        {
            audioSource.PlayOneShot(enemyDeathSFX);
        }
    }

    /// <summary>
    /// Play a shoot effect: muzzle flash + shoot sound.
    /// </summary>
    public void PlayShootEffect(Vector3 position, Quaternion rotation)
    {
        if (muzzleFlashPrefab != null)
        {
            GameObject effect = Instantiate(muzzleFlashPrefab, position, rotation);
            Destroy(effect, 0.5f);
        }
        if (shootSFX != null)
        {
            audioSource.PlayOneShot(shootSFX);
        }
    }

    /// <summary>
    /// Play sword swing sound.
    /// </summary>
    public void PlaySwingSound()
    {
        if (swordSwingSFX != null)
        {
            audioSource.PlayOneShot(swordSwingSFX);
        }
    }

    /// <summary>
    /// Play player hit sound.
    /// </summary>
    public void PlayPlayerHitSound()
    {
        if (playerHitSFX != null)
        {
            audioSource.PlayOneShot(playerHitSFX);
        }
    }

    /// <summary>
    /// Trigger haptic (vibration) feedback on a specific controller.
    /// </summary>
    /// <param name="controller">Which controller (LTouch or RTouch)</param>
    /// <param name="amplitude">Vibration strength (0-1)</param>
    /// <param name="duration">Vibration duration in seconds</param>
    public void TriggerHaptic(OVRInput.Controller controller, float amplitude, float duration)
    {
        StartCoroutine(HapticCoroutine(controller, amplitude, duration));
    }

    private IEnumerator HapticCoroutine(OVRInput.Controller controller, float amplitude, float duration)
    {
        OVRInput.SetControllerVibration(1f, amplitude, controller);
        yield return new WaitForSeconds(duration);
        OVRInput.SetControllerVibration(0f, 0f, controller);
    }
}
