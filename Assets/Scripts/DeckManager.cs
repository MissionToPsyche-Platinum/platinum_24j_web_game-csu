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

    private readonly List<CardData> _deck = new List<CardData>();
    private readonly List<CardData> _hand = new List<CardData>();
    private readonly List<CardData> _discard = new List<CardData>();
    private readonly List<GameObject> _handViews = new List<GameObject>();

    /// <summary>Card templates for random Draw Phase, matching the design doc starting deck.</summary>
    private static readonly (string name, string desc, int p, int b, int t, CardData.EffectType effect, int value, CardData.CardCategory cat)[] RandomCardTemplates =
    {
        ("Solar Array Deploy",      "Gain 3 Power",             0, 0, 0, CardData.EffectType.GainPower,         3, CardData.CardCategory.Resource),
        ("Budget Request",          "Gain 3 Budget",            0, 0, 1, CardData.EffectType.GainBudget,        3, CardData.CardCategory.Resource),
        ("Mission Extension",       "Gain 5 Time",              0, 3, 0, CardData.EffectType.GainTime,          5, CardData.CardCategory.Resource),
        ("Multispectral Imager",    "Collect 2 Surface data",   2, 0, 1, CardData.EffectType.CollectSurface,    2, CardData.CardCategory.Instrument),
        ("Gamma-Ray Spectrometer",  "Collect 3 Elemental data", 3, 0, 2, CardData.EffectType.CollectElemental,  3, CardData.CardCategory.Instrument),
        ("Magnetometer",            "Collect 2 Magnetic data",  2, 0, 2, CardData.EffectType.CollectMagnetic,   2, CardData.CardCategory.Instrument),
        ("X-band Radio",            "Collect 3 Gravity data",   1, 0, 3, CardData.EffectType.CollectGravity,    3, CardData.CardCategory.Instrument),
        ("Trajectory Correction",   "Adjust orbital phase",     2, 1, 0, CardData.EffectType.AdjustOrbit,       1, CardData.CardCategory.Maneuver),
        ("Compositional Analysis",  "3 Elemental + 2 Surface → Composition", 0, 1, 2, CardData.EffectType.CompositionConclusion, 0, CardData.CardCategory.Analysis),
        ("Structural Study",        "3 Gravity + 2 Surface → Interior",      0, 1, 2, CardData.EffectType.InteriorConclusion,    0, CardData.CardCategory.Analysis),
    };

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
            var hand = GameObject.Find("Hand_Zone");
            if (hand != null) handParent = hand.transform;
            else handParent = transform;
        }
        if (!handParent.gameObject.activeSelf)
            handParent.gameObject.SetActive(true);

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
        if (ResourceManager.Instance == null) return false;

        // Adjust power cost if ReducePowerCosts is active this turn
        int effectivePowerCost = card.costPower;
        if (EncounterManager.Instance != null && EncounterManager.Instance.ReducePowerCostsThisTurn && effectivePowerCost > 0)
            effectivePowerCost = Mathf.Max(0, effectivePowerCost - 1);

        if (!ResourceManager.Instance.CanAfford(effectivePowerCost, card.costBudget, card.costTime))
        {
            ShowFeedback("Not enough resources!");
            return false;
        }

        // Spend resources
        ResourceManager.Instance.TrySpend(effectivePowerCost, card.costBudget, card.costTime);

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

    /// <summary>Called by CardView when player clicks to play.</summary>
    public void RequestPlay(CardView view)
    {
        if (view == null || view.CardData == null) return;
        int index = _handViews.IndexOf(view.gameObject);
        if (index >= 0)
            TryPlayCard(index);
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
                rm?.AddPower(card.effectValue);
                break;
            case CardData.EffectType.GainBudget:
                rm?.AddBudget(card.effectValue);
                break;
            case CardData.EffectType.GainTime:
                rm?.AddTime(card.effectValue);
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

            case CardData.EffectType.None:
            default:
                break;
        }
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
            _deck.Add(CreateRuntimeCard("Solar Array Deploy", "Gain 3 Power", 0, 0, 0, CardData.EffectType.GainPower, 3, CardData.CardCategory.Resource));
        for (int i = 0; i < 2; i++)
            _deck.Add(CreateRuntimeCard("Budget Request", "Gain 3 Budget", 0, 0, 1, CardData.EffectType.GainBudget, 3, CardData.CardCategory.Resource));
        for (int i = 0; i < 2; i++)
            _deck.Add(CreateRuntimeCard("Multispectral Imager", "Collect 2 Surface data", 2, 0, 1, CardData.EffectType.CollectSurface, 2, CardData.CardCategory.Instrument));
        _deck.Add(CreateRuntimeCard("Trajectory Correction", "Adjust orbital phase", 2, 1, 0, CardData.EffectType.AdjustOrbit, 1, CardData.CardCategory.Maneuver));
        _deck.Add(CreateRuntimeCard("Compositional Analysis", "3 Elemental + 2 Surface → Composition", 0, 1, 2, CardData.EffectType.CompositionConclusion, 0, CardData.CardCategory.Analysis));
    }

    private static CardData CreateRuntimeCard(string name, string desc, int p, int b, int t,
        CardData.EffectType effect, int value, CardData.CardCategory category = CardData.CardCategory.Resource)
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
        return card;
    }

    /// <summary>Creates one random card from the template pool.</summary>
    private static CardData CreateRandomRuntimeCard()
    {
        int idx = UnityEngine.Random.Range(0, RandomCardTemplates.Length);
        var tmpl = RandomCardTemplates[idx];
        return CreateRuntimeCard(tmpl.name, tmpl.desc, tmpl.p, tmpl.b, tmpl.t, tmpl.effect, tmpl.value, tmpl.cat);
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
