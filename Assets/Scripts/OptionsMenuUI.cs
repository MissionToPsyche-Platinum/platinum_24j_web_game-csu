using UnityEngine;
using UnityEngine.UI;

public class OptionsMenuUI : MonoBehaviour
{
    [Header("UI References")]
    public Slider musicSlider;
    public Slider sfxSlider;
    public Button returnButton;

    private void Awake()
    {
        AutoBindIfNeeded();

        if (musicSlider != null)
        {
            musicSlider.SetValueWithoutNotify(AudioSettingsStore.MusicVolume);
            musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
            musicSlider.onValueChanged.AddListener(OnMusicChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(AudioSettingsStore.SfxVolume);
            sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);
            sfxSlider.onValueChanged.AddListener(OnSfxChanged);
        }

        if (returnButton != null)
        {
            returnButton.onClick.RemoveListener(OnReturnClicked);
            returnButton.onClick.AddListener(OnReturnClicked);
        }
    }

    private void OnDestroy()
    {
        if (musicSlider != null)
            musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
        if (sfxSlider != null)
            sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);
        if (returnButton != null)
            returnButton.onClick.RemoveListener(OnReturnClicked);
    }

    private static void OnMusicChanged(float value)
    {
        AudioSettingsStore.MusicVolume = value;
    }

    private static void OnSfxChanged(float value)
    {
        AudioSettingsStore.SfxVolume = value;
    }

    private static void OnReturnClicked()
    {
        OptionsNavigation.ReturnToPreviousOrFallback();
    }

    private void AutoBindIfNeeded()
    {
        if (musicSlider == null)
        {
            var musicGo = GameObject.Find("MusicSlider");
            if (musicGo != null)
                musicSlider = musicGo.GetComponent<Slider>();
        }

        if (sfxSlider == null)
        {
            var sfxGo = GameObject.Find("SfxSlider");
            if (sfxGo != null)
                sfxSlider = sfxGo.GetComponent<Slider>();
        }

        if (returnButton == null)
        {
            var returnGo = GameObject.Find("ReturnButton");
            if (returnGo != null)
                returnButton = returnGo.GetComponent<Button>();
        }
    }
}

