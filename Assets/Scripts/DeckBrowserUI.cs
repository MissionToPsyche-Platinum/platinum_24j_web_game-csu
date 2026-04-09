using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Full-screen overlay listing draw pile cards in a scrollable grid.
/// Toggle via <see cref="GameUIController"/> wiring to the Deck pile button.
/// </summary>
public class DeckBrowserUI : MonoBehaviour
{
    private enum BrowserMode
    {
        Deck,
        Discard
    }

    public static bool IsDeckBrowserOpen { get; private set; }

    [SerializeField] private GameObject cardPrefab;

    private CanvasGroup _rootGroup;
    private RectTransform _content;
    private ScrollRect _scroll;
    private TMP_Text _titleText;
    private DeckManager _deck;
    private bool _built;
    private BrowserMode _mode = BrowserMode.Deck;

    [Header("Card Display")]
    [Tooltip("Scale applied to each card instance in the grid.")]
    [SerializeField] private float cardScale = 0.95f;

    [Tooltip("Width of each cell in the grid.")]
    [SerializeField] private float cellWidth = 200f;

    [Tooltip("Height of each cell in the grid.")]
    [SerializeField] private float cellHeight = 280f;

    [Tooltip("Spacing between cards in the grid.")]
    [SerializeField] private float gridSpacing = 20f;

    private void Start()
    {
        _deck = FindPrimaryDeck();
        if (_deck != null)
            _deck.OnHandChanged += OnDeckHandChanged;
    }

    private void OnDestroy()
    {
        if (_deck != null)
            _deck.OnHandChanged -= OnDeckHandChanged;
    }

    private void OnDeckHandChanged()
    {
        if (IsDeckBrowserOpen && _built && _content != null)
            Repopulate();
    }

    private static DeckManager FindPrimaryDeck()
    {
        var all = Object.FindObjectsByType<DeckManager>(FindObjectsSortMode.None);
        foreach (var d in all)
        {
            if (d != null && !d.UsesAiResourceWallet)
                return d;
        }

        return all.Length > 0 ? all[0] : null;
    }

    public void Toggle()
    {
        ToggleDeck();
    }

    public void ToggleDeck()
    {
        if (IsDeckBrowserOpen && _mode == BrowserMode.Deck)
            Hide();
        else
            ShowDeck();
    }

    public void ToggleDiscard()
    {
        if (IsDeckBrowserOpen && _mode == BrowserMode.Discard)
            Hide();
        else
            ShowDiscard();
    }

    public void ShowDeck()
    {
        _mode = BrowserMode.Deck;
        ShowCurrentMode();
    }

    public void ShowDiscard()
    {
        _mode = BrowserMode.Discard;
        ShowCurrentMode();
    }

    private void ShowCurrentMode()
    {
        EnsureBuilt();
        _deck = FindPrimaryDeck();
        if (_deck == null)
        {
            Debug.LogWarning("[DeckBrowserUI] No DeckManager found.");
            return;
        }

        Repopulate();
        IsDeckBrowserOpen = true;
        _rootGroup.alpha = 1f;
        _rootGroup.blocksRaycasts = true;
        _rootGroup.interactable = true;
        _rootGroup.transform.SetAsLastSibling();
    }

    public void Hide()
    {
        if (_rootGroup == null)
            return;
        IsDeckBrowserOpen = false;
        _rootGroup.alpha = 0f;
        _rootGroup.blocksRaycasts = false;
        _rootGroup.interactable = false;
        ClearContent();
    }

    private void ClearContent()
    {
        if (_content == null)
            return;
        for (int i = _content.childCount - 1; i >= 0; i--)
            Destroy(_content.GetChild(i).gameObject);
    }

    private void Repopulate()
    {
        ClearContent();
        IReadOnlyList<CardData> cards;
        if (_mode == BrowserMode.Discard)
        {
            cards = _deck.GetDiscardPileOrderedNewestFirst();
            _titleText.text = cards.Count == 0
                ? "Discard pile (empty)"
                : $"Discard pile ({cards.Count}) - newest is top-left";
        }
        else
        {
            cards = _deck.GetDrawPileOrderedNextDrawFirst();
            _titleText.text = cards.Count == 0
                ? "Deck (empty - will shuffle from discard when you draw)"
                : $"Cards to draw ({cards.Count}) - next draw is top-left";
        }

        if (cardPrefab == null)
            cardPrefab = Resources.Load<GameObject>("CardView");
        if (cardPrefab == null)
        {
            var empty = new GameObject("NoPrefab", typeof(RectTransform));
            empty.transform.SetParent(_content, false);
            var t = empty.AddComponent<TextMeshProUGUI>();
            t.text = "CardView prefab missing in Resources.";
            t.fontSize = 22;
            t.alignment = TextAlignmentOptions.Center;
            return;
        }

        foreach (var cd in cards)
        {
            if (cd == null)
                continue;
            var go = Instantiate(cardPrefab, _content);
            var view = go.GetComponent<CardView>();
            if (view != null)
                view.BindForGallery(cd);
            var rt = go.GetComponent<RectTransform>();
            if (rt != null)
                rt.localScale = Vector3.one * cardScale;

            // Strip interaction components — these cards are display-only
            StripInteractionComponents(go);

            // Add a LayoutElement so the grid cell sizes are respected
            var le = go.GetComponent<LayoutElement>();
            if (le == null)
                le = go.AddComponent<LayoutElement>();
            le.preferredWidth = cellWidth;
            le.preferredHeight = cellHeight;
        }

        // After populating, force layout rebuild so ContentSizeFitter updates
        LayoutRebuilder.ForceRebuildLayoutImmediate(_content);

        // Scroll back to top
        if (_scroll != null)
            _scroll.verticalNormalizedPosition = 1f;
    }

    private void EnsureBuilt()
    {
        if (_built)
            return;
        _built = true;

        // ── Root: full-screen dimmed overlay ──
        var root = new GameObject("DeckBrowserRoot");
        var rootRt = root.AddComponent<RectTransform>();
        rootRt.SetParent(transform, false);
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;
        root.layer = gameObject.layer;

        var dim = root.AddComponent<Image>();
        dim.color = new Color(0.02f, 0.02f, 0.06f, 0.92f);
        dim.raycastTarget = true;

        _rootGroup = root.AddComponent<CanvasGroup>();
        _rootGroup.alpha = 0f;
        _rootGroup.blocksRaycasts = false;
        _rootGroup.interactable = false;

        // ── Panel: full-screen with small margin ──
        var panel = new GameObject("Panel");
        var panelRt = panel.AddComponent<RectTransform>();
        panelRt.SetParent(rootRt, false);
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = new Vector2(24f, 24f);   // small margin from screen edges
        panelRt.offsetMax = new Vector2(-24f, -24f);
        var panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.08f, 0.09f, 0.14f, 0.98f);
        panelImg.raycastTarget = true;

        // ── Title ──
        var titleGo = new GameObject("Title", typeof(RectTransform));
        titleGo.transform.SetParent(panelRt, false);
        var titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0, 1);
        titleRt.anchorMax = new Vector2(1, 1);
        titleRt.pivot = new Vector2(0.5f, 1);
        titleRt.anchoredPosition = new Vector2(0, -12);
        titleRt.sizeDelta = new Vector2(-24, 48);
        _titleText = titleGo.AddComponent<TextMeshProUGUI>();
        _titleText.fontSize = 28;
        _titleText.alignment = TextAlignmentOptions.Center;
        _titleText.text = "Deck";

        // ── Scroll area: fills panel between title and close button ──
        var scrollGo = new GameObject("Scroll", typeof(RectTransform));
        var scrollRt = scrollGo.GetComponent<RectTransform>();
        scrollRt.SetParent(panelRt, false);
        scrollRt.anchorMin = Vector2.zero;
        scrollRt.anchorMax = Vector2.one;
        scrollRt.offsetMin = new Vector2(16, 56);    // above close button area
        scrollRt.offsetMax = new Vector2(-16, -64);   // below title

        _scroll = scrollGo.AddComponent<ScrollRect>();
        _scroll.horizontal = false;
        _scroll.vertical = true;
        _scroll.movementType = ScrollRect.MovementType.Clamped;
        _scroll.scrollSensitivity = 40f;

        // ── Viewport ──
        var viewportGo = new GameObject("Viewport", typeof(RectTransform));
        var viewportRt = viewportGo.GetComponent<RectTransform>();
        viewportRt.SetParent(scrollRt, false);
        viewportRt.anchorMin = Vector2.zero;
        viewportRt.anchorMax = Vector2.one;
        viewportRt.offsetMin = Vector2.zero;
        viewportRt.offsetMax = Vector2.zero;
        viewportGo.AddComponent<Image>().color = new Color(0.06f, 0.07f, 0.1f, 1f);
        var mask = viewportGo.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        // ── Content: grid layout ──
        var contentGo = new GameObject("Content", typeof(RectTransform));
        _content = contentGo.GetComponent<RectTransform>();
        _content.SetParent(viewportRt, false);
        // Stretch width to viewport, grow height from top
        _content.anchorMin = new Vector2(0, 1);
        _content.anchorMax = new Vector2(1, 1);
        _content.pivot = new Vector2(0.5f, 1);
        _content.anchoredPosition = Vector2.zero;
        _content.sizeDelta = new Vector2(0, 0);

        var grid = contentGo.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(cellWidth, cellHeight);
        grid.spacing = new Vector2(gridSpacing, gridSpacing);
        grid.padding = new RectOffset(16, 16, 16, 16);
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.constraint = GridLayoutGroup.Constraint.Flexible;

        var fitter = contentGo.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _scroll.content = _content;
        _scroll.viewport = viewportRt;

        // ── Vertical scrollbar (only visible when content overflows) ──
        var scrollbarGo = new GameObject("Scrollbar", typeof(RectTransform));
        var scrollbarRt = scrollbarGo.GetComponent<RectTransform>();
        scrollbarRt.SetParent(scrollRt, false);
        scrollbarRt.anchorMin = new Vector2(1, 0);
        scrollbarRt.anchorMax = new Vector2(1, 1);
        scrollbarRt.pivot = new Vector2(1, 0.5f);
        scrollbarRt.anchoredPosition = new Vector2(14, 0);
        scrollbarRt.sizeDelta = new Vector2(8f, 0);
        var scrollbarImg = scrollbarGo.AddComponent<Image>();
        scrollbarImg.color = new Color(0.15f, 0.16f, 0.22f, 0.6f);
        var scrollbar = scrollbarGo.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        var handleGo = new GameObject("Handle", typeof(RectTransform));
        var handleRt = handleGo.GetComponent<RectTransform>();
        handleRt.SetParent(scrollbarRt, false);
        handleRt.anchorMin = Vector2.zero;
        handleRt.anchorMax = Vector2.one;
        handleRt.offsetMin = Vector2.zero;
        handleRt.offsetMax = Vector2.zero;
        var handleImg = handleGo.AddComponent<Image>();
        handleImg.color = new Color(0.4f, 0.42f, 0.55f, 0.8f);
        scrollbar.handleRect = handleRt;
        scrollbar.targetGraphic = handleImg;

        _scroll.verticalScrollbar = scrollbar;
        _scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

        // ── Close button ──
        var closeGo = new GameObject("CloseButton", typeof(RectTransform));
        var closeRt = closeGo.GetComponent<RectTransform>();
        closeRt.SetParent(panelRt, false);
        closeRt.anchorMin = new Vector2(0.5f, 0);
        closeRt.anchorMax = new Vector2(0.5f, 0);
        closeRt.pivot = new Vector2(0.5f, 0);
        closeRt.anchoredPosition = new Vector2(0, 10);
        closeRt.sizeDelta = new Vector2(220f, 40f);
        var closeImg = closeGo.AddComponent<Image>();
        closeImg.color = new Color(0.25f, 0.28f, 0.38f, 1f);
        var closeBtn = closeGo.AddComponent<Button>();
        closeBtn.targetGraphic = closeImg;
        closeBtn.onClick.AddListener(Hide);

        var closeLabelGo = new GameObject("Label", typeof(RectTransform));
        closeLabelGo.transform.SetParent(closeRt, false);
        var closeLabelRt = closeLabelGo.GetComponent<RectTransform>();
        closeLabelRt.anchorMin = Vector2.zero;
        closeLabelRt.anchorMax = Vector2.one;
        closeLabelRt.offsetMin = Vector2.zero;
        closeLabelRt.offsetMax = Vector2.zero;
        var closeTmp = closeLabelGo.AddComponent<TextMeshProUGUI>();
        closeTmp.text = "Close";
        closeTmp.fontSize = 22;
        closeTmp.alignment = TextAlignmentOptions.Center;
    }

    /// <summary>
    /// Removes pointer-interaction components from a gallery card so it doesn't
    /// intercept scroll input or block the close button.
    /// </summary>
    private static void StripInteractionComponents(GameObject go)
    {
        // Remove from root and all children
        foreach (var ct in go.GetComponentsInChildren<CardTrigger>(true))
            Destroy(ct);
        foreach (var dh in go.GetComponentsInChildren<CardDragHandler>(true))
            Destroy(dh);

        // Ensure the card itself doesn't block scrolling gestures.
        // A CanvasGroup on the card with blocksRaycasts=false lets pointer
        // events pass through to the ScrollRect while keeping the card visible.
        var cg = go.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = go.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;
    }
}
