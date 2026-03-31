using UnityEngine;

/// <summary>
/// A simple machine that push balls on contact.
/// Requires no energy or storage.
/// </summary>
public class BumperMachine : MachineEntity
{
    [SerializeField]
    private float _repulsionForce = 10f;

    private PhysicsPriority _physicsPriority = PhysicsPriority.Machine;

    private void Start()
    {
    }

    // TODO: WE NEED TRIGGER COLLIDER FOR BALLS
    // AND WE NEED COLLIDER FOR OTHER MACHINE, THIS ONE, BALLS CANT USE IT ! (LAYER Matrix)

    /// <summary>
    /// Handles collisions specifically for the bumper mechanics.
    /// </summary>
    public override void OnPartCollisionEnter(string partId, Collision2D collision)
    {
        if (!_isRunning)
        {
            return;
        }

        if (partId == "Bumper" && collision.gameObject.TryGetComponent(out BallEntity ball))
        {
            // Calculate repulsion direction based on the contact point (copie from brown ball)
            Vector2 normal = collision.contacts[0].normal;
            // TODO: MAKE AN ORIENTATION SYSTEM FOR THE BUMPER AND ALL OTHER MACHINE, WHO ARE USABLE WITH GAMEINPUT !
            // TODO: use this system for the direction of the repulsion !

            // Apply impulse force
            ball.Passport.ApplyExternalForce(normal * _repulsionForce, _physicsPriority, ForceMode2D.Impulse);
            //ball.Rb.AddForce(normal * _repulsionForce, ForceMode2D.Impulse);

            // TODO: Play Bumper animation and sound
        }
    }
}
