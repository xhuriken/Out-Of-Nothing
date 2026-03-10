using UnityEngine;

/// <summary>
/// Contract for any machine that can store or provide energy.
/// </summary>
public interface IEnergyStorage
{
    float CurrentEnergy { get; }
    float MaxEnergy { get; }

    /// <summary>
    /// Attempts to extract energy from the machine.
    /// </summary>
    /// <returns>The actual amount of energy extracted.</returns>
    float ExtractEnergy(float amount);
}
