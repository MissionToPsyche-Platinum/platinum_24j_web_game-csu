using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple component that toggles the OptionsOverlay on/off.
/// Attach to any button. On click, it finds and shows the OptionsOverlay.
/// Also plays the button click SFX.
/// </summary>
[RequireComponent(typeof(Button))]
public class OptionsToggle : MonoBehaviour
{
    private GameObject overlay;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(ToggleOptions);
    }

    private void OnDestroy()
    {
        var btn = GetComponent<Button>();
        if (btn != null) btn.onClick.RemoveListener(ToggleOptions);
    }

    public void ToggleOptions()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();

        if (overlay == null)
        {
            // Find even if inactive
            foreach (var root in gameObject.scene.GetRootGameObjects())
            {
                if (root.name == "OptionsCanvas" || root.name == "OptionsOverlay") { overlay = root; break; }
            }
            
            // If still not found, check under --- UI ---
            if (overlay == null)
            {
                var uiRoot = GameObject.Find("--- UI ---");
                if (uiRoot != null)
                {
                    var canvas = uiRoot.transform.Find("OptionsCanvas");
                    if (canvas != null) overlay = canvas.gameObject;
                }
            }
        }

        if (overlay != null)
            overlay.SetActive(!overlay.activeSelf);
        else
            Debug.LogWarning("[OptionsToggle] Could not find OptionsCanvas or OptionsOverlay in the scene.");
    }
}
