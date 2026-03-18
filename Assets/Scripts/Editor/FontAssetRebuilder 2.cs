using UnityEngine;
using UnityEditor;
using TMPro;

/// <summary>
/// Regenerates the Kenney Future SDF asset with a fully populated font weight table
/// to fix NullReferenceException in the TMP Inspector.
/// Run via: Tools > Regenerate Kenney Future SDF
/// </summary>
public static class FontAssetRebuilder
{
    private const string TtfPath       = "Assets/TextMesh Pro/Fonts/Kenney Future.ttf";
    private const string FontAssetPath = "Assets/TextMesh Pro/Fonts/Kenney Future SDF.asset";

    [MenuItem("Tools/Regenerate Kenney Future SDF")]
    public static void Rebuild()
    {
        // Delete existing broken asset
        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath) != null)
        {
            AssetDatabase.DeleteAsset(FontAssetPath);
            Debug.Log("[FontAssetRebuilder] Deleted old SDF asset.");
        }

        Font sourceTtf = AssetDatabase.LoadAssetAtPath<Font>(TtfPath);
        if (sourceTtf == null)
        {
            Debug.LogError($"[FontAssetRebuilder] Could not find TTF at: {TtfPath}");
            return;
        }

        // Create with explicit sampling and padding for a clean atlas
        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
            sourceTtf,
            samplingPointSize: 44,
            atlasPadding: 5,
            renderMode: UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
            atlasWidth: 512,
            atlasHeight: 512);

        if (fontAsset == null)
        {
            Debug.LogError("[FontAssetRebuilder] CreateFontAsset returned null.");
            return;
        }

        // Ensure the font weight table is fully populated (9 entries: 100–900)
        // This prevents the NullReferenceException in TMP_BaseEditorPanel.DrawFont()
        if (fontAsset.fontWeightTable != null)
        {
            for (int i = 0; i < fontAsset.fontWeightTable.Length; i++)
            {
                if (fontAsset.fontWeightTable[i].regularTypeface == null)
                    fontAsset.fontWeightTable[i].regularTypeface = fontAsset;
                if (fontAsset.fontWeightTable[i].italicTypeface == null)
                    fontAsset.fontWeightTable[i].italicTypeface = fontAsset;
            }
        }

        AssetDatabase.CreateAsset(fontAsset, FontAssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[FontAssetRebuilder] Created clean SDF asset at: {FontAssetPath}");

        // Re-apply to all TMP text in GameCanvas
        GameObject gameCanvas = GameObject.Find("GameCanvas");
        if (gameCanvas != null)
        {
            var allTexts = gameCanvas.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
            foreach (var tmp in allTexts)
            {
                Undo.RecordObject(tmp, "Re-apply font");
                tmp.font = fontAsset;
                EditorUtility.SetDirty(tmp);
            }
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameCanvas.scene);
            Debug.Log($"[FontAssetRebuilder] Re-applied font to {allTexts.Length} TMP components.");
        }
    }
}
