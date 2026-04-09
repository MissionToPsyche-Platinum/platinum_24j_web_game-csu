using UnityEditor;
using UnityEngine;
using TMPro;

public class CreateTypeLabel
{
    [MenuItem("Tools/Create Type Label")]
    public static void Execute()
    {
        string[] paths = {
            "Assets/Prefabs/CardView.prefab",
            "Assets/Prefabs/CardPrefab.prefab",
            "Assets/Resources/CardView.prefab"
        };

        foreach (var path in paths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            Debug.Log("Editing " + path);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

            Transform wrapper = inst.transform.Find("Wrapper");
            if (wrapper == null) continue;

            // Check if already exists
            if (wrapper.Find("Type_Text") != null)
            {
                Debug.Log("Type_Text already exists in " + path);
                GameObject.DestroyImmediate(inst);
                continue;
            }

            Transform titleText = wrapper.Find("Title_Text");
            if (titleText == null) continue;

            GameObject typeLabel = GameObject.Instantiate(titleText.gameObject, wrapper);
            typeLabel.name = "Type_Text";

            RectTransform typeRt = typeLabel.GetComponent<RectTransform>();
            RectTransform titleRt = titleText.GetComponent<RectTransform>();

            // Title is at anchory y=-20.9, height=50. Let's put type directly below.
            typeRt.anchoredPosition = new Vector2(0, -60);
            
            TextMeshProUGUI tmp = typeLabel.GetComponent<TextMeshProUGUI>();
            tmp.text = "MANEUVER";
            tmp.fontSize = 20; // smaller than title
            tmp.fontStyle = FontStyles.SmallCaps;

            PrefabUtility.SaveAsPrefabAsset(inst, path);
            GameObject.DestroyImmediate(inst);
        }
    }
}
