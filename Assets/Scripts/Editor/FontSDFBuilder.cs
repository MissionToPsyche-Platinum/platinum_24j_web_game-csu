using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// Properly builds a Kenney Future SDF WITH actual glyph data in the atlas.
/// CreateFontAsset() only creates the container; TryAddCharacters() does the rasterization.
/// Run via: Tools > Build Kenney SDF Properly
/// </summary>
public static class FontSDFBuilder
{
    private const string TtfPath       = "Assets/TextMesh Pro/Fonts/Kenney Future.ttf";
    private const string FontAssetPath = "Assets/TextMesh Pro/Fonts/Kenney Future SDF.asset";

    // All printable ASCII + some extras we need
    private const string Characters =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz" +
        "0123456789 !@#$%^&*()-_=+[]{}|;:'\",.<>?/\\`~" +
        "éèêëàâäùûüîïôöçñ";

    [MenuItem("Tools/Build Kenney SDF Properly")]
    public static void Build()
    {
        Font sourceTtf = AssetDatabase.LoadAssetAtPath<Font>(TtfPath);
        if (sourceTtf == null)
        {
            Debug.LogError($"[FontSDFBuilder] TTF not found at: {TtfPath}");
            return;
        }

        // Delete existing
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (existing != null)
        {
            AssetDatabase.DeleteAsset(FontAssetPath);
            Debug.Log("[FontSDFBuilder] Deleted old SDF asset.");
        }

        // Create container with Dynamic atlas (allows TryAddCharacters)
        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
            sourceTtf,
            samplingPointSize: 44,
            atlasPadding: 5,
            renderMode: GlyphRenderMode.SDFAA,
            atlasWidth: 512,
            atlasHeight: 512,
            enableMultiAtlasSupport: false);

        if (fontAsset == null)
        {
            Debug.LogError("[FontSDFBuilder] CreateFontAsset returned null.");
            return;
        }

        fontAsset.name = "Kenney Future SDF";

        // Set atlas population mode to Dynamic so TryAddCharacters works
        fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;

        // Actually rasterize the glyphs into the atlas!
        bool success = fontAsset.TryAddCharacters(Characters, out string missingChars);
        Debug.Log($"[FontSDFBuilder] TryAddCharacters result: {success}, " +
                  $"Missing: \"{missingChars ?? "none"}\", " +
                  $"Glyphs: {fontAsset.glyphTable.Count}, Characters: {fontAsset.characterTable.Count}");

        if (fontAsset.atlasTexture != null)
            Debug.Log($"[FontSDFBuilder] Atlas: {fontAsset.atlasTexture.width}x{fontAsset.atlasTexture.height}");

        // Lock it to Static so it persists properly after save
        fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;

        // Fix font weight table
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

        // Save the main asset
        AssetDatabase.CreateAsset(fontAsset, FontAssetPath);

        // Save atlas texture as sub-asset
        if (fontAsset.atlasTexture != null)
        {
            fontAsset.atlasTexture.name = "Kenney Future SDF Atlas";
            AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
        }

        // Save material as sub-asset
        if (fontAsset.material != null)
        {
            fontAsset.material.name = "Kenney Future SDF Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        }

        // Save all atlas textures (in case of multi-atlas)
        if (fontAsset.atlasTextures != null)
        {
            for (int i = 1; i < fontAsset.atlasTextures.Length; i++)
            {
                if (fontAsset.atlasTextures[i] != null)
                {
                    fontAsset.atlasTextures[i].name = $"Kenney Future SDF Atlas {i}";
                    AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[i], fontAsset);
                }
            }
        }

        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Reload to verify persistence
        fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        Debug.Log($"[FontSDFBuilder] After reload: Glyphs={fontAsset?.glyphTable?.Count}, " +
                  $"Characters={fontAsset?.characterTable?.Count}, " +
                  $"Atlas={fontAsset?.atlasTexture?.width}x{fontAsset?.atlasTexture?.height}");

        // Apply to all TMP components in GameCanvas
        if (fontAsset != null)
        {
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
                Debug.Log($"[FontSDFBuilder] Applied to {allTMP.Length} TMP components. Scene saved.");
            }
        }

        Debug.Log("[FontSDFBuilder] DONE!");
    }
}
