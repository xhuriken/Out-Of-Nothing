using DG.Tweening;
using Shapes;
using UnityEngine;
using UnityEngine.InputSystem;

public enum CursorMode { Normal, Craft }

/// <summary>
/// Singleton class that continuously follows the game mouse cursor with customizable smoothing.
/// Handles DOTween animations for clicking and dragging, manipulating Shapes.Disc components.
/// Supports swapping between Normal (Discs) and Craft (Hammer) modes.
/// </summary>
public class GameCursor : MonoBehaviour
{
    public static GameCursor Instance { get; private set; }

    [Header("Visual Roots")]
    [SerializeField] private GameObject _normalRoot;
    [SerializeField] private GameObject _craftRoot;

    [Header("Visuals (Shapes)")]
    [SerializeField] private Disc _largeDisc;
    [SerializeField] private Disc _smallDisc;

    [Header("Tracking Settings")]
    [Tooltip("How fast the cursor catches up to the mouse position (lower is faster).")]
    [SerializeField] private float _smoothTime = 0.05f;

    [Tooltip("Maximum speed of the cursor.")]
    [SerializeField] private float _maxSpeed = Mathf.Infinity;

    [Header("Animation Settings")]
    [SerializeField] private float _clickPunchScale = -0.3f;
    [SerializeField] private float _clickDuration = 0.15f;
    [SerializeField] private float _dragThicknessMultiplier = 1.5f;
    [SerializeField] private float _dragDuration = 0.2f;

    [Header("Mode Switch")]
    [SerializeField] private float _modeSwitchDuration = 0.3f;

    private Camera _mainCamera;
    private Vector3 _velocity = Vector3.zero;


    private float _largeDiscInitialRadius;
    private float _largeDiscInitialThickness;
    private float _smallDiscInitialRadius;
    private float _smallDiscInitialThickness;

    private CursorMode _currentMode = CursorMode.Normal;
    private Sequence _clickSequence;
    private Sequence _dragSequence;
    private Sequence _modeSwitchSequence;
    private Tween _rotationTween;

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

        // Ensure DOTween starts cleanly

        DOTween.Init();
    }

    private void Start()
    {
        _mainCamera = Camera.main; // TODO: Cache this properly based on project rules


        if (_largeDisc != null)
        {
            _largeDiscInitialRadius = _largeDisc.Radius;
            _largeDiscInitialThickness = _largeDisc.Thickness;
        }

        if (_smallDisc != null)
        {
            _smallDiscInitialRadius = _smallDisc.Radius;
            _smallDiscInitialThickness = _smallDisc.Thickness;
        }

        // Initialize visibility
        if (_normalRoot != null) _normalRoot.transform.localScale = Vector3.one;
        if (_craftRoot != null) _craftRoot.transform.localScale = Vector3.zero;
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

    /// <summary>
    /// Plays a click animation on the cursor (callable from anywhere).
    /// Safe against spamming.
    /// </summary>
    public void PlayClickAnimation()
    {
        if (_currentMode == CursorMode.Normal)
        {
            _clickSequence?.Kill(true);
            _clickSequence = DOTween.Sequence();

            if (_smallDisc != null)
            {
                float targetRadius = _largeDisc != null ? _largeDisc.Radius * 1.3f : _smallDiscInitialRadius * 2f;
                _clickSequence.Join(DOTween.To(() => _smallDisc.Radius, x => _smallDisc.Radius = x, targetRadius, _clickDuration * 0.5f).SetEase(Ease.OutCubic));
                _clickSequence.Append(DOTween.To(() => _smallDisc.Radius, x => _smallDisc.Radius = x, _smallDiscInitialRadius, _clickDuration).SetEase(Ease.OutElastic));
            }

            if (_largeDisc != null)
            {
                float targetRadius = _largeDiscInitialRadius * 0.8f;
                _clickSequence.Join(DOTween.To(() => _largeDisc.Radius, x => _largeDisc.Radius = x, targetRadius, _clickDuration * 0.5f).SetEase(Ease.OutCubic));
                _clickSequence.Join(DOTween.To(() => _largeDisc.Radius, x => _largeDisc.Radius = x, _largeDiscInitialRadius, _clickDuration).SetEase(Ease.OutElastic));
            }
        }
        else
        {
            // Hammer Click: Elastic rotation punch
            if (_craftRoot != null)
            {
                _craftRoot.transform.DOComplete();
                _craftRoot.transform.DOPunchRotation(new Vector3(0, 0, -30f), 0.4f, 10, 1f).SetEase(Ease.OutElastic);
            }
        }
    }

    /// <summary>
    /// Animates the cursor into a drag state, or reverts it to normal.
    /// Safe against spamming.
    /// </summary>
    /// <param name="isDragging">True if starting drag, false if releasing</param>
    public void SetDragAnimation(bool isDragging)
    {
        _dragSequence?.Kill();
        _dragSequence = DOTween.Sequence();

        if (_currentMode == CursorMode.Normal)
        {
            if (isDragging)
            {
                // Small Disc: Larger, Dotted, Rotating
                if (_smallDisc != null)
                {
                    _smallDisc.Dashed = true;
                    _smallDisc.DashSpacing = 0.5f; // Increase spacing for "less dots"


                    float targetRadius = _largeDiscInitialRadius * 1.4f; // More radius (was 1.1f)
                    _dragSequence.Join(DOTween.To(() => _smallDisc.Radius, x => _smallDisc.Radius = x, targetRadius, _dragDuration).SetEase(Ease.OutBack));


                    _rotationTween?.Kill();
                    _smallDisc.transform.localRotation = Quaternion.identity; // Reset to avoid gimbal/accumulated bugs
                    _rotationTween = _smallDisc.transform.DORotate(new Vector3(0, 0, 360), 2f, RotateMode.LocalAxisAdd)
                        .SetLoops(-1, LoopType.Incremental)
                        .SetEase(Ease.Linear);
                }

                // Large Disc: Smaller, Thicker
                if (_largeDisc != null)
                {
                    _dragSequence.Join(DOTween.To(() => _largeDisc.Radius, x => _largeDisc.Radius = x, _largeDiscInitialRadius * 0.8f, _dragDuration).SetEase(Ease.OutBack));
                    _dragSequence.Join(DOTween.To(() => _largeDisc.Thickness, x => _largeDisc.Thickness = x, _largeDiscInitialThickness * _dragThicknessMultiplier, _dragDuration).SetEase(Ease.OutBack));
                }
            }
            else
            {
                // Reset
                _rotationTween?.Kill();


                if (_smallDisc != null)
                {
                    _dragSequence.Join(DOTween.To(() => _smallDisc.Radius, x => _smallDisc.Radius = x, _smallDiscInitialRadius, _dragDuration).SetEase(Ease.OutQuad));
                    _dragSequence.OnComplete(() =>
                    {
                        _smallDisc.Dashed = false;
                        _smallDisc.transform.localRotation = Quaternion.identity;
                    });
                }

                if (_largeDisc != null)
                {
                    _dragSequence.Join(DOTween.To(() => _largeDisc.Radius, x => _largeDisc.Radius = x, _largeDiscInitialRadius, _dragDuration).SetEase(Ease.OutQuad));
                    _dragSequence.Join(DOTween.To(() => _largeDisc.Thickness, x => _largeDisc.Thickness = x, _largeDiscInitialThickness, _dragDuration).SetEase(Ease.OutQuad));
                }
            }
        }
        else
        {
            // Hammer Drag: Weighty tilt + vibrate
            if (_craftRoot != null)
            {
                if (isDragging)
                {
                    _dragSequence.Join(_craftRoot.transform.DOLocalRotate(new Vector3(0, 0, 20f), 0.2f).SetEase(Ease.OutQuad));
                    _dragSequence.Join(_craftRoot.transform.DOShakePosition(1000f, 0.03f, 20, 90, false, false).SetLoops(-1));
                }
                else
                {
                    _dragSequence.Join(_craftRoot.transform.DOLocalRotate(Vector3.zero, 0.2f).SetEase(Ease.OutBack));
                }
            }
        }
    }

    /// <summary>
    /// Switches between Normal and Craft cursor modes.
    /// </summary>
    public void ToggleMode()
    {
        SetMode(_currentMode == CursorMode.Normal ? CursorMode.Craft : CursorMode.Normal);
    }

    /// <summary>
    /// Sets the cursor mode with a smooth transition animation.
    /// </summary>
    public void SetMode(CursorMode mode)
    {
        if (_currentMode == mode) return;
        _currentMode = mode;


        _modeSwitchSequence?.Kill();
        _modeSwitchSequence = DOTween.Sequence();


        if (_currentMode == CursorMode.Craft)
        {
            if (_normalRoot != null) _modeSwitchSequence.Join(_normalRoot.transform.DOScale(0f, _modeSwitchDuration).SetEase(Ease.InBack));
            if (_craftRoot != null) _modeSwitchSequence.Append(_craftRoot.transform.DOScale(1f, _modeSwitchDuration).SetEase(Ease.OutBack));
        }
        else
        {
            if (_craftRoot != null) _modeSwitchSequence.Join(_craftRoot.transform.DOScale(0f, _modeSwitchDuration).SetEase(Ease.InBack));
            if (_normalRoot != null) _modeSwitchSequence.Append(_normalRoot.transform.DOScale(1f, _modeSwitchDuration).SetEase(Ease.OutBack));
        }
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
