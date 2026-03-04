using UnityEngine;
using TMPro; // This line is crucial for UI!

public class ResourceManager : MonoBehaviour
{
    public int power = 3;
    public int budget = 6;
    public int time = 15;

    [Header("UI References")]
    public TextMeshProUGUI powerText;
    public TextMeshProUGUI budgetText;
    public TextMeshProUGUI timeText;

    void Start()
    {
        UpdateUI();
    }

    // This updates the text on the screen to match our numbers
    public void UpdateUI()
    {
        powerText.text = "Power: " + power;
        budgetText.text = "Budget: " + budget;
        timeText.text = "Time: " + time;
    }
    public void EndTurn() {
    time -= 1; // Mission time decreases
    power = 5; // Reset power for the new turn (like recharging batteries)
    UpdateUI();
    if (time <= 0) Debug.Log("Mission Failed: Out of Time!");
}
}