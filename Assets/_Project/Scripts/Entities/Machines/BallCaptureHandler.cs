using DG.Tweening;
using UnityEngine;

/// <summary>
/// Reusable helper component to handle ball capturing, lock state, and ejection opposite to the entry direction.
/// </summary>
public class BallCaptureHandler : MonoBehaviour
{
    [Header("Capture Animation Settings")]
    [SerializeField] private float _captureMoveDuration = 0.4f;
    [SerializeField] private Ease _captureEase = Ease.OutQuad;
    [SerializeField] private float _disableCollisionDuration = 0.5f;

    public BallEntity CapturedBall { get; private set; }
    public Vector2 EntryDirection { get; set; }
    private Vector3 _lastCapturePosition;

    private void Start()
    {
        _lastCapturePosition = transform.position;
    }

    /// <summary>
    /// Attempts to capture a ball. Locks its physics and tweens its position to the target.
    /// </summary>
    public bool Capture(BallEntity ball, Vector3 targetPosition, System.Action onComplete = null)
    {
        if (CapturedBall != null || ball == null) return false;

        _lastCapturePosition = targetPosition;

        // Force input drop in case player is holding it
        if (GameInputManager.Instance != null)
        {
            GameInputManager.Instance.ForceDrop();
        }

        // Calculate entry direction based on velocity or spatial offset relative to target capture center
        Vector2 velocity = Vector2.zero;
        if (ball.Passport != null)
        {
            velocity = ball.Passport.TrueVelocity;
        }
        else if (ball.Rb != null)
        {
            velocity = ball.Rb.linearVelocity;
        }

        Vector2 offset = (Vector2)ball.transform.position - (Vector2)targetPosition;

        if (velocity.sqrMagnitude > 0.05f)
        {
            // If the ball is moving, it entered from the opposite side of its travel direction
            EntryDirection = -velocity.normalized;
        }
        else if (offset.sqrMagnitude > 0.0001f)
        {
            // Fallback for static/dropped balls: use spatial offset from the capture center
            EntryDirection = offset.normalized;
        }
        else
        {
            EntryDirection = Vector2.right; // Fallback
        }

        CapturedBall = ball;

        // Make the ball completely non-interactive
        if (ball.Collider != null)
        {
            ball.Collider.enabled = false;
        }
        ball.IsProcessing = true;

        if (ball.Passport != null)
        {
            ball.Passport.SetLockState(true);
        }

        // Move the ball fluidly to the center/target position
        ball.transform.DOMove(targetPosition, _captureMoveDuration)
            .SetEase(_captureEase)
            .OnComplete(() => onComplete?.Invoke());

        return true;
    }

    /// <summary>
    /// Releases the captured ball without transforming it (resets it to original state).
    /// </summary>
    public void ReleaseCapturedBall()
    {
        if (CapturedBall == null) return;

        var ball = CapturedBall;
        CapturedBall = null;

        if (ball.Collider != null)
        {
            ball.Collider.enabled = true;
        }
        ball.IsProcessing = false;

        if (ball.Passport != null)
        {
            ball.Passport.SetLockState(false);
        }
    }

    /// <summary>
    /// Clears the internal reference to the captured ball without changing its state.
    /// Useful if the ball has already been destroyed or recycled.
    /// </summary>
    public void ClearReference()
    {
        CapturedBall = null;
    }

    /// <summary>
    /// Destroys/releases the captured ball, spawns a new ball, and ejects it in the opposite direction.
    /// </summary>
    public BallEntity TransformAndEject(BallDataSO newBallData, float ejectionForce, float ejectionOffset = 1.2f, float disableCollisionDuration = -1f)
    {
        if (CapturedBall == null) return null;

        float duration = disableCollisionDuration >= 0f ? disableCollisionDuration : _disableCollisionDuration;

        // Release the captured ball back to the pool
        BallPoolManager.Instance.ReleaseBall(CapturedBall);
        CapturedBall = null;

        // Calculate ejection direction (opposite of entry) and spawn position relative to last capture position
        Vector2 ejectionDir = -EntryDirection;
        Vector3 spawnPos = _lastCapturePosition + (Vector3)ejectionDir * ejectionOffset;

        // Spawn new ball
        BallEntity newBall = BallPoolManager.Instance.SpawnBall(newBallData, spawnPos);
        if (newBall != null)
        {
            // Temporarily ignore collisions with the machine's colliders
            IgnoreCollisionWithMachine(newBall.Collider, true);

            // Temporarily increase mass to easily push other balls out of the way
            float massMultiplier = newBall.SetTemporaryHeavyMass(0.4f, 50f);

            // Apply impulse force
            if (newBall.Rb != null)
            {
                newBall.Rb.linearVelocity = Vector2.zero; // Clear any default pool velocity
                newBall.Rb.AddForce(ejectionDir * (ejectionForce * massMultiplier), ForceMode2D.Impulse);
            }

            // Restore collision ignore after delay
            DOVirtual.DelayedCall(duration, () =>
            {
                if (newBall != null && this != null)
                {
                    IgnoreCollisionWithMachine(newBall.Collider, false);
                }
            });
        }

        return newBall;
    }

    /// <summary>
    /// Ejects the currently captured ball in the opposite direction without transforming it.
    /// </summary>
    public void EjectCapturedBall(float ejectionForce, float ejectionOffset = 1.2f, float disableCollisionDuration = -1f)
    {
        if (CapturedBall == null) return;

        float duration = disableCollisionDuration >= 0f ? disableCollisionDuration : _disableCollisionDuration;

        var ball = CapturedBall;
        CapturedBall = null;

        // Re-enable ball
        if (ball.Collider != null)
        {
            ball.Collider.enabled = true;
        }
        ball.IsProcessing = false;

        if (ball.Passport != null)
        {
            ball.Passport.SetLockState(false);
        }

        Vector2 ejectionDir = -EntryDirection;
        ball.transform.position = _lastCapturePosition + (Vector3)ejectionDir * ejectionOffset;

        // Ignore collisions with machine
        IgnoreCollisionWithMachine(ball.Collider, true);

        // Apply temporary heavy mass and ejection impulse
        float massMultiplier = ball.SetTemporaryHeavyMass(0.4f, 50f);
        if (ball.Rb != null)
        {
            ball.Rb.linearVelocity = Vector2.zero;
            ball.Rb.AddForce(ejectionDir * (ejectionForce * massMultiplier), ForceMode2D.Impulse);
        }

        // Restore collisions after a short delay
        DOVirtual.DelayedCall(duration, () =>
        {
            if (ball != null && this != null)
            {
                IgnoreCollisionWithMachine(ball.Collider, false);
            }
        });
    }

    private void IgnoreCollisionWithMachine(Collider2D ballCollider, bool ignore)
    {
        if (ballCollider == null) return;

        // Start search from the machine root
        Transform machineRoot = transform;
        var machine = GetComponentInParent<MachineEntity>();
        if (machine != null)
        {
            machineRoot = machine.transform;
        }

        // Get all colliders on the machine and its children
        Collider2D[] machineColliders = machineRoot.GetComponentsInChildren<Collider2D>(true);

        foreach (var col in machineColliders)
        {
            if (col != null && col != ballCollider)
            {
                Physics2D.IgnoreCollision(col, ballCollider, ignore);
            }
        }
    }
}
