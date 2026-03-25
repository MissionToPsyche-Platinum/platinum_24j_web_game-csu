using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages deck, hand, and discard. Handles card play effects including resource changes,
/// data collection, maneuvers, analysis, and card draw. Fires events so the UI layer
/// (GameUIController, GameHUD, EncounterPanel) can react to card plays.
/// </summary>
public class DeckManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Parent for hand cards (e.g. Hand_Zone or the GameObject with CardHandLayout).")]
    public Transform handParent;

    [Tooltip("Prefab with CardView (and CardHover for CardHandLayout, or CardTrigger for HandFanner).")]
    public GameObject cardPrefab;

    [Tooltip("Optional: if using CardHandLayout, assign it here so the hand is fanned after drawing.")]
    public CardHandLayout cardHandLayout;

    [Header("Feedback message (optional)")]
    [Tooltip("Optional: assign a Text or TMP_Text for feedback. If empty, will find or create one under Psyche_AutoCanvas.")]
    public GameObject feedbackMessageTarget;

    [Header("Starting deck (assign CardData assets)")]
    [Tooltip("Cards to shuffle into the deck at run start. Leave empty to use Draw Phase: 5 random runtime cards.")]
    public List<CardData> startingDeck = new List<CardData>();

    [Header("Draw Phase: random card pool (used when startingDeck is empty)")]
    [Tooltip("Number of random cards to generate and place in hand at game start.")]
    public int drawPhaseCardCount = 5;

    [Header("Optional: AI opponent deck")]
    [Tooltip("When set, this deck spends Power/Budget/Time and applies Gain* card effects on the AI wallet instead of ResourceManager.")]
    [SerializeField] private OpponentAIController aiResourceWallet;

    /// <summary>True when this deck is wired as the computer's deck (uses <see cref="aiResourceWallet"/>).</summary>
    public bool UsesAiResourceWallet => aiResourceWallet != null;

    private readonly List<CardData> _deck = new List<CardData>();
    private readonly List<CardData> _hand = new List<CardData>();
    private readonly List<CardData> _discard = new List<CardData>();
    private readonly List<GameObject> _handViews = new List<GameObject>();

    /// <summary>Full card pool — every card in the game with art, costs, and effects.</summary>
    private static readonly (string name, string desc, int p, int b, int t, CardData.EffectType effect, int value, CardData.CardCategory cat, string art)[] AllCardTemplates =
    {
        // ── Resource ──
        ("Solar Array Deploy",      "Gain 3 Power",                       0, 0, 0, CardData.EffectType.GainPower,        3, CardData.CardCategory.Resource,   "CardArt/solar array deploy"),
        ("Budget Request",          "Gain 3 Budget",                      0, 0, 1, CardData.EffectType.GainBudget,       3, CardData.CardCategory.Resource,   "CardArt/budget request card"),
        ("Mission Extension",       "Gain 5 Time",                        0, 3, 0, CardData.EffectType.GainTime,         5, CardData.CardCategory.Resource,   "CardArt/mission extension"),
        ("Nuclear Battery",         "Gain 4 Power",                       0, 2, 0, CardData.EffectType.GainPower,        4, CardData.CardCategory.Resource,   "CardArt/nuclear battery"),
        ("Emergency Fund",          "Gain 5 Budget",                      0, 0, 2, CardData.EffectType.GainBudget,       5, CardData.CardCategory.Resource,   "CardArt/emergency fund"),
        ("Deep Space Optical Comms","Reduce power costs this turn",       0, 1, 1, CardData.EffectType.ReducePowerCosts, 1, CardData.CardCategory.Resource,   "CardArt/Deep Space Optical Comms"),

        // ── Instrument (data collection) ──
        ("Multispectral Imager",    "Collect 2 Surface data",             2, 0, 1, CardData.EffectType.CollectSurface,   2, CardData.CardCategory.Instrument, "CardArt/multispectral imager"),
        ("Gamma-Ray Spectrometer",  "Collect 3 Elemental data",           3, 0, 2, CardData.EffectType.CollectElemental, 3, CardData.CardCategory.Instrument, "CardArt/Gamma-Ray & Neutron Spectrometer"),
        ("Magnetometer",            "Collect 2 Magnetic data",            2, 0, 2, CardData.EffectType.CollectMagnetic,  2, CardData.CardCategory.Instrument, "CardArt/Magnetometer"),
        ("X-band Radio",            "Collect 3 Gravity data",             1, 0, 3, CardData.EffectType.CollectGravity,   3, CardData.CardCategory.Instrument, "CardArt/X-band Radio"),
        ("Multi-Instrument Suite",  "Collect 1 of all data types",        3, 1, 2, CardData.EffectType.CollectAllData,   1, CardData.CardCategory.Instrument, "CardArt/Multi-Instrument Suite"),
        ("Magnetic Modeling",       "Collect 3 Magnetic data",            2, 1, 2, CardData.EffectType.CollectMagnetic,  3, CardData.CardCategory.Instrument, "CardArt/Magnetic Modeling"),
        ("Thermal Reconstruction",  "Collect 2 Surface + 1 Elemental",   3, 0, 2, CardData.EffectType.CollectSurface,   3, CardData.CardCategory.Instrument, "CardArt/Thermal Reconstruction"),
        ("Peer Review Publication", "Draw 2 cards",                       0, 2, 1, CardData.EffectType.DrawCards,        2, CardData.CardCategory.Instrument, "CardArt/Peer Review Publication"),

        // ── Maneuver ──
        ("Trajectory Correction",   "Adjust orbital phase (+1 progress)", 2, 1, 0, CardData.EffectType.AdjustOrbit,          1, CardData.CardCategory.Maneuver, "CardArt/Trajectory Correction"),
        ("Altitude Adjustment",     "Next instrument card +1 data",       1, 1, 0, CardData.EffectType.BonusNextInstrument,  1, CardData.CardCategory.Maneuver, "CardArt/Altitude Adjustment"),
        ("Close Approach Flyby",    "Next instrument card x2 data",       3, 2, 0, CardData.EffectType.DoubleNextInstrument, 1, CardData.CardCategory.Maneuver, "CardArt/Close Approach Flyby"),
        ("Orbit Insertion Burn",    "Orbit insertion (+3 progress)",      4, 2, 0, CardData.EffectType.OrbitInsertion,       3, CardData.CardCategory.Maneuver, "CardArt/Orbit Insertion Burn"),
        ("Reaction Wheel Reset",    "Prevent next crisis penalty",        1, 0, 1, CardData.EffectType.PreventPenalty,       1, CardData.CardCategory.Maneuver, "CardArt/Reaction Wheel Reset"),

        // ── Analysis (conclusions) ──
        ("Compositional Analysis",  "3 Elemental + 2 Surface → Composition", 0, 1, 2, CardData.EffectType.CompositionConclusion, 0, CardData.CardCategory.Analysis, "CardArt/Compositional Analysis"),
        ("Structural Study",        "3 Gravity + 2 Surface → Interior",      0, 1, 2, CardData.EffectType.InteriorConclusion,    0, CardData.CardCategory.Analysis, "CardArt/Structural Study"),
        ("Comparative Planetology", "Wild conclusion (any data combo)",       0, 2, 3, CardData.EffectType.WildConclusion,        0, CardData.CardCategory.Analysis, "CardArt/Comparative Planetology"),

        // ── Crisis (negative event cards — 0 cost, auto-play when drawn) ──
        ("Budget Cut Notice",       "Lose 3 Budget",                      0, 0, 0, CardData.EffectType.LoseBudget,      3, CardData.CardCategory.Crisis, "CardArt/Budget Cut Notice"),
        ("Solar Storm Warning",     "Lose 2 Power",                       0, 0, 0, CardData.EffectType.LosePower,       2, CardData.CardCategory.Crisis, "CardArt/Solar Storm Warning"),
        ("Computer Reboot Required","Lose 1 Time",                        0, 0, 0, CardData.EffectType.LoseTime,        1, CardData.CardCategory.Crisis, "CardArt/Computer Reboot Required"),
        ("Data Storage Full",       "Discard 1 random card",              0, 0, 0, CardData.EffectType.DiscardRandom,    1, CardData.CardCategory.Crisis, "CardArt/Data Storage Full"),
        ("Debris Field Detected",   "Lose 2 Progress",                    0, 0, 0, CardData.EffectType.LoseProgress,    2, CardData.CardCategory.Crisis, "CardArt/Debris Field Detected"),
        ("Ground Station Conflict", "Lose 2 Budget",                      0, 0, 0, CardData.EffectType.LoseBudget,      2, CardData.CardCategory.Crisis, "CardArt/Ground Station Conflict"),
        ("Thruster Anomaly",        "Lose 2 Power and 1 Time",            0, 0, 0, CardData.EffectType.LosePower,       2, CardData.CardCategory.Crisis, "CardArt/Thruster Anomaly"),
        ("Safe Mode Recovery",      "Skip next turn",                     0, 0, 0, CardData.EffectType.SkipTurn,        1, CardData.CardCategory.Crisis, "CardArt/Safe Mode Recovery"),
    };

    /// <summary>Non-crisis card templates for random deck building and rewards.</summary>
    private static readonly (string name, string desc, int p, int b, int t, CardData.EffectType effect, int value, CardData.CardCategory cat, string art)[] RandomCardTemplates =
        System.Array.FindAll(AllCardTemplates, c => c.cat != CardData.CardCategory.Crisis);

    // --- Public properties ---
    public IReadOnlyList<CardData> Hand => _hand;
    public int DeckCount => _deck.Count;
    public int DiscardCount => _discard.Count;

    // --- Events ---
    /// <summary>Fired when the hand changes (card drawn or played).</summary>
    public event Action OnHandChanged;

    /// <summary>Fired when a card is successfully played. (CardData playedCard)</summary>
    public event Action<CardData> OnCardPlayed;

    private void Start()
    {
        if (handParent == null)
        {
            var anchor = GameObject.Find("HandViewAnchor");
            if (anchor != null) handParent = anchor.transform;
            else
            {
                var hand = GameObject.Find("Hand_Zone");
                if (hand != null) handParent = hand.transform;
                else handParent = transform;
            }
        }
        if (!handParent.gameObject.activeSelf)
            handParent.gameObject.SetActive(true);

        if (cardPrefab == null)
            cardPrefab = Resources.Load<GameObject>("CardView");

        if (startingDeck != null && startingDeck.Count > 0)
        {
            _deck.Clear();
            foreach (var c in startingDeck)
                if (c != null) _deck.Add(c);
            ShuffleDeck();
            Draw(Mathf.Min(drawPhaseCardCount, _deck.Count));
        }
        else
        {
            // Draw Phase: generate N random cards and place in hand
            _deck.Clear();
            int count = Mathf.Clamp(drawPhaseCardCount, 1, 20);
            for (int i = 0; i < count; i++)
                _deck.Add(CreateRandomRuntimeCard());
            ShuffleDeck();
            Draw(count);
        }

        if (cardHandLayout != null)
        {
            cardHandLayout.CollectExistingCards();
            cardHandLayout.RefreshLayout();
        }
    }

    // -----------------------------------------------------------------------
    // Drawing
    // -----------------------------------------------------------------------

    /// <summary>Draw up to count cards. Refills from discard if deck is empty.</summary>
    public void Draw(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (_deck.Count == 0)
            {
                if (_discard.Count == 0) break;
                foreach (var c in _discard) _deck.Add(c);
                _discard.Clear();
                ShuffleDeck();
            }
            if (_deck.Count == 0) break;
            var card = _deck[_deck.Count - 1];
            _deck.RemoveAt(_deck.Count - 1);
            _hand.Add(card);
            SpawnCardView(card);
        }

        RefreshHandLayout();
        OnHandChanged?.Invoke();
    }

    // -----------------------------------------------------------------------
    // Playing cards
    // -----------------------------------------------------------------------

    /// <summary>Try to play the card at hand index. Returns true if played.</summary>
    public bool TryPlayCard(int handIndex)
    {
        if (handIndex < 0 || handIndex >= _hand.Count) return false;
        var card = _hand[handIndex];
        if (UsesAiResourceWallet)
        {
            if (aiResourceWallet == null) return false;
        }
        else if (ResourceManager.Instance == null)
            return false;

        // Adjust power cost if ReducePowerCosts is active this turn
        int effectivePowerCost = card.costPower;
        if (EncounterManager.Instance != null && EncounterManager.Instance.ReducePowerCostsThisTurn && effectivePowerCost > 0)
            effectivePowerCost = Mathf.Max(0, effectivePowerCost - 1);

        if (UsesAiResourceWallet)
        {
            if (!aiResourceWallet.CanAfford(effectivePowerCost, card.costBudget, card.costTime))
                return false;
            aiResourceWallet.TrySpend(effectivePowerCost, card.costBudget, card.costTime);
        }
        else
        {
            if (!ResourceManager.Instance.CanAfford(effectivePowerCost, card.costBudget, card.costTime))
            {
                ShowFeedback("Not enough resources!");
                return false;
            }

            ResourceManager.Instance.TrySpend(effectivePowerCost, card.costBudget, card.costTime);
        }

        // Apply the card's effect
        ApplyEffect(card);

        // Move card from hand to discard
        _hand.RemoveAt(handIndex);
        RemoveCardViewAt(handIndex);
        _discard.Add(card);

        // Show feedback
        string effectSummary = card.EffectSummary();
        if (!string.IsNullOrEmpty(effectSummary))
            ShowFeedback($"{card.cardName} → {effectSummary}");

        Debug.Log($"[DeckManager] Played: {card.cardName} (Cost P:{effectivePowerCost} B:{card.costBudget} T:{card.costTime}) → {effectSummary}");

        // Refresh layout so remaining cards re-fan
        RefreshHandLayout();

        // Fire events
        OnCardPlayed?.Invoke(card);
        OnHandChanged?.Invoke();
        return true;
    }

    /// <summary>Called by CardView / drop zone when the player plays a card.</summary>
    /// <returns>True if the card was played and resources were spent.</returns>
    public bool RequestPlay(CardView view)
    {
        if (view == null || view.CardData == null) return false;
        int index = _handViews.IndexOf(view.gameObject);
        if (index >= 0)
            return TryPlayCard(index);
        return false;
    }

    // -----------------------------------------------------------------------
    // Effect application — handles ALL CardData.EffectType values
    // -----------------------------------------------------------------------

    private void ApplyEffect(CardData card)
    {
        var rm = ResourceManager.Instance;
        var dt = DataTracker.Instance;
        var em = EncounterManager.Instance;

        switch (card.effectType)
        {
            // === Resource effects ===
            case CardData.EffectType.GainPower:
                if (UsesAiResourceWallet) aiResourceWallet?.AddPower(card.effectValue);
                else rm?.AddPower(card.effectValue);
                break;
            case CardData.EffectType.GainBudget:
                if (UsesAiResourceWallet) aiResourceWallet?.AddBudget(card.effectValue);
                else rm?.AddBudget(card.effectValue);
                break;
            case CardData.EffectType.GainTime:
                if (UsesAiResourceWallet) aiResourceWallet?.AddTime(card.effectValue);
                else rm?.AddTime(card.effectValue);
                break;
            case CardData.EffectType.ReducePowerCosts:
                if (em != null) em.ReducePowerCostsThisTurn = true;
                break;

            // === Instrument effects — collect data ===
            case CardData.EffectType.CollectSurface:
                CollectDataWithBonuses(DataTracker.DataType.Surface, card.effectValue);
                break;
            case CardData.EffectType.CollectElemental:
                CollectDataWithBonuses(DataTracker.DataType.Elemental, card.effectValue);
                break;
            case CardData.EffectType.CollectMagnetic:
                CollectDataWithBonuses(DataTracker.DataType.Magnetic, card.effectValue);
                break;
            case CardData.EffectType.CollectGravity:
                CollectDataWithBonuses(DataTracker.DataType.Gravity, card.effectValue);
                break;
            case CardData.EffectType.CollectAllData:
                int allAmount = card.effectValue > 0 ? card.effectValue : 1;
                if (em != null && em.DoubleNextInstrument) { allAmount *= 2; em.DoubleNextInstrument = false; }
                if (em != null && em.BonusNextInstrument) { allAmount += 1; em.BonusNextInstrument = false; }
                dt?.AddAllData(allAmount);
                em?.AddProgress(allAmount);
                break;

            // === Maneuver effects ===
            case CardData.EffectType.AdjustOrbit:
                em?.AddProgress(card.effectValue);
                break;
            case CardData.EffectType.OrbitInsertion:
                em?.AddProgress(card.effectValue > 0 ? card.effectValue : 3);
                break;
            case CardData.EffectType.BonusNextInstrument:
                if (em != null) em.BonusNextInstrument = true;
                break;
            case CardData.EffectType.PreventPenalty:
                if (em != null) em.PreventNextPenalty = true;
                break;
            case CardData.EffectType.DoubleNextInstrument:
                if (em != null) em.DoubleNextInstrument = true;
                break;
            case CardData.EffectType.CancelCrisis:
                // TODO: integrate with crisis system when implemented
                Debug.Log("[DeckManager] Crisis cancelled (placeholder)");
                break;

            // === Analysis effects — convert data into conclusions ===
            case CardData.EffectType.CompositionConclusion:
                if (dt != null && dt.TryComposition())
                    em?.AddProgress(1);
                break;
            case CardData.EffectType.DynamoConclusion:
                if (dt != null && dt.TryDynamo())
                    em?.AddProgress(1);
                break;
            case CardData.EffectType.InteriorConclusion:
                if (dt != null && dt.TryInterior())
                    em?.AddProgress(1);
                break;
            case CardData.EffectType.FormationConclusion:
                if (dt != null && dt.TryFormation())
                    em?.AddProgress(1);
                break;
            case CardData.EffectType.WildConclusion:
                if (dt != null && dt.TryWildConclusion())
                    em?.AddProgress(1);
                break;
            case CardData.EffectType.UpgradeConclusion:
                dt?.TryUpgradeConclusion();
                break;

            // === Utility ===
            case CardData.EffectType.DrawCards:
                int drawCount = card.effectValue > 0 ? card.effectValue : card.effectValue2;
                if (drawCount > 0) Draw(drawCount);
                break;

            // === Crisis / Negative Events ===
            case CardData.EffectType.LosePower:
                if (rm != null) rm.AddPower(-card.effectValue);
                break;
            case CardData.EffectType.LoseBudget:
                if (rm != null) rm.AddBudget(-card.effectValue);
                break;
            case CardData.EffectType.LoseTime:
                if (rm != null) rm.AddTime(-card.effectValue);
                break;
            case CardData.EffectType.LoseProgress:
                if (em != null) em.AddProgress(-card.effectValue);
                break;
            case CardData.EffectType.SkipTurn:
                if (em != null) em.AdvanceTurn();
                break;
            case CardData.EffectType.DiscardRandom:
                DiscardRandomCards(card.effectValue);
                break;

            case CardData.EffectType.None:
            default:
                break;
        }
    }

    private void DiscardRandomCards(int count)
    {
        for (int i = 0; i < count && _hand.Count > 0; i++)
        {
            int idx = UnityEngine.Random.Range(0, _hand.Count);
            var discarded = _hand[idx];
            _hand.RemoveAt(idx);
            RemoveCardViewAt(idx);
            _discard.Add(discarded);
            Debug.Log($"[DeckManager] Discarded: {discarded.cardName}");
        }
        RefreshHandLayout();
        OnHandChanged?.Invoke();
    }

    /// <summary>Collects data with bonus/double modifiers from maneuver cards.</summary>
    private void CollectDataWithBonuses(DataTracker.DataType type, int baseAmount)
    {
        var em = EncounterManager.Instance;
        var dt = DataTracker.Instance;

        int amount = baseAmount;
        if (em != null && em.DoubleNextInstrument)
        {
            amount *= 2;
            em.DoubleNextInstrument = false;
        }
        if (em != null && em.BonusNextInstrument)
        {
            amount += 1;
            em.BonusNextInstrument = false;
        }

        dt?.AddData(type, amount);
        em?.AddProgress(amount);
    }

    // -----------------------------------------------------------------------
    // Card view management
    // -----------------------------------------------------------------------

    private void SpawnCardView(CardData data)
    {
        if (cardPrefab == null || handParent == null) return;
        var go = Instantiate(cardPrefab, handParent);
        var view = go.GetComponent<CardView>();
        if (view != null)
            view.Bind(data, this);
        _handViews.Add(go);
    }

    private void RemoveCardViewAt(int index)
    {
        if (index >= 0 && index < _handViews.Count)
        {
            var go = _handViews[index];
            _handViews.RemoveAt(index);
            if (go != null) Destroy(go);
        }
    }

    private void RefreshHandLayout()
    {
        if (cardHandLayout != null)
        {
            cardHandLayout.CollectExistingCards();
            cardHandLayout.RefreshLayout();
        }
    }

    // -----------------------------------------------------------------------
    // Deck utilities
    // -----------------------------------------------------------------------

    private void ShuffleDeck()
    {
        for (int i = _deck.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            var t = _deck[i];
            _deck[i] = _deck[j];
            _deck[j] = t;
        }
    }

    private void SetupTestDeck()
    {
        _deck.Clear();
        for (int i = 0; i < 4; i++)
            _deck.Add(CreateRuntimeCard("Solar Array Deploy", "Gain 3 Power", 0, 0, 0, CardData.EffectType.GainPower, 3, CardData.CardCategory.Resource, "CardArt/solar array deploy"));
        for (int i = 0; i < 2; i++)
            _deck.Add(CreateRuntimeCard("Budget Request", "Gain 3 Budget", 0, 0, 1, CardData.EffectType.GainBudget, 3, CardData.CardCategory.Resource, "CardArt/budget request card"));
        for (int i = 0; i < 2; i++)
            _deck.Add(CreateRuntimeCard("Multispectral Imager", "Collect 2 Surface data", 2, 0, 1, CardData.EffectType.CollectSurface, 2, CardData.CardCategory.Instrument, "CardArt/multispectral imager"));
        _deck.Add(CreateRuntimeCard("Trajectory Correction", "Adjust orbital phase", 2, 1, 0, CardData.EffectType.AdjustOrbit, 1, CardData.CardCategory.Maneuver, "CardArt/Trajectory Correction"));
        _deck.Add(CreateRuntimeCard("Compositional Analysis", "3 Elemental + 2 Surface → Composition", 0, 1, 2, CardData.EffectType.CompositionConclusion, 0, CardData.CardCategory.Analysis, "CardArt/Compositional Analysis"));
    }

    private static CardData CreateRuntimeCard(string name, string desc, int p, int b, int t,
        CardData.EffectType effect, int value, CardData.CardCategory category = CardData.CardCategory.Resource,
        string artResourcePath = null)
    {
        var card = ScriptableObject.CreateInstance<CardData>();
        card.cardName = name;
        card.description = desc;
        card.costPower = p;
        card.costBudget = b;
        card.costTime = t;
        card.effectType = effect;
        card.effectValue = value;
        card.category = category;

        if (!string.IsNullOrEmpty(artResourcePath))
        {
            var sprite = Resources.Load<Sprite>(artResourcePath);
            if (sprite == null)
            {
                var tex = Resources.Load<Texture2D>(artResourcePath);
                if (tex != null)
                    sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
            card.cardArt = sprite;
        }

        return card;
    }

    /// <summary>Permanently adds a card to the player's deck (goes into the discard pile so it's available next reshuffle).</summary>
    public void AddCardToDeck(CardData card)
    {
        if (card == null) return;
        _discard.Add(card);
        Debug.Log($"[DeckManager] Added '{card.cardName}' to deck (discard pile). Deck={_deck.Count} Discard={_discard.Count}");
    }

    /// <summary>Creates one random card from the template pool.</summary>
    public static CardData CreateRandomRuntimeCard()
    {
        int idx = UnityEngine.Random.Range(0, RandomCardTemplates.Length);
        var tmpl = RandomCardTemplates[idx];
        return CreateRuntimeCard(tmpl.name, tmpl.desc, tmpl.p, tmpl.b, tmpl.t, tmpl.effect, tmpl.value, tmpl.cat, tmpl.art);
    }

    // -----------------------------------------------------------------------
    // UI Feedback
    // -----------------------------------------------------------------------

    /// <summary>Shows a feedback message on screen. Clears after 2 seconds.</summary>
    private void ShowFeedback(string message)
    {
        var textComponent = GetOrCreateFeedbackText();
        if (textComponent != null)
        {
            SetFeedbackText(textComponent, message);
            StopAllCoroutines();
            StartCoroutine(ClearFeedbackAfterSeconds(textComponent, 2f));
        }
    }

    private void SetFeedbackText(Component textComponent, string text)
    {
        if (textComponent is Text legacyText)
            legacyText.text = text;
        else if (textComponent is TMP_Text tmpText)
            tmpText.text = text;
    }

    private Component GetOrCreateFeedbackText()
    {
        if (feedbackMessageTarget != null)
        {
            var text = feedbackMessageTarget.GetComponent<Text>();
            if (text != null) return text;
            var tmp = feedbackMessageTarget.GetComponent<TMP_Text>();
            if (tmp != null) return tmp;
        }

        var canvasGo = GameObject.Find("Psyche_AutoCanvas");
        if (canvasGo == null)
            canvasGo = GameObject.Find("GameCanvas");
        if (canvasGo == null) return null;

        var existing = canvasGo.transform.Find("FeedbackMessage");
        if (existing != null)
        {
            var t = existing.GetComponent<Text>();
            if (t != null) return t;
            var tmp = existing.GetComponent<TMP_Text>();
            if (tmp != null) return tmp;
        }

        var child = new GameObject("FeedbackMessage");
        child.transform.SetParent(canvasGo.transform, false);
        var rect = child.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 80f);
        rect.sizeDelta = new Vector2(500f, 60f);

        var feedbackText = child.AddComponent<Text>();
        feedbackText.text = "";
        feedbackText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        feedbackText.fontSize = 26;
        feedbackText.alignment = TextAnchor.MiddleCenter;
        feedbackText.color = new Color(0.9f, 0.85f, 0.2f, 1f); // Gold for positive feedback
        return feedbackText;
    }

    private IEnumerator ClearFeedbackAfterSeconds(Component textComponent, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        SetFeedbackText(textComponent, "");
    }
}
