using UnityEngine;

/// <summary>
/// Consumes energy to instantiate RedBalls at a fixed interval when fully charged.
/// </summary>
public class RedMaterialisatorMachine : MachineEntity, IEnergyConsumer
{
    [Header("Materialisator Settings")]
    [SerializeField]
    private BallDataSO _redBallData;

    [SerializeField]
    private float _energyRequiredPerSpawn = 50f;

    [SerializeField]
    private float _ejectionForce = 5f;

    private float _accumulatedEnergy;

    // The machine needs energy if it hasn't reached the spawn threshold
    public bool NeedsEnergy => _accumulatedEnergy < _energyRequiredPerSpawn;

    // Requests only what's missing to reach the next spawn
    public float EnergyRequest => _energyRequiredPerSpawn - _accumulatedEnergy;

    /// <summary>
    /// Receives energy from the network and triggers spawn if threshold is reached.
    /// </summary>
    public void ProvideEnergy(float amount)
    {
        _accumulatedEnergy += amount;
        Debug.Log($"[Materialisator] Received {amount:F1} energy. Progress: {_accumulatedEnergy:F1}/{_energyRequiredPerSpawn}");

        if (_accumulatedEnergy >= _energyRequiredPerSpawn)
        {
            _accumulatedEnergy = 0f;
            SpawnBall();
        }
    }

    /// <summary>
    /// Spawns a ball from the pool and applies an ejection force.
    /// </summary>
    private void SpawnBall()
    {
        if (_redBallData == null) return;
        Debug.Log("[Materialisator] Spawn threshold reached. Creating RedBall.");

        BallEntity newBall = BallPoolManager.Instance.SpawnBall(_redBallData, transform.position);
        if (newBall != null)
        {
            // Eject relative to machine's right direction (transform.right) 
            newBall.Rb.AddForce(transform.right * _ejectionForce, ForceMode2D.Impulse);
        }
    }
}