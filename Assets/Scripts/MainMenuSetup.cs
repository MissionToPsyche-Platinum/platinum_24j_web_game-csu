using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sets up the main menu: title at top center, buttons below, optional background.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class MainMenuSetup : MonoBehaviour
{
    [Header("Content")]
    [Tooltip("Assign the Title_Text object you created under MainMenu_Panel (UI → Text). If set, menuTitle is applied at runtime.")]
    public RectTransform titleTextRef;
    [Tooltip("Text to show in the title. Only used if titleTextRef is assigned.")]
    public string menuTitle = "Psyche Mission Strategy";

    [Header("Sprites (Optional)")]
    public Sprite buttonSprite;
    [Tooltip("Background image (e.g. Psyche asteroid). For video background instead, add MenuVideoBackground to Background_Layer.")]
    public Sprite backgroundImage;

    private int _framesApplied;

    private void Start()
    {
        SetupMenuPanel();
        EnsureVideoBackgroundIsBehind();
        SetupTitle();
        SetupButtons();
        SetupBackground();
        PositionTitleAndButtons();
    }

    private void LateUpdate()
    {
        if (_framesApplied < 3)
        {
            PositionTitleAndButtons();
            _framesApplied++;
        }
    }

    private void SetupMenuPanel()
    {
        RectTransform panelRect = GetComponent<RectTransform>();
        // Full-screen so video background child can stretch to fit the screen
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        panelRect.sizeDelta = Vector2.zero;

        var layoutGroup = GetComponent<VerticalLayoutGroup>();
        if (layoutGroup != null)
            layoutGroup.enabled = false;
    }

    /// <summary>
    /// Configures the title from an existing Title_Text you added in the editor. Does not create it.
    /// Removes Button/Image from the title so it is text-only (no opaque box).
    /// </summary>
    private void SetupTitle()
    {
        Transform titleT = titleTextRef != null ? titleTextRef : transform.Find("Title_Text");
        if (titleT == null) return;

        // So it's text-only: remove Button and hide/remove Image so there's no "strange button" look
        var btn = titleT.GetComponent<Button>();
        if (btn != null) Destroy(btn);
        var img = titleT.GetComponent<Image>();
        if (img != null)
        {
            img.color = new Color(1f, 1f, 1f, 0f);
            img.raycastTarget = false;
        }

        Text titleText = titleT.GetComponent<Text>();
        if (titleText == null) titleText = titleT.gameObject.AddComponent<Text>();

        titleText.text = string.IsNullOrEmpty(menuTitle) ? "Psyche Mission Strategy" : menuTitle;
        titleText.fontSize = 42;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = Color.white;
        if (titleText.font == null)
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        RectTransform titleRect = titleT as RectTransform;
        if (titleRect != null)
        {
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(480, 70);
            titleRect.anchoredPosition = new Vector2(0f, -20f);
        }
    }

    private void EnsureVideoBackgroundIsBehind()
    {
        // So the video draws behind title and buttons, move it to first sibling
        var video = GetComponentInChildren<MenuVideoBackground>(includeInactive: false);
        if (video != null)
            video.transform.SetAsFirstSibling();
        // Make panel background transparent so video is visible
        var panelImage = GetComponent<Image>();
        if (panelImage != null && video != null)
            panelImage.color = new Color(1f, 1f, 1f, 0f);
    }

    private void PositionTitleAndButtons()
    {
        Transform titleT = titleTextRef != null ? titleTextRef : transform.Find("Title_Text");
        if (titleT != null)
        {
            var titleRect = titleT.GetComponent<RectTransform>();
            if (titleRect != null)
            {
                titleRect.anchorMin = new Vector2(0.5f, 1f);
                titleRect.anchorMax = new Vector2(0.5f, 1f);
                titleRect.pivot = new Vector2(0.5f, 1f);
                titleRect.anchoredPosition = new Vector2(0f, -20f);
            }
        }

        // Only position real buttons (exclude title if it was mistakenly a button)
        Button[] buttons = GetComponentsInChildren<Button>(includeInactive: false);
        if (buttons == null || buttons.Length == 0) return;

        float buttonSpacing = 88f;
        float firstButtonY = 40f;
        int index = 0;
        for (int i = 0; i < buttons.Length; i++)
        {
            // Skip the title (in case it still has a Button for a frame)
            if (titleT != null && buttons[i].transform == titleT) continue;

            RectTransform rt = buttons[i].GetComponent<RectTransform>();
            if (rt == null) continue;

            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(300, 80);
            rt.anchoredPosition = new Vector2(0f, firstButtonY - index * buttonSpacing);
            index++;
        }
    }

    private void SetupBackground()
    {
        if (backgroundImage == null) return;
        Image panelImage = GetComponent<Image>();
        if (panelImage != null)
        {
            panelImage.sprite = backgroundImage;
            panelImage.color = Color.white;
            panelImage.type = Image.Type.Simple;
        }
    }

    private void SetupButtons()
    {
        if (buttonSprite == null)
            buttonSprite = FindSpriteByName("button_square_header_large_rectangle");

        Button[] buttons = GetComponentsInChildren<Button>();
        foreach (Button button in buttons)
        {
            RectTransform buttonRect = button.GetComponent<RectTransform>();
            if (buttonRect == null) continue;

            buttonRect.sizeDelta = new Vector2(300, 80);

            Image buttonImage = button.GetComponent<Image>();
            if (buttonImage != null)
            {
                if (buttonSprite != null)
                {
                    buttonImage.sprite = buttonSprite;
                    buttonImage.type = Image.Type.Sliced;
                }
                else
                {
                    buttonImage.sprite = null;
                    buttonImage.color = new Color(0.2f, 0.4f, 0.8f, 1f);
                }
            }

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.9f, 0.9f, 1f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 1f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            button.colors = colors;

            Text buttonText = button.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                RectTransform textRect = buttonText.GetComponent<RectTransform>();
                if (textRect != null)
                {
                    textRect.anchorMin = Vector2.zero;
                    textRect.anchorMax = Vector2.one;
                    textRect.sizeDelta = Vector2.zero;
                    textRect.anchoredPosition = Vector2.zero;
                }
                buttonText.alignment = TextAnchor.MiddleCenter;
                buttonText.fontSize = 28;
                buttonText.color = Color.white;
                buttonText.fontStyle = FontStyle.Bold;
            }
        }
    }

    private Sprite FindSpriteByName(string spriteName)
    {
        Object[] loadedObjects = Resources.FindObjectsOfTypeAll(typeof(Sprite));
        foreach (Object obj in loadedObjects)
        {
            if (obj.name == spriteName)
                return obj as Sprite;
        }
        return null;
    }
}
