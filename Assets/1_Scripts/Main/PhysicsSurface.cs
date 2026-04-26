using UnityEngine;

/// <summary>
/// Configurable surface that modifies ball physics on contact.
/// </summary>
public class PhysicsSurface : MonoBehaviour
{
    [SerializeField] private float _bouncinessMultiplier = 1.2f;
    [SerializeField] private float _absorptionFactor = 0.5f; // 0 = stop, 1 = no change
    [SerializeField] private bool _isSuperBouncy;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.TryGetComponent(out BallEntity ball))
        {
            if (_isSuperBouncy)
            {
                // Reflect and boost
                Vector2 reflect = Vector2.Reflect(collision.relativeVelocity, collision.contacts[0].normal);
                ball.Passport.RequestVelocity(reflect * _bouncinessMultiplier, PhysicsPriority.Surface, VelocityMode.Override);
            }
            else
            {
                // Just absorb some impact
                ball.Passport.RequestVelocity(Vector2.one * _absorptionFactor, PhysicsPriority.Surface, VelocityMode.Multiply);
            }
        }
    }
}