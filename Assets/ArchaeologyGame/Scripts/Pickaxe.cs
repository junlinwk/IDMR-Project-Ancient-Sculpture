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
    [SerializeField] private bool logPickaxeHits = true;

    protected override void Start()
    {
        base.Start();
        // Ensure this object is tagged as "Pickaxe" for rock fragment detection.
        TrySetTag("Pickaxe");
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (logPickaxeHits)
        {
            Debug.Log($"{nameof(Pickaxe)} collided with {collision.gameObject.name}.");
        }

        if (!IsHeld)
        {
            return;
        }

        RockFragment rockFragment = collision.gameObject.GetComponentInParent<RockFragment>();
        if (rockFragment == null)
        {
            return;
        }

        if (logPickaxeHits)
        {
            Debug.Log($"{nameof(Pickaxe)} hit RockFragment on {collision.gameObject.name}.");
        }

        Vector3 impactPoint = transform.position;
        if (collision.contactCount > 0)
        {
            impactPoint = collision.GetContact(0).point;
        }

        PlayStrikeFeedback(impactPoint, pickaxeHapticAmplitude, pickaxeHapticDuration);
    }

    public void PlayStrikeFeedback(Vector3 impactPoint, float amplitude, float duration)
    {
        TriggerHaptic(amplitude, duration);

        if (feedback != null)
        {
            feedback.PlayPickaxeStrikeSound(impactPoint);
        }
    }
}
