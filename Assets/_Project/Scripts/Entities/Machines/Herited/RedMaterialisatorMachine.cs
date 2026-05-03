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
    [SerializeField] private float _inputTransferSpeed = 0.05f; // Absorb slowly (0.05 per tick)
    [SerializeField] private BallDataSO _redBallData;

    [Header("Storage Settings")]
    [SerializeField] private float _animSpeed = 0.5f;

    [Header("Sequencer Settings")]
    [Tooltip("Number of global ticks before attempting an action. (e.g., 20 ticks = 1 action every 20 ticks)")]
    [SerializeField] private int _actionCadenceTicks = 20;
    [Tooltip("Offset to offset the cadence. Useful for creating alternating patterns.")]
    [SerializeField] private int _tickOffset = 0;

    [Header("Debug")]
    [SerializeField] private bool _enableLogs = false;

    private float _currentDashOffset;
    private Color _originalColor;
    private long _startFillTick;

    public override float CurrentEnergy
    {
        get { return base.CurrentEnergy; }
        set { base.CurrentEnergy = value; }
    }

    public float InputTransferSpeed 
    {
        get
        {
            // If we already have enough energy for the next action, we stop demanding immediately.
            // Using a small margin to avoid floating point precision issues near 1.0.
            if (CurrentEnergy >= _consumptionPerAction - 0.0001f) return 0f;

            if (IsWaiting()) return 0f;
            return _inputTransferSpeed;
        }
    }
    
    public float ConsumptionPerAction => _consumptionPerAction;
    public override bool IsDemanding => !IsWaiting();

    protected override void Start()
    {
        base.Start();
        if (_energyRenderer != null)
        {
            _originalColor = _energyRenderer.Color;
        }
        RecalculateStartFillTick();
    }

    public override void OnDragEnd()
    {
        base.OnDragEnd();
        RecalculateStartFillTick();
    }

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
        return taken;
    }

    private void OnValidate()
    {
        if (_energyRenderer != null && _maxStorage > 0f)
        {
            float energyRatio = Mathf.Clamp01(CurrentEnergy / _maxStorage);
            _energyRenderer.DashSpacing = 1f - energyRatio;
        }
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

        long currentTick = PowerTickManager.Instance.CurrentTickCount;
        
        if (currentTick % _actionCadenceTicks == _tickOffset)
        {
            if (CurrentEnergy >= _consumptionPerAction - 0.001f)
            {
                if (_enableLogs) Debug.Log($"[RedLogic] EXECUTING SPAWN at tick {currentTick}. Buffer was {CurrentEnergy}");
                
                CurrentEnergy = EnergyNetwork.Quantize(Mathf.Max(0, CurrentEnergy - _consumptionPerAction));
                SpawnBall();
            }

            // Always recalculate on the deadline tick.
            // This guarantees the machine will WAIT FIRST before pumping if it missed the previous deadline.
            RecalculateStartFillTick();
        }
    }

    private void Update()
    {
        if (_energyRenderer == null || !_isRunning) return;

        // 1. Visual Animation (Fluid)
        float energyRatio = Mathf.Clamp01(CurrentEnergy / _maxStorage);
        _energyRenderer.DashSpacing = 1f - energyRatio;

        float dashPeriod = _energyRenderer.DashSize + _energyRenderer.DashSpacing;

        if (dashPeriod > 0)
        {
            _currentDashOffset += Time.deltaTime * _animSpeed;
            _energyRenderer.DashOffset = _currentDashOffset % dashPeriod;
        }

        // 2. Just-In-Time Feedback
        // Machine is gray ONLY if it is waiting for its scheduled pumping window.
        // If it's full and ready to fire, it stays colored (Active).
        _energyRenderer.Color = IsWaiting() ? Color.gray : _originalColor;
    }

    private bool IsWaiting()
    {
        if (PowerTickManager.Instance == null || !_isRunning) return false;

        // If we are completely disconnected from any producer, we cannot fill anyway. Stay in waiting state.
        if (CurrentNetwork == null || !CurrentNetwork.HasProducers)
        {
            return true;
        }

        // Only wait if the current tick is before our calculated start window.
        bool isWaiting = PowerTickManager.Instance.CurrentTickCount < _startFillTick;
        return isWaiting;
    }

    private void RecalculateStartFillTick()
    {
        if (PowerTickManager.Instance == null || _inputTransferSpeed <= 0f) return;

        long currentTick = PowerTickManager.Instance.CurrentTickCount;
        float missingEnergy = Mathf.Max(0, _consumptionPerAction - CurrentEnergy);
        
        int ticksRequiredToFill = Mathf.CeilToInt(missingEnergy / _inputTransferSpeed);

        long targetTick = currentTick;
        int distance = 0;
        
        while (targetTick % _actionCadenceTicks != _tickOffset)
        {
            targetTick++;
            distance++;
        }
        
        // If we missed this exact tick, target the next cycle
        if (distance == 0) distance = _actionCadenceTicks;

        // NEW FIX: If we don't have enough time to fill before this deadline, we must target a future deadline!
        while (distance < ticksRequiredToFill)
        {
            distance += _actionCadenceTicks;
        }

        _startFillTick = currentTick + distance - ticksRequiredToFill;

        if (_enableLogs)
        {
            Debug.Log($"[RedLogic] {gameObject.name} RECALCULATED -> Tick: {currentTick}, Missing: {missingEnergy:F2}, TicksReq: {ticksRequiredToFill}, Distance: {distance}, StartFillTick: {_startFillTick}");
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
