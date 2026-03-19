using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

/// <summary>
/// Editor utility to rebuild the Options menu sliders with the correct
/// child hierarchy so they can be dragged and wired to audio later.
/// Run via: Tools > Rebuild Options Sliders
/// </summary>
public static class SliderRebuilder
{
    [MenuItem("Tools/Rebuild Options Sliders")]
    public static void RebuildSliders()
    {
        // Find the canvas
        Canvas canvas = GameObject.Find("OptionsCanvas")?.GetComponent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[SliderRebuilder] Could not find 'OptionsCanvas' in the scene. Make sure the Options scene is open.");
            return;
        }

        Transform canvasT = canvas.transform;

        // ---- Music Slider ----
        // Position: centred, y=45 
        Slider musicSlider = BuildSlider(canvasT, "MusicSlider",
            anchoredPos: new Vector2(0f, 45f),
            size: new Vector2(520f, 42f),
            initialValue: AudioSettingsStoreProxy.MusicVolume);

        // ---- SFX Slider ----
        // Position: centred, y=-90
        Slider sfxSlider = BuildSlider(canvasT, "SfxSlider",
            anchoredPos: new Vector2(0f, -90f),
            size: new Vector2(520f, 42f),
            initialValue: AudioSettingsStoreProxy.SfxVolume);

        // Ensure sliders are ordered after labels but before the return button
        Transform returnButton = canvasT.Find("ReturnButton");
        if (returnButton != null)
        {
            int btnIndex = returnButton.GetSiblingIndex();
            musicSlider.transform.SetSiblingIndex(btnIndex);
            sfxSlider.transform.SetSiblingIndex(btnIndex + 1);
        }

        // Ensure Background is at sibling index 0 (renders behind everything)
        Transform bg = canvasT.Find("Background");
        if (bg != null)
            bg.SetSiblingIndex(0);

        // Re-wire OptionsMenuUI references
        OptionsMenuUI optionsUI = canvasT.GetComponent<OptionsMenuUI>();
        if (optionsUI != null)
        {
            SerializedObject so = new SerializedObject(optionsUI);
            so.FindProperty("musicSlider").objectReferenceValue = musicSlider;
            so.FindProperty("sfxSlider").objectReferenceValue = sfxSlider;

            // Wire background image if present
            RawImage bgRaw = canvasT.Find("Background")?.GetComponent<RawImage>();
            if (bgRaw != null)
                so.FindProperty("backgroundImage").objectReferenceValue = bgRaw;

            so.ApplyModifiedProperties();
            Debug.Log("[SliderRebuilder] OptionsMenuUI references updated.");
        }

        EditorUtility.SetDirty(canvas.gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Debug.Log("[SliderRebuilder] Sliders rebuilt successfully. Save the scene to persist changes.");
    }

    // -----------------------------------------------------------------------
    // Builds a fully functional Unity UI Slider with Background, Fill Area,
    // and Handle Slide Area children, then returns the Slider component.
    // -----------------------------------------------------------------------
    private static Slider BuildSlider(Transform parent, string name,
        Vector2 anchoredPos, Vector2 size, float initialValue)
    {
        // Destroy existing object with the same name if any
        Transform existing = parent.Find(name);
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        // --- Root ---
        GameObject root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Slider));
        root.transform.SetParent(parent, false);

        RectTransform rootRT = root.GetComponent<RectTransform>();
        rootRT.anchorMin = new Vector2(0.5f, 0.5f);
        rootRT.anchorMax = new Vector2(0.5f, 0.5f);
        rootRT.pivot = new Vector2(0.5f, 0.5f);
        rootRT.anchoredPosition = anchoredPos;
        rootRT.sizeDelta = size;

        // Background image (semi-transparent dark)
        Image rootImg = root.GetComponent<Image>();
        rootImg.color = new Color(1f, 1f, 1f, 0.15f);
        rootImg.raycastTarget = true;

        // --- Background child (visual track) ---
        GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bg.transform.SetParent(root.transform, false);
        RectTransform bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0f, 0.25f);
        bgRT.anchorMax = new Vector2(1f, 0.75f);
        bgRT.sizeDelta = Vector2.zero;
        bgRT.anchoredPosition = Vector2.zero;
        Image bgImg = bg.GetComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        bgImg.raycastTarget = false;

        // --- Fill Area ---
        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(root.transform, false);
        RectTransform fillAreaRT = fillArea.GetComponent<RectTransform>();
        fillAreaRT.anchorMin = new Vector2(0f, 0.25f);
        fillAreaRT.anchorMax = new Vector2(1f, 0.75f);
        fillAreaRT.offsetMin = new Vector2(5f, 0f);
        fillAreaRT.offsetMax = new Vector2(-15f, 0f);

        // --- Fill ---
        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRT = fill.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = new Vector2(0f, 1f);
        fillRT.sizeDelta = new Vector2(10f, 0f);
        fillRT.anchoredPosition = Vector2.zero;
        Image fillImg = fill.GetComponent<Image>();
        fillImg.color = new Color(0.22f, 0.60f, 0.90f, 1f);  // accent blue
        fillImg.raycastTarget = false;

        // --- Handle Slide Area ---
        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(root.transform, false);
        RectTransform handleAreaRT = handleArea.GetComponent<RectTransform>();
        handleAreaRT.anchorMin = Vector2.zero;
        handleAreaRT.anchorMax = Vector2.one;
        handleAreaRT.offsetMin = new Vector2(10f, 0f);
        handleAreaRT.offsetMax = new Vector2(-10f, 0f);

        // --- Handle ---
        GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform handleRT = handle.GetComponent<RectTransform>();
        handleRT.anchorMin = new Vector2(0f, 0f);
        handleRT.anchorMax = new Vector2(0f, 1f);
        handleRT.sizeDelta = new Vector2(20f, 0f);
        handleRT.anchoredPosition = Vector2.zero;
        Image handleImg = handle.GetComponent<Image>();
        handleImg.color = new Color(0.95f, 0.95f, 0.95f, 1f);
        handleImg.raycastTarget = true;

        // --- Wire Slider ---
        Slider slider = root.GetComponent<Slider>();
        slider.fillRect = fillRT;
        slider.handleRect = handleRT;
        slider.targetGraphic = handleImg;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.value = Mathf.Clamp01(initialValue);

        // Colour tint for interactable feedback
        ColorBlock cb = slider.colors;
        cb.normalColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        cb.highlightedColor = Color.white;
        cb.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        cb.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        slider.colors = cb;

        Debug.Log($"[SliderRebuilder] Built '{name}' with value {slider.value}");
        return slider;
    }

    // Thin proxy so the editor script can read defaults without referencing
    // PlayerPrefs directly (avoids issues if AudioSettingsStore isn't compiled yet).
    private static class AudioSettingsStoreProxy
    {
        public static float MusicVolume => PlayerPrefs.GetFloat("Options.MusicVolume", 0.8f);
        public static float SfxVolume  => PlayerPrefs.GetFloat("Options.SfxVolume",  0.8f);
    }
}
