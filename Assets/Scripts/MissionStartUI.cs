using UnityEngine;
using TMPro;

/// <summary>
/// Script for the Mission Start panel.
/// Shows initial briefing and resets resources for a fresh run.
/// </summary>
public class MissionStartUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text briefingText;

    private void Start()
    {
        // Optional: populate briefing text based on GDD or randomized fluff
        if (briefingText != null)
        {
            briefingText.text = "MISSION BRIEFING:\n" +
                               "Target: Psyche Asteroid\n" +
                               "Phase: Cruise Phase\n" +
                               "Goal: Reach Floor 4 and present Scientific Conclusions.\n" +
                               "Resources are finite. Strategic planning is essential.";
        }
    }

    /// <summary>
    /// Closes the briefing and begins the actual encounter.
    /// </summary>
    public void OnStartMission()
    {
        // 1. Reset resources to start state (P:3, B:6, T:15)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetRun(6, 15, 3);
        }

        // 2. Hide this panel
        gameObject.SetActive(false);
        
        // 3. Start the first encounter
        if (EncounterManager.Instance != null)
        {
            // Floor 1: Cruise Phase
            EncounterManager.Instance.StartEncounter("CRUISE PHASE", "Build your deck and gather initial data.", 3, 8);
        }
    }
}
