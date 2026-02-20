using UnityEngine;

/// <summary>
/// ScriptableObject definition for a single card.
/// Create assets via Assets > Create > Psyche > Card Data.
/// </summary>
[CreateAssetMenu(fileName = "NewCard", menuName = "Psyche/Card Data", order = 0)]
public class CardData : ScriptableObject
{
    [Header("Identity")]
    public string cardName = "Unnamed Card";
    [TextArea(2, 4)]
    public string description = "";

    [Header("Cost (per design doc)")]
    [Tooltip("Power cost to play.")]
    public int costPower;
    [Tooltip("Budget cost to play.")]
    public int costBudget;
    [Tooltip("Time cost to play.")]
    public int costTime;

    [Header("Effect (simple resource effects for now)")]
    public EffectType effectType = EffectType.None;
    [Tooltip("Amount for GainPower, GainBudget, GainTime, etc.")]
    public int effectValue;

    [Header("Meta")]
    public CardRarity rarity = CardRarity.Common;

    public enum EffectType
    {
        None,
        GainPower,
        GainBudget,
        GainTime,
    }

    public enum CardRarity
    {
        Common,
        Uncommon,
        Rare,
    }

    /// <summary>Format cost for UI, e.g. "P:2 B:1 T:1".</summary>
    public string CostString()
    {
        var parts = new System.Collections.Generic.List<string>();
        if (costPower > 0) parts.Add($"P:{costPower}");
        if (costBudget > 0) parts.Add($"B:{costBudget}");
        if (costTime > 0) parts.Add($"T:{costTime}");
        if (parts.Count == 0) return "0";
        return string.Join(" ", parts);
    }
}
