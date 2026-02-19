using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages deck, hand, and discard. Draws cards, spawns CardView under hand parent, handles play.
/// Assign handParent (e.g. Hand_Zone) and cardPrefab (CardView prefab) in the Inspector.
/// </summary>
public class DeckManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Parent for hand cards (e.g. Hand_Zone with HandFanner).")]
    public Transform handParent;

    [Tooltip("Prefab with CardView (and CardTrigger for hover).")]
    public GameObject cardPrefab;

    [Header("Starting deck (assign CardData assets)")]
    [Tooltip("Cards to shuffle into the deck at run start. Leave empty to use test deck.")]
    public List<CardData> startingDeck = new List<CardData>();

    private readonly List<CardData> _deck = new List<CardData>();
    private readonly List<CardData> _hand = new List<CardData>();
    private readonly List<CardData> _discard = new List<CardData>();
    private readonly List<GameObject> _handViews = new List<GameObject>();

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
        }
        else
            SetupTestDeck();
        ShuffleDeck();
        Draw(5);
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

    /// <summary>Try to play the card at hand index. Returns true if played.</summary>
    public bool TryPlayCard(int handIndex)
    {
        if (handIndex < 0 || handIndex >= _hand.Count) return false;
        var card = _hand[handIndex];
        if (ResourceManager.Instance == null) return false;
        if (!ResourceManager.Instance.CanAfford(card.costPower, card.costBudget, card.costTime))
            return false;

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
        var card = CreateInstance<CardData>();
        card.cardName = name;
        card.description = desc;
        card.costPower = p;
        card.costBudget = b;
        card.costTime = t;
        card.effectType = effect;
        card.effectValue = value;
        return card;
    }
}
