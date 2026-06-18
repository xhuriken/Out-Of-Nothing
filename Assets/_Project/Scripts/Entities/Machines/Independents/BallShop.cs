using UnityEngine;
using TMPro;
using DG.Tweening;
using Shapes;

/// <summary>
/// Represents an individual purchasable ball slot within the Shop interface.
/// Handles visual spawn/hide transitions and price indicator animations.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BallShop : MonoBehaviour
{
    [System.Serializable]
    public class BallIdentityData
    {
        [Tooltip("The price in points required to purchase this ball.")]
        public double Price;

        [Tooltip("The data configuration asset for this ball type.")]
        public BallDataSO BallData;

        /// <summary>
        /// Returns the associated prefab GameObject for the ball.
        /// </summary>
        public GameObject instanceBall => BallData != null && BallData.prefab != null ? BallData.prefab.gameObject : null;
    }

    [Header("Identity")]
    [SerializeField]
    private BallIdentityData _identity;

    [Header("UI References")]
    [SerializeField]
    [Tooltip("Text element displaying the price of this ball.")]
    private TMP_Text _priceText;

    [SerializeField]
    [Tooltip("Shapes Disc component representing the ball visual.")]
    private Disc _visualDisc;

    [Header("Hover & Offset Settings")]
    [SerializeField]
    [Tooltip("Offset distance for price text relative to slot center.")]
    private float _priceTextOffset = 0.5f;

    [SerializeField]
    [Tooltip("Scale multiplier applied on mouse hover.")]
    private float _hoverScaleMultiplier = 1.15f;

    [SerializeField]
    [Tooltip("Color glow brightness multiplier applied on mouse hover.")]
    private float _hoverGlowMultiplier = 1.5f;

    [Header("Locked State Settings")]
    [SerializeField]
    [Tooltip("If true, this slot is locked. It displays runic symbols, appears grey, and cannot be purchased.")]
    private bool _isLocked = false;

    private Vector3 _localSpawnTargetPosition;
    private Shop _shop;
    private bool _isInteractive = false;
    private bool _isHiding = false;
    private float _lastFlashTime = -1f;
    private Color _originalPriceColor = Color.white;
    private bool _hasCachedOriginalColor = false;

    private float _nextRuneChangeTime = 0f;
    private const float RuneChangeInterval = 0.1f;

    /// <summary>
    /// Gets the parent Shop coordinator linked to this slot.
    /// </summary>
    public Shop ParentShop => _shop;

    /// <summary>
    /// Gets whether this slot is currently interactive (fully spawned and not hidden/animating).
    /// </summary>
    public bool IsInteractive => _isInteractive;

    /// <summary>
    /// Gets whether this slot is currently in its retract/hide animation.
    /// </summary>
    public bool IsHiding => _isHiding;

    /// <summary>
    /// Gets whether this slot is locked.
    /// </summary>
    public bool IsLocked => _isLocked;

    /// <summary>
    /// Gets the identity configuration for this shop slot.
    /// </summary>
    public BallIdentityData identity => _identity;

    /// <summary>
    /// Gets the local target position this slot is animated to during shop activation.
    /// </summary>
    public Vector3 localSpawnTargetPosition => _localSpawnTargetPosition;

    /// <summary>
    /// Links this slot with the parent Shop coordinator and caches default visual states.
    /// </summary>
    public void Initialize(Shop shop)
    {
        _shop = shop;
        _isInteractive = false;
        _isHiding = false;
        
        // Synchronize visual display elements
        if (_priceText != null)
        {
            if (_isLocked)
            {
                _priceText.text = "???";
                if (!_hasCachedOriginalColor)
                {
                    _originalPriceColor = Color.gray;
                    _hasCachedOriginalColor = true;
                }
                _priceText.color = Color.gray;
            }
            else if (_identity != null)
            {
                _priceText.text = _identity.Price.ToString("F0");
                if (!_hasCachedOriginalColor)
                {
                    _originalPriceColor = _priceText.color;
                    _hasCachedOriginalColor = true;
                }
            }
        }

        if (_visualDisc != null)
        {
            if (_isLocked)
            {
                _visualDisc.ColorInner = Color.gray * 0.7f;
                _visualDisc.ColorOuter = Color.gray;
                _visualDisc.Radius = 0.35f; // Standard default radius for locked slots
            }
            else if (_identity != null && _identity.BallData != null)
            {
                _visualDisc.ColorInner = _identity.BallData.color * 0.7f;
                _visualDisc.ColorOuter = _identity.BallData.color;
                _visualDisc.Radius = _identity.BallData.radius;
            }
            
            // Set base local scale to one
            _visualDisc.transform.localScale = Vector3.one;
        }

        gameObject.SetActive(false);
    }

    /// <summary>
    /// Animates the slot into the local target position on the outer radius of the shop.
    /// Positions the price text to always point outwards.
    /// </summary>
    public void SpawnWithMoveAndScale(Vector3 localTargetPos, Vector3 direction, float duration)
    {
        _localSpawnTargetPosition = localTargetPos;
        _isInteractive = false;
        _isHiding = false;
        gameObject.SetActive(true);

        // Kill active tweens of the spawner/hider, hover, and shake/color IDs to prevent conflicts
        string moveId = "slot_move_" + GetInstanceID();
        string hoverId = "slot_hover_" + GetInstanceID();
        string shakeId = "slot_shake_" + GetInstanceID();
        DOTween.Kill(moveId);
        DOTween.Kill(hoverId);
        DOTween.Kill(shakeId);

        // Reset price text color and position to clean defaults
        if (_priceText != null)
        {
            _priceText.DOKill();
            _priceText.color = _originalPriceColor;
            _priceText.transform.localPosition = direction * _priceTextOffset;
        }

        // Spawn from center with zero scale and local position at zero
        transform.localPosition = Vector3.zero;
        transform.localScale = Vector3.zero;
        
        // Ensure child visual scale is reset
        if (_visualDisc != null)
        {
            _visualDisc.transform.localScale = Vector3.one;
        }
        
        transform.DOLocalMove(localTargetPos, duration).SetEase(Ease.OutBack).SetId(moveId);
        transform.DOScale(Vector3.one, duration).SetEase(Ease.OutBack).SetId(moveId);

        // Make the slot interactive when it is almost at the end of the spawn animation (60% duration)
        DOVirtual.DelayedCall(duration * 0.6f, () =>
        {
            if (this != null && !_isHiding)
            {
                _isInteractive = true;
            }
        }).SetId(moveId);
    }

    /// <summary>
    /// Animates the slot back towards the center of the shop (local zero) and deactivates it.
    /// </summary>
    public void HideWithMoveAndScale(Vector3 localCenter, float duration)
    {
        _isInteractive = false;

        string moveId = "slot_move_" + GetInstanceID();
        string hoverId = "slot_hover_" + GetInstanceID();
        string shakeId = "slot_shake_" + GetInstanceID();
        DOTween.Kill(moveId);
        DOTween.Kill(hoverId);
        DOTween.Kill(shakeId);

        // Reset price text color to default
        if (_priceText != null)
        {
            _priceText.DOKill();
            _priceText.color = _originalPriceColor;
        }

        // Reset hover scale and colors on the child visual disc
        if (_visualDisc != null)
        {
            _visualDisc.transform.localScale = Vector3.one;
            if (_identity != null && _identity.BallData != null)
            {
                _visualDisc.ColorOuter = _identity.BallData.color;
            }
        }

        // If not active in hierarchy, clean up state immediately without generating tweens
        if (!gameObject.activeInHierarchy)
        {
            _isHiding = false;
            gameObject.SetActive(false);
            return;
        }

        _isHiding = true;
        transform.DOLocalMove(localCenter, duration).SetEase(Ease.InBack).SetId(moveId);
        transform.DOScale(Vector3.zero, duration).SetEase(Ease.InBack).SetId(moveId)
                 .OnComplete(() =>
                 {
                      _isHiding = false;
                      gameObject.SetActive(false);
                 });
    }

    /// <summary>
    /// Triggers a visual flash and shake feedback when funds are insufficient.
    /// Shakes the child components instead of the main transform to avoid breaking spawn animations.
    /// Animates its outer disc to a bright red glow.
    /// </summary>
    public void FlashPriceTextRed()
    {
        if (Time.time < _lastFlashTime + 0.4f) return;
        _lastFlashTime = Time.time;

        string shakeId = "slot_shake_" + GetInstanceID();
        DOTween.Kill(shakeId);

        // Shake the visual disc instead of the main transform
        if (_visualDisc != null)
        {
            _visualDisc.transform.DOShakePosition(0.4f, 0.15f, 30).SetId(shakeId);
        }

        // Shake and color the price text
        if (_priceText != null)
        {
            _priceText.transform.DOShakePosition(0.4f, 0.15f, 30).SetId(shakeId);

            _priceText.DOKill();
            _priceText.color = Color.red;
            _priceText.DOColor(_originalPriceColor, 0.5f).SetDelay(0.2f);
        }

        // Trigger red HDR glow intensity on the outer disc
        if (_visualDisc != null)
        {
            _visualDisc.DOKill();
            Color originalColor = _isLocked ? Color.gray : (_identity != null && _identity.BallData != null ? _identity.BallData.color : Color.gray);
            _visualDisc.ColorOuter = Color.red * 5.0f; // High intensity HDR glow red
            DOTween.To(() => _visualDisc.ColorOuter, x => _visualDisc.ColorOuter = x, originalColor, 0.5f).SetEase(Ease.OutQuad);
        }
    }

    /// <summary>
    /// Scales up/down and brightens/resets the slot visuals based on hover state.
    /// Tweens are directed at the _visualDisc to prevent interrupting parent slot movement.
    /// </summary>
    public void SetHovered(bool hovered)
    {
        if (_isHiding) return; // Do not apply hover changes while retracting/hiding
        if (_visualDisc == null) return;

        string hoverId = "slot_hover_" + GetInstanceID();
        Transform targetTransform = _visualDisc.transform;

        Color baseColor = _isLocked ? Color.gray : (_identity != null && _identity.BallData != null ? _identity.BallData.color : Color.gray);

        if (hovered)
        {
            if (_shop != null && _shop.IsBeingDragged) return;
            
            // Tween scale of the child visual disc component using unique ID
            DOTween.Kill(hoverId);
            targetTransform.DOScale(Vector3.one * _hoverScaleMultiplier, 0.2f).SetEase(Ease.OutQuad).SetId(hoverId);

            _visualDisc.ColorOuter = baseColor * _hoverGlowMultiplier;
        }
        else
        {
            // Reset scale of the child visual disc component using unique ID
            DOTween.Kill(hoverId);
            targetTransform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutQuad).SetId(hoverId);

            _visualDisc.ColorOuter = baseColor;
        }
    }

    // Unity built-in mouse messages are disabled in favor of GameInputManager's custom cursor and action radius tracking.
    // private void OnMouseEnter() { ... }
    // private void OnMouseExit() { ... }
    // private void OnMouseDown() { ... }

    /// <summary>
    /// Generates a random 3-character string from a specific set of runes/symbols.
    /// </summary>
    private string GetRandomRuneString()
    {
        const string glyphs = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789#@$&?*!%+=<>";
        char[] chars = new char[3];
        for (int i = 0; i < 3; i++)
        {
            chars[i] = glyphs[Random.Range(0, glyphs.Length)];
        }
        return new string(chars);
    }

    /// <summary>
    /// Cycles the locked slot's runic text at a periodic interval.
    /// </summary>
    private void Update()
    {
        if (_isLocked && gameObject.activeInHierarchy && _priceText != null)
        {
            if (Time.time >= _nextRuneChangeTime)
            {
                _priceText.text = GetRandomRuneString();
                _nextRuneChangeTime = Time.time + RuneChangeInterval;
            }
        }
    }
}
