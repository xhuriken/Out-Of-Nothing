using UnityEngine;

/// <summary>
/// Base class for all machines. 
/// Handles common state management, drag-and-drop mechanics, and the delegates specific logic.
/// </summary>
public abstract class MachineEntity : MonoBehaviour, IDraggable
{
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

    #endregion
}