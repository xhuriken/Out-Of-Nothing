using System;
using UnityEngine;

/// <summary>
/// Base contract for ball behaviors using Unity physics.
/// </summary>
[Serializable]
public abstract class BallBehavior
{
    /// <summary>
    /// Required to prevent shared state issues between balls.
    /// </summary>
    public abstract BallBehavior Clone();

    /// <summary>
    /// Core physics loop executed inside FixedUpdate.
    /// </summary>
    public virtual void ExecuteFixedUpdate(BallEntity ball, float fixedDeltaTime) { }

    /// <summary>
    /// Triggered when the ball receives a valid click.
    /// </summary>
    public virtual void OnClick(BallEntity ball) { }

    /// <summary>
    /// Default duplication logic. Can be overridden for specific behaviors.
    /// </summary>
    /// <param name="ball">The source ball entity.</param>
    public virtual void OnDuplicate(BallEntity ball)
    {
        // Executes the standard duplication defined in the entity
        ball.PerformDefaultDuplicate();
    }

    /// <summary>
    /// Triggered when the ball starts being dragged.
    /// </summary>
    public virtual void OnDragStart(BallEntity ball) { }

    /// <summary>
    /// Triggered when the ball is released after a drag.
    /// </summary>
    public virtual void OnDragEnd(BallEntity ball) { }

    /// <summary>
    /// Triggered when the ball collides with another physics object.
    /// </summary>
    public virtual void OnCollisionEnter(BallEntity ball, Collision2D collision) { }

}