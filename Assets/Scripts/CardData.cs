using UnityEngine;

[CreateAssetMenu(fileName = "NewCard", menuName = "Psyche/Card")]
public class CardData : ScriptableObject 
{
    public string cardName;
    
    public enum CardType { Resource, Instrument, Maneuver, Analysis, Crisis }
    public CardType type;

    public enum CardCategory { Resource, Instrument, Maneuver, Analysis, Crisis }
    public CardCategory category;

    [Header("Costs")]
    public int costPower;
    public int costBudget;
    public int costTime;

    [Header("Effects")]
    public string description;
    public int dataValue;
    public Sprite cardArt;

    public enum EffectType
    {
        None,

        // --- Resource ---
        GainPower, GainBudget, GainTime, ReducePowerCosts,

        // --- Instrument (data collection) ---
        CollectSurface, CollectElemental, CollectMagnetic, CollectGravity, CollectAllData,

        // --- Maneuver ---
        AdjustOrbit, OrbitInsertion, BonusNextInstrument, PreventPenalty,
        DoubleNextInstrument, CancelCrisis,

        // --- Analysis (conclusions) ---
        CompositionConclusion, DynamoConclusion, InteriorConclusion,
        FormationConclusion, WildConclusion, UpgradeConclusion,

        // --- Utility ---
        DrawCards,

        // --- Crisis / Negative Events ---
        LosePower, LoseBudget, LoseTime,
        LoseProgress, SkipTurn, DiscardRandom,
    }
    public EffectType effectType;

    [Header("Effect Values")]
    public int effectValue;
    public int effectValue2;

    public string EffectSummary()
    {
        switch (effectType)
        {
            case EffectType.GainPower:           return $"+{effectValue} Power";
            case EffectType.GainBudget:          return $"+{effectValue} Budget";
            case EffectType.GainTime:            return $"+{effectValue} Time";
            case EffectType.ReducePowerCosts:    return "Power costs reduced this turn";
            case EffectType.CollectSurface:      return $"+{effectValue} Surface data";
            case EffectType.CollectElemental:    return $"+{effectValue} Elemental data";
            case EffectType.CollectMagnetic:     return $"+{effectValue} Magnetic data";
            case EffectType.CollectGravity:      return $"+{effectValue} Gravity data";
            case EffectType.CollectAllData:      return $"+{effectValue} All data";
            case EffectType.AdjustOrbit:         return "Orbit adjusted";
            case EffectType.OrbitInsertion:      return "Orbit insertion";
            case EffectType.BonusNextInstrument: return "Next instrument +1";
            case EffectType.PreventPenalty:       return "Penalty prevented";
            case EffectType.DoubleNextInstrument: return "Next instrument x2";
            case EffectType.CancelCrisis:        return "Crisis cancelled";
            case EffectType.CompositionConclusion: return "Composition conclusion";
            case EffectType.DynamoConclusion:    return "Dynamo conclusion";
            case EffectType.InteriorConclusion:  return "Interior conclusion";
            case EffectType.FormationConclusion: return "Formation conclusion";
            case EffectType.WildConclusion:      return "Wild conclusion";
            case EffectType.UpgradeConclusion:   return "Conclusion upgraded";
            case EffectType.DrawCards:           return $"Draw {(effectValue > 0 ? effectValue : effectValue2)} cards";
            case EffectType.LosePower:           return $"-{effectValue} Power";
            case EffectType.LoseBudget:          return $"-{effectValue} Budget";
            case EffectType.LoseTime:            return $"-{effectValue} Time";
            case EffectType.LoseProgress:        return $"-{effectValue} Progress";
            case EffectType.SkipTurn:            return "Skip next turn";
            case EffectType.DiscardRandom:       return $"Discard {effectValue} cards";
            default:                             return "";
        }
    }
}
