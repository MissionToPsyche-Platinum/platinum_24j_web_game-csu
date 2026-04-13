using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Full-screen mission outcome overlay with distinct success/failure states:
/// separate background images, separate center art, outcome title, and bottom Return button.
/// Add the MissionEndScreens object to the scene via Tools → UI → Create Mission End Screens In Hierarchy, then edit layout and sprites in the Inspector.
/// </summary>
public class MissionEndScreenUI : MonoBehaviour
{
    private static MissionEndScreenUI _instance;
    private bool _isShowing;
    private readonly List<GameObject> _hiddenTargets = new List<GameObject>();
    private readonly List<bool> _hiddenWasActive = new List<bool>();

    private static readonly Color32 DefaultButtonColor = new Color32(0, 94, 184, 255); // #005EB8

    [Header("Hierarchy (assign or use Tools → UI → create)")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image successBackgroundImage;
    [SerializeField] private Image failureBackgroundImage;
    [SerializeField] private Image successArtImage;
    [SerializeField] private Image failureArtImage;
    [SerializeField] private TextMeshProUGUI successTitleLabel;
    [SerializeField] private TextMeshProUGUI failureTitleLabel;
    [SerializeField] private TextMeshProUGUI outcomeTitleLabel;
    [SerializeField] private string successTitleText = "Mission Success!";
    [SerializeField] private string failureTitleText = "Mission Failure!";
    [SerializeField] private Button returnButton;
    [SerializeField] private TextMeshProUGUI returnLabel;

    [Tooltip("Tint for the Return button Image.")]
    [SerializeField] private Color returnButtonColor = DefaultButtonColor;

    [Header("Fallback (no MainMenuUI in scene)")]
    [Tooltip("If no MainMenuUI exists, load this scene instead (e.g. Main). Leave empty to only log a warning.")]
    [SerializeField] private string fallbackSceneIfNoMainMenuUI = "Main";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _instance = null;
    }

    public static void ShowSuccess()
    {
        ResolveInstance()?.Present(true);
    }

    public static void ShowFailure()
    {
        ResolveInstance()?.Present(false);
    }

    private static MissionEndScreenUI ResolveInstance()
    {
        if (_instance != null)
            return _instance;

        _instance = Object.FindFirstObjectByType<MissionEndScreenUI>(FindObjectsInactive.Include);
        if (_instance == null)
        {
            Debug.LogError("[MissionEndScreenUI] No MissionEndScreenUI in the scene. Use Tools → UI → Create Mission End Screens In Hierarchy.");
            return null;
        }

        return _instance;
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning("[MissionEndScreenUI] Duplicate MissionEndScreenUI — destroying duplicate.", this);
            Destroy(gameObject);
            return;
        }

        _instance = this;
        WireReturnIfNeeded();
        ApplyReturnButtonColor();
        EnsureOutcomeTitleLabel();
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
        if (Time.timeScale == 0f)
            Time.timeScale = 1f;
    }

    private void WireReturnIfNeeded()
    {
        if (returnButton == null)
            return;
        returnButton.onClick.RemoveListener(OnReturnClicked);
        returnButton.onClick.AddListener(OnReturnClicked);
    }

    private void ApplyReturnButtonColor()
    {
        if (returnButton == null)
            return;
        var img = returnButton.targetGraphic as Image;
        if (img != null)
            img.color = returnButtonColor;
    }

    private void EnsureOutcomeTitleLabel()
    {
        if (outcomeTitleLabel != null)
            return;

        var existing = transform.Find("OutcomeTitle");
        if (existing != null)
            outcomeTitleLabel = existing.GetComponent<TextMeshProUGUI>();
        if (outcomeTitleLabel != null)
            return;

        var go = new GameObject("OutcomeTitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -48f);
        rt.sizeDelta = new Vector2(1200f, 96f);

        outcomeTitleLabel = go.GetComponent<TextMeshProUGUI>();
        outcomeTitleLabel.alignment = TextAlignmentOptions.Center;
        outcomeTitleLabel.fontSize = 72f;
        outcomeTitleLabel.color = Color.white;
        if (returnLabel != null && returnLabel.font != null)
            outcomeTitleLabel.font = returnLabel.font;
    }

    /// <summary>Shows win or lose art and freezes time. Safe to call when the root starts inactive.</summary>
    public void Present(bool success)
    {
        if (_isShowing)
            return;

        HideOtherViews();

        // Backward compatibility: if outcome-specific backgrounds are not assigned,
        // keep using the legacy single background image.
        if (backgroundImage != null)
            backgroundImage.gameObject.SetActive(successBackgroundImage == null && failureBackgroundImage == null);
        if (successBackgroundImage != null)
            successBackgroundImage.gameObject.SetActive(success);
        if (failureBackgroundImage != null)
            failureBackgroundImage.gameObject.SetActive(!success);

        if (successArtImage != null)
            successArtImage.gameObject.SetActive(success);
        if (failureArtImage != null)
            failureArtImage.gameObject.SetActive(!success);

        if (successTitleLabel != null)
            successTitleLabel.gameObject.SetActive(success);
        if (failureTitleLabel != null)
            failureTitleLabel.gameObject.SetActive(!success);
        if (outcomeTitleLabel != null)
            outcomeTitleLabel.text = success ? successTitleText : failureTitleText;

        if (returnButton != null)
            returnButton.gameObject.SetActive(true);

        _isShowing = true;
        gameObject.SetActive(true);
        Time.timeScale = 0f;
    }

    private void OnReturnClicked()
    {
        Time.timeScale = 1f;
        _isShowing = false;
        RestoreHiddenViews();
        gameObject.SetActive(false);

        var mainMenu = Object.FindFirstObjectByType<MainMenuUI>(FindObjectsInactive.Include);
        if (mainMenu != null)
        {
            mainMenu.ReturnToMainMenuView();
            return;
        }

        if (!string.IsNullOrEmpty(fallbackSceneIfNoMainMenuUI))
            SceneManager.LoadScene(fallbackSceneIfNoMainMenuUI);
        else
            Debug.LogWarning("[MissionEndScreenUI] No MainMenuUI in scene and no fallback scene — nothing to return to.");
    }

    private void OnDisable()
    {
        if (_isShowing)
        {
            _isShowing = false;
            RestoreHiddenViews();
            if (Time.timeScale == 0f)
                Time.timeScale = 1f;
        }
    }

    private void HideOtherViews()
    {
        _hiddenTargets.Clear();
        _hiddenWasActive.Clear();

        // Hide siblings under the same parent (e.g. other Main_Canvas views).
        var parent = transform.parent;
        if (parent != null)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var sibling = parent.GetChild(i).gameObject;
                if (sibling == gameObject)
                    continue;
                TrackAndDisable(sibling);
            }
        }

        // Hide other roots under the parent group (e.g. --- VIEWS --- siblings like OptionsCanvas).
        var grandParent = parent != null ? parent.parent : null;
        if (grandParent != null)
        {
            for (int i = 0; i < grandParent.childCount; i++)
            {
                var root = grandParent.GetChild(i);
                if (IsAncestorOrSelf(root, transform))
                    continue;
                TrackAndDisable(root.gameObject);
            }
        }
    }

    private void TrackAndDisable(GameObject go)
    {
        if (go == null || _hiddenTargets.Contains(go))
            return;
        _hiddenTargets.Add(go);
        _hiddenWasActive.Add(go.activeSelf);
        go.SetActive(false);
    }

    private void RestoreHiddenViews()
    {
        for (int i = 0; i < _hiddenTargets.Count; i++)
        {
            var go = _hiddenTargets[i];
            if (go == null)
                continue;
            bool wasActive = i < _hiddenWasActive.Count && _hiddenWasActive[i];
            go.SetActive(wasActive);
        }
        _hiddenTargets.Clear();
        _hiddenWasActive.Clear();
    }

    private static bool IsAncestorOrSelf(Transform possibleAncestor, Transform t)
    {
        for (var cur = t; cur != null; cur = cur.parent)
        {
            if (cur == possibleAncestor)
                return true;
        }
        return false;
    }

}
