using UnityEngine.SceneManagement;

public static class OptionsNavigation
{
    public const string DefaultOptionsScene = "Options";
    public const string DefaultFallbackScene = "SampleScene";

    private static string _previousSceneName;

    public static void OpenOptions(string optionsSceneName = DefaultOptionsScene)
    {
        _previousSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(optionsSceneName);
    }

    public static void ReturnToPreviousOrFallback(string fallbackScene = DefaultFallbackScene)
    {
        string sceneToLoad = string.IsNullOrEmpty(_previousSceneName) ? fallbackScene : _previousSceneName;
        SceneManager.LoadScene(sceneToLoad);
    }
}

