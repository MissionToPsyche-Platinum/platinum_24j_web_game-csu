using UnityEngine;
using UnityEngine.UI;

public class OptionsMenuUI : MonoBehaviour
{
    [Header("Background")]
    [SerializeField] private RawImage backgroundImage;

    [Header("UI References")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Button returnButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Text optionsTitleText;

    [Header("Tabs")]
    [SerializeField] private Button audioTabButton;
    [SerializeField] private Button howToPlayTabButton;
    [SerializeField] private GameObject[] audioTabObjects;
    [SerializeField] private GameObject howToPlayPanel;

    [Header("How To Play Page")]
    [SerializeField] private Color htpBackgroundColor = new Color(0.08f, 0.10f, 0.18f, 0.95f);
    [SerializeField] private Color htpBackButtonColor = new Color(0.28f, 0.4f, 0.75f, 1f);

    [Header("Tab Colors")]
    [SerializeField] private Color selectedTabColor = new Color(0.28f, 0.4f, 0.75f, 1f);
    [SerializeField] private Color selectedTabHighlightColor = new Color(0.34f, 0.46f, 0.82f, 1f);
    [SerializeField] private Color selectedTabPressedColor = new Color(0.24f, 0.35f, 0.66f, 1f);
    [SerializeField] private Color unselectedTabColor = new Color(1f, 1f, 1f, 0.92f);
    [SerializeField] private Color unselectedTabHighlightColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color unselectedTabPressedColor = new Color(0.87f, 0.9f, 0.98f, 1f);
    [SerializeField] private Color selectedTabTextColor = Color.white;
    [SerializeField] private Color unselectedTabTextColor = new Color(0.12f, 0.18f, 0.33f, 1f);

    private GameObject _htpPage;
    private bool _htpPageOpen;
    private Font _cachedFont;

    private void Awake()
    {
        AutoBindReferences();
        ConfigureUI();
    }

    private void AutoBindReferences()
    {
        if (musicSlider == null)
            musicSlider = transform.Find("MusicSlider")?.GetComponent<Slider>();

        if (sfxSlider == null)
            sfxSlider = transform.Find("SfxSlider")?.GetComponent<Slider>();

        if (returnButton == null)
            returnButton = transform.Find("ReturnButton")?.GetComponent<Button>();

        if (mainMenuButton == null)
            mainMenuButton = transform.Find("MainMenuButton")?.GetComponent<Button>();

        if (optionsTitleText == null)
            optionsTitleText = transform.Find("OptionsTitle")?.GetComponent<Text>();

        if (backgroundImage == null)
            backgroundImage = transform.Find("Background")?.GetComponent<RawImage>();

        if (audioTabButton == null)
            audioTabButton = transform.Find("AudioTabButton")?.GetComponent<Button>();

        if (howToPlayTabButton == null)
            howToPlayTabButton = transform.Find("HowToPlayTabButton")?.GetComponent<Button>();

        if (howToPlayPanel == null)
            howToPlayPanel = transform.Find("HowToPlayPanel")?.gameObject;

        if (audioTabObjects == null || audioTabObjects.Length == 0)
        {
            audioTabObjects = new[]
            {
                transform.Find("MusicLabel")?.gameObject,
                transform.Find("MusicSlider")?.gameObject,
                transform.Find("SfxLabel")?.gameObject,
                transform.Find("SfxSlider")?.gameObject
            };
        }
    }

    private void ConfigureUI()
    {
        if (musicSlider != null)
        {
            musicSlider.SetValueWithoutNotify(AudioSettingsStore.MusicVolume);
            musicSlider.onValueChanged.AddListener(HandleMusicChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(AudioSettingsStore.SfxVolume);
            sfxSlider.onValueChanged.AddListener(HandleSfxChanged);
        }

        if (returnButton != null)
            returnButton.onClick.AddListener(HandleReturnClicked);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(HandleMainMenuClicked);

        if (audioTabButton != null)
            audioTabButton.onClick.AddListener(HandleAudioTabClicked);

        if (howToPlayTabButton != null)
            howToPlayTabButton.onClick.AddListener(HandleHowToPlayTabClicked);

        if (howToPlayPanel != null)
            howToPlayPanel.SetActive(false);

        BuildHowToPlayPage();
        ShowOptionsView();
    }

    private void OnDestroy()
    {
        if (musicSlider != null)
            musicSlider.onValueChanged.RemoveListener(HandleMusicChanged);

        if (sfxSlider != null)
            sfxSlider.onValueChanged.RemoveListener(HandleSfxChanged);

        if (returnButton != null)
            returnButton.onClick.RemoveListener(HandleReturnClicked);

        if (mainMenuButton != null)
            mainMenuButton.onClick.RemoveListener(HandleMainMenuClicked);

        if (audioTabButton != null)
            audioTabButton.onClick.RemoveListener(HandleAudioTabClicked);

        if (howToPlayTabButton != null)
            howToPlayTabButton.onClick.RemoveListener(HandleHowToPlayTabClicked);
    }

    // ── Build full-screen How To Play page at runtime ──

    private void BuildHowToPlayPage()
    {
        _htpPage = new GameObject("HowToPlayPage");
        _htpPage.transform.SetParent(transform, false);

        // Match the options canvas layout: center-anchored, fixed pixel size.
        // The existing options UI spans roughly 760w × 760h centered on the canvas.
        const float panelW = 760f;
        const float panelH = 760f;

        RectTransform pageRect = _htpPage.AddComponent<RectTransform>();
        pageRect.anchorMin = new Vector2(0.5f, 0.5f);
        pageRect.anchorMax = new Vector2(0.5f, 0.5f);
        pageRect.pivot = new Vector2(0.5f, 0.5f);
        pageRect.sizeDelta = new Vector2(panelW, panelH);
        pageRect.anchoredPosition = Vector2.zero;

        Image pageBg = _htpPage.AddComponent<Image>();
        pageBg.color = htpBackgroundColor;
        pageBg.raycastTarget = true;

        // ── Title (top of panel) ──
        GameObject titleObj = CreateTextObject("HTP_Title", _htpPage.transform,
            "How To Play", 34, FontStyle.Bold, TextAnchor.MiddleCenter);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.sizeDelta = new Vector2(panelW - 40f, 70f);
        titleRect.anchoredPosition = new Vector2(0f, panelH * 0.5f - 50f);

        // ── Scrollable area (fills the middle) ──
        GameObject scrollObj = new GameObject("HTP_Scroll");
        scrollObj.transform.SetParent(_htpPage.transform, false);

        float scrollTop = panelH * 0.5f - 95f;
        float scrollBottom = -panelH * 0.5f + 75f;
        float scrollH = scrollTop - scrollBottom;
        float scrollY = (scrollTop + scrollBottom) * 0.5f;

        RectTransform scrollRectTransform = scrollObj.AddComponent<RectTransform>();
        scrollRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        scrollRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        scrollRectTransform.pivot = new Vector2(0.5f, 0.5f);
        scrollRectTransform.sizeDelta = new Vector2(panelW - 60f, scrollH);
        scrollRectTransform.anchoredPosition = new Vector2(0f, scrollY);

        Image scrollBg = scrollObj.AddComponent<Image>();
        scrollBg.color = new Color(0f, 0f, 0f, 0.3f);
        scrollBg.raycastTarget = true;

        ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;

        Mask mask = scrollObj.AddComponent<Mask>();
        mask.showMaskGraphic = true;

        // ── Content container inside scroll ──
        GameObject contentObj = new GameObject("HTP_Content");
        contentObj.transform.SetParent(scrollObj.transform, false);

        RectTransform contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        ContentSizeFitter fitter = contentObj.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.content = contentRect;
        scroll.viewport = scrollRectTransform;

        // ── Body text ──
        string bodyText = GetHowToPlayText();

        GameObject bodyObj = CreateTextObject("HTP_Body", contentObj.transform,
            bodyText, 22, FontStyle.Normal, TextAnchor.UpperLeft);
        RectTransform bodyRect = bodyObj.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0f, 1f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.pivot = new Vector2(0.5f, 1f);
        bodyRect.offsetMin = new Vector2(16f, 0f);
        bodyRect.offsetMax = new Vector2(-16f, 0f);

        ContentSizeFitter bodyFitter = bodyObj.AddComponent<ContentSizeFitter>();
        bodyFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ── Back button (bottom of panel) ──
        GameObject backBtnObj = new GameObject("HTP_BackButton");
        backBtnObj.transform.SetParent(_htpPage.transform, false);

        RectTransform backRect = backBtnObj.AddComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0.5f, 0.5f);
        backRect.anchorMax = new Vector2(0.5f, 0.5f);
        backRect.pivot = new Vector2(0.5f, 0.5f);
        backRect.sizeDelta = new Vector2(220f, 56f);
        backRect.anchoredPosition = new Vector2(0f, -panelH * 0.5f + 40f);

        Image backBg = backBtnObj.AddComponent<Image>();
        backBg.color = htpBackButtonColor;

        Button backBtn = backBtnObj.AddComponent<Button>();
        ColorBlock cb = backBtn.colors;
        cb.normalColor = htpBackButtonColor;
        cb.highlightedColor = new Color(
            Mathf.Min(htpBackButtonColor.r + 0.08f, 1f),
            Mathf.Min(htpBackButtonColor.g + 0.08f, 1f),
            Mathf.Min(htpBackButtonColor.b + 0.08f, 1f), 1f);
        cb.pressedColor = new Color(
            Mathf.Max(htpBackButtonColor.r - 0.06f, 0f),
            Mathf.Max(htpBackButtonColor.g - 0.06f, 0f),
            Mathf.Max(htpBackButtonColor.b - 0.06f, 0f), 1f);
        cb.selectedColor = cb.highlightedColor;
        backBtn.colors = cb;
        backBtn.targetGraphic = backBg;
        backBtn.onClick.AddListener(CloseHowToPlayPage);

        CreateTextObject("BackLabel", backBtnObj.transform,
            "Back", 22, FontStyle.Bold, TextAnchor.MiddleCenter);

        _htpPage.SetActive(false);
    }

    private string GetHowToPlayText()
    {
        if (howToPlayPanel != null)
        {
            Transform bodyTransform = howToPlayPanel.transform.Find("HowToPlayBody");
            if (bodyTransform != null)
            {
                Text bodyText = bodyTransform.GetComponent<Text>();
                if (bodyText != null && !string.IsNullOrEmpty(bodyText.text))
                    return bodyText.text;
            }

            Text fallbackText = howToPlayPanel.GetComponentInChildren<Text>(true);
            if (fallbackText != null && !string.IsNullOrEmpty(fallbackText.text))
                return fallbackText.text;
        }

        return
            "Your goal in each Psyche encounter is to complete the mission objective before you " +
            "run out of turns or Time. Check the encounter panel to track the current objective, " +
            "your progress, and the number of turns remaining.\n\n" +
            "At the start of your turn, your Power and Budget refresh. Drag cards from your hand " +
            "into the play area, but only if you can afford their Power, Budget, and Time costs.\n\n" +
            "Resource cards build momentum by giving you more Power, Budget, or Time. Instrument " +
            "cards collect science data. Maneuver cards improve your position, set up stronger " +
            "combos, or protect you from penalties. Analysis cards convert the data you gathered " +
            "into larger mission progress.\n\n" +
            "When you are finished, press End Turn. The AI acts next, then the encounter advances " +
            "and passive Time drain is applied. Tougher encounters can drain Time faster, so " +
            "efficient turns are important.\n\n" +
            "The best rounds usually come from chaining support cards into data collection and " +
            "then cashing that data in with analysis plays before the turn ends.";
    }

    private Font ResolveFont()
    {
        if (_cachedFont != null)
            return _cachedFont;

        Text existingText = GetComponentInChildren<Text>(true);
        if (existingText != null && existingText.font != null)
        {
            _cachedFont = existingText.font;
            return _cachedFont;
        }

        _cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (_cachedFont == null)
            _cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (_cachedFont == null)
            _cachedFont = Font.CreateDynamicFontFromOSFont("Arial", 14);

        return _cachedFont;
    }

    private GameObject CreateTextObject(string name, Transform parent, string content,
        int fontSize, FontStyle style, TextAnchor alignment)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Text text = obj.AddComponent<Text>();
        text.text = content;
        text.font = ResolveFont();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        text.lineSpacing = 1.2f;

        return obj;
    }

    // ── View switching ──

    private void ShowOptionsView()
    {
        _htpPageOpen = false;
        if (_htpPage != null) _htpPage.SetActive(false);

        SetOptionsElementsActive(true);

        if (optionsTitleText != null)
            optionsTitleText.text = "Audio Settings";

        ApplyTabColors(audioTabButton, true);
        ApplyTabColors(howToPlayTabButton, false);
    }

    private void OpenHowToPlayPage()
    {
        _htpPageOpen = true;
        SetOptionsElementsActive(false);

        if (_htpPage != null)
        {
            _htpPage.SetActive(true);
            _htpPage.transform.SetAsLastSibling();
        }
    }

    private void CloseHowToPlayPage()
    {
        ShowOptionsView();
    }

    private void SetOptionsElementsActive(bool active)
    {
        if (audioTabObjects != null)
        {
            foreach (GameObject obj in audioTabObjects)
            {
                if (obj != null)
                    obj.SetActive(active);
            }
        }

        if (audioTabButton != null)
            audioTabButton.gameObject.SetActive(active);

        if (howToPlayTabButton != null)
            howToPlayTabButton.gameObject.SetActive(active);

        if (optionsTitleText != null)
            optionsTitleText.gameObject.SetActive(active);

        if (returnButton != null)
            returnButton.gameObject.SetActive(active);

        if (mainMenuButton != null)
            mainMenuButton.gameObject.SetActive(active);

        if (howToPlayPanel != null)
            howToPlayPanel.SetActive(false);
    }

    // ── Event Handlers ──

    private void HandleMusicChanged(float value)
    {
        AudioSettingsStore.MusicVolume = value;
    }

    private void HandleSfxChanged(float value)
    {
        AudioSettingsStore.SfxVolume = value;
    }

    private void HandleReturnClicked()
    {
        if (_htpPageOpen)
        {
            CloseHowToPlayPage();
            return;
        }

        if (OptionsOverlayController.HideIfVisible())
            return;
        OptionsNavigation.ReturnToPreviousOrFallback();
    }

    private void HandleMainMenuClicked()
    {
        if (_htpPageOpen)
        {
            CloseHowToPlayPage();
            return;
        }

        if (OptionsOverlayController.HideAndShowMainMenu())
            return;
        OptionsNavigation.LoadMainMenu();
    }

    private void HandleAudioTabClicked()
    {
        if (_htpPageOpen)
            CloseHowToPlayPage();
    }

    private void HandleHowToPlayTabClicked()
    {
        OpenHowToPlayPage();
    }

    private void ApplyTabColors(Button button, bool isSelected)
    {
        if (button == null)
            return;

        ColorBlock colors = button.colors;

        if (isSelected)
        {
            colors.normalColor = selectedTabColor;
            colors.highlightedColor = selectedTabHighlightColor;
            colors.pressedColor = selectedTabPressedColor;
            colors.selectedColor = selectedTabHighlightColor;
        }
        else
        {
            colors.normalColor = unselectedTabColor;
            colors.highlightedColor = unselectedTabHighlightColor;
            colors.pressedColor = unselectedTabPressedColor;
            colors.selectedColor = unselectedTabHighlightColor;
        }

        button.colors = colors;
        UpdateTabLabel(button, isSelected ? selectedTabTextColor : unselectedTabTextColor);
    }

    private void UpdateTabLabel(Button button, Color labelColor)
    {
        Text label = button != null ? button.GetComponentInChildren<Text>(true) : null;
        if (label != null)
            label.color = labelColor;
    }
}
