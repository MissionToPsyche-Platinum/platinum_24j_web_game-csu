using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Full-screen credits overlay that lists the development team and shows the
/// NASA / Psyche / CSU sponsor disclaimer text. Mirrors the construction pattern
/// used by <see cref="CardGalleryOverlay"/>, but auto-instantiates itself on first
/// use so it does not require a placement in the scene.
///
/// Triggered from <see cref="MainMenuUI.OnOpenCredits"/>.
/// </summary>
public class CreditsOverlay : MonoBehaviour
{
    private static CreditsOverlay _instance;

    private CanvasGroup   _rootGroup;
    private RectTransform _rootRect;
    private bool          _built;

    private static readonly Color BtnNormal    = new Color(0.255f, 0.353f, 0.749f, 1f);
    private static readonly Color BtnHighlight = new Color(0.310f, 0.420f, 0.820f, 1f);
    private static readonly Color BtnPressed   = new Color(0.200f, 0.290f, 0.650f, 1f);

    private const string TeamNames =
        "Cole Pochedley   •   Het Patel   •   Robel Mezgebe   •   Parth Patel";

    // Asset / tooling attributions shown above the sponsor disclaimer.
    private const string AssetCredits =
        "<b>Asset Credits</b>\n" +
        "• Images and sound effects in this game were generated with the assistance of AI tools.\n" +
        "• UI elements use the \"UI Pack - Sci-Fi\" from Kenney Game Assets (https://kenney.nl/assets/ui-pack-sci-fi).";

    // Disclaimer body, kept in two sections separated by a blank line so the
    // sponsor program guidance and the CSU/NASA legal notice read clearly.
    private const string DisclaimerBody =
        "The sponsor specification originates from the NASA Psyche Mission Student Collaborations program, " +
        "specifically the Psyche Capstone Program guidelines. Content for the game should draw from the Psyche " +
        "mission website, the Innovation Toolkit online courses, and papers available in the participant resource " +
        "folder, with additional questions directed to Cassie Bowman via Slack. The target audience is to be " +
        "determined by the development team with appropriate design practices for that chosen audience informed " +
        "by background research on best practices for educational game design.\n\n" +

        "The game should engage users in the excitement and innovation of the Psyche mission while helping them " +
        "feel connected to the mission's goals and discoveries. The game must not introduce or reinforce " +
        "misconceptions about the mission, asteroid science, or space exploration. Technical requirements specify " +
        "that the game should work well on consumer-grade computers with standard home WiFi connections, avoiding " +
        "designs that require gaming hardware or high-speed internet. Mobile compatibility is not required but " +
        "may be included if the team chooses to pursue that additional challenge.\n\n" +

        "Program guidelines prohibit shooting or violence of any kind, e-commerce functionality, user accounts " +
        "or profiles, mechanisms for players to contact each other, copyright infringement, and license misuse. " +
        "Acceptance criteria require that the game function on all standard browsers including Chrome, Mozilla " +
        "Firefox, and Edge, not crash user browsers or cause computer problems, contain no security " +
        "vulnerabilities, avoid introducing or reinforcing misconceptions, and be appropriate for the intended " +
        "audience. Teams wishing to include unrealistic elements such as surface landing should discuss with the " +
        "sponsor and frame such content as taking place in the far future, referring to any human presence as a " +
        "community rather than colony or settlement.\n\n" +

        "This work was created in partial fulfillment of Cleveland State University Capstone Course \"CIS 494\". " +
        "The work is a result of the Psyche Student Collaborations component of NASA's Psyche Mission " +
        "(https://psyche.ssl.berkeley.edu/). \"Psyche: A Journey to a Metal World\" [Contract number NNM16AA09C] " +
        "is part of the NASA Discovery Program mission to solar system targets. Trade names and trademarks of " +
        "ASU and NASA are used in this work for identification only. Their usage does not constitute an official " +
        "endorsement, either expressed or implied, by Arizona State University or National Aeronautics and Space " +
        "Administration. The content is solely the responsibility of the authors and does not necessarily " +
        "represent the official views of ASU or NASA.\n" +
        "https://psyche.ssl.berkeley.edu/";

    public static bool IsOpen { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _instance = null;
        IsOpen = false;
    }

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    /// <summary>
    /// Show the credits overlay. If no scene instance exists yet, one is created on the fly.
    /// Always returns true.
    /// </summary>
    public static bool TryShow()
    {
        if (_instance == null)
            _instance = FindFirstObjectByType<CreditsOverlay>(FindObjectsInactive.Include);

        if (_instance == null)
        {
            var host = new GameObject("CreditsOverlay");
            _instance = host.AddComponent<CreditsOverlay>();
        }

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
        IsOpen = true;
        _rootGroup.alpha = 1f;
        _rootGroup.blocksRaycasts = true;
        _rootGroup.interactable   = true;
        _rootRect.SetAsLastSibling();
    }

    public void Hide()
    {
        if (_rootGroup == null) return;
        IsOpen = false;
        _rootGroup.alpha = 0f;
        _rootGroup.blocksRaycasts = false;
        _rootGroup.interactable   = false;

        var mainMenu = FindFirstObjectByType<MainMenuUI>(FindObjectsInactive.Include);
        if (mainMenu != null && mainMenu.mainMenuPanel != null)
            mainMenu.mainMenuPanel.SetActive(true);
    }

    // ────────────────────────────────────────────────────────────────
    //  UI Construction
    // ────────────────────────────────────────────────────────────────

    private void EnsureBuilt()
    {
        if (_built) return;
        _built = true;

        // Prefer the existing Main_Canvas so we sit inside the same scaler/sorting context as the menu.
        Transform parent = ResolveCanvasParent();

        var root   = new GameObject("CreditsOverlayRoot");
        _rootRect  = root.AddComponent<RectTransform>();
        _rootRect.SetParent(parent, false);
        _rootRect.anchorMin = Vector2.zero;
        _rootRect.anchorMax = Vector2.one;
        _rootRect.offsetMin = Vector2.zero;
        _rootRect.offsetMax = Vector2.zero;
        root.layer = parent != null ? parent.gameObject.layer : gameObject.layer;

        var bgImg = root.AddComponent<Image>();
        bgImg.color = new Color(0.04f, 0.05f, 0.10f, 0.96f);
        bgImg.raycastTarget = true;

        _rootGroup = root.AddComponent<CanvasGroup>();
        _rootGroup.alpha = 0f;
        _rootGroup.blocksRaycasts = false;
        _rootGroup.interactable   = false;

        // Inset panel
        var panel   = new GameObject("Panel");
        var panelRt = panel.AddComponent<RectTransform>();
        panelRt.SetParent(_rootRect, false);
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = new Vector2(40f, 40f);
        panelRt.offsetMax = new Vector2(-40f, -40f);
        panel.AddComponent<Image>().color = new Color(0.06f, 0.07f, 0.12f, 0.92f);

        // Title
        var titleGO = new GameObject("Title", typeof(RectTransform));
        titleGO.transform.SetParent(panelRt, false);
        var titleRt = titleGO.GetComponent<RectTransform>();
        titleRt.anchorMin        = new Vector2(0f, 1f);
        titleRt.anchorMax        = new Vector2(1f, 1f);
        titleRt.pivot            = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -16f);
        titleRt.sizeDelta        = new Vector2(-32f, 56f);
        var title = titleGO.AddComponent<TextMeshProUGUI>();
        title.fontSize  = 36;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
        title.text      = "Credits";
        title.color     = Color.white;

        // Team line
        var teamGO = new GameObject("Team", typeof(RectTransform));
        teamGO.transform.SetParent(panelRt, false);
        var teamRt = teamGO.GetComponent<RectTransform>();
        teamRt.anchorMin        = new Vector2(0f, 1f);
        teamRt.anchorMax        = new Vector2(1f, 1f);
        teamRt.pivot            = new Vector2(0.5f, 1f);
        teamRt.anchoredPosition = new Vector2(0f, -76f);
        teamRt.sizeDelta        = new Vector2(-48f, 36f);
        var team = teamGO.AddComponent<TextMeshProUGUI>();
        team.fontSize  = 20;
        team.alignment = TextAlignmentOptions.Center;
        team.text      = TeamNames;
        team.color     = new Color(0.85f, 0.92f, 1f, 1f);

        // Return button (top-right)
        var retGO = new GameObject("ReturnButton", typeof(RectTransform));
        retGO.transform.SetParent(panelRt, false);
        var retRt = retGO.GetComponent<RectTransform>();
        retRt.anchorMin        = new Vector2(1f, 1f);
        retRt.anchorMax        = new Vector2(1f, 1f);
        retRt.pivot            = new Vector2(1f, 1f);
        retRt.anchoredPosition = new Vector2(-16f, -14f);
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

        // Scrollable disclaimer area
        var scrollGO = new GameObject("Scroll", typeof(RectTransform));
        var scrollRt = scrollGO.GetComponent<RectTransform>();
        scrollRt.SetParent(panelRt, false);
        scrollRt.anchorMin = Vector2.zero;
        scrollRt.anchorMax = Vector2.one;
        scrollRt.offsetMin = new Vector2(24f, 24f);
        scrollRt.offsetMax = new Vector2(-24f, -120f);

        var scroll = scrollGO.AddComponent<ScrollRect>();
        scroll.horizontal        = false;
        scroll.vertical          = true;
        scroll.movementType      = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40f;

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

        var contentGO = new GameObject("Content", typeof(RectTransform));
        var contentRt = contentGO.GetComponent<RectTransform>();
        contentRt.SetParent(vpRt, false);
        contentRt.anchorMin        = new Vector2(0f, 1f);
        contentRt.anchorMax        = new Vector2(1f, 1f);
        contentRt.pivot            = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta        = Vector2.zero;

        var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
        vlg.padding        = new RectOffset(20, 20, 18, 18);
        vlg.spacing        = 14f;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = true;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        var fitter = contentGO.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

        var assetGO = new GameObject("AssetCredits", typeof(RectTransform));
        assetGO.transform.SetParent(contentRt, false);
        var assets = assetGO.AddComponent<TextMeshProUGUI>();
        assets.text               = AssetCredits;
        assets.fontSize           = 18;
        assets.alignment          = TextAlignmentOptions.TopLeft;
        assets.color              = new Color(0.92f, 0.94f, 1f, 1f);
        assets.enableWordWrapping = true;
        assets.richText           = true;
        assets.margin             = new Vector4(4f, 4f, 4f, 4f);

        var dividerGO = new GameObject("Divider", typeof(RectTransform));
        dividerGO.transform.SetParent(contentRt, false);
        var dividerImg = dividerGO.AddComponent<Image>();
        dividerImg.color = new Color(1f, 1f, 1f, 0.15f);
        var dividerLE = dividerGO.AddComponent<LayoutElement>();
        dividerLE.minHeight       = 2f;
        dividerLE.preferredHeight = 2f;

        var bodyGO = new GameObject("Disclaimer", typeof(RectTransform));
        bodyGO.transform.SetParent(contentRt, false);
        var body = bodyGO.AddComponent<TextMeshProUGUI>();
        body.text               = DisclaimerBody;
        body.fontSize           = 18;
        body.alignment          = TextAlignmentOptions.TopLeft;
        body.color              = new Color(0.92f, 0.94f, 1f, 1f);
        body.enableWordWrapping = true;
        body.richText           = true;
        body.margin             = new Vector4(4f, 4f, 4f, 4f);

        scroll.content  = contentRt;
        scroll.viewport = vpRt;

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
        sb.handleRect    = hndRt;
        sb.targetGraphic = hndImg;

        scroll.verticalScrollbar           = sb;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
    }

    /// <summary>
    /// Resolve the best parent for the overlay. Prefers Main_Canvas if it exists,
    /// otherwise creates a new full-screen overlay Canvas so this overlay still works
    /// when invoked from a scene that lacks the standard menu canvas.
    /// </summary>
    private Transform ResolveCanvasParent()
    {
        var canvasGO = GameObject.Find("Main_Canvas");
        if (canvasGO != null) return canvasGO.transform;

        var anyCanvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (anyCanvas != null) return anyCanvas.transform;

        var newCanvasGO = new GameObject("CreditsOverlayCanvas");
        var canvas = newCanvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;
        newCanvasGO.AddComponent<CanvasScaler>();
        newCanvasGO.AddComponent<GraphicRaycaster>();
        return newCanvasGO.transform;
    }
}
