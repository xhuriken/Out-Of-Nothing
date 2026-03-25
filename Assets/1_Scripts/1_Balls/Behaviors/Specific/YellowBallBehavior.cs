using DG.Tweening;
using System;
using Unity.VisualScripting;
using UnityEngine;

[Serializable]
public class YellowBallBehavior : BallBehavior, IEnergyStorage, IEnergyConsumer, IEnergyNode
{
    [SerializeField] private float _maxStorage = 1f;
    [SerializeField] private float _maxFlowRate = 5f;
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

        _currentEnergy = Mathf.Min(_currentEnergy + amount, _maxStorage);
        Debug.Log($"Hey I'am {gameObject.name} and I added: {amount} inside my storage of {_currentEnergy}");
        UpdateVisuals();
    }

    public float ExtractEnergy(float amount)
    {
        float taken = Mathf.Min(amount, _currentEnergy);
        _currentEnergy -= taken;
        Debug.Log($"Hey I'am {gameObject.name} and we take me: " + taken);
        if (_currentEnergy <= 0f)
        {
            // DESTRUCTION OU PAS A VOIR AVEC MATHEO CE NEUILLE
            //BallPoolManager.Instance.ReleaseBall(_me);
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