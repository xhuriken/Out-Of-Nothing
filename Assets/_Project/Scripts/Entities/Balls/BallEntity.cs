using DG.Tweening;
using Shapes;
using Sirenix.OdinInspector;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(BallJellyBounce))]
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
    [SerializeField] private bool _isDuplicating;

    [Header("Settings")]
    [SerializeField] private float _dragForceMultiplier = 15f;
    [SerializeField] private float _maxDragSpeed = 30f;

    [Header("Duplication Feel Settings (Mitosis)")]
    [SerializeField, FoldoutGroup("Duplication Settings")]
    [Tooltip("The duration of the initial preparation/elongation phase.")]
    private float _prepDuration = 0.35f;

    [SerializeField, FoldoutGroup("Duplication Settings")]
    [Tooltip("The duration of the actual split/separation phase.")]
    private float _splitDuration = 0.45f;


    [SerializeField, FoldoutGroup("Duplication Settings")]
    [Tooltip("The intensity of the preparatory vibration/shake effect before splitting.")]
    private float _vibrationIntensity = 0.08f;

    [SerializeField, FoldoutGroup("Duplication Settings")]
    [Tooltip("The maximum visual stretch applied to the balls along the split axis.")]
    private float _maxStretch = 1.4f;

    [SerializeField, FoldoutGroup("Duplication Settings")]
    [Tooltip("The minimum visual squash applied to the balls perpendicular to the split axis.")]
    private float _minSquash = 0.6f;

    [SerializeField, FoldoutGroup("Duplication Settings")]
    [Tooltip("The parting impulse force applied immediately after the split concludes to restore natural momentum.")]
    private float _partingImpulse = 4f;


    [SerializeField, FoldoutGroup("Duplication Settings")]
    [Tooltip("The ease function used for the scale recovery.")]
    private Ease _scaleEase = Ease.OutElastic;

    private float _lastClickTime;
    private int _currentClickCount;
    private Rigidbody2D _rb;
    private CircleCollider2D _collider;
    private BallBehavior _behavior; // Found on the prefab
    private bool _isBeingDragged;
    private float _originalMass;

    public BallDataSO Data => _data;
    public Rigidbody2D Rb => _rb;
    public Disc Renderer => _renderer;
    public bool IsBeingDragged => _isBeingDragged;
    public BallBehavior Behavior => _behavior; // Exposed for EnergyManager

    private BallPhysicsPassport _passport;
    public BallPhysicsPassport Passport => _passport;

    public float ColliderRadius => _data.radius + (_renderer.Thickness / 2);

    public bool IsProcessing
    {
        get => _isProcessing;
        set => _isProcessing = value;
    }

    public bool IsDuplicating
    {
        get => _isDuplicating;
        set => _isDuplicating = value;
    }

    public int CurrentClickCount
    {
        get => _currentClickCount;
        set => _currentClickCount = value;
    }

    /// <summary>
    /// Gets or sets whether the ball is currently attracted by a black hole.
    /// </summary>
    public bool IsAttracted { get; set; }

    public CircleCollider2D Collider => _collider;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _collider = GetComponent<CircleCollider2D>();
        _passport = GetComponent<BallPhysicsPassport>();
        // Find the behavior component added to the prefab
        _behavior = GetComponent<BallBehavior>();

        if (_rb != null)
        {
            _originalMass = _rb.mass;
        }

        if (_data != null) Initialize(_data);
    }

    private void FixedUpdate()
    {
        if (_isProcessing) return;

        // Skip behavior execution if being dragged, EXCEPT for Yellow Balls 
        // which need to rebuild topology and pump energy while moving.
        if (_isBeingDragged && !(_behavior is YellowBallBehavior)) return;

        _behavior?.ExecuteFixedUpdate(this, Time.fixedDeltaTime);
    }

    public void Initialize(BallDataSO newData)
    {
        transform.localScale = Vector3.one;
        transform.rotation = Quaternion.identity;
        _data = newData;
        _currentClickCount = 0;
        _lastClickTime = 0f;
        _isProcessing = false;
        _isDuplicating = false;
        IsAttracted = false;

        if (_collider != null)
        {
            _collider.enabled = true;
        }

        if (_rb != null)
        {
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
            _rb.mass = _originalMass;
        }

        UpdateVisualsAndPhysics();
        _behavior?.Initialize(this);

        // Ignore collision with all active bumpers
        foreach (var bumper in BumperMachine.ActiveBumpers)
        {
            if (bumper != null)
            {
                Collider2D bumperCollider = bumper.GetComponent<Collider2D>();
                if (bumperCollider != null && _collider != null)
                {
                    Physics2D.IgnoreCollision(_collider, bumperCollider, true);
                }
            }
        }
    }

    #region Interaction Relay

    public void ReceiveClick()
    {
        if (_isProcessing || _isDuplicating) return;
        if (Time.time - _lastClickTime < _data.clickCooldown) return;

        _lastClickTime = Time.time;
        _currentClickCount++;
        _behavior?.OnClick(this);

        if (_currentClickCount >= _data.clicksRequiredForDuplication)
        {
            _currentClickCount = 0;
            Duplicate();
        }
    }

    private void Duplicate()
    {
        _behavior?.OnDuplicate(this);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        _behavior?.OnBallCollisionEnter(this, collision);
    }

    #endregion

    #region Drag Logic

    public bool OnDragStart()
    {
        if (_isDuplicating) return false;

        if (_isProcessing)
        {
            if (CraftingManager.Instance == null || !CraftingManager.Instance.IsCrafting || !CraftingManager.Instance.IsBallSelected(this))
            {
                return false;
            }
        }

        _isBeingDragged = true;
        //_rb.linearVelocity = Vector2.zero;
        _behavior?.OnDragStart(this);
        return true;
    }

    public void OnDragUpdate(Vector2 position)
    {
        Vector2 direction = position - _rb.position;
        Vector2 desiredVelocity = direction * _dragForceMultiplier;
        Vector2 clampedVelocity = Vector2.ClampMagnitude(desiredVelocity, _maxDragSpeed);

        // Use the passport instead of direct RB access
        _passport.RequestVelocity(clampedVelocity, PhysicsPriority.Drag, VelocityMode.Override);
    }

    public void OnDragEnd()
    {
        _isBeingDragged = false;
        //_rb.linearVelocity = Vector2.zero;
        _behavior?.OnDragEnd(this);
    }

    public void OnDragRotate(float scrollDelta) { }

    #endregion

    #region Visuals and Gizmos

    private void UpdateVisualsAndPhysics()
    {
        if (_data == null) return;

        _renderer.ColorInner = _data.color * 0.7f;
        _renderer.ColorOuter = _data.color;
        _renderer.Radius = _data.radius;

        if (_collider != null) _collider.radius = ColliderRadius;
        if (_rb != null) _rb.gravityScale = 0f;
    }

    private void OnEnable()
    {
        _behavior?.OnEnableBehavior(this);
    }

    private void OnDisable()
    {
        // Debug.Log($"[DIAGNOSTIC] Ball {gameObject.name} (ID: {(_data != null ? _data.id : "null")}) was deactivated. Stack Trace:\n{System.Environment.StackTrace}");

        DOTween.Kill(this);
        DOTween.Kill(transform);
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;
        if (_rb != null)
        {
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
            _rb.mass = _originalMass;
        }
        _isProcessing = false;
        _isDuplicating = false;
        _isBeingDragged = false;
        IsAttracted = false;

        _behavior?.OnDisableBehavior(this);
    }

    private void OnDrawGizmos()
    {
        _behavior?.OnDrawGizmosBehavior(this);
    }

    private void OnValidate()
    {
        UpdateVisualsAndPhysics();
    }

    #endregion

    #region Physics Modifiers

    /// <summary>
    /// Temporarily makes the ball heavier for a duration, so it pushes other balls easily.
    /// Returns the mass multiplier applied, so callers can scale their impulses accordingly.
    /// </summary>
    public float SetTemporaryHeavyMass(float duration, float massMultiplier = 50f)
    {
        if (_rb == null) return 1f;

        // Use the cached original mass to prevent compounding mass issues when called multiple times
        _rb.mass = _originalMass * massMultiplier;

        DOVirtual.DelayedCall(duration, () =>
        {
            if (this != null && _rb != null)
            {
                _rb.mass = _originalMass;
            }
        });

        return massMultiplier;
    }

    #endregion

    #region Default Performers

    /// <summary>
    /// Performs a high-fidelity mitosis-style duplication animation using DOTween.
    /// During the division, the parent and child balls ignore collision between each other
    /// but continue to interact physically with other dynamic objects.
    /// </summary>
    public void PerformDefaultDuplicate()
    {
        PerformDuplicate(_data);
    }

    /// <summary>
    /// Performs a high-fidelity mitosis-style duplication animation using DOTween, spawning the specified child ball.
    /// </summary>
    public void PerformDuplicate(BallDataSO childData)
    {
        if (_isProcessing || _isDuplicating)
        {
            return;
        }

        // Kill active tweens on this object to prevent overlap conflicts
        DOTween.Kill(this);
        transform.localScale = Vector3.one;

        // Choose a random split direction
        Vector2 splitDirection = Random.insideUnitCircle.normalized;
        float angle = Mathf.Atan2(splitDirection.y, splitDirection.x) * Mathf.Rad2Deg;

        // Lock the parent's physics state and make it Kinematic so we can control its path
        _isProcessing = true;
        _isDuplicating = true;
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.linearVelocity = Vector2.zero;
        _rb.angularVelocity = 0f;

        // Visual orientation: Align local X axis with split direction
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // Define target squashed/stretched scales
        Vector3 stretchedScale = new Vector3(_maxStretch, _minSquash, 1f);

        // Sequence 1: Preparation (Elongate and shake/vibrate like a high-tension cell)
        Sequence prepSeq = DOTween.Sequence();
        prepSeq.SetTarget(this);

        prepSeq.Append(transform.DOScale(stretchedScale, _prepDuration).SetEase(Ease.InQuad));
        prepSeq.Join(transform.DOShakePosition(_prepDuration, _vibrationIntensity, 30, 90f, false, false));

        prepSeq.OnComplete(() =>
        {
            // Spawn the child ball from the pool
            BallEntity newBall = BallPoolManager.Instance.SpawnBall(childData, transform.position);
            if (newBall == null)
            {
                // Fallback cleanup if spawn failed
                ResetAfterDuplicate();
                return;
            }

            // Play particles at the split point for juicy feedback
            if (_particlesDuplicate != null)
            {
                _particlesDuplicate.Play();
            }

            // Set parent back to dynamic & active physics immediately
            _isProcessing = false;
            _rb.bodyType = RigidbodyType2D.Dynamic;
            transform.rotation = Quaternion.identity;

            // Set up child ball's initial duplicate state (Dynamic immediately, same scale as parent)
            newBall.IsProcessing = false;
            newBall.IsDuplicating = true;
            newBall.Rb.bodyType = RigidbodyType2D.Dynamic;
            newBall.transform.rotation = Quaternion.identity;
            newBall.transform.localScale = transform.localScale;

            // Trigger child's particles too for symmetry
            if (newBall._particlesDuplicate != null)
            {
                newBall._particlesDuplicate.Play();
            }

            // IGNORE collision only between specifically these two balls during the split flyout!
            Physics2D.IgnoreCollision(_collider, newBall.Collider, true);

            // Temporarily increase mass so they push other balls away easily during the split animation
            float parentMassMult = this.SetTemporaryHeavyMass(_splitDuration, 50f);
            float childMassMult = newBall.SetTemporaryHeavyMass(_splitDuration, 50f);

            // Apply a single powerful parting impulse immediately to fly them apart naturally!
            _passport.ApplyImpulse(splitDirection * (_partingImpulse * parentMassMult), PhysicsPriority.Behavior);
            newBall.Passport.ApplyImpulse(-splitDirection * (_partingImpulse * childMassMult), PhysicsPriority.Behavior);

            // Visual separation wobble: Tween their scale back to normal (1,1,1) with an elastic wobble
            transform.DOScale(Vector3.one, _splitDuration).SetEase(_scaleEase);
            newBall.transform.DOScale(Vector3.one, _splitDuration).SetEase(_scaleEase);

            // Re-enable collisions between them after the split duration has elapsed
            DOVirtual.DelayedCall(_splitDuration, () =>
            {
                if (this != null && newBall != null && _collider != null && newBall.Collider != null)
                {
                    Physics2D.IgnoreCollision(_collider, newBall.Collider, false);
                }

                if (this != null) _isDuplicating = false;
                if (newBall != null) newBall.IsDuplicating = false;
            });
        });
    }

    /// <summary>
    /// Safe fallback cleanup method to restore the ball state if duplication gets interrupted.
    /// </summary>
    private void ResetAfterDuplicate()
    {
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;
        if (_rb != null)
        {
            _rb.bodyType = RigidbodyType2D.Dynamic;
        }
        _isProcessing = false;
        _isDuplicating = false;
    }

    public void PerformDefaultClick()
    {
        if (_isProcessing || _isDuplicating) return;
        DOTween.Kill(this);
        transform.localScale = Vector3.one;
        this.transform.DOScale(Vector3.one * 0.90f, _data.clickCooldown)
                      .From().SetEase(Ease.InOutElastic).SetTarget(this);
        _particlesClick.Play();
    }

    #endregion
}