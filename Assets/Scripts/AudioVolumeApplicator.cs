using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Applies <see cref="AudioSettingsStore"/> music/SFX levels to all <see cref="AudioSource"/> instances.
/// Uses optional <see cref="AudioChannel"/> per source; unmarked sources count as SFX.
/// </summary>
public static class AudioVolumeApplicator
{
    private static readonly Dictionary<AudioSource, float> BaseVolumes = new Dictionary<AudioSource, float>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ApplyOnBoot()
    {
        ApplyAll();
    }

    public static void ApplyAll()
    {
        PruneDestroyedSources();

        float mv = AudioSettingsStore.MusicVolume;
        float sv = AudioSettingsStore.SfxVolume;

        var sources = Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var src in sources)
        {
            if (src == null)
                continue;

            var marker = src.GetComponent<AudioChannel>();
            bool isMusic = marker != null && marker.channel == AudioChannel.Kind.Music;
            float mult = isMusic ? mv : sv;

            if (!BaseVolumes.TryGetValue(src, out float baseVol))
            {
                baseVol = marker != null ? marker.GetDesignVolume(src) : src.volume;
                BaseVolumes[src] = baseVol;
            }

            src.volume = Mathf.Clamp01(baseVol * mult);
        }
    }

    private static void PruneDestroyedSources()
    {
        foreach (var k in new List<AudioSource>(BaseVolumes.Keys))
        {
            if (k == null)
                BaseVolumes.Remove(k);
        }
    }

    /// <summary>Clears cached base volumes (e.g. after destroying AudioSources).</summary>
    public static void ClearCache()
    {
        BaseVolumes.Clear();
    }
}
