using UnityEngine;

/// <summary>
/// Multi Selector machine.
/// Fully draggable but does not require or consume electricity.
/// </summary>
public class MultiSelectorMachine : MachineEntity
{
    public override bool IsDemanding => false;

    protected override void Start()
    {
        base.Start();
        _maxStorage = 0f;
        _currentEnergy = 0f;
    }

    protected override void OnTickExecuted()
    {
        // No tick-based behavior needed
    }
}
