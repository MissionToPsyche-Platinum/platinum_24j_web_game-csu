using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton for scene transitions and run reset entry points.
/// Power, budget, and time are owned exclusively by <see cref="ResourceManager"/>; this class does not duplicate that state.
/// HUD updates flow from ResourceManager.OnResourcesChanged (ResourceHUD, GameHUD, etc.).
/// </summary>
public class GameManager : MonoBehaviour
{
    private static GameManager _instance;

    public static GameManager Instance => _instance;

    [Header("Run State")]
    [SerializeField] private int currentFloor = 1;

    [Header("Transition to next floor")]
    [Tooltip("Time cost when transitioning to the next encounter/floor (deducted from ResourceManager).")]
    [SerializeField] private int timeCostPerFloor = 1;

    [Tooltip("Scene name to load as the next encounter. Leave empty to reload the current scene.")]
    [SerializeField] private string nextEncounterSceneName = "";

    // Events
    public System.Action OnGameLoss;
    public System.Action OnGameWin;

    public int CurrentFloor => currentFloor;

    /// <summary>Delegates to ResourceManager.Budget.</summary>
    public int CurrentBudget => ResourceManager.Instance != null ? ResourceManager.Instance.Budget : 0;

    /// <summary>Delegates to ResourceManager.TimeRemaining.</summary>
    public int CurrentTime => ResourceManager.Instance != null ? ResourceManager.Instance.TimeRemaining : 0;

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

    private void Start()
    {
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.OnMissionFailed += HandleRunFailure;
        }
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            if (ResourceManager.Instance != null)
                ResourceManager.Instance.OnMissionFailed -= HandleRunFailure;
            _instance = null;
        }
    }

    private void HandleRunFailure()
    {
        Debug.Log("[GameManager] Run Failure Triggered!");
        OnGameLoss?.Invoke();
    }

    public void TriggerVictory()
    {
        Debug.Log("[GameManager] Run Victory Triggered!");
        OnGameWin?.Invoke();
    }

    /// <summary>Forwards to ResourceManager.</summary>
    public void AddBudget(int amount)
    {
        ResourceManager.Instance?.AddBudget(amount);
    }

    /// <summary>Forwards to ResourceManager.</summary>
    public void AddTime(int amount)
    {
        ResourceManager.Instance?.AddTime(amount);
    }

    /// <summary>Subtracts transition time via ResourceManager, then loads the next scene.</summary>
    public void TransitionToNextFloor()
    {
        currentFloor++;
        if (currentFloor > 4)
        {
            TriggerVictory();
            return;
        }

        if (ResourceManager.Instance != null)
            ResourceManager.Instance.AddTime(-timeCostPerFloor);

        if (string.IsNullOrEmpty(nextEncounterSceneName))
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        else
            SceneManager.LoadScene(nextEncounterSceneName);
    }

    /// <summary>Resets run resources on ResourceManager (e.g. new run from menu).</summary>
    public void ResetRun(int startBudget = 6, int startTime = 15, int startPower = 3)
    {
        currentFloor = 1;
        ResourceManager.Instance?.ResetForNewRun(startPower, startBudget, startTime);
    }
}
