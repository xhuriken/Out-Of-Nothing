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
    [SerializeField]
    private float _pauseDuration = 2f;

    [SerializeField]
    private float _amplitude = 5f;

    [SerializeField]
    private float _speed = 2f;

    //private Vector3 _positionStart;

    // Runtime state variables
    private float _currentPauseTimer;
    private bool _isPaused = true;
    private float _oscillationTime;
    private bool _isSmall = false;

    private float _originalRadius;

    public override void Initialize(BallEntity ball)
    {
        //_positionStart = ball.transform.position;
        _currentPauseTimer = _pauseDuration; // when the ball spawn, it on the pause state
        _oscillationTime = 0f;
        _originalRadius = ball.Data.radius;
    }

    /// <summary>
    /// Applies vertical force or processes pause state.
    /// </summary>
    public override void ExecuteFixedUpdate(BallEntity ball, float fixedDeltaTime)
    {
        // If the ball is being dragged, skip all behavior logic !
        if (ball.IsBeingDragged || ball.IsProcessing) return;

        if (_isPaused)
        {
            // Visual
            if (!_isSmall)
            {
                DOTween.Kill(this);
                // Set it smaller
                DOTween.To(() => ball.Renderer.Radius, x => ball.Renderer.Radius = x, _originalRadius * 0.9f, 0.4f).SetEase(Ease.OutElastic).SetTarget(this);
                DOTween.To(() => ball.Collider.radius, x => ball.Collider.radius = x, ball.ColliderRadius * 0.9f, 0.4f).SetEase(Ease.OutElastic).SetTarget(this);
                _isSmall = true;
            }

            // Core
            _currentPauseTimer -= fixedDeltaTime;

            if(_currentPauseTimer <= 0.4f && _isSmall)
            {
                DOTween.Kill(this);
                // Set it back to normal
                DOTween.To(() => ball.Renderer.Radius, x => ball.Renderer.Radius = x, _originalRadius, 0.4f).SetEase(Ease.OutElastic).SetTarget(this);
                DOTween.To(() => ball.Collider.radius, x => ball.Collider.radius = x, ball.ColliderRadius, 0.4f).SetEase(Ease.OutElastic).SetTarget(this);
                _isSmall = false;
            }

            if (_currentPauseTimer <= 0f)
            {
                _isPaused = false;
                //ball.Rb.bodyType = RigidbodyType2D.Dynamic;

                //restart the position start
                //_positionStart = ball.transform.position;
                _oscillationTime = 0f;
            }

            return;
        }

        // Apply normal oscillation force
        _oscillationTime += fixedDeltaTime;

        //float newY = _positionStart.y + Mathf.Sin(_oscillationTime * _speed) * _amplitude;

        //Vector2 newPosition = new Vector2(_positionStart.x, newY);

        //ball.Rb.MovePosition(newPosition);

        float velocityY = Mathf.Cos(_oscillationTime * _speed) * _amplitude;

        // We only override the Y velocity. The X velocity remains controlled by physics collisions !
        ball.Rb.linearVelocity = new Vector2(ball.Rb.linearVelocity.x, velocityY);
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