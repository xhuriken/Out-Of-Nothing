using DG.Tweening;
using System;
using Unity.VisualScripting;
using UnityEngine;
using System.Collections.Generic;

[Serializable]
public class YellowBallBehavior : BallBehavior, IEnergyConsumer, IEnergyProducer, IEnergyNode
{
    [SerializeField] private float _maxStorage = 1f;
    [SerializeField] private float _inputTransferSpeed = 5f;
    [SerializeField] private float _outputTransferSpeed = 5f;

    [Header("Debug")]
    [SerializeField] private bool _enableLogs = false;

    [Header("Visual Settings")]
    [SerializeField] private Color _colorFull = new Color(1f, 0.92f, 0.016f, 1f);
    [SerializeField] private Color _colorEmpty = Color.gray;

    [Header("Live Data")]
    [SerializeField] private float _currentEnergy;

    private float _baseRendererRadius;
    private float _baseRendererThickness;
    private float _baseColliderRadius;

    private readonly HashSet<Collider2D> _currentNeighbors = new HashSet<Collider2D>();
    private readonly Collider2D[] _overlapResults = new Collider2D[16];

    public float CurrentEnergy
    {
        get { return _currentEnergy; }
        set 
        { 
            _currentEnergy = value; 
            UpdateVisuals(); 
        }
    }
    public float MaxEnergy => _maxStorage;
    public float MaxStorage => _maxStorage;
    public float InputTransferSpeed => _inputTransferSpeed;
    public float ConsumptionPerAction => 0f; // Batteries do not consume energy for actions
    public float ProductionPerTick => 0f; // Batteries do not produce energy
    public float OutputTransferSpeed => _outputTransferSpeed;
    public float EnergyAllocationRate { get; set; }

    public EnergyNetwork CurrentNetwork { get; set; }
    public int DistanceToSource { get; set; }
    public Collider2D PhysicsCollider => _me != null ? _me.Collider : null;
    public bool IsBeingDragged => _me != null && _me.IsBeingDragged;
    public bool IsDemanding => true;
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


    public void UpdateVisuals()
    {
        if (_me == null || _me.Renderer == null) return;

        float energyRatio = _currentEnergy / _maxStorage;
        energyRatio = Mathf.Clamp01(energyRatio);

        // Smooth transition from empty (gray) to full (yellow)
        _me.Renderer.Color = Color.Lerp(_colorEmpty, _colorFull, energyRatio);
    }

    public override void OnDragEnd(BallEntity ball)
    {
        base.OnDragEnd(ball);
        EnergyManager.Instance?.MarkTopologyDirty();
    }

    public override void ExecuteFixedUpdate(BallEntity ball, float fixedDeltaTime)
    {
        // Trigger topology update if the ball is dragged OR moving (physics/collisions)
        if (ball.IsBeingDragged || ball.Rb.linearVelocity.sqrMagnitude > 0.01f)
        {
            CheckTopologyChanges();
        }
    }

    private void CheckTopologyChanges()
    {
        int count = Physics2D.OverlapCircleNonAlloc(Position, ConnectionRadius, _overlapResults);
        
        bool hasChanged = false;
        int validCount = 0;
        
        for (int i = 0; i < count; i++)
        {
            if (_overlapResults[i].gameObject != this.gameObject) validCount++;
        }

        if (validCount != _currentNeighbors.Count)
        {
            hasChanged = true;
        }
        else
        {
            for (int i = 0; i < count; i++)
            {
                Collider2D col = _overlapResults[i];
                if (col.gameObject == this.gameObject) continue;
                if (!_currentNeighbors.Contains(col))
                {
                    hasChanged = true;
                    break;
                }
            }
        }

        if (hasChanged)
        {
            _currentNeighbors.Clear();
            for (int i = 0; i < count; i++)
            {
                Collider2D col = _overlapResults[i];
                if (col.gameObject == this.gameObject) continue;
                _currentNeighbors.Add(col);
            }
            EnergyManager.Instance?.MarkTopologyDirty();
        }
    }
    public override void OnEnableBehavior(BallEntity ball)
    {
        EnergyManager.Instance?.RegisterNode(this);
    }

    public override void OnDisableBehavior(BallEntity ball)
    {
        _currentNeighbors.Clear();
        EnergyManager.Instance?.UnregisterNode(this);
    }

    public override void OnDrawGizmosBehavior(BallEntity ball)
    {
        // The behavior knows its own color and radius
        Gizmos.color = new Color(1f, 0.92f, 0.016f, 1f);
        Gizmos.DrawWireSphere(ball.transform.position, ConnectionRadius);
    }
}