using UnityEngine;
using System;

/// <summary>
/// Manages the three run resources for Psyche Mission Strategy:
/// Power (refreshes each encounter), Budget (persistent), Time (persistent).
/// Design doc: Power 3, Budget 6, Time 15 at run start; Power resets to 3 each encounter.
/// </summary>
public class ResourceManager : MonoBehaviour
{
    [Header("Starting values (per design doc)")]
    [Tooltip("Refreshes to this value at the start of each encounter.")]
    public int startingPower = 3;

    [Tooltip("Persistent for the whole run.")]
    public int startingBudget = 6;

    [Tooltip("Persistent countdown; 0 = game over.")]
    public int startingTime = 15;

    [Header("Current values (read-only in Inspector)")]
    [SerializeField] private int _power;
    [SerializeField] private int _budget;
    [SerializeField] private int _time;

    /// <summary>Power - refreshes each encounter. Use for playing cards.</summary>
    public int Power => _power;

    /// <summary>Budget - persistent. Use for expensive cards and events.</summary>
    public int Budget => _budget;

    /// <summary>Time - persistent countdown. Reaching 0 = loss.</summary>
    public int TimeRemaining => _time;

    /// <summary>True if Time is 0 or Budget is negative (game over conditions).</summary>
    public bool IsGameOver => _time <= 0 || _budget < 0;

    /// <summary>Fired when any resource changes. (Power, Budget, Time).</summary>
    public event Action<int, int, int> OnResourcesChanged;

    /// <summary>Fired when game over condition is met (Time 0 or Budget &lt; 0).</summary>
    public event Action OnGameOver;

    private static ResourceManager _instance;
    /// <summary>Singleton access. Ensure one ResourceManager exists in the scene (e.g. on a GameManager).</summary>
    public static ResourceManager Instance => _instance;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        ResetForNewRun();
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    /// <summary>Call at the start of a new run (e.g. when starting game from menu). Sets Power, Budget, Time to starting values.</summary>
    public void ResetForNewRun()
    {
        _power = startingPower;
        _budget = startingBudget;
        _time = startingTime;
        NotifyChange();
        CheckGameOver();
    }

    /// <summary>Call at the start of each encounter. Refreshes Power to starting value; Budget and Time unchanged.</summary>
    public void RefreshForEncounter()
    {
        _power = startingPower;
        NotifyChange();
    }

    /// <summary>Try to spend resources. Returns true if successful and deducts cost.</summary>
    public bool TrySpend(int power, int budget, int time)
    {
        if (!CanAfford(power, budget, time))
            return false;

        _power -= power;
        _budget -= budget;
        _time -= time;
        NotifyChange();
        CheckGameOver();
        return true;
    }

    /// <summary>Check if the player can afford this cost (without actually spending).</summary>
    public bool CanAfford(int power, int budget, int time)
    {
        return _power >= power && _budget >= budget && _time >= time;
    }

    /// <summary>Add resources (e.g. from card effects). Use negative values to subtract (prefer TrySpend for costs).</summary>
    public void Add(int power, int budget, int time)
    {
        _power += power;
        _budget += budget;
        _time += time;
        NotifyChange();
        CheckGameOver();
    }

    public void AddPower(int amount) => Add(amount, 0, 0);
    public void AddBudget(int amount) => Add(0, amount, 0);
    public void AddTime(int amount) => Add(0, 0, amount);

    private void NotifyChange()
    {
        OnResourcesChanged?.Invoke(_power, _budget, _time);
    }

    private void CheckGameOver()
    {
        if (_time <= 0 || _budget < 0)
            OnGameOver?.Invoke();
    }
}
