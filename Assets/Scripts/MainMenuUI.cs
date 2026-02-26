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

    /// <summary>
    /// Called by the Start Game button.
    /// If a game scene name is set, loads that scene.
    /// Otherwise, toggles existing layers in the current scene:
    /// - Hides the main menu panel
    /// - Enables gameplay and hand layers
    /// </summary>
    public void OnStartGame()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();
        if (!string.IsNullOrEmpty(gameSceneName))
        {
            SceneManager.LoadScene(gameSceneName);
            return;
        }

        // In-scene toggle mode using existing Main_Canvas layers.
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (gameplayLayer != null)
            gameplayLayer.SetActive(true);

        if (handZoneLayer != null)
            handZoneLayer.SetActive(true);

        if (backgroundLayer != null)
            backgroundLayer.SetActive(true);
    }

    /// <summary>
    /// Called by the Options button.
    /// Shows the options overlay panel (no scene change).
    /// </summary>
    public void OnOpenOptions()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();

        // Find the overlay if not assigned
        if (optionsPanel == null)
        {
            var overlay = GameObject.Find("OptionsOverlay");
            if (overlay != null) optionsPanel = overlay;
        }

        if (optionsPanel != null)
            optionsPanel.SetActive(true);
    }

    /// <summary>
    /// Called by a Close / Back button on the options panel.
    /// </summary>
    public void OnCloseOptions()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();
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
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();
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
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();
        Debug.Log("MainMenuUI: Quit requested.");
        Application.Quit();
    }
}

