#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Builds editable Mission Success / Mission Failure UI under Main_Canvas (inactive by default).
/// </summary>
public static class MissionEndScreenHierarchyBuilder
{
    private const string MenuPath = "Tools/UI/Create Mission End Screens In Hierarchy";

    [MenuItem(MenuPath)]
    public static void CreateInHierarchy()
    {
        var existing = Object.FindFirstObjectByType<MissionEndScreenUI>(FindObjectsInactive.Include);
        if (existing != null)
        {
            EditorUtility.DisplayDialog(
                "Mission End Screens",
                "A MissionEndScreens object already exists in this scene.",
                "OK");
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        var canvasGo = GameObject.Find("Main_Canvas");
        if (canvasGo == null)
        {
            EditorUtility.DisplayDialog(
                "Mission End Screens",
                "Could not find Main_Canvas. Open your game scene (e.g. SampleScene) and try again.",
                "OK");
            return;
        }

        var root = new GameObject("MissionEndScreens");
        Undo.RegisterCreatedObjectUndo(root, "Create Mission End Screens");
        Undo.SetTransformParent(root.transform, canvasGo.transform, "Parent MissionEndScreens");

        var rootRt = root.AddComponent<RectTransform>();
        StretchFull(rootRt);

        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 32000;
        root.AddComponent<GraphicRaycaster>();

        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var bgGo = CreateUiChild(root.transform, "Background");
        StretchFull(bgGo.GetComponent<RectTransform>());
        var bgImg = bgGo.AddComponent<Image>();
        bgImg.color = Color.black;
        bgImg.raycastTarget = true;

        var successBgGo = CreateUiChild(root.transform, "SuccessBackground");
        StretchFull(successBgGo.GetComponent<RectTransform>());
        var successBgImg = successBgGo.AddComponent<Image>();
        successBgImg.sprite = LoadSprite("Assets/Resources/MissionEnd/psychewinnobg.png");
        successBgImg.preserveAspect = false;
        successBgImg.raycastTarget = true;

        var failureBgGo = CreateUiChild(root.transform, "FailureBackground");
        StretchFull(failureBgGo.GetComponent<RectTransform>());
        var failureBgImg = failureBgGo.AddComponent<Image>();
        failureBgImg.sprite = LoadSprite("Assets/Resources/MissionEnd/psychelosenobg.png");
        failureBgImg.preserveAspect = false;
        failureBgImg.raycastTarget = true;
        failureBgGo.SetActive(false);

        var winGo = CreateUiChild(root.transform, "SuccessArt");
        CenterMiddle(winGo.GetComponent<RectTransform>(), new Vector2(900f, 500f));
        var winImg = winGo.AddComponent<Image>();
        winImg.sprite = LoadSprite("Assets/Resources/MissionEnd/psychewinnobg.png");
        winImg.preserveAspect = true;

        var loseGo = CreateUiChild(root.transform, "FailureArt");
        CenterMiddle(loseGo.GetComponent<RectTransform>(), new Vector2(900f, 500f));
        var loseImg = loseGo.AddComponent<Image>();
        loseImg.sprite = LoadSprite("Assets/Resources/MissionEnd/psychelosenobg.png");
        loseImg.preserveAspect = true;
        loseGo.SetActive(false);
        var kenney = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/Kenney Future SDF.asset");

        var titleGo = CreateUiChild(root.transform, "OutcomeTitle");
        var titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 1f);
        titleRt.anchorMax = new Vector2(0.5f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -48f);
        titleRt.sizeDelta = new Vector2(1200f, 96f);
        var title = titleGo.AddComponent<TextMeshProUGUI>();
        title.text = "Mission Success!";
        title.alignment = TextAlignmentOptions.Center;
        title.fontSize = 72;
        title.color = Color.white;
        if (kenney != null)
            title.font = kenney;

        var btnGo = CreateUiChild(root.transform, "ReturnButton");
        var btnRt = btnGo.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.5f, 0f);
        btnRt.anchorMax = new Vector2(0.5f, 0f);
        btnRt.pivot = new Vector2(0.5f, 0f);
        btnRt.anchoredPosition = new Vector2(0f, 96f);
        btnRt.sizeDelta = new Vector2(280f, 72f);
        var btnImg = btnGo.AddComponent<Image>();
        btnImg.sprite = LoadSprite("Assets/Resources/MissionEnd/button_rectangle.png");
        btnImg.type = Image.Type.Simple;
        btnImg.color = new Color32(0, 94, 184, 255);
        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.85f, 0.9f, 1f, 1f);
        colors.pressedColor = new Color(0.65f, 0.75f, 0.95f, 1f);
        btn.colors = colors;

        var labelGo = CreateUiChild(btnGo.transform, "Label");
        StretchFull(labelGo.GetComponent<RectTransform>());
        var tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.text = "Return";
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 36;
        tmp.color = Color.white;
        if (kenney != null)
            tmp.font = kenney;

        var ctrl = root.AddComponent<MissionEndScreenUI>();
        var so = new SerializedObject(ctrl);
        so.FindProperty("backgroundImage").objectReferenceValue = bgImg;
        so.FindProperty("successBackgroundImage").objectReferenceValue = successBgImg;
        so.FindProperty("failureBackgroundImage").objectReferenceValue = failureBgImg;
        so.FindProperty("successArtImage").objectReferenceValue = winImg;
        so.FindProperty("failureArtImage").objectReferenceValue = loseImg;
        so.FindProperty("outcomeTitleLabel").objectReferenceValue = title;
        so.FindProperty("returnButton").objectReferenceValue = btn;
        so.FindProperty("returnLabel").objectReferenceValue = tmp;
        so.ApplyModifiedPropertiesWithoutUndo();

        root.transform.SetAsLastSibling();
        root.SetActive(false);

        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"[{nameof(MissionEndScreenHierarchyBuilder)}] Created MissionEndScreens under Main_Canvas. It starts inactive; open the hierarchy to edit.");
    }

    private static GameObject CreateUiChild(Transform parent, string name)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        Undo.SetTransformParent(go.transform, parent, $"Parent {name}");
        go.AddComponent<RectTransform>();
        return go;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    private static void CenterMiddle(RectTransform rt, Vector2 sizeDelta)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = sizeDelta;
    }

    private static Sprite LoadSprite(string assetPath)
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (s != null)
            return s;
        foreach (var a in AssetDatabase.LoadAllAssetsAtPath(assetPath))
        {
            if (a is Sprite sp)
                return sp;
        }
        Debug.LogWarning($"[{nameof(MissionEndScreenHierarchyBuilder)}] No Sprite at {assetPath}. Assign sprites manually on SuccessArt / FailureArt / ReturnButton.");
        return null;
    }
}
#endif
