using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Top HUD bar: displays Power, Budget, Time resources and the current floor.
/// Subscribes to ResourceManager.OnResourcesChanged for live updates.
/// </summary>
public class GameHUD : MonoBehaviour
{
    [Header("Resource Labels")]
    [SerializeField] private TMP_Text powerText;
    [SerializeField] private TMP_Text budgetText;
    [SerializeField] private TMP_Text timeText;

    [Header("Floor Indicator")]
    [SerializeField] private TMP_Text floorText;

    [Header("Resource Icons (optional)")]
    [SerializeField] private Image powerIcon;
    [SerializeField] private Image budgetIcon;
    [SerializeField] private Image timeIcon;

    // Colour coding for low-resource warnings
    [Header("Warning Colours")]
    [SerializeField] private Color normalColour  = Color.white;
    [SerializeField] private Color warningColour = new Color(1f, 0.6f, 0.1f, 1f);  // orange
    [SerializeField] private Color criticalColour = new Color(0.9f, 0.15f, 0.15f, 1f); // red

    [Header("Warning Thresholds")]
    [SerializeField] private int powerWarning   = 1;
    [SerializeField] private int budgetWarning  = 2;
    [SerializeField] private int timeWarning    = 4;
    [SerializeField] private int timeCritical   = 2;

    private void Start()
    {
        // Subscribe to ResourceManager for live updates
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.OnResourcesChanged += OnResourcesChanged;
            // Show current values
            UpdateResources(ResourceManager.Instance.Power,
                            ResourceManager.Instance.Budget,
                            ResourceManager.Instance.TimeRemaining);
        }
        else
        {
            // Fallback to design doc defaults if no ResourceManager yet
            UpdateResources(3, 6, 15);
        }
        SetFloor(1, 4);
    }

    private void OnDestroy()
    {
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.OnResourcesChanged -= OnResourcesChanged;
    }

    private void OnResourcesChanged(int power, int budget, int time)
    {
        UpdateResources(power, budget, time);
    }

    /// <summary>
    /// Refreshes all three resource labels with colour-coded warnings.
    /// </summary>
    public void UpdateResources(int power, int budget, int time)
    {
        if (powerText  != null) { powerText.text  = $"PWR  {power}";  powerText.color  = power  <= powerWarning  ? warningColour : normalColour; }
        if (budgetText != null) { budgetText.text = $"BDG  {budget}"; budgetText.color = budget <= budgetWarning ? warningColour : normalColour; }
        if (timeText   != null)
        {
            timeText.text  = $"TIME  {time}";
            timeText.color = time <= timeCritical ? criticalColour
                           : time <= timeWarning  ? warningColour
                           : normalColour;
        }
    }

    /// <summary>
    /// Updates the floor indicator (e.g. "Floor 2 / 4").
    /// </summary>
    public void SetFloor(int current, int total)
    {
        if (floorText != null)
            floorText.text = $"Floor {current} / {total}";
    }
}

