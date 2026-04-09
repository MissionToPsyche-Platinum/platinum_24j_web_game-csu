using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class ForcePlayFromMain
{
    private const string MENU_PATH = "Tools/Always Play From Main Menu";

    static ForcePlayFromMain()
    {
        EditorApplication.delayCall += () =>
        {
            bool isEnabled = EditorPrefs.GetBool(MENU_PATH, false);
            Menu.SetChecked(MENU_PATH, isEnabled);
            ApplySetting(isEnabled);
        };
    }

    [MenuItem(MENU_PATH)]
    public static void ToggleAction()
    {
        bool isEnabled = !Menu.GetChecked(MENU_PATH);
        Menu.SetChecked(MENU_PATH, isEnabled);
        EditorPrefs.SetBool(MENU_PATH, isEnabled);
        ApplySetting(isEnabled);
    }

    private static void ApplySetting(bool enabled)
    {
        if (enabled)
        {
            string startScenePath = "Assets/Scenes/MainMenu.unity";
            SceneAsset myWantedStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(startScenePath);
            if (myWantedStartScene != null)
            {
                EditorSceneManager.playModeStartScene = myWantedStartScene;
                Debug.Log("Unity will now ALWAYS start from the Main Menu when you hit Play.");
            }
            else
            {
                Debug.LogWarning("Could not find MainMenu scene!");
            }
        }
        else
        {
            EditorSceneManager.playModeStartScene = null;
            Debug.Log("Unity will now play whichever scene you currently have open.");
        }
    }
}
