using UnityEngine;

/// <summary>
/// Runtime-wide gate for debug tooling (hotkeys, cheats, auto-win, etc.).
///
/// Resolution order:
///   • Unity Editor                       → always enabled
///   • WebGL build with <c>?debug=1</c> in the URL (or <c>#debug</c>) → enabled
///   • Development build (Debug.isDebugBuild) → enabled
///   • Otherwise (public hosted release)  → disabled
///
/// Usage for the symposium demo: append <c>?debug=1</c> to the hosted URL
/// (e.g. <c>https://host/psyche/?debug=1</c>). Leave the plain URL for the
/// public-facing version so judges / guests cannot trigger auto-win.
///
/// Accepted forms (case-insensitive):
///   ?debug=1   ?debug=true   &amp;debug=1   #debug
/// </summary>
public static class DebugMode
{
    private static bool _resolved;
    private static bool _enabled;

    public static bool Enabled
    {
        get
        {
            if (!_resolved)
            {
                _enabled  = Resolve();
                _resolved = true;
                if (_enabled)
                    Debug.Log("[DebugMode] Debug hotkeys ENABLED (editor, dev build, or ?debug=1 URL).");
            }
            return _enabled;
        }
    }

    /// <summary>Forces the debug-mode flag. Intended for tests or manual toggles from a hidden UI.</summary>
    public static void ForceEnable(bool on)
    {
        _enabled = on;
        _resolved = true;
        Debug.Log($"[DebugMode] Debug hotkeys forced {(on ? "ENABLED" : "DISABLED")}.");
    }

    private static bool Resolve()
    {
#if UNITY_EDITOR
        return true;
#else
        // Dev build (Build Settings → Development Build) always enables hotkeys.
        if (Debug.isDebugBuild) return true;

#if UNITY_WEBGL
        string url = Application.absoluteURL;
        if (string.IsNullOrEmpty(url)) return false;
        string lower = url.ToLowerInvariant();
        // Query-string form (?debug=1 or &debug=1 or ?debug=true / &debug=true)
        if (lower.Contains("?debug=1")    || lower.Contains("&debug=1"))    return true;
        if (lower.Contains("?debug=true") || lower.Contains("&debug=true")) return true;
        // Fragment form (#debug) — survives static hosts that strip query strings
        if (lower.Contains("#debug")) return true;
#endif
        return false;
#endif
    }
}
