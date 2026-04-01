using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Simple main menu controller script.
///
///
/// Expected buttons (all optional but recommended):
/// - Start Game   -> calls OnStartGame()
/// - Options      -> calls OnOpenOptions() / OnCloseOptions()
/// - View Cards   -> calls OnViewCards()
///
/// You can wire these in the Button.onClick events in the Inspector.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("Scene Names")]
    [Tooltip("Scene to load when starting the game (e.g., 'SampleScene').")]
    public string gameSceneName = "SampleScene";

    [Tooltip("Optional: scene to load when viewing the full card collection (can be added later).")]
    public string cardCollectionSceneName = "";

    [Tooltip("Scene to load for options menu.")]
    public string optionsSceneName = OptionsNavigation.DefaultOptionsScene;

    [Header("Panels / Layout")]
    [Tooltip("Optional: options panel GameObject to toggle on/off.")]
    public GameObject optionsPanel;

    [Tooltip("Optional: root menu panel to hide when navigating to sub-menus.")]
    public GameObject mainMenuPanel;

    [Header("Existing View Layers (optional)")]
    [Tooltip("Gameplay layer under Main_Canvas (e.g., 'Gameplay_layer'). Will be enabled when the game starts if no scene load is configured.")]
    public GameObject gameplayLayer;

    [Tooltip("Hand zone layer under Main_Canvas (e.g., 'Hand_zone'). Will be enabled when the game starts if no scene load is configured.")]
    public GameObject handZoneLayer;

    [Tooltip("Background layer under Main_Canvas (e.g., 'Background_Layer'). Usually stays enabled for both menu and gameplay.")]
    public GameObject backgroundLayer;

    [Header("In-scene game start")]
    [Tooltip("Optional: GameCanvas under --- UI --- (HUD). If unset, searches for a child named GameCanvas under a root named \"--- UI ---\".")]
    public GameObject gameCanvas;

    /// <summary>
    /// Called by the Start Game button.
    /// If a game scene name is set, loads that scene.
    /// Otherwise, toggles existing layers in the current scene:
    /// - Hides the main menu panel
    /// - Enables GameCanvas (under --- UI ---) when assigned or found
    /// - Enables gameplay and hand layers (hand / DeckManager should start disabled until this runs)
    /// </summary>
    public void OnStartGame()
    {
        if (!string.IsNullOrEmpty(gameSceneName))
        {
            SceneManager.LoadScene(gameSceneName);
            return;
        }

        // In-scene toggle mode using existing Main_Canvas layers.
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        EnableGameCanvasIfPresent();

        if (gameplayLayer != null)
            gameplayLayer.SetActive(true);

        if (handZoneLayer != null)
            handZoneLayer.SetActive(true);

        if (backgroundLayer != null)
            backgroundLayer.SetActive(true);
    }

    void EnableGameCanvasIfPresent()
    {
        if (gameCanvas != null)
        {
            gameCanvas.SetActive(true);
            return;
        }

        var uiFolder = GameObject.Find("--- UI ---");
        if (uiFolder == null)
            return;
        var t = uiFolder.transform.Find("GameCanvas");
        if (t != null)
            t.gameObject.SetActive(true);
    }

    /// <summary>
    /// Called by the Options button.
    /// Opens the dedicated options scene.
    /// </summary>
    public void OnOpenOptions()
    {
        if (!string.IsNullOrEmpty(optionsSceneName))
        {
            OptionsNavigation.OpenOptions(optionsSceneName);
            return;
        }

        // Fallback: old in-scene panel behavior.
        if (optionsPanel != null)
            optionsPanel.SetActive(true);
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);
    }

    /// <summary>
    /// Called by a Close / Back button on the options panel.
    /// </summary>
    public void OnCloseOptions()
    {
        if (optionsPanel != null)
            optionsPanel.SetActive(false);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
    }

    /// <summary>
    /// Called by the View Cards button.
    /// For now this can either log a message or, once implemented,
    /// load a dedicated card-collection scene.
    /// </summary>
    public void OnViewCards()
    {
        if (!string.IsNullOrEmpty(cardCollectionSceneName))
        {
            SceneManager.LoadScene(cardCollectionSceneName);
        }
        else
        {
            Debug.Log("MainMenuUI: View Cards clicked. Card collection scene not set yet.");
        }
    }

    /// <summary>
    /// Optional: Quit button handler. Works in standalone builds.
    /// </summary>
    public void OnQuitGame()
    {
        Debug.Log("MainMenuUI: Quit requested.");
        Application.Quit();
    }
}

