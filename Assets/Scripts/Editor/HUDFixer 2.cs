using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// One-shot fix: update HUD text to remove emoji characters and make PlayZone invisible.
/// Run via: Tools > Fix HUD Text + PlayZone
/// </summary>
public static class HUDFixer
{
    [MenuItem("Tools/Fix HUD Text + PlayZone")]
    public static void Fix()
    {
        int fixes = 0;

        // --- Fix text content (emoji → plain labels) ---
        FixText("PowerText",  "PWR  3",  ref fixes);
        FixText("BudgetText", "BDG  6",  ref fixes);
        FixText("TimeText",   "TIME  15", ref fixes);

        // --- Make PlayZone invisible (keep RectTransform for drop detection) ---
        GameObject playZone = GameObject.Find("PlayZone");
        if (playZone != null)
        {
            // Remove the visible Image component; keep the RectTransform for raycasting
            Image img = playZone.GetComponent<Image>();
            if (img != null)
            {
                img.color = new Color(0, 0, 0, 0); // fully transparent
                img.raycastTarget = true;           // still catches drops
                EditorUtility.SetDirty(img);
                fixes++;
            }

            // Remove the border child and hint text
            Transform border = playZone.transform.Find("PlayZoneBorder");
            if (border != null) { Object.DestroyImmediate(border.gameObject); fixes++; }

            Transform hint = playZone.transform.Find("PlayZoneHint");
            if (hint != null) { Object.DestroyImmediate(hint.gameObject); fixes++; }

            // Remove the Outline component if present
            var outline = playZone.GetComponent<Outline>();
            if (outline != null) { Object.DestroyImmediate(outline); fixes++; }
        }

        if (fixes > 0)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        }

        Debug.Log($"[HUDFixer] Applied {fixes} fixes.");
    }

    private static void FixText(string goName, string newText, ref int count)
    {
        GameObject go = GameObject.Find(goName);
        if (go == null) { Debug.LogWarning($"[HUDFixer] Could not find '{goName}'"); return; }

        var tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp == null) { Debug.LogWarning($"[HUDFixer] No TMP on '{goName}'"); return; }

        Undo.RecordObject(tmp, "Fix HUD text");
        tmp.text = newText;
        EditorUtility.SetDirty(tmp);
        count++;
        Debug.Log($"[HUDFixer] Fixed '{goName}' → \"{newText}\"");
    }
}
