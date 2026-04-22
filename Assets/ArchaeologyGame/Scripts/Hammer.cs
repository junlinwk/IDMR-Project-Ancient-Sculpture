using UnityEngine;

/// <summary>
/// Hammer tool controller.
/// Extends WeaponBase to handle grab detection and interaction with AnvilStation.
///
/// Uses velocity+proximity detection instead of OnCollisionEnter because
/// OVRGrabbable makes the Rigidbody kinematic while held, and Unity does not
/// fire OnCollisionEnter for kinematic vs. static collider pairs.
/// </summary>
public class Hammer : WeaponBase
{
    [Header("Hammer Settings")]
    [SerializeField] private float hammerHapticAmplitude = 0.5f;
    [SerializeField] private float hammerHapticDuration = 0.1f;

    [Tooltip("Max distance from anvil centre for a strike to register (metres). " +
             "Increase if the hammerhead model is large or offset.")]
    [SerializeField] private float strikeRadius = 0.25f;

    [Tooltip("Minimum hand speed (m/s) required to count as a deliberate strike. " +
             "Prevents slow contact from registering.")]
    [SerializeField] private float minStrikeSpeed = 1.0f;

    [Tooltip("Cooldown between consecutive anvil hits to prevent rapid multi-trigger.")]
    [SerializeField] private float strikeCooldown = 0.4f;

    [Header("Debug")]
    [SerializeField] private bool logHammer = true;

    private AnvilStation _cachedAnvil;
    private Vector3 _prevPos;
    private float _currentSpeed;
    private float _lastStrikeTime = -999f;
    private bool wasHeldLastFrame = false;

    protected override void Start()
    {
        base.Start();
        TrySetTag("Hammer");
        _cachedAnvil = Object.FindFirstObjectByType<AnvilStation>();
        if (_cachedAnvil == null)
            Debug.LogWarning("[Hammer] No AnvilStation found in scene.");
        _prevPos = transform.position;
    }

    private void Update()
    {
        // Track hand speed every frame regardless of held state.
        _currentSpeed = (transform.position - _prevPos).magnitude / Time.deltaTime;
        _prevPos = transform.position;

        // Log grab state transitions.
        if (logHammer && IsHeld != wasHeldLastFrame)
        {
            string grabberName = (grabbable != null && grabbable.grabbedBy != null)
                ? grabbable.grabbedBy.gameObject.name : "(none)";
            float gripL = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger);
            float gripR = OVRInput.Get(OVRInput.Axis1D.SecondaryHandTrigger);
            Debug.Log($"[Hammer] IsHeld {wasHeldLastFrame} -> {IsHeld} (frame {Time.frameCount}). " +
                      $"grabbedBy={grabberName}, gripL={gripL:F2}, gripR={gripR:F2}");
            wasHeldLastFrame = IsHeld;
        }

        if (!IsHeld) return;
        if (_cachedAnvil == null) return;
        if (Time.time - _lastStrikeTime < strikeCooldown) return;

        float dist = Vector3.Distance(transform.position, _cachedAnvil.transform.position);
        if (dist > strikeRadius) return;
        if (_currentSpeed < minStrikeSpeed) return;

        if (logHammer)
            Debug.Log($"[Hammer] Strike detected — dist={dist:F3}m, speed={_currentSpeed:F2}m/s — calling anvil.Hit()");

        _lastStrikeTime = Time.time;
        _cachedAnvil.Hit();
        TriggerHaptic(hammerHapticAmplitude, hammerHapticDuration);
    }
}
