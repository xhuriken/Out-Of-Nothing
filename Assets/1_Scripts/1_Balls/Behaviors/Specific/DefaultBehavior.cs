using Assets.Scripts.Interfaces;
using System;
using System.Collections;
using UnityEngine;


/// <summary>
/// Used for FirstBall and RedBall. Standard Unity physics handles collisions and bounces
/// </summary>
[Serializable]
public class DefaultBehavior : BallBehavior
{
    /// <summary>
    /// Clones the behavior to ensure independent runtime state.
    /// </summary>
    public override BallBehavior Clone()
    {
        // Shallow copy is sufficient for basic value types
        return (BallBehavior) MemberwiseClone();
    }

    public override void ExecuteFixedUpdate(BallEntity ball, float fixedDeltaTime)
    {
        if (ball.IsProcessing) return;
        // No specific override required
    }

    public virtual void ReceiveEnergy(float amount)
    {
    }
}
