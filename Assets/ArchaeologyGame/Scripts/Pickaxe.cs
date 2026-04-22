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
    [Tooltip("When enabled, prints a detailed grab-state snapshot every Nth frame " +
             "(see Grab Diag Interval). Useful for diagnosing instant-release issues.")]
    [SerializeField] private bool logGrabDiagnostic = false;
    [SerializeField] private int grabDiagIntervalFrames = 30;

    private bool wasHeldLastFrame = false;
    private int grabDiagCounter = 0;

    protected override void Start()
    {
        base.Start();
        // Ensure this object is tagged as "Pickaxe" for rock fragment detection.
        TrySetTag("Pickaxe");
    }

    private void Update()
    {
        // Log grab state transitions so we can spot simulator / dual-hand issues.
        if (logPickaxeHits && IsHeld != wasHeldLastFrame)
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
            Debug.Log($"[Pickaxe] IsHeld {wasHeldLastFrame} -> {IsHeld} (frame {Time.frameCount}). " +
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
                Debug.Log($"[Pickaxe DIAG] IsHeld={IsHeld}, gripL={gripL:F2}, gripR={gripR:F2}, pos={transform.position:F2}");
            }
        }
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
