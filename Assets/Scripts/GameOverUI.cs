using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Handles displaying the Mission Complete / Mission Failed overlays.
/// Capable of auto-generating a fallback UI if none is rigged in the scene.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("UI References (Optional - will auto-generate if empty)")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Button restartButton;

    private void Awake()
    {
        // Ensure that if the GameOverUI is already in the scene,
        // it starts completely hidden and does not block raycasts across the game.
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    public void ShowVictory()
    {
        EnsureUI();
        resultText.text = "MISSION COMPLETE!";
        resultText.color = new Color(0.2f, 0.8f, 0.2f, 1f); // Green
        ShowPanel();
    }

    public void ShowDefeat()
    {
        EnsureUI();
        resultText.text = "MISSION FAILED";
        resultText.color = new Color(0.9f, 0.2f, 0.2f, 1f); // Red
        ShowPanel();
    }

    private void EnsureUI()
    {
        if (canvasGroup != null && resultText != null && restartButton != null)
            return;

        // Auto-generate a fallback programmatic UI if not assigned
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            var canvasGo = new GameObject("GameOverCanvas");
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<GraphicRaycaster>();
            transform.SetParent(canvasGo.transform, false);
        }

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        var rt = gameObject.GetComponent<RectTransform>();
        if (rt == null) rt = gameObject.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Dim background
        var bg = gameObject.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.85f);

        // Result Text
        var textGo = new GameObject("ResultText");
        textGo.transform.SetParent(transform, false);
        var textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0.5f, 0.7f);
        textRt.anchorMax = new Vector2(0.5f, 0.7f);
        textRt.anchoredPosition = Vector2.zero;
        textRt.sizeDelta = new Vector2(800, 200);

        resultText = textGo.AddComponent<TextMeshProUGUI>();
        resultText.alignment = TextAlignmentOptions.Center;
        resultText.fontSize = 72;
        resultText.fontStyle = FontStyles.Bold;

        // Restart Button
        var btnGo = new GameObject("RestartButton");
        btnGo.transform.SetParent(transform, false);
        var btnRt = btnGo.AddComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.5f, 0.3f);
        btnRt.anchorMax = new Vector2(0.5f, 0.3f);
        btnRt.anchoredPosition = Vector2.zero;
        btnRt.sizeDelta = new Vector2(300, 80);

        var btnImg = btnGo.AddComponent<Image>();
        btnImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        restartButton = btnGo.AddComponent<Button>();
        restartButton.targetGraphic = btnImg;
        restartButton.onClick.AddListener(RestartGame);

        var btnTextGo = new GameObject("Text");
        btnTextGo.transform.SetParent(btnGo.transform, false);
        var btnTextRt = btnTextGo.AddComponent<RectTransform>();
        btnTextRt.anchorMin = Vector2.zero;
        btnTextRt.anchorMax = Vector2.one;
        btnTextRt.offsetMin = Vector2.zero;
        btnTextRt.offsetMax = Vector2.zero;

        var btnText = btnTextGo.AddComponent<TextMeshProUGUI>();
        btnText.text = "RETURN";
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.fontSize = 32;
        btnText.color = Color.white;
    }

    private void ShowPanel()
    {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        var uiCtrl = FindAnyObjectByType<GameUIController>();
        if (uiCtrl != null) {
            uiCtrl.SetEndTurnInteractable(false);
        }
    }

    private void RestartGame()
    {
        HidePanel();
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetRun();
            GameManager.Instance.TransitionToNextFloor();
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    private void HidePanel()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }
}
