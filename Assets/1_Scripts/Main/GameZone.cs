using Shapes;
using UnityEngine;

/// <summary>
/// Represents the border of the map. All object inside must bounce on collision.
/// </summary>
[RequireComponent(typeof(EdgeCollider2D))]
public class GameZone : MonoBehaviour
{
    // The global instance singleton
    public static GameZone Instance { get; private set; }

    [Header("Zone Settings")]
    [SerializeField] private float _width = 18f;
    [SerializeField] private float _height = 10f;
    [SerializeField] private Rectangle _rendering;
    [SerializeField] private float _tickness = 0.1f;

    [Header("Bounce Rules")]
    
    //[Tooltip("The minimum velocity magnitude applied to balls hitting the wall.")]
    //[SerializeField] private float _minBounceVelocity = 3f;

    private EdgeCollider2D _edgeCollider;

    // Limits properties for easy access
    // Left = minX, Right = maxX, Top = maxY, Bottom = minY
    // We must substract the tickness of the _renderer for better alignement
    public float MinX => - _width / 2f + _tickness;
    public float MaxX => _width / 2f - _tickness;
    public float MinY => - _height / 2f + _tickness;
    public float MaxY => _height / 2f - _tickness;

    private void Awake()
    {
        // Singleton pattern: if an instance already exists, destroy this one
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        // Set the instance to this object
        Instance = this;
        // not useful for now
        // DontDestroyOnLoad(gameObject);

        _edgeCollider = GetComponent<EdgeCollider2D>();
        UpdateBoundaries();
    }

    /// <summary>
    /// Unity Editor method called when a script is loaded or a value is changed in the Inspector.
    /// Ensures boundaries are updated immediately when tweaking size in the Editor.
    /// </summary>
    private void OnValidate()
    {
        if (_edgeCollider == null)
        {
            _edgeCollider = GetComponent<EdgeCollider2D>();
        }

        UpdateBoundaries();
    }

    /// <summary>
    /// Updates the EdgeCollider2D points to form a closed rectangle based on current dimensions.
    /// </summary>
    public void UpdateBoundaries()
    {
        if (_edgeCollider == null)
        {
            return;
        }

        // Update the rendering rectangle to match with size
        _rendering.Width = _width;
        _rendering.Height = _height;
        _rendering.Thickness = _tickness;

        // Define the 5 points to create a closed rectangular loop
        Vector2[] boundaryPoints = new Vector2[5];
        boundaryPoints[0] = new Vector2(MinX, MaxY); // Top Left
        boundaryPoints[1] = new Vector2(MaxX, MaxY); // Top Right
        boundaryPoints[2] = new Vector2(MaxX, MinY); // Bottom Right
        boundaryPoints[3] = new Vector2(MinX, MinY); // Bottom Left
        boundaryPoints[4] = new Vector2(MinX, MaxY); // Top Left (close loop)

        _edgeCollider.points = boundaryPoints;
    }


    public Vector3 GetNearestSide(Vector3 position)
    {
        // Compute distances between each side and the position
        float dLeft   = Mathf.Abs(position.x - MinX);
        float dRight  = Mathf.Abs(position.x - MaxX);
        float dBottom = Mathf.Abs(position.y - MinY);
        float dTop    = Mathf.Abs(position.y - MaxY);

        // search the minimal value between those 4 distances
        float min = Mathf.Min(Mathf.Min(dLeft, dRight), Mathf.Min(dBottom, dTop));

        return min switch
        {
            _ when min == dLeft => new Vector3(-1f, 0f, 1f),
            _ when min == dRight => new Vector3(1f, 0f, 1f),
            _ when min == dBottom => new Vector3(0f, -1f, 1f),
            _ => new Vector3(0f, 1f, 1f) // Top
        };
    }


    //public void OnCollisionEnter2D(Collision2D collision)
    //{
    //    // We only care about collisions with balls
    //    if (!collision.gameObject.TryGetComponent(out BallEntity ball))
    //    {
    //        return;
    //    }

    //    // Read the post-collision velocity magnitude
    //    float currentSpeed = ball.Rb.linearVelocity.magnitude;

    //    // If the ball is moving too slowly (or is stopped by a behavior), we enforce the minimum speed.
    //    if (currentSpeed < _minBounceVelocity)
    //    {
    //        // The bounce direction is already calculated by the PhysicsMaterial2D.
    //        // If the velocity is extremely low (near 0), we use the contact normal to push it away.
    //        Vector2 direction = currentSpeed > 0.1f
    //            ? ball.Rb.linearVelocity.normalized
    //            : collision.contacts[0].normal;

    //        ball.Rb.linearVelocity = direction * _minBounceVelocity;
    //    }
    //}

}
