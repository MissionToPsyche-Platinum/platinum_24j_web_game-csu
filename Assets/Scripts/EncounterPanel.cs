using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Encounter objective panel (top-right).
/// Shows the encounter type, objective description, and a fill-bar progress indicator.
/// </summary>
public class EncounterPanel : MonoBehaviour
{
    [Header("Labels")]
    [SerializeField] private TMP_Text encounterTypeText;   // e.g. "DATA COLLECTION"
    [SerializeField] private TMP_Text objectiveText;       // e.g. "Collect 5 Surface data"
    [SerializeField] private TMP_Text progressText;        // e.g. "2 / 5"
    [SerializeField] private TMP_Text turnLimitText;       // e.g. "Turns left: 6"

    [Header("Progress Bar")]
    [SerializeField] private Image progressFill;           // Image with FillMethod = Horizontal

    [Header("Turn Counter")]
    [SerializeField] private TMP_Text turnCounterText;     // Current turn number

    private int _maxProgress = 1;

    private void Start()
    {
        // Automatically find UI elements if they aren't assigned in the inspector
        AutoBindUI();

        // Subscribe to EncounterManager events so the UI automatically updates!
        if (EncounterManager.Instance != null)
        {
            EncounterManager.Instance.OnEncounterStarted += HandleEncounterStarted;
            EncounterManager.Instance.OnProgressChanged += UpdateProgress;
            EncounterManager.Instance.OnTurnAdvanced += SetTurn;

            // Initialize with current state
            HandleEncounterStarted(EncounterManager.Instance.EncounterType, 
                                   EncounterManager.Instance.ObjectiveDesc, 
                                   EncounterManager.Instance.CurrentProgress, 
                                   EncounterManager.Instance.TargetProgress);
            SetTurn(EncounterManager.Instance.CurrentTurn);
        }
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
    }

    private void OnDestroy()
    {
        if (EncounterManager.Instance != null)
        {
            EncounterManager.Instance.OnEncounterStarted -= HandleEncounterStarted;
            EncounterManager.Instance.OnProgressChanged -= UpdateProgress;
            EncounterManager.Instance.OnTurnAdvanced -= SetTurn;
        }
    }

    private void HandleEncounterStarted(string type, string objective, int current, int max)
    {
        int turnLimit = 8; // Default, you can retrieve actual turn limit if needed
        if (EncounterManager.Instance != null) turnLimit = EncounterManager.Instance.MaxTurns;
        
        SetEncounter(type, objective, current, max, turnLimit);
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

        if (encounterTypeText != null) encounterTypeText.text = type.ToUpper();
        if (objectiveText     != null) objectiveText.text     = objective;
        if (turnLimitText     != null) turnLimitText.text     = $"Turn limit: {turnLimit}";

        UpdateProgress(current, max);
    }

    /// <summary>
    /// Updates only the progress bar and counter (call each time the player collects data etc.)
    /// </summary>
    public void UpdateProgress(int current, int max)
    {
        _maxProgress = Mathf.Max(1, max);
        float fill = Mathf.Clamp01((float)current / _maxProgress);

        if (progressFill != null) progressFill.fillAmount = fill;
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
