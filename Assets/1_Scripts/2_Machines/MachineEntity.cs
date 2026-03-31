using DG.Tweening;
using UnityEngine;
using static UnityEngine.InputSystem.OnScreen.OnScreenStick;

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
/// Handles common state management, drag-and-drop mechanics, and the delegates specific logic.
/// </summary>
public abstract class MachineEntity : MonoBehaviour, IDraggable, IEnergyNode
{
    [Header("Rotation Settings")]
    [SerializeField]
    protected MachineRotationMode _rotationMode = MachineRotationMode.Fixed90Degrees;
    [SerializeField] private float _speed = 1f;

    [SerializeField]
    [Tooltip("Multiplier for free rotation mode. Ignored in Fixed mode.")]
    protected float _freeRotationSpeed = 2f;
    protected bool _isRunning = true;
    private bool _isBeingDragged;
    [Header("Energy Settings")]
    [SerializeField] protected float _connectionRadius = 3.5f;

    [Header("Settings")]
    [SerializeField] private float _dragForceMultiplier = 15f;
    [SerializeField] private float _maxDragSpeed = 30f;


    private Rigidbody2D _rb;

    /// <summary>
    /// Evaluates if the machine is currently active and processing its logic.
    /// </summary>
    public bool IsRunning
    {
        get { return _isRunning; }
    }

    public bool IsBeingDragged => _isBeingDragged;

    #region IEnergyNode Implementation

    public Vector2 Position => transform.position;

    public float ConnectionRadius => _connectionRadius;

    public EnergyNetwork CurrentNetwork { get; set; }

    #endregion


    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    protected virtual void OnEnable() {
        EnergyManager.Instance?.RegisterNode(this);
    } 
    protected virtual void OnDisable() => EnergyManager.Instance?.UnregisterNode(this);

    /// <summary>
    /// Receives collision events forwarded by child proxy colliders.
    /// </summary>
    /// <param name="partId">The identifier of the specific collider that was hit.</param>
    /// <param name="collision">The collision data.</param>
    public virtual void OnPartCollisionEnter(string partId, Collision2D collision)
    {
        // Override in specific machines
        // Switch case inside to handle different parts if necessary imo
    }

    /// <summary>
    /// Receives trigger events forwarded by child proxy colliders.
    /// </summary>
    /// <param name="partId">The identifier of the specific collider that was hit.</param>
    /// <param name="collider">The collider.</param>
    public virtual void OnPartTriggerEnter(string partId, Collider2D collider)
    {
        // Override in specific machines
        // Switch case inside to handle different parts if necessary imo
    }

    #region Drag & drop

    public virtual bool OnDragStart()
    {
        _isRunning = false; // Stop function while moving
        _isBeingDragged = true;
        _rb.bodyType = RigidbodyType2D.Dynamic;
        _rb.linearVelocity = Vector2.zero;
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
        EnergyManager.Instance?.RequestRebuild();
        _isRunning = true;
        _isBeingDragged = false;
        _rb.linearVelocity = Vector2.zero;
        _rb.bodyType = RigidbodyType2D.Kinematic;

    }

    /// <summary>
    /// Handles the rotation logic. Fixed90 for 90-degree steps, 
    /// and Free (Snap) for smaller divisors of 90.
    /// </summary>
    public virtual void OnDragRotate(float scrollDelta)
    {
        if (_rotationMode == MachineRotationMode.None || Mathf.Approximately(scrollDelta, 0f))
        {
            return;
        }

        float direction = Mathf.Sign(scrollDelta);
        float snapAngle;
        float duration;

        if (_rotationMode == MachineRotationMode.Fixed90Degrees)
        {
            snapAngle = 90f;
            duration = 0.15f;
        }
        else if (_rotationMode == MachineRotationMode.Free)
        {
            snapAngle = 15f;
            duration = 0.05f; 
        }
        else
        {
            return;
        }

        ApplySnapRotation(direction, snapAngle, duration);
    }

    /// <summary>
    /// Calculates and executes the snapped rotation tween.
    /// </summary>
    private void ApplySnapRotation(float direction, float snapAngle, float duration)
    {
        DOTween.Kill(transform);

        // get current Z
        float currentZ = transform.eulerAngles.z;

        // Round the current angle to the nearest snapAngle to find the base angle
        float baseZ = Mathf.Round(currentZ / snapAngle) * snapAngle;

        // Calc target
        float targetZ = baseZ + (direction * snapAngle);

        // Tween !
        transform.DORotate(new Vector3(0f, 0f, targetZ), duration, RotateMode.FastBeyond360)
            .SetEase(Ease.OutBack)
            .SetTarget(transform);
    }

    #endregion

    /// <summary>
    /// Draws the connection radius when the machine is selected in the editor.
    /// </summary>
    protected virtual void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _connectionRadius);
    }
}