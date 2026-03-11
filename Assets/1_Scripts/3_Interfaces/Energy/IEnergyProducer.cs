using UnityEngine;

/// <summary>
/// Contract for objects that generate energy continuously (Generators).
/// </summary>
public interface IEnergyProducer : IEnergyNode
{
    float ProduceEnergy(float deltaTime);
}