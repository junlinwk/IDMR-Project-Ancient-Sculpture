using UnityEngine;

/// <summary>
/// Toggles renderer visibility for one or more target objects using the right-hand B button.
/// Attach this to an object that stays active all the time.
/// </summary>
public class RightBRenderToggle : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private GameObject[] targets;

    [Header("Settings")]
    [SerializeField] private bool startVisible = true;
    [SerializeField] private bool logDebug = true;
    [SerializeField] private bool useEditorKeyFallback = true;

    private Renderer[][] cachedRenderers;
    private bool isVisible;

    private void Awake()
    {
        isVisible = startVisible;
        CacheRenderers();
        ApplyVisibility();
    }

    private void Update()
    {
        if (!WasRightBPressed())
        {
            return;
        }

        isVisible = !isVisible;
        ApplyVisibility();

        if (logDebug)
        {
            Debug.Log($"{nameof(RightBRenderToggle)} toggled {(isVisible ? "visible" : "hidden")} on {gameObject.name}.");
        }
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

    private void CacheRenderers()
    {
        if (targets == null || targets.Length == 0)
        {
            cachedRenderers = new Renderer[0][];
            if (logDebug)
            {
                Debug.LogWarning($"{nameof(RightBRenderToggle)} on {gameObject.name} has no targets assigned.");
            }
            return;
        }

        cachedRenderers = new Renderer[targets.Length][];

        for (int i = 0; i < targets.Length; i++)
        {
            GameObject target = targets[i];
            if (target == null)
            {
                cachedRenderers[i] = null;
                continue;
            }

            cachedRenderers[i] = target.GetComponentsInChildren<Renderer>(true);
        }
    }

    private void ApplyVisibility()
    {
        if (cachedRenderers == null)
        {
            return;
        }

        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            Renderer[] renderers = cachedRenderers[i];
            if (renderers == null)
            {
                continue;
            }

            foreach (Renderer renderer in renderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = isVisible;
                }
            }
        }
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            CacheRenderers();
        }
    }
}
