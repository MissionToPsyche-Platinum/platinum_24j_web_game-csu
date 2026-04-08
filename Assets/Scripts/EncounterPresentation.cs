/// <summary>
/// Maps <see cref="EncounterManager"/> encounter type strings to UI copy (HUD subtitle, turn-limit phrasing).
/// </summary>
public static class EncounterPresentation
{
    public enum Kind
    {
        DataCollection,
        SystemsStressTest,
        CrisisResponse,
        AnalysisChallenge,
        Other
    }

    public static Kind Classify(string encounterType)
    {
        if (string.IsNullOrEmpty(encounterType))
            return Kind.Other;
        var u = encounterType.ToUpperInvariant();
        if (u.Contains("SYSTEMS") && u.Contains("STRESS"))
            return Kind.SystemsStressTest;
        if (u.Contains("DATA") || u.Contains("COLLECTION"))
            return Kind.DataCollection;
        if (u.Contains("CRISIS"))
            return Kind.CrisisResponse;
        if (u.Contains("ANALYSIS"))
            return Kind.AnalysisChallenge;
        return Kind.Other;
    }

    /// <summary>Optional one-line hint under the top resource bar (if <see cref="GameHUD"/> has EncounterBriefText).</summary>
    public static string HudSubtitleForType(string encounterType)
    {
        switch (Classify(encounterType))
        {
            case Kind.DataCollection:
                return "Data collection — gather instrument readings toward the goal.";
            case Kind.SystemsStressTest:
                return "Systems stress test — resolve crises before they overwhelm you.";
            case Kind.CrisisResponse:
                return "Crisis response — clear hazards before they compound.";
            case Kind.AnalysisChallenge:
                return "Analysis — convert raw data into conclusions.";
            default:
                return "Complete the encounter objective before the turn limit.";
        }
    }

    public static string FormatTurnLimitLine(Kind kind, int maxTurns)
    {
        switch (kind)
        {
            case Kind.SystemsStressTest:
                return $"Resolve 4 crises before 3 stack";
            case Kind.CrisisResponse:
                return $"Resolve in ≤ {maxTurns} turns";
            case Kind.AnalysisChallenge:
                return $"Analysis deadline: {maxTurns} turns";
            default:
                return $"Turn limit: {maxTurns}";
        }
    }
}
