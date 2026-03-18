using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Singleton GameManager. Stores persistent currentBudget and currentTime,
/// provides TransitionToNextFloor() to load the next encounter and subtract Time,
/// and updates the HUD on Psyche_AutoCanvas whenever values change.
/// </summary>
public class GameManager : MonoBehaviour
{
    private static GameManager _instance;

    /// <summary>Singleton access. Ensure one GameManager exists in the scene (e.g. in --- SYSTEMS --- or at root).</summary>
    public static GameManager Instance => _instance;

    [Header("Persistent run values")]
    [Tooltip("Starting budget for the run. Persists across encounters.")]
    [SerializeField] private int _currentBudget = 6;

    [Tooltip("Starting time for the run. Counts down; 0 = game over. Persists across encounters.")]
    [SerializeField] private int _currentTime = 15;

    [Header("Transition to next floor")]
    [Tooltip("Time cost when transitioning to the next encounter/floor.")]
    [SerializeField] private int timeCostPerFloor = 1;

    [Tooltip("Scene name to load as the next encounter. Leave empty to reload the current scene.")]
    [SerializeField] private string nextEncounterSceneName = "";

    [Header("HUD reference")]
    [Tooltip("Optional: assign Psyche_AutoCanvas here. If empty, will find by name at runtime.")]
    [SerializeField] private Canvas psycheAutoCanvas;

    [Tooltip("Format for budget display. {0} = value.")]
    [SerializeField] private string budgetFormat = "Budget: {0}";

    [Tooltip("Format for time display. {0} = value.")]
    [SerializeField] private string timeFormat = "Time: {0}";

    /// <summary>Current budget. Persists across encounters.</summary>
    public int CurrentBudget => _currentBudget;

    /// <summary>Current time remaining. Persists across encounters; 0 = game over.</summary>
    public int CurrentTime => _currentTime;

    /// <summary>Fired when budget or time changes. (budget, time).</summary>
    public event Action<int, int> OnBudgetTimeChanged;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    private void Start()
    {
        UpdateHudOnPsycheCanvas();
        OnBudgetTimeChanged?.Invoke(_currentBudget, _currentTime);
    }

    /// <summary>Add or subtract budget. Updates HUD on Psyche_AutoCanvas.</summary>
    public void AddBudget(int amount)
    {
        _currentBudget += amount;
        UpdateHudOnPsycheCanvas();
        OnBudgetTimeChanged?.Invoke(_currentBudget, _currentTime);
    }

    /// <summary>Add or subtract time. Updates HUD on Psyche_AutoCanvas.</summary>
    public void AddTime(int amount)
    {
        _currentTime += amount;
        UpdateHudOnPsycheCanvas();
        OnBudgetTimeChanged?.Invoke(_currentBudget, _currentTime);
    }

    /// <summary>Subtract time for transition, then load the next encounter scene. If nextEncounterSceneName is empty, reloads current scene.</summary>
    public void TransitionToNextFloor()
    {
        _currentTime -= timeCostPerFloor;
        UpdateHudOnPsycheCanvas();
        OnBudgetTimeChanged?.Invoke(_currentBudget, _currentTime);

        if (string.IsNullOrEmpty(nextEncounterSceneName))
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        else
            SceneManager.LoadScene(nextEncounterSceneName);
    }

    /// <summary>Resets budget and time to starting values (e.g. for new run).</summary>
    public void ResetRun(int startBudget = 6, int startTime = 15)
    {
        _currentBudget = startBudget;
        _currentTime = startTime;
        UpdateHudOnPsycheCanvas();
        OnBudgetTimeChanged?.Invoke(_currentBudget, _currentTime);
    }

    /// <summary>Finds Psyche_AutoCanvas and updates Budget/Time HUD text whenever values change.</summary>
    private void UpdateHudOnPsycheCanvas()
    {
        Canvas canvas = psycheAutoCanvas != null ? psycheAutoCanvas : FindPsycheAutoCanvas();
        if (canvas == null) return;

        // Prefer updating via ResourceHUD if it exists on this canvas (keeps one source of truth)
        var hud = canvas.GetComponentInChildren<ResourceHUD>();
        if (hud != null)
        {
            if (ResourceManager.Instance != null)
            {
                // Sync GameManager values into ResourceManager so ResourceHUD displays correctly
                SyncToResourceManager();
            }
            if (hud.budgetText != null) hud.budgetText.text = string.Format(budgetFormat, _currentBudget);
            if (hud.timeText != null) hud.timeText.text = string.Format(timeFormat, _currentTime);
            if (hud.budgetTMP != null) hud.budgetTMP.text = string.Format(budgetFormat, _currentBudget);
            if (hud.timeTMP != null) hud.timeTMP.text = string.Format(timeFormat, _currentTime);
            return;
        }

        // Fallback: find Text components by name under the canvas
        var allTexts = canvas.GetComponentsInChildren<Text>(true);
        foreach (var t in allTexts)
        {
            if (t.gameObject.name == "BudgetText")
                t.text = string.Format(budgetFormat, _currentBudget);
            else if (t.gameObject.name == "TimeText")
                t.text = string.Format(timeFormat, _currentTime);
        }

        var allTMP = canvas.GetComponentsInChildren<TMP_Text>(true);
        foreach (var t in allTMP)
        {
            if (t.gameObject.name == "BudgetText")
                t.text = string.Format(budgetFormat, _currentBudget);
            else if (t.gameObject.name == "TimeText")
                t.text = string.Format(timeFormat, _currentTime);
        }
    }

    private static Canvas FindPsycheAutoCanvas()
    {
        var go = GameObject.Find("Psyche_AutoCanvas");
        return go != null ? go.GetComponent<Canvas>() : null;
    }

    /// <summary>Syncs current budget/time into ResourceManager so card logic and existing HUD stay in sync.</summary>
    private void SyncToResourceManager()
    {
        if (ResourceManager.Instance == null) return;
        // ResourceManager doesn't expose setters; GameManager is the source of truth for display.
        // If you need ResourceManager.Budget/TimeRemaining to match, add setters on ResourceManager or call AddBudget(0) / AddTime(0) after setting internal state. For now we only update the HUD display.
    }
}
