using System.Collections;
using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// Gates gameplay behind an intro video. Supports two start modes:
///   • Auto      — video plays as soon as the scene loads.
///   • ButtonPress — screen is dark until the player presses the trigger button.
///
/// Includes fade-in / fade-out transitions between the video and the HUD so the
/// handover isn't a hard cut. Requires a CanvasGroup on each canvas (added
/// automatically at runtime if missing).
/// </summary>
public class IntroVideoController : MonoBehaviour
{
    public enum StartMode
    {
        Auto,
        ButtonPress
    }

    [Header("References")]
    [Tooltip("Video player that will play the intro.")]
    [SerializeField] private VideoPlayer videoPlayer;
    [Tooltip("Canvas containing the video RawImage. Alpha is controlled via its CanvasGroup.")]
    [SerializeField] private GameObject videoCanvas;
    [Tooltip("HUD canvas (OreHUD_Canvas). Starts hidden, fades in after the intro.")]
    [SerializeField] private GameObject hudCanvas;

    [Header("Start Mode")]
    [SerializeField] private StartMode startMode = StartMode.ButtonPress;

    [Header("Trigger (Button Press mode)")]
    [Tooltip("Controller button that starts the video.")]
    [SerializeField] private OVRInput.Button triggerButton = OVRInput.Button.PrimaryIndexTrigger;
    [SerializeField] private OVRInput.Controller triggerController = OVRInput.Controller.RTouch;
    [Tooltip("Keyboard key used in the Unity editor as a fallback.")]
    [SerializeField] private KeyCode editorTriggerKey = KeyCode.Space;

    [Header("Skip (during playback)")]
    [SerializeField] private bool allowSkip = true;
    [SerializeField] private OVRInput.Button skipButton = OVRInput.Button.One;
    [SerializeField] private OVRInput.Controller skipController = OVRInput.Controller.RTouch;
    [SerializeField] private KeyCode editorSkipKey = KeyCode.Space;

    [Header("Fade Settings")]
    [Tooltip("Seconds for the video canvas to fade in after trigger / at scene start.")]
    [SerializeField] private float videoFadeInDuration = 0.6f;
    [Tooltip("Seconds for the video canvas to fade out before the HUD appears.")]
    [SerializeField] private float videoFadeOutDuration = 0.8f;
    [Tooltip("Seconds for the HUD to fade in after the video finishes.")]
    [SerializeField] private float hudFadeInDuration = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool logIntro = true;

    private CanvasGroup videoGroup;
    private CanvasGroup hudGroup;
    private bool awaitingTrigger = false;
    private bool playbackStarted = false;
    private bool introFinished = false;

    private void Start()
    {
        // Grab (or create) CanvasGroup so we can fade with alpha instead of popping.
        videoGroup = EnsureCanvasGroup(videoCanvas);
        hudGroup = EnsureCanvasGroup(hudCanvas);

        // HUD starts invisible. We still keep the GameObject active so event
        // subscriptions (OreHUD, etc.) run from the beginning.
        if (hudGroup != null)
        {
            hudGroup.alpha = 0f;
            hudGroup.blocksRaycasts = false;
            hudGroup.interactable = false;
        }

        if (videoCanvas != null)
        {
            videoCanvas.SetActive(true);

            // Defensive: legacy scripts (e.g. FullscreenVideoOnLeftY.Awake) may
            // have disabled the RawImage or its GameObject before our Start ran.
            // Awake fires even for disabled components, so unchecking them in
            // the Inspector isn't enough — we force-enable them here.
            UnityEngine.UI.RawImage[] rawImages = videoCanvas.GetComponentsInChildren<UnityEngine.UI.RawImage>(true);
            foreach (UnityEngine.UI.RawImage ri in rawImages)
            {
                if (ri == null) continue;
                if (!ri.gameObject.activeSelf) ri.gameObject.SetActive(true);
                if (!ri.enabled) ri.enabled = true;
            }
            if (logIntro && rawImages.Length > 0)
            {
                Debug.Log($"[IntroVideo] Ensured {rawImages.Length} RawImage(s) enabled in video canvas.");
            }
        }
        if (videoGroup != null)
        {
            videoGroup.alpha = 0f;
        }

        if (videoPlayer == null)
        {
            Debug.LogWarning("[IntroVideo] No VideoPlayer assigned — skipping intro.");
            RevealHudImmediate();
            return;
        }

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.loopPointReached += OnVideoEnded;
        videoPlayer.errorReceived += OnVideoError;

        if (startMode == StartMode.Auto)
        {
            StartIntro();
        }
        else
        {
            awaitingTrigger = true;
            if (logIntro)
            {
                Debug.Log($"[IntroVideo] Waiting for {triggerButton} (or {editorTriggerKey} in editor) to start intro.");
            }
        }
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoEnded;
            videoPlayer.errorReceived -= OnVideoError;
        }
    }

    private void Update()
    {
        if (awaitingTrigger && TriggerPressed())
        {
            awaitingTrigger = false;
            StartIntro();
            return;
        }

        if (playbackStarted && !introFinished && allowSkip && SkipPressed())
        {
            if (logIntro) Debug.Log("[IntroVideo] Skipped by player input.");
            if (videoPlayer != null) videoPlayer.Stop();
            StartCoroutine(EndIntroRoutine());
        }
    }

    private bool TriggerPressed()
    {
        if (OVRInput.GetDown(triggerButton, triggerController)) return true;
        if (Application.isEditor && Input.GetKeyDown(editorTriggerKey)) return true;
        return false;
    }

    private bool SkipPressed()
    {
        if (OVRInput.GetDown(skipButton, skipController)) return true;
        if (Application.isEditor && Input.GetKeyDown(editorSkipKey)) return true;
        return false;
    }

    private void StartIntro()
    {
        if (playbackStarted) return;
        playbackStarted = true;

        if (logIntro) Debug.Log("[IntroVideo] Starting playback.");
        videoPlayer.Play();
        StartCoroutine(FadeCanvasGroup(videoGroup, 0f, 1f, videoFadeInDuration));
    }

    private void OnVideoEnded(VideoPlayer vp)
    {
        if (introFinished) return;
        if (logIntro) Debug.Log("[IntroVideo] Video finished naturally.");
        StartCoroutine(EndIntroRoutine());
    }

    private void OnVideoError(VideoPlayer vp, string message)
    {
        Debug.LogError($"[IntroVideo] Video error: {message}. Revealing HUD so gameplay can proceed.");
        if (!introFinished)
        {
            StartCoroutine(EndIntroRoutine());
        }
    }

    private IEnumerator EndIntroRoutine()
    {
        if (introFinished) yield break;
        introFinished = true;

        yield return StartCoroutine(FadeCanvasGroup(videoGroup, 1f, 0f, videoFadeOutDuration));

        if (videoCanvas != null)
        {
            videoCanvas.SetActive(false);
        }

        yield return StartCoroutine(FadeCanvasGroup(hudGroup, 0f, 1f, hudFadeInDuration));

        if (hudGroup != null)
        {
            hudGroup.blocksRaycasts = true;
            hudGroup.interactable = true;
        }

        if (logIntro) Debug.Log("[IntroVideo] HUD revealed. Gameplay ready.");
    }

    private void RevealHudImmediate()
    {
        if (videoCanvas != null) videoCanvas.SetActive(false);
        if (hudGroup != null)
        {
            hudGroup.alpha = 1f;
            hudGroup.blocksRaycasts = true;
            hudGroup.interactable = true;
        }
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null || duration <= 0f)
        {
            if (group != null) group.alpha = to;
            yield break;
        }

        float elapsed = 0f;
        group.alpha = from;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        group.alpha = to;
    }

    private CanvasGroup EnsureCanvasGroup(GameObject go)
    {
        if (go == null) return null;
        CanvasGroup cg = go.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = go.AddComponent<CanvasGroup>();
        }
        return cg;
    }
}
