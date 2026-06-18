using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;
using Shapes;

/// <summary>
/// A draggable, clickable machine that functions as a purchase shop.
/// Expels purchased items physically using forces and scale transitions.
/// Coordinates purchase slots relative to its local coordinates so they follow dragging.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class Shop : MonoBehaviour, IDraggable
{
    [Header("References")]
    [SerializeField]
    [Tooltip("Container holding the individual BallShop child items.")]
    private GameObject _ballShopContainer;

    [SerializeField]
    [Tooltip("Shapes Disc component representing the shop outer ring (Main Disc).")]
    private Disc _discComponent;

    [SerializeField]
    [Tooltip("Shapes Disc component representing the background disc.")]
    private Disc _backgroundDisc;

    [SerializeField]
    [Tooltip("Shader visual sprite renderer.")]
    private SpriteRenderer _shaderRenderer;

    [SerializeField]
    [Tooltip("Reflect / repulsion zone visual sprite renderer.")]
    private SpriteRenderer _reflectRenderer;

    [Header("Shop Dimension Settings")]
    [SerializeField]
    [Tooltip("The event horizon radius of the shop (gRadius). Used for visual bounds and repulsion offset.")]
    private float _gRadius = 1f;

    [Header("Visual Offsets")]
    [SerializeField]
    [Tooltip("Offset added to _gRadius for the Main Disc radius.")]
    private float _mainDiscOffset = -0.54f;

    [SerializeField]
    [Tooltip("Offset added to _gRadius for the Background Disc radius.")]
    private float _backgroundOffset = 0.09f;

    [SerializeField]
    [Tooltip("Offset added to _gRadius for the BlackHoleShader _BlackHoleRadius.")]
    private float _shaderOffset = -0.1f;

    [SerializeField]
    [Tooltip("Offset added to _gRadius for the Reflect shader _BlackHoleRadius.")]
    private float _reflectShaderOffset = 2.5f;

    [Header("Expel Animation Settings")]
    [SerializeField] private float _spawnDelay = 0.03f;
    [SerializeField] private float _hideDelay = 0.03f;
    [SerializeField] private float _postHideDelay = 0.05f;
    [SerializeField] private float _moveDuration = 0.2f;
    [SerializeField] [Tooltip("Circular distance multiplier relative to GRadius.")] private float _radius = 1.5f;
    [SerializeField] private float _shopDetectionRadius = 0.8f;
    [SerializeField] private float _expelForce = 6f;

    [Header("Dragging Settings")]
    [SerializeField] private float _dragForceMultiplier = 15f;
    [SerializeField] private float _maxDragSpeed = 30f;

    private bool _isShopActive = false;
    private bool _isOpening = false;
    private bool _isClosing = false;
    private bool _isBeingDragged = false;
    private Rigidbody2D _rb;
    private MaterialPropertyBlock _propBlock;
    private Vector3 _lastPosition;

    /// <summary>
    /// Reference to the active spawn coroutine.
    /// </summary>
    private Coroutine _spawnCoroutine;

    /// <summary>
    /// Reference to the active hide coroutine.
    /// </summary>
    private Coroutine _hideCoroutine;

    /// <summary>
    /// Reference to the active purchase and ejection coroutine.
    /// </summary>
    private Coroutine _purchaseCoroutine;

    private readonly List<BallShop> _ballShops = new List<BallShop>();

    /// <summary>
    /// Gets the event horizon radius of the shop.
    /// </summary>
    public float GRadius => _gRadius;

    /// <summary>
    /// Gets whether the shop user interface is currently open.
    /// </summary>
    public bool IsShopActive => _isShopActive;

    /// <summary>
    /// Gets whether the shop is currently in a transition animation.
    /// </summary>
    public bool IsAnimating => _isOpening || _isClosing;

    /// <summary>
    /// Gets whether this object is currently being dragged.
    /// </summary>
    public bool IsBeingDragged => _isBeingDragged;

    /// <summary>
    /// Caches references to critical components.
    /// </summary>
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _propBlock = new MaterialPropertyBlock();
        _lastPosition = transform.position;
        UpdateVisualsAndCollider();
    }

    /// <summary>
    /// Initializes slot children and updates visuals to align with GRadius.
    /// </summary>
    private void Start()
    {
        if (Application.isPlaying)
        {
            if (_ballShopContainer != null)
            {
                foreach (Transform child in _ballShopContainer.transform)
                {
                    BallShop bs = child.GetComponent<BallShop>();
                    if (bs != null)
                    {
                        _ballShops.Add(bs);
                        bs.Initialize(this);
                    }
                }
            }
            else
            {
                Debug.LogWarning("[Shop] BallShopContainer is not assigned!");
            }
        }

        _lastPosition = transform.position;
        UpdateVisualsAndCollider();
    }

    /// <summary>
    /// Updates shader centers and collider bounds. Click toggles are routed centrally via GameInputManager.
    /// </summary>
    private void Update()
    {
        // Always update shader centers and collider bounds in Update
        UpdateVisualsAndCollider();
    }

    /// <summary>
    /// Toggles the shop active state (opens or closes it). Exposes interface routing for GameInputManager.
    /// </summary>
    public void ToggleShopActiveState()
    {
        if (IsAnimating || _isBeingDragged) return;

        if (!_isShopActive)
        {
            ActivateShop();
        }
        else
        {
            HideShop();
        }
    }

    /// <summary>
    /// Synchronizes shader reflect center whenever the shop changes position.
    /// </summary>
    private void LateUpdate()
    {
        if (transform.position != _lastPosition)
        {
            _lastPosition = transform.position;
            UpdateShaderReflectCenter();
        }
    }

    /// <summary>
    /// Updates the reflect shader's center parameter in the material property block.
    /// </summary>
    private void UpdateShaderReflectCenter()
    {
        if (_reflectRenderer != null)
        {
            if (_propBlock == null)
            {
                _propBlock = new MaterialPropertyBlock();
            }
            _propBlock.Clear();
            _reflectRenderer.GetPropertyBlock(_propBlock);
            _propBlock.SetVector("_ReflectCenter", transform.position);
            _reflectRenderer.SetPropertyBlock(_propBlock);
        }
    }

    /// <summary>
    /// Syncs GRadius to the Shapes Discs, SpriteRenderers, and CircleCollider2D inside Unity editor and at runtime.
    /// </summary>
    private void UpdateVisualsAndCollider()
    {
        // 1. Main disc radius
        if (_discComponent != null)
        {
            _discComponent.Radius = Mathf.Max(0.01f, _gRadius + _mainDiscOffset);
        }

        // 2. Background disc radius
        if (_backgroundDisc != null)
        {
            _backgroundDisc.Radius = Mathf.Max(0.01f, _gRadius + _backgroundOffset);
        }

        // 3. Collider radius
        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col != null)
        {
            col.radius = _gRadius;
        }

        // 4. Shaders Material properties
        if (_propBlock == null)
        {
            _propBlock = new MaterialPropertyBlock();
        }

        if (_shaderRenderer != null)
        {
            _propBlock.Clear();
            _shaderRenderer.GetPropertyBlock(_propBlock);
            _propBlock.SetFloat("_BlackHoleRadius", Mathf.Max(0.01f, _gRadius + _shaderOffset));
            _shaderRenderer.SetPropertyBlock(_propBlock);
        }

        if (_reflectRenderer != null)
        {
            _propBlock.Clear();
            _reflectRenderer.GetPropertyBlock(_propBlock);
            _propBlock.SetFloat("_BlackHoleRadius", Mathf.Max(0.01f, _gRadius + _reflectShaderOffset));
            _propBlock.SetVector("_ReflectCenter", transform.position);
            _reflectRenderer.SetPropertyBlock(_propBlock);
        }
    }

    /// <summary>
    /// Editor hook to immediately align collider and visuals when altering GRadius.
    /// </summary>
    private void OnValidate()
    {
        UpdateVisualsAndCollider();
    }

    /// <summary>
    /// Performs purchase transaction, deducting points and starting the ejection flow.
    /// Stops active spawn or hide animations to avoid conflicts during the purchase transition.
    /// </summary>
    public void OnBallSelected(BallShop selectedBall)
    {
        if (_isClosing || selectedBall == null || selectedBall.identity == null) return;

        double price = selectedBall.identity.Price;
        if (IncrementManager.Instance != null && IncrementManager.Instance.Points >= price)
        {
            IncrementManager.Instance.RemovePoints(price);
            
            // Clear hover state in GameInputManager only when purchase is successful
            if (GameInputManager.Instance != null)
            {
                GameInputManager.Instance.ClearHoveredSlot();
            }

            // Stop any active spawn or hide routines to avoid tweening/state conflicts
            if (_spawnCoroutine != null)
            {
                StopCoroutine(_spawnCoroutine);
                _spawnCoroutine = null;
            }
            _isOpening = false;

            if (_hideCoroutine != null)
            {
                StopCoroutine(_hideCoroutine);
                _hideCoroutine = null;
            }
            _isClosing = false;

            _purchaseCoroutine = StartCoroutine(HideBallShopsAndPurchaseRoutine(selectedBall));
        }
        else
        {
            selectedBall.FlashPriceTextRed();
        }
    }

    #region IDraggable Implementation

    /// <summary>
    /// Initiates dragging by turning on dynamic physics constraints.
    /// </summary>
    public bool OnDragStart()
    {
        _isBeingDragged = true;
        _rb.bodyType = RigidbodyType2D.Dynamic;
        _rb.linearVelocity = Vector2.zero;
        return true;
    }

    /// <summary>
    /// Translates the drag position to physical velocities.
    /// </summary>
    public void OnDragUpdate(Vector2 position)
    {
        Vector2 direction = position - _rb.position;
        Vector2 desiredVelocity = direction * _dragForceMultiplier;
        Vector2 clampedVelocity = Vector2.ClampMagnitude(desiredVelocity, _maxDragSpeed);
        _rb.linearVelocity = clampedVelocity;
    }

    /// <summary>
    /// Ends dragging and returns the rigidbody to Kinematic.
    /// </summary>
    public void OnDragEnd()
    {
        _isBeingDragged = false;
        _rb.linearVelocity = Vector2.zero;
        _rb.bodyType = RigidbodyType2D.Kinematic;
    }

    /// <summary>
    /// Rotation is disabled on the Shop.
    /// </summary>
    public void OnDragRotate(float scrollDelta)
    {
        // Rotation is disabled for the Shop.
    }

    #endregion

    #region Input Overlaps

    /// <summary>
    /// Checks if the visual cursor is hovering over the shop detection radius (including custom cursor action tolerance).
    /// </summary>
    public bool IsMouseOverShop()
    {
        Vector2 cursorPos = Vector2.zero;
        float actionRadius = 0.5f;

        if (GameInputManager.Instance != null)
        {
            cursorPos = GameInputManager.Instance.GetCursorWorldPosition();
            actionRadius = GameInputManager.Instance.CursorActionRadius;
        }
        else if (GameCursor.Instance != null)
        {
            cursorPos = GameCursor.Instance.transform.position;
        }
        else
        {
            if (Camera.main == null || Mouse.current == null) return false;
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            if (float.IsNaN(mouseScreenPos.x) || float.IsNaN(mouseScreenPos.y)) return false;
            cursorPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        }

        float distance = Vector2.Distance(cursorPos, transform.position);
        return distance <= (_shopDetectionRadius + actionRadius);
    }

    #endregion

    #region Shop GUI Activation

    /// <summary>
    /// Activates the shop, triggering the spawner animation for purchase items.
    /// </summary>
    private void ActivateShop()
    {
        if (IsAnimating) return;
        _isShopActive = true;

        if (_hideCoroutine != null)
        {
            StopCoroutine(_hideCoroutine);
            _hideCoroutine = null;
        }
        _isClosing = false;

        _spawnCoroutine = StartCoroutine(SpawnBallShopsRoutine());
    }

    /// <summary>
    /// Closes the shop interface, hiding all items.
    /// </summary>
    private void HideShop()
    {
        if (IsAnimating) return;

        if (_spawnCoroutine != null)
        {
            StopCoroutine(_spawnCoroutine);
            _spawnCoroutine = null;
        }
        _isOpening = false;

        _hideCoroutine = StartCoroutine(HideAllBallsRoutine());
    }

    /// <summary>
    /// Coroutine animating slots sliding out in a local circular pattern relative to the Shop.
    /// </summary>
    private IEnumerator SpawnBallShopsRoutine()
    {
        _isOpening = true;
        int count = _ballShops.Count;
        float actualRadius = _gRadius * _radius;
        for (int i = 0; i < count; i++)
        {
            BallShop ball = _ballShops[i];
            float angle = -(360f / count) * i + 90f;
            float rad = angle * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
            Vector3 localTargetPos = direction * actualRadius;
            ball.SpawnWithMoveAndScale(localTargetPos, direction, _moveDuration);
            yield return new WaitForSeconds(_spawnDelay);
        }
        
        // Wait for the final ball's deploy tween to finish
        yield return new WaitForSeconds(_moveDuration - _spawnDelay);
        _isOpening = false;
        _spawnCoroutine = null;
    }

    /// <summary>
    /// Coroutine animating slots sliding back into the shop local center.
    /// </summary>
    private IEnumerator HideAllBallsRoutine()
    {
        _isClosing = true;
        for (int i = 0; i < _ballShops.Count; i++)
        {
            BallShop ball = _ballShops[i];
            ball.HideWithMoveAndScale(Vector3.zero, _moveDuration);
            yield return new WaitForSeconds(_hideDelay);
        }
        
        // Wait for the final ball's retract tween to finish
        yield return new WaitForSeconds(_moveDuration - _hideDelay);
        _isShopActive = false;
        _isClosing = false;
        _hideCoroutine = null;
    }

    /// <summary>
    /// Closes interface and ejects the purchased item outward using physics.
    /// </summary>
    private IEnumerator HideBallShopsAndPurchaseRoutine(BallShop selectedBall)
    {
        _isClosing = true;
        for (int i = 0; i < _ballShops.Count; i++)
        {
            BallShop ballSlot = _ballShops[i];
            ballSlot.HideWithMoveAndScale(Vector3.zero, _moveDuration);
            yield return new WaitForSeconds(_hideDelay);
        }
        
        // Wait for the retract tween to finish before proceeding to spawn
        yield return new WaitForSeconds(_moveDuration - _hideDelay);
        yield return new WaitForSeconds(_postHideDelay);

        // Convert the local slot target direction to world space
        Vector3 direction = transform.TransformDirection(selectedBall.localSpawnTargetPosition.normalized);

        BallDataSO ballData = selectedBall.identity.BallData;
        if (ballData != null && BallPoolManager.Instance != null)
        {
            // Spawn the ball at the center of the shop
            Vector3 spawnPosition = transform.position;

            BallEntity newBall = BallPoolManager.Instance.SpawnBall(ballData, spawnPosition);
            if (newBall != null)
            {
                newBall.transform.localScale = Vector3.zero;
                newBall.IsProcessing = true;

                // Temporarily ignore collision between the spawned ball and the Shop collider to prevent physical overlap issues
                Collider2D shopCollider = GetComponent<Collider2D>();
                if (shopCollider != null && newBall.Collider != null)
                {
                    Physics2D.IgnoreCollision(shopCollider, newBall.Collider, true);
                    
                    // Re-enable collisions after a short delay (time to exit)
                    DOVirtual.DelayedCall(0.5f, () =>
                    {
                        if (this != null && newBall != null && shopCollider != null && newBall.Collider != null)
                        {
                            Physics2D.IgnoreCollision(shopCollider, newBall.Collider, false);
                        }
                    });
                }

                // Make the ball temporarily heavy to push obstacles easily, caching the multiplier
                float massMultiplier = newBall.SetTemporaryHeavyMass(2f, 50f);

                // Apply physics-based push force scaled by the mass multiplier (halved expulsion force)
                if (newBall.Rb != null)
                {
                    newBall.Rb.bodyType = RigidbodyType2D.Dynamic;
                    newBall.Rb.linearVelocity = Vector2.zero; // Clear residual physical velocity
                    newBall.Rb.angularVelocity = 0f;          // Clear rotation velocity
                    newBall.Rb.AddForce(direction * ((_expelForce * 0.5f) * massMultiplier), ForceMode2D.Impulse);
                }

                // Smoothly animate scale from 0 to normal size
                newBall.transform.DOScale(Vector3.one, 0.5f)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() =>
                    {
                        if (newBall != null)
                        {
                            newBall.IsProcessing = false;
                        }
                    });
            }
        }
        _isShopActive = false;
        _isClosing = false;
        _purchaseCoroutine = null;
    }

    #endregion
}
