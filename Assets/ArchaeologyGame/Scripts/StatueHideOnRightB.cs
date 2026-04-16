using UnityEngine;

/// <summary>
/// Hides the target object once when the right-hand B button is pressed.
/// Attach this to an always-active object.
/// </summary>
public class StatueHideOnRightB : MonoBehaviour
{
    [SerializeField] private GameObject target;
    [SerializeField] private bool useEditorKeyFallback = true;
    [SerializeField] private bool logDebug = true;

    private bool hasHidden;

    private void Awake()
    {
        if (target == null)
        {
            target = gameObject;
        }
    }

    private void Update()
    {
        if (hasHidden)
        {
            return;
        }

        if (!WasRightBPressed())
        {
            return;
        }

        HideOnce();
    }

    private bool WasRightBPressed()
    {
        if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch))
        {
            return true;
        }

        if (useEditorKeyFallback && Application.isEditor && Input.GetKeyDown(KeyCode.JoystickButton1))
        {
            return true;
        }

        return false;
    }

    private void HideOnce()
    {
        hasHidden = true;

        if (target != null)
        {
            target.SetActive(false);
        }

        if (logDebug)
        {
            Debug.Log($"{nameof(StatueHideOnRightB)} hid {target.name} once.");
        }
    }
}
