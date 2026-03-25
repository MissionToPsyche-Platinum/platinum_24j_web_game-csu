using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Loads the main menu scene from a UI button. Attach to the same GameObject as <see cref="Button"/> or assign explicitly.
/// </summary>
[DisallowMultipleComponent]
public class MainMenuSceneLoader : MonoBehaviour
{
    [SerializeField] private Button mainMenuButton;

    private void Awake()
    {
        if (mainMenuButton == null)
            mainMenuButton = GetComponent<Button>();

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
    }

    private void OnDestroy()
    {
        if (mainMenuButton != null)
            mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
    }

    public void OnMainMenuClicked()
    {
        OptionsNavigation.LoadMainMenu();
    }
}
