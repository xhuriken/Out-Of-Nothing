using UnityEngine;

/// <summary>
/// Contract for objects that store energy (Yellow Balls, Internal machine buffers).
/// </summary>
public interface IEnergyStorage : IEnergyNode
{
    float CurrentEnergy { get; }
    float MaxEnergy { get; }
    float ExtractEnergy(float amount);
}