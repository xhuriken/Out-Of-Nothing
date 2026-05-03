using System;
using UnityEngine;

/// <summary>
/// Base contract for ball behaviors using Unity physics.
/// Now a MonoBehaviour to follow standard Unity component logic.
/// </summary>
public abstract class BallBehavior : MonoBehaviour
{
    /// <summary>
    /// Core physics loop executed inside FixedUpdate.
    /// </summary>
    public virtual void ExecuteFixedUpdate(BallEntity ball, float fixedDeltaTime) { }

    /// <summary>
    /// Default click logic. Can be overridden for specific behaviors.
    /// </summary>
    public virtual void OnClick(BallEntity ball)
    {
        ball.PerformDefaultClick();
    }

    /// <summary>
    /// Default duplication logic. Can be overridden for specific behaviors.
    /// </summary>
    public virtual void OnDuplicate(BallEntity ball)
    {
        ball.PerformDefaultDuplicate();
    }

    public virtual void OnDragStart(BallEntity ball) { }

    public virtual void OnDragEnd(BallEntity ball) { }

    /// <summary>
    /// Triggered when the ball collides with another physics object.
    /// </summary>
    public virtual void OnBallCollisionEnter(BallEntity ball, Collision2D collision) { }

    public virtual void OnEnableBehavior(BallEntity ball) { }

    public virtual void OnDisableBehavior(BallEntity ball) { }

    public virtual void OnDrawGizmosBehavior(BallEntity ball) { }

    /// <summary>
    /// Initial setup for the behavior.
    /// </summary>
    public virtual void Initialize(BallEntity ball) { }
}