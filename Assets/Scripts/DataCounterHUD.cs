using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays per-data-type counters (Surface, Elemental, Magnetic, Gravity, Thermal).
/// Only shown during Floor 4. Builds its own UI overlay at runtime — no Inspector wiring needed.
/// Add to any scene GameObject; call Show() / Hide() from GameUIController.
/// </summary>
public class DataCounterHUD : MonoBehaviour
{
    private GameObject _panel;
    private TMP_Text _surfaceText;
    private TMP_Text _elementalText;
    private TMP_Text _magneticText;
    private TMP_Text _gravityText;
    private TMP_Text _thermalText;
    private bool _subscribed;

    private void Awake()
    {
        BuildUI();
        if (_panel != null) _panel.SetActive(false);
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    public void Show()
    {
        if (_panel != null) _panel.SetActive(true);
        Subscribe();
        Refresh();
    }

    public void Hide()
    {
        if (_panel != null) _panel.SetActive(false);
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (_subscribed || DataTracker.Instance == null) return;
        DataTracker.Instance.OnDataChanged += Refresh;
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed) return;
        if (DataTracker.Instance != null)
            DataTracker.Instance.OnDataChanged -= Refresh;
        _subscribed = false;
    }

    private void Refresh()
    {
        var dt = DataTracker.Instance;
        if (dt == null) return;
        if (_surfaceText   != null) _surfaceText.text   = $"Surface:   {dt.Surface}";
        if (_elementalText != null) _elementalText.text = $"Elemental: {dt.Elemental}";
        if (_magneticText  != null) _magneticText.text  = $"Magnetic:  {dt.Magnetic}";
        if (_gravityText   != null) _gravityText.text   = $"Gravity:   {dt.Gravity}";
        if (_thermalText   != null) _thermalText.text   = $"Thermal:   {dt.Thermal}";
    }

    private void BuildUI()
    {
        var canvasGo = GameObject.Find("Psyche_AutoCanvas") ?? GameObject.Find("GameCanvas");
        if (canvasGo == null)
        {
            Debug.LogWarning("[DataCounterHUD] No canvas found — panel will not appear.");
            return;
        }

        _panel = new GameObject("DataCounterHUD_Panel");
        _panel.transform.SetParent(canvasGo.transform, false);

        var panelRT = _panel.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(1f, 0f);
        panelRT.anchorMax = new Vector2(1f, 0f);
        panelRT.pivot     = new Vector2(1f, 0f);
        // Sit just above the hand area in the bottom-right corner
        panelRT.anchoredPosition = new Vector2(-12f, 100f);
        panelRT.sizeDelta        = new Vector2(190f, 150f);

        var bg = _panel.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.15f, 0.82f);

        MakeLabel(_panel.transform, "DATA COLLECTED", 0f, 126f, 13, new Color(0.4f, 0.85f, 1f));

        float rowHeight = 22f;
        float startY    = 98f;
        _surfaceText   = MakeLabel(_panel.transform, "Surface:   0", 0f, startY - rowHeight * 0, 12, Color.white);
        _elementalText = MakeLabel(_panel.transform, "Elemental: 0", 0f, startY - rowHeight * 1, 12, Color.white);
        _magneticText  = MakeLabel(_panel.transform, "Magnetic:  0", 0f, startY - rowHeight * 2, 12, Color.white);
        _gravityText   = MakeLabel(_panel.transform, "Gravity:   0", 0f, startY - rowHeight * 3, 12, Color.white);
        _thermalText   = MakeLabel(_panel.transform, "Thermal:   0", 0f, startY - rowHeight * 4, 12, Color.white);
    }

    private static TMP_Text MakeLabel(Transform parent, string text, float x, float y, int fontSize, Color color)
    {
        var go = new GameObject(text.Split(':')[0].Trim());
        go.transform.SetParent(parent, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin       = new Vector2(0f, 0f);
        rt.anchorMax       = new Vector2(1f, 0f);
        rt.pivot           = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta       = new Vector2(0f, 20f);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.color     = color;
        tmp.alignment = TextAlignmentOptions.Center;
        return tmp;
    }
}
