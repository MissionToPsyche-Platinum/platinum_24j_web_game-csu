using System;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }
    public event System.Action<int, int, int> OnResourcesChanged;

    /// <summary>Fired once when Time reaches 0 (mission failure condition).</summary>
    public event Action OnMissionFailedByTime;

    private bool _missionFailureByTimeDispatched;

    [Header("Resource Values")]
    public int power = 4;
    public int budget = 4;
    public int time = 45;

    [Header("Player turn (after End Turn / turn advance)")]
    [Tooltip("Power and Budget are set to these values at the start of each player turn.")]
    public int turnStartPower = 4;
    public int turnStartBudget = 4;

    public int Power => power;
    public int Budget => budget;
    public int TimeRemaining => time;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void UpdateUI()
    {
        OnResourcesChanged?.Invoke(power, budget, time);
        NotifyMissionFailureByTimeIfNeeded();
    }

    private void NotifyMissionFailureByTimeIfNeeded()
    {
        if (time > 0 || _missionFailureByTimeDispatched)
            return;
        _missionFailureByTimeDispatched = true;
        OnMissionFailedByTime?.Invoke();
    }
// --- ADDED METHODS TO FIX CS1061 ERRORS ---
    public void AddPower(int amount)
    {
        power = Mathf.Max(0, power + amount);
        UpdateUI();
        EncounterManager.Instance?.CheckBossConditions();
    }

    public void AddBudget(int amount)
    {
        budget = Mathf.Max(0, budget + amount);
        UpdateUI();
    }

    public void AddTime(int amount)
    {
        time = Mathf.Max(0, time + amount);
        UpdateUI();
    }
    // ------------------------------------------
    /// <summary>Legacy hook: same passive Time loss as <see cref="EncounterManager.AdvanceTurn"/> plus P/B reset.</summary>
    public void EndTurn()
    {
        ApplyPassiveTimeDrainAtTurnEnd();
        ResetPowerAndBudgetForPlayerTurn();
        if (time <= 0) Debug.Log("Mission Failed — Time depleted!");
    }

    /// <summary>
    /// Time acts as run HP: loses a fixed amount at each turn advance (standard vs boss / late floor).
    /// </summary>
    public void ApplyPassiveTimeDrainAtTurnEnd()
    {
        int drain = 1;
        if (EncounterManager.Instance != null)
            drain = EncounterManager.Instance.GetPassiveTimeDrainPerTurn();
        time = Mathf.Max(0, time - drain);
        UpdateUI();
    }

    /// <summary>Sets Power and Budget to turn-start values and refreshes UI.</summary>
    public void ResetPowerAndBudgetForPlayerTurn()
    {
        power = turnStartPower;
        budget = turnStartBudget;
        UpdateUI();
        EncounterManager.Instance?.CheckBossConditions();
    }

    /// <summary>Called when a new encounter begins (turn 1).</summary>
    public void RefreshForEncounter()
    {
        ResetPowerAndBudgetForPlayerTurn();
    }

    /// <summary>Reset run resources to defaults (or override for custom starts / menu).</summary>
    public void ResetForNewRun(int startPower = 4, int startBudget = 4, int startTime = 45)
    {
        power = startPower;
        budget = startBudget;
        time = startTime;
        _missionFailureByTimeDispatched = false;
        UpdateUI();
    }
    
    public bool CanAfford(int p, int b, int t) => power >= p && budget >= b && time >= t;

    public void TrySpend(int p, int b, int t)
    {
        power -= p; budget -= b; time -= t;
        if (b > 0)
            EncounterManager.Instance?.NotifyBudgetSpent(b);
        UpdateUI();
        EncounterManager.Instance?.CheckBossConditions(); // e.g. if an event triggered logic spending power, probably don't need this, but to be safe.
    }
}