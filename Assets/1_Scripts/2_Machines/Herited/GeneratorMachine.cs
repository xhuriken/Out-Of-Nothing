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
        float produced = _productionRate * deltaTime;
        _currentEnergy = Mathf.Min(_currentEnergy + produced, _maxCapacity);
        Debug.Log($"Hi i'm {gameObject.name} and we ask me to produce energy ! now i produced : {produced} & i have {CurrentEnergy}");
        return produced;
    }

    /// <summary>
    /// Extracts energy from the internal buffer for the network.
    /// </summary>
    public float ExtractEnergy(float amount)
    {
        float given = Mathf.Min(amount, _currentEnergy);
        _currentEnergy -= given;
        Debug.Log($"Hi i'm {gameObject.name} and we ask me to extract my energy ! I extracted : {given}, and i have {CurrentEnergy}");
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