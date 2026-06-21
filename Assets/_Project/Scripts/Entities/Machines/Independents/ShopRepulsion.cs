using UnityEngine;

/// <summary>
/// Component dedicated to repelling balls and other machines away from the shop.
/// Excludes itself during drag-and-drop or shop slot interactions.
/// </summary>
[RequireComponent(typeof(Shop))]
public class ShopRepulsion : MonoBehaviour
{
    [Header("Repulsion settings")]
    [SerializeField]
    [Tooltip("The force applied to repel objects away from the shop.")]
    private float _repelForce = 30f;

    [SerializeField]
    [Tooltip("Offset added to shop's GRadius to define the outer boundary of the repulsion zone.")]
    private float _repelRadiusOffset = 2f;

    [SerializeField]
    [Tooltip("Defines which layers are repelled by the passive force field.")]
    private LayerMask _repelLayerMask;

    private Shop _shop;
    private readonly Collider2D[] _repelCollidersBuffer = new Collider2D[64];

    /// <summary>
    /// Retrieves the reference to the main Shop component.
    /// </summary>
    private void Awake()
    {
        _shop = GetComponent<Shop>();
    }

    /// <summary>
    /// Applies repulsion forces to balls and machines in FixedUpdate.
    /// </summary>
    private void FixedUpdate()
    {
        if (_shop == null)
        {
            return;
        }

        // Repulse at all times (including drag and UI active states)

        RepelEntities();
    }

    /// <summary>
    /// Scans and repels all colliders inside the repulsion radius.
    /// </summary>
    private void RepelEntities()
    {
        float gRadius = _shop.GRadius;
        float currentRepelRadius = gRadius + _repelRadiusOffset;
        int count = Physics2D.OverlapCircleNonAlloc(transform.position, currentRepelRadius, _repelCollidersBuffer, _repelLayerMask);

        for (int i = 0; i < count; i++)
        {
            Collider2D col = _repelCollidersBuffer[i];
            if (col.gameObject == gameObject)
            {
                continue;
            }

            Rigidbody2D targetRb = col.attachedRigidbody;
            if (targetRb == null)
            {
                continue;
            }

            // Prevent processing the same Rigidbody multiple times if it has multiple colliders
            bool alreadyProcessed = false;
            for (int j = 0; j < i; j++)
            {
                if (_repelCollidersBuffer[j].attachedRigidbody == targetRb)
                {
                    alreadyProcessed = true;
                    break;
                }
            }
            if (alreadyProcessed)
            {
                continue;
            }

            Vector2 direction = targetRb.position - (Vector2)transform.position;
            float distanceToCenter = direction.magnitude;
            if (distanceToCenter <= 0f)
            {
                continue;
            }

            Vector2 pushDirection = direction.normalized;
            float range = currentRepelRadius - gRadius;
            if (range <= 0f)
            {
                continue;
            }

            Vector2 closestPoint = col.ClosestPoint(transform.position);
            float distanceToEdge = Vector2.Distance(transform.position, closestPoint);

            if (distanceToEdge <= currentRepelRadius)
            {
                float distanceFromHorizon = distanceToEdge - gRadius;
                float forceMultiplier = 1f - Mathf.Clamp01(distanceFromHorizon / range);

                if (targetRb.bodyType == RigidbodyType2D.Kinematic)
                {
                    // Push kinematic machines smoothly
                    float pushSpeed = _repelForce * forceMultiplier * 0.1f;
                    Vector2 movement = pushDirection * (pushSpeed * Time.fixedDeltaTime);
                    float distance = movement.magnitude;

                    if (distance > 0f)
                    {
                        ContactFilter2D contactFilter = new ContactFilter2D();
                        contactFilter.SetLayerMask(_repelLayerMask);
                        contactFilter.useLayerMask = true;
                        contactFilter.useTriggers = false;

                        RaycastHit2D[] hits = new RaycastHit2D[5];
                        int hitCount = targetRb.Cast(pushDirection, contactFilter, hits, distance);

                        if (hitCount > 0)
                        {
                            float minFraction = 1f;
                            for (int hitIdx = 0; hitIdx < hitCount; hitIdx++)
                            {
                                if (hits[hitIdx].collider.gameObject == targetRb.gameObject || hits[hitIdx].collider.gameObject == gameObject)
                                {
                                    continue;
                                }
                                if (hits[hitIdx].fraction < minFraction)
                                {
                                    minFraction = hits[hitIdx].fraction;
                                }
                            }
                            distance *= minFraction;
                            if (distance < 0.001f)
                            {
                                distance = 0f;
                            }
                        }

                        if (distance > 0f)
                        {
                            Vector2 targetPos = targetRb.position + pushDirection * distance;
                            if (GameZone.Instance != null)
                            {
                                float radius = 0.5f;
                                var machine = targetRb.GetComponent<MachineEntity>();
                                if (machine != null)
                                {
                                    radius = machine.PhysicalRadius;
                                }
                                else
                                {
                                    var circleCol = targetRb.GetComponent<CircleCollider2D>();
                                    if (circleCol != null)
                                    {
                                        radius = circleCol.radius * targetRb.transform.lossyScale.x;
                                    }
                                }
                                targetPos.x = Mathf.Clamp(targetPos.x, GameZone.Instance.MinX + radius, GameZone.Instance.MaxX - radius);
                                targetPos.y = Mathf.Clamp(targetPos.y, GameZone.Instance.MinY + radius, GameZone.Instance.MaxY - radius);
                            }
                            targetRb.MovePosition(targetPos);
                        }
                    }
                }
                else
                {
                    // Push dynamic balls using physics forces
                    float force = _repelForce * forceMultiplier;
                    if (targetRb.GetComponent<BallEntity>() != null)
                    {
                        force *= 1.5f; // Extra push for balls to feel dynamic
                    }
                    targetRb.AddForce(pushDirection * force, ForceMode2D.Force);
                }
            }
        }
    }

    /// <summary>
    /// Draws wire sphere gizmos for general debugging.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (_shop == null)
        {
            _shop = GetComponent<Shop>();
        }
        if (_shop == null)
        {
            return;
        }

        float gRadius = _shop.GRadius;
        Gizmos.color = new Color(0f, 1f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, gRadius);

        Gizmos.color = new Color(0.5f, 1f, 0.5f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, gRadius + _repelRadiusOffset);
    }
}
