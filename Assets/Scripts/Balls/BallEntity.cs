using Shapes;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Main controller for ball entities. Manages physics and delegates logic to behavior.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class BallEntity : MonoBehaviour
{
    [Required]
    [SerializeField]
    private BallDataSO _data;

    [SerializeField] private Disc _renderer;

    private float _lastClickTime;
    private int _currentClickCount;
    private Rigidbody2D _rb;
    private BallBehavior _runtimeBehavior;
    private CircleCollider2D _collider;

    /// <summary>
    /// Exposes the configuration data.
    /// </summary>
    public BallDataSO Data
    {
        get { return _data; }
    }

    /// <summary>
    /// Exposes the Rigidbody2D component.
    /// </summary>
    public Rigidbody2D Rb
    {
        get { return _rb; }
    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();

        // Critical: Create an independent instance to hold local state
        if (_data.behaviorTemplate != null)
        {
            _runtimeBehavior = _data.behaviorTemplate.Clone();
        }

        UpdateVisualsAndPhysics();
    }

    private void FixedUpdate()
    {
        _runtimeBehavior?.ExecuteFixedUpdate(this, Time.fixedDeltaTime);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        _runtimeBehavior?.OnCollisionEnter(this, collision);
    }

    /// <summary>
    /// Called strictly by the centralized GameInputManager.
    /// Handles per-ball anti-spam cooldown based on data configuration.
    /// </summary>
    public void ReceiveClick()
    {
        if (Time.time - _lastClickTime < _data.clickCooldown)
        {
            return;
        }

        _lastClickTime = Time.time;
        _currentClickCount++;

        _runtimeBehavior?.OnClick(this);

        if (_currentClickCount >= _data.clicksRequiredForDuplication)
        {
            _currentClickCount = 0;
            Duplicate();
        }
    }

    private void Duplicate()
    {
        _runtimeBehavior?.OnDuplicate(this);

        // Placeholder for Object Pooling spawn logic
        Debug.Log($"Duplicating {_data.id}");
    }


    // VISUAL PART

    /// <summary>
    /// Unity Editor method called when a script is loaded or a value is changed in the Inspector.
    /// Used to apply DataSO values to the GameObject without entering Play Mode.
    /// </summary>
    private void OnValidate()
    {
        UpdateVisualsAndPhysics();
    }

    /// <summary>
    /// Synchronizes the Unity components (Collider, Physics and Shapes Visual) with his current BallDataSO.
    /// </summary>
    private void UpdateVisualsAndPhysics()
    {
        if (_data == null)
        {
            return;
        }

        _renderer.ColorInner = _data.color;
        _renderer.Radius = _data.radius;

        if (_collider == null) _collider = GetComponent<CircleCollider2D>();
        if (_collider != null) _collider.radius = _data.radius + (_renderer.Thickness/2);

        if (_rb == null) _rb = GetComponent<Rigidbody2D>();
        if (_rb != null) _rb.gravityScale = 0f;
    }
}