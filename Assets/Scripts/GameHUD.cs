using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Top HUD bar: displays Power, Budget, Time resources and the current floor.
/// Subscribes to ResourceManager.OnResourcesChanged for live updates.
/// If TMP references are not wired in the Inspector, labels are resolved by GameObject name
/// under this transform (e.g. PowerText, BudgetText, TimeText, or *Value variants).
/// </summary>
public class GameHUD : MonoBehaviour
{
    [Header("Resource Labels (TMP — auto-found by name if left empty)")]
    [SerializeField] private TMP_Text powerText;
    [SerializeField] private TMP_Text budgetText;
    [SerializeField] private TMP_Text timeText;

    [Header("Resource Labels (legacy UI Text — optional)")]
    [SerializeField] private Text powerTextLegacy;
    [SerializeField] private Text budgetTextLegacy;
    [SerializeField] private Text timeTextLegacy;

    [Header("Display format (matches stylized headers in GameCanvas)")]
    [SerializeField] private string powerFormat = "POWER  {0}";
    [SerializeField] private string budgetFormat = "BUDGET  {0}";
    [SerializeField] private string timeFormat = "TIME  {0}";

    [Header("Floor Indicator")]
    [SerializeField] private TMP_Text floorText;
    [SerializeField] private Text floorTextLegacy;

    [Header("Resource Icons (optional)")]
    [SerializeField] private Image powerIcon;
    [SerializeField] private Image budgetIcon;
    [SerializeField] private Image timeIcon;

    [Header("Warning Colours")]
    [SerializeField] private Color normalColour = Color.white;
    [SerializeField] private Color warningColour = new Color(1f, 0.6f, 0.1f, 1f);
    [SerializeField] private Color criticalColour = new Color(0.9f, 0.15f, 0.15f, 1f);

    [Header("Warning Thresholds")]
    [SerializeField] private int powerWarning = 1;
    [SerializeField] private int budgetWarning = 2;
    [SerializeField] private int timeWarning = 4;
    [SerializeField] private int timeCritical = 2;

    private bool _subscribed;

    private void Awake()
    {
        BindResourceLabelsIfNeeded();
        BindFloorLabelIfNeeded();
    }

    private void OnEnable()
    {
        SubscribeResources();
        PushCurrentResources();
    }

    private void OnDisable()
    {
        UnsubscribeResources();
    }

    private void Start()
    {
        SetFloor(1, 4);
    }

    private void SubscribeResources()
    {
        if (_subscribed) return;
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.OnResourcesChanged += OnResourcesChanged;
            _subscribed = true;
        }
    }

    /// <summary>ResourceManager may be created after this HUD enables (e.g. PsycheBootstrap).</summary>
    private void EnsureSubscribedToResources()
    {
        if (!_subscribed)
            SubscribeResources();
    }

    private void UnsubscribeResources()
    {
        if (!_subscribed || ResourceManager.Instance == null) return;
        ResourceManager.Instance.OnResourcesChanged -= OnResourcesChanged;
        _subscribed = false;
    }

    private void PushCurrentResources()
    {
        EnsureSubscribedToResources();
        if (ResourceManager.Instance != null)
        {
            UpdateResources(
                ResourceManager.Instance.Power,
                ResourceManager.Instance.Budget,
                ResourceManager.Instance.TimeRemaining);
        }
        else
            UpdateResources(3, 6, 15);
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
        EnsureSubscribedToResources();

        Color pCol = power <= powerWarning ? warningColour : normalColour;
        Color bCol = budget <= budgetWarning ? warningColour : normalColour;
        Color tCol = time <= timeCritical ? criticalColour
            : time <= timeWarning ? warningColour
            : normalColour;

        SetPowerLine(string.Format(powerFormat, power), pCol);
        SetBudgetLine(string.Format(budgetFormat, budget), bCol);
        SetTimeLine(string.Format(timeFormat, time), tCol);
    }

    private void SetPowerLine(string text, Color color)
    {
        if (powerText != null)
        {
            powerText.text = text;
            powerText.color = color;
        }
        else if (powerTextLegacy != null)
        {
            powerTextLegacy.text = text;
            powerTextLegacy.color = color;
        }
    }

    private void SetBudgetLine(string text, Color color)
    {
        if (budgetText != null)
        {
            budgetText.text = text;
            budgetText.color = color;
        }
        else if (budgetTextLegacy != null)
        {
            budgetTextLegacy.text = text;
            budgetTextLegacy.color = color;
        }
    }

    private void SetTimeLine(string text, Color color)
    {
        if (timeText != null)
        {
            timeText.text = text;
            timeText.color = color;
        }
        else if (timeTextLegacy != null)
        {
            timeTextLegacy.text = text;
            timeTextLegacy.color = color;
        }
    }

    /// <summary>
    /// Updates the floor indicator (e.g. "Floor 2 / 4").
    /// </summary>
    public void SetFloor(int current, int total)
    {
        string line = $"Floor {current} / {total}";
        if (floorText != null)
            floorText.text = line;
        else if (floorTextLegacy != null)
            floorTextLegacy.text = line;
    }

    private void BindResourceLabelsIfNeeded()
    {
        if (!HasPowerBinding())
            TryBindLine("PowerText", "PowerValue", "PWR_Value", ref powerText, ref powerTextLegacy);
        if (!HasBudgetBinding())
            TryBindLine("BudgetText", "BudgetValue", "BDG_Value", ref budgetText, ref budgetTextLegacy);
        if (!HasTimeBinding())
            TryBindLine("TimeText", "TimeValue", "TIME_Value", ref timeText, ref timeTextLegacy);
    }

    private bool HasPowerBinding() => powerText != null || powerTextLegacy != null;
    private bool HasBudgetBinding() => budgetText != null || budgetTextLegacy != null;
    private bool HasTimeBinding() => timeText != null || timeTextLegacy != null;

    private void TryBindLine(string primary, string alt1, string alt2, ref TMP_Text tmp, ref Text legacy)
    {
        Transform t = FindChildRecursive(transform, primary)
                      ?? FindChildRecursive(transform, alt1)
                      ?? FindChildRecursive(transform, alt2);
        if (t == null)
            return;

        if (tmp == null)
            tmp = t.GetComponent<TMP_Text>();
        if (tmp == null && legacy == null)
            legacy = t.GetComponent<Text>();
    }

    private void BindFloorLabelIfNeeded()
    {
        if (floorText != null || floorTextLegacy != null)
            return;

        Transform t = FindChildRecursive(transform, "FloorText");
        if (t == null)
            return;

        floorText = t.GetComponent<TMP_Text>();
        if (floorText == null)
            floorTextLegacy = t.GetComponent<Text>();
    }

    private static Transform FindChildRecursive(Transform root, string name)
    {
        if (root.name == name)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            var found = FindChildRecursive(root.GetChild(i), name);
            if (found != null)
                return found;
        }

        return null;
    }
}
