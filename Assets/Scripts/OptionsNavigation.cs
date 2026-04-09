using UnityEngine;
using UnityEngine.SceneManagement;

public static class OptionsNavigation
{
    public const string DefaultOptionsScene = "Options";
    public const string DefaultMainMenuScene = "MainMenu";

    /// <summary>Used when no "previous" scene is stored (e.g. opened Options directly in editor).</summary>
    public const string DefaultFallbackScene = DefaultMainMenuScene;

    private static string _previousSceneName;

    /// <summary>
    /// Prefers in-scene <see cref="OptionsOverlayController"/>; otherwise loads <paramref name="optionsSceneName"/> (full scene swap).
    /// </summary>
    public static void OpenOptions(string optionsSceneName = DefaultOptionsScene)
    {
        if (OptionsOverlayController.TryShow())
            return;

        var name = string.IsNullOrEmpty(optionsSceneName) ? DefaultOptionsScene : optionsSceneName;
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogWarning("[OptionsNavigation] No options scene name set and no OptionsOverlayController in scene.");
            return;
        }

        _previousSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(name);
    }

    public static void ReturnToPreviousOrFallback(string fallbackScene = DefaultFallbackScene)
    {
        string sceneToLoad = string.IsNullOrEmpty(_previousSceneName) ? fallbackScene : _previousSceneName;
        SceneManager.LoadScene(sceneToLoad);
    }

    public static void LoadMainMenu(string mainMenuSceneName = DefaultMainMenuScene)
    {
        if (string.IsNullOrEmpty(mainMenuSceneName))
            return;

        SceneManager.LoadScene(mainMenuSceneName);
    }
}

