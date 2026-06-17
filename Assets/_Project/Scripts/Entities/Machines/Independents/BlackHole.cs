using DG.Tweening;
using Sirenix.OdinInspector;
using Shapes;
using UnityEngine;
using System;

/// <summary>
/// Core component for the gravitational anomaly, acting as the state coordinator.
/// </summary>
[RequireComponent(typeof(Disc))]
[RequireComponent(typeof(BlackHolePhysics))]
[RequireComponent(typeof(BlackHoleVisuals))]
[RequireComponent(typeof(BlackHoleVisualGlitch))]
public class BlackHole : MonoBehaviour
{
    [Header("Growth Settings")]
    [SerializeField]
    [Tooltip("The radius of the black hole when the game starts.")]
    private float _startRadius = 0.5f;

    [SerializeField]
    [Tooltip("The amount by which the radius grows upon consuming an entity.")]
    private float _growthAmount = 0.005f;

    [SerializeField]
    [Tooltip("The event horizon radius where entities are consumed.")]
    private float _gRadius = 1f;

    [Header("Visual Effects")]
    [SerializeField]
    [Tooltip("Color HDR multiplier during the consume flash effect.")]
    private float _hdrFlashMultiplier = 3f;

    [SerializeField]
    [Tooltip("Duration of the flash build-up phase.")]
    private float _flashInDuration = 0.05f;

    [SerializeField]
    [Tooltip("Duration of the flash decay phase.")]
    private float _flashOutDuration = 0.35f;

    private Disc _disc;
    private Color _baseColor;

    /// <summary>
    /// Event fired whenever the event horizon radius changes.
    /// </summary>
    public event Action<float> OnRadiusChanged;

    /// <summary>
    /// Gets or sets the event horizon radius. Fires the OnRadiusChanged event.
    /// </summary>
    public float GRadius
    {
        get => _gRadius;
        set
        {
            _gRadius = value;
            OnRadiusChanged?.Invoke(_gRadius);
        }
    }

    /// <summary>
    /// Applies initial event horizon radius and caches color data for flash animations.
    /// </summary>
    private void Awake()
    {
        _disc = GetComponent<Disc>();
        if (_disc != null)
        {
            _baseColor = _disc.ColorOuter;
        }
        GRadius = _startRadius;
    }

    /// <summary>
    /// Destroys or recycles the entity and triggers the black hole growth.
    /// </summary>
    public void ConsumeEntity(GameObject targetObject)
    {
        // Force drop if the consumed object is currently dragged
        if (GameInputManager.Instance != null && GameInputManager.Instance.CurrentDraggedObject != null)
        {
            var draggable = targetObject.GetComponentInParent<IDraggable>();
            if (draggable != null && GameInputManager.Instance.CurrentDraggedObject == draggable)
            {
                GameInputManager.Instance.ForceDrop();
            }
        }

        bool isTargetValid = false;

        if (targetObject.TryGetComponent(out BallEntity ball))
        {
            isTargetValid = true;
            
            // If the ball is selected in the CraftingManager, deselect it first to prevent bugs
            if (CraftingManager.Instance != null && CraftingManager.Instance.IsBallSelected(ball))
            {
                CraftingManager.Instance.DeselectBall(ball);
            }

            // Award points based on the consumed ball's SO configuration
            if (ball.Data != null && IncrementManager.Instance != null)
            {
                IncrementManager.Instance.AddPoints(ball.Data.pointValue);
            }

            if (BallPoolManager.Instance != null)
            {
                BallPoolManager.Instance.ReleaseBall(ball);
            }
            else
            {
                Destroy(targetObject);
            }
        }
        else if (targetObject.TryGetComponent(out MachineEntity machine))
        {
            isTargetValid = true;
            machine.gameObject.SetActive(false); // Force synchronous OnDisable to update networks
            Destroy(machine.gameObject);
        }
        else
        {
            // Ensure OnDisable runs before destroy by deactivating it
            targetObject.SetActive(false);
            Destroy(targetObject);
        }

        if (isTargetValid)
        {
            PlayFlash();
        }

        GrowBlackHole();
    }

    /// <summary>
    /// Plays a high-intensity HDR color flash animation on the main Disc.
    /// </summary>
    private void PlayFlash()
    {
        if (_disc == null)
        {
            return;
        }

        // Kill any existing color tweens on the disc to prevent overlapping issues
        DOTween.Kill(_disc);

        // Reset to base color first
        _disc.ColorOuter = _baseColor;

        // Sequence to boost color to HDR, then decay back to base
        Color hdrColor = new Color(
            _baseColor.r * _hdrFlashMultiplier,
            _baseColor.g * _hdrFlashMultiplier,
            _baseColor.b * _hdrFlashMultiplier,
            _baseColor.a
        );

        Sequence seq = DOTween.Sequence();
        seq.SetLink(gameObject); // Safely link to this GameObject's lifecycle
        seq.Append(DOTween.To(() => _disc.ColorOuter, x => _disc.ColorOuter = x, hdrColor, _flashInDuration).SetEase(Ease.OutQuad));
        seq.Append(DOTween.To(() => _disc.ColorOuter, x => _disc.ColorOuter = x, _baseColor, _flashOutDuration).SetEase(Ease.InQuad));
    }

    /// <summary>
    /// Grows the black hole radius by the pre-configured growth amount.
    /// </summary>
    private void GrowBlackHole()
    {
        GRadius += _growthAmount;
    }

    /// <summary>
    /// Set the gRadius to a new value smoothly using DOTween.
    /// </summary>
    [Button("Set Radius Animated", ButtonSizes.Large)]
    public void SetRadiusAnimated(float targetRadius, float duration = 1f)
    {
        DOTween.To(() => GRadius, x => GRadius = x, targetRadius, duration).SetEase(Ease.InOutSine);
    }

    /// <summary>
    /// Synchronizes visuals in real-time when modifying values in the editor.
    /// </summary>
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            OnRadiusChanged?.Invoke(_gRadius);
        }
    }
}