using UnityEngine;

/// <summary>
/// Hammer tool controller.
/// Extends WeaponBase to handle grab detection and interaction with AnvilStation.
/// </summary>
public class Hammer : WeaponBase
{
    [Header("Hammer Settings")]
    [SerializeField] private float hammerHapticAmplitude = 0.5f;
    [SerializeField] private float hammerHapticDuration = 0.1f;

    protected override void Start()
    {
        base.Start();
        // Ensure this object is tagged as "Hammer" for detection
        gameObject.tag = "Hammer";
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Check if striking the anvil station
        AnvilStation anvil = collision.gameObject.GetComponent<AnvilStation>();
        if (anvil != null && IsHeld)
        {
            anvil.Hit();

            // Haptic feedback on anvil strike
            TriggerHaptic(hammerHapticAmplitude, hammerHapticDuration);
        }
    }
}
