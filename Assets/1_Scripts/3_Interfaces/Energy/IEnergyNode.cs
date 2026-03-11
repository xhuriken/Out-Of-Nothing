using UnityEngine;

/// <summary>
/// Base contract for any object part of the energy grid.
/// </summary>
public interface IEnergyNode
{
    Vector2 Position { get; }
    float ConnectionRadius { get; }
    EnergyNetwork CurrentNetwork { get; set; }
}