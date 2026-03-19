using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Automatic bootstrap for Psyche Mission Strategy.
/// - Ensures a ResourceManager exists (and persists across scenes).
/// - Creates a simple ResourceHUD overlay if none is present.
/// This runs automatically on scene load; you don't have to wire it manually.
/// </summary>
public static class PsycheBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnAfterSceneLoad()
    {
        // Ensure there is a ResourceManager (singleton).
        var existingManager = Object.FindFirstObjectByType<ResourceManager>();
        if (existingManager == null)
        {
            var go = new GameObject("ResourceManager");
            existingManager = go.AddComponent<ResourceManager>();
            Object.DontDestroyOnLoad(go);
        }

        // Start a fresh run the first time we load into a scene.
        existingManager.ResetForNewRun();

        // Ensure there is at least one ResourceHUD.
        var existingHud = Object.FindFirstObjectByType<ResourceHUD>();
        if (existingHud == null)
        {
            CreateSimpleHud(existingManager);
        }
    }

    private static void CreateSimpleHud(ResourceManager manager)
    {
        // Try to attach to an existing Canvas (e.g. Main_Canvas) if present.
        Canvas targetCanvas = Object.FindFirstObjectByType<Canvas>();
        if (targetCanvas == null)
        {
            // Fallback: create our own overlay canvas.
            var canvasGo = new GameObject("Psyche_AutoCanvas");
            targetCanvas = canvasGo.AddComponent<Canvas>();
            targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();
        }

        var hudRoot = new GameObject("ResourceHUD_Auto");
        hudRoot.transform.SetParent(targetCanvas.transform, false);

        var hud = hudRoot.AddComponent<ResourceHUD>();

        // Helper to create a label.
        Text MakeLabel(string name, Vector2 anchoredPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(hudRoot.transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = anchoredPos;

            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 18;
            text.alignment = TextAnchor.UpperLeft;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        // Create three labels stacked in the top-left corner.
        hud.powerText = MakeLabel("PowerText", new Vector2(10, -10));
        hud.budgetText = MakeLabel("BudgetText", new Vector2(10, -30));
        hud.timeText = MakeLabel("TimeText", new Vector2(10, -50));

        // Initialize once.
        hud.powerFormat = "Power: {0}";
        hud.budgetFormat = "Budget: {0}";
        hud.timeFormat = "Time: {0}";

        // Force an initial update.
        hud.SendMessage("OnEnable", SendMessageOptions.DontRequireReceiver);
    }
}

