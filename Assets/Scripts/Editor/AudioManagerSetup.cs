using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Editor utility to set up the AudioManager in the active scene.
/// Generates a procedural button click SFX and wires up the BGM clip.
/// Run via: Tools > Setup Audio Manager
/// </summary>
public static class AudioManagerSetup
{
    private const string BgmPath = "Assets/Audio/Airlock.mp3";
    private const string ClickPath = "Assets/Audio/ButtonClick.wav";

    [MenuItem("Tools/Setup Audio Manager")]
    public static void Setup()
    {
        // --- 1. Generate button click SFX if it doesn't exist ---
        AudioClip clickClip = AssetDatabase.LoadAssetAtPath<AudioClip>(ClickPath);
        if (clickClip == null)
        {
            GenerateButtonClick();
            AssetDatabase.Refresh();
            clickClip = AssetDatabase.LoadAssetAtPath<AudioClip>(ClickPath);
        }

        // --- 2. Load BGM clip ---
        AudioClip bgmClip = AssetDatabase.LoadAssetAtPath<AudioClip>(BgmPath);
        if (bgmClip == null)
        {
            Debug.LogWarning("[AudioManagerSetup] BGM not found at: " + BgmPath);
        }

        // --- 3. Find or create AudioManager GameObject ---
        AudioManager existing = Object.FindObjectOfType<AudioManager>();
        GameObject audioGO;

        if (existing != null)
        {
            audioGO = existing.gameObject;
            Debug.Log("[AudioManagerSetup] Found existing AudioManager, updating references.");
        }
        else
        {
            audioGO = new GameObject("--- AudioManager ---");
            Undo.RegisterCreatedObjectUndo(audioGO, "Create AudioManager");
            audioGO.AddComponent<AudioManager>();
            Debug.Log("[AudioManagerSetup] Created new AudioManager GameObject.");
        }

        // --- 4. Set up AudioSource components ---
        AudioSource[] sources = audioGO.GetComponents<AudioSource>();

        AudioSource bgmSource;
        AudioSource sfxSource;

        if (sources.Length >= 2)
        {
            bgmSource = sources[0];
            sfxSource = sources[1];
        }
        else if (sources.Length == 1)
        {
            bgmSource = sources[0];
            sfxSource = Undo.AddComponent<AudioSource>(audioGO);
        }
        else
        {
            bgmSource = Undo.AddComponent<AudioSource>(audioGO);
            sfxSource = Undo.AddComponent<AudioSource>(audioGO);
        }

        // Configure BGM source
        bgmSource.clip = bgmClip;
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
        bgmSource.volume = AudioSettingsStore.MusicVolume;

        // Configure SFX source
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        sfxSource.volume = AudioSettingsStore.SfxVolume;

        // --- 5. Wire references on AudioManager via SerializedObject ---
        var manager = audioGO.GetComponent<AudioManager>();
        SerializedObject so = new SerializedObject(manager);
        so.FindProperty("bgmSource").objectReferenceValue = bgmSource;
        so.FindProperty("sfxSource").objectReferenceValue = sfxSource;
        so.FindProperty("bgmClip").objectReferenceValue = bgmClip;
        so.FindProperty("buttonClickClip").objectReferenceValue = clickClip;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(audioGO);
        EditorSceneManager.MarkSceneDirty(audioGO.scene);

        Debug.Log($"[AudioManagerSetup] Done! BGM: {(bgmClip != null ? bgmClip.name : "MISSING")}, " +
                  $"Click SFX: {(clickClip != null ? clickClip.name : "MISSING")}");
    }

    /// <summary>
    /// Generates a short procedural button click WAV file.
    /// A quick "tick" sound: short attack, fast decay, slight pitch sweep.
    /// </summary>
    private static void GenerateButtonClick()
    {
        int sampleRate = 44100;
        float duration = 0.08f; // 80ms — snappy click
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float normalizedT = (float)i / sampleCount;

            // Envelope: sharp attack, fast exponential decay
            float envelope = Mathf.Exp(-normalizedT * 12f);

            // Two tones blended: a high tick + a lower thud
            float highFreq = 3200f - (normalizedT * 800f); // pitch sweep down
            float lowFreq = 800f;

            float high = Mathf.Sin(2f * Mathf.PI * highFreq * t) * 0.6f;
            float low = Mathf.Sin(2f * Mathf.PI * lowFreq * t) * 0.4f;

            // Add a tiny noise burst at the start for "click" texture
            float noise = (normalizedT < 0.15f) ? (Random.value * 2f - 1f) * 0.3f * (1f - normalizedT / 0.15f) : 0f;

            samples[i] = (high + low + noise) * envelope * 0.85f;
        }

        // Write as WAV
        byte[] wavData = EncodeWAV(samples, sampleRate, 1);

        // Ensure directory exists
        string dir = System.IO.Path.GetDirectoryName(
            System.IO.Path.Combine(Application.dataPath, "..", ClickPath));
        if (!System.IO.Directory.Exists(dir))
            System.IO.Directory.CreateDirectory(dir);

        string fullPath = System.IO.Path.Combine(Application.dataPath, "..", ClickPath);
        System.IO.File.WriteAllBytes(fullPath, wavData);
        Debug.Log($"[AudioManagerSetup] Generated button click SFX at: {ClickPath} ({sampleCount} samples)");
    }

    private static byte[] EncodeWAV(float[] samples, int sampleRate, int channels)
    {
        int sampleCount = samples.Length;
        int byteRate = sampleRate * channels * 2; // 16-bit
        int blockAlign = channels * 2;
        int dataSize = sampleCount * 2;
        int fileSize = 44 + dataSize;

        byte[] wav = new byte[fileSize];
        int pos = 0;

        // RIFF header
        WriteString(wav, ref pos, "RIFF");
        WriteInt(wav, ref pos, fileSize - 8);
        WriteString(wav, ref pos, "WAVE");

        // fmt chunk
        WriteString(wav, ref pos, "fmt ");
        WriteInt(wav, ref pos, 16); // chunk size
        WriteShort(wav, ref pos, 1); // PCM format
        WriteShort(wav, ref pos, (short)channels);
        WriteInt(wav, ref pos, sampleRate);
        WriteInt(wav, ref pos, byteRate);
        WriteShort(wav, ref pos, (short)blockAlign);
        WriteShort(wav, ref pos, 16); // bits per sample

        // data chunk
        WriteString(wav, ref pos, "data");
        WriteInt(wav, ref pos, dataSize);

        for (int i = 0; i < sampleCount; i++)
        {
            short s = (short)(Mathf.Clamp(samples[i], -1f, 1f) * 32767f);
            wav[pos++] = (byte)(s & 0xFF);
            wav[pos++] = (byte)((s >> 8) & 0xFF);
        }

        return wav;
    }

    private static void WriteString(byte[] buffer, ref int pos, string value)
    {
        foreach (char c in value) buffer[pos++] = (byte)c;
    }
    private static void WriteInt(byte[] buffer, ref int pos, int value)
    {
        buffer[pos++] = (byte)(value & 0xFF);
        buffer[pos++] = (byte)((value >> 8) & 0xFF);
        buffer[pos++] = (byte)((value >> 16) & 0xFF);
        buffer[pos++] = (byte)((value >> 24) & 0xFF);
    }
    private static void WriteShort(byte[] buffer, ref int pos, short value)
    {
        buffer[pos++] = (byte)(value & 0xFF);
        buffer[pos++] = (byte)((value >> 8) & 0xFF);
    }
}
