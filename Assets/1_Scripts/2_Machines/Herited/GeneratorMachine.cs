using UnityEngine;

/// <summary>
/// Machine that produces energy over time and stores it in an internal buffer.
/// Currently produces a fixed amount as the liquid system is not yet implemented.
/// </summary>
public class GeneratorMachine : MachineEntity, IEnergyStorage, IEnergyProducer
{
    [Header("Generator Settings")]
    [SerializeField]
    private float _productionRate = 2f; // Energy per second

    [SerializeField]
    private float _maxCapacity = 10f;

    private float _currentEnergy;

    public float CurrentEnergy => _currentEnergy;
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
        return produced;
    }

    /// <summary>
    /// Extracts energy from the internal buffer for the network.
    /// </summary>
    public float ExtractEnergy(float amount)
    {
        float given = Mathf.Min(amount, _currentEnergy);
        _currentEnergy -= given;
        return given;
    }
}