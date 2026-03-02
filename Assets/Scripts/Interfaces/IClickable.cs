using UnityEngine;

/// <summary>
/// This object can be clicked for dragging ?
/// </summary>
public interface IClickable
{
    /// <summary>
    /// I am actually being dragged ?
    /// </summary>
    public bool isDragging { get; set; }
}
