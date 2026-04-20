using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Always-active singleton that handles global debug hotkeys.
/// Auto-instantiates via [RuntimeInitializeOnLoadMethod] — no scene setup needed.
///
/// Hotkeys:
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
