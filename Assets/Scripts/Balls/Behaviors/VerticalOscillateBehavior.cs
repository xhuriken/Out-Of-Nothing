using Assets.Scripts.Interfaces;
using System;
using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Balls.Behaviors
{
    /// <summary>
    /// Used for BlueBall. Oscillate vertically.
    /// </summary>
    [Serializable]
    public class VerticalOscillateBehavior : IBallBehavior
    {
        [SerializeField] private float verticalForce = 10f;
        
        public void ExecuteFixedUpdate(BallEntity ball, float fixedDeltaTime)
        {
            // Replaces vertical velocity but keeps horizontal momentum
            Vector2 currentVelocity = ball.Rb.linearVelocity;

            // Here we just apply a constant force for test, later we need to add an real logic. and do something when collide.
            ball.Rb.AddForce(Vector2.up * verticalForce * fixedDeltaTime, ForceMode2D.Force);
        }
    }
}