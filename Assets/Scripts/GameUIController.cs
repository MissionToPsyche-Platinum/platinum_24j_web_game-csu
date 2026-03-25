using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

/// <summary>
/// Master UI controller for the main game scene.
/// Wires together GameHUD, EncounterPanel, pile counters, and the End Turn button.
/// Subscribes to backend events (ResourceManager, DeckManager, EncounterManager)
/// so the UI updates automatically when cards are played or turns advance.
/// </summary>
public class GameUIController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameHUD         hudPanel;
    [SerializeField] private EncounterPanel  encounterPanel;

    [Header("Pile Counters")]
    [SerializeField] private TMP_Text deckCountText;
    [SerializeField] private TMP_Text discardCountText;

    [Header("End Turn Button")]
    [SerializeField] private Button endTurnButton;

    [Header("Turn flow (Human vs. AI)")]
    [Tooltip("When set, End Turn runs AI turn then EncounterManager.AdvanceTurn. Leave empty for legacy solo flow.")]
    [SerializeField] private GamePhaseController gamePhaseController;

    [Header("Play Zone")]
    [SerializeField] private GameObject playZone;   // Drop target for played cards

    [Header("Events")]
    /// <summary>Subscribe to this to receive End Turn notifications.</summary>
    public UnityEvent onEndTurn = new UnityEvent();

    // Cached references to singletons
    private DeckManager _deckManager;
    private EncounterManager _encounterManager;

    private void Awake()
    {
        if (endTurnButton == null)
        {
            foreach (var b in GetComponentsInChildren<Button>(true))
            {
                if (b.gameObject.name == "EndTurnButton")
                {
                    endTurnButton = b;
                    break;
                }
            }
        }

        if (endTurnButton != null)
        {
            // Scene prefab overrides once pointed OnClick at editor MonoScript assets; strip so our handler runs.
            endTurnButton.onClick.RemoveAllListeners();
            endTurnButton.onClick.AddListener(HandleEndTurn);
        }
    }

    private void Start()
    {
        // Prefer the human deck when two DeckManagers exist (player + AI).
        _deckManager = FindPrimaryPlayerDeckManager();
        _encounterManager = EncounterManager.Instance;

        // Subscribe to ResourceManager events → update HUD
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.OnResourcesChanged += OnResourcesChanged;

        // Subscribe to DeckManager events → update pile counts + feedback
        if (_deckManager != null)
        {
            _deckManager.OnHandChanged += OnHandChanged;
            _deckManager.OnCardPlayed += OnCardPlayed;
        }

        // Subscribe to EncounterManager events → update encounter panel
        if (_encounterManager != null)
        {
            _encounterManager.OnEncounterStarted += OnEncounterStarted;
            _encounterManager.OnProgressChanged += OnProgressChanged;
            _encounterManager.OnTurnAdvanced += OnTurnAdvanced;
            _encounterManager.OnEncounterComplete += OnEncounterComplete;
        }

        // Initial UI state from current values
        RefreshAllUI();
    }

    private void OnDestroy()
    {
        if (endTurnButton != null)
            endTurnButton.onClick.RemoveListener(HandleEndTurn);

        if (ResourceManager.Instance != null)
            ResourceManager.Instance.OnResourcesChanged -= OnResourcesChanged;

        if (_deckManager != null)
        {
            _deckManager.OnHandChanged -= OnHandChanged;
            _deckManager.OnCardPlayed -= OnCardPlayed;
        }

        if (_encounterManager != null)
        {
            _encounterManager.OnEncounterStarted -= OnEncounterStarted;
            _encounterManager.OnProgressChanged -= OnProgressChanged;
            _encounterManager.OnTurnAdvanced -= OnTurnAdvanced;
            _encounterManager.OnEncounterComplete -= OnEncounterComplete;
        }
    }

    // -----------------------------------------------------------------------
    // Event handlers — backend → frontend
    // -----------------------------------------------------------------------

    private void OnResourcesChanged(int power, int budget, int time)
    {
        hudPanel?.UpdateResources(power, budget, time);
    }

    private void OnHandChanged()
    {
        if (_deckManager != null)
        {
            UpdateDeckCount(_deckManager.DeckCount);
            UpdateDiscardCount(_deckManager.DiscardCount);
        }
    }

    private void OnCardPlayed(CardData card)
    {
        Debug.Log($"[GameUIController] Card played: {card.cardName}");
    }

    private void OnEncounterStarted(string type, string objective, int current, int turnLimit)
    {
        if (_encounterManager != null)
            encounterPanel?.SetEncounter(type, objective, current, _encounterManager.TargetProgress, turnLimit);
    }

    private void OnProgressChanged(int current, int max)
    {
        encounterPanel?.UpdateProgress(current, max);
    }

    private void OnTurnAdvanced(int turn)
    {
        encounterPanel?.SetTurn(turn);
    }

    private void OnEncounterComplete(bool success)
    {
        string result = success ? "VICTORY!" : "DEFEAT!";
        Debug.Log($"[GameUIController] Encounter result: {result}");
        // TODO: show victory/defeat overlay
    }

    // -----------------------------------------------------------------------
    // Public API — call these from your game logic scripts
    // -----------------------------------------------------------------------

    /// <summary>Updates the resource HUD. Call after any resource change.</summary>
    public void UpdateResources(int power, int budget, int time)
    {
        hudPanel?.UpdateResources(power, budget, time);
    }

    /// <summary>Sets the current floor and refreshes the floor indicator.</summary>
    public void SetFloor(int floor)
    {
        hudPanel?.SetFloor(floor, 4);
    }

    /// <summary>Updates the deck pile counter label.</summary>
    public void UpdateDeckCount(int count)
    {
        if (deckCountText != null)
            deckCountText.text = $"Deck\n{count}";
    }

    /// <summary>Updates the discard pile counter label.</summary>
    public void UpdateDiscardCount(int count)
    {
        if (discardCountText != null)
            discardCountText.text = $"Discard\n{count}";
    }

    /// <summary>Populates the encounter panel with new encounter data.</summary>
    public void SetEncounter(string type, string objective, int current, int max, int turnLimit)
    {
        encounterPanel?.SetEncounter(type, objective, current, max, turnLimit);
    }

    /// <summary>Updates encounter progress (e.g. after playing an instrument card).</summary>
    public void UpdateEncounterProgress(int current, int max)
    {
        encounterPanel?.UpdateProgress(current, max);
    }

    /// <summary>Enables or disables the End Turn button (e.g. during animations).</summary>
    public void SetEndTurnInteractable(bool interactable)
    {
        if (endTurnButton != null)
            endTurnButton.interactable = interactable;
    }

    // -----------------------------------------------------------------------
    // Internal helpers
    // -----------------------------------------------------------------------

    private static DeckManager FindPrimaryPlayerDeckManager()
    {
        var all = Object.FindObjectsByType<DeckManager>(FindObjectsSortMode.None);
        foreach (var d in all)
        {
            if (d != null && !d.UsesAiResourceWallet)
                return d;
        }

        return all.Length > 0 ? all[0] : null;
    }

    private void RefreshAllUI()
    {
        // Resources
        if (ResourceManager.Instance != null)
            hudPanel?.UpdateResources(
                ResourceManager.Instance.Power,
                ResourceManager.Instance.Budget,
                ResourceManager.Instance.TimeRemaining);
        else
            hudPanel?.UpdateResources(3, 6, 15); // Design doc defaults

        hudPanel?.SetFloor(1, 4);

        // Pile counts
        if (_deckManager != null)
        {
            UpdateDeckCount(_deckManager.DeckCount);
            UpdateDiscardCount(_deckManager.DiscardCount);
        }
        else
        {
            UpdateDeckCount(10);
            UpdateDiscardCount(0);
        }
    }

    private void HandleEndTurn()
    {
        if (gamePhaseController == null)
            gamePhaseController = FindAnyObjectByType<GamePhaseController>();

        if (gamePhaseController != null)
        {
            gamePhaseController.OnPlayerPressedEndTurn();
            return;
        }

        if (_encounterManager != null)
            _encounterManager.AdvanceTurn();

        Debug.Log("[GameUIController] End Turn clicked");
        onEndTurn?.Invoke();
    }

    /// <summary>Called by <see cref="GamePhaseController"/> after AI + encounter advance so other listeners still get a round-complete signal.</summary>
    public void NotifyRoundCompletedAfterPhase()
    {
        Debug.Log("[GameUIController] Round complete (player + AI)");
        onEndTurn?.Invoke();
    }
}

