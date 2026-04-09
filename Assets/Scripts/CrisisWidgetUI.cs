using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CrisisWidgetUI : MonoBehaviour
{
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private Button resolveButton;

    private CardData.EffectType _crisisType;
    private int _costPower;
    private int _costBudget;
    private int _costTime;
    
    private bool _resolvable;

    public CardData.EffectType CrisisType => _crisisType;

    public void Setup(CardData.EffectType crisisType)
    {
        _crisisType = crisisType;
        _resolvable = true;
        _costPower = 0;
        _costBudget = 0;
        _costTime = 0;

        string desc = "";
        string costStr = "";

        switch (crisisType)
        {
            case CardData.EffectType.CrisisSolarStorm:
                desc = "Solar Storm: -2 Time/turn";
                _costPower = 3;
                costStr = "Pay 3 Power";
                break;
            case CardData.EffectType.CrisisThrusterTax:
                desc = "Thruster Anomaly: Maneuvers cost +1 Power";
                _costBudget = 2; _costTime = 2;
                costStr = "Pay 2 Budget, 2 Time";
                break;
            case CardData.EffectType.CrisisBlockDrawOnce:
                desc = "Ground Station Conflict: Skip next draw";
                _costBudget = 2;
                costStr = "Pay 2 Budget";
                break;
            case CardData.EffectType.CrisisBlockDataCollection:
                desc = "Data Storage Full: Cannot collect data";
                _costTime = 2;
                costStr = "Pay 2 Time";
                break;
            case CardData.EffectType.CrisisBlockNextManeuver:
                desc = "Debris Field: Next Maneuver fails";
                _costPower = 3; _costBudget = 1;
                costStr = "Pay 3 Power, 1 Budget";
                break;
            case CardData.EffectType.CrisisComputerReboot:
                desc = "Computer Reboot: Skip next turn";
                _costPower = 4;
                costStr = "Pay 4 Power";
                break;
            case CardData.EffectType.CrisisBudgetCut:
                desc = "Budget Cut: Max Budget reduced";
                _costTime = 3;
                costStr = "Pay 3 Time (Restore 2)";
                break;
            default:
                _resolvable = false;
                desc = "Unknown Crisis";
                break;
        }

        if (descriptionText != null) descriptionText.text = desc;
        if (costText != null) costText.text = costStr;
        
        if (!_resolvable)
        {
            if (resolveButton != null) resolveButton.gameObject.SetActive(false);
        }
        else
        {
            if (resolveButton != null)
            {
                resolveButton.gameObject.SetActive(true);
                resolveButton.onClick.RemoveAllListeners();
                resolveButton.onClick.AddListener(OnResolveClicked);
            }
        }

        RefreshButtonState();
    }

    private void OnEnable()
    {
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.OnResourcesChanged += OnResourcesChanged;
    }

    private void OnDisable()
    {
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.OnResourcesChanged -= OnResourcesChanged;
    }

    private void OnResourcesChanged(int p, int b, int t)
    {
        RefreshButtonState();
    }

    private void RefreshButtonState()
    {
        if (!_resolvable || resolveButton == null || ResourceManager.Instance == null) return;
        resolveButton.interactable = ResourceManager.Instance.CanAfford(_costPower, _costBudget, _costTime);
    }

    private void OnResolveClicked()
    {
        var em = EncounterManager.Instance;
        if (em == null) return;
        
        bool success = false;
        switch (_crisisType)
        {
            case CardData.EffectType.CrisisSolarStorm: success = em.TryResolveSolarStormWithPower(); break;
            case CardData.EffectType.CrisisThrusterTax: success = em.TryResolveThrusterAnomaly(); break;
            case CardData.EffectType.CrisisBlockDrawOnce: success = em.TryResolveGroundStationConflict(); break;
            case CardData.EffectType.CrisisBlockDataCollection: success = em.TryResolveDataStorageFull(); break;
            case CardData.EffectType.CrisisBlockNextManeuver: success = em.TryResolveDebrisField(); break;
            case CardData.EffectType.CrisisComputerReboot: success = em.TryResolveComputerReboot(); break;
            case CardData.EffectType.CrisisBudgetCut: success = em.TryResolveBudgetCutRestore(); break;
        }

        if (success)
        {
            // Destruction is handled by GameUIController listening to OnCrisisResolved
            resolveButton.interactable = false;
        }
    }
}
