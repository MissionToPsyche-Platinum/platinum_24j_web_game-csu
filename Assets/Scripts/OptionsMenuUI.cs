using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the Options overlay panel.
/// Reads/writes volume via AudioSettingsStore and updates AudioManager live.
/// The return button hides the overlay (no scene loading).
/// </summary>
public class OptionsMenuUI : MonoBehaviour
{
    [Header("Background")]
    [SerializeField] private UnityEngine.UI.RawImage backgroundImage;

    [Header("UI References")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Button returnButton;

    [Header("Game UI Reference")]
    [Tooltip("Optional Canvas to hide when the Options menu is open in-game.")]
    [SerializeField] private GameObject gameCanvas;

    private void OnEnable()
    {
        // Re-bind every time the panel is shown (in case references were lost)
        AutoBindReferences();
        ConfigureUI();

        // Hide GameCanvas if assigned
        if (gameCanvas != null)
        {
            gameCanvas.SetActive(false);
        }
    }

    private void AutoBindReferences()
    {
        if (musicSlider == null)
            musicSlider = transform.Find("Panel/MusicSlider")?.GetComponent<Slider>()
                       ?? FindInChildren<Slider>("MusicSlider");

        if (sfxSlider == null)
            sfxSlider = transform.Find("Panel/SfxSlider")?.GetComponent<Slider>()
                     ?? FindInChildren<Slider>("SfxSlider");

        if (returnButton == null)
            returnButton = FindInChildren<Button>("ReturnButton");

        if (backgroundImage == null)
            backgroundImage = FindInChildren<RawImage>("Background");

        // Try to find GameCanvas if not assigned
        if (gameCanvas == null)
        {
            var uiRoot = GameObject.Find("--- UI ---");
            if (uiRoot != null)
            {
                var canvas = uiRoot.transform.Find("GameCanvas");
                if (canvas != null) gameCanvas = canvas.gameObject;
            }
        }
    }

    private T FindInChildren<T>(string childName) where T : Component
    {
        foreach (var t in GetComponentsInChildren<T>(true))
        {
            if (t.gameObject.name == childName)
                return t;
        }
        return null;
    }

    private void ConfigureUI()
    {
        if (musicSlider != null)
        {
            musicSlider.onValueChanged.RemoveListener(HandleMusicChanged);
            musicSlider.SetValueWithoutNotify(AudioSettingsStore.MusicVolume);
            musicSlider.onValueChanged.AddListener(HandleMusicChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveListener(HandleSfxChanged);
            sfxSlider.SetValueWithoutNotify(AudioSettingsStore.SfxVolume);
            sfxSlider.onValueChanged.AddListener(HandleSfxChanged);
        }

        if (returnButton != null)
        {
            returnButton.onClick.RemoveListener(HandleReturnClicked);
            returnButton.onClick.AddListener(HandleReturnClicked);
        }
    }

    private void OnDisable()
    {
        if (musicSlider != null)
            musicSlider.onValueChanged.RemoveListener(HandleMusicChanged);
        if (sfxSlider != null)
            sfxSlider.onValueChanged.RemoveListener(HandleSfxChanged);
        if (returnButton != null)
            returnButton.onClick.RemoveListener(HandleReturnClicked);
    }

    // --- Event Handlers ---

    private void HandleMusicChanged(float value)
    {
        AudioSettingsStore.MusicVolume = value;
        if (AudioManager.Instance != null)
            AudioManager.Instance.ApplyVolumes();
    }

    private void HandleSfxChanged(float value)
    {
        AudioSettingsStore.SfxVolume = value;
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ApplyVolumes();
            // Play a click so the user hears the new volume level
            AudioManager.Instance.PlayButtonClick();
        }
    }

    private void HandleReturnClicked()
    {
        // Play click SFX
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();

        // Restore GameCanvas if we had hidden it
        if (gameCanvas != null)
        {
            // Only restore if we are actually in the game scene and not the main menu!
            // Wait, we need to be careful: if we are in MainMenu, MainMenuUI will handle it.
            // But we can just check if GameCanvas is meant to be active?
            // Let's just activate it if MainMenu_Panel is NOT active.
            var mainMenu = GameObject.Find("MainMenu_Panel");
            bool mainMenuIsActive = mainMenu != null && mainMenu.activeSelf;
            
            if (!mainMenuIsActive)
            {
                gameCanvas.SetActive(true);
            }
        }

        // Hide this overlay panel
        gameObject.SetActive(false);
    }
}