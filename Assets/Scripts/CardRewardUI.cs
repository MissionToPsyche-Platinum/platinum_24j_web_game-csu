using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Post-encounter "Choose a Card" reward screen (STS-style).
/// Generates card options across multiple rounds; the player picks one per round to add to their deck or skips.
/// Attach to a panel GameObject under the main GameCanvas.
/// </summary>
public class CardRewardUI : MonoBehaviour
{
    /// <summary>True while the reward overlay is shown. Used by <see cref="HandFanner"/> to suppress hand hover.</summary>
    public static bool IsRewardPanelOpen { get; private set; }

    /// <summary>Fired after all reward rounds are done (picked or skipped). Use to start the next encounter.</summary>
    public event Action OnRewardsComplete;

    [Header("UI References")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text headerText;
    [SerializeField] private Button skipButton;

    [Header("Card Slots")]
    [SerializeField] private Transform cardSlotContainer;

    [Header("Prefab")]
    [SerializeField] private GameObject cardPrefab;

    [Header("Settings")]
    [Tooltip("Number of card choices shown per round.")]
    [SerializeField] private int cardsPerRound = 3;
    [Tooltip("How many rounds of card picks the player gets after an encounter.")]
    [SerializeField] private int totalRounds = 2;

    [Header("Debug")]
    [Tooltip("Enable to open the reward screen with ]. Disable when done testing.")]
    [SerializeField] private bool enableDebugHotkey = true;

    private DeckManager _deckManager;
    private CardData[] _options;
    private GameObject[] _optionViews;
    private bool _subscribed;
    private CanvasGroup _canvasGroup;
    private int _currentRound;

    private void Awake()
    {
        if (skipButton != null)
            skipButton.onClick.AddListener(OnSkip);

        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        SetPanelVisible(false);
    }

    private void Start()
    {
        SubscribeToEncounter();
    }

    private void OnEnable()
    {
        SubscribeToEncounter();
    }

    private void SubscribeToEncounter()
    {
        if (_subscribed) return;
        if (EncounterManager.Instance != null)
        {
            EncounterManager.Instance.OnEncounterComplete += OnEncounterComplete;
            _subscribed = true;
        }
    }

    private void Update()
    {
        if (enableDebugHotkey && UnityEngine.InputSystem.Keyboard.current != null
            && UnityEngine.InputSystem.Keyboard.current.rightBracketKey.wasPressedThisFrame)
        {
            bool isOpen = _canvasGroup != null && _canvasGroup.alpha > 0f;
            if (isOpen)
                Hide();
            else
                BeginRewards();
        }
    }

    private void OnDestroy()
    {
        if (IsRewardPanelOpen)
            IsRewardPanelOpen = false;

        if (EncounterManager.Instance != null)
            EncounterManager.Instance.OnEncounterComplete -= OnEncounterComplete;

        if (skipButton != null)
            skipButton.onClick.RemoveListener(OnSkip);
    }

    private void OnEncounterComplete(bool success)
    {
        if (!success) return;
        BeginRewards();
    }

    private void BeginRewards()
    {
        _currentRound = 0;
        transform.SetAsLastSibling();
        ShowCurrentRound();
    }

    private void ShowCurrentRound()
    {
        if (cardPrefab == null)
            cardPrefab = Resources.Load<GameObject>("CardView");
        if (cardPrefab == null)
        {
            Debug.LogWarning("[CardRewardUI] No card prefab assigned and none found in Resources.");
            return;
        }

        ClearOptions();

        _options = new CardData[cardsPerRound];
        _optionViews = new GameObject[cardsPerRound];

        for (int i = 0; i < cardsPerRound; i++)
        {
            _options[i] = DeckManager.CreateRandomRuntimeCard();

            var go = Instantiate(cardPrefab, cardSlotContainer);
            _optionViews[i] = go;

            var view = go.GetComponent<CardView>();
            if (view != null)
                view.BindForGallery(_options[i]);

            var rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.localScale = Vector3.one * 1.15f;
                rt.localRotation = Quaternion.identity;
            }

            int idx = i;
            var btn = go.GetComponent<Button>();
            if (btn == null)
                btn = go.AddComponent<Button>();
            btn.onClick.AddListener(() => OnCardPicked(idx));
        }

        if (headerText != null)
            headerText.text = $"Choose a Card  ({_currentRound + 1} / {totalRounds})";

        SetPanelVisible(true);
    }

    private void OnCardPicked(int index)
    {
        if (_options == null || index < 0 || index >= _options.Length) return;

        CardData picked = _options[index];
        if (_deckManager == null)
            _deckManager = FindPrimaryPlayerDeckManager();

        if (_deckManager != null)
        {
            _deckManager.AddCardToDeck(picked);
            Debug.Log($"[CardRewardUI] Round {_currentRound + 1}/{totalRounds} — picked: {picked.cardName}");
        }

        AdvanceOrClose();
    }

    private void OnSkip()
    {
        Debug.Log($"[CardRewardUI] Round {_currentRound + 1}/{totalRounds} — skipped.");
        AdvanceOrClose();
    }

    private void AdvanceOrClose()
    {
        _currentRound++;
        if (_currentRound < totalRounds)
        {
            ShowCurrentRound();
        }
        else
        {
            Hide();
        }
    }

    private void Hide()
    {
        ClearOptions();
        SetPanelVisible(false);
        OnRewardsComplete?.Invoke();
    }

    private void SetPanelVisible(bool visible)
    {
        IsRewardPanelOpen = visible;
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.blocksRaycasts = visible;
            _canvasGroup.interactable = visible;
        }
    }

    private void ClearOptions()
    {
        if (_optionViews != null)
        {
            foreach (var go in _optionViews)
                if (go != null) Destroy(go);
        }
        _optionViews = null;
        _options = null;
    }

    private static DeckManager FindPrimaryPlayerDeckManager()
    {
        var all = UnityEngine.Object.FindObjectsByType<DeckManager>(FindObjectsSortMode.None);
        foreach (var d in all)
        {
            if (d != null && !d.UsesAiResourceWallet)
                return d;
        }
        return all.Length > 0 ? all[0] : null;
    }
}
