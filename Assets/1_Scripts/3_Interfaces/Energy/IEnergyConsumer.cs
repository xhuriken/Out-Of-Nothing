using UnityEngine;

/// <summary>
/// Contract for objects that require energy to perform an action.
/// </summary>
public interface IEnergyConsumer : IEnergyNode
{
    float InputTransferSpeed { get; }
    float ConsumptionPerAction { get; }
}