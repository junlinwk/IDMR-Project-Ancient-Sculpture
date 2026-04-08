using UnityEngine;

/// <summary>
/// Abstract base class for all grabbable weapons/tools in the game.
/// Provides common functionality for grab detection, controller identification, and haptic feedback.
///
/// Subclasses (Pickaxe, Hammer) inherit this to get:
/// - Grab state detection (IsHeld property)
/// - Controller hand identification (GetHoldingController)
/// - Trigger button mapping (GetIndexTriggerButton, IsIndexTriggerHeld)
/// - Haptic feedback triggering (TriggerHaptic)
/// </summary>
public abstract class WeaponBase : MonoBehaviour
{
    protected OVRGrabbable grabbable;
    protected FeedbackManager feedback;

    /// <summary>
    /// True if this weapon is currently being held/grabbed by a controller.
    /// </summary>
    public bool IsHeld => grabbable != null && grabbable.isGrabbed;

    protected virtual void Awake()
    {
        grabbable = GetComponent<OVRGrabbable>();
        if (grabbable == null)
        {
            Debug.LogError($"WeaponBase: OVRGrabbable missing on {gameObject.name}");
        }
    }

    protected virtual void Start()
    {
        feedback = FindObjectOfType<FeedbackManager>();
    }

    /// <summary>
    /// Returns which controller is holding this weapon (LTouch or RTouch).
    /// Examines the grabber's name to determine left vs right.
    /// Defaults to RTouch if unable to determine.
    /// </summary>
    protected OVRInput.Controller GetHoldingController()
    {
        if (grabbable == null || !grabbable.isGrabbed || grabbable.grabbedBy == null)
            return OVRInput.Controller.RTouch;

        // Check if the grabber is on the left or right hand by examining its name/parent
        string grabberName = grabbable.grabbedBy.gameObject.name.ToLower();
        if (grabberName.Contains("left") || grabberName.Contains("l_"))
            return OVRInput.Controller.LTouch;

        return OVRInput.Controller.RTouch;
    }

    /// <summary>
    /// Returns the correct index trigger button for the holding hand.
    /// </summary>
    protected OVRInput.Button GetIndexTriggerButton()
    {
        if (GetHoldingController() == OVRInput.Controller.LTouch)
            return OVRInput.Button.PrimaryIndexTrigger;
        return OVRInput.Button.SecondaryIndexTrigger;
    }

    /// <summary>
    /// Returns whether the index trigger was just pressed this frame.
    /// </summary>
    protected bool IsIndexTriggerDown()
    {
        return OVRInput.GetDown(GetIndexTriggerButton());
    }

    /// <summary>
    /// Returns whether the index trigger is currently held down.
    /// </summary>
    protected bool IsIndexTriggerHeld()
    {
        return OVRInput.Get(GetIndexTriggerButton());
    }

    /// <summary>
    /// Trigger haptic (vibration) feedback on the holding controller.
    /// </summary>
    /// <param name="amplitude">Vibration strength (0-1)</param>
    /// <param name="duration">Vibration duration in seconds</param>
    protected void TriggerHaptic(float amplitude, float duration)
    {
        if (feedback != null)
        {
            feedback.TriggerHaptic(GetHoldingController(), amplitude, duration);
        }
    }
}
