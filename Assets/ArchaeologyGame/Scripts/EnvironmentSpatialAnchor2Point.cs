using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Two-point environment calibration backed by OVRSpatialAnchor.
///
/// Calibration flow (first run):
///   1. Stand at real-world Point A (marked in the scan as Scan Reference Point A).
///   2. Press the Create button — captures controller pose as real A.
///   3. Walk to real-world Point B (marked as Scan Reference Point B).
///   4. Press again — captures real B, aligns translation + rotation, saves anchor.
///
/// The anchor is stored with its forward axis pointing from A→B. On subsequent
/// runs the anchor re-localises and the script reconstructs the full alignment
/// from the saved pose. Height alignment is horizontal-only by default so the
/// scene floor stays at its authored height.
/// </summary>
public class EnvironmentSpatialAnchor2Point : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Root transform of the scanned environment (parent of all scan models).")]
    [SerializeField] private Transform environmentRoot;
    [Tooltip("Child transform in the scan that corresponds to real-world Point A.")]
    [SerializeField] private Transform scanReferencePointA;
    [Tooltip("Child transform in the scan that corresponds to real-world Point B.")]
    [SerializeField] private Transform scanReferencePointB;
    [SerializeField] private OVRCameraRig cameraRig;

    [Header("Capture Input")]
    [SerializeField] private OVRInput.Button captureButton = OVRInput.Button.Two;        // B
    [SerializeField] private OVRInput.Controller captureController = OVRInput.Controller.RTouch;
    [SerializeField] private KeyCode editorCaptureKey = KeyCode.C;

    [Header("Forget / Restart Input")]
    [SerializeField] private OVRInput.Button forgetButton = OVRInput.Button.Four;        // Y
    [SerializeField] private OVRInput.Controller forgetController = OVRInput.Controller.LTouch;
    [SerializeField] private KeyCode editorForgetKey = KeyCode.R;

    [Header("Alignment Options")]
    [Tooltip("Match vertical (Y) height as well. If false, keeps the scene's original floor height.")]
    [SerializeField] private bool alignVertical = false;
    [Tooltip("Use horizontal (Y-axis only) rotation. If false, full 3D rotation is applied.")]
    [SerializeField] private bool horizontalRotationOnly = true;

    [Header("Storage")]
    [SerializeField] private string playerPrefsKey = "ArchaeologyEnvAnchor2P";

    [Header("Input Gating")]
    [Tooltip("If true, capture/forget buttons are ignored until EnableInput() is called externally " +
             "(e.g. by IntroVideoController after the intro video ends). Auto-loading of saved " +
             "anchors still runs immediately regardless of this flag.")]
    [SerializeField] private bool waitForExternalEnable = false;

    [Header("Debug")]
    [SerializeField] private bool logAnchor = true;

    private bool inputEnabled = true;

    private enum CaptureState { Idle, WaitingForB, Aligned }
    private CaptureState state = CaptureState.Idle;
    private Vector3 capturedA;
    private OVRSpatialAnchor currentAnchor;
    private bool isBusy;

    private async void Start()
    {
        if (waitForExternalEnable)
        {
            inputEnabled = false;
            if (logAnchor) Debug.Log("[Anchor2P] Input gated. Waiting for external EnableInput() call.");
        }
        // Anchor auto-load still runs immediately — we want saved anchors to
        // realign the environment as soon as possible regardless of gating.
        await TryLoadSavedAnchor();
    }

    /// <summary>
    /// Called externally (e.g. by IntroVideoController when the intro ends) to
    /// begin listening for the capture/forget buttons.
    /// </summary>
    public void EnableInput()
    {
        inputEnabled = true;
        if (logAnchor) Debug.Log("[Anchor2P] Input enabled. Capture/forget buttons now active.");
    }

    private void Update()
    {
        if (isBusy || !inputEnabled) return;

        if (PressedNow(captureButton, captureController, editorCaptureKey))
        {
            _ = HandleCapturePress();
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

    private Vector3 GetControllerPosition()
    {
        Transform ctrl = captureController == OVRInput.Controller.LTouch
            ? cameraRig.leftControllerAnchor
            : cameraRig.rightControllerAnchor;
        return ctrl.position;
    }

    // ──────────────────────────────────────────────────────────
    // CAPTURE STATE MACHINE
    // ──────────────────────────────────────────────────────────

    private async Task HandleCapturePress()
    {
        if (cameraRig == null || environmentRoot == null ||
            scanReferencePointA == null || scanReferencePointB == null)
        {
            Debug.LogError("[Anchor2P] Missing references. Assign environmentRoot, scanReferencePointA, scanReferencePointB, cameraRig.");
            return;
        }

        switch (state)
        {
            case CaptureState.Idle:
            case CaptureState.Aligned:
                capturedA = GetControllerPosition();
                state = CaptureState.WaitingForB;
                if (logAnchor) Debug.Log($"[Anchor2P] Point A captured at {capturedA:F2}. Walk to Point B and press again.");
                break;

            case CaptureState.WaitingForB:
                Vector3 capturedB = GetControllerPosition();
                await CreateAnchorFromTwoPoints(capturedA, capturedB);
                state = CaptureState.Aligned;
                break;
        }
    }

    private async Task CreateAnchorFromTwoPoints(Vector3 realA, Vector3 realB)
    {
        isBusy = true;
        try
        {
            if (currentAnchor != null)
            {
                Destroy(currentAnchor.gameObject);
                currentAnchor = null;
            }

            // Anchor pose: position at A, forward axis points toward B.
            Quaternion realRotation = ComputeRotation(realA, realB);

            GameObject anchorGO = new GameObject("EnvSpatialAnchor2P");
            anchorGO.transform.SetPositionAndRotation(realA, realRotation);
            OVRSpatialAnchor sa = anchorGO.AddComponent<OVRSpatialAnchor>();

            while (!sa.Created)
            {
                await Task.Yield();
            }
            if (logAnchor) Debug.Log($"[Anchor2P] Anchor created. UUID={sa.Uuid}, pos={realA:F2}, rot.euler={realRotation.eulerAngles:F1}");

            var saveResult = await sa.SaveAnchorAsync();
            if (saveResult.Success)
            {
                PlayerPrefs.SetString(playerPrefsKey, sa.Uuid.ToString());
                PlayerPrefs.Save();
                if (logAnchor) Debug.Log("[Anchor2P] Saved. Next session aligns automatically.");
            }
            else
            {
                Debug.LogWarning($"[Anchor2P] Save failed: {saveResult.Status}. Alignment works this session only.");
            }

            currentAnchor = sa;
            ApplyAlignment(realA, realRotation);
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
            if (logAnchor) Debug.Log($"[Anchor2P] No saved anchor. Press {captureButton} twice (at A, then at B) to calibrate.");
            return;
        }

        if (!System.Guid.TryParse(saved, out System.Guid uuid))
        {
            Debug.LogWarning($"[Anchor2P] Saved UUID malformed. Clearing.");
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
                Debug.LogWarning($"[Anchor2P] Anchor {uuid} not found on this device. Recalibrate with {captureButton}.");
                return;
            }

            GameObject anchorGO = new GameObject("EnvSpatialAnchor2P (Loaded)");
            OVRSpatialAnchor sa = anchorGO.AddComponent<OVRSpatialAnchor>();

            bool localized = await unbound[0].LocalizeAsync();
            if (!localized)
            {
                Debug.LogWarning("[Anchor2P] Localization failed.");
                Destroy(anchorGO);
                return;
            }
            unbound[0].BindTo(sa);

            int timeout = 600;
            while (!sa.Localized && timeout-- > 0)
            {
                await Task.Yield();
            }
            if (!sa.Localized)
            {
                Debug.LogWarning("[Anchor2P] Anchor never localized.");
                Destroy(anchorGO);
                return;
            }

            currentAnchor = sa;
            ApplyAlignment(sa.transform.position, sa.transform.rotation);
            state = CaptureState.Aligned;

            if (logAnchor) Debug.Log($"[Anchor2P] Loaded and aligned. Press {forgetButton} to reset.");
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
                var r = await currentAnchor.EraseAnchorAsync();
                if (logAnchor) Debug.Log(r.Success ? "[Anchor2P] Erased from device." : $"[Anchor2P] Erase failed: {r.Status}");
                Destroy(currentAnchor.gameObject);
                currentAnchor = null;
            }

            PlayerPrefs.DeleteKey(playerPrefsKey);
            PlayerPrefs.Save();
            state = CaptureState.Idle;

            if (logAnchor) Debug.Log("[Anchor2P] Forgotten. Ready for new calibration.");
        }
        finally
        {
            isBusy = false;
        }
    }

    // ──────────────────────────────────────────────────────────
    // MATH
    // ──────────────────────────────────────────────────────────

    private Quaternion ComputeRotation(Vector3 a, Vector3 b)
    {
        Vector3 dir = b - a;
        if (horizontalRotationOnly) dir.y = 0f;
        if (dir.sqrMagnitude < 1e-6f) return Quaternion.identity;
        return Quaternion.LookRotation(dir.normalized, Vector3.up);
    }

    private void ApplyAlignment(Vector3 anchorPos, Quaternion anchorRot)
    {
        if (environmentRoot == null || scanReferencePointA == null || scanReferencePointB == null) return;

        // Scan-space rotation: A→B direction in scan space.
        Quaternion scanRot = ComputeRotation(scanReferencePointA.position, scanReferencePointB.position);

        // Rotate environment so scan A→B aligns with anchor's forward (real A→B).
        Quaternion deltaRot = anchorRot * Quaternion.Inverse(scanRot);
        if (horizontalRotationOnly)
        {
            // Extract yaw only to avoid tilting.
            Vector3 e = deltaRot.eulerAngles;
            deltaRot = Quaternion.Euler(0f, e.y, 0f);
        }

        // Rotate environment around scanReferencePointA so A stays put.
        environmentRoot.rotation = deltaRot * environmentRoot.rotation;
        Vector3 aAfterRotation = scanReferencePointA.position;  // recomputed via parent transform

        // Translate so A lands on the anchor.
        Vector3 offset = anchorPos - aAfterRotation;
        if (!alignVertical) offset.y = 0f;
        environmentRoot.position += offset;

        if (logAnchor)
        {
            float residual = Vector3.Distance(scanReferencePointB.position, anchorPos + anchorRot * Vector3.forward * Vector3.Distance(scanReferencePointA.position, scanReferencePointB.position));
            Debug.Log($"[Anchor2P] Alignment applied. A→anchor offset was {offset:F3}. " +
                      $"Residual to anchor+forward of |AB|: {residual:F3}m (0 if scan distances match reality).");
        }
    }
}
