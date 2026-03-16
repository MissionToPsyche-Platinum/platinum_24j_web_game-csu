using UnityEngine;

[CreateAssetMenu(fileName = "NewCard", menuName = "Psyche/Card")]
public class CardData : ScriptableObject 
{
    public string cardName;
    
    public enum CardType { Resource, Instrument, Maneuver, Analysis, Crisis }
    public CardType type;

    [Header("Costs")]
    public int costPower;
    public int costBudget;
    public int costTime;

    [Header("Effects")]
    public string description;
    public int dataValue; // Used for Instrument cards to track data collected
    public Sprite cardArt;

    public enum EffectType { None, GainPower, GainBudget, GainTime }
    public EffectType effectType;
    
    public enum Rarity { Common, Uncommon, Rare }
    public Rarity rarity;
}