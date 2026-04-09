using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Encounter objective panel (centered under Top HUD).
/// Content is driven by <see cref="GameUIController"/> / <see cref="EncounterManager"/> events.
/// </summary>
public class EncounterPanel : MonoBehaviour
{
    [Header("Labels")]
    [SerializeField] private TMP_Text encounterTypeText;   // e.g. "DATA COLLECTION"
    [SerializeField] private TMP_Text objectiveText;       // e.g. "Collect 5 Surface data"
    [SerializeField] private TMP_Text progressText;        // e.g. "2 / 5"
    [SerializeField] private TMP_Text turnLimitText;       // e.g. "Turns left: 6"
    [SerializeField] private TMP_Text bossProgressText;    // Second line for boss dual objectives

    [Header("Progress Bar")]
    [SerializeField] private Image progressFill;           // Image Type = Filled, Horizontal (optional sprite: UI/Default)
    [Tooltip("Optional display-only slider; if set, value tracks current/max like the fill bar.")]
    [SerializeField] private Slider progressSlider;

    [Header("Turn Counter")]
    [SerializeField] private TMP_Text turnCounterText;     // Current turn number

    private int _maxProgress = 1;

    private void Awake()
    {
        AutoBindUI();
    }

    private void AutoBindUI()
    {
        if (encounterTypeText == null) encounterTypeText = transform.Find("EncounterTypeText")?.GetComponent<TMP_Text>();
        if (objectiveText == null) objectiveText = transform.Find("ObjectiveText")?.GetComponent<TMP_Text>();
        if (progressText == null) progressText = transform.Find("ProgressText")?.GetComponent<TMP_Text>();
        if (turnLimitText == null) turnLimitText = transform.Find("TurnLimitText")?.GetComponent<TMP_Text>();
        if (bossProgressText == null) bossProgressText = transform.Find("BossProgressText")?.GetComponent<TMP_Text>();
        
        // Find the progress fill image. It might be under ProgressBarBg/ProgressFill
        if (progressFill == null) 
        {
            Transform bg = transform.Find("ProgressBarBg");
            if (bg != null) progressFill = bg.Find("ProgressFill")?.GetComponent<Image>() ?? bg.GetComponent<Image>();
        }

        if (progressSlider == null)
            progressSlider = GetComponentInChildren<Slider>(true);

        if (progressSlider != null)
        {
            progressSlider.interactable = false;
            progressSlider.transition = Selectable.Transition.None;
        }
    }

    /// <summary>
    /// Populates the panel with encounter information.
    /// </summary>
    /// <param name="type">Encounter category label (e.g. "CRISIS RESPONSE")</param>
    /// <param name="objective">Human-readable objective string</param>
    /// <param name="current">Current progress value</param>
    /// <param name="max">Target progress value</param>
    /// <param name="turnLimit">Maximum turns allowed for this encounter</param>
    public void SetEncounter(string type, string objective, int current, int max, int turnLimit)
    {
        _maxProgress = Mathf.Max(1, max);

        var kind = EncounterPresentation.Classify(type);
        if (encounterTypeText != null)
            encounterTypeText.text = type.ToUpperInvariant();
        if (objectiveText != null)
        {
            string obj = objective ?? "";
            if (kind == EncounterPresentation.Kind.DataCollection && !string.IsNullOrEmpty(obj))
                objectiveText.text = obj.ToUpperInvariant();
            else if (kind == EncounterPresentation.Kind.SystemsStressTest && !string.IsNullOrEmpty(obj))
                objectiveText.text = obj;
            else
                objectiveText.text = string.IsNullOrEmpty(obj) ? "—" : obj;
        }
        if (turnLimitText != null)
            turnLimitText.text = EncounterPresentation.FormatTurnLimitLine(kind, turnLimit);

        UpdateProgress(current, max);

        // Hide boss dual-progress by default; SetBossProgress will override when needed
        ClearBossProgress();
    }

    /// <summary>
    /// Shows two boss progress lines (e.g. Maneuvers + Budget) and hides the generic progress bar.
    /// </summary>
    public void SetBossProgress(string label1, int cur1, int max1, string label2, int cur2, int max2)
    {
        // Use progressText for line 1
        if (progressText != null)
            progressText.text = $"{label1}: {cur1} / {max1}";

        // Use bossProgressText for line 2 (create dynamically if missing)
        if (bossProgressText == null && progressText != null)
        {
            var go = UnityEngine.Object.Instantiate(progressText.gameObject, progressText.transform.parent);
            go.name = "BossProgressText";
            bossProgressText = go.GetComponent<TMP_Text>();
            // Position it below progressText
            var rt = go.GetComponent<RectTransform>();
            var srcRt = progressText.GetComponent<RectTransform>();
            rt.anchoredPosition = srcRt.anchoredPosition + new UnityEngine.Vector2(0, -srcRt.sizeDelta.y - 4f);
        }

        if (bossProgressText != null)
        {
            bossProgressText.text = $"{label2}: {cur2} / {max2}";
            bossProgressText.gameObject.SetActive(true);
        }

        // Hide the fill bar / slider for boss encounters (two text lines replace it)
        if (progressFill != null) progressFill.fillAmount = 0f;
        if (progressSlider != null) progressSlider.gameObject.SetActive(false);
    }

    /// <summary>Hides the boss second line and re-enables generic progress.</summary>
    public void ClearBossProgress()
    {
        if (bossProgressText != null)
        {
            bossProgressText.color = Color.white; // reset possible color tinting
            bossProgressText.gameObject.SetActive(false);
        }
        if (progressSlider != null)
            progressSlider.gameObject.SetActive(true);
    }

    /// <summary>
    /// Shows stress test progress (Resolved Crises vs Active Crises).
    /// </summary>
    public void SetStressTestProgress(int resolvedCur, int resolvedMax, int activeCur, int activeMax)
    {
        if (progressText != null)
            progressText.text = $"CRISES RESOLVED: {resolvedCur} / {resolvedMax}";

        if (bossProgressText == null && progressText != null)
        {
            var go = UnityEngine.Object.Instantiate(progressText.gameObject, progressText.transform.parent);
            go.name = "BossProgressText";
            bossProgressText = go.GetComponent<TMP_Text>();
            var rt = go.GetComponent<RectTransform>();
            var srcRt = progressText.GetComponent<RectTransform>();
            rt.anchoredPosition = srcRt.anchoredPosition + new UnityEngine.Vector2(0, -srcRt.sizeDelta.y - 4f);
        }

        if (bossProgressText != null)
        {
            bossProgressText.text = $"ACTIVE CRISES: {activeCur} / {activeMax}";
            // Turn text red if 2 or more active crises
            bossProgressText.color = (activeCur >= 2) ? Color.red : Color.white;
            bossProgressText.gameObject.SetActive(true);
        }

        if (progressFill != null) progressFill.fillAmount = 0f;
        if (progressSlider != null) progressSlider.gameObject.SetActive(false);
    }

    /// <summary>
    /// Updates only the progress bar and counter (call each time the player collects data etc.)
    /// </summary>
    public void UpdateProgress(int current, int max)
    {
        _maxProgress = Mathf.Max(1, max);
        float fill = Mathf.Clamp01((float)current / _maxProgress);

        if (progressFill != null)
            progressFill.fillAmount = fill;

        if (progressSlider != null)
        {
            progressSlider.minValue = 0f;
            progressSlider.maxValue = 1f;
            progressSlider.wholeNumbers = false;
            progressSlider.value = fill;
        }

        if (progressText != null) progressText.text = $"{current} / {max}";
    }

    /// <summary>
    /// Updates the current turn display.
    /// </summary>
    public void SetTurn(int turnNumber)
    {
        if (turnCounterText != null)
            turnCounterText.text = $"Turn {turnNumber}";
    }
}
