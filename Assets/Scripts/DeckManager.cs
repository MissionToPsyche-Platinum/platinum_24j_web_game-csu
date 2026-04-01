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
    [Tooltip("Optional override: shuffle these into the deck at run start. If empty (player deck), uses design doc §8 — 10 cards (4× Solar Array Deploy, 2× Budget Request, 2× Multispectral Imager, 1× Trajectory Correction, 1× Compositional Analysis). AI deck still uses random cards when empty.")]
    public List<CardData> startingDeck = new List<CardData>();

    [Header("Opening hand")]
    [Tooltip("Cards drawn after the deck is shuffled at game start (design doc: draw up to 5).")]
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

    /// <summary>Full card pool — design doc roster (rarity ignored). Tuple ends with effectValue2 (e.g. DrawCards count).</summary>
    private static readonly (string name, string desc, int p, int b, int t, CardData.EffectType effect, int value, int value2, CardData.CardCategory cat, string art)[] AllCardTemplates =
    {
        // ── Resource ──
        ("Solar Array Deploy", "Gain 3 Power", 0, 0, 0, CardData.EffectType.GainPower, 3, 0, CardData.CardCategory.Resource, "CardArt/solar array deploy"),
        ("Budget Request", "Gain 3 Budget", 0, 0, 1, CardData.EffectType.GainBudget, 3, 0, CardData.CardCategory.Resource, "CardArt/budget request card"),
        ("Mission Extension", "Gain 5 Time", 0, 3, 0, CardData.EffectType.GainTime, 5, 0, CardData.CardCategory.Resource, "CardArt/mission extension"),
        ("Nuclear Battery", "+2 Power at the start of each turn", 0, 2, 0, CardData.EffectType.AddTurnStartPower, 2, 0, CardData.CardCategory.Resource, "CardArt/nuclear battery"),
        ("Power Conservation", "All Power costs reduced by 1 this turn", 0, 0, 1, CardData.EffectType.ReducePowerCosts, 1, 0, CardData.CardCategory.Resource, "CardArt/Power Conservation"),
        ("Emergency Fund", "Gain 2 Budget", 0, 0, 0, CardData.EffectType.GainBudget, 2, 0, CardData.CardCategory.Resource, "CardArt/emergency fund"),

        // ── Instrument ──
        ("Multispectral Imager", "Collect 2 Surface data", 2, 0, 1, CardData.EffectType.CollectSurface, 2, 0, CardData.CardCategory.Instrument, "CardArt/multispectral imager"),
        ("Gamma-Ray Spectrometer", "Collect 3 Elemental data", 3, 0, 2, CardData.EffectType.CollectElemental, 3, 0, CardData.CardCategory.Instrument, "CardArt/Gamma-Ray & Neutron Spectrometer"),
        ("Magnetometer", "Collect 2 Magnetic data", 2, 0, 2, CardData.EffectType.CollectMagnetic, 2, 0, CardData.CardCategory.Instrument, "CardArt/Magnetometer"),
        ("X-band Radio", "Collect 3 Gravity data", 1, 0, 3, CardData.EffectType.CollectGravity, 3, 0, CardData.CardCategory.Instrument, "CardArt/X-band Radio"),
        ("Deep Space Optical Comms", "Draw 2 cards", 3, 0, 1, CardData.EffectType.DrawCards, 2, 0, CardData.CardCategory.Instrument, "CardArt/Deep Space Optical Comms"),
        ("Multi-Instrument Suite", "Collect 1 of each data type", 4, 0, 2, CardData.EffectType.CollectAllData, 1, 0, CardData.CardCategory.Instrument, "CardArt/Multi-Instrument Suite"),

        // ── Maneuver ──
        ("Trajectory Correction", "Adjust orbital phase (+1 progress)", 0, 1, 0, CardData.EffectType.AdjustOrbit, 1, 0, CardData.CardCategory.Maneuver, "CardArt/Trajectory Correction"),
        ("Orbit Insertion Burn", "Enter stable orbit (+4 progress)", 5, 3, 0, CardData.EffectType.OrbitInsertion, 4, 0, CardData.CardCategory.Maneuver, "CardArt/Orbit Insertion Burn"),
        ("Altitude Adjustment", "Next Instrument collects +1 data", 1, 1, 0, CardData.EffectType.BonusNextInstrument, 1, 0, CardData.CardCategory.Maneuver, "CardArt/Altitude Adjustment"),
        ("Reaction Wheel Reset", "Prevent penalty on next Instrument card", 1, 0, 0, CardData.EffectType.PreventPenalty, 1, 0, CardData.CardCategory.Maneuver, "CardArt/Reaction Wheel Reset"),
        ("Close Approach Flyby", "Next Instrument collects double data", 2, 1, 0, CardData.EffectType.DoubleNextInstrument, 1, 0, CardData.CardCategory.Maneuver, "CardArt/Close Approach Flyby"),
        ("Safe Mode Recovery", "Clear all active crisis effects", 2, 0, 2, CardData.EffectType.CancelCrisis, 1, 0, CardData.CardCategory.Maneuver, "CardArt/Safe Mode Recovery"),

        // ── Analysis ──
        ("Compositional Analysis", "Convert 3 Elemental + 2 Surface → Composition", 0, 1, 2, CardData.EffectType.CompositionConclusion, 0, 0, CardData.CardCategory.Analysis, "CardArt/Compositional Analysis"),
        ("Magnetic Modeling", "Convert 4 Magnetic → Dynamo Conclusion", 0, 2, 3, CardData.EffectType.DynamoConclusion, 0, 0, CardData.CardCategory.Analysis, "CardArt/Magnetic Modeling"),
        ("Structural Study", "Convert 3 Gravity + 2 Surface → Interior", 0, 1, 2, CardData.EffectType.InteriorConclusion, 0, 0, CardData.CardCategory.Analysis, "CardArt/Structural Study"),
        ("Thermal Reconstruction", "Convert 2 of each data type → Formation", 0, 2, 4, CardData.EffectType.FormationConclusion, 0, 0, CardData.CardCategory.Analysis, "CardArt/Thermal Reconstruction"),
        ("Comparative Planetology", "Convert any 5 data → a Conclusion", 0, 2, 2, CardData.EffectType.WildConclusion, 0, 0, CardData.CardCategory.Analysis, "CardArt/Comparative Planetology"),
        ("Peer Review Publication", "Upgrade 1 Conclusion to count as 2", 0, 3, 3, CardData.EffectType.UpgradeConclusion, 0, 0, CardData.CardCategory.Analysis, "CardArt/Peer Review Publication"),

        // ── Crisis (penalty + resolve costs — see EncounterManager.TryResolve*) ──
        ("Solar Storm Warning", "Penalty: −2 Time/turn (stacks). Resolve: 3 Power, or Safe Mode (clear all).", 0, 0, 0, CardData.EffectType.CrisisSolarStorm, 2, 0, CardData.CardCategory.Crisis, "CardArt/Solar Storm Warning"),
        ("Thruster Anomaly", "Penalty: Maneuvers +1 Power. Resolve: 2 Budget + 2 Time.", 0, 0, 0, CardData.EffectType.CrisisThrusterTax, 1, 0, CardData.CardCategory.Crisis, "CardArt/Thruster Anomaly"),
        ("Ground Station Conflict", "Penalty: next draw skipped. Resolve: 2 Budget.", 0, 0, 0, CardData.EffectType.CrisisBlockDrawOnce, 1, 0, CardData.CardCategory.Crisis, "CardArt/Ground Station Conflict"),
        ("Data Storage Full", "Penalty: cannot collect data. Resolve: 2 Time (downlink).", 0, 0, 0, CardData.EffectType.CrisisBlockDataCollection, 1, 0, CardData.CardCategory.Crisis, "CardArt/Data Storage Full"),
        ("Debris Field Detected", "Penalty: maneuvers blocked. Resolve: 3 Power + 1 Budget.", 0, 0, 0, CardData.EffectType.CrisisBlockNextManeuver, 1, 0, CardData.CardCategory.Crisis, "CardArt/Debris Field Detected"),
        ("Computer Reboot Required", "Penalty: lose next turn's draw. Resolve: 4 Power (no turn lost).", 0, 0, 0, CardData.EffectType.CrisisComputerReboot, 1, 0, CardData.CardCategory.Crisis, "CardArt/Computer Reboot Required"),
        ("Budget Cut Notice", "Penalty: −3 Budget now. Resolve: 3 Time → +2 Budget.", 0, 0, 0, CardData.EffectType.CrisisBudgetCut, 3, 2, CardData.CardCategory.Crisis, "CardArt/Budget Cut Notice"),
    };

    /// <summary>Non-crisis card templates for random deck building and rewards.</summary>
    private static readonly (string name, string desc, int p, int b, int t, CardData.EffectType effect, int value, int value2, CardData.CardCategory cat, string art)[] RandomCardTemplates =
        System.Array.FindAll(AllCardTemplates, c => c.cat != CardData.CardCategory.Crisis);

    /// <summary>Design document §8 — default player starting deck (10 cards), built from <see cref="AllCardTemplates"/>.</summary>
    private static readonly string[] DefaultStartingDeckCardNames =
    {
        "Solar Array Deploy", "Solar Array Deploy", "Solar Array Deploy", "Solar Array Deploy",
        "Budget Request", "Budget Request", "Budget Request",
        "Multispectral Imager", "Multispectral Imager", "Multispectral Imager"
    };

    // --- Public properties ---
    public IReadOnlyList<CardData> Hand => _hand;
    public int DeckCount => _deck.Count;
    public int DiscardCount => _discard.Count;

    /// <summary>
    /// Draw order: <see cref="Draw"/> pops from the end of <c>_deck</c>, so index <c>Count-1</c> is drawn next.
    /// This list is ordered next-draw-first (left-to-right in the deck browser UI).
    /// </summary>
    public List<CardData> GetDrawPileOrderedNextDrawFirst()
    {
        var list = new List<CardData>(_deck.Count);
        for (int i = _deck.Count - 1; i >= 0; i--)
            list.Add(_deck[i]);
        return list;
    }

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

        bool hasExplicitStartingCards = false;
        if (startingDeck != null)
        {
            foreach (var c in startingDeck)
            {
                if (c != null) { hasExplicitStartingCards = true; break; }
            }
        }

        if (hasExplicitStartingCards)
        {
            _deck.Clear();
            foreach (var c in startingDeck)
                if (c != null) _deck.Add(c);
            ShuffleDeck();
            Draw(Mathf.Min(drawPhaseCardCount, _deck.Count));
        }
        else if (UsesAiResourceWallet)
        {
            // Opponent: no scripted starter — random non-crisis pool
            _deck.Clear();
            int count = Mathf.Clamp(drawPhaseCardCount, 1, 20);
            for (int i = 0; i < count; i++)
                _deck.Add(CreateRandomRuntimeCard());
            ShuffleDeck();
            Draw(count);
        }
        else
        {
            BuildDesignDocStartingDeck();
            ShuffleDeck();
            Draw(Mathf.Min(drawPhaseCardCount, _deck.Count));
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
        if (count <= 0)
            return;

        if (EncounterManager.Instance != null && EncounterManager.Instance.ConsumeSkipDrawOnce())
        {
            Debug.Log("[DeckManager] Draw phase skipped (Ground Station crisis).");
            OnHandChanged?.Invoke();
            return;
        }

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

    /// <summary>Moves all remaining hand cards to the discard pile (end of turn).</summary>
    public void DiscardHand()
    {
        for (int i = _hand.Count - 1; i >= 0; i--)
        {
            _discard.Add(_hand[i]);
            _hand.RemoveAt(i);
            RemoveCardViewAt(i);
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

        var em0 = EncounterManager.Instance;
        if (card.category == CardData.CardCategory.Instrument && em0 != null && em0.BlockInstrumentData)
        {
            ShowFeedback("Data Storage Full — cannot collect data.");
            return false;
        }

        if (card.category == CardData.CardCategory.Maneuver && em0 != null && em0.BlockNextManeuverPlay)
        {
            ShowFeedback(em0.DebrisFieldCrisisActive
                ? "Debris field — maneuvers blocked until you pay 3 Power & 1 Budget."
                : "Maneuver blocked!");
            return false;
        }

        if (!CanPlayCard(card))
        {
            return false;
        }

        // Adjust power cost if ReducePowerCosts is active this turn
        int effectivePowerCost = card.costPower;
        if (em0 != null && em0.ReducePowerCostsThisTurn && effectivePowerCost > 0)
            effectivePowerCost = Mathf.Max(0, effectivePowerCost - 1);
        if (em0 != null && card.category == CardData.CardCategory.Maneuver && em0.ExtraManeuverPowerCost > 0)
            effectivePowerCost += em0.ExtraManeuverPowerCost;

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

        if (EncounterManager.Instance != null && card.category == CardData.CardCategory.Maneuver)
            EncounterManager.Instance.NotifyManeuverPlayedSuccessfully();

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

    /// <summary>Checks if the card's requirements (e.g. required data for analysis) are met.</summary>
    private bool CanPlayCard(CardData card)
    {
        if (UsesAiResourceWallet) return true; // AI ignores requirements

        var dt = DataTracker.Instance;
        
        switch (card.effectType)
        {
            case CardData.EffectType.CompositionConclusion:
                if (dt == null || dt.Elemental < 3 || dt.Surface < 2)
                {
                    ShowFeedback("Need 3 Elemental & 2 Surface data.");
                    return false;
                }
                break;
            case CardData.EffectType.DynamoConclusion:
                if (dt == null || dt.Magnetic < 4)
                {
                    ShowFeedback("Need 4 Magnetic data.");
                    return false;
                }
                break;
            case CardData.EffectType.InteriorConclusion:
                if (dt == null || dt.Gravity < 3 || dt.Surface < 2)
                {
                    ShowFeedback("Need 3 Gravity & 2 Surface data.");
                    return false;
                }
                break;
            case CardData.EffectType.FormationConclusion:
                if (dt == null || dt.Surface < 2 || dt.Elemental < 2 || dt.Magnetic < 2 || dt.Gravity < 2 || dt.Thermal < 2)
                {
                    ShowFeedback("Need 2 of each data type.");
                    return false;
                }
                break;
            case CardData.EffectType.WildConclusion:
                if (dt == null || dt.TotalData < 5)
                {
                    ShowFeedback("Need any 5 data total.");
                    return false;
                }
                break;
            case CardData.EffectType.UpgradeConclusion:
                if (dt == null || dt.TotalConclusions <= 0)
                {
                    ShowFeedback("No conclusions to upgrade.");
                    return false;
                }
                break;
        }
        
        return true;
    }

    // -----------------------------------------------------------------------
    // Effect application — handles ALL CardData.EffectType values
    // -----------------------------------------------------------------------

    private void ApplyEffect(CardData card)
    {
        var rm = ResourceManager.Instance;
        var dt = DataTracker.Instance;
        var em = EncounterManager.Instance;

        // AI decks only affect their own resource wallet — skip encounter progress and data tracker.
        bool isAiPlay = UsesAiResourceWallet;

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
                if (!isAiPlay && em != null) em.ReducePowerCostsThisTurn = true;
                break;
            case CardData.EffectType.AddTurnStartPower:
                if (!isAiPlay) em?.AddTurnStartPowerBonus(card.effectValue);
                break;

            // === Instrument effects — collect data (AI skips — no shared progress) ===
            case CardData.EffectType.CollectSurface:
                if (!isAiPlay) CollectDataWithBonuses(DataTracker.DataType.Surface, card.effectValue);
                break;
            case CardData.EffectType.CollectElemental:
                if (!isAiPlay) CollectDataWithBonuses(DataTracker.DataType.Elemental, card.effectValue);
                break;
            case CardData.EffectType.CollectMagnetic:
                if (!isAiPlay) CollectDataWithBonuses(DataTracker.DataType.Magnetic, card.effectValue);
                break;
            case CardData.EffectType.CollectGravity:
                if (!isAiPlay) CollectDataWithBonuses(DataTracker.DataType.Gravity, card.effectValue);
                break;
            case CardData.EffectType.CollectAllData:
                if (!isAiPlay)
                {
                    int allAmount = card.effectValue > 0 ? card.effectValue : 1;
                    if (em != null && em.DoubleNextInstrument) { allAmount *= 2; em.DoubleNextInstrument = false; }
                    if (em != null && em.BonusNextInstrument) { allAmount += 1; em.BonusNextInstrument = false; }
                    dt?.AddAllData(allAmount);
                    em?.AddProgress(allAmount);
                }
                break;

            // === Maneuver effects (AI skips encounter state) ===
            case CardData.EffectType.AdjustOrbit:
                if (!isAiPlay) em?.AddProgress(card.effectValue);
                break;
            case CardData.EffectType.OrbitInsertion:
                if (!isAiPlay)
                {
                    em?.AddProgress(card.effectValue > 0 ? card.effectValue : 3);
                    em?.SetStableOrbitAchieved();
                }
                break;
            case CardData.EffectType.BonusNextInstrument:
                if (!isAiPlay && em != null) em.BonusNextInstrument = true;
                break;
            case CardData.EffectType.PreventPenalty:
                if (!isAiPlay && em != null) em.PreventNextPenalty = true;
                break;
            case CardData.EffectType.DoubleNextInstrument:
                if (!isAiPlay && em != null) em.DoubleNextInstrument = true;
                break;
            case CardData.EffectType.CancelCrisis:
                if (!isAiPlay) em?.ClearAllCrisisEffects();
                break;

            // === Analysis effects (AI skips — no shared data/progress) ===
            case CardData.EffectType.CompositionConclusion:
                if (!isAiPlay && dt != null && dt.TryComposition())
                    em?.AddProgress(1);
                break;
            case CardData.EffectType.DynamoConclusion:
                if (!isAiPlay && dt != null && dt.TryDynamo())
                    em?.AddProgress(1);
                break;
            case CardData.EffectType.InteriorConclusion:
                if (!isAiPlay && dt != null && dt.TryInterior())
                    em?.AddProgress(1);
                break;
            case CardData.EffectType.FormationConclusion:
                if (!isAiPlay && dt != null && dt.TryFormation())
                    em?.AddProgress(1);
                break;
            case CardData.EffectType.WildConclusion:
                if (!isAiPlay && dt != null && dt.TryWildConclusion())
                    em?.AddProgress(1);
                break;
            case CardData.EffectType.UpgradeConclusion:
                if (!isAiPlay) dt?.TryUpgradeConclusion();
                break;

            // === Utility ===
            case CardData.EffectType.DrawCards:
                int drawCount = card.effectValue > 0 ? card.effectValue : card.effectValue2;
                if (drawCount > 0) Draw(drawCount);
                break;

            // === Crisis / Negative Events (AI skips — these target the player) ===
            case CardData.EffectType.LosePower:
                if (!isAiPlay && rm != null) rm.AddPower(-card.effectValue);
                break;
            case CardData.EffectType.LoseBudget:
                if (!isAiPlay && rm != null) rm.AddBudget(-card.effectValue);
                break;
            case CardData.EffectType.LoseTime:
                if (!isAiPlay && rm != null) rm.AddTime(-card.effectValue);
                break;
            case CardData.EffectType.LoseProgress:
                if (!isAiPlay && em != null) em.AddProgress(-card.effectValue);
                break;
            case CardData.EffectType.SkipTurn:
                if (!isAiPlay && em != null) em.AdvanceTurn();
                break;
            case CardData.EffectType.DiscardRandom:
                if (!isAiPlay) DiscardRandomCards(card.effectValue);
                break;

            case CardData.EffectType.CrisisSolarStorm:
                if (!isAiPlay) em?.AddSolarStormExtraTimeDrain(card.effectValue);
                break;
            case CardData.EffectType.CrisisThrusterTax:
                if (!isAiPlay) em?.ActivateThrusterAnomaly(card.effectValue > 0 ? card.effectValue : 1);
                break;
            case CardData.EffectType.CrisisBlockDrawOnce:
                if (!isAiPlay) em?.ActivateGroundStationConflict();
                break;
            case CardData.EffectType.CrisisBlockDataCollection:
                if (!isAiPlay) em?.ActivateDataStorageFull();
                break;
            case CardData.EffectType.CrisisBlockNextManeuver:
                if (!isAiPlay) em?.ActivateDebrisField();
                break;
            case CardData.EffectType.CrisisComputerReboot:
                if (!isAiPlay) em?.ActivateComputerRebootCrisis();
                break;
            case CardData.EffectType.CrisisBudgetCut:
                if (!isAiPlay) em?.ActivateBudgetCut(
                    card.effectValue > 0 ? card.effectValue : 3,
                    card.effectValue2 > 0 ? card.effectValue2 : 2);
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

    /// <summary>§8 Starting Deck — 10 runtime <see cref="CardData"/> instances from <see cref="AllCardTemplates"/>.</summary>
    private void BuildDesignDocStartingDeck()
    {
        _deck.Clear();
        foreach (var cardName in DefaultStartingDeckCardNames)
        {
            if (!TryGetCardTemplateByName(cardName, out var t))
            {
                Debug.LogError($"[DeckManager] Starting deck references unknown card '{cardName}'.");
                continue;
            }
            _deck.Add(CreateRuntimeCard(t.name, t.desc, t.p, t.b, t.t, t.effect, t.value, t.cat, t.art, t.value2));
        }
    }

    private static bool TryGetCardTemplateByName(string cardName,
        out (string name, string desc, int p, int b, int t, CardData.EffectType effect, int value, int value2, CardData.CardCategory cat, string art) tmpl)
    {
        for (int i = 0; i < AllCardTemplates.Length; i++)
        {
            if (AllCardTemplates[i].name == cardName)
            {
                tmpl = AllCardTemplates[i];
                return true;
            }
        }
        tmpl = default;
        return false;
    }

    private static CardData CreateRuntimeCard(string name, string desc, int p, int b, int t,
        CardData.EffectType effect, int value, CardData.CardCategory category = CardData.CardCategory.Resource,
        string artResourcePath = null, int effectValue2 = 0)
    {
        var card = ScriptableObject.CreateInstance<CardData>();
        card.cardName = name;
        card.description = desc;
        card.costPower = p;
        card.costBudget = b;
        card.costTime = t;
        card.effectType = effect;
        card.effectValue = value;
        card.effectValue2 = effectValue2;
        card.category = category;
        card.type = (CardData.CardType)(int)category;

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
        OnHandChanged?.Invoke();
    }

    /// <summary>Creates one random card from the template pool.</summary>
    public static CardData CreateRandomRuntimeCard()
    {
        int idx = UnityEngine.Random.Range(0, RandomCardTemplates.Length);
        var tmpl = RandomCardTemplates[idx];
        return CreateRuntimeCard(tmpl.name, tmpl.desc, tmpl.p, tmpl.b, tmpl.t, tmpl.effect, tmpl.value, tmpl.cat, tmpl.art, tmpl.value2);
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
        feedbackText.font = UiFontHelper.KenneyFutureOrFallback();
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
