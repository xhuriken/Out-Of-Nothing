using Shapes;
using UnityEngine;
using Shapes;
using System;
using UnityEngine;

/// <summary>
/// Represents a gravitational anomaly that attracts and consumes dynamic entities.
/// </summary>
[RequireComponent(typeof(Disc))]
public class BlackHole : MonoBehaviour
{
    [Header("Physics Settings")]
    [SerializeField]
    private float _attractForce = 25f;

    [SerializeField]
    private float _attractRadius = 2f;

    [SerializeField]
    private float _radius = 1f;

    [SerializeField]
    [Tooltip("Defines which layers the black hole can interact with (e.g., Balls, Machines).")]
    private LayerMask _targetLayerMask;

    [Header("Growth Settings")]
    [SerializeField]
    private float _growthAmount = 0.03f;

    private readonly Collider2D[] _collidersBuffer = new Collider2D[64];
    private Disc _renderer;

    private void Awake()
    {
        _renderer = GetComponent<Disc>();
        UpdateVisuals();
    }

    private void FixedUpdate()
    {
        int count = Physics2D.OverlapCircleNonAlloc(transform.position, _attractRadius, _collidersBuffer, _targetLayerMask);

        for (int i = 0; i < count; i++)
        {
            Collider2D col = _collidersBuffer[i];
            Rigidbody2D targetRb = col.attachedRigidbody;

            if (targetRb == null)
            {
                continue;
            }

            Vector2 direction = (Vector2)transform.position - targetRb.position;
            float distance = direction.magnitude;

            if (distance <= _radius)
            {
                ConsumeEntity(col.gameObject);
            }
            else
            {
                AttractEntity(targetRb, direction, distance);
            }
        }
    }

    /// <summary>
    /// Applies a gravitational pull to the target.
    /// The force intensifies as the object gets closer to the event horizon.
    /// </summary>
    private void AttractEntity(Rigidbody2D targetRb, Vector2 direction, float distance)
    {
        // Normalize the direction vector
        Vector2 pullDirection = direction / distance;

        // Calculate force intensity inversely proportional to the distance
        float forceMultiplier = 1f - (distance / _attractRadius);

        targetRb.AddForce(pullDirection * _attractForce * forceMultiplier, ForceMode2D.Force);

        // TODO: détecter si c'est une boulle qu'on est en train de drag
        GameInputManager.Instance.ForceDrop();
    }

    /// <summary>
    /// Destroys or recycles the entity and triggers the black hole growth.
    /// </summary>
    private void ConsumeEntity(GameObject targetObject)
    {
        // Check for BallEntity to properly use the Object Pool
        if (targetObject.TryGetComponent(out BallEntity ball))
        {
            BallPoolManager.Instance.ReleaseBall(ball);
        }
        else if (targetObject.TryGetComponent(out MachineEntity machine))
        {
            // Machines are not pooled yet, destroy them normally TODO POLLED IT ?
            Destroy(machine.gameObject);
        }
        else
        {
            Destroy(targetObject);
        }

        //IncrementManager.Instance.AddPoints(ball.Data.point);
        // si il bouffe une machine, on donne le nombre de point qu'elle à couter a crafter ?

        NothingLoveEating();
    }

    /// <summary>
    /// Grows the black hole radius and visual size after consuming an entity.
    /// </summary>
    private void NothingLoveEating()
    {
        _radius += _growthAmount;
        _attractRadius += _growthAmount;

        UpdateVisuals();
    }

    /// <summary>
    /// Synchronizes the visual rendering with the physical data.
    /// </summary>
    private void UpdateVisuals()
    {
        if (_renderer != null)
        {
            _renderer.Radius = _radius;
            // Shader too ?
        }
    }

    /// <summary>
    /// Draws debug gizmos in the Unity Editor to visualize the radius.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.black;
        Gizmos.DrawWireSphere(transform.position, _radius);

        Gizmos.color = Color.grey;
        Gizmos.DrawWireSphere(transform.position, _attractRadius);
    }
}