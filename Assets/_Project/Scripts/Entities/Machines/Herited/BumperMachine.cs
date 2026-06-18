using UnityEngine;
using System.Collections;
using DG.Tweening;

/// <summary>
/// A machine that push balls on contact.
/// Requires no energy or storage.
/// </summary>
public class BumperMachine : MachineEntity
{
    public enum BumperPushDirection
    {
        BumperFacingUp,
        BumperFacingDown,
        ContactNormal
    }

    [Header("Bumper Settings")]
    [SerializeField] private float _repulsionForce = 10f;
    [SerializeField] private BumperPushDirection _pushDirectionMode = BumperPushDirection.BumperFacingDown;
    [SerializeField] private LayerMask _objectsLayerMask;

    [Header("Backwards Compatibility")]
    [Tooltip("Old field mapped to _repulsionForce if modified")]
    public float bumpForce = 5f;

    private PhysicsPriority _physicsPriority = PhysicsPriority.Machine;

    private readonly System.Collections.Generic.Dictionary<BallEntity, float> _lastPushTimes = new System.Collections.Generic.Dictionary<BallEntity, float>();

    public override bool IsDemanding => false;

    public static readonly System.Collections.Generic.List<BumperMachine> ActiveBumpers = new System.Collections.Generic.List<BumperMachine>();

    protected override void OnEnable()
    {
        ActiveBumpers.Add(this);
        IgnoreCollisionWithAllBalls();
    }

    protected override void OnDisable()
    {
        ActiveBumpers.Remove(this);
    }

    private void IgnoreCollisionWithAllBalls()
    {
        Collider2D myCollider = GetComponent<Collider2D>();
        if (myCollider == null) return;

        BallEntity[] balls = FindObjectsByType<BallEntity>(FindObjectsSortMode.None);
        foreach (var ball in balls)
        {
            if (ball != null && ball.Collider != null)
            {
                Physics2D.IgnoreCollision(myCollider, ball.Collider, true);
            }
        }
    }

    protected override void OnDestroy()
    {
        // Do not call base.OnDestroy since we didn't register to tick events
    }

    protected override void OnDrawGizmos()
    {
        // Bumper doesn't need energy connection radius gizmo, so we override it to do nothing.
    }

    protected override void Start()
    {
        base.Start();

        // Set default layer mask if not configured
        if (_objectsLayerMask == 0)
        {
            _objectsLayerMask = LayerMask.GetMask("Objects");
        }

        // Map old serialized bumpForce if it was configured differently from default
        if (bumpForce != 5f)
        {
            _repulsionForce = bumpForce;
        }

        // Enforce the default push direction to BumperFacingDown (tip direction)
        // if it was left to the ContactNormal default on the prefab, to ensure it acts as an accelerator
        if (_pushDirectionMode == BumperPushDirection.ContactNormal)
        {
            _pushDirectionMode = BumperPushDirection.BumperFacingDown;
        }
    }

    /// <summary>
    /// Handles collision on the parent solid collider directly.
    /// </summary>
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!_isRunning || IsBeingDragged) return;

        Vector2 normal = collision.contactCount > 0 ? collision.contacts[0].normal : (Vector2)transform.up;
        HandleBallContact(collision.collider, normal);
    }

    /// <summary>
    /// Handles collision with ball forwarded by proxy.
    /// </summary>
    public override void OnPartCollisionEnter(string partId, Collision2D collision)
    {
        if (!_isRunning || IsBeingDragged) return;

        if (partId == "Bumper")
        {
            Vector2 normal = collision.contactCount > 0 ? collision.contacts[0].normal : (Vector2)transform.up;
            HandleBallContact(collision.collider, normal);
        }
    }

    /// <summary>
    /// Handles trigger with ball forwarded by proxy.
    /// </summary>
    public override void OnPartTriggerEnter(string partId, Collider2D collider)
    {
        if (!_isRunning || IsBeingDragged) return;

        if (partId == "Bumper")
        {
            HandleBallContact(collider, null);
        }
    }

    private void HandleBallContact(Collider2D collider, Vector2? contactNormal)
    {
        // if (collider.gameObject.CompareTag("Ball") || collider.gameObject.CompareTag("FirstBall") || collider.gameObject.TryGetComponent<BallEntity>(out _))
        if (collider.gameObject.TryGetComponent<BallEntity>(out _))
        {
            Rigidbody2D ballRigidbody = collider.gameObject.GetComponent<Rigidbody2D>();
            BallEntity ballEntity = collider.gameObject.GetComponent<BallEntity>();

            if (ballEntity != null)
            {
                PrunePushTimes();
                if (_lastPushTimes.TryGetValue(ballEntity, out float lastTime))
                {
                    if (Time.time - lastTime < 0.2f)
                    {
                        return; // Cooldown active, prevent double-push
                    }
                }
                _lastPushTimes[ballEntity] = Time.time;
            }

            if (ballRigidbody != null)
            {
                // Prevent duplicate tweens and lock physics state
                ballRigidbody.transform.DOKill();
                ballRigidbody.bodyType = RigidbodyType2D.Kinematic;
                ballRigidbody.linearVelocity = Vector2.zero;
                ballRigidbody.angularVelocity = 0f;

                if (ballEntity != null)
                {
                    ballEntity.IsProcessing = true;
                }

                // Smoothly center the ball
                ballRigidbody.transform.DOMove(transform.position, 0.08f)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() =>
                    {
                        if (ballRigidbody == null) return;

                        // Restore physics state
                        ballRigidbody.bodyType = RigidbodyType2D.Dynamic;

                        if (ballEntity != null)
                        {
                            ballEntity.IsProcessing = false;
                        }

                        // Calculate repulsion direction
                        Vector2 pushDir = Vector2.zero;
                        switch (_pushDirectionMode)
                        {
                            case BumperPushDirection.BumperFacingUp:
                                pushDir = transform.up;
                                break;
                            case BumperPushDirection.BumperFacingDown:
                                pushDir = -transform.up;
                                break;
                            case BumperPushDirection.ContactNormal:
                                if (contactNormal.HasValue)
                                {
                                    pushDir = contactNormal.Value;
                                }
                                else
                                {
                                    pushDir = (ballRigidbody.transform.position - transform.position).normalized;
                                }
                                break;
                        }

                        // Override velocity directly to the tip direction with repulsion force, resetting previous momentum
                        if (ballEntity != null)
                        {
                            ballEntity.Passport.RequestVelocity(pushDir * _repulsionForce, _physicsPriority, VelocityMode.Override);
                        }
                        else
                        {
                            ballRigidbody.linearVelocity = pushDir * _repulsionForce;
                        }
                    });
            }
        }
    }

    private void PrunePushTimes()
    {
        float now = Time.time;
        var keysToRemove = new System.Collections.Generic.List<BallEntity>();
        foreach (var kvp in _lastPushTimes)
        {
            if (kvp.Key == null || now - kvp.Value > 1.0f)
            {
                keysToRemove.Add(kvp.Key);
            }
        }
        foreach (var key in keysToRemove)
        {
            _lastPushTimes.Remove(key);
        }
    }

    /// <summary>
    /// Detects overlap with other objects on drop and triggers repulsion.
    /// </summary>
    public override void OnDragEnd()
    {
        base.OnDragEnd();
        CheckLayerCollisionAndRepulse();
    }

    private void CheckLayerCollisionAndRepulse()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
            return;

        Collider2D[] hits = null;

        if (col is CircleCollider2D circle)
        {
            float radius = circle.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
            hits = Physics2D.OverlapCircleAll(transform.position, radius, _objectsLayerMask);
        }
        else if (col is PolygonCollider2D)
        {
            ContactFilter2D filter = new ContactFilter2D();
            filter.useTriggers = true;
            filter.SetLayerMask(_objectsLayerMask);
            Collider2D[] results = new Collider2D[10];
            int count = col.Overlap(filter, results);
            hits = new Collider2D[count];
            for (int i = 0; i < count; i++)
            {
                hits[i] = results[i];
            }
        }
        else
        {
            Vector2 size = col.bounds.size;
            float angle = transform.eulerAngles.z;
            hits = Physics2D.OverlapBoxAll(transform.position, size, angle, _objectsLayerMask);
        }

        if (hits == null)
            return;

        foreach (Collider2D hit in hits)
        {
            if (hit.gameObject != gameObject)
            {
                Debug.Log("Found collision with: " + hit.gameObject.name);
                RepulseDraggedWith(hit.gameObject);
                break;
            }
        }
    }

    private void RepulseDraggedWith(GameObject other)
    {
        Rigidbody2D myRb = GetComponent<Rigidbody2D>();
        if (myRb == null) return;

        bool thisWasKinematic = myRb.isKinematic;
        if (myRb.isKinematic)
        {
            myRb.isKinematic = false;
            myRb.bodyType = RigidbodyType2D.Dynamic;
            Debug.Log(gameObject.name + " set to dynamic for dragged repulsion");
        }

        Vector2 repulseDirection = (transform.position - other.transform.position).normalized;
        if (repulseDirection == Vector2.zero)
        {
            repulseDirection = Random.insideUnitCircle.normalized;
        }

        Debug.Log("RepulseDraggedWith direction: " + repulseDirection);
        myRb.AddForce(repulseDirection * _repulsionForce, ForceMode2D.Impulse);

        StartCoroutine(ResetKinematicState(myRb, thisWasKinematic));
    }

    private IEnumerator ResetKinematicState(Rigidbody2D rb, bool originalState)
    {
        yield return new WaitForSeconds(0.15f);
        rb.linearVelocity = Vector2.zero;
        rb.isKinematic = originalState;
        rb.bodyType = originalState ? RigidbodyType2D.Kinematic : RigidbodyType2D.Dynamic;
        Debug.Log("Reset " + rb.gameObject.name + " to isKinematic: " + originalState);
        CheckLayerCollisionAndRepulse();
    }

    protected override void OnTickExecuted()
    {
        // Bumper doesn't need tick executions (doesn't consume or generate energy over ticks)
    }
}
