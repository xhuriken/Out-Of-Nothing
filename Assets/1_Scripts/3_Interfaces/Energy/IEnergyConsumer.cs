using UnityEngine;

/// <summary>
/// Contract for objects that require energy to perform an action.
/// </summary>
public interface IEnergyConsumer : IEnergyNode
{
    /// <summary>
    /// He didnt need energy if his battery is full
    /// </summary>
    bool NeedsEnergy { get; }
    /// <summary>
    /// Gets the amount of energy requested.
    /// </summary>
    float EnergyRequest { get; }
    /// <summary>
    /// The maximum amount of energy this consumer can absorb per second.
    /// </summary>
    float MaxFlowRate { get; }
    void ProvideEnergy(float amount);
}