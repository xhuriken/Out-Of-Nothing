using DG.Tweening;
using Shapes;
using Sirenix.OdinInspector;
using UnityEngine;

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
    [SerializeField] private float _dragForceMultiplier = 15f;
    [SerializeField] private float _maxDragSpeed = 30f;

    private float _lastClickTime;
    private int _currentClickCount;
    private Rigidbody2D _rb;
    private CircleCollider2D _collider;
    private BallBehavior _behavior; // Found on the prefab
    private bool _isBeingDragged;

    public BallDataSO Data => _data;
    protected Rigidbody2D Rb => _rb;
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

    public CircleCollider2D Collider => _collider;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _collider = GetComponent<CircleCollider2D>();
        _passport = GetComponent<BallPhysicsPassport>();
        // Find the behavior component added to the prefab
        _behavior = GetComponent<BallBehavior>();

        if (_data != null) Initialize(_data);
    }

    private void FixedUpdate()
    {
        if (_isBeingDragged || _isProcessing) return;
        _behavior?.ExecuteFixedUpdate(this, Time.fixedDeltaTime);
    }

    public void Initialize(BallDataSO newData)
    {
        transform.localScale = Vector3.one;
        _data = newData;
        _currentClickCount = 0;
        _lastClickTime = 0f;
        _isProcessing = false;

        UpdateVisualsAndPhysics();
        _behavior?.Initialize(this);
    }

    #region Interaction Relay

    public void ReceiveClick()
    {
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
        if (_isProcessing) return false;
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

    private void OnEnable() => _behavior?.OnEnableBehavior(this);
    private void OnDisable() => _behavior?.OnDisableBehavior(this);
    private void OnDrawGizmos() => _behavior?.OnDrawGizmosBehavior(this);
    private void OnValidate() => UpdateVisualsAndPhysics();

    #endregion

    #region Default Performers

    public void PerformDefaultDuplicate()
    {
        if (_isProcessing) return;
        DOTween.Kill(this);
        _particlesDuplicate.Play();
        BallEntity newBall = BallPoolManager.Instance.SpawnBall(_data, transform.position);

        Vector2 ejectionDirection = Random.insideUnitCircle.normalized;
        newBall.Rb.AddForce(ejectionDirection * 5f, ForceMode2D.Impulse);
    }

    public void PerformDefaultClick()
    {
        if (_isProcessing) return;
        DOTween.Kill(this);
        transform.localScale = Vector3.one;
        this.transform.DOScale(Vector3.one * 0.90f, _data.clickCooldown)
                      .From().SetEase(Ease.InOutElastic).SetTarget(this);
        _particlesClick.Play();
    }

    #endregion
}