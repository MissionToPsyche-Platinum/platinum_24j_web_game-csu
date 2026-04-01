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
    /// <summary>Non–Solar-Storm power loss at turn start (legacy / other effects).</summary>
    [SerializeField] private int _powerDrainEachTurn;
    [SerializeField] private int _extraManeuverPowerCost;
    [SerializeField] private bool _skipDrawOnce;
    [SerializeField] private bool _blockInstrumentData;
    [SerializeField] private bool _blockNextManeuverPlay;
    [SerializeField] private bool _stableOrbitAchieved;

    [Tooltip("Solar Storm: extra Time lost each turn end (stacks). Design: −2/turn on top of passive drain.")]
    [SerializeField] private int _solarStormExtraTimePerTurn;
    [SerializeField] private bool _thrusterAnomalyActive;
    [SerializeField] private bool _groundStationConflictActive;
    [SerializeField] private bool _dataStorageFullActive;
    [SerializeField] private bool _debrisFieldCrisisActive;
    [SerializeField] private bool _computerRebootCrisisActive;
    [SerializeField] private bool _computerRebootPendingSkipDraw;
    [SerializeField] private bool _computerRebootCompletingSkippedTurn;
    [SerializeField] private bool _budgetCutRestoreAvailable;
    [SerializeField] private int _budgetCutRestoreBudgetAmount = 2;

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

    public enum EncounterLogicType { Standard, OrbitInsertionBoss, MissionReviewBoss }
    [Header("Boss Tracking")]
    [SerializeField] private EncounterLogicType _currentLogicType = EncounterLogicType.Standard;
    [SerializeField] private int _budgetSpentThisEncounter = 0;

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

    public int SolarStormExtraTimePerTurn => _solarStormExtraTimePerTurn;
    public bool ThrusterAnomalyActive => _thrusterAnomalyActive;
    public bool GroundStationConflictActive => _groundStationConflictActive;
    public bool DataStorageFullActive => _dataStorageFullActive;
    public bool DebrisFieldCrisisActive => _debrisFieldCrisisActive;
    public bool ComputerRebootCrisisActive => _computerRebootCrisisActive;
    public bool BudgetCutRestoreAvailable => _budgetCutRestoreAvailable;

    public int RunFloor => _runFloor;
    public bool IsBossEncounter => _bossEncounter;

    /// <summary>Passive Time lost at turn end (Floor 1: 1, Floor 2: 2, Floor 3: 2, Floor 4: 1).</summary>
    public int GetPassiveTimeDrainPerTurn()
    {
        if (_runFloor == 2 || _runFloor == 3)
            return _passiveTimeDrainEscalated; // 2
        return _passiveTimeDrainStandard; // 1
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

    /// <summary>Called when a maneuver is successfully played. Debris crisis is only cleared via <see cref="TryResolveDebrisField"/>.</summary>
    public void NotifyManeuverPlayedSuccessfully()
    {
        if (_debrisFieldCrisisActive) return;
        _blockNextManeuverPlay = false;
    }

    /// <summary>Clears ongoing crisis flags (Safe Mode Recovery / Cancel Crisis — “enter safe mode”).</summary>
    public void ClearAllCrisisEffects()
    {
        _powerDrainEachTurn = 0;
        _extraManeuverPowerCost = 0;
        _skipDrawOnce = false;
        _blockInstrumentData = false;
        _blockNextManeuverPlay = false;
        _solarStormExtraTimePerTurn = 0;
        _thrusterAnomalyActive = false;
        _groundStationConflictActive = false;
        _dataStorageFullActive = false;
        _debrisFieldCrisisActive = false;
        _computerRebootCrisisActive = false;
        _computerRebootPendingSkipDraw = false;
        _computerRebootCompletingSkippedTurn = false;
        _budgetCutRestoreAvailable = false;
    }

    public event Action<CardData.EffectType> OnCrisisActivated;
    public event Action<CardData.EffectType> OnCrisisResolved;

    public void TriggerRandomCrisis()
    {
        var possibleCrises = new System.Collections.Generic.List<CardData.EffectType> {
            CardData.EffectType.CrisisSolarStorm,
            CardData.EffectType.CrisisThrusterTax,
            CardData.EffectType.CrisisBlockDrawOnce,
            CardData.EffectType.CrisisBlockDataCollection,
            CardData.EffectType.CrisisBlockNextManeuver,
            CardData.EffectType.CrisisComputerReboot,
            CardData.EffectType.CrisisBudgetCut
        };
        var type = possibleCrises[UnityEngine.Random.Range(0, possibleCrises.Count)];
        
        switch (type)
        {
            case CardData.EffectType.CrisisSolarStorm: AddSolarStormExtraTimeDrain(2); break;
            case CardData.EffectType.CrisisThrusterTax: ActivateThrusterAnomaly(1); break;
            case CardData.EffectType.CrisisBlockDrawOnce: ActivateGroundStationConflict(); break;
            case CardData.EffectType.CrisisBlockDataCollection: ActivateDataStorageFull(); break;
            case CardData.EffectType.CrisisBlockNextManeuver: ActivateDebrisField(); break;
            case CardData.EffectType.CrisisComputerReboot: ActivateComputerRebootCrisis(); break;
            case CardData.EffectType.CrisisBudgetCut: ActivateBudgetCut(3, 2); break;
        }
    }

    // --- Crisis activation (from crisis cards) ---

    public void AddSolarStormExtraTimeDrain(int amountPerTurn)
    {
        if (amountPerTurn <= 0) return;
        _solarStormExtraTimePerTurn += amountPerTurn;
        OnCrisisActivated?.Invoke(CardData.EffectType.CrisisSolarStorm);
    }

    public void ActivateThrusterAnomaly(int extraPowerPerManeuver = 1)
    {
        if (extraPowerPerManeuver <= 0) return;
        _thrusterAnomalyActive = true;
        AddExtraManeuverPowerCost(extraPowerPerManeuver);
        OnCrisisActivated?.Invoke(CardData.EffectType.CrisisThrusterTax);
    }

    public void ActivateGroundStationConflict()
    {
        _groundStationConflictActive = true;
        SetSkipDrawOnce();
        OnCrisisActivated?.Invoke(CardData.EffectType.CrisisBlockDrawOnce);
    }

    public void ActivateDataStorageFull()
    {
        _dataStorageFullActive = true;
        SetBlockInstrumentData(true);
        OnCrisisActivated?.Invoke(CardData.EffectType.CrisisBlockDataCollection);
    }

    public void ActivateDebrisField()
    {
        _debrisFieldCrisisActive = true;
        SetBlockNextManeuverPlay();
        OnCrisisActivated?.Invoke(CardData.EffectType.CrisisBlockNextManeuver);
    }

    public void ActivateComputerRebootCrisis()
    {
        _computerRebootCrisisActive = true;
        _computerRebootPendingSkipDraw = true;
        OnCrisisActivated?.Invoke(CardData.EffectType.CrisisComputerReboot);
    }

    public void ActivateBudgetCut(int immediateBudgetLoss, int restoreBudgetOnResolve)
    {
        var rm = ResourceManager.Instance;
        if (rm != null)
            rm.AddBudget(-Mathf.Abs(immediateBudgetLoss));
        _budgetCutRestoreAvailable = true;
        _budgetCutRestoreBudgetAmount = Mathf.Max(1, restoreBudgetOnResolve);
        OnCrisisActivated?.Invoke(CardData.EffectType.CrisisBudgetCut);
    }

    // --- Crisis resolution (pay costs from design doc table) ---

    /// <summary>Solar Storm: Pay 3 Power (Safe Mode / Cancel Crisis clears all instead).</summary>
    public bool TryResolveSolarStormWithPower()
    {
        var rm = ResourceManager.Instance;
        if (rm == null || _solarStormExtraTimePerTurn <= 0) return false;
        if (!rm.CanAfford(3, 0, 0)) return false;
        rm.TrySpend(3, 0, 0);
        _solarStormExtraTimePerTurn = 0;
        OnCrisisResolved?.Invoke(CardData.EffectType.CrisisSolarStorm);
        return true;
    }

    /// <summary>Thruster: Pay 2 Budget and 2 Time.</summary>
    public bool TryResolveThrusterAnomaly()
    {
        var rm = ResourceManager.Instance;
        if (rm == null || !_thrusterAnomalyActive) return false;
        if (!rm.CanAfford(0, 2, 2)) return false;
        rm.TrySpend(0, 2, 2);
        _thrusterAnomalyActive = false;
        if (_extraManeuverPowerCost > 0)
            _extraManeuverPowerCost = Mathf.Max(0, _extraManeuverPowerCost - 1);
        OnCrisisResolved?.Invoke(CardData.EffectType.CrisisThrusterTax);
        return true;
    }

    /// <summary>Ground Station: Pay 2 Budget (clears upcoming draw skip if not yet consumed).</summary>
    public bool TryResolveGroundStationConflict()
    {
        var rm = ResourceManager.Instance;
        if (rm == null || !_groundStationConflictActive) return false;
        if (!rm.CanAfford(0, 2, 0)) return false;
        rm.TrySpend(0, 2, 0);
        _groundStationConflictActive = false;
        _skipDrawOnce = false;
        OnCrisisResolved?.Invoke(CardData.EffectType.CrisisBlockDrawOnce);
        return true;
    }

    /// <summary>Data Storage: Pay 2 Time to downlink.</summary>
    public bool TryResolveDataStorageFull()
    {
        var rm = ResourceManager.Instance;
        if (rm == null || !_dataStorageFullActive) return false;
        if (!rm.CanAfford(0, 0, 2)) return false;
        rm.TrySpend(0, 0, 2);
        _dataStorageFullActive = false;
        _blockInstrumentData = false;
        OnCrisisResolved?.Invoke(CardData.EffectType.CrisisBlockDataCollection);
        return true;
    }

    /// <summary>Debris: Pay 3 Power and 1 Budget.</summary>
    public bool TryResolveDebrisField()
    {
        var rm = ResourceManager.Instance;
        if (rm == null || !_debrisFieldCrisisActive) return false;
        if (!rm.CanAfford(3, 1, 0)) return false;
        rm.TrySpend(3, 1, 0);
        _debrisFieldCrisisActive = false;
        _blockNextManeuverPlay = false;
        OnCrisisResolved?.Invoke(CardData.EffectType.CrisisBlockNextManeuver);
        return true;
    }

    /// <summary>Computer Reboot: Pay 4 Power — no turn lost if paid before skip resolves.</summary>
    public bool TryResolveComputerReboot()
    {
        var rm = ResourceManager.Instance;
        if (rm == null || !_computerRebootCrisisActive) return false;
        if (!rm.CanAfford(4, 0, 0)) return false;
        rm.TrySpend(4, 0, 0);
        bool refillAfterSkippedDraw = _computerRebootCompletingSkippedTurn;
        _computerRebootCrisisActive = false;
        _computerRebootPendingSkipDraw = false;
        _computerRebootCompletingSkippedTurn = false;
        if (refillAfterSkippedDraw && deckManager != null)
        {
            int need = handSize - deckManager.Hand.Count;
            if (need > 0)
                deckManager.Draw(need);
        }
        OnCrisisResolved?.Invoke(CardData.EffectType.CrisisComputerReboot);
        return true;
    }

    /// <summary>Budget Cut: Pay 3 Time to restore 2 Budget (amounts from card effectValue2).</summary>
    public bool TryResolveBudgetCutRestore()
    {
        var rm = ResourceManager.Instance;
        if (rm == null || !_budgetCutRestoreAvailable) return false;
        if (!rm.CanAfford(0, 0, 3)) return false;
        rm.TrySpend(0, 0, 3);
        rm.AddBudget(_budgetCutRestoreBudgetAmount);
        _budgetCutRestoreAvailable = false;
        OnCrisisResolved?.Invoke(CardData.EffectType.CrisisBudgetCut);
        return true;
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
            
        _currentLogicType = EncounterLogicType.Standard;
        _budgetSpentThisEncounter = 0;

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
        _solarStormExtraTimePerTurn = 0;
        _thrusterAnomalyActive = false;
        _groundStationConflictActive = false;
        _dataStorageFullActive = false;
        _debrisFieldCrisisActive = false;
        _computerRebootCrisisActive = false;
        _computerRebootPendingSkipDraw = false;
        _computerRebootCompletingSkippedTurn = false;
        _budgetCutRestoreAvailable = false;

        // Refresh power for the new encounter
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.RefreshForEncounter();

        Debug.Log($"[EncounterManager] Started: {type} — {objectiveDesc} (target: {target}, turns: {turnLimit})");
        OnEncounterStarted?.Invoke(type, objectiveDesc, _currentProgress, _targetProgress);
        OnProgressChanged?.Invoke(_currentProgress, _targetProgress);
        OnTurnAdvanced?.Invoke(_currentTurn);
    }

    /// <summary>Starts a data-collection encounter (UI + HUD use <c>DATA COLLECTION</c> copy).</summary>
    public void StartDataCollectionEncounter(string objectiveDesc, int targetDataPoints, int turnLimit, bool? isBossEncounter = null)
    {
        StartEncounter("DATA COLLECTION", objectiveDesc, targetDataPoints, turnLimit, isBossEncounter);
    }

    /// <summary>Starts a resource-management encounter (UI + HUD use <c>RESOURCE MANAGEMENT</c> copy).</summary>
    public void StartResourceManagementEncounter(string objectiveDesc, int targetProgress, int turnLimit, bool? isBossEncounter = null)
    {
        StartEncounter("RESOURCE MANAGEMENT", objectiveDesc, targetProgress, turnLimit, isBossEncounter);
    }

    /// <summary>Starts Floor 2 Boss.</summary>
    public void StartOrbitInsertionBoss(int turnLimit)
    {
        StartEncounter("ORBIT INSERTION", "Accumulate 10 Power & spend 5 Budget", 1, turnLimit, true);
        _currentLogicType = EncounterLogicType.OrbitInsertionBoss;
        CheckBossConditions();
    }

    /// <summary>Starts Floor 4 Boss.</summary>
    public void StartMissionReviewBoss(int turnLimit)
    {
        StartEncounter("MISSION REVIEW", "Present 3 Conclusions", 1, turnLimit, true);
        _currentLogicType = EncounterLogicType.MissionReviewBoss;
        CheckBossConditions();
    }

    public void NotifyBudgetSpent(int amount)
    {
        if (amount > 0)
        {
            _budgetSpentThisEncounter += amount;
            CheckBossConditions();
        }
    }

    public void CheckBossConditions()
    {
        if (!_encounterActive) return;

        if (_currentLogicType == EncounterLogicType.OrbitInsertionBoss)
        {
            var rm = ResourceManager.Instance;
            // Fake progress out of 1 for the generic bar (optional). The win condition is literal.
            _currentProgress = (rm != null && rm.Power >= 10 && _budgetSpentThisEncounter >= 5) ? 1 : 0;
            OnProgressChanged?.Invoke(_currentProgress, _targetProgress);

            if (_currentProgress >= 1)
                CompleteEncounter(true);
        }
        else if (_currentLogicType == EncounterLogicType.MissionReviewBoss)
        {
            var dt = DataTracker.Instance;
            _currentProgress = (dt != null && dt.TotalConclusions >= 3) ? 1 : 0;
            OnProgressChanged?.Invoke(_currentProgress, _targetProgress);

            if (_currentProgress >= 1)
                CompleteEncounter(true);
        }
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

        // Computer Reboot: player just finished the “no draw” turn — crisis ends (turn was lost) unless they paid 4 Power earlier.
        if (_computerRebootCompletingSkippedTurn)
        {
            _computerRebootCompletingSkippedTurn = false;
            _computerRebootCrisisActive = false;
        }

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
            if (_solarStormExtraTimePerTurn > 0)
            {
                int loss = Mathf.Min(_solarStormExtraTimePerTurn, rm.TimeRemaining);
                if (loss > 0)
                    rm.AddTime(-loss);
            }
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

        // Draw cards up to hand size (Computer Reboot: skip one draw phase unless resolved with 4 Power)
        if (deckManager != null)
        {
            // Discard remaining hand cards first
            deckManager.DiscardHand();

            if (_computerRebootCrisisActive && _computerRebootPendingSkipDraw)
            {
                _computerRebootPendingSkipDraw = false;
                _computerRebootCompletingSkippedTurn = true;
            }
            else
            {
                int cardsNeeded = handSize - deckManager.Hand.Count;
                if (cardsNeeded > 0)
                    deckManager.Draw(cardsNeeded);
            }
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
