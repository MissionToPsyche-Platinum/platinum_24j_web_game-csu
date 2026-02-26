using UnityEngine;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Wires up Options buttons throughout the scene:
/// 1. Main menu Options_Button → calls MainMenuUI.OnOpenOptions()
/// 2. Adds an Options gear button to GameCanvas → shows OptionsOverlay
/// Also wires StartGame and ViewCards buttons to their MainMenuUI handlers.
/// Run via: Tools > Wire Options Buttons
/// </summary>
public static class OptionsButtonWirer
{
    private const string FontAssetPath = "Assets/TextMesh Pro/Fonts/Kenney Future SDF.asset";
    private const string ButtonSpritePath = "Assets/Art/button_rectangle.asset";

    [MenuItem("Tools/Wire Options Buttons")]
    public static void Wire()
    {
        // --- 1. Find the MainMenuUI and its buttons ---
        var mainMenuUI = Object.FindObjectOfType<MainMenuUI>();
        if (mainMenuUI == null)
        {
            Debug.LogError("[OptionsButtonWirer] No MainMenuUI found in scene!");
            return;
        }

        // Wire the optionsPanel reference on MainMenuUI
        var overlay = GameObject.Find("OptionsOverlay");
        if (overlay == null)
        {
            // Try finding inactive
            foreach (var root in mainMenuUI.gameObject.scene.GetRootGameObjects())
            {
                if (root.name == "OptionsOverlay") { overlay = root; break; }
            }
        }

        if (overlay != null)
        {
            Undo.RecordObject(mainMenuUI, "Wire optionsPanel");
            mainMenuUI.optionsPanel = overlay;

            // Clear the optionsSceneName so it doesn't try to load a scene
            mainMenuUI.optionsSceneName = "";
            EditorUtility.SetDirty(mainMenuUI);
            Debug.Log("[OptionsButtonWirer] Wired OptionsOverlay to MainMenuUI.optionsPanel");
        }
        else
        {
            Debug.LogWarning("[OptionsButtonWirer] OptionsOverlay not found! Run Tools > Build Options Overlay first.");
        }

        // Wire GameCanvas to gameplayLayer
        GameObject gameCanvas = GameObject.Find("GameCanvas");
        if (gameCanvas == null)
        {
            // Try finding inactive
            foreach (GameObject go in Resources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[])
            {
                if (go.name == "GameCanvas" && go.scene == mainMenuUI.gameObject.scene)
                {
                    gameCanvas = go;
                    break;
                }
            }
        }

        if (gameCanvas != null)
        {
            Undo.RecordObject(mainMenuUI, "Wire GameCanvas");
            mainMenuUI.gameplayLayer = gameCanvas;
            mainMenuUI.gameSceneName = ""; // Ensure it uses in-scene toggle mode
            EditorUtility.SetDirty(mainMenuUI);
            Debug.Log("[OptionsButtonWirer] Wired GameCanvas to MainMenuUI.gameplayLayer");
            gameCanvas.SetActive(false); // Make sure it starts hidden
        }
        else
        {
            Debug.LogWarning("[OptionsButtonWirer] GameCanvas not found. Cannot wire to MainMenuUI.");
        }

        // Find buttons under MainMenu_Panel
        Transform menuPanel = mainMenuUI.transform;

        // Wire Options_Button
        WireButton(menuPanel, "Options_Button", mainMenuUI, "OnOpenOptions");

        // Wire StartGame_Button
        WireButton(menuPanel, "StartGame_Button", mainMenuUI, "OnStartGame");

        // Wire ViewCards_Button
        WireButton(menuPanel, "ViewCards_Button", mainMenuUI, "OnViewCards");

        // --- 2. Add Options button to GameCanvas (if it exists) ---
        AddGameUIOptionsButton(overlay, gameCanvas);

        EditorSceneManager.MarkSceneDirty(mainMenuUI.gameObject.scene);
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[OptionsButtonWirer] All buttons wired. Scene saved.");
    }

    private static void WireButton(Transform parent, string buttonName, MonoBehaviour target, string methodName)
    {
        Transform btnTransform = parent.Find(buttonName);
        if (btnTransform == null)
        {
            Debug.LogWarning($"[OptionsButtonWirer] Button '{buttonName}' not found under {parent.name}");
            return;
        }

        Button btn = btnTransform.GetComponent<Button>();
        if (btn == null)
        {
            Debug.LogWarning($"[OptionsButtonWirer] No Button component on '{buttonName}'");
            return;
        }


        // Clear existing listeners and add the correct one
        Undo.RecordObject(btn, $"Wire {buttonName}");
        btn.onClick = new Button.ButtonClickedEvent();

        var action = System.Delegate.CreateDelegate(
            typeof(UnityEngine.Events.UnityAction),
            target, methodName) as UnityEngine.Events.UnityAction;

        UnityEventTools.AddPersistentListener(btn.onClick, action);
        EditorUtility.SetDirty(btn);
        Debug.Log($"[OptionsButtonWirer] Wired {buttonName} → {target.GetType().Name}.{methodName}()");
    }

    private static void AddGameUIOptionsButton(GameObject overlay, GameObject gameCanvas)
    {
        if (gameCanvas == null)
        {
            Debug.Log("[OptionsButtonWirer] No GameCanvas passed - skipping in-game options button.");
            return;
        }

        // Check if button already exists
        Transform existing = gameCanvas.transform.Find("OptionsButton");
        if (existing != null)
        {
            Debug.Log("[OptionsButtonWirer] In-game OptionsButton already exists.");
            return;
        }

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        Sprite btnSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ButtonSpritePath);

        // Create the button in the top-right corner
        GameObject btnGO = new GameObject("OptionsButton");
        Undo.RegisterCreatedObjectUndo(btnGO, "Create in-game Options button");
        btnGO.AddComponent<RectTransform>();
        btnGO.transform.SetParent(gameCanvas.transform, false);

        var rt = btnGO.GetComponent<RectTransform>();
        // Top-right corner, with some padding
        rt.anchorMin = new Vector2(1, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(1, 1);
        rt.anchoredPosition = new Vector2(-15, -15);
        rt.sizeDelta = new Vector2(120, 50);

        var img = btnGO.AddComponent<Image>();
        img.sprite = btnSprite;
        img.color = new Color(0.28f, 0.40f, 0.75f, 1f); // Match original button tint

        var btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = img;

        // Button label
        GameObject label = new GameObject("Label");
        label.AddComponent<RectTransform>();
        label.transform.SetParent(btnGO.transform, false);
        var labelRT = label.GetComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = Vector2.zero;
        labelRT.offsetMax = Vector2.zero;

        var tmp = label.AddComponent<TextMeshProUGUI>();
        tmp.text = "Options";
        tmp.fontSize = 18;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        if (font) tmp.font = font;

        // Add a simple toggle component
        var toggle = btnGO.AddComponent<OptionsToggle>();
        EditorUtility.SetDirty(btnGO);
        Debug.Log("[OptionsButtonWirer] Created in-game OptionsButton on GameCanvas.");
    }
}
