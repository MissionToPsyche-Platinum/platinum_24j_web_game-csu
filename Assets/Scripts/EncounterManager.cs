using UnityEngine;
using System;

/// <summary>
/// Manages the encounter lifecycle: setup, play, victory/defeat.
/// Tracks current encounter objective, progress, and turns.
/// Wires to EncounterPanel for UI and to DeckManager for card draws.
/// </summary>
public class EncounterManager : MonoBehaviour
{
    private static EncounterManager _instance;
    public static EncounterManager Instance => _instance;

    [Header("References")]
    [Tooltip("DeckManager for drawing cards at turn start.")]
    [SerializeField] private DeckManager deckManager;

    [Header("Current Encounter State (read-only in Inspector)")]
    [SerializeField] private string _encounterType = "DATA COLLECTION";
    [SerializeField] private string _objectiveDesc = "Collect 5 Surface data";
    [SerializeField] private int _targetProgress = 5;
    [SerializeField] private int _currentProgress;
    [SerializeField] private int _maxTurns = 8;
    [SerializeField] private int _currentTurn = 1;
    [SerializeField] private bool _encounterActive;

    [Header("Turn Settings")]
    [Tooltip("Cards to draw up to at the start of each turn.")]
    [SerializeField] private int handSize = 5;

    // Maneuver state flags (set by card effects, consumed on next relevant action)
    private bool _bonusNextInstrument;
    private bool _doubleNextInstrument;
    private bool _preventNextPenalty;
    private bool _reducePowerCostsThisTurn;

    [Header("Crisis / passive (runtime)")]
    [SerializeField] private int _turnStartPowerBonus;
    [SerializeField] private int _powerDrainEachTurn;
    [SerializeField] private int _extraManeuverPowerCost;
    [SerializeField] private bool _skipDrawOnce;
    [SerializeField] private bool _blockInstrumentData;
    [SerializeField] private bool _blockNextManeuverPlay;
    [SerializeField] private bool _stableOrbitAchieved;

    [Header("Passive Time drain (Time as run HP)")]
    [Tooltip("Time lost at each turn end (after End Turn) under normal conditions.")]
    [SerializeField] private int _passiveTimeDrainStandard = 1;
    [Tooltip("Time lost each turn end on boss encounters or when run floor ≥ threshold.")]
    [SerializeField] private int _passiveTimeDrainEscalated = 2;
    [Tooltip("Run floor at or above this uses escalated drain (stack with boss: either condition triggers 2/turn).")]
    [SerializeField] private int _floorThresholdForIncreasedDrain = 3;
    [Tooltip("Current run floor (1-based). Wire from GameUIController.SetFloor or GameManager when changing floors.")]
    [SerializeField] private int _runFloor = 1;
    [Tooltip("When true, uses escalated Time drain for this encounter.")]
    [SerializeField] private bool _bossEncounter = false;

    // Public accessors
    public string EncounterType => _encounterType;
    public string ObjectiveDesc => _objectiveDesc;
    public int TargetProgress => _targetProgress;
    public int CurrentProgress => _currentProgress;
    public int MaxTurns => _maxTurns;
    public int CurrentTurn => _currentTurn;
    public bool IsEncounterActive => _encounterActive;

    // Maneuver state accessors
    public bool BonusNextInstrument { get => _bonusNextInstrument; set => _bonusNextInstrument = value; }
    public bool DoubleNextInstrument { get => _doubleNextInstrument; set => _doubleNextInstrument = value; }
    public bool PreventNextPenalty { get => _preventNextPenalty; set => _preventNextPenalty = value; }
    public bool ReducePowerCostsThisTurn { get => _reducePowerCostsThisTurn; set => _reducePowerCostsThisTurn = value; }

    public int TurnStartPowerBonus => _turnStartPowerBonus;
    public int PowerDrainEachTurn => _powerDrainEachTurn;
    public int ExtraManeuverPowerCost => _extraManeuverPowerCost;
    public bool BlockInstrumentData => _blockInstrumentData;
    public bool BlockNextManeuverPlay => _blockNextManeuverPlay;
    public bool StableOrbitAchieved => _stableOrbitAchieved;

    public int RunFloor => _runFloor;
    public bool IsBossEncounter => _bossEncounter;

    /// <summary>Passive Time lost at turn end (standard 1; escalated 2 when boss or floor ≥ threshold).</summary>
    public int GetPassiveTimeDrainPerTurn()
    {
        if (_bossEncounter || _runFloor >= _floorThresholdForIncreasedDrain)
            return _passiveTimeDrainEscalated;
        return _passiveTimeDrainStandard;
    }

    /// <summary>Updates run floor for Time-drain scaling. Optionally sets boss encounter flag.</summary>
    public void SetRunFloor(int floor, bool? bossEncounter = null)
    {
        _runFloor = Mathf.Max(1, floor);
        if (bossEncounter.HasValue)
            _bossEncounter = bossEncounter.Value;
    }

    public void AddTurnStartPowerBonus(int amount)
    {
        if (amount <= 0) return;
        _turnStartPowerBonus += amount;
    }

    public void AddPowerDrainPerTurn(int amount)
    {
        if (amount <= 0) return;
        _powerDrainEachTurn += amount;
    }

    public void AddExtraManeuverPowerCost(int amount)
    {
        if (amount <= 0) return;
        _extraManeuverPowerCost += amount;
    }

    public void SetSkipDrawOnce() => _skipDrawOnce = true;

    public void SetBlockInstrumentData(bool on) => _blockInstrumentData = on;

    public void SetBlockNextManeuverPlay() => _blockNextManeuverPlay = true;

    public void SetStableOrbitAchieved() => _stableOrbitAchieved = true;

    /// <summary>If true, the next draw phase draws nothing (Ground Station crisis).</summary>
    public bool ConsumeSkipDrawOnce()
    {
        if (!_skipDrawOnce) return false;
        _skipDrawOnce = false;
        return true;
    }

    /// <summary>Called when a maneuver is successfully played; clears debris block if it was waiting.</summary>
    public void NotifyManeuverPlayedSuccessfully()
    {
        _blockNextManeuverPlay = false;
    }

    /// <summary>Clears ongoing crisis flags (Safe Mode Recovery / Cancel Crisis).</summary>
    public void ClearAllCrisisEffects()
    {
        _powerDrainEachTurn = 0;
        _extraManeuverPowerCost = 0;
        _skipDrawOnce = false;
        _blockInstrumentData = false;
        _blockNextManeuverPlay = false;
    }

    // Events
    /// <summary>Fired when a new encounter starts. (type, objective, target, turnLimit)</summary>
    public event Action<string, string, int, int> OnEncounterStarted;

    /// <summary>Fired when encounter progress changes. (current, max)</summary>
    public event Action<int, int> OnProgressChanged;

    /// <summary>Fired when the turn advances. (turnNumber)</summary>
    public event Action<int> OnTurnAdvanced;

    /// <summary>Fired when the encounter ends. (success)</summary>
    public event Action<bool> OnEncounterComplete;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    private void Start()
    {
        // Find DeckManager if not assigned (prefer human deck if an AI deck exists)
        if (deckManager == null)
        {
            var all = FindObjectsByType<DeckManager>(FindObjectsSortMode.None);
            foreach (var d in all)
            {
                if (d != null && !d.UsesAiResourceWallet)
                {
                    deckManager = d;
                    break;
                }
            }
            if (deckManager == null && all.Length > 0)
                deckManager = all[0];
        }

        // Start a default encounter
        StartEncounter(_encounterType, _objectiveDesc, _targetProgress, _maxTurns);
    }

    // -----------------------------------------------------------------------
    // Encounter lifecycle
    // -----------------------------------------------------------------------

    /// <summary>Begin a new encounter with the given parameters.</summary>
    /// <param name="isBossEncounter">If set, overrides boss flag for passive Time drain (2/turn). If null, keeps current serialized value.</param>
    public void StartEncounter(string type, string objectiveDesc, int target, int turnLimit, bool? isBossEncounter = null)
    {
        _encounterType = type;
        _objectiveDesc = objectiveDesc;
        _targetProgress = Mathf.Max(1, target);
        _currentProgress = 0;
        _maxTurns = turnLimit;
        _currentTurn = 1;
        _encounterActive = true;

        if (isBossEncounter.HasValue)
            _bossEncounter = isBossEncounter.Value;

        // Reset maneuver flags
        _bonusNextInstrument = false;
        _doubleNextInstrument = false;
        _preventNextPenalty = false;
        _reducePowerCostsThisTurn = false;
        _turnStartPowerBonus = 0;
        _powerDrainEachTurn = 0;
        _extraManeuverPowerCost = 0;
        _skipDrawOnce = false;
        _blockInstrumentData = false;
        _blockNextManeuverPlay = false;
        _stableOrbitAchieved = false;

        // Refresh power for the new encounter
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.RefreshForEncounter();

        Debug.Log($"[EncounterManager] Started: {type} — {objectiveDesc} (target: {target}, turns: {turnLimit})");
        OnEncounterStarted?.Invoke(type, objectiveDesc, _currentProgress, _targetProgress);
        OnProgressChanged?.Invoke(_currentProgress, _targetProgress);
        OnTurnAdvanced?.Invoke(_currentTurn);
    }

    /// <summary>Add progress toward the encounter objective (e.g. from instrument data collection).</summary>
    public void AddProgress(int amount)
    {
        if (!_encounterActive) return;
        _currentProgress = Mathf.Clamp(_currentProgress + amount, 0, _targetProgress);
        Debug.Log($"[EncounterManager] Progress: {_currentProgress}/{_targetProgress}");
        OnProgressChanged?.Invoke(_currentProgress, _targetProgress);

        if (_currentProgress >= _targetProgress)
        {
            CompleteEncounter(true);
        }
    }

    /// <summary>Advance to the next turn. Resets per-turn flags, draws cards.</summary>
    public void AdvanceTurn()
    {
        if (!_encounterActive) return;
        _currentTurn++;

        // Reset per-turn flags
        _reducePowerCostsThisTurn = false;

        var rm = ResourceManager.Instance;
        if (rm != null)
        {
            rm.ResetPowerAndBudgetForPlayerTurn();
            if (_turnStartPowerBonus > 0)
                rm.AddPower(_turnStartPowerBonus);
            if (_powerDrainEachTurn > 0)
                rm.AddPower(-_powerDrainEachTurn);

            rm.ApplyPassiveTimeDrainAtTurnEnd();
            if (rm.TimeRemaining <= 0)
            {
                Debug.Log("[EncounterManager] Time depleted — mission failed!");
                CompleteEncounter(false);
                return;
            }
        }

        Debug.Log($"[EncounterManager] Turn {_currentTurn}/{_maxTurns}");
        OnTurnAdvanced?.Invoke(_currentTurn);

        // Check turn limit
        if (_currentTurn > _maxTurns)
        {
            Debug.Log("[EncounterManager] Turn limit reached — encounter failed!");
            CompleteEncounter(false);
            return;
        }

        // Draw cards up to hand size
        if (deckManager != null)
        {
            int cardsNeeded = handSize - deckManager.Hand.Count;
            if (cardsNeeded > 0)
                deckManager.Draw(cardsNeeded);
        }
    }

    private void CompleteEncounter(bool success)
    {
        _encounterActive = false;
        string result = success ? "VICTORY" : "DEFEAT";
        Debug.Log($"[EncounterManager] Encounter {result}!");
        OnEncounterComplete?.Invoke(success);
    }

    /// <summary>Get the current progress as a 0-1 fraction.</summary>
    public float ProgressFraction => (float)_currentProgress / Mathf.Max(1, _targetProgress);
}
