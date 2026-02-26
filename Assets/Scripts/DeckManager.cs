using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DeckManager : MonoBehaviour
{
    [Header("References")]
    public Transform handParent;
    public GameObject cardPrefab;
    public CardHandLayout cardHandLayout;

    [Header("Feedback message (optional)")]
    public GameObject feedbackMessageTarget;

    [Header("Starting deck (assign CardData assets)")]
    public List<CardData> startingDeck = new List<CardData>();

    [Header("Draw Phase: random card pool")]
    public int drawPhaseCardCount = 5;

    private readonly List<CardData> _deck = new List<CardData>();
    private readonly List<CardData> _hand = new List<CardData>();
    private readonly List<CardData> _discard = new List<CardData>();
    private readonly List<GameObject> _handViews = new List<GameObject>();

    // FIXED: Updated templates to use CardData.EffectType
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

    private void Start()
    {
        if (handParent == null) handParent = transform;
        
        if (startingDeck != null && startingDeck.Count > 0)
        {
            _deck.Clear();
            foreach (var c in startingDeck) if (c != null) _deck.Add(c);
            ShuffleDeck();
            Draw(Mathf.Min(drawPhaseCardCount, _deck.Count));
        }
        else
        {
            _deck.Clear();
            int count = Mathf.Clamp(drawPhaseCardCount, 1, 20);
            for (int i = 0; i < count; i++) _deck.Add(CreateRandomRuntimeCard());
            ShuffleDeck();
            Draw(count);
        }
    }

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
    }

    private void ApplyEffect(CardData card)
    {
        if (ResourceManager.Instance == null) return;
        
        // FIXED: Corrected the switch cases to use the proper Enum paths
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

    private static CardData CreateRuntimeCard(string name, string desc, int p, int b, int t, CardData.EffectType effect, int value)
    {
        var card = ScriptableObject.CreateInstance<CardData>();
        card.cardName = name;
        card.description = desc;
        card.costPower = p;
        card.costBudget = b;
        card.costTime = t;
        card.effectType = effect; // FIXED: Added missing assignment
        card.effectValue = value;
        return card;
    }

    private static CardData CreateRandomRuntimeCard()
    {
        int idx = UnityEngine.Random.Range(0, RandomCardTemplates.Length);
        var t = RandomCardTemplates[idx];
        return CreateRuntimeCard(t.name, t.desc, t.p, t.b, t.t, t.effect, t.value);
    }

    // ... (Remaining methods like ShuffleDeck, SpawnCardView, etc. stay the same as your original)
}