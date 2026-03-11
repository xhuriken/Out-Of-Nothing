using DG.Tweening;
using Shapes;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Main controller for ball entities. Manages physics and delegates logic to behavior.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class BallEntity : MonoBehaviour, IDraggable
{
    [Required]
    [SerializeField] private BallDataSO _data;

    [Header("References")]
    [SerializeField] private Disc _renderer;
    [SerializeField] private ParticleSystem _particlesClick;
    [SerializeField] private ParticleSystem _particlesDuplicate;
    [Header("States")]
    [SerializeField] private bool _isProcessing;

    [Header("Settings")]
    [SerializeField]
    private float _dragForceMultiplier = 15f;

    private float _lastClickTime;
    private int _currentClickCount;
    private BallBehavior _runtimeBehavior;
    private CircleCollider2D _collider;

    [SerializeField] private float _energyOutputRadius = 2.5f;
    private Collider2D[] _energyResults = new Collider2D[10];
    private MachineEntity _energyTarget;

    /// <summary>
    /// Exposes the configuration data.
    /// </summary>
    public BallDataSO Data => _data;

    private Rigidbody2D _rb;
    /// <summary>
    /// Exposes the Rigidbody2D component.
    /// </summary>
    public Rigidbody2D Rb => _rb;

    /// <summary>
    /// Exposes the Disc renderer component. (From shapes lib)
    /// </summary>
    public Disc Renderer => _renderer;

    private bool _isBeingDragged;
    /// <summary>
    /// Exposes the drag state of the ball.
    /// </summary>
    public bool IsBeingDragged => _isBeingDragged;

    /// <summary>
    /// Exposes the collider radius.
    /// </summary>
    public float ColliderRadius => _data.radius + (_renderer.Thickness / 2);

    /// <summary>
    /// Exposes the process state
    /// </summary>
    public bool IsProcessing {
        get => _isProcessing;
        set 
        {
            _isProcessing = value;
        }
    }

    /// <summary>
    /// Exposes the Collider of the ball.
    /// </summary>
    public CircleCollider2D Collider => _collider;


    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();

        // Create an independent instance to hold local state
        if (_data.behaviorTemplate != null)
        {
            _runtimeBehavior = _data.behaviorTemplate.Clone();
        }

        // Apply initial configuration from the ScriptableObject
        if (_data != null)
        {
            Initialize(_data);
        }
        else
        {
            Debug.LogError("Sus, c'est senser ne jamais arriver.. La terre est plate ?");
            UpdateVisualsAndPhysics();
        }
    }

    private void FixedUpdate()
    {
        if (IsBeingDragged || IsProcessing) return;
        _runtimeBehavior?.ExecuteFixedUpdate(this, Time.fixedDeltaTime);
    }

    /// <summary>
    /// Initializes or resets the ball with new configuration data.
    /// Used primarily by the BallPoolManager when recycling instances from the pool.
    /// </summary>
    /// <param name="newData">The ball configuration to apply.</param>
    public void Initialize(BallDataSO newData)
    {
        transform.localScale = Vector3.one; // Reset scale in case it was modified by animations
        _data = newData;

        // Reset runtime tracking variables to prevent state carry-over from previous life
        _currentClickCount = 0;
        _lastClickTime = 0f;

        // Prototype Pattern: Clone the behavior to ensure independent logic state
        if (_data != null && _data.behaviorTemplate != null)
        {
            _runtimeBehavior = _data.behaviorTemplate.Clone();
            _runtimeBehavior.Initialize(this);
        }
        else
        {
            _runtimeBehavior = null;
        }
        _isProcessing = false;

        // Apply visual and physical properties defined in the ScriptableObject
        UpdateVisualsAndPhysics();
    }

    #region Ball Interacion (Click & Collision)

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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        _runtimeBehavior?.OnCollisionEnter(this, collision);
    }

    #endregion

    #region Visual and Physics Sync

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

        _renderer.ColorInner = _data.color * 0.7f;
        _renderer.ColorOuter = _data.color;
        _renderer.Radius = _data.radius;

        if (_particlesClick != null)
        {
            ParticleSystem.MainModule clickMain = _particlesClick.main;
            clickMain.startColor = _data.color;
        }

        if (_particlesDuplicate != null)
        {
            ParticleSystem.MainModule dupliMain = _particlesDuplicate.main;
            dupliMain.startColor = _data.color;
        }

        if (_collider == null) _collider = GetComponent<CircleCollider2D>();
        if (_collider != null) _collider.radius = ColliderRadius;

        if (_rb == null) _rb = GetComponent<Rigidbody2D>();
        if (_rb != null) _rb.gravityScale = 0f;
    }

    #endregion

    #region Click and Duplication Logic

    /// <summary>
    /// Handles the technical process of duplication.
    /// Called by behaviors to perform the standard spawn.
    /// </summary>
    public void PerformDefaultDuplicate()
    {
        if (IsProcessing) return;
        DOTween.Kill(this);
        _particlesDuplicate.Play();
        transform.localScale = Vector3.one;
        this.transform.DOScale(Vector3.zero, 0.2f) // Go to 0
                      .From().SetEase(Ease.OutBack).SetTarget(this); // Go to one

        BallEntity newBall = BallPoolManager.Instance.SpawnBall(_data, transform.position);

        newBall.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack); // Go to one

        // Ejection force using current mass
        Vector2 ejectionDirection = Random.insideUnitCircle.normalized;
        newBall.Rb.AddForce(ejectionDirection * 5f, ForceMode2D.Impulse);
    }

    /// <summary>
    /// Handles the technical process of  click.
    /// Called by behaviors to perform the standard click.
    /// </summary>
    public void PerformDefaultClick()
    {
        if (IsProcessing) return;
        DOTween.Kill(this);
        transform.localScale = Vector3.one;
        // Simple bounce click animation
        this.transform.DOScale(Vector3.one * 0.90f, _data.clickCooldown) // Go to 0.90
                      .From().SetEase(Ease.InOutElastic).SetTarget(this); // Go to one
        // Spawn particles
        _particlesClick.Play();
    }

    #endregion

    #region Drag

    /// <summary>
    /// Prepares the ball for dynamic physical dragging.
    /// </summary>
    public bool OnDragStart()
    {
        //ElectricManager.Instance.MarkDirty();
        //Debug.Log($"Trying to drag ball {_data.id} (IsProcessing: {_isProcessing})");
        if (_isProcessing)
        {
            return false;
        }
        _isBeingDragged = true;
        // We keep the bodyType as Dynamic to preserve physical collisions
        _rb.linearVelocity = Vector2.zero;

        if (_runtimeBehavior != null)
        {
            // Tell tho the behavior that we're started the drag !
            _runtimeBehavior.OnDragStart(this);
        }
        return true;
    }

    /// <summary>
    /// Updates the velocity to move towards the mouse position.
    /// Preserves physical interactions with other objects.
    /// </summary>
    public void OnDragUpdate(Vector2 position)
    {
        // Calculate the direction and distance to the mouse
        Vector2 direction = position - _rb.position;

        // Apply velocity proportional to the distance. 
        // The further the mouse, the faster it tries to catch up.
        _rb.linearVelocity = direction * _dragForceMultiplier;
    }

    /// <summary>
    /// Resets the drag state and stops momentum.
    /// </summary>
    public void OnDragEnd()
    {
        _isBeingDragged = false;
        _rb.linearVelocity = Vector2.zero; // avoid the ball from flying away on release

        if (_runtimeBehavior != null)
        {
            // Tell tho the behavior that we're stopped the drag !
            _runtimeBehavior.OnDragEnd(this);
        }
    }

    /// <summary>
    /// Handles rotation input during a drag.
    /// Balls are symmetrical circles, so physical rotation is ignored by default.
    /// </summary>
    public void OnDragRotate(float scrollDelta)
    {
        // Do nothing, or pass to _runtimeBehavior if a specific ball needs visual rotation.
    }

    #endregion

    private void OnEnable()
    {
        // Simple relay to the behavior
        _runtimeBehavior?.OnEnableBehavior(this);
    }

    private void OnDisable()
    {
        // Simple relay to the behavior
        _runtimeBehavior?.OnDisableBehavior(this);
    }

    private void OnDrawGizmos()
    {
        // The entity doesn't care WHAT is drawn, it just allows the behavior to draw.
        _runtimeBehavior?.OnDrawGizmosBehavior(this);
    }
}