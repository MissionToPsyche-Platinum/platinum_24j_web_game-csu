using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays Power, Budget, and Time from ResourceManager.
/// Add this to a Canvas (e.g. under Gameplay_Layer). Assign the three Text components in the Inspector.
/// If ResourceManager is not in the scene (e.g. main menu), this will do nothing.
/// </summary>
public class ResourceHUD : MonoBehaviour
{
    [Header("Resource labels (optional - assign Text components)")]
    [Tooltip("Shows current Power. Leave empty to hide.")]
    public Text powerText;

    [Tooltip("Shows current Budget. Leave empty to hide.")]
    public Text budgetText;

    [Tooltip("Shows current Time. Leave empty to hide.")]
    public Text timeText;

    [Header("Label format")]
    [Tooltip("Format: {0} = value. E.g. 'Power: {0}'")]
    public string powerFormat = "Power: {0}";

    public string budgetFormat = "Budget: {0}";
    public string timeFormat = "Time: {0}";

    private void OnEnable()
    {
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.OnResourcesChanged += UpdateDisplay;
        UpdateDisplay(0, 0, 0);
    }

    private void OnDisable()
    {
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.OnResourcesChanged -= UpdateDisplay;
    }

    private void UpdateDisplay(int power, int budget, int time)
    {
        if (ResourceManager.Instance == null)
            return;

        int p = ResourceManager.Instance.Power;
        int b = ResourceManager.Instance.Budget;
        int t = ResourceManager.Instance.TimeRemaining;

        if (powerText != null) powerText.text = string.Format(powerFormat, p);
        if (budgetText != null) budgetText.text = string.Format(budgetFormat, b);
        if (timeText != null) timeText.text = string.Format(timeFormat, t);
    }
}
