using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays Power, Budget, and Time from ResourceManager.
/// Assign either legacy UI Text or TextMeshPro (TMP) labels in the Inspector.
/// If ResourceManager is not in the scene (e.g. main menu), this will do nothing.
/// </summary>
public class ResourceHUD : MonoBehaviour
{
    [Header("Resource labels - Legacy UI Text")]
    [Tooltip("Shows current Power. Leave empty if using TMP below.")]
    public Text powerText;

    public Text budgetText;
    public Text timeText;

    [Header("Resource labels - TextMeshPro (optional)")]
    [Tooltip("Use these if your HUD uses TextMeshPro - Text (UI).")]
    public TMP_Text powerTMP;

    public TMP_Text budgetTMP;
    public TMP_Text timeTMP;

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

        string powerStr = string.Format(powerFormat, p);
        string budgetStr = string.Format(budgetFormat, b);
        string timeStr = string.Format(timeFormat, t);

        if (powerText != null) powerText.text = powerStr;
        if (budgetText != null) budgetText.text = budgetStr;
        if (timeText != null) timeText.text = timeStr;

        if (powerTMP != null) powerTMP.text = powerStr;
        if (budgetTMP != null) budgetTMP.text = budgetStr;
        if (timeTMP != null) timeTMP.text = timeStr;
    }
}
