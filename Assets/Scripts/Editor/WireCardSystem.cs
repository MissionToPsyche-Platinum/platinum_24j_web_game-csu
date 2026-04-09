using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// One-shot editor utility: 
/// 1. Wires DeckManager refs to HandViewAnchor + CardHandLayout
/// 2. Cleans missing scripts on CardView prefab and adds CardHover + transparent Image
/// 3. Creates a FeedbackText TMP element under GameCanvas (for Kenney font)
/// 4. Positions HandViewAnchor at bottom-center for card hand
/// Run via: Tools > Wire Card System
/// </summary>
public static class WireCardSystem
{
    [MenuItem("Tools/Wire Card System")]
    public static void Wire()
    {
        // === 1. Fix CardView Prefab ===
        FixCardViewPrefab();

        // === 2. Find & wire DeckManager ===
        var deckMgr = Object.FindFirstObjectByType<DeckManager>();
        if (deckMgr == null)
        {
            Debug.LogError("[WireCardSystem] No DeckManager found in scene.");
            return;
        }

        var handAnchor = GameObject.Find("HandViewAnchor");
        if (handAnchor == null)
        {
            Debug.LogError("[WireCardSystem] No HandViewAnchor found in scene.");
            return;
        }

        var cardHandLayout = handAnchor.GetComponent<CardHandLayout>();
        if (cardHandLayout == null)
        {
            cardHandLayout = handAnchor.AddComponent<CardHandLayout>();
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CardView.prefab");

        // Wire DeckManager via SerializedObject
        var so = new SerializedObject(deckMgr);
        so.FindProperty("handParent").objectReferenceValue = handAnchor.transform;
        so.FindProperty("cardHandLayout").objectReferenceValue = cardHandLayout;
        var prefabProp = so.FindProperty("cardPrefab");
        prefabProp.objectReferenceValue = prefab;
        so.FindProperty("drawPhaseCardCount").intValue = 5;
        so.ApplyModifiedProperties();

        // Wire CardHandLayout
        var layoutSO = new SerializedObject(cardHandLayout);
        layoutSO.FindProperty("cardsParent").objectReferenceValue = handAnchor.transform;
        layoutSO.FindProperty("cardPrefab").objectReferenceValue = prefab;
        layoutSO.ApplyModifiedProperties();

        // === 3. Setup HandViewAnchor RectTransform at bottom-center ===
        var handRect = handAnchor.GetComponent<RectTransform>();
        if (handRect != null)
        {
            handRect.anchorMin = new Vector2(0.5f, 0f);
            handRect.anchorMax = new Vector2(0.5f, 0f);
            handRect.pivot = new Vector2(0.5f, 0f);
            handRect.anchoredPosition = new Vector2(0f, -30f);
            handRect.sizeDelta = new Vector2(1200f, 300f);
        }

        // === 5. Mark scene dirty ===
        EditorUtility.SetDirty(deckMgr.gameObject);
        EditorSceneManager.MarkSceneDirty(deckMgr.gameObject.scene);

        Debug.Log("[WireCardSystem] ✅ All wiring complete:\n" +
                  $"  handParent = {handAnchor.name}\n" +
                  $"  cardHandLayout = {cardHandLayout}\n" +
                  $"  cardPrefab = CardView.prefab\n" +
                  $"  HandViewAnchor positioned at bottom-center\n" +
                  $"  User will create FeedbackMessage manually");
    }

    private static void FixCardViewPrefab()
    {
        var prefabPath = "Assets/Prefabs/CardView.prefab";
        var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefabAsset == null)
        {
            Debug.LogWarning("[WireCardSystem] CardView.prefab not found.");
            return;
        }

        var root = PrefabUtility.LoadPrefabContents(prefabPath);

        // Remove missing (null) MonoBehaviour components
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root);

        // Ensure CardView exists
        if (root.GetComponent<CardView>() == null)
            root.AddComponent<CardView>();

        // Ensure CardHover exists (now in its own file)
        if (root.GetComponent<CardHover>() == null)
            root.AddComponent<CardHover>();

        // Ensure CanvasGroup exists
        if (root.GetComponent<CanvasGroup>() == null)
            root.AddComponent<CanvasGroup>();

        // Ensure Image exists (transparent, for raycast)
        var image = root.GetComponent<Image>();
        if (image == null)
        {
            root.AddComponent<CanvasRenderer>();
            image = root.AddComponent<Image>();
        }
        image.color = new Color(0, 0, 0, 0); // Fully transparent
        image.raycastTarget = true;

        // Set root RectTransform size (cards need a size for raycasting)
        var rt = root.GetComponent<RectTransform>();
        if (rt != null && rt.sizeDelta == Vector2.zero)
        {
            rt.sizeDelta = new Vector2(180f, 250f);
        }

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        PrefabUtility.UnloadPrefabContents(root);

        Debug.Log("[WireCardSystem] CardView.prefab fixed: missing scripts cleaned, CardHover + Image added, size set.");
    }
}
