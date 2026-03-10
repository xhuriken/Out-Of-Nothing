using UnityEngine;
using UnityEngine.UIElements;

public class RedMaterialisatorMachine : MachineEntity, IEnergyConsumer
{
    private int _maxEnergy;
    private int _currentEnergy;
    private Vector2 _spawnForce;

    public float EnergyNeeded => _maxEnergy - _currentEnergy;

    void Start()
    {
        ElectricManager.Instance.Register(this);
    }

    public void ReceiveEnergy(float amount)
    {
        _currentEnergy += (int)amount;
        _currentEnergy = Mathf.Clamp(_currentEnergy, 0, _maxEnergy);
    }

    /// <summary>
    /// Transfer energy at the nearby machine.
    /// </summary>
    //private void SpawnBall()
    //    {

    //        BallEntity newBall = BallPoolManager.Instance.SpawnBall(_redBallData, transform.position);

    //        newBall.Rb.AddForce(Vector2.right * _spawnForce, ForceMode2D.Impulse);
    //    }
}
