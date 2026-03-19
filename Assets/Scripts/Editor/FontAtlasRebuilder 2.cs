using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// Properly rebuilds the Kenney Future SDF font asset with an embedded atlas texture.
/// The key difference from the previous attempt: the atlas Texture2D is saved as a
/// sub-asset of the TMP_FontAsset, which is required for it to persist across editor sessions.
/// Run via: Tools > Rebuild Font With Atlas
/// </summary>
public static class FontAtlasRebuilder
{
    private const string TtfPath       = "Assets/TextMesh Pro/Fonts/Kenney Future.ttf";
    private const string FontAssetPath = "Assets/TextMesh Pro/Fonts/Kenney Future SDF.asset";

    [MenuItem("Tools/Rebuild Font With Atlas")]
    public static void Rebuild()
    {
        // --- 1. Load source TTF ---
        Font sourceTtf = AssetDatabase.LoadAssetAtPath<Font>(TtfPath);
        if (sourceTtf == null)
        {
            Debug.LogError($"[FontAtlasRebuilder] TTF not found at: {TtfPath}");
            return;
        }

        // --- 2. Delete existing broken asset ---
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (existing != null)
        {
            AssetDatabase.DeleteAsset(FontAssetPath);
            Debug.Log("[FontAtlasRebuilder] Deleted old (broken) SDF asset.");
        }

        // --- 3. Create the font asset ---
        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
            sourceTtf,
            samplingPointSize: 44,
            atlasPadding: 5,
            renderMode: GlyphRenderMode.SDFAA,
            atlasWidth: 512,
            atlasHeight: 512);

        if (fontAsset == null)
        {
            Debug.LogError("[FontAtlasRebuilder] CreateFontAsset returned null.");
            return;
        }

        fontAsset.name = "Kenney Future SDF";

        // --- 4. Fix the font weight table (prevents Inspector NullRef) ---
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

        // --- 5. Save the main asset first ---
        AssetDatabase.CreateAsset(fontAsset, FontAssetPath);

        // --- 6. Save the atlas texture as a sub-asset (THIS is the critical step) ---
        if (fontAsset.atlasTexture != null)
        {
            fontAsset.atlasTexture.name = "Kenney Future SDF Atlas";
            AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
            Debug.Log($"[FontAtlasRebuilder] Atlas texture saved: {fontAsset.atlasTexture.width}x{fontAsset.atlasTexture.height}");
        }
        else
        {
            Debug.LogWarning("[FontAtlasRebuilder] No atlas texture generated!");
        }

        // Also save the material as a sub-asset if it exists
        if (fontAsset.material != null)
        {
            fontAsset.material.name = "Kenney Future SDF Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        }

        AssetDatabase.SaveAssets();

        Debug.Log($"[FontAtlasRebuilder] Created SDF asset at: {FontAssetPath}");
        Debug.Log($"[FontAtlasRebuilder] Glyphs: {fontAsset.glyphTable?.Count}, Characters: {fontAsset.characterTable?.Count}");

        // --- 7. Re-apply to all TMP in GameCanvas ---
        // Reload from disk to get the persisted version
        AssetDatabase.Refresh();
        fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (fontAsset == null)
        {
            Debug.LogError("[FontAtlasRebuilder] Failed to reload font asset after save!");
            return;
        }

        GameObject canvas = GameObject.Find("GameCanvas");
        if (canvas != null)
        {
            var allTMP = canvas.GetComponentsInChildren<TMP_Text>(true);
            foreach (var tmp in allTMP)
            {
                Undo.RecordObject(tmp, "Apply Kenney font");
                tmp.font = fontAsset;
                tmp.SetAllDirty();
                EditorUtility.SetDirty(tmp);
            }
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(canvas.scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            Debug.Log($"[FontAtlasRebuilder] Applied to {allTMP.Length} TMP components. Scene saved.");
        }

        Debug.Log("[FontAtlasRebuilder] Complete!");
    }
}
