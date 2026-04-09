using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Full-screen mission outcome overlay: black background, center art (win/lose), bottom Return button.
/// Add the MissionEndScreens object to the scene via Tools → UI → Create Mission End Screens In Hierarchy, then edit layout and sprites in the Inspector.
/// </summary>
public class MissionEndScreenUI : MonoBehaviour
{
    private static MissionEndScreenUI _instance;
    private bool _isShowing;

    private static readonly Color32 DefaultButtonColor = new Color32(0, 94, 184, 255); // #005EB8

    [Header("Hierarchy (assign or use Tools → UI → create)")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image successArtImage;
    [SerializeField] private Image failureArtImage;
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

    /// <summary>Shows win or lose art and freezes time. Safe to call when the root starts inactive.</summary>
    public void Present(bool success)
    {
        if (_isShowing)
            return;

        if (successArtImage != null)
            successArtImage.gameObject.SetActive(success);
        if (failureArtImage != null)
            failureArtImage.gameObject.SetActive(!success);

        _isShowing = true;
        gameObject.SetActive(true);
        Time.timeScale = 0f;
    }

    private void OnReturnClicked()
    {
        Time.timeScale = 1f;
        _isShowing = false;
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
            if (Time.timeScale == 0f)
                Time.timeScale = 1f;
        }
    }

}
