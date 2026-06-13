using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public class VideoPlaySimple : MonoBehaviour
{
    [Header("References")]
    public VideoPlayer videoPlayer;
    public Image buttonIcon;

    [Header("Sprites")]
    public Sprite playSprite;
    public Sprite pauseSprite;

    [Header("Video Settings")]
    [SerializeField] private bool playAtStart = false;
    [SerializeField] private string videoURL;   // <- IMPORTANT for WebGL

    private bool isPrepared = false;

    private void Awake()
    {
        if (videoPlayer == null)
        {
            Debug.LogError("VideoPlayer not assigned.");
            return;
        }

        // Force URL mode (important for WebGL)
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = videoURL;

        videoPlayer.playOnAwake = false;
        videoPlayer.waitForFirstFrame = true;

        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.errorReceived += OnVideoError;

        videoPlayer.Prepare();
    }

    private void OnVideoPrepared(VideoPlayer vp)
    {
        isPrepared = true;

        if (playAtStart)
        {
            vp.Play();
            SetPauseIcon();
        }
        else
        {
            vp.Pause();
            SetPlayIcon();
        }
    }

    public void PlayVideo()
    {
        if (!isPrepared) return;

        if (videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
            SetPlayIcon();
        }
        else
        {
            videoPlayer.Play();
            SetPauseIcon();
        }
    }

    private void OnVideoError(VideoPlayer vp, string message)
    {
        Debug.LogError("Video Error: " + message);
    }

    private void SetPlayIcon()
    {
        if (buttonIcon != null && playSprite != null)
            buttonIcon.sprite = playSprite;
    }

    private void SetPauseIcon()
    {
        if (buttonIcon != null && pauseSprite != null)
            buttonIcon.sprite = pauseSprite;
    }
}