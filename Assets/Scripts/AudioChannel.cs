using UnityEngine;

/// <summary>
/// Marks an <see cref="AudioSource"/> as music vs SFX for <see cref="AudioVolumeApplicator"/>.
/// Add to music sources; unmarked sources are treated as SFX.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AudioChannel : MonoBehaviour
{
    public enum Kind
    {
        Sfx,
        Music
    }

    [Tooltip("Music uses the Music slider; everything else uses the SFX slider.")]
    public Kind channel = Kind.Sfx;

    [Tooltip("Designer volume before user prefs (captured at runtime if 0).")]
    [SerializeField] private float designVolume = -1f;

    private void Awake()
    {
        var src = GetComponent<AudioSource>();
        if (src == null)
            return;
        if (designVolume < 0f)
            designVolume = src.volume;
    }

    public float GetDesignVolume(AudioSource src)
    {
        if (designVolume >= 0f)
            return designVolume;
        return src != null ? src.volume : 1f;
    }
}
