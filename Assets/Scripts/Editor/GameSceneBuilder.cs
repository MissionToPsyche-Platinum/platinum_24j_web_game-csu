using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using UnityEditor.SceneManagement;

/// <summary>
/// Editor utility that builds the full Game UI canvas hierarchy in the active scene.
/// Run via: Tools > Build Game Scene UI
///
/// After running:
///   1. Drag the existing HandView prefab onto the HandViewAnchor object.
///   2. Assign your sprites/fonts in the Inspector if desired.
///   3. Wire GameUIController references in the Inspector.
/// </summary>
public static class GameSceneBuilder
{
    // -----------------------------------------------------------------------
    // Colour palette (space / Psyche theme)
    // -----------------------------------------------------------------------
    private static readonly Color ColBg          = new Color(0.05f, 0.06f, 0.12f, 0.92f);  // deep navy
    private static readonly Color ColPanel        = new Color(0.08f, 0.10f, 0.18f, 0.95f);  // dark blue-grey
    private static readonly Color ColAccent       = new Color(0.22f, 0.60f, 0.90f, 1.00f);  // Psyche blue
    private static readonly Color ColAccentDark   = new Color(0.10f, 0.30f, 0.55f, 1.00f);
    private static readonly Color ColText         = new Color(0.90f, 0.92f, 0.96f, 1.00f);  // near-white
    private static readonly Color ColSubText      = new Color(0.60f, 0.65f, 0.75f, 1.00f);  // muted
    private static readonly Color ColEndTurn      = new Color(0.85f, 0.40f, 0.10f, 1.00f);  // orange-red
    private static readonly Color ColEndTurnHover = new Color(1.00f, 0.55f, 0.20f, 1.00f);
    private static readonly Color ColPlayZone     = new Color(0.22f, 0.60f, 0.90f, 0.08f);  // faint blue tint
    private static readonly Color ColPlayZoneBorder = new Color(0.22f, 0.60f, 0.90f, 0.35f);

    private const string FontAssetPath    = "Assets/TextMesh Pro/Fonts/Kenney Future SDF.asset";
    private const string ButtonSpritePath = "Assets/Imports/kenney_ui-pack-space-expansion/PNG/Green/Extra/Double/button_rectangle.png";

    private static TMP_FontAsset LoadFont()
    {
        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
    }

    private static Sprite LoadButtonSprite()
    {
        // Load as sprite; the texture must be imported as Sprite or Multiple in the importer.
        Sprite spr = AssetDatabase.LoadAssetAtPath<Sprite>(ButtonSpritePath);
        if (spr == null)
        {
            // Fallback: try loading the texture and wrapping it
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(ButtonSpritePath);
            if (tex != null)
                spr = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }
        if (spr == null)
            Debug.LogWarning("[GameSceneBuilder] Could not load button_rectangle sprite at: " + ButtonSpritePath);
        return spr;
    }

    [MenuItem("Tools/Build Game Scene UI")]
    public static void BuildGameUI()
    {
        TMP_FontAsset kenneyFont = LoadFont();

        GameObject existingCanvas = GameObject.Find("GameCanvas");
        if (existingCanvas != null)
        {
            bool overwrite = EditorUtility.DisplayDialog(
                "GameCanvas already exists",
                "A 'GameCanvas' already exists in the scene. Overwrite it?",
                "Overwrite", "Cancel");
            if (!overwrite) return;
            Object.DestroyImmediate(existingCanvas);
        }

        // ---- Root canvas ----
        GameObject canvasGO = new GameObject("GameCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ---- Find or create the --- UI --- group ----
        GameObject uiGroup = GameObject.Find("--- UI ---");
        if (uiGroup != null)
            canvasGO.transform.SetParent(uiGroup.transform, false);

        Transform root = canvasGO.transform;

        // ====================================================================
        // 1. TOP HUD
        // ====================================================================
        GameObject hudGO = CreatePanel(root, "TopHUD",
            anchorMin: new Vector2(0f, 1f), anchorMax: new Vector2(1f, 1f),
            pivot: new Vector2(0.5f, 1f),
            offsetMin: new Vector2(0f, -70f), offsetMax: Vector2.zero);
        hudGO.GetComponent<Image>().color = ColPanel;

        // Resource icons + text (left side)
        float iconX = 30f;
        string[] resourceLabels = { "PWR  3", "BDG  6", "TIME  15" };
        string[] resourceNames  = { "PowerText", "BudgetText", "TimeText" };
        Color[]  resourceColours = { new Color(1f, 0.85f, 0.2f, 1f), new Color(0.3f, 0.9f, 0.4f, 1f), new Color(0.4f, 0.75f, 1f, 1f) };

        for (int i = 0; i < 3; i++)
        {
            GameObject res = CreateTMPLabel(hudGO.transform, resourceNames[i],
                text: resourceLabels[i], fontSize: 26, colour: resourceColours[i],
                anchoredPos: new Vector2(iconX + i * 160f, -35f), size: new Vector2(150f, 50f),
                font: kenneyFont);
            res.GetComponent<TMP_Text>().fontStyle = FontStyles.Bold;
        }

        // Floor indicator (right side)
        GameObject floorLabel = CreateTMPLabel(hudGO.transform, "FloorText",
            text: "Floor 1 / 4", fontSize: 22, colour: ColSubText,
            anchoredPos: new Vector2(-20f, -35f), size: new Vector2(180f, 50f),
            anchorMin: new Vector2(1f, 0f), anchorMax: new Vector2(1f, 1f),
            pivot: new Vector2(1f, 0.5f), font: kenneyFont);
        floorLabel.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Right;

        // Add GameHUD component
        GameHUD gameHUD = hudGO.AddComponent<GameHUD>();
        SerializedObject hudSO = new SerializedObject(gameHUD);
        hudSO.FindProperty("powerText").objectReferenceValue  = hudGO.transform.Find("PowerText").GetComponent<TMP_Text>();
        hudSO.FindProperty("budgetText").objectReferenceValue = hudGO.transform.Find("BudgetText").GetComponent<TMP_Text>();
        hudSO.FindProperty("timeText").objectReferenceValue   = hudGO.transform.Find("TimeText").GetComponent<TMP_Text>();
        hudSO.FindProperty("floorText").objectReferenceValue  = hudGO.transform.Find("FloorText").GetComponent<TMP_Text>();
        hudSO.ApplyModifiedProperties();

        // ====================================================================
        // 2. ENCOUNTER PANEL (top-right)
        // ====================================================================
        GameObject encGO = CreatePanel(root, "EncounterPanel",
            anchorMin: new Vector2(1f, 1f), anchorMax: new Vector2(1f, 1f),
            pivot: new Vector2(1f, 1f),
            offsetMin: new Vector2(-320f, -280f), offsetMax: new Vector2(-20f, -80f));
        encGO.GetComponent<Image>().color = ColPanel;

        // Thin accent top border
        GameObject topBorder = CreateImage(encGO.transform, "TopBorder", ColAccent,
            anchorMin: new Vector2(0f, 1f), anchorMax: new Vector2(1f, 1f),
            pivot: new Vector2(0.5f, 1f), size: new Vector2(0f, 4f), anchoredPos: Vector2.zero);

        // Encounter type label
        GameObject encType = CreateTMPLabel(encGO.transform, "EncounterTypeText",
            text: "DATA COLLECTION", fontSize: 14, colour: ColAccent,
            anchoredPos: new Vector2(0f, -18f), size: new Vector2(-20f, 24f),
            anchorMin: new Vector2(0f, 1f), anchorMax: new Vector2(1f, 1f),
            pivot: new Vector2(0.5f, 1f), font: kenneyFont);
        encType.GetComponent<TMP_Text>().fontStyle = FontStyles.Bold;
        encType.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;

        // Divider
        CreateImage(encGO.transform, "Divider", ColAccentDark,
            anchorMin: new Vector2(0f, 1f), anchorMax: new Vector2(1f, 1f),
            pivot: new Vector2(0.5f, 1f), size: new Vector2(-20f, 1f), anchoredPos: new Vector2(0f, -44f));

        // Objective text
        GameObject objText = CreateTMPLabel(encGO.transform, "ObjectiveText",
            text: "Collect 5 Surface data", fontSize: 16, colour: ColText,
            anchoredPos: new Vector2(0f, -60f), size: new Vector2(-20f, 40f),
            anchorMin: new Vector2(0f, 1f), anchorMax: new Vector2(1f, 1f),
            pivot: new Vector2(0.5f, 1f), font: kenneyFont);
        objText.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;
        objText.GetComponent<TMP_Text>().enableWordWrapping = true;

        // Progress label
        GameObject progLabel = CreateTMPLabel(encGO.transform, "ProgressText",
            text: "0 / 5", fontSize: 14, colour: ColSubText,
            anchoredPos: new Vector2(0f, -105f), size: new Vector2(-20f, 24f),
            anchorMin: new Vector2(0f, 1f), anchorMax: new Vector2(1f, 1f),
            pivot: new Vector2(0.5f, 1f), font: kenneyFont);
        progLabel.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;

        // Progress bar background
        GameObject progBg = CreateImage(encGO.transform, "ProgressBarBg", new Color(0.15f, 0.18f, 0.28f, 1f),
            anchorMin: new Vector2(0f, 1f), anchorMax: new Vector2(1f, 1f),
            pivot: new Vector2(0.5f, 1f), size: new Vector2(-20f, 14f), anchoredPos: new Vector2(0f, -132f));

        // Progress bar fill
        GameObject progFill = CreateImage(progBg.transform, "ProgressFill", ColAccent,
            anchorMin: Vector2.zero, anchorMax: Vector2.one,
            pivot: new Vector2(0f, 0.5f), size: Vector2.zero, anchoredPos: Vector2.zero);
        Image fillImg = progFill.GetComponent<Image>();
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillAmount = 0f;

        // Turn limit label
        GameObject turnLimitLabel = CreateTMPLabel(encGO.transform, "TurnLimitText",
            text: "Turn limit: 8", fontSize: 13, colour: ColSubText,
            anchoredPos: new Vector2(0f, -155f), size: new Vector2(-20f, 24f),
            anchorMin: new Vector2(0f, 1f), anchorMax: new Vector2(1f, 1f),
            pivot: new Vector2(0.5f, 1f), font: kenneyFont);
        turnLimitLabel.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;

        // Add EncounterPanel component
        EncounterPanel encPanel = encGO.AddComponent<EncounterPanel>();
        SerializedObject encSO = new SerializedObject(encPanel);
        encSO.FindProperty("encounterTypeText").objectReferenceValue = encType.GetComponent<TMP_Text>();
        encSO.FindProperty("objectiveText").objectReferenceValue     = objText.GetComponent<TMP_Text>();
        encSO.FindProperty("progressText").objectReferenceValue      = progLabel.GetComponent<TMP_Text>();
        encSO.FindProperty("progressFill").objectReferenceValue      = fillImg;
        encSO.FindProperty("turnLimitText").objectReferenceValue     = turnLimitLabel.GetComponent<TMP_Text>();
        encSO.ApplyModifiedProperties();

        // ====================================================================
        // 3. PLAY ZONE (centre drop area)
        // ====================================================================
        GameObject playZone = CreatePanel(root, "PlayZone",
            anchorMin: new Vector2(0.15f, 0.25f), anchorMax: new Vector2(0.85f, 0.80f),
            pivot: new Vector2(0.5f, 0.5f),
            offsetMin: Vector2.zero, offsetMax: Vector2.zero);
        // Invisible drop target — transparent image kept for raycast detection only
        Image playZoneImg = playZone.GetComponent<Image>();
        playZoneImg.color = new Color(0, 0, 0, 0);
        playZoneImg.raycastTarget = true;

        // ====================================================================
        // 4. DECK PILE (bottom-left)
        // ====================================================================
        GameObject deckPile = CreatePanel(root, "DeckPile",
            anchorMin: new Vector2(0f, 0f), anchorMax: new Vector2(0f, 0f),
            pivot: new Vector2(0f, 0f),
            offsetMin: new Vector2(20f, 20f), offsetMax: new Vector2(120f, 100f));
        deckPile.GetComponent<Image>().color = ColPanel;

        GameObject deckLabel = CreateTMPLabel(deckPile.transform, "DeckCountText",
            text: "Deck\n10", fontSize: 18, colour: ColText,
            anchoredPos: Vector2.zero, size: Vector2.zero,
            anchorMin: Vector2.zero, anchorMax: Vector2.one, pivot: new Vector2(0.5f, 0.5f),
            font: kenneyFont);
        deckLabel.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;
        deckLabel.GetComponent<TMP_Text>().fontStyle = FontStyles.Bold;

        // ====================================================================
        // 5. DISCARD PILE (bottom-right)
        // ====================================================================
        GameObject discardPile = CreatePanel(root, "DiscardPile",
            anchorMin: new Vector2(1f, 0f), anchorMax: new Vector2(1f, 0f),
            pivot: new Vector2(1f, 0f),
            offsetMin: new Vector2(-120f, 20f), offsetMax: new Vector2(-20f, 100f));
        discardPile.GetComponent<Image>().color = ColPanel;

        GameObject discardLabel = CreateTMPLabel(discardPile.transform, "DiscardCountText",
            text: "Discard\n0", fontSize: 18, colour: ColText,
            anchoredPos: Vector2.zero, size: Vector2.zero,
            anchorMin: Vector2.zero, anchorMax: Vector2.one, pivot: new Vector2(0.5f, 0.5f),
            font: kenneyFont);
        discardLabel.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;
        discardLabel.GetComponent<TMP_Text>().fontStyle = FontStyles.Bold;

        // ====================================================================
        // 6. TURN COUNTER (top of encounter panel area, left of it)
        // ====================================================================
        GameObject turnCounter = CreateTMPLabel(root, "TurnCounterText",
            text: "Turn 1", fontSize: 20, colour: ColSubText,
            anchoredPos: new Vector2(-340f, -120f), size: new Vector2(160f, 36f),
            anchorMin: new Vector2(1f, 1f), anchorMax: new Vector2(1f, 1f),
            pivot: new Vector2(1f, 1f), font: kenneyFont);
        turnCounter.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Right;

        // Wire turn counter to EncounterPanel
        encSO.FindProperty("turnCounterText").objectReferenceValue = turnCounter.GetComponent<TMP_Text>();
        encSO.ApplyModifiedProperties();

        // ====================================================================
        // 7. END TURN BUTTON (bottom-right)
        // ====================================================================
        Sprite btnSprite = LoadButtonSprite();
        GameObject endTurnGO = new GameObject("EndTurnButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        endTurnGO.transform.SetParent(root, false);
        RectTransform endTurnRT = endTurnGO.GetComponent<RectTransform>();
        endTurnRT.anchorMin = new Vector2(1f, 0f);
        endTurnRT.anchorMax = new Vector2(1f, 0f);
        endTurnRT.pivot     = new Vector2(1f, 0f);
        endTurnRT.anchoredPosition = new Vector2(-20f, 110f);
        endTurnRT.sizeDelta = new Vector2(180f, 60f);

        Image endTurnImg = endTurnGO.GetComponent<Image>();
        if (btnSprite != null)
        {
            endTurnImg.sprite = btnSprite;
            endTurnImg.type   = Image.Type.Sliced;
            endTurnImg.color  = ColEndTurn;
        }
        else
        {
            endTurnImg.color = ColEndTurn;
        }

        Button endTurnBtn = endTurnGO.GetComponent<Button>();
        ColorBlock endTurnColors = endTurnBtn.colors;
        endTurnColors.normalColor      = ColEndTurn;
        endTurnColors.highlightedColor = ColEndTurnHover;
        endTurnColors.pressedColor     = new Color(0.6f, 0.25f, 0.05f, 1f);
        endTurnColors.selectedColor    = ColEndTurn;
        endTurnBtn.colors = endTurnColors;

        // Button label
        GameObject endTurnText = CreateTMPLabel(endTurnGO.transform, "EndTurnLabel",
            text: "END TURN", fontSize: 22, colour: Color.white,
            anchoredPos: Vector2.zero, size: Vector2.zero,
            anchorMin: Vector2.zero, anchorMax: Vector2.one, pivot: new Vector2(0.5f, 0.5f),
            font: kenneyFont);
        endTurnText.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;
        endTurnText.GetComponent<TMP_Text>().fontStyle = FontStyles.Bold;

        // ====================================================================
        // 8. HAND VIEW ANCHOR (bottom-centre placeholder)
        // ====================================================================
        GameObject handAnchor = new GameObject("HandViewAnchor", typeof(RectTransform));
        handAnchor.transform.SetParent(root, false);
        RectTransform handRT = handAnchor.GetComponent<RectTransform>();
        handRT.anchorMin = new Vector2(0.1f, 0f);
        handRT.anchorMax = new Vector2(0.9f, 0f);
        handRT.pivot     = new Vector2(0.5f, 0f);
        handRT.anchoredPosition = new Vector2(0f, 10f);
        handRT.sizeDelta = new Vector2(0f, 220f);

        // Faint label so it's visible in editor
        GameObject handHint = CreateTMPLabel(handAnchor.transform, "HandViewHint",
            text: "← Place HandView prefab here →", fontSize: 16,
            colour: new Color(0.5f, 0.5f, 0.5f, 0.4f),
            anchoredPos: new Vector2(0f, 110f), size: new Vector2(600f, 30f),
            font: kenneyFont);
        handHint.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;
        handHint.GetComponent<TMP_Text>().fontStyle = FontStyles.Italic;

        // ====================================================================
        // 9. GAME UI CONTROLLER (on the canvas root)
        // ====================================================================
        GameUIController controller = canvasGO.AddComponent<GameUIController>();
        SerializedObject ctrlSO = new SerializedObject(controller);
        ctrlSO.FindProperty("hudPanel").objectReferenceValue         = gameHUD;
        ctrlSO.FindProperty("encounterPanel").objectReferenceValue   = encPanel;
        ctrlSO.FindProperty("deckCountText").objectReferenceValue    = deckLabel.GetComponent<TMP_Text>();
        ctrlSO.FindProperty("discardCountText").objectReferenceValue = discardLabel.GetComponent<TMP_Text>();
        ctrlSO.FindProperty("endTurnButton").objectReferenceValue    = endTurnBtn;
        ctrlSO.FindProperty("playZone").objectReferenceValue         = playZone;
        ctrlSO.ApplyModifiedProperties();

        // ====================================================================
        // Mark dirty and save
        // ====================================================================
        EditorUtility.SetDirty(canvasGO);
        EditorSceneManager.MarkSceneDirty(canvasGO.scene);

        Debug.Log("[GameSceneBuilder] GameCanvas built successfully. " +
                  "Drag the HandView prefab onto HandViewAnchor, then save the scene.");
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static GameObject CreatePanel(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot     = pivot;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        go.GetComponent<Image>().color = ColPanel;
        return go;
    }

    private static GameObject CreateImage(Transform parent, string name, Color colour,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 anchoredPos)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin       = anchorMin;
        rt.anchorMax       = anchorMax;
        rt.pivot           = pivot;
        rt.sizeDelta       = size;
        rt.anchoredPosition = anchoredPos;
        go.GetComponent<Image>().color = colour;
        return go;
    }

    private static GameObject CreateTMPLabel(Transform parent, string name,
        string text, float fontSize, Color colour,
        Vector2 anchoredPos, Vector2 size,
        Vector2? anchorMin = null, Vector2? anchorMax = null, Vector2? pivot = null,
        TMP_FontAsset font = null)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = anchorMin ?? new Vector2(0.5f, 0.5f);
        rt.anchorMax        = anchorMax ?? new Vector2(0.5f, 0.5f);
        rt.pivot            = pivot    ?? new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = size;
        rt.anchoredPosition = anchoredPos;

        TMP_Text tmp = go.GetComponent<TMP_Text>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.color     = colour;
        tmp.raycastTarget = false;
        if (font != null) tmp.font = font;
        return go;
    }
}
