using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Singleton class that continuously follows the game mouse cursor with customizable smoothing.
/// Expected to contain Shapes components on this GameObject.
/// </summary>
public class GameCursor : MonoBehaviour
{
    public static GameCursor Instance { get; private set; }

    [Header("Tracking Settings")]
    [Tooltip("How fast the cursor catches up to the mouse position (lower is faster).")]
    [SerializeField] private float _smoothTime = 0.05f;

    [Tooltip("Maximum speed of the cursor.")]
    [SerializeField] private float _maxSpeed = Mathf.Infinity;

    private Camera _mainCamera;
    private Vector3 _velocity = Vector3.zero;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Hide the default system cursor
        Cursor.visible = false;
    }

    private void Start()
    {
        _mainCamera = Camera.main; // TODO: Cache this properly based on project rules
    }

    private void Update()
    {
        if (_mainCamera == null) return;

        // Get target world position based on mouse screen position
        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
        Vector3 targetWorldPosition = _mainCamera.ScreenToWorldPoint(mouseScreenPosition);
        targetWorldPosition.z = 0f; // Keep it on the 2D plane

        // Smoothly move the cursor towards the target position
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetWorldPosition,
            ref _velocity,
            _smoothTime,
            _maxSpeed
        );
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            // Optionally restore the cursor if this manager gets destroyed
            Cursor.visible = true;
            Instance = null;
        }
    }
}
