using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Aligns the scanned environment to the real world using a persistent OVRSpatialAnchor.
///
/// First run:
///   1. Player stands at a known real-world landmark (a point also marked in the scan).
///   2. Player presses the Create button — an OVRSpatialAnchor is created at the controller.
///   3. Environment root is translated so Scan Reference Point overlaps the anchor.
///   4. Anchor UUID is saved to PlayerPrefs so the next session skips the calibration.
///
/// Subsequent runs:
///   - Saved UUID is loaded and localised automatically.
///   - Environment snaps to the same real-world position without user input.
///
/// Re-calibrate: press the Forget button to erase the saved anchor, then recreate.
/// </summary>
public class EnvironmentSpatialAnchor : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Root transform of the scanned environment (parent of all scan models).")]
    [SerializeField] private Transform environmentRoot;
    [Tooltip("Child transform inside the scan that corresponds to a visible real-world landmark.")]
    [SerializeField] private Transform scanReferencePoint;
    [SerializeField] private OVRCameraRig cameraRig;

    [Header("Create Anchor (first calibration)")]
    [SerializeField] private OVRInput.Button createAnchorButton = OVRInput.Button.Two;    // B
    [SerializeField] private OVRInput.Controller createController = OVRInput.Controller.RTouch;
    [SerializeField] private KeyCode editorCreateKey = KeyCode.C;

    [Header("Forget / Recalibrate")]
    [SerializeField] private OVRInput.Button forgetButton = OVRInput.Button.Four;          // Y
    [SerializeField] private OVRInput.Controller forgetController = OVRInput.Controller.LTouch;
    [SerializeField] private KeyCode editorForgetKey = KeyCode.R;

    [Header("Alignment Options")]
    [Tooltip("Also rotate the environment so its scan reference point's forward matches the anchor's forward.")]
    [SerializeField] private bool alignRotation = false;
    [Tooltip("Match vertical (Y) height as well. If false, keeps the scene's original floor height.")]
    [SerializeField] private bool alignVertical = false;

    [Header("Storage")]
    [SerializeField] private string playerPrefsKey = "ArchaeologyEnvAnchorUuid";

    [Header("Debug")]
    [SerializeField] private bool logAnchor = true;

    private OVRSpatialAnchor currentAnchor;
    private bool isBusy;

    private async void Start()
    {
        await TryLoadSavedAnchor();
    }

    private void Update()
    {
        if (isBusy) return;

        if (PressedNow(createAnchorButton, createController, editorCreateKey))
        {
            _ = CreateAnchorAtControllerAsync();
        }
        else if (PressedNow(forgetButton, forgetController, editorForgetKey))
        {
            _ = ForgetAnchorAsync();
        }
    }

    private bool PressedNow(OVRInput.Button btn, OVRInput.Controller ctrl, KeyCode key)
    {
        if (OVRInput.GetDown(btn, ctrl)) return true;
        if (Application.isEditor && Input.GetKeyDown(key)) return true;
        return false;
    }

    // ──────────────────────────────────────────────────────────
    // CREATE
    // ──────────────────────────────────────────────────────────

    private async Task CreateAnchorAtControllerAsync()
    {
        if (cameraRig == null || environmentRoot == null || scanReferencePoint == null)
        {
            Debug.LogError("[Anchor] Missing references. Assign cameraRig, environmentRoot, scanReferencePoint.");
            return;
        }

        isBusy = true;

        try
        {
            // Clear existing anchor if any
            if (currentAnchor != null)
            {
                Destroy(currentAnchor.gameObject);
                currentAnchor = null;
            }

            // Create a GameObject at the current controller pose
            Transform ctrl = createController == OVRInput.Controller.LTouch
                ? cameraRig.leftControllerAnchor
                : cameraRig.rightControllerAnchor;

            GameObject anchorGO = new GameObject("EnvSpatialAnchor");
            anchorGO.transform.SetPositionAndRotation(ctrl.position, ctrl.rotation);

            OVRSpatialAnchor sa = anchorGO.AddComponent<OVRSpatialAnchor>();

            // Wait until the anchor system creates it
            while (!sa.Created)
            {
                await Task.Yield();
            }

            if (logAnchor) Debug.Log($"[Anchor] Created at {ctrl.position:F2}. UUID={sa.Uuid}");

            // Save for persistence across sessions
            var saveResult = await sa.SaveAnchorAsync();
            if (saveResult.Success)
            {
                PlayerPrefs.SetString(playerPrefsKey, sa.Uuid.ToString());
                PlayerPrefs.Save();
                if (logAnchor) Debug.Log($"[Anchor] Saved. Next session will load automatically.");
            }
            else
            {
                Debug.LogWarning($"[Anchor] Save failed: {saveResult.Status}. Anchor works for this session only.");
            }

            currentAnchor = sa;
            AlignEnvironmentToAnchor(sa.transform);
        }
        finally
        {
            isBusy = false;
        }
    }

    // ──────────────────────────────────────────────────────────
    // LOAD
    // ──────────────────────────────────────────────────────────

    private async Task TryLoadSavedAnchor()
    {
        string saved = PlayerPrefs.GetString(playerPrefsKey, string.Empty);
        if (string.IsNullOrEmpty(saved))
        {
            if (logAnchor) Debug.Log($"[Anchor] No saved anchor. Press {createAnchorButton} ({editorCreateKey} in editor) to calibrate.");
            return;
        }

        if (!System.Guid.TryParse(saved, out System.Guid uuid))
        {
            Debug.LogWarning($"[Anchor] Saved UUID malformed: '{saved}'. Clearing.");
            PlayerPrefs.DeleteKey(playerPrefsKey);
            return;
        }

        isBusy = true;
        try
        {
            var unbound = new List<OVRSpatialAnchor.UnboundAnchor>();
            var result = await OVRSpatialAnchor.LoadUnboundAnchorsAsync(new[] { uuid }, unbound);
            if (!result.Success || unbound.Count == 0)
            {
                Debug.LogWarning($"[Anchor] Could not load anchor {uuid} on this device. " +
                                 $"Press {createAnchorButton} to calibrate again.");
                return;
            }

            // Create the GameObject that will host the anchor
            GameObject anchorGO = new GameObject("EnvSpatialAnchor (Loaded)");
            OVRSpatialAnchor sa = anchorGO.AddComponent<OVRSpatialAnchor>();

            bool localized = await unbound[0].LocalizeAsync();
            if (!localized)
            {
                Debug.LogWarning("[Anchor] Localization failed.");
                Destroy(anchorGO);
                return;
            }

            unbound[0].BindTo(sa);

            // Wait until Localized flag flips true
            int timeoutFrames = 600;
            while (!sa.Localized && timeoutFrames-- > 0)
            {
                await Task.Yield();
            }

            if (!sa.Localized)
            {
                Debug.LogWarning("[Anchor] Anchor never localized. Calibration lost.");
                Destroy(anchorGO);
                return;
            }

            currentAnchor = sa;
            AlignEnvironmentToAnchor(sa.transform);

            if (logAnchor) Debug.Log($"[Anchor] Loaded and aligned to UUID={sa.Uuid}.");
        }
        finally
        {
            isBusy = false;
        }
    }

    // ──────────────────────────────────────────────────────────
    // FORGET
    // ──────────────────────────────────────────────────────────

    private async Task ForgetAnchorAsync()
    {
        isBusy = true;
        try
        {
            if (currentAnchor != null)
            {
                var eraseResult = await currentAnchor.EraseAnchorAsync();
                if (logAnchor)
                {
                    Debug.Log(eraseResult.Success
                        ? $"[Anchor] Erased from device."
                        : $"[Anchor] Erase failed: {eraseResult.Status}");
                }
                Destroy(currentAnchor.gameObject);
                currentAnchor = null;
            }

            PlayerPrefs.DeleteKey(playerPrefsKey);
            PlayerPrefs.Save();

            if (logAnchor) Debug.Log($"[Anchor] Forgotten. Press {createAnchorButton} to recalibrate.");
        }
        finally
        {
            isBusy = false;
        }
    }

    // ──────────────────────────────────────────────────────────
    // ALIGNMENT
    // ──────────────────────────────────────────────────────────

    private void AlignEnvironmentToAnchor(Transform anchor)
    {
        if (environmentRoot == null || scanReferencePoint == null) return;

        if (alignRotation)
        {
            Quaternion delta = anchor.rotation * Quaternion.Inverse(scanReferencePoint.rotation);
            environmentRoot.rotation = delta * environmentRoot.rotation;
        }

        Vector3 offset = anchor.position - scanReferencePoint.position;
        if (!alignVertical) offset.y = 0f;
        environmentRoot.position += offset;

        if (logAnchor)
        {
            Debug.Log($"[Anchor] Environment aligned — offset {offset:F3}, " +
                      $"alignRotation={alignRotation}, alignVertical={alignVertical}");
        }
    }
}
