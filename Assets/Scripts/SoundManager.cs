using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Singleton that generates and plays procedural SFX for UI buttons and card plays.
/// No audio files required — all clips are synthesised on Awake.
///
/// Setup (one-time):
///   1. Add this component to any persistent GameObject in Main.unity (e.g. a "SoundManager" GameObject).
///   2. That's it — it auto-wires DeckManager.OnCardPlayed and all Buttons currently in the scene.
///
/// Respects the existing SFX volume setting via AudioChannel / AudioVolumeApplicator.
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { _staticInstance = null; }
    private static SoundManager _staticInstance;

    /// <summary>
    /// Auto-creates the SoundManager once at startup and keeps it alive across scene reloads
    /// (DontDestroyOnLoad). Card sounds use a direct static call from DeckManager so they work
    /// without any re-subscription. Button sounds are re-hooked via SceneManager.sceneLoaded.
    /// NOTE: [RuntimeInitializeOnLoadMethod(AfterSceneLoad)] fires only once per app session,
    /// not on every SceneManager.LoadScene — so DontDestroyOnLoad is essential here.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (_staticInstance != null) return;
        var go = new GameObject("SoundManager");
        Object.DontDestroyOnLoad(go);
        go.AddComponent<SoundManager>();
    }

    private AudioSource _src;

    // Generated clips
    private AudioClip _clickClip;
    private AudioClip _cardClip;
    private AudioClip _maneuverClip;
    private AudioClip _crisisClip;

    private const int Rate = 44100;

    // ─── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (_staticInstance != null && _staticInstance != this)
        {
            Destroy(gameObject);
            return;
        }
        _staticInstance = this;
        Instance = this;

        // AudioSource used for all one-shots
        _src = gameObject.AddComponent<AudioSource>();
        _src.playOnAwake = false;
        _src.spatialBlend = 0f;
        _src.volume = 1f;

        // Mark as SFX so the options-menu volume slider applies
        var ch = gameObject.AddComponent<AudioChannel>();
        ch.channel = AudioChannel.Kind.Sfx;

        BuildAllClips();
        AudioVolumeApplicator.ApplyAll();

        // Re-hook buttons after every scene load (buttons are new objects each reload)
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        HookButtons();
    }

    private void OnDestroy()
    {
        if (_staticInstance == this)
            _staticInstance = null;

        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // Buttons are freshly instantiated after a scene reload — re-wire them all
        HookButtons();
    }

    // ─── Public static API ──────────────────────────────────────────────────────

    /// <summary>Called directly by DeckManager.TryPlayCard — no event subscription needed.</summary>
    public static void PlayCardSound(CardData card)
    {
        if (_staticInstance == null || card == null) return;
        _staticInstance.HandleCardPlayed(card);
    }

    /// <summary>Call this directly from any Button.onClick if it was added after scene start.</summary>
    public static void PlayClick() => _staticInstance?.Play(_staticInstance._clickClip);

    // ─── Internal ───────────────────────────────────────────────────────────────

    private void HookButtons()
    {
        var buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var btn in buttons)
        {
            // Avoid adding duplicate listeners if Start is somehow re-entered
            btn.onClick.RemoveListener(PlayClick);
            btn.onClick.AddListener(PlayClick);
        }
        Debug.Log($"[SoundManager] Hooked {buttons.Length} button(s) for click SFX.");
    }

    private void HandleCardPlayed(CardData card)
    {
        if (card == null) return;

        if (card.category == CardData.CardCategory.Crisis)
            Play(_crisisClip);
        else if (card.category == CardData.CardCategory.Maneuver)
            Play(_maneuverClip);
        else
            Play(_cardClip);
    }

    private void Play(AudioClip clip)
    {
        if (clip != null)
            _src.PlayOneShot(clip);
    }

    // ─── Procedural clip generation ─────────────────────────────────────────────

    private void BuildAllClips()
    {
        _clickClip    = MakeButtonClick();
        _cardClip     = MakeCardPlay();
        _maneuverClip = MakeManeuverPlay();
        _crisisClip   = MakeCrisisPlay();
    }

    // Short, crisp tick: high-frequency sine with fast decay + tiny noise.
    private AudioClip MakeButtonClick()
    {
        int n = (int)(Rate * 0.07f);    // 70 ms
        float[] d = new float[n];
        float phase = 0f;

        for (int i = 0; i < n; i++)
        {
            float t   = (float)i / Rate;
            float env = Mathf.Exp(-t * 65f);
            // Slight pitch drop adds "solidity"
            float freq = 950f + 350f * Mathf.Exp(-t * 90f);
            phase += 2f * Mathf.PI * freq / Rate;
            float noise = (Random.value * 2f - 1f) * 0.05f;
            d[i] = env * (Mathf.Sin(phase) * 0.45f + noise);
        }
        return Bake("UI_Click", d);
    }

    // Soft swoosh with a card-texture noise burst — data / analysis cards.
    private AudioClip MakeCardPlay()
    {
        int n = (int)(Rate * 0.22f);    // 220 ms
        float[] d = new float[n];
        float phase = 0f;

        for (int i = 0; i < n; i++)
        {
            float t      = (float)i / Rate;
            float attack = Mathf.Min(t / 0.006f, 1f);   // 6 ms attack
            float decay  = Mathf.Exp(-t * 11f);
            float env    = attack * decay;

            // Descending pitch sweep: card-placement feel
            float freq = 280f + 480f * Mathf.Exp(-t * 20f);
            phase += 2f * Mathf.PI * freq / Rate;

            float noise = (Random.value * 2f - 1f);
            float nEnv  = Mathf.Exp(-t * 28f) * 0.28f;  // noise fades fast

            d[i] = env * (Mathf.Sin(phase) * 0.38f + noise * nEnv);
        }
        return Bake("Card_Play", d);
    }

    // Quick upward "zip" — action / maneuver cards.
    private AudioClip MakeManeuverPlay()
    {
        int n = (int)(Rate * 0.20f);    // 200 ms
        float[] d = new float[n];
        float phase = 0f;

        for (int i = 0; i < n; i++)
        {
            float t      = (float)i / Rate;
            float attack = Mathf.Min(t / 0.007f, 1f);
            float decay  = Mathf.Exp(-t * 13f);
            float env    = attack * decay;

            // Rise then fall — sense of propulsion
            const float peak = 0.018f;
            float freq;
            if (t < peak)
                freq = 350f + (700f * (t / peak));
            else
                freq = 1050f * Mathf.Exp(-(t - peak) * 14f) + 220f;

            phase += 2f * Mathf.PI * freq / Rate;
            float noise = (Random.value * 2f - 1f) * 0.07f * Mathf.Exp(-t * 35f);

            d[i] = env * (Mathf.Sin(phase) * 0.50f + noise);
        }
        return Bake("Maneuver_Play", d);
    }

    // Low ominous rumble with noise burst — crisis cards.
    private AudioClip MakeCrisisPlay()
    {
        int n = (int)(Rate * 0.32f);    // 320 ms
        float[] d = new float[n];
        float ph1 = 0f, ph2 = 0f;

        for (int i = 0; i < n; i++)
        {
            float t   = (float)i / Rate;
            float env = Mathf.Exp(-t * 6.5f);

            // Low fundamental with slight tremolo
            float tremolo = 1f + 0.04f * Mathf.Sin(2f * Mathf.PI * 7f * t);
            float freq1   = 140f + 90f * Mathf.Exp(-t * 9f);
            ph1 += 2f * Mathf.PI * freq1 * tremolo / Rate;

            // Slightly detuned second partial — adds tension
            float freq2 = freq1 * 2.07f;
            ph2 += 2f * Mathf.PI * freq2 / Rate;

            // Noise burst at onset
            float noise = (Random.value * 2f - 1f);
            float nEnv  = Mathf.Exp(-t * 20f) * 0.38f;

            d[i] = env * (Mathf.Sin(ph1) * 0.33f + Mathf.Sin(ph2) * 0.14f + noise * nEnv);
        }
        return Bake("Crisis_Play", d);
    }

    private static AudioClip Bake(string clipName, float[] samples)
    {
        var clip = AudioClip.Create(clipName, samples.Length, 1, Rate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
