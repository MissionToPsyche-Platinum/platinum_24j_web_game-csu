using UnityEngine;
using TMPro;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }
    public System.Action<int, int, int> OnResourcesChanged;

    [Header("Resource Values")]
    public int power = 3;
    public int budget = 6;
    public int time = 15;

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
        OnResourcesChanged?.Invoke(power, budget, time);
    }

    public void EndTurn() 
    {
        time -= 1; 
        power = 5; 
        UpdateUI();
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