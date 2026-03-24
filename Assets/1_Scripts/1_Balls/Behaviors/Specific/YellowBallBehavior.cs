using DG.Tweening;
using System;
using Unity.VisualScripting;
using UnityEngine;

[Serializable]
public class YellowBallBehavior : BallBehavior, IEnergyStorage, IEnergyNode
{
    [SerializeField] private float _maxStorage = 1f;
    [SerializeField] private float _currentStorage = 1f;

    public float CurrentEnergy => _currentStorage;
    public float MaxEnergy => _maxStorage;
    public EnergyNetwork CurrentNetwork { get; set; }
    [SerializeField] private float _connectionRadius = 3f;
    public float ConnectionRadius => _connectionRadius;
    public Vector2 Position => _me != null ? (Vector2) _me.transform.position : Vector2.zero;

    private BallEntity _me;

    public override void Initialize(BallEntity ball)
    {
        _me = ball;
        _currentStorage = _maxStorage;

        // RE initialize it, if the pool attribute an another ball to this one
        //EnergyManager.Instance?.RegisterNode(this);
    }


    public float ExtractEnergy(float amount)
    {
        float taken = Mathf.Min(amount, _currentStorage);
        _currentStorage -= taken;
        Debug.Log("I'm a Yball and we remove me : " + taken);
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
        DOTween.Kill(this);
        _me.Renderer.Radius *= energyRatio;
        _me.Renderer.Thickness *= energyRatio;
        _me.Collider.radius *= energyRatio;
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
        Debug.Log("Yellow gizmo");
        // The behavior knows its own color and radius
        Gizmos.color = new Color(1f, 0.92f, 0.016f, 1f);
        Gizmos.DrawWireSphere(ball.transform.position, ConnectionRadius);
    }
}