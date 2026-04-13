using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Full-screen card gallery overlay. Mirrors DeckBrowserUI construction pattern.
/// Call TryShow() from MainMenuUI.OnViewCards(). Return button calls Hide() and restores main menu.
/// </summary>
public class CardGalleryOverlay : MonoBehaviour
{
    private static CardGalleryOverlay _instance;

    [SerializeField] private GameObject cardPrefab;

    [Header("Card Display")]
    [SerializeField] private float cardScale   = 0.95f;
    [SerializeField] private float cellWidth   = 200f;
    [SerializeField] private float cellHeight  = 280f;
    [SerializeField] private float gridSpacing = 20f;

    // ── Runtime UI ──
    private CanvasGroup    _rootGroup;
    private RectTransform  _content;
    private ScrollRect     _scroll;
    private TMP_Text       _titleText;
    private bool           _built;

    // ── Pre-built card instances ──
    private readonly List<GameObject> _allCardGOs = new List<GameObject>();
    private bool _cardsBuilt;

    // ── Colors matching main menu buttons ──
    private static readonly Color BtnNormal    = new Color(0.255f, 0.353f, 0.749f, 1f);
    private static readonly Color BtnHighlight = new Color(0.310f, 0.420f, 0.820f, 1f);
    private static readonly Color BtnPressed   = new Color(0.200f, 0.290f, 0.650f, 1f);

    public static bool IsOpen { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { _instance = null; }

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    // ────────────────────────────────────────────────────────────────
    //  Public API
    // ────────────────────────────────────────────────────────────────

    public static bool TryShow()
    {
        if (_instance == null)
            _instance = FindFirstObjectByType<CardGalleryOverlay>(FindObjectsInactive.Include);
        if (_instance == null) { Debug.LogWarning("[CardGalleryOverlay] No instance in scene."); return false; }
        _instance.Show();
        return true;
    }

    public static bool TryHide()
    {
        if (_instance == null || !IsOpen) return false;
        _instance.Hide();
        return true;
    }

    public void Show()
    {
        EnsureBuilt();
        EnsureCardsBuilt();
        IsOpen = true;
        _rootGroup.alpha = 1f;
        _rootGroup.blocksRaycasts = true;
        _rootGroup.interactable   = true;
        _rootGroup.transform.SetAsLastSibling();
        if (_scroll != null)
            _scroll.verticalNormalizedPosition = 1f;
    }

    public void Hide()
    {
        if (_rootGroup == null) return;
        IsOpen = false;
        _rootGroup.alpha = 0f;
        _rootGroup.blocksRaycasts = false;
        _rootGroup.interactable   = false;

        // Restore main menu
        var mainMenu = FindFirstObjectByType<MainMenuUI>(FindObjectsInactive.Include);
        if (mainMenu != null && mainMenu.mainMenuPanel != null)
            mainMenu.mainMenuPanel.SetActive(true);
    }

    // ────────────────────────────────────────────────────────────────
    //  Card instantiation — done ONCE, then filtered via SetActive
    // ────────────────────────────────────────────────────────────────

    private void EnsureCardsBuilt()
    {
        if (_cardsBuilt) return;
        _cardsBuilt = true;

        if (cardPrefab == null)
            cardPrefab = Resources.Load<GameObject>("CardView");

        if (cardPrefab == null)
        {
            Debug.LogError("[CardGalleryOverlay] CardView prefab missing from Resources.");
            return;
        }

        var allCards = DeckManager.GetAllCardData();
        foreach (var cd in allCards)
        {
            if (cd == null) continue;

            var go = Instantiate(cardPrefab, _content);
            var view = go.GetComponent<CardView>();
            if (view != null)
                view.BindForGallery(cd);

            var rt = go.GetComponent<RectTransform>();
            if (rt != null)
                rt.localScale = Vector3.one * cardScale;

            StripInteraction(go);

            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.preferredWidth  = cellWidth;
            le.preferredHeight = cellHeight;

            _allCardGOs.Add(go);
        }

        if (_titleText != null)
            _titleText.text = $"Card Gallery ({_allCardGOs.Count})";

        LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
    }

    private static void StripInteraction(GameObject go)
    {
        foreach (var dh in go.GetComponentsInChildren<CardDragHandler>(true))
            Destroy(dh);
        foreach (var ch in go.GetComponentsInChildren<CardHover>(true))
            Destroy(ch);

        var cg = go.GetComponent<CanvasGroup>() ?? go.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable   = false;
    }


    // ────────────────────────────────────────────────────────────────
    //  UI Construction  (mirrors DeckBrowserUI.EnsureBuilt)
    // ────────────────────────────────────────────────────────────────

    private void EnsureBuilt()
    {
        if (_built) return;
        _built = true;

        // Find parent canvas
        var canvasGO = GameObject.Find("Main_Canvas");
        Transform parent = canvasGO != null ? canvasGO.transform : transform;

        // ── Root fullscreen overlay ──
        var root   = new GameObject("CardGalleryRoot");
        var rootRt = root.AddComponent<RectTransform>();
        rootRt.SetParent(parent, false);
        rootRt.anchorMin  = Vector2.zero;
        rootRt.anchorMax  = Vector2.one;
        rootRt.offsetMin  = Vector2.zero;
        rootRt.offsetMax  = Vector2.zero;
        root.layer = gameObject.layer;

        // Background texture
        var bgImg = root.AddComponent<Image>();
        var bgTex = Resources.Load<Texture2D>("Options_CardView_Background");
        if (bgTex != null)
        {
            bgImg.sprite = Sprite.Create(bgTex,
                new Rect(0, 0, bgTex.width, bgTex.height), new Vector2(0.5f, 0.5f));
            bgImg.type = Image.Type.Simple;
        }
        else
        {
            bgImg.color = new Color(0.06f, 0.08f, 0.16f, 1f);
        }
        bgImg.raycastTarget = true;

        _rootGroup = root.AddComponent<CanvasGroup>();
        _rootGroup.alpha = 0f;
        _rootGroup.blocksRaycasts = false;
        _rootGroup.interactable   = false;

        // ── Panel (small inset) ──
        var panel   = new GameObject("Panel");
        var panelRt = panel.AddComponent<RectTransform>();
        panelRt.SetParent(rootRt, false);
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = new Vector2(24f, 24f);
        panelRt.offsetMax = new Vector2(-24f, -24f);
        panel.AddComponent<Image>().color = new Color(0.06f, 0.07f, 0.12f, 0.92f);

        // ── Title ──
        var titleGO = new GameObject("Title", typeof(RectTransform));
        titleGO.transform.SetParent(panelRt, false);
        var titleRt = titleGO.GetComponent<RectTransform>();
        titleRt.anchorMin        = new Vector2(0f, 1f);
        titleRt.anchorMax        = new Vector2(1f, 1f);
        titleRt.pivot            = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -12f);
        titleRt.sizeDelta        = new Vector2(-24f, 48f);
        _titleText = titleGO.AddComponent<TextMeshProUGUI>();
        _titleText.fontSize  = 28;
        _titleText.alignment = TextAlignmentOptions.Center;
        _titleText.text      = "Card Gallery";

        // ── Return button (top-right) ──
        var retGO = new GameObject("ReturnButton", typeof(RectTransform));
        retGO.transform.SetParent(panelRt, false);
        var retRt = retGO.GetComponent<RectTransform>();
        retRt.anchorMin        = new Vector2(1f, 1f);
        retRt.anchorMax        = new Vector2(1f, 1f);
        retRt.pivot            = new Vector2(1f, 1f);
        retRt.anchoredPosition = new Vector2(-12f, -10f);
        retRt.sizeDelta        = new Vector2(160f, 44f);
        var retImg = retGO.AddComponent<Image>();
        retImg.color = BtnNormal;
        var retBtn = retGO.AddComponent<Button>();
        retBtn.targetGraphic = retImg;
        var retCb = retBtn.colors;
        retCb.normalColor = BtnNormal; retCb.highlightedColor = BtnHighlight; retCb.pressedColor = BtnPressed;
        retBtn.colors = retCb;
        retBtn.onClick.AddListener(Hide);
        var retLabel = new GameObject("Label", typeof(RectTransform));
        retLabel.transform.SetParent(retGO.transform, false);
        var retLabelRt = retLabel.GetComponent<RectTransform>();
        retLabelRt.anchorMin = Vector2.zero; retLabelRt.anchorMax = Vector2.one;
        retLabelRt.offsetMin = Vector2.zero; retLabelRt.offsetMax = Vector2.zero;
        var retTmp = retLabel.AddComponent<TextMeshProUGUI>();
        retTmp.text = "Return"; retTmp.fontSize = 20; retTmp.fontStyle = FontStyles.Bold;
        retTmp.alignment = TextAlignmentOptions.Center; retTmp.color = Color.white;

        // ── Scroll area ──
        var scrollGO = new GameObject("Scroll", typeof(RectTransform));
        var scrollRt = scrollGO.GetComponent<RectTransform>();
        scrollRt.SetParent(panelRt, false);
        scrollRt.anchorMin = Vector2.zero;
        scrollRt.anchorMax = Vector2.one;
        scrollRt.offsetMin = new Vector2(16f, 56f);   // above close gap
        scrollRt.offsetMax = new Vector2(-16f, -72f); // below title

        _scroll = scrollGO.AddComponent<ScrollRect>();
        _scroll.horizontal         = false;
        _scroll.vertical           = true;
        _scroll.movementType       = ScrollRect.MovementType.Clamped;
        _scroll.scrollSensitivity  = 40f;

        // Viewport
        var vpGO = new GameObject("Viewport", typeof(RectTransform));
        var vpRt = vpGO.GetComponent<RectTransform>();
        vpRt.SetParent(scrollRt, false);
        vpRt.anchorMin = Vector2.zero;
        vpRt.anchorMax = Vector2.one;
        vpRt.offsetMin = Vector2.zero;
        vpRt.offsetMax = Vector2.zero;
        vpGO.AddComponent<Image>().color = new Color(0.06f, 0.07f, 0.10f, 1f);
        var mask = vpGO.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        // Content
        var contentGO = new GameObject("Content", typeof(RectTransform));
        _content = contentGO.GetComponent<RectTransform>();
        _content.SetParent(vpRt, false);
        _content.anchorMin        = new Vector2(0f, 1f);
        _content.anchorMax        = new Vector2(1f, 1f);
        _content.pivot            = new Vector2(0.5f, 1f);
        _content.anchoredPosition = Vector2.zero;
        _content.sizeDelta        = Vector2.zero;

        var grid = contentGO.AddComponent<GridLayoutGroup>();
        grid.cellSize       = new Vector2(cellWidth, cellHeight);
        grid.spacing        = new Vector2(gridSpacing, gridSpacing);
        grid.padding        = new RectOffset(16, 16, 16, 16);
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.constraint     = GridLayoutGroup.Constraint.Flexible;

        var fitter = contentGO.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

        _scroll.content  = _content;
        _scroll.viewport = vpRt;

        // Scrollbar
        var sbGO = new GameObject("Scrollbar", typeof(RectTransform));
        var sbRt = sbGO.GetComponent<RectTransform>();
        sbRt.SetParent(scrollRt, false);
        sbRt.anchorMin        = new Vector2(1f, 0f);
        sbRt.anchorMax        = new Vector2(1f, 1f);
        sbRt.pivot            = new Vector2(1f, 0.5f);
        sbRt.anchoredPosition = new Vector2(14f, 0f);
        sbRt.sizeDelta        = new Vector2(8f, 0f);
        sbGO.AddComponent<Image>().color = new Color(0.15f, 0.16f, 0.22f, 0.6f);
        var sb = sbGO.AddComponent<Scrollbar>();
        sb.direction = Scrollbar.Direction.BottomToTop;

        var hndGO = new GameObject("Handle", typeof(RectTransform));
        var hndRt = hndGO.GetComponent<RectTransform>();
        hndRt.SetParent(sbRt, false);
        hndRt.anchorMin = Vector2.zero; hndRt.anchorMax = Vector2.one;
        hndRt.offsetMin = Vector2.zero; hndRt.offsetMax = Vector2.zero;
        var hndImg = hndGO.AddComponent<Image>();
        hndImg.color = new Color(0.4f, 0.42f, 0.55f, 0.8f);
        sb.handleRect     = hndRt;
        sb.targetGraphic  = hndImg;

        _scroll.verticalScrollbar            = sb;
        _scroll.verticalScrollbarVisibility  = ScrollRect.ScrollbarVisibility.AutoHide;
    }
}
