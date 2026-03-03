using Shapes;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Main controller for ball entities. Manages physics and delegates logic to behavior.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class BallEntity : MonoBehaviour, IDraggable
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
    private bool _isBeingDragged;

    /// <summary>
    /// Exposes the configuration data.
    /// </summary>
    public BallDataSO Data => _data;

    /// <summary>
    /// Exposes the Rigidbody2D component.
    /// </summary>
    public Rigidbody2D Rb => _rb;

    /// <summary>
    /// Exposes the Disc renderer component. (From shapes lib)
    /// </summary>
    public Disc Renderer => _renderer;

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

    /// <summary>
    /// Handles the technical process of duplication.
    /// Called by behaviors to perform the standard spawn.
    /// </summary>
    public void PerformDefaultDuplicate()
    {
        BallEntity newBall = BallPoolManager.Instance.SpawnBall(_data, transform.position);

        // Ejection force using current mass
        Vector2 ejectionDirection = Random.insideUnitCircle.normalized;
        newBall.Rb.AddForce(ejectionDirection * 5f, ForceMode2D.Impulse);

        // TODO: Trigger DOTween visual feedback here
    }

    /// <summary>
    /// Initializes or resets the ball with new configuration data.
    /// Used primarily by the BallPoolManager when recycling instances from the pool.
    /// </summary>
    /// <param name="newData">The ball configuration to apply.</param>
    public void Initialize(BallDataSO newData)
    {
        _data = newData;

        // Reset runtime tracking variables to prevent state carry-over from previous life
        _currentClickCount = 0;
        _lastClickTime = 0f;

        // Prototype Pattern: Clone the behavior to ensure independent logic state
        if (_data != null && _data.behaviorTemplate != null)
        {
            _runtimeBehavior = _data.behaviorTemplate.Clone();
        }
        else
        {
            _runtimeBehavior = null;
        }

        // Apply visual and physical properties defined in the ScriptableObject
        UpdateVisualsAndPhysics();
    }

    #region Drag

    /// <summary>
    /// Sets the ball to Kinematic to allow manual position updates via drag.
    /// </summary>
    public void OnDragStart()
    {
        _isBeingDragged = true;
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.linearVelocity = Vector2.zero;
        _runtimeBehavior?.OnDragStart(this);
    }

    /// <summary>
    /// Updates the position during the drag.
    /// TODO LATER: THE DRAG ADD FORCE TO THE BALL INSTEAD OF TELEPORTING IT, TO PRESERVE PHYSICAL INTERACTIONS WITH OTHER OBJECTS.
    /// ON RELEASE, WE REPLACE HIS ACTUAL VELOCITY BY HIS LAST BEFORE DRAGGING
    /// </summary>
    public void OnDragUpdate(Vector2 position)
    {
        transform.position = position;
    }

    /// <summary>
    /// Resets the ball to Dynamic to resume physical interactions.
    /// </summary>
    public void OnDragEnd()
    {
        _isBeingDragged = false;
        _rb.bodyType = RigidbodyType2D.Dynamic;
        _runtimeBehavior?.OnDragEnd(this);
    }

    #endregion
}