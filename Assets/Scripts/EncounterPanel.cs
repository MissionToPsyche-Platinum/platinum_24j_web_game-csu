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
        // Default placeholder encounter shown at startup
        SetEncounter("DATA COLLECTION", "Collect 5 Surface data", 0, 5, 8);
        SetTurn(1);
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
