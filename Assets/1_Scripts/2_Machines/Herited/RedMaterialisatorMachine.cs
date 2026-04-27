using Shapes;
using UnityEngine;

/// <summary>
/// Consumes energy to fill an internal buffer, then uses that buffer to instantiate RedBalls.
/// Implements IEnergyStorage to allow other machines to potentially draw from its reserve.
/// </summary>
public class RedMaterialisatorMachine : MachineEntity, IEnergyConsumer, IEnergyStorage
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

    [Header("Debug")]
    [SerializeField] private bool _enableLogs = false;

    [Header("Live Data")]
    [SerializeField] private float _currentEnergy;
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
        _currentEnergy = EnergyNetwork.Quantize(Mathf.Min(_currentEnergy + amount, _maxCapacity));
        if (_enableLogs) Debug.Log($"[RedMaterialisator] {gameObject.name} received {amount} energy. Total: {_currentEnergy:F2}");
    }

    /// <summary>
    /// Allows others to extract energy from this machine.
    /// </summary>
    public float ExtractEnergy(float amount)
    {
        float taken = EnergyNetwork.Quantize(Mathf.Min(amount, _currentEnergy));
        _currentEnergy = EnergyNetwork.Quantize(_currentEnergy - taken);
        UpdateVisuals();
        return taken;
    }

    private void OnValidate()
    {
        UpdateVisuals();
    }

    private void Update()
    {
        UpdateVisuals();
    }

    /// <summary>
    /// Synchronized logic executed only on PowerTick.
    /// </summary>
    protected override void OnTickExecuted()
    {
        if (_maxCapacity < _energyRequiredPerSpawn)
        {
            Debug.LogWarning($"[RedMaterialisator] {gameObject.name} is misconfigured! MaxCapacity ({_maxCapacity}) is lower than EnergyRequiredPerSpawn ({_energyRequiredPerSpawn}). It will never spawn.");
        }

        // Now using quantized deterministic comparison
        if (_currentEnergy >= _energyRequiredPerSpawn)
        {
            if (_enableLogs) Debug.Log($"[RedLogic] EXECUTING SPAWN. Buffer was {_currentEnergy:F4}");
            
            _currentEnergy = EnergyNetwork.Quantize(Mathf.Max(0, _currentEnergy - _energyRequiredPerSpawn));
            SpawnBall();
        }
    }

    /// <summary>
    /// Synchronizes the Shapes Rectangle dashes with the energy level and animates them.
    /// </summary>
    private void UpdateVisuals()
    {
        if (_energyRenderer == null) return;

        // Adjust spacing based on energy (0 energy = wide spacing, Full = no spacing)
        float energyRatio = Mathf.Clamp01(_currentEnergy / _maxCapacity);
        _energyRenderer.DashSpacing = 1f - energyRatio;

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
            Debug.LogError("[Materialisator] RedBallDataSO is not assigned.");
            return;
        }

        // Logic handled through the pool manager
        BallEntity newBall = BallPoolManager.Instance.SpawnBall(_redBallData, transform.position);

        if (newBall != null)
        {
            newBall.GetComponent<Rigidbody2D>()?.AddForce(transform.right * _ejectionForce, ForceMode2D.Impulse);
        }
    }
}