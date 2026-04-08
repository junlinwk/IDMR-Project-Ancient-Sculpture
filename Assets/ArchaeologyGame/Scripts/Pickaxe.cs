using UnityEngine;

/// <summary>
/// Pickaxe tool controller.
/// Extends WeaponBase to handle grab detection and haptic feedback.
/// When it collides with rock fragments, the collision handler triggers the hit.
/// </summary>
public class Pickaxe : WeaponBase
{
    [Header("Pickaxe Settings")]
    [SerializeField] private float pickaxeHapticAmplitude = 0.7f;
    [SerializeField] private float pickaxeHapticDuration = 0.15f;

    protected override void Start()
    {
        base.Start();
        // Ensure this object is tagged as "Pickaxe" for rock fragment detection
        gameObject.tag = "Pickaxe";
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Check if this is being held and colliding with a rock
        if (IsHeld && collision.gameObject.CompareTag("Rock"))
        {
            // Haptic feedback on strike
            TriggerHaptic(pickaxeHapticAmplitude, pickaxeHapticDuration);
        }
    }
}
