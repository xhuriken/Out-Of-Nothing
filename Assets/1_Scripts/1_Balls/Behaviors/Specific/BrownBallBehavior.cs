using System;
using UnityEngine;

/// <summary>
/// Specific behavior for the Blue Ball, pausing on collision.
/// </summary>
[Serializable]


public class BrownBallBehavior : BallBehavior
{
    [SerializeField] private float _attractionForce = 15f;
    
    private bool _isBouncing = false;

    /// <summary>
    /// Clones the behavior to ensure independent runtime state.
    /// </summary>
    public override BallBehavior Clone()
    {
        // Shallow copy is sufficient for basic value types
        return (BallBehavior) MemberwiseClone();
    }

    /// <summary>
    /// Applies vertical force or processes pause state.
    /// </summary>
    public override void ExecuteFixedUpdate(BallEntity ball, float fixedDeltaTime)
    {
        if (ball.IsBeingDragged || ball.IsProcessing) return;

        // if is different than the last, we smoothly apply a force to the direction (calculate before)
        // and stop to apply the force to the previous side
        // WE DONT REPLACE THE ACTUAL VELOCITY, WE JUST ADD A FORCE TO THE BALL

        // find the nearest side of the zone (Vector 3 direction)
        if (_isBouncing)
        {
            if (ball.Rb.linearVelocity.magnitude < 0.05f)
            {
                _isBouncing = false;
            }
            else
            {
                return;
            }
        }

        Vector2 direction = GameZone.Instance.GetNearestSide(ball.transform.position);

        direction.Normalize();

        ball.Rb.AddForce(direction * _attractionForce, ForceMode2D.Force);
    }

    /// <summary>
    /// Triggers the pause state upon collision.
    /// </summary>
    public override void OnCollisionEnter(BallEntity ball, Collision2D collision)
    {
        // VISUAL (LATER) :
        // PLAY SHADER EFFECT SHOCKWAVE
        // PLAY SOUND EFFECT

        // PHYSICS :
        // Stop the ball's movement
        // wait X seconds
        // backward a little
        // resume movement !

        //Bounce
        _isBouncing = true;

        //Vector2 normal = collision.contacts[0].normal;

        //float impactSpeed = collision.relativeVelocity.magnitude;

        //ball.Rb.AddForce(normal * impactSpeed, ForceMode2D.Impulse);
    }
}