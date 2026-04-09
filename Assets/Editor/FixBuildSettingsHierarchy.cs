using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class FixBuildSettingsHierarchy : EditorWindow
{
    [MenuItem("Tools/Set Up Build Settings Hierarchy")]
    public static void SetUpHierarchy()
    {
        // Define the scenes we want in exact order of hierarchy
        string[] orderedScenes = new string[]
        {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/Options.unity",
            "Assets/Scenes/SampleScene.unity",
            "Assets/Scenes/CardGallery.unity"
        };

        List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>();

        foreach (string scenePath in orderedScenes)
        {
            // Only add the scene if it actually exists in the project
            if (System.IO.File.Exists(scenePath))
            {
                buildScenes.Add(new EditorBuildSettingsScene(scenePath, true));
                Debug.Log($"Added {scenePath} to Build Settings.");
            }
            else
            {
                Debug.LogWarning($"Could not find scene: {scenePath}");
            }
        }

        // Overwrite the build settings with our perfectly ordered list
        EditorBuildSettings.scenes = buildScenes.ToArray();
        
        // Also ensure that the MainMenuUI prefab points directly to SampleScene
        string prefabPath = "Assets/Prefabs/MainMenu_Panel.prefab";
        GameObject mainMenuPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (mainMenuPrefab != null)
        {
            MainMenuUI menuUI = mainMenuPrefab.GetComponent<MainMenuUI>();
            if (menuUI != null)
            {
                menuUI.gameSceneName = "SampleScene";
                menuUI.optionsSceneName = "Options";
                EditorUtility.SetDirty(menuUI);
                PrefabUtility.SavePrefabAsset(mainMenuPrefab);
                Debug.Log("Wired MainMenu buttons to load SampleScene and Options!");
            }
        }

        Debug.Log("Build Settings Hierarchy setup complete! MainMenu is now Scene 0.");
    }
}
