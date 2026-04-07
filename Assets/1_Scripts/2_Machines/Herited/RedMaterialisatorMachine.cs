using UnityEngine;

using Shapes;
using UnityEngine;

/// <summary>
/// Consumes energy to fill an internal buffer, then uses that buffer to instantiate RedBalls.
/// Implements IEnergyStorage to allow other machines to potentially draw from its reserve.
/// </summary>
public class RedMaterialisatorMachine : MachineEntity, IEnergyConsumer
{
    [Header("References")]
    [SerializeField] private Rectangle _energyRenderer;

    [Header("Materialisator Settings")]
    [SerializeField] private float _ejectionForce = 5f;
    [SerializeField] private float _energyRequiredPerSpawn = 50f;
    [SerializeField] private BallDataSO _redBallData;

    [Header("Storage Settings")]
    [SerializeField] private float _animSpeed = 0.5f;
    [SerializeField] private float _maxCapacity = 100f;
    [SerializeField] private float _maxFlowRate = 10f;

    private float _currentEnergy;
    private float _currentDashOffset;

    /// <summary>
    /// Gets the current energy stored in the machine's buffer.
    /// </summary>
    public float CurrentEnergy
    {
        get { return _currentEnergy; }
        set { _currentEnergy = value; UpdateVisuals(); }
    }

    /// <summary>
    /// Gets the maximum capacity of the internal buffer.
    /// </summary>
    public float MaxEnergy
    {
        get { return _maxCapacity; }
    }

    /// <summary>
    /// The machine requests energy from the network as long as its buffer isn't full.
    /// </summary>
    public bool NeedsEnergy
    {
        get { return _currentEnergy < _maxCapacity; }
    }

    /// <summary>
    /// Returns the amount of energy needed to top up the buffer.
    /// </summary>
    public float EnergyRequest
    {
        get { return _maxCapacity - _currentEnergy; }
    }

    public float MaxFlowRate => _maxFlowRate;

    /// <summary>
    /// Allows the network to fill the internal storage.
    /// </summary>
    public void ProvideEnergy(float amount)
    {
        _currentEnergy = Mathf.Min(_currentEnergy + amount, _maxCapacity);
        Debug.Log($"Hi i'm {gameObject.name} and i provide {amount} energy, i have {CurrentEnergy} now");
    }
    void OnValidate()
    {
        UpdateVisuals();
    }

    private void Update()
    {
        //if (!_isRunning)
        //{
        //    return;
        //}

        //UpdateLogic();
        UpdateVisuals();
    }

    /// <summary>
    /// Synchronized logic executed only on PowerTick.
    /// </summary>
    protected override void OnTickExecuted()
    {
        if (_currentEnergy >= _energyRequiredPerSpawn)
        {
            _currentEnergy -= _energyRequiredPerSpawn;
            SpawnBall();
        }
    }

    /// <summary>
    /// Synchronizes the Shapes Rectangle dashes with the energy level and animates them.
    /// </summary>
    private void UpdateVisuals()
    {
        if (_energyRenderer == null)
        {
            return;
        }

        // Adjust spacing based on energy (0 energy = wide spacing, Full = no spacing)
        _energyRenderer.DashSpacing = 1f - (_currentEnergy / _maxCapacity);

        float dashPeriod = _energyRenderer.DashSize + _energyRenderer.DashSpacing;

        if (dashPeriod > 0)
        {
            _currentDashOffset += Time.deltaTime * _animSpeed;
            _energyRenderer.DashOffset = _currentDashOffset % dashPeriod;
        }
    }

    /// <summary>
    /// Spawns a ball from the pool and applies ejection force.
    /// </summary>
    private void SpawnBall()
    {
        if (_redBallData == null)
        {
            Debug.LogError("[Materialisator] RedBallDataSO is not assigned, what da fuck are you doing biatch ?");
            return;
        }

        Debug.Log("[Materialisator] Buffer reached threshold. Spawning RedBall !");

        //BallEntity newBall = BallPoolManager.Instance.SpawnBall(_redBallData, transform.position);

        //if (newBall != null)
        //{
        //    newBall.Rb.AddForce(transform.right * _ejectionForce, ForceMode2D.Impulse);
        //}
    }
}