using Shapes;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// Machine that produces energy over time and stores it in an internal buffer.
/// Currently produces a fixed amount as the liquid system is not yet implemented.
/// </summary>
public class GeneratorMachine : MachineEntity, IEnergyStorage, IEnergyProducer
{
    [Header("References")]
    [SerializeField] private Rectangle _energyRenderer;

    [Header("Generator Settings")]
    [SerializeField] private float _productionRate = 10f; // Energy per second
    [SerializeField] private float _maxCapacity = 100f;
    [SerializeField] private float _animSpeed = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool _enableLogs = false;

    [Header("Live Data")]
    [SerializeField] private float _currentEnergy;
    private float _currentDashOffset;

    public float CurrentEnergy
    {
        get { return _currentEnergy; }
        set { _currentEnergy = value; UpdateVisuals(); }
    }
    public float MaxEnergy => _maxCapacity;

    /// <summary>
    /// Produces energy based on the time elapsed. 
    /// The excess is lost if the internal buffer is full.
    /// </summary>
    /// <param name="deltaTime">Time since last tick.</param>
    /// <returns>The amount of energy produced in this tick.</returns>
    public float ProduceEnergy(float deltaTime)
    {
        float produced = EnergyNetwork.Quantize(_productionRate * deltaTime);
        _currentEnergy = EnergyNetwork.Quantize(Mathf.Min(_currentEnergy + produced, _maxCapacity));
        if (_enableLogs) Debug.Log($"[Generator] {gameObject.name} produced {produced:F3}. Current: {_currentEnergy:F2}");
        return produced;
    }

    /// <summary>
    /// Extracts energy from the internal buffer for the network.
    /// </summary>
    public float ExtractEnergy(float amount)
    {
        float given = EnergyNetwork.Quantize(Mathf.Min(amount, _currentEnergy));
        _currentEnergy = EnergyNetwork.Quantize(_currentEnergy - given);
        if (_enableLogs) Debug.Log($"[Generator] {gameObject.name} extracted {given:F3}. Remaining: {_currentEnergy:F2}");
        return given;
    }

    protected override void OnTickExecuted()
    {
        // Add synchronized SFX or particle triggers here
    }

    private void Update()
    {
        UpdateVisuals();
    }

    void OnValidate()
    {
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        _energyRenderer.DashSpacing = 1f - (_currentEnergy / _maxCapacity);
        float dashPeriod = _energyRenderer.DashSize + _energyRenderer.DashSpacing;

        if (dashPeriod > 0)
        {
            _currentDashOffset += Time.deltaTime * _animSpeed;
            _energyRenderer.DashOffset = _currentDashOffset % dashPeriod;
        }
    }
}