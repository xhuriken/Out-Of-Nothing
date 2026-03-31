using UnityEngine;

/// <summary>
/// Surface that reflects balls at a strictly defined constant speed.
/// </summary>
public class ConstantBounceSurface : MonoBehaviour
{
    [SerializeField] private float _constantBounceSpeed = 10f;
    [SerializeField] private PhysicsPriority _priority = PhysicsPriority.Surface;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.TryGetComponent(out BallEntity ball))
        {
            // Calculate direction based on impact normal
            Vector2 normal = collision.contacts[0].normal;
            Vector2 incomingVelocity = collision.relativeVelocity * -1f; // Relative to world
            Vector2 reflectDir = Vector2.Reflect(incomingVelocity, normal).normalized;

            // Apply fixed velocity regardless of impact force
            ball.Passport.RequestVelocity(
                reflectDir * _constantBounceSpeed,
                _priority,
                VelocityMode.Override
            );
        }
    }
}