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

    [Header("Run State Panels")]
    [SerializeField] private GameObject missionStartPanel; // Shown at start of run
    [SerializeField] private GameObject victoryPanel;      // Shown on run win
    [SerializeField] private GameObject defeatPanel;       // Shown on run loss

    [Header("Pile Counters")]
    [SerializeField] private TMP_Text deckCountText;
    [SerializeField] private TMP_Text discardCountText;

    [Header("End Turn Button")]
    [SerializeField] private Button endTurnButton;

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
        if (endTurnButton != null)
            endTurnButton.onClick.AddListener(HandleEndTurn);
    }

    private void Start()
    {
        // Find singletons
        _deckManager = FindAnyObjectByType<DeckManager>();
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

        // Subscribe to GameManager run events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameWin += ShowVictory;
            GameManager.Instance.OnGameLoss += ShowDefeat;
        }

        // Initially show mission start if on floor 1 and encounter not active
        if (GameManager.Instance != null && GameManager.Instance.CurrentFloor == 1 && (_encounterManager == null || !_encounterManager.IsEncounterActive))
        {
            missionStartPanel?.SetActive(true);
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

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameWin -= ShowVictory;
            GameManager.Instance.OnGameLoss -= ShowDefeat;
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
        
        // If it's a boss and we failed, GameManager.OnGameLoss will handle it.
        // If it's a normal victory, we might want to show a simple "Continue" button or just TransitionToNextFloor.
        // For now, let's just log. The user asked for "start game and end game interface",
        // which I interpret as the whole run.
    }

    private void ShowVictory()
    {
        victoryPanel?.SetActive(true);
    }

    private void ShowDefeat()
    {
        defeatPanel?.SetActive(true);
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
        // Delegate to EncounterManager for turn logic
        if (_encounterManager != null)
        {
            _encounterManager.AdvanceTurn();
        }

        Debug.Log($"[GameUIController] End Turn clicked");
        onEndTurn?.Invoke();
    }
}

