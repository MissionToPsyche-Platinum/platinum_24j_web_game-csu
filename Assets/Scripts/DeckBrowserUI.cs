using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Full-screen overlay listing draw pile cards in draw order (next draw first, left to right).
/// Toggle via <see cref="GameUIController"/> wiring to the Deck pile button.
/// </summary>
public class DeckBrowserUI : MonoBehaviour
{
    public static bool IsDeckBrowserOpen { get; private set; }

    [SerializeField] private GameObject cardPrefab;

    private CanvasGroup _rootGroup;
    private RectTransform _content;
    private TMP_Text _titleText;
    private DeckManager _deck;
    private bool _built;

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
        if (IsDeckBrowserOpen)
            Hide();
        else
            Show();
    }

    public void Show()
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
        IReadOnlyList<CardData> cards = _deck.GetDrawPileOrderedNextDrawFirst();
        _titleText.text = cards.Count == 0
            ? "Deck (empty — will shuffle from discard when you draw)"
            : $"Cards to draw ({cards.Count}) — next draw is leftmost";

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
                rt.localScale = Vector3.one * 0.52f;
        }
    }

    private void EnsureBuilt()
    {
        if (_built)
            return;
        _built = true;

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

        var panel = new GameObject("Panel");
        var panelRt = panel.AddComponent<RectTransform>();
        panelRt.SetParent(rootRt, false);
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(960f, 440f);
        panelRt.anchoredPosition = Vector2.zero;
        var panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.08f, 0.09f, 0.14f, 0.98f);
        panelImg.raycastTarget = true;

        var titleGo = new GameObject("Title", typeof(RectTransform));
        titleGo.transform.SetParent(panelRt, false);
        var titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0, 1);
        titleRt.anchorMax = new Vector2(1, 1);
        titleRt.pivot = new Vector2(0.5f, 1);
        titleRt.anchoredPosition = new Vector2(0, -10);
        titleRt.sizeDelta = new Vector2(-24, 44);
        _titleText = titleGo.AddComponent<TextMeshProUGUI>();
        _titleText.fontSize = 26;
        _titleText.alignment = TextAlignmentOptions.Center;
        _titleText.text = "Deck";

        var scrollGo = new GameObject("Scroll", typeof(RectTransform));
        var scrollRt = scrollGo.GetComponent<RectTransform>();
        scrollRt.SetParent(panelRt, false);
        scrollRt.anchorMin = new Vector2(0, 0);
        scrollRt.anchorMax = new Vector2(1, 1);
        scrollRt.offsetMin = new Vector2(16, 52);
        scrollRt.offsetMax = new Vector2(-16, -54);

        var scroll = scrollGo.AddComponent<ScrollRect>();
        scroll.horizontal = true;
        scroll.vertical = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;

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

        var contentGo = new GameObject("Content", typeof(RectTransform));
        _content = contentGo.GetComponent<RectTransform>();
        _content.SetParent(viewportRt, false);
        _content.anchorMin = new Vector2(0, 0.5f);
        _content.anchorMax = new Vector2(0, 0.5f);
        _content.pivot = new Vector2(0, 0.5f);
        _content.anchoredPosition = Vector2.zero;
        var hlg = contentGo.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 14;
        hlg.padding = new RectOffset(8, 8, 8, 8);
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        var fitter = contentGo.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.content = _content;
        scroll.viewport = viewportRt;

        var closeGo = new GameObject("CloseButton", typeof(RectTransform));
        var closeRt = closeGo.GetComponent<RectTransform>();
        closeRt.SetParent(panelRt, false);
        closeRt.anchorMin = new Vector2(0.5f, 0);
        closeRt.anchorMax = new Vector2(0.5f, 0);
        closeRt.pivot = new Vector2(0.5f, 0);
        closeRt.anchoredPosition = new Vector2(0, 10);
        closeRt.sizeDelta = new Vector2(200f, 36f);
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
        closeTmp.fontSize = 20;
        closeTmp.alignment = TextAlignmentOptions.Center;
    }
}
