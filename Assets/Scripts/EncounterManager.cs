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
    public void StartEncounter(string type, string objectiveDesc, int target, int turnLimit)
    {
        _encounterType = type;
        _objectiveDesc = objectiveDesc;
        _targetProgress = Mathf.Max(1, target);
        _currentProgress = 0;
        _maxTurns = turnLimit;
        _currentTurn = 1;
        _encounterActive = true;

        // Reset maneuver flags
        _bonusNextInstrument = false;
        _doubleNextInstrument = false;
        _preventNextPenalty = false;
        _reducePowerCostsThisTurn = false;

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
