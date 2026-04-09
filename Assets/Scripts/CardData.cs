using UnityEngine;

[CreateAssetMenu(fileName = "New Card", menuName = "Card System/Card Data")]
public class CardData : ScriptableObject
{
    public string cardName;
    
    public enum CardType { Resource, Instrument, Maneuver, Analysis, Crisis }
    public CardType type;

    [Header("Costs")]
    public int powerCost;
    public int budgetCost;
    public int timeCost;

    [Header("Effects")]
    public string description;
    public int dataValue; // Used for Instrument cards to track data collected
    public Sprite cardArt;
    
    public enum Rarity { Common, Uncommon, Rare }
    public Rarity rarity;
}