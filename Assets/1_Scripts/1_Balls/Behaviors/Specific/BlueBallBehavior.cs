using DG.Tweening;
using System;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

/// <summary>
/// Specific behavior for the Blue Ball, pausing on collision.
/// </summary>
[Serializable]
public class BlueBallBehavior : BallBehavior
{
    [Header("Movement Settings")]
    [SerializeField] private float _pauseDuration = 2f;
    [SerializeField] private float _amplitude = 5f;
    [SerializeField] private float _speed = 2f;

    [Header("Visual Settings")]
    [SerializeField] private float _shrinkScale = 0.9f;
    [SerializeField] private float _tweenDuration = 0.4f;

    // Runtime state variables
    private float _currentPauseTimer;
    private bool _isPaused = true;
    private float _oscillationTime;
    private bool _isSmall;
    private float _originalRadius;
    private float _originalColliderRadius;

    private readonly PhysicsPriority _priority = PhysicsPriority.Behavior;

    public override void Initialize(BallEntity ball)
    {
        _currentPauseTimer = _pauseDuration;
        _oscillationTime = 0f;
        _originalRadius = ball.Data.radius;
        _originalColliderRadius = ball.ColliderRadius;
        _isPaused = true;
        _isSmall = false;
    }

    /// <summary>
    /// Applies vertical force or processes pause state.
    /// </summary>
    public override void ExecuteFixedUpdate(BallEntity ball, float fixedDeltaTime)
    {
        if (ball.IsBeingDragged || ball.IsProcessing) return;

        if (_isPaused)
        {
            HandlePauseState(ball, fixedDeltaTime);
            return;
        }

        // Apply oscillation
        _oscillationTime += fixedDeltaTime;
        float targetVelocityY = Mathf.Cos(_oscillationTime * _speed) * _amplitude;

        // CRITICAL: We take the CURRENT X from physics and OVERRIDE with our calculated Y.
        // We use Override because we want to stick strictly to the sine wave on Y.
        Vector2 currentVelocity = ball.Passport.Rb.linearVelocity;
        ball.Passport.RequestVelocity(new Vector2(currentVelocity.x, targetVelocityY), _priority, VelocityMode.Override);
    }

    private void HandlePauseState(BallEntity ball, float fixedDeltaTime)
    {
        // Visual: Shrink
        if (!_isSmall)
        {
            ApplyVisualScale(ball, _originalRadius * _shrinkScale, _originalColliderRadius * _shrinkScale);
            _isSmall = true;
        }

        //// Core: Stop the ball while paused
        //ball.Passport.RequestVelocity(Vector2.zero, _priority, VelocityMode.Override);

        _currentPauseTimer -= fixedDeltaTime;

        // Visual: Prepare to grow back
        if (_currentPauseTimer <= _tweenDuration && _isSmall)
        {
            ApplyVisualScale(ball, _originalRadius, _originalColliderRadius);
            _isSmall = false;
        }

        if (_currentPauseTimer <= 0f)
        {
            _isPaused = false;
            _oscillationTime = 0f;
        }
    }

    private void ApplyVisualScale(BallEntity ball, float targetRadius, float targetColliderRadius)
    {
        DOTween.Kill(this);
        DOTween.To(() => ball.Renderer.Radius, x => ball.Renderer.Radius = x, targetRadius, _tweenDuration)
            .SetEase(Ease.OutElastic)
            .SetTarget(this);

        DOTween.To(() => ball.Collider.radius, x => ball.Collider.radius = x, targetColliderRadius, _tweenDuration)
            .SetEase(Ease.OutElastic)
            .SetTarget(this);
    }

    /// <summary>
    /// Triggers the pause state upon collision.
    /// </summary>
    public override void OnBallCollisionEnter(BallEntity ball, Collision2D collision)
    {
        if (!_isPaused)
        {
            _isPaused = true;
            _currentPauseTimer = _pauseDuration;

            // Freeze physics interactions temporarily
            //ball.Rb.bodyType = RigidbodyType2D.Kinematic;

            //restart the position start
            //_positionStart = ball.transform.position;
            //_oscillationTime = 0f;
        }
    }

    /// <summary>
    /// The drag as started ?
    /// </summary>
    public override void OnDragStart(BallEntity ball)
    {
        //_isPaused = true;
    }

    /// <summary>
    /// The drag as ended ?
    /// </summary>
    /// 
    public override void OnDragEnd(BallEntity ball)
    {
        //_isPaused = false;
    }
}