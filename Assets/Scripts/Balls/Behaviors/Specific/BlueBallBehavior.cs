using System;
using UnityEngine;

/// <summary>
/// Specific behavior for the Blue Ball, pausing on collision.
/// </summary>
[Serializable]
public class BlueBallBehavior : BallBehavior
{
    [SerializeField]
    private float _pauseDuration = 2f;

    [SerializeField]
    private float _verticalForce = 10f;

    // Runtime state variables (Not shared because of Clone)
    private float _currentPauseTimer;
    private bool _isPaused;

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
        if (_isPaused)
        {
            _currentPauseTimer -= fixedDeltaTime;

            if (_currentPauseTimer <= 0f)
            {
                _isPaused = false;
                //ball.Rb.bodyType = RigidbodyType2D.Dynamic;
            }

            return;
        }

        // Apply normal oscillation force
        ball.Rb.AddForce(Vector2.up * _verticalForce * fixedDeltaTime, ForceMode2D.Force);
    }

    /// <summary>
    /// Triggers the pause state upon collision.
    /// </summary>
    public override void OnCollisionEnter(BallEntity ball, Collision2D collision)
    {
        if (!_isPaused)
        {
            _isPaused = true;
            _currentPauseTimer = _pauseDuration;

            // Freeze physics interactions temporarily
            ball.Rb.linearVelocity = Vector2.zero;
            //ball.Rb.bodyType = RigidbodyType2D.Kinematic;
        }
    }
}