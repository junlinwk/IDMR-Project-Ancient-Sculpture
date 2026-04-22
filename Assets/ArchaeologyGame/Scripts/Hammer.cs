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

    [Header("Debug")]
    [SerializeField] private bool logHammer = true;

    private bool wasHeldLastFrame = false;

    protected override void Start()
    {
        base.Start();
        // Ensure this object is tagged as "Hammer" for detection.
        TrySetTag("Hammer");
    }

    private void Update()
    {
        // Log grab state transitions so we can spot simulator / dual-hand issues.
        if (logHammer && IsHeld != wasHeldLastFrame)
        {
            string grabberName = (grabbable != null && grabbable.grabbedBy != null)
                ? grabbable.grabbedBy.gameObject.name
                : "(none)";
            Debug.Log($"[Hammer] IsHeld {wasHeldLastFrame} -> {IsHeld}. grabbedBy={grabberName}, controller={GetHoldingController()}");
            wasHeldLastFrame = IsHeld;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (logHammer)
        {
            Debug.Log($"[Hammer] Collision with '{collision.gameObject.name}' (IsHeld={IsHeld})");
        }

        // Check if striking the anvil station
        AnvilStation anvil = collision.gameObject.GetComponent<AnvilStation>();
        if (anvil == null)
        {
            anvil = collision.gameObject.GetComponentInParent<AnvilStation>();
        }

        if (anvil != null && IsHeld)
        {
            if (logHammer) Debug.Log($"[Hammer] Hit AnvilStation — calling anvil.Hit()");
            anvil.Hit();

            // Haptic feedback on anvil strike
            TriggerHaptic(hammerHapticAmplitude, hammerHapticDuration);
        }
        else if (anvil != null && !IsHeld && logHammer)
        {
            Debug.Log($"[Hammer] Touched AnvilStation but NOT HELD — ignoring.");
        }
    }
}
