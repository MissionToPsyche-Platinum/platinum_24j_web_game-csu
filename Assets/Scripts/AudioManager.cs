using UnityEngine;

/// <summary>
/// Singleton Audio Manager that persists across scenes.
/// Handles background music (BGM) and UI sound effects (SFX).
/// Reads volume from AudioSettingsStore (PlayerPrefs-backed).
///
/// Setup: Attach to an empty GameObject in your first scene,
/// or use the editor tool: Tools > Setup Audio Manager.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Clips")]
    [SerializeField] private AudioClip bgmClip;       // Assign Airlock.mp3
    [SerializeField] private AudioClip buttonClickClip; // Assign generated click

    private void Awake()
    {
        // Singleton: destroy duplicates
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Create audio sources if not assigned
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
        }
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }
    }

    private void Start()
    {
        ApplyVolumes();
        PlayBGM();
    }

    /// <summary>
    /// Applies the saved volume levels from AudioSettingsStore.
    /// Call this whenever the user changes volume sliders.
    /// </summary>
    public void ApplyVolumes()
    {
        if (bgmSource != null) bgmSource.volume = AudioSettingsStore.MusicVolume;
        if (sfxSource != null) sfxSource.volume = AudioSettingsStore.SfxVolume;
    }

    /// <summary>
    /// Starts or restarts the background music.
    /// </summary>
    public void PlayBGM()
    {
        if (bgmSource == null || bgmClip == null) return;

        if (!bgmSource.isPlaying || bgmSource.clip != bgmClip)
        {
            bgmSource.clip = bgmClip;
            bgmSource.Play();
        }
    }

    /// <summary>
    /// Stops the background music.
    /// </summary>
    public void StopBGM()
    {
        if (bgmSource != null) bgmSource.Stop();
    }

    /// <summary>
    /// Plays the button click sound effect.
    /// Call this from any UI button's onClick event.
    /// </summary>
    public void PlayButtonClick()
    {
        if (sfxSource != null && buttonClickClip != null)
            sfxSource.PlayOneShot(buttonClickClip, AudioSettingsStore.SfxVolume);
    }

    /// <summary>
    /// Plays an arbitrary sound effect.
    /// </summary>
    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
            sfxSource.PlayOneShot(clip, AudioSettingsStore.SfxVolume);
    }
}
