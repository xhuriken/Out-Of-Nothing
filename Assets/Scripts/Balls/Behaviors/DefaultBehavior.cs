using Assets.Scripts.Interfaces;
using System;
using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Balls.Behaviors
{
    /// <summary>
    /// Used for FirstBall and RedBall. Standard Unity physics handles collisions and bounces
    /// </summary>
    [Serializable]
    public class DefaultBehavior : IBallBehavior
    {
        public void ExecuteFixedUpdate(BallEntity ball, float fixedDeltaTime)
        {
            // No specific override required
        }
    }
}