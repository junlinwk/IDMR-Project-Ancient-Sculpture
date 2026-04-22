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
    [Tooltip("When enabled, prints a detailed grab-state snapshot every Nth frame. " +
             "Useful for diagnosing instant-release issues.")]
    [SerializeField] private bool logGrabDiagnostic = false;
    [SerializeField] private int grabDiagIntervalFrames = 30;

    private bool wasHeldLastFrame = false;
    private int grabDiagCounter = 0;

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
            float gripL = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger);
            float gripR = OVRInput.Get(OVRInput.Axis1D.SecondaryHandTrigger);
            Rigidbody rb = GetComponent<Rigidbody>();
            string rbInfo = rb != null
                ? $"kinematic={rb.isKinematic}, gravity={rb.useGravity}, mass={rb.mass}"
                : "no Rigidbody";
            Debug.Log($"[Hammer] IsHeld {wasHeldLastFrame} -> {IsHeld} (frame {Time.frameCount}). " +
                      $"grabbedBy={grabberName}, controller={GetHoldingController()}, " +
                      $"gripL={gripL:F2}, gripR={gripR:F2}, {rbInfo}");
            wasHeldLastFrame = IsHeld;
        }

        // Optional periodic snapshot to detect sub-frame flicker.
        if (logGrabDiagnostic)
        {
            grabDiagCounter++;
            if (grabDiagCounter >= grabDiagIntervalFrames)
            {
                grabDiagCounter = 0;
                float gripL = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger);
                float gripR = OVRInput.Get(OVRInput.Axis1D.SecondaryHandTrigger);
                Debug.Log($"[Hammer DIAG] IsHeld={IsHeld}, gripL={gripL:F2}, gripR={gripR:F2}, pos={transform.position:F2}");
            }
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
