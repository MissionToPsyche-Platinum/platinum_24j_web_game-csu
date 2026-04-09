using UnityEngine;

public static class AudioSettingsStore
{
    private const string MusicKey = "Options.MusicVolume";
    private const string SfxKey = "Options.SfxVolume";

    public static float MusicVolume
    {
        get => PlayerPrefs.GetFloat(MusicKey, 0.8f);
        set
        {
            PlayerPrefs.SetFloat(MusicKey, Mathf.Clamp01(value));
            PlayerPrefs.Save();
            AudioVolumeApplicator.ApplyAll();
        }
    }

    public static float SfxVolume
    {
        get => PlayerPrefs.GetFloat(SfxKey, 0.8f);
        set
        {
            PlayerPrefs.SetFloat(SfxKey, Mathf.Clamp01(value));
            PlayerPrefs.Save();
            AudioVolumeApplicator.ApplyAll();
        }
    }
}

