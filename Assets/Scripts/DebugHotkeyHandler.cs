using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Global debug-hotkey singleton. Only auto-instantiates when <see cref="DebugMode.Enabled"/>
/// is true (Unity Editor, development builds, or WebGL URL with <c>?debug=1</c>).
/// Public hosted releases skip creation entirely, so judges / guests cannot trigger
/// the auto-win / auto-lose shortcuts.
///
/// Hotkeys (when enabled):
///   −  / Numpad −   → Mission Failure
///   Shift+= / Numpad + → Mission Success
/// </summary>
public class DebugHotkeyHandler : MonoBehaviour
{
    private static DebugHotkeyHandler _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { _instance = null; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (_instance != null) return;
        if (!DebugMode.Enabled) return; // public release: no hotkey singleton
        var go = new GameObject("[DebugHotkeys]");
        Object.DontDestroyOnLoad(go);
        go.AddComponent<DebugHotkeyHandler>();
    }

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    private void Update()
    {
        // Second gate in case the flag is toggled at runtime (e.g. via DebugMode.ForceEnable).
        if (!DebugMode.Enabled) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb[Key.Minus].wasPressedThisFrame || kb[Key.NumpadMinus].wasPressedThisFrame)
        {
            MissionEndScreenUI.ShowFailure();
            return;
        }

        bool plus = kb[Key.NumpadPlus].wasPressedThisFrame
            || (kb[Key.Equals].wasPressedThisFrame
                && (kb[Key.LeftShift].isPressed || kb[Key.RightShift].isPressed));
        if (plus)
            MissionEndScreenUI.ShowSuccess();
    }
}
