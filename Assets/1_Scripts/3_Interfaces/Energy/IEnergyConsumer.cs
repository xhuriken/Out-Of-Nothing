using UnityEngine;

/// <summary>
/// Contract for any machine that requires external energy to function.
/// </summary>
public interface IEnergyConsumer
{
    float EnergyNeeded { get; }

    void ReceiveEnergy(float amount);
}