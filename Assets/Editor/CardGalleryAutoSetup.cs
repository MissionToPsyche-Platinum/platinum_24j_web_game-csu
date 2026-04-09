using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class CardGalleryAutoSetup : EditorWindow
{
    [MenuItem("Tools/Auto-Setup Card Gallery")]
    public static void Setup()
    {
        // 1. Find all CardData
        string[] guids = AssetDatabase.FindAssets("t:CardData");
        List<CardData> allCards = new List<CardData>();
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            CardData data = AssetDatabase.LoadAssetAtPath<CardData>(path);
            if (data != null) allCards.Add(data);
        }

        // 2. Edit MainMenu_Panel.prefab to set cardCollectionSceneName
        string prefabPath = "Assets/Prefabs/MainMenu_Panel.prefab";
        GameObject mainMenuPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (mainMenuPrefab != null)
        {
            MainMenuUI menuUI = mainMenuPrefab.GetComponent<MainMenuUI>();
            if (menuUI != null)
            {
                menuUI.cardCollectionSceneName = "CardGallery";
                EditorUtility.SetDirty(menuUI);
                PrefabUtility.SavePrefabAsset(mainMenuPrefab);
                Debug.Log("MainMenuUI updated to point to CardGallery scene.");
            }
        }

        // 3. Setup CardGallery.unity scene
        string scenePath = "Assets/Scenes/CardGallery.unity";
        Scene galleryScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // Delete existing objects in the duplicated sample scene
        GameObject[] roots = galleryScene.GetRootGameObjects();
        foreach(var root in roots)
        {
            if (root.name == "Main Camera" || root.name == "Directional Light" || root.name == "EventSystem") continue;
            DestroyImmediate(root);
        }

        // Create EventSystem if missing
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // Create Canvas
        GameObject canvasObj = new GameObject("CardGallery_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.AddComponent<GraphicRaycaster>();

        // Create ScrollView
        GameObject scrollViewObj = DefaultControls.CreateScrollView(new DefaultControls.Resources());
        scrollViewObj.name = "Scroll View";
        scrollViewObj.transform.SetParent(canvasObj.transform, false);
        RectTransform scrollRT = scrollViewObj.GetComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0.05f, 0.05f);
        scrollRT.anchorMax = new Vector2(0.95f, 0.85f);
        scrollRT.sizeDelta = Vector2.zero;
        scrollRT.anchoredPosition = Vector2.zero;

        // Setup Content Grid
        ScrollRect scrollRect = scrollViewObj.GetComponent<ScrollRect>();
        RectTransform contentRT = scrollRect.content;
        GridLayoutGroup grid = contentRT.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(250, 350);
        grid.spacing = new Vector2(20, 20);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.Flexible;

        ContentSizeFitter fitter = contentRT.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        contentRT.pivot = new Vector2(0.5f, 1f);

        // Add CardGalleryManager
        GameObject managerObj = new GameObject("CardGalleryManager");
        managerObj.transform.SetParent(canvasObj.transform, false);
        CardGalleryManager manager = managerObj.AddComponent<CardGalleryManager>();
        manager.gridContent = contentRT;
        manager.cardCollection = allCards;
        
        string cardPrefabPath = "Assets/Prefabs/CardPrefab.prefab";
        manager.cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(cardPrefabPath);
        if (manager.cardPrefab == null)
        {
            manager.cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CardView.prefab");
        }

        // Add Back Button
        GameObject buttonObj = DefaultControls.CreateButton(new DefaultControls.Resources());
        buttonObj.name = "BackButton";
        buttonObj.transform.SetParent(canvasObj.transform, false);
        RectTransform btnRT = buttonObj.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0.05f, 0.88f);
        btnRT.anchorMax = new Vector2(0.25f, 0.95f);
        btnRT.sizeDelta = Vector2.zero;
        btnRT.anchoredPosition = Vector2.zero;
        buttonObj.GetComponentInChildren<Text>().text = "Back to Menu";

        // Add listener properly
        UnityEngine.Events.UnityAction action = new UnityEngine.Events.UnityAction(manager.BackToMenu);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(buttonObj.GetComponent<Button>().onClick, action);

        // Ensure scenes are in Build Settings
        List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool hasMenu = false, hasGallery = false;
        foreach(var s in buildScenes)
        {
            if (s.path.Contains("MainMenu")) hasMenu = true;
            if (s.path.Contains("CardGallery")) hasGallery = true;
        }
        if (!hasMenu) buildScenes.Add(new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true));
        if (!hasGallery) buildScenes.Add(new EditorBuildSettingsScene("Assets/Scenes/CardGallery.unity", true));
        EditorBuildSettings.scenes = buildScenes.ToArray();

        // Save & reload main
        EditorSceneManager.SaveScene(galleryScene);
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");

        Debug.Log($"Card Gallery Setup Complete! Added {allCards.Count} cards.");
    }
}
