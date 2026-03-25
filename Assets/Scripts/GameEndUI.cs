using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Script for the Victory and Defeat panels.
/// Handles button logic and potential text customization.
/// </summary>
public class GameEndUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text resultTitle;   // e.g. "MISSION SUCCESS" or "MISSION FAILED"
    [SerializeField] private TMP_Text resultMessage; // e.g. "We gathered all data!" or "Budget depleted."

    [Header("Menu Scene")]
    [SerializeField] private string mainMenuScene = "MainMenu";

    /// <summary>
    /// Restarts the run from Floor 1.
    /// </summary>
    public void OnRestartRun()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetRun();
        }
        
        // Reload current scene or load specific start scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Returns to the main menu.
    /// </summary>
    public void OnBackToMenu()
    {
        SceneManager.LoadScene(mainMenuScene);
    }

    /// <summary>
    /// Quits the application.
    /// </summary>
    public void OnQuitGame()
    {
        Debug.Log("[GameEndUI] Quit requested.");
        Application.Quit();
    }
}
