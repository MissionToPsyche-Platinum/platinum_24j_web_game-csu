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
    public int currentFloor = 1;
    public static GameManager Instance => _instance;

    [Header("Transition to next floor")]
    [Tooltip("Time cost when transitioning to the next encounter/floor (deducted from ResourceManager).")]
    [SerializeField] private int timeCostPerFloor = 1;

    [Tooltip("Scene name to load as the next encounter. Leave empty to reload the current scene.")]
    [SerializeField] private string nextEncounterSceneName = "";

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

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
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
    
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.AddTime(-timeCostPerFloor);

        if (string.IsNullOrEmpty(nextEncounterSceneName))
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        else
            SceneManager.LoadScene(nextEncounterSceneName);
    }

    /// <summary>Resets run resources on ResourceManager (e.g. new run from menu).</summary>
    public void ResetRun(int startPower = 4, int startBudget = 4, int startTime = 40)
    {
        ResourceManager.Instance?.ResetForNewRun(startPower, startBudget, startTime);
    }
}
