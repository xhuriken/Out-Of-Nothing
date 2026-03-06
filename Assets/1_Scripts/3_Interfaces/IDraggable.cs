using UnityEngine;

/// <summary>
/// Defines the contract for any object that can be dragged by the input system.
/// </summary>
public interface IDraggable
{
    /// <summary>
    /// Triggered when the drag interaction starts.
    /// </summary>
    void OnDragStart();

    /// <summary>
    /// Triggered every frame while the object is being dragged.
    /// </summary>
    /// <param name="position">The new world position for the object.</param>
    void OnDragUpdate(Vector2 position);

    /// <summary>
    /// Triggered when the drag interaction ends.
    /// </summary>
    void OnDragEnd();
}