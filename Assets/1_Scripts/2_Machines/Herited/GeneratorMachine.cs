using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class GeneratorMachine : MachineEntity, IEnergyStorage
{
    private float _currentEnergy;
    private float _maxEnergy;

    public float CurrentEnergy => _currentEnergy;
    public float MaxEnergy => _maxEnergy;

    public float ExtractEnergy(float amount)
    {
        float given = Mathf.Min(amount, _currentEnergy);
        _currentEnergy -= given;
        return given;
    }
    void Start()
    {
        ElectricManager.Instance.Register(this);
    }
}