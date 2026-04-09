using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shows the in-scene <c>OptionsCanvas</c> by disabling other menu/gameplay UI roots, then restores them on hide.
/// Add one instance to your main scene and assign <see cref="optionsCanvasRoot"/> (and optionally <see cref="mainMenuUI"/>).
/// </summary>
public class OptionsOverlayController : MonoBehaviour
{
    private static OptionsOverlayController _instance;

    [Header("Required")]
    [Tooltip("The OptionsCanvas root (usually starts inactive).")]
    [SerializeField] private GameObject optionsCanvasRoot;

    [Header("Layer sources")]
    [Tooltip("If set, main menu panel, GameCanvas, gameplay/hand layers are hidden when options open.")]
    [SerializeField] private MainMenuUI mainMenuUI;

    [Tooltip("Extra roots to disable while options are open (e.g. a full-screen group).")]
    [SerializeField] private GameObject[] additionalRootsToHide;

    private readonly List<GameObject> _targets = new List<GameObject>();
    private readonly List<bool> _wasActive = new List<bool>();
    private bool _shown;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning("[OptionsOverlayController] Multiple instances — destroying duplicate.", this);
            Destroy(gameObject);
            return;
        }

        _instance = this;

        if (optionsCanvasRoot == null)
        {
            var found = GameObject.Find("OptionsCanvas");
            if (found != null)
                optionsCanvasRoot = found;
        }

        if (mainMenuUI == null)
            mainMenuUI = FindFirstObjectByType<MainMenuUI>();

        if (optionsCanvasRoot != null && optionsCanvasRoot.activeSelf)
            optionsCanvasRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    /// <summary>Tries to show the options overlay. Returns true if this controller handled it.</summary>
    public static bool TryShow()
    {
        if (_instance == null)
            _instance = FindFirstObjectByType<OptionsOverlayController>(FindObjectsInactive.Include);
        if (_instance == null || _instance.optionsCanvasRoot == null)
            return false;
        return _instance.Show();
    }

    /// <summary>Hides the overlay and restores previous layer active states.</summary>
    public static bool HideIfVisible()
    {
        if (_instance == null)
            _instance = FindFirstObjectByType<OptionsOverlayController>(FindObjectsInactive.Include);
        if (_instance == null || !_instance._shown)
            return false;
        _instance.HideRestorePrevious();
        return true;
    }

    /// <summary>Hides options and sends the player to the main menu view (does not restore prior gameplay UI).</summary>
    public static bool HideAndShowMainMenu()
    {
        if (_instance == null)
            _instance = FindFirstObjectByType<OptionsOverlayController>(FindObjectsInactive.Include);
        if (_instance == null || !_instance._shown)
            return false;
        _instance.HideDiscardRestoreAndGoMainMenu();
        return true;
    }

    /// <returns>True if the overlay is visible after this call (including already shown).</returns>
    private bool Show()
    {
        if (_shown)
            return true;

        if (mainMenuUI != null && mainMenuUI.mainMenuPanel != null && optionsCanvasRoot != null
            && IsUnder(optionsCanvasRoot.transform, mainMenuUI.mainMenuPanel.transform))
        {
            Debug.LogError(
                "[OptionsOverlayController] OptionsCanvas is under MainMenu_Panel. Move OptionsCanvas to be a sibling (e.g. both under Main_Canvas), not a child of the menu panel.");
            return false;
        }

        _targets.Clear();
        _wasActive.Clear();

        if (mainMenuUI != null)
            mainMenuUI.CollectRootsToHideForOptionsOverlay(_targets);

        if (additionalRootsToHide != null)
        {
            for (int i = 0; i < additionalRootsToHide.Length; i++)
            {
                var go = additionalRootsToHide[i];
                if (go != null && !_targets.Contains(go))
                    _targets.Add(go);
            }
        }

        for (int i = 0; i < _targets.Count; i++)
        {
            var go = _targets[i];
            if (go == null)
            {
                _wasActive.Add(false);
                continue;
            }

            _wasActive.Add(go.activeSelf);
            go.SetActive(false);
        }

        optionsCanvasRoot.SetActive(true);
        _shown = true;
        return true;
    }

    private static bool IsUnder(Transform child, Transform ancestor)
    {
        for (var t = child; t != null; t = t.parent)
        {
            if (t == ancestor)
                return true;
        }
        return false;
    }

    private void HideRestorePrevious()
    {
        if (!_shown)
            return;

        if (optionsCanvasRoot != null)
            optionsCanvasRoot.SetActive(false);

        for (int i = 0; i < _targets.Count; i++)
        {
            var go = _targets[i];
            if (go == null)
                continue;
            bool prev = i < _wasActive.Count && _wasActive[i];
            go.SetActive(prev);
        }

        _targets.Clear();
        _wasActive.Clear();
        _shown = false;
    }

    private void HideDiscardRestoreAndGoMainMenu()
    {
        if (!_shown)
            return;

        if (optionsCanvasRoot != null)
            optionsCanvasRoot.SetActive(false);

        _targets.Clear();
        _wasActive.Clear();
        _shown = false;

        if (mainMenuUI == null)
            mainMenuUI = FindFirstObjectByType<MainMenuUI>();
        mainMenuUI?.ReturnToMainMenuView();
    }
}
