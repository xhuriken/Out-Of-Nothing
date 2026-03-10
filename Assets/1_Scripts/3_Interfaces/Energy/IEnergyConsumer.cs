using UnityEngine;

/// <summary>
/// Contract for any machine that requires external energy to function.
/// </summary>
public interface IEnergyConsumer
{
    void ProvideEnergy(float amount);
}
