using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

/// <summary>
/// Master UI controller for the main game scene.
/// Wires together GameHUD, EncounterPanel, pile counters, and the End Turn button.
/// Other game systems (deck manager, encounter manager) should call into this controller
/// rather than touching UI components directly.
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

    [Header("Play Zone")]
    [SerializeField] private GameObject playZone;   // Drop target for played cards

    [Header("Events")]
    /// <summary>Subscribe to this to receive End Turn notifications.</summary>
    public UnityEvent onEndTurn = new UnityEvent();

    // -----------------------------------------------------------------------
    // Internal state (mirrors ResourceManager starting values from design doc)
    // -----------------------------------------------------------------------
    private int _power  = 3;
    private int _budget = 6;
    private int _time   = 15;
    private int _floor  = 1;
    private int _turn   = 1;

    private void Awake()
    {
        if (endTurnButton != null)
            endTurnButton.onClick.AddListener(HandleEndTurn);
    }

    private void Start()
    {
        RefreshHUD();
        UpdateDeckCount(10);    // Starting deck size from design doc
        UpdateDiscardCount(0);
    }

    private void OnDestroy()
    {
        if (endTurnButton != null)
            endTurnButton.onClick.RemoveListener(HandleEndTurn);
    }

    // -----------------------------------------------------------------------
    // Public API — call these from your game logic scripts
    // -----------------------------------------------------------------------

    /// <summary>Updates the resource HUD. Call after any resource change.</summary>
    public void UpdateResources(int power, int budget, int time)
    {
        _power  = power;
        _budget = budget;
        _time   = time;
        RefreshHUD();
    }

    /// <summary>Sets the current floor and refreshes the floor indicator.</summary>
    public void SetFloor(int floor)
    {
        _floor = floor;
        hudPanel?.SetFloor(_floor, 4);
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

    private void RefreshHUD()
    {
        hudPanel?.UpdateResources(_power, _budget, _time);
        hudPanel?.SetFloor(_floor, 4);
    }

    private void HandleEndTurn()
    {
        _turn++;
        encounterPanel?.SetTurn(_turn);
        Debug.Log($"[GameUIController] End Turn → Turn {_turn}");
        onEndTurn?.Invoke();
    }
}
