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
    [SerializeField] private float _consumptionPerAction = 1f;
    [SerializeField] private float _inputTransferSpeed = 5f;
    [SerializeField] private BallDataSO _redBallData;

    [Header("Storage Settings")]
    [SerializeField] private float _animSpeed = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool _enableLogs = false;

    private float _currentDashOffset;

    public override float CurrentEnergy
    {
        get { return base.CurrentEnergy; }
        set { base.CurrentEnergy = value; UpdateVisuals(); }
    }

    public float InputTransferSpeed => _inputTransferSpeed;
    public float ConsumptionPerAction => _consumptionPerAction;

    /// <summary>
    /// Kept for compatibility if anything calls it manually outside of network loop.
    /// </summary>
    public void ProvideEnergy(float amount)
    {
        CurrentEnergy = EnergyNetwork.Quantize(Mathf.Min(CurrentEnergy + amount, _maxStorage));
        if (_enableLogs) Debug.Log($"[RedMaterialisator] {gameObject.name} received {amount} energy. Total: {CurrentEnergy:F2}");
    }

    public float ExtractEnergy(float amount)
    {
        float taken = EnergyNetwork.Quantize(Mathf.Min(amount, CurrentEnergy));
        CurrentEnergy = EnergyNetwork.Quantize(CurrentEnergy - taken);
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
        if (_maxStorage < _consumptionPerAction)
        {
            Debug.LogWarning($"[RedMaterialisator] {gameObject.name} is misconfigured! MaxStorage ({_maxStorage}) is lower than ConsumptionPerAction ({_consumptionPerAction}). It will never spawn.");
        }

        // Now using quantized deterministic comparison
        if (CurrentEnergy >= _consumptionPerAction)
        {
            if (_enableLogs) Debug.Log($"[RedLogic] EXECUTING SPAWN. Buffer was {CurrentEnergy:F4}");
            
            CurrentEnergy = EnergyNetwork.Quantize(Mathf.Max(0, CurrentEnergy - _consumptionPerAction));
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
        float energyRatio = Mathf.Clamp01(CurrentEnergy / _maxStorage);
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