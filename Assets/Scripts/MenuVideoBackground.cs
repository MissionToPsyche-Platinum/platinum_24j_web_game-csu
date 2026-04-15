using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays a static full-screen image as the main menu background.
/// Replaces the VideoPlayer approach which is unreliable on WebGL.
/// Assign backgroundTexture in the Inspector (e.g. mainmenubackground.webp).
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class MenuVideoBackground : MonoBehaviour
{
    [Header("Background Image")]
    [Tooltip("Texture to display as the menu background (e.g. mainmenubackground.webp).")]
    public Texture2D backgroundTexture;

    private RawImage _rawImage;

    private void Start()
    {
        SetupDisplay();
        ForceStretch();
        transform.SetAsFirstSibling();
    }

    private void OnEnable()
    {
        ForceStretch();
    }

    private void SetupDisplay()
    {
        _rawImage = GetComponent<RawImage>();
        if (_rawImage == null)
            _rawImage = gameObject.AddComponent<RawImage>();

        _rawImage.color = Color.white;

        if (backgroundTexture != null)
            _rawImage.texture = backgroundTexture;
        else
            Debug.LogWarning("[MenuVideoBackground] No backgroundTexture assigned — menu background will be blank.");
    }

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
}
