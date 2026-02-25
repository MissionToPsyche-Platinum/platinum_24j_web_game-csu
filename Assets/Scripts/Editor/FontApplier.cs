using UnityEngine;
using UnityEditor;
using TMPro;

/// <summary>
/// Editor utility to:
///   1. Create a TMP_FontAsset from Kenney Future.ttf (if not already done)
///   2. Apply it to every TextMeshProUGUI component under GameCanvas
///
/// Run via: Tools > Apply Kenney Future Font
/// </summary>
public static class FontApplier
{
    private const string TtfPath       = "Assets/TextMesh Pro/Fonts/Kenney Future.ttf";
    private const string FontAssetPath = "Assets/TextMesh Pro/Fonts/Kenney Future SDF.asset";

    [MenuItem("Tools/Apply Kenney Future Font")]
    public static void ApplyFont()
    {
        // ---- Step 1: Get or create the TMP font asset ----
        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);

        if (fontAsset == null)
        {
            Font sourceTtf = AssetDatabase.LoadAssetAtPath<Font>(TtfPath);
            if (sourceTtf == null)
            {
                Debug.LogError($"[FontApplier] Could not find TTF at: {TtfPath}");
                return;
            }

            fontAsset = TMP_FontAsset.CreateFontAsset(sourceTtf);
            if (fontAsset == null)
            {
                Debug.LogError("[FontApplier] TMP_FontAsset.CreateFontAsset returned null.");
                return;
            }

            AssetDatabase.CreateAsset(fontAsset, FontAssetPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[FontApplier] Created TMP font asset at: {FontAssetPath}");
        }
        else
        {
            Debug.Log($"[FontApplier] Using existing TMP font asset: {FontAssetPath}");
        }

        // ---- Step 2: Apply to all TMP texts in GameCanvas ----
        GameObject gameCanvas = GameObject.Find("GameCanvas");
        if (gameCanvas == null)
        {
            Debug.LogError("[FontApplier] Could not find 'GameCanvas' in the scene. Run 'Tools > Build Game Scene UI' first.");
            return;
        }

        TextMeshProUGUI[] allTexts = gameCanvas.GetComponentsInChildren<TextMeshProUGUI>(includeInactive: true);
        int count = 0;
        foreach (TextMeshProUGUI tmp in allTexts)
        {
            Undo.RecordObject(tmp, "Apply Kenney Future Font");
            tmp.font = fontAsset;
            EditorUtility.SetDirty(tmp);
            count++;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameCanvas.scene);
        Debug.Log($"[FontApplier] Applied Kenney Future font to {count} TextMeshProUGUI components in GameCanvas.");
    }
}
