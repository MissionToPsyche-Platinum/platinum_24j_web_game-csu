using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Plays a video as a full-screen UI background (e.g. Psyche asteroid rotation).
/// Place this on a child of MainMenu_Panel so it only shows on the main menu;
/// when you switch to Options / View Cards / Start Game, the panel is hidden and the video stops.
/// Assign either a VideoClip (in-project) or set a direct .mp4 URL for WebGL.
/// Note: YouTube links cannot be used directly; use a downloaded/hosted .mp4 file.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class MenuVideoBackground : MonoBehaviour
{
    [Header("Video Source (use one)")]
    [Tooltip("Video file from your project (e.g. in StreamingAssets or Assets).")]
    public VideoClip videoClip;

    [Tooltip("Direct URL to an .mp4 file (e.g. for WebGL). Not a YouTube page URL.")]
    public string videoUrl;

    [Header("Playback")]
    [Tooltip("Loop the video.")]
    public bool loop = true;

    [Tooltip("Mute video audio.")]
    public bool mute = true;

    private VideoPlayer _videoPlayer;
    private RenderTexture _renderTexture;
    private RawImage _rawImage;
    private int _framesApplied;

    private void Start()
    {
        ForceStretch();
        transform.SetAsFirstSibling(); // Draw behind title and buttons
        SetupDisplay();
        if (videoClip == null && string.IsNullOrEmpty(videoUrl))
            return;
        SetupVideoPlayer();
    }

    private void OnEnable()
    {
        ForceStretch();
        if (_videoPlayer != null && (_videoPlayer.clip != null || !string.IsNullOrEmpty(_videoPlayer.url)))
            _videoPlayer.Play();
    }

    private void LateUpdate()
    {
        if (_framesApplied < 4)
        {
            ForceStretch();
            _framesApplied++;
        }
    }

    private void OnDisable()
    {
        if (_videoPlayer != null)
            _videoPlayer.Pause();
    }

    private void OnDestroy()
    {
        if (_renderTexture != null)
        {
            _renderTexture.Release();
            _renderTexture = null;
        }
    }

    /// <summary>
    /// Force this object to fill its parent (full stretch). Call from Start/OnEnable and a few LateUpdates so it sticks.
    /// </summary>
    private void ForceStretch()
    {
        RectTransform rt = GetComponent<RectTransform>();
        if (rt == null) return;

        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
        rt.anchoredPosition = Vector2.zero;
    }

    private void SetupDisplay()
    {
        _rawImage = GetComponent<RawImage>();
        if (_rawImage == null)
            _rawImage = gameObject.AddComponent<RawImage>();

        _rawImage.color = Color.white;

        ForceStretch();
    }

    private void SetupVideoPlayer()
    {
        _videoPlayer = GetComponent<VideoPlayer>();
        if (_videoPlayer == null)
            _videoPlayer = gameObject.AddComponent<VideoPlayer>();

        int width = 1280;
        int height = 720;
        _renderTexture = new RenderTexture(width, height, 0);
        _rawImage.texture = _renderTexture;

        _videoPlayer.targetTexture = _renderTexture;
        _videoPlayer.isLooping = loop;
        _videoPlayer.skipOnDrop = true;

        if (mute)
            _videoPlayer.audioOutputMode = VideoAudioOutputMode.None;

        if (videoClip != null)
        {
            _videoPlayer.source = VideoSource.VideoClip;
            _videoPlayer.clip = videoClip;
        }
        else if (!string.IsNullOrEmpty(videoUrl))
        {
            _videoPlayer.source = VideoSource.Url;
            _videoPlayer.url = videoUrl;
        }

        _videoPlayer.Play();
    }
}
