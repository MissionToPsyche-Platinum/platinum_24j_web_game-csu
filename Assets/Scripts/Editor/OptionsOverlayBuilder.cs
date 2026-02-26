using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Builds the OptionsOverlay panel inside the current scene,
/// replicating the exact layout from the original Options scene.
/// Run via: Tools > Build Options Overlay
/// </summary>
public static class OptionsOverlayBuilder
{
    private const string ButtonSpritePath = "Assets/Art/button_rectangle.asset";
    private const string FontPath = "Assets/Imports/kenney_ui-pack-space-expansion/Font/Kenney Future.ttf";
    private const string FontSdfPath = "Assets/TextMesh Pro/Fonts/Kenney Future SDF.asset";

    [MenuItem("Tools/Build Options Overlay")]
    public static void Build()
    {
        // Load assets
        Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
        TMP_FontAsset fontSdf = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontSdfPath);
        Sprite btnSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ButtonSpritePath);

        // Destroy old overlay if it exists
        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == "OptionsOverlay")
            {
                Undo.DestroyObjectImmediate(root);
                break;
            }
        }

        // --- Root Canvas: OptionsOverlay ---
        GameObject overlay = new GameObject("OptionsOverlay");
        Undo.RegisterCreatedObjectUndo(overlay, "Create OptionsOverlay");
        overlay.layer = LayerMask.NameToLayer("UI");

        var canvas = overlay.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // Always on top

        var scaler = overlay.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

        overlay.AddComponent<GraphicRaycaster>();
        overlay.AddComponent<OptionsMenuUI>();

        // --- Background (RawImage, fullscreen) ---
        // Matches the original Background object
        GameObject bgGO = CreateChild(overlay, "Background");
        var bgRT = bgGO.GetComponent<RectTransform>();
        SetStretchFill(bgRT);
        bgGO.AddComponent<CanvasRenderer>();
        var rawImg = bgGO.AddComponent<RawImage>();
        rawImg.color = Color.clear; // Original has no texture assigned, transparent backdrop
        rawImg.raycastTarget = true;

        // --- OptionsPanel (centered semi-transparent box) ---
        // Original: 760x520, centered, black with alpha 0.55
        GameObject panelGO = CreateChild(overlay, "OptionsPanel");
        var panelRT = panelGO.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.anchoredPosition = Vector2.zero;
        panelRT.sizeDelta = new Vector2(760, 520);
        panelGO.AddComponent<CanvasRenderer>();
        var panelImg = panelGO.AddComponent<Image>();
        panelImg.sprite = null;
        panelImg.color = new Color(0f, 0f, 0f, 0.55f);
        panelImg.raycastTarget = true;

        // --- OptionsTitle (UI.Text, 48px, centered, y=350) ---
        // Original: "Options", 520x80, fontSize 48, centered, white, Kenney Future
        CreateLegacyText(overlay, "OptionsTitle", "Options",
            font, 48, TextAnchor.MiddleCenter,
            new Vector2(0, 350), new Vector2(520, 80));

        // --- MusicLabel (UI.Text, 28px, centered, y=135) ---
        CreateLegacyText(overlay, "MusicLabel", "Music Volume",
            font, 28, TextAnchor.MiddleCenter,
            new Vector2(0, 135), new Vector2(520, 64));

        // --- MusicSlider (y=75, 520x42) ---
        // Original: bg color white 15% alpha, standard Unity slider children
        CreateSlider(overlay, "MusicSlider",
            new Vector2(0, 75), new Vector2(520, 42));

        // --- SfxLabel (UI.Text, 28px, centered, y=-40) ---
        CreateLegacyText(overlay, "SfxLabel", "SFX Volume",
            font, 28, TextAnchor.MiddleCenter,
            new Vector2(0, -40), new Vector2(520, 64));

        // --- SfxSlider (y=-90, 520x42) ---
        CreateSlider(overlay, "SfxSlider",
            new Vector2(0, -90), new Vector2(520, 42));

        // --- ReturnButton (280x72, y=-250, button_rectangle, blue tint) ---
        // Original: color (0.28, 0.40, 0.75, 1.0)
        GameObject btnGO = CreateChild(overlay, "ReturnButton");
        var btnRT = btnGO.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0.5f, 0.5f);
        btnRT.anchorMax = new Vector2(0.5f, 0.5f);
        btnRT.pivot = new Vector2(0.5f, 0.5f);
        btnRT.anchoredPosition = new Vector2(0, -250);
        btnRT.sizeDelta = new Vector2(280, 72);

        btnGO.AddComponent<CanvasRenderer>();
        var btnImg = btnGO.AddComponent<Image>();
        btnImg.sprite = btnSprite;
        btnImg.color = new Color(0.28f, 0.40f, 0.75f, 1.0f);
        btnImg.raycastTarget = true;

        var btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = btnImg;

        // Button label: "Return" using legacy Text to match original
        CreateLegacyText(btnGO, "Text", "Return",
            font, 28, TextAnchor.MiddleCenter,
            Vector2.zero, Vector2.zero, true);

        // --- Start disabled ---
        overlay.SetActive(false);

        // Save scene
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[OptionsOverlayBuilder] Options overlay panel created and saved (starts disabled).");
    }

    // --- Helpers ---

    private static GameObject CreateChild(GameObject parent, string name)
    {
        GameObject go = new GameObject(name);
        go.AddComponent<RectTransform>();
        go.transform.SetParent(parent.transform, false);
        go.layer = LayerMask.NameToLayer("UI");
        return go;
    }

    private static void SetStretchFill(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void CreateLegacyText(GameObject parent, string name, string text,
        Font font, int fontSize, TextAnchor alignment,
        Vector2 anchoredPos, Vector2 sizeDelta, bool stretchFill = false)
    {
        GameObject go = CreateChild(parent, name);
        var rt = go.GetComponent<RectTransform>();
        if (stretchFill)
        {
            SetStretchFill(rt);
        }
        else
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
        }

        go.AddComponent<CanvasRenderer>();
        var txt = go.AddComponent<Text>();
        txt.text = text;
        txt.font = font;
        txt.fontSize = fontSize;
        txt.alignment = alignment;
        txt.color = Color.white;
        txt.raycastTarget = true;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow = VerticalWrapMode.Truncate;
    }

    private static void CreateSlider(GameObject parent, string sliderName,
        Vector2 anchoredPos, Vector2 sizeDelta)
    {
        // Main slider object with background image
        GameObject sliderGO = CreateChild(parent, sliderName);
        var sliderRT = sliderGO.GetComponent<RectTransform>();
        sliderRT.anchorMin = new Vector2(0.5f, 0.5f);
        sliderRT.anchorMax = new Vector2(0.5f, 0.5f);
        sliderRT.pivot = new Vector2(0.5f, 0.5f);
        sliderRT.anchoredPosition = anchoredPos;
        sliderRT.sizeDelta = sizeDelta;

        sliderGO.AddComponent<CanvasRenderer>();
        var bgImg = sliderGO.AddComponent<Image>();
        bgImg.sprite = null;
        bgImg.color = new Color(1f, 1f, 1f, 0.15f); // Original bg color
        bgImg.raycastTarget = true;

        // Fill Area
        GameObject fillArea = CreateChild(sliderGO, "Fill Area");
        var fillAreaRT = fillArea.GetComponent<RectTransform>();
        fillAreaRT.anchorMin = new Vector2(0, 0.25f);
        fillAreaRT.anchorMax = new Vector2(1, 0.75f);
        fillAreaRT.offsetMin = new Vector2(5, 0);
        fillAreaRT.offsetMax = new Vector2(-15, 0);

        // Fill
        GameObject fill = CreateChild(fillArea, "Fill");
        var fillRT = fill.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.sizeDelta = new Vector2(10, 0);
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;

        fill.AddComponent<CanvasRenderer>();
        var fillImg = fill.AddComponent<Image>();
        fillImg.sprite = null;
        fillImg.color = new Color(0.28f, 0.40f, 0.75f, 1f);

        // Handle Slide Area
        GameObject handleArea = CreateChild(sliderGO, "Handle Slide Area");
        var handleAreaRT = handleArea.GetComponent<RectTransform>();
        handleAreaRT.anchorMin = Vector2.zero;
        handleAreaRT.anchorMax = Vector2.one;
        handleAreaRT.offsetMin = new Vector2(10, 0);
        handleAreaRT.offsetMax = new Vector2(-10, 0);

        // Handle
        GameObject handle = CreateChild(handleArea, "Handle");
        var handleRT = handle.GetComponent<RectTransform>();
        SetStretchFill(handleRT);
        handleRT.sizeDelta = new Vector2(20, 0);

        handle.AddComponent<CanvasRenderer>();
        var handleImg = handle.AddComponent<Image>();
        handleImg.sprite = null;
        handleImg.color = Color.white;

        // Slider component
        var slider = sliderGO.AddComponent<Slider>();
        slider.fillRect = fillRT;
        slider.handleRect = handleRT;
        slider.targetGraphic = handleImg;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0;
        slider.maxValue = 1;
        slider.value = 1;

        // Match original transition colors
        var colors = slider.colors;
        colors.normalColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        colors.highlightedColor = Color.white;
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        slider.colors = colors;
    }
}
