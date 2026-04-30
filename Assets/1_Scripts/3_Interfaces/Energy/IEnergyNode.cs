using UnityEngine;

/// <summary>
/// Base contract for any object part of the energy grid.
/// </summary>
public interface IEnergyNode
{
    Vector2 Position { get; }
    float ConnectionRadius { get; }
    float PhysicalRadius { get; }
    EnergyNetwork CurrentNetwork { get; set; }
    
    // Core properties for Load Balancing and Fluid Flow
    float MaxStorage { get; }
    float CurrentEnergy { get; set; }
    float EnergyAllocationRate { get; set; }
    int DistanceToSource { get; set; }
}