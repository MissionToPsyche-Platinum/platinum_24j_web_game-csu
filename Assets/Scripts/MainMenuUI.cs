using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Simple main menu controller script. Button methods use UnityEngine.SceneManagement to load scenes.
/// Expected buttons: Start Game -> OnStartGame() (loads SampleScene); Options -> OnOpenOptions()/OnCloseOptions(); View Cards -> OnViewCards() (loads CardGallery).
/// Wire these in the Button.onClick events in the Inspector.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("Scene Names")]
    [Tooltip("Scene to load when starting the game. Uses SceneManager.LoadScene.")]
    public string gameSceneName = "SampleScene";

    [Tooltip("Scene to load when viewing the card collection (e.g. CardGallery). Uses SceneManager.LoadScene.")]
    public string cardCollectionSceneName = "CardGallery";

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
    /// Called by the Start Game button. Loads SampleScene (or gameSceneName) via SceneManager.
    /// </summary>
    public void OnStartGame()
    {
        string sceneToLoad = !string.IsNullOrEmpty(gameSceneName) ? gameSceneName : "SampleScene";
        SceneManager.LoadScene(sceneToLoad);
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
    /// Called by the View Cards button. Loads CardGallery (or cardCollectionSceneName) via SceneManager.
    /// </summary>
    public void OnViewCards()
    {
        string sceneToLoad = !string.IsNullOrEmpty(cardCollectionSceneName) ? cardCollectionSceneName : "CardGallery";
        SceneManager.LoadScene(sceneToLoad);
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

