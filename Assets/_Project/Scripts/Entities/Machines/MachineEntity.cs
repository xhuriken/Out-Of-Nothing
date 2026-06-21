using DG.Tweening;
using UnityEngine;

/// <summary>
/// Defines the allowed rotation behavior during drag operations.
/// </summary>
public enum MachineRotationMode
{
    None,
    Free,
    Fixed90Degrees
}

/// <summary>
/// Base class for all machines. 
/// Handles common state management, drag-and-drop mechanics, and delegates specific logic.
/// </summary>
public abstract class MachineEntity : MonoBehaviour, IDraggable, IEnergyNode
{
    [Header("Rotation Settings")]
    [SerializeField] protected MachineRotationMode _rotationMode = MachineRotationMode.Fixed90Degrees;
    [SerializeField] protected float _freeRotationSpeed = 2f;

    [Header("Energy Settings")]
    [SerializeField] protected float _connectionRadius = 3.5f;
    [SerializeField] protected float _physicalRadius = 1.0f;
    [SerializeField] protected float _maxStorage = 100f;

    [Header("Settings")]
    [SerializeField] private float _dragForceMultiplier = 15f;
    [SerializeField] private float _maxDragSpeed = 30f;

    [Header("Debug/Live")]
    [SerializeField] protected bool _isRunning = true;
    [SerializeField] private bool _isBeingDragged;
    protected bool _isWaitingForTick;
    [SerializeField] protected float _currentEnergy;
    private Rigidbody2D _rb;

    public bool IsWaitingForTick => _isWaitingForTick;
    public bool IsRunning => _isRunning;
    public bool IsBeingDragged => _isBeingDragged;

    public float NetworkEfficiency => CurrentNetwork != null ? CurrentNetwork.GetNetworkEfficiency() : 1f;

    #region IEnergyNode Implementation
    public Vector2 Position => transform.position;
    public float ConnectionRadius => _connectionRadius;
    public float PhysicalRadius => _physicalRadius;
    public EnergyNetwork CurrentNetwork { get; set; }
    public float MaxStorage => _maxStorage;
    public virtual float CurrentEnergy 
    { 
        get => _currentEnergy; 
        set => _currentEnergy = value; 
    }
    public float EnergyAllocationRate { get; set; }
    public int DistanceToSource { get; set; }
    public Collider2D PhysicsCollider => GetComponent<Collider2D>();
    public virtual bool IsDemanding => true;
    #endregion

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    protected virtual void OnEnable()
    {
        EnergyManager.Instance?.RegisterNode(this);

        if (PowerTickManager.Instance != null)
        {
            PowerTickManager.Instance.OnPowerTick -= HandleTick;
            PowerTickManager.Instance.OnPowerTick += HandleTick;
        }
    }

    protected virtual void OnDisable()
    {
        EnergyManager.Instance?.UnregisterNode(this);

        if (PowerTickManager.Instance != null)
        {
            PowerTickManager.Instance.OnPowerTick -= HandleTick;
        }

        // Clean up and release any captured ball to the pool so it doesn't get orphaned/stuck in the scene
        var captureHandlers = GetComponentsInChildren<BallCaptureHandler>();
        foreach (var handler in captureHandlers)
        {
            if (handler != null && handler.CapturedBall != null)
            {
                BallEntity ball = handler.CapturedBall;
                handler.ClearReference();
                
                if (BallPoolManager.Instance != null)
                {
                    BallPoolManager.Instance.ReleaseBall(ball);
                }
                else
                {
                    Destroy(ball.gameObject);
                }
            }
        }
    }

    protected virtual void Start()
    {
        if (PowerTickManager.Instance != null)
        {
            PowerTickManager.Instance.OnPowerTick -= HandleTick;
            PowerTickManager.Instance.OnPowerTick += HandleTick;
        }
    }

    protected virtual void OnDestroy()
    {
        if (PowerTickManager.Instance != null)
        {
            PowerTickManager.Instance.OnPowerTick -= HandleTick;
        }
    }

    private void HandleTick()
    {
        if (!_isRunning || _isBeingDragged) return;

        _isWaitingForTick = false;
        OnTickExecuted();
        _isWaitingForTick = true;
    }

    protected abstract void OnTickExecuted();

    #region Physics & Collisions
    public virtual void OnPartCollisionEnter(string partId, Collision2D collision) { }
    public virtual void OnPartTriggerEnter(string partId, Collider2D collider) { }
    public virtual void OnPartTriggerStay(string partId, Collider2D collider) { }
    #endregion

    public virtual void ReleaseCapturedBalls()
    {
        var captureHandlers = GetComponentsInChildren<BallCaptureHandler>();
        foreach (var handler in captureHandlers)
        {
            if (handler != null && handler.CapturedBall != null)
            {
                handler.ReleaseCapturedBall();
            }
        }
    }

    #region Drag & Drop Implementation
    public virtual bool OnDragStart()
    {
        _isRunning = false;
        _isBeingDragged = true;
        
        // Isolate from network
        CurrentNetwork?.RemoveNode(this);
        EnergyAllocationRate = 0f;
        EnergyManager.Instance?.MarkTopologyDirty();

        _rb.bodyType = RigidbodyType2D.Dynamic;
        _rb.linearVelocity = Vector2.zero;

        // Release any captured ball when dragging starts
        ReleaseCapturedBalls();

        return true;
    }

    public virtual void OnDragUpdate(Vector2 position)
    {
        Vector2 direction = position - _rb.position;
        Vector2 desiredVelocity = direction * _dragForceMultiplier;
        Vector2 clampedVelocity = Vector2.ClampMagnitude(desiredVelocity, _maxDragSpeed);
        _rb.linearVelocity = clampedVelocity;
    }

    public virtual void OnDragEnd()
    {
        EnergyManager.Instance?.MarkTopologyDirty();
        _isRunning = true;
        _isBeingDragged = false;
        _rb.linearVelocity = Vector2.zero;
        _rb.bodyType = RigidbodyType2D.Kinematic;
    }

    public virtual void OnDragRotate(float scrollDelta)
    {
        if (_rotationMode == MachineRotationMode.None || Mathf.Approximately(scrollDelta, 0f)) return;

        float direction = Mathf.Sign(scrollDelta);
        float snapAngle = (_rotationMode == MachineRotationMode.Fixed90Degrees) ? 90f : 15f;
        float duration = (_rotationMode == MachineRotationMode.Fixed90Degrees) ? 0.15f : 0.05f;

        ApplySnapRotation(direction, snapAngle, duration);
    }

    private void ApplySnapRotation(float direction, float snapAngle, float duration)
    {
        DOTween.Kill(transform);
        float currentZ = transform.eulerAngles.z;
        float baseZ = Mathf.Round(currentZ / snapAngle) * snapAngle;
        float targetZ = baseZ + (direction * snapAngle);

        transform.DORotate(new Vector3(0f, 0f, targetZ), duration, RotateMode.FastBeyond360)
            .SetEase(Ease.OutBack)
            .SetTarget(transform);
    }
    #endregion

    protected virtual void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _connectionRadius);
    }
}