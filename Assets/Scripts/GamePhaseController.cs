using System.Collections;
using UnityEngine;

/// <summary>
/// Human vs. computer turn flow: PlayerTurn → AITurn → (EncounterManager advances) → PlayerTurn.
/// When this component is present, wire <see cref="GameUIController"/> to delegate End Turn here.
/// </summary>
public class GamePhaseController : MonoBehaviour
{
    public enum TurnPhase
    {
        PlayerTurn,
        AITurn,
        ResolvingTurn
    }

    public static GamePhaseController Instance { get; private set; }

    [Header("References")]
    [SerializeField] private OpponentAIController opponent;
    [SerializeField] private EncounterManager encounterManager;
    [Tooltip("Optional: used to disable End Turn during AI / resolve.")]
    [SerializeField] private GameUIController gameUIController;

    [Header("State (read-only)")]
    [SerializeField] private TurnPhase currentPhase = TurnPhase.PlayerTurn;

    private Coroutine _runningSequence;

    public TurnPhase CurrentPhase => currentPhase;

    /// <summary>When no controller exists, interaction stays allowed (solo / legacy).</summary>
    public static bool PlayerMayInteractWithCards =>
        Instance == null || Instance.currentPhase == TurnPhase.PlayerTurn;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[GamePhaseController] Duplicate instance — destroying this one.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Safe default before Start / encounter callbacks (Inspector might serialize another enum value).
        currentPhase = TurnPhase.PlayerTurn;
    }

    private void OnDestroy()
    {
        if (encounterManager != null)
            encounterManager.OnEncounterStarted -= OnEncounterStarted;

        if (Instance == this)
            Instance = null;
    }

    private void OnEnable()
    {
        // Re-entering play mode or toggling the object must not leave cards blocked in AITurn.
        ResetToPlayerTurnSoft();
    }

    private void Start()
    {
        if (encounterManager == null)
            encounterManager = EncounterManager.Instance;
        if (opponent == null)
            opponent = FindAnyObjectByType<OpponentAIController>();
        if (gameUIController == null)
            gameUIController = FindAnyObjectByType<GameUIController>();

        if (encounterManager != null)
            encounterManager.OnEncounterStarted += OnEncounterStarted;

        ResetToPlayerTurnSoft();
    }

    private void OnEncounterStarted(string type, string objective, int current, int target)
    {
        // New encounter = human acts first; cancel any stuck phase coroutine from edit-mode / reload.
        if (_runningSequence != null)
        {
            StopCoroutine(_runningSequence);
            _runningSequence = null;
        }

        currentPhase = TurnPhase.PlayerTurn;
        ApplyEndTurnButtonState();
    }

    /// <summary>Clears phase to player without stopping coroutines (used from OnEnable).</summary>
    private void ResetToPlayerTurnSoft()
    {
        currentPhase = TurnPhase.PlayerTurn;
        ApplyEndTurnButtonState();
    }

    /// <summary>
    /// Call this from the End Turn button (via <see cref="GameUIController"/> delegation).
    /// </summary>
    public void OnPlayerPressedEndTurn()
    {
        if (currentPhase != TurnPhase.PlayerTurn)
            return;

        if (_runningSequence != null)
            StopCoroutine(_runningSequence);

        _runningSequence = StartCoroutine(CoRoundAfterPlayerEnds());
    }

    private IEnumerator CoRoundAfterPlayerEnds()
    {
        currentPhase = TurnPhase.AITurn;
        ApplyEndTurnButtonState();

        if (opponent != null)
            yield return StartCoroutine(opponent.RunTurnCoroutine());
        else
            yield return null;

        currentPhase = TurnPhase.ResolvingTurn;

        if (encounterManager != null)
            encounterManager.AdvanceTurn();

        currentPhase = TurnPhase.PlayerTurn;
        ApplyEndTurnButtonState();

        gameUIController?.NotifyRoundCompletedAfterPhase();
        _runningSequence = null;
    }

    private void ApplyEndTurnButtonState()
    {
        bool allow = currentPhase == TurnPhase.PlayerTurn;
        if (gameUIController != null)
            gameUIController.SetEndTurnInteractable(allow);
    }
}
