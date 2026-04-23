using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// Plays an intro video on game start, hides the HUD until the video finishes,
/// then reveals the HUD for gameplay. Supports user skip via controller button or keyboard.
/// </summary>
public class IntroVideoController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Video player that will play the intro.")]
    [SerializeField] private VideoPlayer videoPlayer;
    [Tooltip("The Canvas / GameObject that contains the video display (will be hidden after playback).")]
    [SerializeField] private GameObject videoCanvas;
    [Tooltip("The HUD Canvas (OreHUD_Canvas). Will be hidden until intro finishes.")]
    [SerializeField] private GameObject hudCanvas;

    [Header("Settings")]
    [Tooltip("If true, the intro starts automatically when the scene loads.")]
    [SerializeField] private bool autoPlayOnStart = true;
    [Tooltip("If true, player can press the skip button/key to end the intro early.")]
    [SerializeField] private bool allowSkip = true;
    [SerializeField] private OVRInput.Button skipButton = OVRInput.Button.One; // A button on RTouch
    [SerializeField] private OVRInput.Controller skipController = OVRInput.Controller.RTouch;
    [SerializeField] private KeyCode editorSkipKey = KeyCode.Space;

    [Header("Debug")]
    [SerializeField] private bool logIntro = true;

    private bool introPlaying = false;

    private void Start()
    {
        // HUD starts hidden so the player only sees the intro video.
        if (hudCanvas != null)
        {
            hudCanvas.SetActive(false);
        }

        if (autoPlayOnStart && videoPlayer != null)
        {
            StartIntro();
        }
        else
        {
            // No intro configured — reveal HUD immediately so gameplay still works.
            RevealHUD();
        }
    }

    private void StartIntro()
    {
        if (videoCanvas != null)
        {
            videoCanvas.SetActive(true);
        }

        introPlaying = true;

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.loopPointReached += OnVideoEnded;
        videoPlayer.errorReceived += OnVideoError;
        videoPlayer.Play();

        if (logIntro)
        {
            Debug.Log("[IntroVideo] Intro started. HUD hidden until video finishes.");
        }
    }

    private void OnVideoEnded(VideoPlayer vp)
    {
        if (logIntro)
        {
            Debug.Log("[IntroVideo] Video finished naturally.");
        }
        EndIntro();
    }

    private void OnVideoError(VideoPlayer vp, string message)
    {
        Debug.LogError($"[IntroVideo] Video error: {message}. Skipping intro so gameplay can proceed.");
        EndIntro();
    }

    private void Update()
    {
        if (!introPlaying || !allowSkip)
        {
            return;
        }

        bool skipped = OVRInput.GetDown(skipButton, skipController) ||
                       (Application.isEditor && Input.GetKeyDown(editorSkipKey));

        if (skipped)
        {
            if (logIntro)
            {
                Debug.Log("[IntroVideo] Skipped by player input.");
            }
            if (videoPlayer != null)
            {
                videoPlayer.Stop();
            }
            EndIntro();
        }
    }

    private void EndIntro()
    {
        if (!introPlaying)
        {
            return;
        }
        introPlaying = false;

        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoEnded;
            videoPlayer.errorReceived -= OnVideoError;
        }

        if (videoCanvas != null)
        {
            videoCanvas.SetActive(false);
        }

        RevealHUD();
    }

    private void RevealHUD()
    {
        if (hudCanvas != null)
        {
            hudCanvas.SetActive(true);
        }
        if (logIntro)
        {
            Debug.Log("[IntroVideo] HUD revealed. Gameplay ready.");
        }
    }
}
