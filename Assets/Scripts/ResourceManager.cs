using UnityEngine;
using TMPro;

public class ResourceManager : MonoBehaviour
{
    // 1. The "Instance" that ResourceHUD is looking for
    public static ResourceManager Instance { get; private set; }

    // 2. The event that tells the HUD to refresh
    public static ResourceManager Instance { get; private set; }
    public System.Action<int, int, int> OnResourcesChanged;

    [Header("Resource Values")]
    public int power = 3;
    public int budget = 6;
    public int time = 15;

    // These properties help other scripts read the values correctly
    public int Power => power;
    public int Budget => budget;
    public int TimeRemaining => time;

    [Header("UI References")]
    public TextMeshProUGUI powerText;
    public TextMeshProUGUI budgetText;
    public TextMeshProUGUI timeText;

    private void Awake()
    {
        // Setup the Singleton so ResourceHUD.cs can find this script
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    public int Power => power;
    public int Budget => budget;
    public int TimeRemaining => time;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void UpdateUI()
    {
        if(powerText != null) powerText.text = "Power: " + power;
        if(budgetText != null) budgetText.text = "Budget: " + budget;
        if(timeText != null) timeText.text = "Time: " + time;
        
        // Notify other scripts (like ResourceHUD) that numbers changed
        OnResourcesChanged?.Invoke(power, budget, time);
    }

    public void EndTurn() 
    {
        time -= 1; 
        power = 5; 
        UpdateUI();
        if (time <= 0) Debug.Log("Mission Failed: Out of Time!");
    }

    // This fix is for the 'PsycheBootstrap' error in your console
    public void ResetForNewRun()
    {
        power = 3;
        budget = 6;
        time = 15;
        UpdateUI();
    }

    // This fix is for the 'CanAfford' error if you bring back DeckManager
    public bool CanAfford(int p, int b, int t)
    {
        return power >= p && budget >= b && time >= t;
    }

    public void TrySpend(int p, int b, int t)
    {
        power -= p;
        budget -= b;
        time -= t;
        if (time <= 0) Debug.Log("Mission Failed!");
    }

    public void ResetForNewRun() { power = 3; budget = 6; time = 15; UpdateUI(); }
    
    public bool CanAfford(int p, int b, int t) => power >= p && budget >= b && time >= t;

    public void TrySpend(int p, int b, int t)
    {
        power -= p; budget -= b; time -= t;
        UpdateUI();
    }
}