using UnityEngine;

/// <summary>
/// Forwards collision events from child colliders to the main machine entity.
/// Allows a single machine to have multiple distinct hitboxes.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class MachineColliderProxy : MonoBehaviour
{
    [SerializeField]
    private MachineEntity _parentMachine;

    [SerializeField]
    [Tooltip("Identifies which part of the machine this collider represents !")]
    private string _partId;

    // Maybe later we need OnTriggerEnter2D, or OnTrigger Stay, or Exit ? IDK FUCK
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (_parentMachine != null)
        {
            _parentMachine.OnPartCollisionEnter(_partId, collision);
        }
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (_parentMachine != null)
        {
            _parentMachine.OnPartTriggerEnter(_partId, collider);
        }
    }
}