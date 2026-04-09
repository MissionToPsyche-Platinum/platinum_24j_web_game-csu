using UnityEngine;
using System;

/// <summary>
/// Singleton that tracks raw scientific data and conclusions.
/// Instrument cards add data; Analysis cards convert data into conclusions.
/// Design doc data types: Surface, Elemental, Magnetic, Gravity, Thermal.
/// Conclusions: Composition, Dynamo, Interior, Formation.
/// </summary>
public class DataTracker : MonoBehaviour
{
    private static DataTracker _instance;
    public static DataTracker Instance => _instance;

    // Raw data counts
    [Header("Raw Data (read-only in Inspector)")]
    [SerializeField] private int _surface;
    [SerializeField] private int _elemental;
    [SerializeField] private int _magnetic;
    [SerializeField] private int _gravity;
    [SerializeField] private int _thermal;

    // Conclusion counts
    [Header("Conclusions (read-only in Inspector)")]
    [SerializeField] private int _composition;
    [SerializeField] private int _dynamo;
    [SerializeField] private int _interior;
    [SerializeField] private int _formation;
    [SerializeField] private int _conclusionBonuses; // from Peer Review upgrades

    // Public accessors
    public int Surface => _surface;
    public int Elemental => _elemental;
    public int Magnetic => _magnetic;
    public int Gravity => _gravity;
    public int Thermal => _thermal;

    public int Composition => _composition;
    public int Dynamo => _dynamo;
    public int Interior => _interior;
    public int Formation => _formation;

    /// <summary>Total effective conclusions (including upgrades). 3 required for final boss.</summary>
    public int TotalConclusions => _composition + _dynamo + _interior + _formation + _conclusionBonuses;

    /// <summary>Total raw data points collected.</summary>
    public int TotalData => _surface + _elemental + _magnetic + _gravity + _thermal;

    /// <summary>Fired when any data or conclusion value changes.</summary>
    public event Action OnDataChanged;

    private void TriggerDataChanged()
    {
        OnDataChanged?.Invoke();
        EncounterManager.Instance?.CheckBossConditions();
    }

    public enum DataType { Surface, Elemental, Magnetic, Gravity, Thermal }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    // -----------------------------------------------------------------------
    // Data collection
    // -----------------------------------------------------------------------

    /// <summary>Add raw data of the given type.</summary>
    public void AddData(DataType type, int amount)
    {
        switch (type)
        {
            case DataType.Surface:    _surface += amount; break;
            case DataType.Elemental:  _elemental += amount; break;
            case DataType.Magnetic:   _magnetic += amount; break;
            case DataType.Gravity:    _gravity += amount; break;
            case DataType.Thermal:    _thermal += amount; break;
        }
        Debug.Log($"[DataTracker] +{amount} {type} data (total: {GetDataCount(type)})");
        TriggerDataChanged();
    }

    /// <summary>Add 1 of each data type (Multi-Instrument Suite).</summary>
    public void AddAllData(int amount = 1)
    {
        _surface += amount;
        _elemental += amount;
        _magnetic += amount;
        _gravity += amount;
        _thermal += amount;
        Debug.Log($"[DataTracker] +{amount} of each data type");
        TriggerDataChanged();
    }

    public int GetDataCount(DataType type)
    {
        switch (type)
        {
            case DataType.Surface:   return _surface;
            case DataType.Elemental: return _elemental;
            case DataType.Magnetic:  return _magnetic;
            case DataType.Gravity:   return _gravity;
            case DataType.Thermal:   return _thermal;
            default: return 0;
        }
    }

    // -----------------------------------------------------------------------
    // Conclusion synthesis
    // -----------------------------------------------------------------------

    /// <summary>Try Composition: 3 Elemental + 2 Surface → 1 Composition Conclusion.</summary>
    public bool TryComposition()
    {
        if (_elemental >= 3 && _surface >= 2)
        {
            _elemental -= 3; _surface -= 2; _composition++;
            Debug.Log("[DataTracker] Composition Conclusion synthesized!");
            TriggerDataChanged();
            return true;
        }
        Debug.Log("[DataTracker] Not enough data for Composition (need 3 Elemental + 2 Surface)");
        return false;
    }

    /// <summary>Try Dynamo: 4 Magnetic → 1 Dynamo Conclusion.</summary>
    public bool TryDynamo()
    {
        if (_magnetic >= 4)
        {
            _magnetic -= 4; _dynamo++;
            Debug.Log("[DataTracker] Dynamo Conclusion synthesized!");
            TriggerDataChanged();
            return true;
        }
        Debug.Log("[DataTracker] Not enough data for Dynamo (need 4 Magnetic)");
        return false;
    }

    /// <summary>Try Interior: 3 Gravity + 2 Surface → 1 Interior Conclusion.</summary>
    public bool TryInterior()
    {
        if (_gravity >= 3 && _surface >= 2)
        {
            _gravity -= 3; _surface -= 2; _interior++;
            Debug.Log("[DataTracker] Interior Conclusion synthesized!");
            TriggerDataChanged();
            return true;
        }
        Debug.Log("[DataTracker] Not enough data for Interior (need 3 Gravity + 2 Surface)");
        return false;
    }

    /// <summary>Try Formation: 2 of each data type → 1 Formation Conclusion.</summary>
    public bool TryFormation()
    {
        if (_surface >= 2 && _elemental >= 2 && _magnetic >= 2 && _gravity >= 2 && _thermal >= 2)
        {
            _surface -= 2; _elemental -= 2; _magnetic -= 2; _gravity -= 2; _thermal -= 2;
            _formation++;
            Debug.Log("[DataTracker] Formation Conclusion synthesized!");
            TriggerDataChanged();
            return true;
        }
        Debug.Log("[DataTracker] Not enough data for Formation (need 2 of each type)");
        return false;
    }

    /// <summary>Wild Conclusion (Comparative Planetology): any 5 data → 1 Conclusion of choice. Picks the most plentiful data types.</summary>
    public bool TryWildConclusion()
    {
        if (TotalData < 5)
        {
            Debug.Log("[DataTracker] Not enough data for Wild Conclusion (need any 5 data)");
            return false;
        }

        // Consume 5 data, picking from the most plentiful first
        int remaining = 5;
        while (remaining > 0)
        {
            if (_surface > 0 && remaining > 0) { _surface--; remaining--; continue; }
            if (_elemental > 0 && remaining > 0) { _elemental--; remaining--; continue; }
            if (_magnetic > 0 && remaining > 0) { _magnetic--; remaining--; continue; }
            if (_gravity > 0 && remaining > 0) { _gravity--; remaining--; continue; }
            if (_thermal > 0 && remaining > 0) { _thermal--; remaining--; continue; }
            break;
        }

        // Add to whichever conclusion type is lowest (balanced approach)
        int min = Mathf.Min(_composition, Mathf.Min(_dynamo, Mathf.Min(_interior, _formation)));
        if (_composition == min) _composition++;
        else if (_dynamo == min) _dynamo++;
        else if (_interior == min) _interior++;
        else _formation++;

        Debug.Log("[DataTracker] Wild Conclusion synthesized!");
        TriggerDataChanged();
        return true;
    }

    /// <summary>Peer Review: upgrade 1 conclusion to count as 2.</summary>
    public bool TryUpgradeConclusion()
    {
        if (TotalConclusions - _conclusionBonuses > 0)
        {
            _conclusionBonuses++;
            Debug.Log("[DataTracker] Conclusion upgraded (Peer Review)!");
            TriggerDataChanged();
            return true;
        }
        Debug.Log("[DataTracker] No conclusions to upgrade");
        return false;
    }

    /// <summary>Reset all data and conclusions for a new run.</summary>
    public void ResetForNewRun()
    {
        _surface = _elemental = _magnetic = _gravity = _thermal = 0;
        _composition = _dynamo = _interior = _formation = _conclusionBonuses = 0;
        TriggerDataChanged();
    }
}
