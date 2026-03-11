using UnityEngine;

/// <summary>
/// Contract for objects that require energy to perform an action.
/// </summary>
public interface IEnergyConsumer : IEnergyNode
{
    bool NeedsEnergy { get; }
    float EnergyRequest { get; }
    void ProvideEnergy(float amount);
}