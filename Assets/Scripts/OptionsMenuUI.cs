using UnityEngine;
using UnityEngine.UI;

public class OptionsMenuUI : MonoBehaviour
{
    [Header("Background")]
    [SerializeField] private UnityEngine.UI.RawImage backgroundImage;

    [Header("UI References")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Button returnButton;

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

        if (backgroundImage == null)
            backgroundImage = transform.Find("Background")?.GetComponent<UnityEngine.UI.RawImage>();
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
}