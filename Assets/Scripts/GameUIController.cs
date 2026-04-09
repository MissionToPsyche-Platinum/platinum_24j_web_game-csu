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

    [Header("Deck browser")]
    [Tooltip("Optional; added at runtime on this GameObject if missing.")]
    [SerializeField] private DeckBrowserUI deckBrowser;

    [Header("End Turn Button")]
    [SerializeField] private Button endTurnButton;

    [Header("Turn flow (Human vs. AI)")]
    [Tooltip("When set, End Turn runs AI turn then EncounterManager.AdvanceTurn. Leave empty for legacy solo flow.")]
    [SerializeField] private GamePhaseController gamePhaseController;

    [Header("Play Zone")]
    [SerializeField] private GameObject playZone;   // Drop target for played cards

    [Header("Crisis UI")]
    [SerializeField] private CrisisWidgetUI crisisWidgetPrefab;
    [SerializeField] private Transform crisisContainer;
    private System.Collections.Generic.List<CrisisWidgetUI> _activeCrises = new System.Collections.Generic.List<CrisisWidgetUI>();

    [Header("End Game UI")]
    [SerializeField] private GameOverUI gameOverUI;

    [Header("Testing & Flow")]
    [Tooltip("If true, the game ends in Victory immediately after hitting progress on the first encounter. Useful for testing without playing all 4 floors.")]
    public bool singleEncounterMode = true;

    [Header("Events")]
    /// <summary>Subscribe to this to receive End Turn notifications.</summary>
    public UnityEvent onEndTurn = new UnityEvent();

    // Cached references to singletons
    private DeckManager _deckManager;
    private EncounterManager _encounterManager;
    private CardRewardUI _cardRewardUI;

    [Header("Game Flow State")]
    private int _currentPhase = 1;
    private int _encountersCompletedInPhase = 0;

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

        TryResolvePileCounterTexts();
        TryResolveHudPanel();
        TryResolveEncounterPanel();
        TryWireDeckPileButton();
    }

    private void EnsureGameOverUI()
    {
        if (gameOverUI != null) return;
        
        gameOverUI = GetComponentInChildren<GameOverUI>(true);
        if (gameOverUI == null)
        {
            var go = new GameObject("GameOverUI");
            go.transform.SetParent(transform, false);
            gameOverUI = go.AddComponent<GameOverUI>();
        }
    }

    private void TryResolveHudPanel()
    {
        if (hudPanel == null)
            hudPanel = GetComponentInChildren<GameHUD>(true);
    }

    private void TryResolveEncounterPanel()
    {
        if (encounterPanel == null)
            encounterPanel = GetComponentInChildren<EncounterPanel>(true);
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

        if (deckBrowser == null)
            deckBrowser = GetComponent<DeckBrowserUI>();
        if (deckBrowser == null)
            deckBrowser = gameObject.AddComponent<DeckBrowserUI>();

        // Subscribe to EncounterManager events → update encounter panel
        if (_encounterManager != null)
        {
            _encounterManager.OnEncounterStarted += OnEncounterStarted;
            _encounterManager.OnProgressChanged += OnProgressChanged;
            _encounterManager.OnTurnAdvanced += OnTurnAdvanced;
            _encounterManager.OnEncounterComplete += OnEncounterComplete;
        }

        _cardRewardUI = GetComponentInChildren<CardRewardUI>(true);
        if (_cardRewardUI != null)
        {
            _cardRewardUI.OnRewardsComplete += HandleRewardsComplete;
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

        if (_cardRewardUI != null)
        {
            _cardRewardUI.OnRewardsComplete -= HandleRewardsComplete;
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

    private void OnEncounterStarted(string type, string objective, int currentProgress, int targetProgress)
    {
        if (_encounterManager == null)
            return;
        int maxTurns = _encounterManager.MaxTurns;
        encounterPanel?.SetEncounter(type, objective, currentProgress, targetProgress, maxTurns);
        hudPanel?.SetEncounterBrief(EncounterPresentation.HudSubtitleForType(type));
    }

    private void OnProgressChanged(int current, int max)
    {
        encounterPanel?.UpdateProgress(current, max);
    }

    private void OnTurnAdvanced(int turn)
    {
        encounterPanel?.SetTurn(turn);
        hudPanel?.SetTurn(turn);
    }

    private void OnEncounterComplete(bool success)
    {
        string result = success ? "VICTORY!" : "DEFEAT!";
        Debug.Log($"[GameUIController] Encounter result: {result}");
        if (!success)
        {
            EnsureGameOverUI();
            gameOverUI.ShowDefeat();
        }
        else if (singleEncounterMode)
        {
            EnsureGameOverUI();
            gameOverUI.ShowVictory();
        }
        // Otherwise, victory triggers CardRewardUI, which then calls HandleRewardsComplete
    }

    private void OnCrisisActivated(CardData.EffectType crisisType)
    {
        if (crisisWidgetPrefab == null || crisisContainer == null) return;
        
        var widget = Instantiate(crisisWidgetPrefab, crisisContainer);
        widget.Setup(crisisType);
        _activeCrises.Add(widget);
    }

    private void OnCrisisResolved(CardData.EffectType crisisType)
    {
        for (int i = _activeCrises.Count - 1; i >= 0; i--)
        {
            if (_activeCrises[i] != null && _activeCrises[i].CrisisType == crisisType)
            {
                Destroy(_activeCrises[i].gameObject);
                _activeCrises.RemoveAt(i);
                break; // Only remove one instance of that type
            }
        }
    }

    private void HandleRewardsComplete()
    {
        if (_encounterManager == null) return;
        
        _encountersCompletedInPhase++;
        
        int reqEncounters = GetRequiredEncountersForPhase(_currentPhase);
        if (_encountersCompletedInPhase >= reqEncounters)
        {
            _currentPhase++;
            _encountersCompletedInPhase = 0;
        }
        
        // If we beat floor 4 boss, we win.
        if (_currentPhase > 4)
        {
            Debug.Log("GAME WON! Mission Complete.");
            EnsureGameOverUI();
            gameOverUI.ShowVictory();
            return;
        }

        SetFloor(_currentPhase);
        StartEncounterForCurrentPhase();
        
        if (_deckManager != null)
        {
            _deckManager.DiscardHand();
            _deckManager.Draw(5);
        }
    }

    private int GetRequiredEncountersForPhase(int phase)
    {
        switch (phase)
        {
            case 1: return 3; // Cruise Phase
            case 2: return 1; // Orbit Boss
            case 3: return 4; // Science Ops
            case 4: return 1; // Mission Review Boss
            default: return 99;
        }
    }

    private void StartEncounterForCurrentPhase()
    {
        switch (_currentPhase)
        {
            case 1: // Cruise Phase
                int d1 = 5 + _encountersCompletedInPhase;
                _encounterManager.StartDataCollectionEncounter($"Collect {d1} Surface Data", d1, 6 + _encountersCompletedInPhase);
                // 30% chance for a crisis after the first tutorial encounter
                if (_encountersCompletedInPhase > 0 && UnityEngine.Random.value < 0.3f)
                    _encounterManager.TriggerRandomCrisis();
                break;
            case 2: // Orbit Insertion
                _encounterManager.StartOrbitInsertionBoss(8);
                break;
            case 3: // Science Operations
                int d3 = 8 + _encountersCompletedInPhase;
                _encounterManager.StartDataCollectionEncounter($"Collect {d3} Elemental Data", d3, 8 + _encountersCompletedInPhase);
                // 50% chance for a crisis in Science Ops
                if (UnityEngine.Random.value < 0.5f)
                    _encounterManager.TriggerRandomCrisis();
                break;
            case 4: // Mission Review
                _encounterManager.StartMissionReviewBoss(10);
                break;
        }
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
        bool isBoss = (floor == 2 || floor == 4);
        int encounterDisplay = _encountersCompletedInPhase + 1;
        hudPanel?.SetFloorPhase(floor, encounterDisplay, isBoss);
        EncounterManager.Instance?.SetRunFloor(floor);
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

    private void TryResolvePileCounterTexts()
    {
        if (deckCountText == null || discardCountText == null)
        {
            foreach (var tmp in GetComponentsInChildren<TMP_Text>(true))
            {
                if (tmp == null) continue;
                if (deckCountText == null && tmp.gameObject.name == "DeckCountText")
                    deckCountText = tmp;
                else if (discardCountText == null && tmp.gameObject.name == "DiscardCountText")
                    discardCountText = tmp;
            }
        }
    }

    private void TryWireDeckPileButton()
    {
        Transform deckPile = FindDeepChild(transform, "DeckPile");
        if (deckPile == null)
            return;

        var btn = deckPile.GetComponent<Button>();
        if (btn == null)
        {
            btn = deckPile.gameObject.AddComponent<Button>();
            var img = deckPile.GetComponent<Image>();
            if (img != null)
                btn.targetGraphic = img;
        }

        btn.onClick.RemoveListener(HandleDeckPileClicked);
        btn.onClick.AddListener(HandleDeckPileClicked);
    }

    private void HandleDeckPileClicked()
    {
        if (deckBrowser == null)
            deckBrowser = GetComponent<DeckBrowserUI>();
        if (deckBrowser == null)
            deckBrowser = gameObject.AddComponent<DeckBrowserUI>();
        deckBrowser.Toggle();
    }

    private static Transform FindDeepChild(Transform root, string childName)
    {
        if (root == null) return null;
        if (root.name == childName)
            return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var found = FindDeepChild(root.GetChild(i), childName);
            if (found != null)
                return found;
        }

        return null;
    }

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
            hudPanel?.UpdateResources(4, 4, 40);

        hudPanel?.SetFloorPhase(1, 1, false);

        if (_encounterManager != null)
        {
            encounterPanel?.SetEncounter(
                _encounterManager.EncounterType,
                _encounterManager.ObjectiveDesc,
                _encounterManager.CurrentProgress,
                _encounterManager.TargetProgress,
                _encounterManager.MaxTurns);
            hudPanel?.SetEncounterBrief(EncounterPresentation.HudSubtitleForType(_encounterManager.EncounterType));
            encounterPanel?.SetTurn(_encounterManager.CurrentTurn);
            hudPanel?.SetTurn(_encounterManager.CurrentTurn);
        }

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

