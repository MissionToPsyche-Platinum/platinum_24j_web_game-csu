using UnityEngine;
using UnityEngine.UI;

public class OptionsMenuUI : MonoBehaviour
{
    private enum TabType
    {
        Audio,
        HowToPlay
    }

    [Header("Background")]
    [SerializeField] private RawImage backgroundImage;

    [Header("UI References")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Button returnButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Text optionsTitleText;

    [Header("Tabs")]
    [SerializeField] private Button audioTabButton;
    [SerializeField] private Button howToPlayTabButton;
    [SerializeField] private GameObject[] audioTabObjects;
    [SerializeField] private GameObject howToPlayPanel;

    [Header("Tab Colors")]
    [SerializeField] private Color selectedTabColor = new Color(0.28f, 0.4f, 0.75f, 1f);
    [SerializeField] private Color selectedTabHighlightColor = new Color(0.34f, 0.46f, 0.82f, 1f);
    [SerializeField] private Color selectedTabPressedColor = new Color(0.24f, 0.35f, 0.66f, 1f);
    [SerializeField] private Color unselectedTabColor = new Color(1f, 1f, 1f, 0.92f);
    [SerializeField] private Color unselectedTabHighlightColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color unselectedTabPressedColor = new Color(0.87f, 0.9f, 0.98f, 1f);
    [SerializeField] private Color selectedTabTextColor = Color.white;
    [SerializeField] private Color unselectedTabTextColor = new Color(0.12f, 0.18f, 0.33f, 1f);

    private void Awake()
    {
        // Automatically attempt to find references if they are missing in the Inspector
        AutoBindReferences();
        
        // Setup initial values and listeners
        ConfigureUI();
    }

    private void AutoBindReferences()
    {
        // We use transform.Find for specific names to avoid picking up the wrong slider
        if (musicSlider == null) 
            musicSlider = transform.Find("MusicSlider")?.GetComponent<Slider>();
        
        if (sfxSlider == null) 
            sfxSlider = transform.Find("SfxSlider")?.GetComponent<Slider>();
        
        if (returnButton == null) 
            returnButton = transform.Find("ReturnButton")?.GetComponent<Button>();

        if (mainMenuButton == null)
            mainMenuButton = transform.Find("MainMenuButton")?.GetComponent<Button>();

        if (optionsTitleText == null)
            optionsTitleText = transform.Find("OptionsTitle")?.GetComponent<Text>();

        if (backgroundImage == null)
            backgroundImage = transform.Find("Background")?.GetComponent<RawImage>();

        if (audioTabButton == null)
            audioTabButton = transform.Find("AudioTabButton")?.GetComponent<Button>();

        if (howToPlayTabButton == null)
            howToPlayTabButton = transform.Find("HowToPlayTabButton")?.GetComponent<Button>();

        if (howToPlayPanel == null)
            howToPlayPanel = transform.Find("HowToPlayPanel")?.gameObject;

        if (audioTabObjects == null || audioTabObjects.Length == 0)
        {
            audioTabObjects = new[]
            {
                transform.Find("MusicLabel")?.gameObject,
                transform.Find("MusicSlider")?.gameObject,
                transform.Find("SfxLabel")?.gameObject,
                transform.Find("SfxSlider")?.gameObject
            };
        }
    }

    private void ConfigureUI()
    {
        // Setup Music Slider
        if (musicSlider != null)
        {
            // Use SetValueWithoutNotify to avoid triggering the 'Save' logic on Awake
            musicSlider.SetValueWithoutNotify(AudioSettingsStore.MusicVolume);
            musicSlider.onValueChanged.AddListener(HandleMusicChanged);
        }

        // Setup SFX Slider
        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(AudioSettingsStore.SfxVolume);
            sfxSlider.onValueChanged.AddListener(HandleSfxChanged);
        }

        // Setup Return Button
        if (returnButton != null)
        {
            returnButton.onClick.AddListener(HandleReturnClicked);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(HandleMainMenuClicked);
        }

        if (audioTabButton != null)
        {
            audioTabButton.onClick.AddListener(HandleAudioTabClicked);
        }

        if (howToPlayTabButton != null)
        {
            howToPlayTabButton.onClick.AddListener(HandleHowToPlayTabClicked);
        }

        SetActiveTab(TabType.Audio);
    }

    private void OnDestroy()
    {
        // Unsubscribe from all events to prevent memory leaks or errors on scene change
        if (musicSlider != null) 
            musicSlider.onValueChanged.RemoveListener(HandleMusicChanged);
        
        if (sfxSlider != null) 
            sfxSlider.onValueChanged.RemoveListener(HandleSfxChanged);
        
        if (returnButton != null) 
            returnButton.onClick.RemoveListener(HandleReturnClicked);

        if (mainMenuButton != null)
            mainMenuButton.onClick.RemoveListener(HandleMainMenuClicked);

        if (audioTabButton != null)
            audioTabButton.onClick.RemoveListener(HandleAudioTabClicked);

        if (howToPlayTabButton != null)
            howToPlayTabButton.onClick.RemoveListener(HandleHowToPlayTabClicked);
    }

    // --- Event Handlers ---

    private void HandleMusicChanged(float value)
    {
        AudioSettingsStore.MusicVolume = value;
        // Optional: Trigger your Audio Manager here to update live music volume
    }

    private void HandleSfxChanged(float value)
    {
        AudioSettingsStore.SfxVolume = value;
        // Optional: Play a small "blip" sound here so the user hears the new volume level
    }

    private void HandleReturnClicked()
    {
        // Navigates back using your project's existing navigation logic
        OptionsNavigation.ReturnToPreviousOrFallback();
    }

    private void HandleMainMenuClicked()
    {
        OptionsNavigation.LoadMainMenu();
    }

    private void HandleAudioTabClicked()
    {
        SetActiveTab(TabType.Audio);
    }

    private void HandleHowToPlayTabClicked()
    {
        SetActiveTab(TabType.HowToPlay);
    }

    private void SetActiveTab(TabType tab)
    {
        bool showAudioTab = tab == TabType.Audio;

        if (audioTabObjects != null)
        {
            foreach (GameObject audioObject in audioTabObjects)
            {
                if (audioObject != null)
                    audioObject.SetActive(showAudioTab);
            }
        }

        if (howToPlayPanel != null)
            howToPlayPanel.SetActive(!showAudioTab);

        if (optionsTitleText != null)
            optionsTitleText.text = showAudioTab ? "Audio Settings" : "How To Play";

        ApplyTabColors(audioTabButton, showAudioTab);
        ApplyTabColors(howToPlayTabButton, !showAudioTab);
    }

    private void ApplyTabColors(Button button, bool isSelected)
    {
        if (button == null)
            return;

        ColorBlock colors = button.colors;

        if (isSelected)
        {
            colors.normalColor = selectedTabColor;
            colors.highlightedColor = selectedTabHighlightColor;
            colors.pressedColor = selectedTabPressedColor;
            colors.selectedColor = selectedTabHighlightColor;
        }
        else
        {
            colors.normalColor = unselectedTabColor;
            colors.highlightedColor = unselectedTabHighlightColor;
            colors.pressedColor = unselectedTabPressedColor;
            colors.selectedColor = unselectedTabHighlightColor;
        }

        button.colors = colors;
        UpdateTabLabel(button, isSelected ? selectedTabTextColor : unselectedTabTextColor);
    }

    private void UpdateTabLabel(Button button, Color labelColor)
    {
        Text label = button != null ? button.GetComponentInChildren<Text>(true) : null;
        if (label != null)
            label.color = labelColor;
    }
}