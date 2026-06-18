using DG.Tweening;
using Sirenix.OdinInspector;
using Shapes;
using UnityEngine;
using UnityEngine.InputSystem;
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

    [Header("Implosion Animation (ImploseNothing)")]
    [SerializeField]
    [Tooltip("Target GRadius at the end of Phase 1.")]
    private float _implodeGRadiusTarget = 0.25f;

    [SerializeField]
    [Tooltip("Duration of Phase 1 (Xtemps).")]
    private float _xDuration = 1f;

    [SerializeField]
    [Tooltip("Ease curve for Phase 1 (Xtemps).")]
    private Ease _xEase = Ease.InOutElastic;

    [SerializeField]
    [Tooltip("Target radius of the main disc at the end of Phase 2.")]
    private float _implodeMainDiscTargetRadius = 0.1f;

    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("Percentage of the attract shader target radius that the black hole (GRadius and Main Disc) should reach at the end of Phase 3.")]
    private float _implodeGRadiusGrowthPercent = 0.8f;

    [SerializeField]
    [Tooltip("Target outer color of the main disc at the end of Phase 2.")]
    private Color _implodeTargetColor = Color.red;

    [SerializeField]
    [Tooltip("Duration of Phase 2 (Ytemps).")]
    private float _yDuration = 1f;

    [SerializeField]
    [Tooltip("Ease curve for Phase 2 (Ytemps).")]
    private Ease _yEase = Ease.InOutQuad;

    [SerializeField]
    [Tooltip("Duration of Phase 3 (Ztemps).")]
    private float _zDuration = 1f;

    [SerializeField]
    [Tooltip("Ease curve for Phase 3 (Ztemps).")]
    private Ease _zEase = Ease.InOutQuad;

    [SerializeField]
    [Tooltip("Duration of the restoration Phase 4.")]
    private float _returnDuration = 1f;

    [SerializeField]
    [Tooltip("Ease curve for Phase 4.")]
    private Ease _returnEase = Ease.InOutQuad;

    [Header("Shake Settings")]
    [SerializeField]
    [Tooltip("Amplitude of the GRadius shake during Phase 2 and 3.")]
    private float _shakeAmplitude = 0.15f;

    [SerializeField]
    [Tooltip("Frequency of the GRadius shake during Phase 2 and 3.")]
    private float _shakeFrequency = 50f;

    private Disc _disc;
    private Color _baseColor;
    private Color _currentColor;
    private float _flashIntensityMultiplier = 1f;
    private Tween _flashTween;
    private Sequence _implodeSequence;
    private float _originalMainDiscThickness;
    private BlackHoleVisuals _visuals;
    private float _gRadiusShakeOffset = 0f;

    /// <summary>
    /// Gets or sets a value indicating whether the main disc radius/thickness is overridden by custom animations.
    /// </summary>
    public bool OverrideMainDisc { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether the attract shader radius parameter is overridden by custom animations.
    /// </summary>
    public bool OverrideAttractShader { get; set; } = false;

    /// <summary>
    /// Gets or sets the physical attraction radius override during custom animations.
    /// </summary>
    public float CurrentAttractPhysicsRadius { get; set; }

    /// <summary>
    /// Gets a value indicating whether the black hole is currently playing the implosion sequence.
    /// </summary>
    public bool IsImploding { get; private set; } = false;

    /// <summary>
    /// Event fired whenever the event horizon radius changes.
    /// </summary>
    public event Action<float> OnRadiusChanged;

    /// <summary>
    /// Gets or sets the event horizon radius. Fires the OnRadiusChanged event.
    /// </summary>
    public float GRadius
    {
        get => _gRadius + _gRadiusShakeOffset;
        set
        {
            _gRadius = value - _gRadiusShakeOffset;
            OnRadiusChanged?.Invoke(GRadius);
        }
    }

    /// <summary>
    /// Applies initial event horizon radius and caches color/thickness data for animations.
    /// </summary>
    private void Awake()
    {
        _disc = GetComponent<Disc>();
        if (_disc != null)
        {
            _baseColor = _disc.ColorOuter;
            _currentColor = _baseColor;
            _originalMainDiscThickness = _disc.Thickness;
        }
        _visuals = GetComponent<BlackHoleVisuals>();
        GRadius = _startRadius;
    }

    /// <summary>
    /// Listens for keyboard shortcuts at runtime.
    /// </summary>
    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame)
        {
            ImploseNothing();
        }
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
        else if (targetObject.TryGetComponent(out Shop shop))
        {
            return;
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
    /// Updates the outer color of the main disc based on the current base color and flash intensity multiplier.
    /// </summary>
    private void UpdateDiscColor()
    {
        if (_disc != null)
        {
            Color targetColor = new Color(
                _currentColor.r * _flashIntensityMultiplier,
                _currentColor.g * _flashIntensityMultiplier,
                _currentColor.b * _flashIntensityMultiplier,
                _currentColor.a
            );
            _disc.ColorOuter = targetColor;
        }
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

        // Kill the existing flash tween to prevent overlapping issues
        if (_flashTween != null && _flashTween.IsActive())
        {
            _flashTween.Kill();
        }

        Sequence seq = DOTween.Sequence();
        seq.SetLink(gameObject);

        seq.Append(DOTween.To(() => _flashIntensityMultiplier, x =>
        {
            _flashIntensityMultiplier = x;
            UpdateDiscColor();
        }, _hdrFlashMultiplier, _flashInDuration).SetEase(Ease.OutQuad));

        seq.Append(DOTween.To(() => _flashIntensityMultiplier, x =>
        {
            _flashIntensityMultiplier = x;
            UpdateDiscColor();
        }, 1f, _flashOutDuration).SetEase(Ease.InQuad));

        _flashTween = seq;
    }

    /// <summary>
    /// Grows the black hole radius by the pre-configured growth amount.
    /// </summary>
    private void GrowBlackHole()
    {
        if (IsImploding)
        {
            return;
        }
        _gRadius += _growthAmount;
        OnRadiusChanged?.Invoke(GRadius);
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
    /// Triggers the 4-phase custom implosion and restoration animation sequence.
    /// </summary>
    [Button("Implose Nothing", ButtonSizes.Large)]
    public void ImploseNothing()
    {
        if (_implodeSequence != null && _implodeSequence.IsActive())
        {
            _implodeSequence.Kill();
        }

        IsImploding = true;
        OverrideMainDisc = false;
        OverrideAttractShader = false;
        _gRadiusShakeOffset = 0f;

        float preImplodeGRadius = GRadius;
        float targetGRadiusPhase1 = _implodeGRadiusTarget;

        _implodeSequence = DOTween.Sequence();
        _implodeSequence.SetLink(gameObject);

        // --- Phase 1: Xtemps (default 1) ---
        // GRadius decreases to target GRadius using xEase
        _implodeSequence.Append(
            DOTween.To(() => GRadius, x => GRadius = x, targetGRadiusPhase1, _xDuration)
                   .SetEase(_xEase)
        );

        // --- Transition to Phase 2 ---
        _implodeSequence.AppendCallback(() =>
        {
            OverrideMainDisc = true;
        });

        // --- Phase 2: Ytemps ---
        // Main disc radius decreases, and thickness increases proportionally to keep the outer edge stationary.
        // Outer color changes to target implode color.
        float startMainDiscRadius = targetGRadiusPhase1 + (_visuals != null ? _visuals.MainDiscOffset : -0.54f);
        float outerBoundary = startMainDiscRadius + _originalMainDiscThickness * 0.5f;
        float baseMainDiscRadius = startMainDiscRadius;

        _implodeSequence.Append(
            DOTween.To(() => baseMainDiscRadius, r =>
            {
                baseMainDiscRadius = r;
                _disc.Radius = r + _gRadiusShakeOffset;
                _disc.Thickness = 2f * (outerBoundary - r);
            }, _implodeMainDiscTargetRadius, _yDuration)
                   .SetEase(_yEase)
        );

        _implodeSequence.Join(
            DOTween.To(() => _currentColor, c =>
            {
                _currentColor = c;
                UpdateDiscColor();
            }, _implodeTargetColor, _yDuration)
                   .SetEase(_yEase)
        );

        // --- Transition to Phase 3 ---
        _implodeSequence.AppendCallback(() =>
        {
            OverrideAttractShader = true;
        });

        // --- Phase 3: Ztemps ---
        // Attract shader radius grows to target covering the entire GameZone boundaries (+ 3 margin)
        float attractShaderRadiusTarget = 5f; // Fallback
        if (GameZone.Instance != null)
        {
            float halfWidth = Mathf.Max(Mathf.Abs(GameZone.Instance.MinX), Mathf.Abs(GameZone.Instance.MaxX));
            float halfHeight = Mathf.Max(Mathf.Abs(GameZone.Instance.MinY), Mathf.Abs(GameZone.Instance.MaxY));
            attractShaderRadiusTarget = Mathf.Sqrt(halfWidth * halfWidth + halfHeight * halfHeight) + 3f;
        }

        float startAttractShaderRadius = targetGRadiusPhase1 + (_visuals != null ? _visuals.AttractShaderOffset : 2.5f);
        float currentAttractShaderRadius = startAttractShaderRadius;
        CurrentAttractPhysicsRadius = currentAttractShaderRadius + _gRadiusShakeOffset;

        float blackHoleGrowthTarget = _implodeGRadiusGrowthPercent * attractShaderRadiusTarget;
        float mainDiscGrowthTarget = blackHoleGrowthTarget + (_visuals != null ? _visuals.MainDiscOffset : -0.54f);
        float phase2EndThickness = 2f * (outerBoundary - _implodeMainDiscTargetRadius);

        _implodeSequence.Append(
            DOTween.To(() => currentAttractShaderRadius, x =>
            {
                currentAttractShaderRadius = x;
                CurrentAttractPhysicsRadius = x + _gRadiusShakeOffset;
                if (_visuals != null)
                {
                    _visuals.SetAttractShaderRadius(x + _gRadiusShakeOffset);
                }
            }, attractShaderRadiusTarget, _zDuration)
                   .SetEase(_zEase)
        );

        _implodeSequence.Join(
            DOTween.To(() => baseMainDiscRadius, r =>
            {
                baseMainDiscRadius = r;
                _disc.Radius = r + _gRadiusShakeOffset;
            }, mainDiscGrowthTarget, _zDuration)
                   .SetEase(_zEase)
        );

        _implodeSequence.Join(
            DOTween.To(() => _disc.Thickness, t => _disc.Thickness = t, _originalMainDiscThickness, _zDuration)
                   .SetEase(_zEase)
        );

        _implodeSequence.Join(
            DOTween.To(() => GRadius, g => GRadius = g, blackHoleGrowthTarget, _zDuration)
                   .SetEase(_zEase)
        );

        // --- Run Shake Tween in Parallel across Phase 2 and Phase 3 ---
        float totalShakeDuration = _yDuration + _zDuration;
        float shakeTime = 0f;
        _implodeSequence.Insert(_xDuration,
            DOTween.To(() => shakeTime, val =>
            {
                shakeTime = val;
                float progress = val / totalShakeDuration;
                float decay = 1f - progress;
                _gRadiusShakeOffset = _shakeAmplitude * Mathf.Sin(val * _shakeFrequency) * decay;
                OnRadiusChanged?.Invoke(GRadius);

                if (OverrideMainDisc && _disc != null)
                {
                    _disc.Radius = baseMainDiscRadius + _gRadiusShakeOffset;
                }

                if (OverrideAttractShader && _visuals != null)
                {
                    _visuals.SetAttractShaderRadius(currentAttractShaderRadius + _gRadiusShakeOffset);
                    CurrentAttractPhysicsRadius = currentAttractShaderRadius + _gRadiusShakeOffset;
                }
            }, totalShakeDuration, totalShakeDuration).SetEase(Ease.Linear)
        );

        // --- Transition to Phase 4 ---
        _implodeSequence.AppendCallback(() =>
        {
            _gRadiusShakeOffset = 0f;
        });

        // --- Phase 4: Stylized Return ---
        // GRadius, main disc radius, main disc thickness, base color, and attract shader radius return to standard defaults.
        float endMainDiscRadius = preImplodeGRadius + (_visuals != null ? _visuals.MainDiscOffset : -0.54f);
        float endAttractShaderRadius = preImplodeGRadius + (_visuals != null ? _visuals.AttractShaderOffset : 2.5f);
        float returnCurrentAttractShaderRadius = attractShaderRadiusTarget;

        _implodeSequence.Append(
            DOTween.To(() => GRadius, x => GRadius = x, preImplodeGRadius, _returnDuration)
                   .SetEase(_returnEase)
        );

        _implodeSequence.Join(
            DOTween.To(() => _disc.Radius, x => _disc.Radius = x, endMainDiscRadius, _returnDuration)
                   .SetEase(_returnEase)
        );

        _implodeSequence.Join(
            DOTween.To(() => _disc.Thickness, x => _disc.Thickness = x, _originalMainDiscThickness, _returnDuration)
                   .SetEase(_returnEase)
        );

        _implodeSequence.Join(
            DOTween.To(() => _currentColor, c =>
            {
                _currentColor = c;
                UpdateDiscColor();
            }, _baseColor, _returnDuration)
                   .SetEase(_returnEase)
        );

        _implodeSequence.Join(
            DOTween.To(() => returnCurrentAttractShaderRadius, x =>
            {
                returnCurrentAttractShaderRadius = x;
                CurrentAttractPhysicsRadius = x;
                if (_visuals != null)
                {
                    _visuals.SetAttractShaderRadius(x);
                }
            }, endAttractShaderRadius, _returnDuration)
                   .SetEase(_returnEase)
        );

        _implodeSequence.OnComplete(() =>
        {
            OverrideMainDisc = false;
            OverrideAttractShader = false;
            IsImploding = false;
            _gRadiusShakeOffset = 0f;
            if (_visuals != null)
            {
                _visuals.UpdateVisuals(GRadius);
            }
        });
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