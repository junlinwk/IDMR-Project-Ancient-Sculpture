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
    public AudioClip pickaxeStrikeSFX;
    public AudioClip rockDropSFX;
    public AudioClip rockLandSFX;
    public bool useProceduralAudio = true;

    [Header("Screen Shake")]
    public ScreenShake screenShake;
    public float defaultRockShakeIntensity = 0.04f;
    public float defaultRockShakeDuration = 0.18f;
    public bool logFeedbackEvents = true;

    private AudioSource audioSource;
    private AudioClip proceduralPickaxeStrikeClip;
    private AudioClip proceduralRockDropClip;
    private AudioClip proceduralRockLandClip;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.spatialBlend = 0f; // 2D sound for UI feedback

        if (useProceduralAudio)
        {
            proceduralPickaxeStrikeClip = BuildProceduralPickaxeStrikeClip();
            proceduralRockDropClip = BuildProceduralRockDropClip();
            proceduralRockLandClip = BuildProceduralRockLandClip();
        }
    }

    private void Start()
    {
        if (screenShake == null)
        {
            screenShake = Object.FindFirstObjectByType<ScreenShake>();
        }
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
    /// Play the pickaxe strike sound at the impact position.
    /// </summary>
    public void PlayPickaxeStrikeSound(Vector3 position, float volume = 1f)
    {
        if (pickaxeStrikeSFX != null)
        {
            if (logFeedbackEvents)
            {
                Debug.Log($"{nameof(FeedbackManager)}: playing assigned pickaxe strike clip.");
            }
            audioSource.PlayOneShot(pickaxeStrikeSFX, volume);
            return;
        }

        if (useProceduralAudio && proceduralPickaxeStrikeClip != null)
        {
            if (logFeedbackEvents)
            {
                Debug.Log($"{nameof(FeedbackManager)}: playing procedural pickaxe strike clip.");
            }
            audioSource.PlayOneShot(proceduralPickaxeStrikeClip, volume);
            return;
        }

        if (logFeedbackEvents)
        {
            Debug.LogWarning($"{nameof(FeedbackManager)}: no pickaxe strike audio available.");
        }
    }

    /// <summary>
    /// Play the rock drop sound and a subtle shake.
    /// </summary>
    public void PlayRockDropFeedback(Vector3 position, float shakeIntensity = -1f, float shakeDuration = -1f, float volume = 1f)
    {
        if (rockDropSFX != null)
        {
            if (logFeedbackEvents)
            {
                Debug.Log($"{nameof(FeedbackManager)}: playing assigned rock drop clip.");
            }
            audioSource.PlayOneShot(rockDropSFX, volume);
        }
        else if (useProceduralAudio && proceduralRockDropClip != null)
        {
            if (logFeedbackEvents)
            {
                Debug.Log($"{nameof(FeedbackManager)}: playing procedural rock drop clip.");
            }
            audioSource.PlayOneShot(proceduralRockDropClip, volume);
        }
        else if (logFeedbackEvents)
        {
            Debug.LogWarning($"{nameof(FeedbackManager)}: no rock drop audio available.");
        }

        ShakeScreen(shakeIntensity < 0f ? defaultRockShakeIntensity : shakeIntensity,
            shakeDuration < 0f ? defaultRockShakeDuration : shakeDuration);
    }

    /// <summary>
    /// Play the final landing sound when the loosened stone hits the ground.
    /// </summary>
    public void PlayRockLandFeedback(Vector3 position, float volume = 1f)
    {
        if (rockLandSFX != null)
        {
            if (logFeedbackEvents)
            {
                Debug.Log($"{nameof(FeedbackManager)}: playing assigned rock land clip.");
            }
            audioSource.PlayOneShot(rockLandSFX, volume);
            return;
        }

        if (useProceduralAudio && proceduralRockLandClip != null)
        {
            if (logFeedbackEvents)
            {
                Debug.Log($"{nameof(FeedbackManager)}: playing procedural rock land clip.");
            }
            audioSource.PlayOneShot(proceduralRockLandClip, volume);
            return;
        }

        if (logFeedbackEvents)
        {
            Debug.LogWarning($"{nameof(FeedbackManager)}: no rock land audio available.");
        }
    }

    /// <summary>
    /// Shake the configured screen shake target.
    /// </summary>
    public void ShakeScreen(float intensity, float duration)
    {
        if (screenShake != null)
        {
            if (logFeedbackEvents)
            {
                Debug.Log($"{nameof(FeedbackManager)}: screen shake {intensity:0.###}, {duration:0.###}s.");
            }
            screenShake.Shake(intensity, duration);
            return;
        }

        if (logFeedbackEvents)
        {
            Debug.LogWarning($"{nameof(FeedbackManager)}: screenShake reference is missing.");
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

    private AudioClip BuildProceduralPickaxeStrikeClip()
    {
        const int sampleRate = 44100;
        const float duration = 0.12f;
        int samples = Mathf.CeilToInt(sampleRate * duration);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float env = Mathf.Exp(-t * 28f);
            float clickEnv = Mathf.Exp(-t * 120f);
            float metallic =
                Mathf.Sin(2f * Mathf.PI * 920f * t) * 0.45f +
                Mathf.Sin(2f * Mathf.PI * 1680f * t) * 0.28f +
                Mathf.Sin(2f * Mathf.PI * 2850f * t) * 0.18f;
            float noise = (Random.value * 2f - 1f) * 0.08f * env;
            float click = (Random.value * 2f - 1f) * 0.35f * clickEnv;
            data[i] = Mathf.Clamp((metallic * env) + noise + click, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("ProceduralPickaxeStrike", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip BuildProceduralRockDropClip()
    {
        const int sampleRate = 44100;
        const float duration = 0.32f;
        int samples = Mathf.CeilToInt(sampleRate * duration);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float env = Mathf.Exp(-t * 8f);
            float low =
                Mathf.Sin(2f * Mathf.PI * 34f * t) * 0.55f +
                Mathf.Sin(2f * Mathf.PI * 58f * t) * 0.35f +
                Mathf.Sin(2f * Mathf.PI * 96f * t) * 0.18f;
            float thud = Mathf.Sin(2f * Mathf.PI * 118f * t) * Mathf.Exp(-t * 24f) * 0.28f;
            float rumble = (Random.value * 2f - 1f) * 0.10f * env;
            data[i] = Mathf.Clamp((low * env) + thud + rumble, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("ProceduralRockDrop", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip BuildProceduralRockLandClip()
    {
        const int sampleRate = 44100;
        const float duration = 0.24f;
        int samples = Mathf.CeilToInt(sampleRate * duration);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float env = Mathf.Exp(-t * 12f);
            float thud =
                Mathf.Sin(2f * Mathf.PI * 46f * t) * Mathf.Exp(-t * 16f) * 0.72f +
                Mathf.Sin(2f * Mathf.PI * 84f * t) * Mathf.Exp(-t * 18f) * 0.30f;
            float dirt = (Random.value * 2f - 1f) * 0.08f * env;
            data[i] = Mathf.Clamp(thud + dirt, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("ProceduralRockLand", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
