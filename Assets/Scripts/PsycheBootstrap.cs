using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Automatic bootstrap for Psyche Mission Strategy.
/// - Ensures a ResourceManager exists (and persists across scenes).
/// - Creates a simple ResourceHUD overlay if none is present (gameplay scenes only).
/// Menu scenes never get <see cref="ResourceHUD"/> auto-spawn; any leftover <c>ResourceHUD_Auto</c> is removed when a menu loads.
/// </summary>
public static class PsycheBootstrap
{
    private static bool _subscribed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegisterSceneCallback()
    {
        if (_subscribed)
            return;
        _subscribed = true;
        SceneManager.sceneLoaded += OnSceneLoaded;
        // First scene: sceneLoaded may have fired before we subscribed; handle active scene once.
        ProcessScene(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ProcessScene(scene);
    }

    private static void ProcessScene(Scene scene)
    {
        EnsureResourceManager();

        if (IsMenuStyleScene(scene))
        {
            DestroyAutoResourceHud();
            return;
        }

        var existingResourceHud = UnityEngine.Object.FindFirstObjectByType<ResourceHUD>();
        var existingGameHud = UnityEngine.Object.FindFirstObjectByType<GameHUD>();
        if (existingResourceHud == null && existingGameHud == null)
            CreateSimpleHud();
    }

    private static void EnsureResourceManager()
    {
        var existingManager = UnityEngine.Object.FindFirstObjectByType<ResourceManager>();
        if (existingManager != null)
            return;

        var go = new GameObject("ResourceManager");
        existingManager = go.AddComponent<ResourceManager>();
        UnityEngine.Object.DontDestroyOnLoad(go);
        existingManager.ResetForNewRun();
    }

    /// <summary>True for main menu (and similar) scenes — no resource strip, no auto HUD.</summary>
    private static bool IsMenuStyleScene(Scene scene)
    {
        if (!scene.IsValid())
            return false;

        if (scene.name.IndexOf("MainMenu", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        if (!string.IsNullOrEmpty(scene.path) &&
            scene.path.IndexOf("MainMenu.unity", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        // Scene objects may not be ready the same frame; still useful when name/path differ.
        if (UnityEngine.Object.FindFirstObjectByType<MainMenuSetup>() != null)
            return true;

        return false;
    }

    private static void DestroyAutoResourceHud()
    {
        var huds = UnityEngine.Object.FindObjectsByType<ResourceHUD>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        foreach (var h in huds)
        {
            if (h == null)
                continue;
            if (h.gameObject.name == "ResourceHUD_Auto")
                UnityEngine.Object.Destroy(h.gameObject);
        }
    }

    private static void CreateSimpleHud()
    {
        var existingManager = UnityEngine.Object.FindFirstObjectByType<ResourceManager>();
        if (existingManager == null)
            return;

        Canvas targetCanvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (targetCanvas == null)
        {
            var canvasGo = new GameObject("Psyche_AutoCanvas");
            targetCanvas = canvasGo.AddComponent<Canvas>();
            targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();
        }

        var hudRoot = new GameObject("ResourceHUD_Auto");
        hudRoot.transform.SetParent(targetCanvas.transform, false);

        var hud = hudRoot.AddComponent<ResourceHUD>();

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
            text.font = UiFontHelper.KenneyFutureOrFallback();
            text.fontSize = 18;
            text.alignment = TextAnchor.UpperLeft;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        hud.powerText = MakeLabel("PowerText", new Vector2(10, -10));
        hud.budgetText = MakeLabel("BudgetText", new Vector2(10, -30));
        hud.timeText = MakeLabel("TimeText", new Vector2(10, -50));

        hud.powerFormat = "Power: {0}";
        hud.budgetFormat = "Budget: {0}";
        hud.timeFormat = "Time: {0}";

        hud.SendMessage("OnEnable", SendMessageOptions.DontRequireReceiver);
    }
}
