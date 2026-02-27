using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages deck, hand, and discard. Draw phase: generates 5 random cards via CreateInstance&lt;CardData&gt;() and places them in the hand (CardHandLayout).
/// HUD (Power, Budget, Time) updates automatically when a card is played via ResourceManager.OnResourcesChanged.
/// Budget is validated before play; "Not enough budget" is shown on Psyche_AutoCanvas when budget is too low.
/// Assign handParent (e.g. Hand_Zone or CardHandLayout's cardsParent), cardPrefab, and optionally cardHandLayout in the Inspector.
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
    [Tooltip("Optional: assign a Text or TMP_Text to show 'Not enough budget'. If empty, will find or create one under Psyche_AutoCanvas.")]
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

    /// <summary>Card templates for random Draw Phase. Each is (name, description, costP, costB, costT, effectType, effectValue).</summary>
    private static readonly (string name, string desc, int p, int b, int t, CardData.EffectType effect, int value)[] RandomCardTemplates =
    {
        ("Solar Array Deploy", "Gain 3 Power", 0, 0, 0, CardData.EffectType.GainPower, 3),
        ("Budget Request", "Gain 3 Budget", 0, 0, 1, CardData.EffectType.GainBudget, 3),
        ("Time Extension", "Gain 2 Time", 1, 0, 0, CardData.EffectType.GainTime, 2),
        ("Multispectral Imager", "Collect Surface data", 2, 0, 1, CardData.EffectType.None, 0),
        ("Trajectory Correction", "Adjust orbital phase", 2, 1, 0, CardData.EffectType.None, 0),
        ("Compositional Analysis", "Convert data to Conclusion", 0, 1, 2, CardData.EffectType.None, 0),
        ("Power Relay", "Gain 2 Power", 0, 1, 0, CardData.EffectType.GainPower, 2),
        ("Emergency Budget", "Gain 2 Budget", 1, 0, 1, CardData.EffectType.GainBudget, 2),
    };

    public IReadOnlyList<CardData> Hand => _hand;

    public event Action OnHandChanged;

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
            // Draw Phase: generate N random cards with CreateInstance<CardData>() and place in hand
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
        OnHandChanged?.Invoke();
    }

    /// <summary>Try to play the card at hand index. Returns true if played. Validates Budget (and other costs); shows "Not enough budget" on Psyche_AutoCanvas when budget is too low.</summary>
    public bool TryPlayCard(int handIndex)
    {
        if (handIndex < 0 || handIndex >= _hand.Count) return false;
        var card = _hand[handIndex];
        if (ResourceManager.Instance == null) return false;

        if (!ResourceManager.Instance.CanAfford(card.costPower, card.costBudget, card.costTime))
        {
            if (card.costBudget > 0 && ResourceManager.Instance.Budget < card.costBudget)
                ShowNotEnoughBudget();
            return false;
        }

        ResourceManager.Instance.TrySpend(card.costPower, card.costBudget, card.costTime);
        ApplyEffect(card);
        _hand.RemoveAt(handIndex);
        RemoveCardViewAt(handIndex);
        _discard.Add(card);
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

    private void SpawnCardView(CardData data)
    {
        if (cardPrefab == null || handParent == null) return;
        var go = Instantiate(cardPrefab, handParent);
        var view = go.GetComponent<CardView>();
        if (view != null)
        {
            view.Bind(data, this);
        }
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

    private void ApplyEffect(CardData card)
    {
        if (ResourceManager.Instance == null) return;
        switch (card.effectType)
        {
            case CardData.EffectType.GainPower:
                ResourceManager.Instance.AddPower(card.effectValue);
                break;
            case CardData.EffectType.GainBudget:
                ResourceManager.Instance.AddBudget(card.effectValue);
                break;
            case CardData.EffectType.GainTime:
                ResourceManager.Instance.AddTime(card.effectValue);
                break;
        }
    }

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
        // Solar Array Deploy x4, Budget Request x2, etc. - create runtime data for testing
        for (int i = 0; i < 4; i++)
            _deck.Add(CreateRuntimeCard("Solar Array Deploy", "Gain 3 Power", 0, 0, 0, CardData.EffectType.GainPower, 3));
        for (int i = 0; i < 2; i++)
            _deck.Add(CreateRuntimeCard("Budget Request", "Gain 3 Budget", 0, 0, 1, CardData.EffectType.GainBudget, 3));
        for (int i = 0; i < 2; i++)
            _deck.Add(CreateRuntimeCard("Multispectral Imager", "Collect 2 Surface data", 2, 0, 1, CardData.EffectType.None, 0));
        _deck.Add(CreateRuntimeCard("Trajectory Correction", "Adjust orbital phase", 2, 1, 0, CardData.EffectType.None, 0));
        _deck.Add(CreateRuntimeCard("Compositional Analysis", "Convert data to Conclusion", 0, 1, 2, CardData.EffectType.None, 0));
    }

    private static CardData CreateRuntimeCard(string name, string desc, int p, int b, int t, CardData.EffectType effect, int value)
    {
        var card = ScriptableObject.CreateInstance<CardData>();
        card.cardName = name;
        card.description = desc;
        card.costPower = p;
        card.costBudget = b;
        card.costTime = t;
        card.effectType = effect;
        card.effectValue = value;
        return card;
    }

    /// <summary>Creates one random card using ScriptableObject.CreateInstance&lt;CardData&gt;() from the template pool.</summary>
    private static CardData CreateRandomRuntimeCard()
    {
        int idx = UnityEngine.Random.Range(0, RandomCardTemplates.Length);
        var t = RandomCardTemplates[idx];
        return CreateRuntimeCard(t.name, t.desc, t.p, t.b, t.t, t.effect, t.value);
    }

    /// <summary>Shows "Not enough budget" on Psyche_AutoCanvas (or feedbackMessageTarget). Clears after 2 seconds.</summary>
    private void ShowNotEnoughBudget()
    {
        const string message = "Not enough budget";
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
        if (canvasGo == null) return null;

        var existing = canvasGo.transform.Find("BudgetMessage");
        if (existing != null)
        {
            var t = existing.GetComponent<Text>();
            if (t != null) return t;
            var tmp = existing.GetComponent<TMP_Text>();
            if (tmp != null) return tmp;
        }

        var child = new GameObject("BudgetMessage");
        child.transform.SetParent(canvasGo.transform, false);
        var rect = child.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(400f, 60f);

        var budgetMessageText = child.AddComponent<Text>();
        budgetMessageText.text = "";
        budgetMessageText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        budgetMessageText.fontSize = 24;
        budgetMessageText.alignment = TextAnchor.MiddleCenter;
        budgetMessageText.color = Color.red;
        return budgetMessageText;
    }

    private IEnumerator ClearFeedbackAfterSeconds(Component textComponent, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        SetFeedbackText(textComponent, "");
    }
}
