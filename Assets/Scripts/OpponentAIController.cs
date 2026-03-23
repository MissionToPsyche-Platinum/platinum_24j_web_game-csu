using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Separate resource pool for the computer opponent and a hook for running its turn.
/// Wire the AI's <see cref="DeckManager"/> with <see cref="DeckManager.aiResourceWallet"/> = this component.
/// </summary>
public class OpponentAIController : MonoBehaviour
{
    [Header("AI deck")]
    [Tooltip("This deck must have aiResourceWallet assigned to this OpponentAIController.")]
    [SerializeField] private DeckManager aiDeckManager;

    [Header("Starting resources (first run / full reset)")]
    [SerializeField] private int startingPower = 3;
    [SerializeField] private int startingBudget = 6;
    [SerializeField] private int startingTime = 15;

    [Header("Turn behaviour")]
    [SerializeField] private int targetHandSize = 5;
    [SerializeField] private float delayBetweenCardPlays = 0.4f;
    [SerializeField] private int maxPlaysPerTurn = 24;

    private int _power;
    private int _budget;
    private int _time;

    public int Power => _power;
    public int Budget => _budget;
    public int TimeRemaining => _time;

    public DeckManager AiDeck => aiDeckManager;

    /// <summary>Fired when AI power/budget/time change (optional HUD).</summary>
    public event Action<int, int, int> OnAiResourcesChanged;

    private void Awake()
    {
        ResetResourcesToStarting();
    }

    private void Start()
    {
        // If EncounterManager already started before this component enabled, match player power refresh.
        if (EncounterManager.Instance != null && EncounterManager.Instance.IsEncounterActive)
            _power = 5;
    }

    private void OnEnable()
    {
        if (EncounterManager.Instance != null)
            EncounterManager.Instance.OnEncounterStarted += OnEncounterStarted;
    }

    private void OnDisable()
    {
        if (EncounterManager.Instance != null)
            EncounterManager.Instance.OnEncounterStarted -= OnEncounterStarted;
    }

    private void OnEncounterStarted(string type, string objective, int current, int target)
    {
        // Match ResourceManager.RefreshForEncounter: refresh power only; keep budget/time.
        _power = 5;
        NotifyChanged();
    }

    /// <summary>Full reset (e.g. new run from menu).</summary>
    public void ResetResourcesToStarting()
    {
        _power = startingPower;
        _budget = startingBudget;
        _time = startingTime;
        NotifyChanged();
    }

    public bool CanAfford(int p, int b, int t) => _power >= p && _budget >= b && _time >= t;

    public void TrySpend(int p, int b, int t)
    {
        _power -= p;
        _budget -= b;
        _time -= t;
        NotifyChanged();
    }

    public void AddPower(int amount)
    {
        _power += amount;
        NotifyChanged();
    }

    public void AddBudget(int amount)
    {
        _budget += amount;
        NotifyChanged();
    }

    public void AddTime(int amount)
    {
        _time += amount;
        NotifyChanged();
    }

    private void NotifyChanged()
    {
        OnAiResourcesChanged?.Invoke(_power, _budget, _time);
    }

    /// <summary>
    /// Greedy loop: draw toward hand size, then repeatedly play the first affordable card until none left.
    /// </summary>
    public IEnumerator RunTurnCoroutine()
    {
        if (aiDeckManager == null)
            yield break;

        int drawNeed = Mathf.Max(0, targetHandSize - aiDeckManager.Hand.Count);
        if (drawNeed > 0)
            aiDeckManager.Draw(drawNeed);

        yield return null;

        int plays = 0;
        while (plays < maxPlaysPerTurn && aiDeckManager.Hand.Count > 0)
        {
            bool playedOne = false;
            for (int i = 0; i < aiDeckManager.Hand.Count; i++)
            {
                if (aiDeckManager.TryPlayCard(i))
                {
                    playedOne = true;
                    plays++;
                    if (delayBetweenCardPlays > 0f)
                        yield return new WaitForSeconds(delayBetweenCardPlays);
                    break;
                }
            }

            if (!playedOne)
                break;
        }
    }
}
