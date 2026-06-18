using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Data representing an object currently attracted by the black hole.
/// </summary>
public struct AttractedObjectData
{
    /// <summary>
    /// How deep the object is in the attraction zone (0 at the outer edge, 1 at the event horizon).
    /// </summary>
    public float Depth;

    /// <summary>
    /// The BallEntity component of the attracted object, if it is a ball.
    /// </summary>
    public BallEntity Ball;

    /// <summary>
    /// The MachineEntity component of the attracted object, if it is a machine.
    /// </summary>
    public MachineEntity Machine;
}

/// <summary>
/// Handles gravity attraction forces and event horizon consumption for the BlackHole.
/// </summary>
[RequireComponent(typeof(BlackHole))]
public class BlackHolePhysics : MonoBehaviour
{
    [Header("Physics Settings")]
    [SerializeField]
    [Tooltip("The force applied to pull objects toward the center.")]
    private float _attractForce = 30f;

    [SerializeField]
    [Tooltip("The offset added to the event horizon radius to define the outer attraction range.")]
    private float _attractRadiusOffset = 2f;

    [SerializeField]
    [Tooltip("Defines which layers the black hole can interact with (e.g., Balls, Machines).")]
    private LayerMask _targetLayerMask;

    private BlackHole _blackHole;
    private readonly Collider2D[] _collidersBuffer = new Collider2D[64];
    private readonly Dictionary<Transform, AttractedObjectData> _attractedObjectsThisFrame = new Dictionary<Transform, AttractedObjectData>();

    /// <summary>
    /// Gets the list of attracted objects and their capture depth during this frame.
    /// </summary>
    public IReadOnlyDictionary<Transform, AttractedObjectData> AttractedObjects => _attractedObjectsThisFrame;

    /// <summary>
    /// Exposes the attraction radius offset.
    /// </summary>
    public float AttractRadiusOffset => _attractRadiusOffset;

    /// <summary>
    /// Initializes reference to the core BlackHole component.
    /// </summary>
    private void Awake()
    {
        _blackHole = GetComponent<BlackHole>();
    }

    /// <summary>
    /// Processes physical attraction force and event horizon checks on target entities.
    /// </summary>
    private void FixedUpdate()
    {
        if (_blackHole == null)
        {
            return;
        }

        float gRadius = _blackHole.GRadius;
        float currentAttractRadius = _blackHole.OverrideAttractShader ? _blackHole.CurrentAttractPhysicsRadius : (gRadius + _attractRadiusOffset);
        int count = Physics2D.OverlapCircleNonAlloc(transform.position, currentAttractRadius, _collidersBuffer, _targetLayerMask);

        _attractedObjectsThisFrame.Clear();

        for (int i = 0; i < count; i++)
        {
            Collider2D col = _collidersBuffer[i];
            Rigidbody2D targetRb = col.attachedRigidbody;

            if (targetRb == null)
            {
                continue;
            }

            bool alreadyProcessed = false;
            for (int j = 0; j < i; j++)
            {
                if (_collidersBuffer[j].attachedRigidbody == targetRb)
                {
                    alreadyProcessed = true;
                    break;
                }
            }
            if (alreadyProcessed)
            {
                continue;
            }

            Vector2 closestPoint = col.ClosestPoint(transform.position);
            Vector2 direction = (Vector2)transform.position - targetRb.position;
            float distanceToCenter = direction.magnitude;
            float distanceToEdge = Vector2.Distance(transform.position, closestPoint);

            // Consume only if the center has crossed the event horizon
            if (distanceToCenter <= gRadius)
            {
                _blackHole.ConsumeEntity(targetRb.gameObject);
            }
            // Attract if the edge has entered the attraction zone
            else if (distanceToEdge <= currentAttractRadius)
            {
                AttractEntity(targetRb, direction, distanceToEdge, gRadius, currentAttractRadius);

                // Track for visual glitch
                float range = currentAttractRadius - gRadius;
                if (range > 0f)
                {
                    float distanceFromHorizon = distanceToEdge - gRadius;
                    float depth = 1f - Mathf.Clamp01(distanceFromHorizon / range);
                    BallEntity ball = targetRb.GetComponent<BallEntity>();
                    MachineEntity machine = ball == null ? targetRb.GetComponent<MachineEntity>() : null;
                    _attractedObjectsThisFrame[targetRb.transform] = new AttractedObjectData 
                    { 
                        Depth = depth, 
                        Ball = ball, 
                        Machine = machine 
                    };
                }
            }
        }
    }

    /// <summary>
    /// Applies a gravitational pull to the target.
    /// </summary>
    private void AttractEntity(Rigidbody2D targetRb, Vector2 direction, float distanceToEdge, float gRadius, float attractRadius)
    {
        float distanceToCenter = direction.magnitude;
        if (distanceToCenter <= 0f)
        {
            return;
        }

        Vector2 pullDirection = direction / distanceToCenter;
        
        float range = attractRadius - gRadius;
        if (range <= 0f)
        {
            return;
        }

        float distanceFromHorizon = distanceToEdge - gRadius;
        float forceMultiplier = 1f - Mathf.Clamp01(distanceFromHorizon / range);

        if (targetRb.bodyType == RigidbodyType2D.Kinematic)
        {
            float pullSpeed = _attractForce * forceMultiplier * 0.1f;
            Vector2 targetPos = Vector2.MoveTowards(targetRb.position, transform.position, pullSpeed * Time.fixedDeltaTime);
            targetRb.MovePosition(targetPos);
        }
        else
        {
            float force = _attractForce * forceMultiplier;

            if (targetRb.GetComponent<BallEntity>() != null)
            {
                force *= 1.5f;
            }

            targetRb.AddForce(pullDirection * force, ForceMode2D.Force);
        }
    }

    /// <summary>
    /// Draws wire sphere gizmos for general debugging.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (_blackHole == null)
        {
            _blackHole = GetComponent<BlackHole>();
        }
        if (_blackHole == null)
        {
            return;
        }

        float gRadius = _blackHole.GRadius;
        Gizmos.color = new Color(0f, 1f, 1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, gRadius);

        Gizmos.color = new Color(1f, 0.6f, 0f, 0.2f);
        float attractRadius = _blackHole.OverrideAttractShader ? _blackHole.CurrentAttractPhysicsRadius : (gRadius + _attractRadiusOffset);
        Gizmos.DrawWireSphere(transform.position, attractRadius);
    }

    /// <summary>
    /// Draws wire sphere gizmos when selected.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (_blackHole == null)
        {
            _blackHole = GetComponent<BlackHole>();
        }
        if (_blackHole == null)
        {
            return;
        }

        float gRadius = _blackHole.GRadius;
        Gizmos.color = new Color(0f, 1f, 1f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, gRadius);

        Gizmos.color = new Color(1f, 0.6f, 0f, 0.5f);
        float attractRadius = _blackHole.OverrideAttractShader ? _blackHole.CurrentAttractPhysicsRadius : (gRadius + _attractRadiusOffset);
        Gizmos.DrawWireSphere(transform.position, attractRadius);
    }
}
