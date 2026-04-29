using Shapes;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// Machine that produces energy over time and stores it in an internal buffer.
/// Currently produces a fixed amount as the liquid system is not yet implemented.
/// </summary>
public class GeneratorMachine : MachineEntity, IEnergyProducer
{
    [Header("References")]
    [SerializeField] private Rectangle _energyRenderer;

    [Header("Generator Settings")]
    [SerializeField] private float _productionPerTick = 0.12f; // Can sustain 2 Reds (0.10) but not 3 (0.15)
    [SerializeField] private float _outputTransferSpeed = 0.5f;
    [SerializeField] private float _animSpeed = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool _enableLogs = false;

    private float _currentDashOffset;

    public override float CurrentEnergy
    {
        get { return base.CurrentEnergy; }
        set { base.CurrentEnergy = value; UpdateVisuals(); }
    }

    public float ProductionPerTick => _productionPerTick;
    public float OutputTransferSpeed => _outputTransferSpeed;

    private void FixedUpdate()
    {
        if (PowerTickManager.Instance == null) return;

        // Produce energy fluidly over time based on the tick rate
        float tickRate = PowerTickManager.Instance.TickRate;
        float producedPerSec = _productionPerTick / tickRate;
        float producedThisFrame = EnergyNetwork.Quantize(producedPerSec * Time.fixedDeltaTime);
        
        CurrentEnergy = EnergyNetwork.Quantize(Mathf.Min(CurrentEnergy + producedThisFrame, _maxStorage));
    }

    protected override void OnTickExecuted()
    {
        // Add synchronized SFX or particle triggers here
    }

    private void Update()
    {
        UpdateVisuals();
    }

    void OnValidate()
    {
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        _energyRenderer.DashSpacing = 1f - Mathf.Clamp01(CurrentEnergy / _maxStorage);
        float dashPeriod = _energyRenderer.DashSize + _energyRenderer.DashSpacing;

        if (dashPeriod > 0)
        {
            _currentDashOffset += Time.deltaTime * _animSpeed;
            _energyRenderer.DashOffset = _currentDashOffset % dashPeriod;
        }
    }
}