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
/// Handles common state management, drag-and-drop mechanics, and the delegates specific logic.
/// </summary>
public abstract class MachineEntity : MonoBehaviour, IDraggable
{
    [Header("Rotation Settings")]
    [SerializeField]
    protected MachineRotationMode _rotationMode = MachineRotationMode.Fixed90Degrees;

    [SerializeField]
    [Tooltip("Multiplier for free rotation mode. Ignored in Fixed mode.")]
    protected float _freeRotationSpeed = 0.5f;

    protected bool _isRunning = true;
    /// <summary>
    /// Evaluates if the machine is currently active and processing its logic.
    /// </summary>
    public bool IsRunning
    {
        get { return _isRunning; }
    }

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

    #region Drag & drop

    public virtual void OnDragStart()
    {
        _isRunning = false; // Stop function while moving
        // TODO: Handle visual feedback ((Animations)
    }

    public virtual void OnDragUpdate(Vector2 position)
    {
        // I think we will made the same thing than the ball, but for now, position will be sufficient
        transform.position = position;
    }

    public virtual void OnDragEnd()
    {
        _isRunning = true;

    }

    public virtual void OnDragRotate(float scrollDelta)
    {
        if (_rotationMode == MachineRotationMode.None)
        {
            return;
        }

        // Determine direction
        // +1 forward or -1 backward
        float direction = Mathf.Sign(scrollDelta);

        if (_rotationMode == MachineRotationMode.Fixed90Degrees)
        {
            // Snap rotation by exactly 90 degrees
            transform.Rotate(0f, 0f, direction * 90f);
        }
        else if (_rotationMode == MachineRotationMode.Free)
        {
            // Smooth, continuous rotation based on scroll magnitude
            transform.Rotate(0f, 0f, scrollDelta * _freeRotationSpeed);
        }
    }

    #endregion
}