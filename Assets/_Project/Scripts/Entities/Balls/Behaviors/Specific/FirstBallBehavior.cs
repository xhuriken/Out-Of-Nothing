using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Behavior for the First Ball which cannot be consumed by the Black Hole,
/// expels itself upon contact, and duplicates into standard Red Balls.
/// </summary>
[Serializable]
public class FirstBallBehavior : BallBehavior
{
    [SerializeField]
    [Tooltip("The BallDataSO representing the Red Ball to spawn upon duplication.")]
    private BallDataSO _redBallData;

    /// <summary>
    /// The velocity applied to expel the ball away from the Black Hole.
    /// </summary>
    [SerializeField]
    [Tooltip("The velocity applied to expel the ball away from the Black Hole.")]
    private float _expelVelocity = 24f;

    private float _cooldownTimer = 0f;
    private int _blackHoleContactCount = 0;

    /// <summary>
    /// Initial setup for the behavior.
    /// </summary>
    public override void Initialize(BallEntity ball)
    {
        base.Initialize(ball);
        _cooldownTimer = 0f;
        _blackHoleContactCount = 0;
    }

    /// <summary>
    /// Update tick to handle cooldowns.
    /// </summary>
    private void Update()
    {
        if (_cooldownTimer > 0f)
        {
            _cooldownTimer -= Time.deltaTime;
        }
    }

    /// <summary>
    /// Overrides duplication to spawn a standard Red Ball instead of another First Ball.
    /// </summary>
    public override void OnDuplicate(BallEntity ball)
    {
        if (_redBallData == null)
        {
            Debug.LogError("[FirstBall] RedBallData is not assigned.");
            return;
        }
        ball.PerformDuplicate(_redBallData);
    }

    /// <summary>
    /// Handles the First Ball's repulsion when it hits the Black Hole.
    /// </summary>
    public void HandleBlackHoleCollision(BallEntity ball, BlackHole blackHole)
    {
        if (_cooldownTimer > 0f || blackHole == null || ball == null)
        {
            return;
        }

        _cooldownTimer = 1f;
        _blackHoleContactCount++;

        // Trigger monologue on the 5th collision (unique event)
        if (_blackHoleContactCount == 5)
        {
            if (MonologueManager.Instance != null)
            {
                MonologueManager.Instance.TriggerMonologueDirect("Dumb...", 3f);
            }
        }

        // Reset jelly bounce to stop any squish animation and return scale to normal
        ball.GetComponent<BallJellyBounce>()?.ResetJellyState();
        ball.transform.localScale = Vector3.one;

        Rigidbody2D rb = ball.Rb;
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;

            // Calculate direction away from the black hole center
            Vector2 direction = ((Vector2)ball.transform.position - (Vector2)blackHole.transform.position);
            if (direction.sqrMagnitude < 0.001f)
            {
                direction = UnityEngine.Random.insideUnitCircle.normalized;
            }
            else
            {
                direction.Normalize();
            }

            // Immediately place the ball just outside the event horizon to prevent re-collision/stuck state
            float pushOffset = blackHole.GRadius + ball.ColliderRadius + 0.1f;
            rb.position = (Vector2)blackHole.transform.position + direction * pushOffset;

            // Clear residual velocity
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;

            // Apply a strong outward velocity impulse to escape the black hole's gravity fluidly
            if (ball.Passport != null)
            {
                ball.Passport.RequestVelocity(direction * _expelVelocity, PhysicsPriority.Behavior, VelocityMode.Override);
            }
            else
            {
                rb.linearVelocity = direction * _expelVelocity;
            }
            rb.angularVelocity = UnityEngine.Random.Range(-180f, 180f);
        }
    }
}
