using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Press left-hand Y to show a full-screen video overlay.
/// Press again to hide it.
/// </summary>
public class FullscreenVideoOnLeftY : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private bool useEditorKeyFallback = true;

    [Header("Video")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RawImage fullscreenImage;
    [SerializeField] private CanvasGroup overlayCanvasGroup;
    [SerializeField] private bool hideWhenVideoEnds = true;

    [Header("Render Texture")]
    [SerializeField] private int textureWidth = 1920;
    [SerializeField] private int textureHeight = 1080;

    private RenderTexture runtimeTexture;
    private bool isVisible;
    private bool pendingPlay;

    private void Awake()
    {
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
        }

        if (overlayCanvasGroup == null)
        {
            overlayCanvasGroup = GetComponent<CanvasGroup>();
            if (overlayCanvasGroup == null)
            {
                overlayCanvasGroup = GetComponentInParent<CanvasGroup>();
            }
        }

        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.prepareCompleted += HandlePrepared;
            videoPlayer.loopPointReached += HandleVideoEnded;
            videoPlayer.errorReceived += HandleVideoError;
        }

        EnsureRenderTexture();
        ApplyVideoTexture();

        SetVisible(false);
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= HandlePrepared;
            videoPlayer.loopPointReached -= HandleVideoEnded;
            videoPlayer.errorReceived -= HandleVideoError;
        }

        if (runtimeTexture != null)
        {
            runtimeTexture.Release();
            Destroy(runtimeTexture);
        }
    }

    private void Update()
    {
        if (!WasLeftYPressed())
        {
            return;
        }

        if (isVisible)
        {
            HideVideo();
        }
        else
        {
            ShowVideo();
        }
    }

    private bool WasLeftYPressed()
    {
        // Meta/Oculus mapping: left-hand Y is OVRInput.Button.Two with LTouch.
        if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.LTouch))
        {
            return true;
        }

        if (useEditorKeyFallback && Application.isEditor && Input.GetKeyDown(KeyCode.Y))
        {
            return true;
        }

        return false;
    }

    private void ShowVideo()
    {
        EnsureRenderTexture();
        ApplyVideoTexture();
        SetVisible(true);

        if (videoPlayer == null)
        {
            return;
        }

        pendingPlay = true;
        videoPlayer.Stop();
        videoPlayer.Prepare();
    }

    private void HideVideo()
    {
        pendingPlay = false;

        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }

        SetVisible(false);
    }

    private void HandlePrepared(VideoPlayer player)
    {
        if (!pendingPlay)
        {
            return;
        }

        pendingPlay = false;
        player.Play();
    }

    private void HandleVideoError(VideoPlayer player, string message)
    {
        pendingPlay = false;
        Debug.LogError($"{nameof(FullscreenVideoOnLeftY)} video error on {player.gameObject.name}: {message}");
    }

    private void HandleVideoEnded(VideoPlayer player)
    {
        if (hideWhenVideoEnds)
        {
            HideVideo();
        }
    }

    private void EnsureRenderTexture()
    {
        if (videoPlayer == null)
        {
            return;
        }

        if (runtimeTexture != null)
        {
            return;
        }

        runtimeTexture = new RenderTexture(textureWidth, textureHeight, 0, RenderTextureFormat.ARGB32);
        runtimeTexture.Create();
        videoPlayer.targetTexture = runtimeTexture;
    }

    private void ApplyVideoTexture()
    {
        if (fullscreenImage == null)
        {
            return;
        }

        if (runtimeTexture != null)
        {
            fullscreenImage.texture = runtimeTexture;
        }
        else if (videoPlayer != null)
        {
            fullscreenImage.texture = videoPlayer.texture;
        }

        fullscreenImage.enabled = true;
    }

    private void SetVisible(bool visible)
    {
        isVisible = visible;

        if (overlayCanvasGroup != null)
        {
            overlayCanvasGroup.alpha = visible ? 1f : 0f;
            overlayCanvasGroup.interactable = visible;
            overlayCanvasGroup.blocksRaycasts = visible;
        }

        if (fullscreenImage != null)
        {
            fullscreenImage.enabled = visible;
            fullscreenImage.gameObject.SetActive(visible);
        }
    }
}
