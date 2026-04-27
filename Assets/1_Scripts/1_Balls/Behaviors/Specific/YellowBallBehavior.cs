using DG.Tweening;
using System;
using Unity.VisualScripting;
using UnityEngine;

[Serializable]
public class YellowBallBehavior : BallBehavior, IEnergyStorage, IEnergyConsumer, IEnergyNode
{
    [SerializeField] private float _maxStorage = 1f;
    [SerializeField] private float _maxFlowRate = 5f;

    [Header("Debug")]
    [SerializeField] private bool _enableLogs = false;

    [Header("Live Data")]
    [SerializeField] private float _currentEnergy;

    private float _baseRendererRadius;
    private float _baseRendererThickness;
    private float _baseColliderRadius;

    public float CurrentEnergy
    {
        get { return _currentEnergy; }
        set { _currentEnergy = value; UpdateVisuals(); }
    }
    public float MaxEnergy => _maxStorage;
    public float MaxFlowRate => _maxFlowRate;
    public bool NeedsEnergy => _currentEnergy < _maxStorage;
    public float EnergyRequest => _maxStorage - _currentEnergy;

    public EnergyNetwork CurrentNetwork { get; set; }
    public float ConnectionRadius => 3f;
    public float PhysicalRadius => _me != null ? _me.Renderer.Radius : 0.5f;

    // FIX: Ensure position is updated for the FloodFill algorithm
    public Vector2 Position => _me != null ? (Vector2)_me.transform.position : Vector2.zero;

    private BallEntity _me;
    public override void Initialize(BallEntity ball)
    {
        _me = ball;
        _currentEnergy = _maxStorage;

        if (_me != null)
        {
            if (_me.Renderer != null)
            {
                _baseRendererRadius = _me.Renderer.Radius;
                _baseRendererThickness = _me.Renderer.Thickness;
            }
            else
            {
                Debug.LogError("Wtf are going on here ?");
            }

            _baseColliderRadius =  _me.ColliderRadius;
        }

        // RE initialize it, if the pool attribute an another ball to this one
        //EnergyManager.Instance?.RegisterNode(this);
    }

    public void ProvideEnergy(float amount)
    {
        _currentEnergy = EnergyNetwork.Quantize(Mathf.Min(_currentEnergy + amount, _maxStorage));
        if (_enableLogs) Debug.Log($"[YellowBall] {gameObject.name} received {amount} energy. Total: {_currentEnergy:F2}");
        UpdateVisuals();
    }

    public float ExtractEnergy(float amount)
    {
        float taken = EnergyNetwork.Quantize(Mathf.Min(amount, _currentEnergy));
        _currentEnergy = EnergyNetwork.Quantize(_currentEnergy - taken);
        if (_enableLogs) Debug.Log($"[YellowBall] {gameObject.name} extracted {taken} energy. Remaining: {_currentEnergy:F2}");
        
        if (_currentEnergy <= 0f)
        {
            // Ball destruction or release
            EnergyManager.Instance?.UnregisterNode(this);
            Destroy(this.gameObject);
        }
        UpdateVisuals();
        return taken;
    }

    public void UpdateVisuals()
    {
        if (_me == null) return;

        float energyRatio = _currentEnergy / _maxStorage;
        energyRatio = Mathf.Clamp01(energyRatio);

        Debug.Log($"Hey I'am {gameObject.name} and i have this size ratio now: " + energyRatio);

        DOTween.Kill(this);

        float targetRadius = _baseRendererRadius * energyRatio;
        float targetThickness = _baseRendererThickness * energyRatio;
        float targetColliderRadius = _baseColliderRadius * energyRatio;

        const float minVisible = 0.001f;
        if (_me.Renderer != null)
        {
            _me.Renderer.Radius = Mathf.Max(minVisible, targetRadius);
            _me.Renderer.Thickness = Mathf.Max(minVisible, targetThickness);
        }

        if (_me.Collider != null)
        {
            _me.Collider.radius = Mathf.Max(minVisible, targetColliderRadius);
        }
    }

    public override void OnDragEnd(BallEntity ball)
    {
        base.OnDragEnd(ball);
        EnergyManager.Instance?.RequestRebuild();
    }
    public override void OnEnableBehavior(BallEntity ball)
    {
        EnergyManager.Instance?.RegisterNode(this);
    }

    public override void OnDisableBehavior(BallEntity ball)
    {
        EnergyManager.Instance?.UnregisterNode(this);
    }

    public override void OnDrawGizmosBehavior(BallEntity ball)
    {
        // The behavior knows its own color and radius
        Gizmos.color = new Color(1f, 0.92f, 0.016f, 1f);
        Gizmos.DrawWireSphere(ball.transform.position, ConnectionRadius);
    }
}