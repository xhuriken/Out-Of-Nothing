using System;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

/// <summary>
/// Specific behavior for the Blue Ball, pausing on collision.
/// </summary>
[Serializable]
public class BlueBallBehavior : BallBehavior
{
    [SerializeField]
    private float _pauseDuration = 2f;

    [SerializeField]
    private float _amplitude = 1f;
    private float _speed = 1f;
    private Vector3 _positionStart;
    private float _oscillationTime;

    // Runtime state variables
    private float _currentPauseTimer;
    private bool _isPaused = true; // true by default when the ball spawn

    /// <summary>
    /// Clones the behavior to ensure independent runtime state.
    /// </summary>
    public override BallBehavior Clone()
    {
        // Shallow copy
        return (BallBehavior) MemberwiseClone();
    }

    public override void Initialize(BallEntity ball)
    {
        _positionStart = ball.transform.position;
        _currentPauseTimer = _pauseDuration; // when the ball spawn, it on the pause state
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

                //restart the position start
                _positionStart = ball.transform.position;
                _oscillationTime = 0f;
            }

            return;
        }

        // Apply normal oscillation force
        _oscillationTime += fixedDeltaTime;

        float newY = _positionStart.y + Mathf.Sin(_oscillationTime * _speed) * _amplitude;

        Vector2 newPosition = new Vector2(_positionStart.x, newY);

        ball.Rb.MovePosition(newPosition);
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

            //restart the position start
            _positionStart = ball.transform.position;
            _oscillationTime = 0f;
        }
    }
}