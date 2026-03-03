using Shapes;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Represents a ball in game.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class BallEntity : MonoBehaviour
{
    [Required][SerializeField] private BallDataSO data;

    [SerializeField] private Disc renderer;

    private Rigidbody2D rb;
    private CircleCollider2D col;
    

    public Rigidbody2D Rb => rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CircleCollider2D>();

        InitializePhysics();
    }

    private void InitializePhysics()
    {
        rb.mass = data.mass;
        col.radius = data.radius;
        // Visual setup (Shapes) should be applied here using data.displayColor, data.radius, etc etc...
        renderer.Radius = data.radius;
        renderer.Color = data.displayColor;
    }

    private void FixedUpdate()
    {
        // Delegate physics logic to the injected behavior
        data.behavior?.ExecuteFixedUpdate(this, Time.fixedDeltaTime);
    }
}
