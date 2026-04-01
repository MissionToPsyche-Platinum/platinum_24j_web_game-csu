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
            else if (kind == EncounterPresentation.Kind.ResourceManagement && !string.IsNullOrEmpty(obj))
                objectiveText.text = obj;
            else
                objectiveText.text = string.IsNullOrEmpty(obj) ? "—" : obj;
        }
        if (turnLimitText != null)
            turnLimitText.text = EncounterPresentation.FormatTurnLimitLine(kind, turnLimit);

        UpdateProgress(current, max);
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
