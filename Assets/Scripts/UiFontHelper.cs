using UnityEngine;

/// <summary>
/// Resolves the Kenney Future font (Kenney “space” UI family) for legacy <see cref="UnityEngine.UI.Text"/>.
/// Falls back to Unity’s built-in font if Resources copy is missing.
/// </summary>
public static class UiFontHelper
{
    private static Font _cached;

    public static Font KenneyFutureOrFallback()
    {
        if (_cached != null)
            return _cached;

        _cached = Resources.Load<Font>("Fonts/KenneyFuture");
        if (_cached == null)
            _cached = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return _cached;
    }
}
