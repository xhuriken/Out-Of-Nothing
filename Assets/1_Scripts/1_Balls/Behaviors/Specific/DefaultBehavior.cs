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
    public override void ExecuteFixedUpdate(BallEntity ball, float fixedDeltaTime)
    {
        if (ball.IsProcessing) return;
        // No specific override required
    }


}
