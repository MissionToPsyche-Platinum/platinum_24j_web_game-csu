using UnityEngine;
using UnityEditor;
using TMPro;

/// <summary>
/// Re-applies the Kenney Future SDF font to every TMP component in the GameCanvas.
/// Run via: Tools > Re-Apply Kenney Font
/// </summary>
public static class FontReApplier
{
    private const string FontAssetPath = "Assets/TextMesh Pro/Fonts/Kenney Future SDF.asset";

    [MenuItem("Tools/Re-Apply Kenney Font")]
    public static void ReApply()
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (font == null)
        {
            Debug.LogError("[FontReApplier] Cannot find font at: " + FontAssetPath);
            return;
        }

        // Log atlas info so we can verify glyphs exist
        Debug.Log($"[FontReApplier] Font: {font.name}, Glyph count: {font.glyphTable?.Count}, " +
                  $"Character count: {font.characterTable?.Count}, " +
                  $"Atlas: {font.atlasTexture?.width}x{font.atlasTexture?.height}");

        GameObject canvas = GameObject.Find("GameCanvas");
        if (canvas == null)
        {
            Debug.LogError("[FontReApplier] No GameCanvas in scene.");
            return;
        }

        var allTMP = canvas.GetComponentsInChildren<TMP_Text>(true);
        int count = 0;

        foreach (var tmp in allTMP)
        {
            Undo.RecordObject(tmp, "Re-apply Kenney font");

            // Set the font asset reference
            tmp.font = font;

            // Force TMP to regenerate mesh with new font
            tmp.SetAllDirty();
            EditorUtility.SetDirty(tmp);

            count++;
            Debug.Log($"[FontReApplier] Applied to: {tmp.gameObject.name} → \"{tmp.text}\"");
        }

        // Mark scene dirty so it saves
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(canvas.scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        Debug.Log($"[FontReApplier] Done. Re-applied Kenney Future SDF to {count} TMP components. Scene saved.");
    }
}
