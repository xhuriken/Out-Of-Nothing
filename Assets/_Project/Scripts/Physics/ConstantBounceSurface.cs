using UnityEngine;

/// <summary>
/// Surface that reflects balls, ensuring a minimum speed upon reflection.
/// </summary>
public class ConstantBounceSurface : MonoBehaviour
{
    [Tooltip("If the incoming ball speed is below this threshold...")]
    [SerializeField] private float _thresholdSpeed = 5f;

    [Tooltip("...force the bounce speed to this value.")]
    [SerializeField] private float _boostSpeed = 15f;

    [SerializeField] private PhysicsPriority _priority = PhysicsPriority.Surface;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.TryGetComponent(out BallPhysicsPassport passport)) return;

        // 1. Force drop if the ball was being dragged
        if (passport.TryGetComponent(out IDraggable draggable) &&
            GameInputManager.Instance != null &&
            GameInputManager.Instance.CurrentDraggedObject == draggable)
        {
            GameInputManager.Instance.ForceDrop();
        }

        // 2. Determine the normal (Use GameZone's flawless normal if available, otherwise fallback to collision contact)
        Vector2 normal = collision.contacts[0].normal;
        if (TryGetComponent(out GameZone zone))
        {
            Vector3 nearest = zone.GetNearestSide(passport.transform.position);
            normal = new Vector2(-nearest.x, -nearest.y);
        }

        // 3. Get incoming velocity (bypass Unity's physics bugs by using TrueVelocity)
        Vector2 incomingVelocity = passport.TrueVelocity;
        if (incomingVelocity.sqrMagnitude < 0.1f)
        {
            incomingVelocity = passport.Rb.linearVelocity;
            if (incomingVelocity.sqrMagnitude < 0.1f)
            {
                incomingVelocity = collision.relativeVelocity * -1f;
            }
        }

        // 4. Calculate bounce direction
        Vector2 bounceDirection;
        if (Vector2.Dot(incomingVelocity, normal) > 0)
        {
            // Already escaping, just keep direction
            bounceDirection = incomingVelocity.normalized;
        }
        else
        {
            // Reflect perfectly
            bounceDirection = Vector2.Reflect(incomingVelocity.normalized, normal).normalized;
            
            // Failsafe to ensure it points outwards
            if (Vector2.Dot(bounceDirection, normal) <= 0.1f)
            {
                bounceDirection = (bounceDirection + normal).normalized;
            }
        }

        // 5. Apply the speed rule
        float currentSpeed = incomingVelocity.magnitude;
        float finalSpeed = currentSpeed;
        
        if (currentSpeed < _thresholdSpeed)
        {
            finalSpeed = _boostSpeed;
        }

        // 6. Apply velocity
        passport.RequestVelocity(bounceDirection * finalSpeed, _priority, VelocityMode.Override);

        // 7. Manual Depenetration to prevent Unity from launching it sideways
        float separation = collision.GetContact(0).separation;
        if (separation < 0)
        {
            passport.Rb.position += normal * (-separation + 0.05f);
        }
    }
}