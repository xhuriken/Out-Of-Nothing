using System;
using Unity.VisualScripting;
using UnityEngine;

[Serializable]
public class YellowBallBehavior : BallBehavior, IEnergyStorage, IEnergyNode
{
    [SerializeField] private float _maxStorage = 1f;
    private float _currentStorage;

    public float CurrentEnergy => _currentStorage;
    public float MaxEnergy => _maxStorage;
    public EnergyNetwork CurrentNetwork { get; set; }
    public float ConnectionRadius => 3f;
    public Vector2 Position { get; set; }

    private BallEntity _me;

    public override BallBehavior Clone()
    {
        return (BallBehavior) MemberwiseClone();
    }

    public override void Initialize(BallEntity ball)
    {
        _me = ball;
        _currentStorage = _maxStorage;

        // RE initialize it, if the pool attribute an another ball to this one
        EnergyManager.Instance?.RegisterNode(this);
    }


    public float ExtractEnergy(float amount)
    {
        float taken = Mathf.Min(amount, _currentStorage);
        _currentStorage -= taken;

        if (_currentStorage <= 0)
        {
            // DESTRUCTION OU PAS A VOIR AVEC MATHEO CE NEUILLE
            BallPoolManager.Instance.ReleaseBall(_me);
        }
        UpdateVisuals();
        return taken;
    }

    public void UpdateVisuals()
    {
        float energyRatio = _currentStorage / _maxStorage;
        // TOdo Dotween
        _me.Renderer.Radius = energyRatio;
        _me.Collider.radius = energyRatio;
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
        Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.3f);
        Gizmos.DrawWireSphere(ball.transform.position, ConnectionRadius);
    }
}