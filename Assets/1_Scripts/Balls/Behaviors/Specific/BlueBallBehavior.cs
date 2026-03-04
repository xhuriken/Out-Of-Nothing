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
    private float amplitude = 1f;
    private float speed = 1f;
    private Vector3 positionStart;
    private float _oscillationTime;

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

    public override void Initialize(BallEntity ball)
    {
        positionStart = ball.transform.position;
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
                positionStart = ball.transform.position;
                _oscillationTime = 0f;
            }

            return;
        }

        // Apply normal oscillation force
        _oscillationTime += fixedDeltaTime;

        float newY = positionStart.y + Mathf.Sin(_oscillationTime * speed) * amplitude;

        Vector2 newPosition = new Vector2(positionStart.x, newY);

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
            positionStart = ball.transform.position;
            _oscillationTime = 0f;

        }

        
    }
}