using DG.Tweening;
using Sirenix.OdinInspector;
using Shapes;
using UnityEngine;

/// <summary>
/// Represents a gravitational anomaly that attracts and consumes dynamic entities.
/// </summary>
[RequireComponent(typeof(Disc))]
public class BlackHole : MonoBehaviour
{
    [Header("Physics Settings")]
    [SerializeField]
    [Tooltip("The force applied to pull objects toward the center.")]
    private float _attractForce = 25f;

    [SerializeField]
    [Tooltip("The offset added to the event horizon radius to define the outer attraction range.")]
    private float _attractRadiusOffset = 1f;

    [SerializeField]
    [Tooltip("The event horizon radius where entities are consumed.")]
    private float _gRadius = 1f;
 
    [SerializeField]
    [Tooltip("Defines which layers the black hole can interact with (e.g., Balls, Machines).")]
    private LayerMask _targetLayerMask;

    [Header("Growth Settings")]
    [SerializeField]
    [Tooltip("The radius of the black hole when the game starts.")]
    private float _startRadius = 0.5f;

    [SerializeField]
    [Tooltip("The amount by which the radius grows upon consuming an entity.")]
    private float _growthAmount = 0.03f;

    [Header("Visual References")]
    [Tooltip("The attraction zone sprite renderer.")]
    public SpriteRenderer AttractRenderer;

    [Tooltip("The background Shapes Disc component.")]
    public Disc BackgroundDisc;

    [Tooltip("The shader visual sprite renderer.")]
    public SpriteRenderer ShaderRenderer;

    [Header("Visual Offsets")]
    [SerializeField]
    [Tooltip("Offset added to _gRadius for the Main Disc radius.")]
    private float _mainDiscOffset = -0.54f;

    [SerializeField]
    [Tooltip("Offset added to _gRadius for the Background Disc radius.")]
    private float _backgroundOffset = 1.52f;

    [SerializeField]
    [Tooltip("Offset added to _gRadius for the BlackHoleShader _BlackHoleRadius.")]
    private float _shaderOffset = -0.1f;

    [SerializeField]
    [Tooltip("Offset added to _gRadius for the Attract shader _BlackHoleRadius.")]
    private float _attractShaderOffset = 1.5f;

    // Cache initial values
    private float _initialGRadius;
    private float _initialAttractForce;
    private bool _isInitialized;

    private readonly Collider2D[] _collidersBuffer = new Collider2D[64];
    private Disc _renderer;
    private MaterialPropertyBlock _propBlock;

    /// <summary>
    /// Initializes cached physical values.
    /// </summary>
    private void Awake()
    {
        _renderer = GetComponent<Disc>();
        _propBlock = new MaterialPropertyBlock();

        _initialGRadius = _gRadius;
        _initialAttractForce = _attractForce;

        _isInitialized = true;

        // Appliquer la valeur de départ du gameplay
        _gRadius = _startRadius;
        UpdateVisuals();
    }

    /// <summary>
    /// Processes physical attraction force and horizon checks on target entities.
    /// </summary>
    private void FixedUpdate()
    {
        float currentAttractRadius = GetAttractRadius();
        int count = Physics2D.OverlapCircleNonAlloc(transform.position, currentAttractRadius, _collidersBuffer, _targetLayerMask);

        for (int i = 0; i < count; i++)
        {
            Collider2D col = _collidersBuffer[i];
            Rigidbody2D targetRb = col.attachedRigidbody;

            if (targetRb == null)
            {
                continue;
            }

            Vector2 direction = (Vector2)transform.position - targetRb.position;
            float distance = direction.magnitude;

            // USE _gRadius HERE - This is what caused the compilation error in the old code
            if (distance <= _gRadius)
            {
                ConsumeEntity(col.gameObject);
            }
            else
            {
                AttractEntity(targetRb, direction, distance, currentAttractRadius);
            }
        }
    }

    /// <summary>
    /// Calculates the total range of attraction from the center.
    /// The attraction starts right at the limit of the event horizon (_gRadius).
    /// </summary>
    private float GetAttractRadius()
    {
        return _gRadius + _attractRadiusOffset;
    }

    /// <summary>
    /// Applies a gravitational pull to the target.
    /// </summary>
    private void AttractEntity(Rigidbody2D targetRb, Vector2 direction, float distance, float attractRadius)
    {
        // Avoid division by zero
        if (distance <= 0f) return;

        Vector2 pullDirection = direction / distance;
        
        // Calculate the force multiplier
        // Maximum force at the event horizon (_gRadius), falling to zero at attractRadius
        float range = attractRadius - _gRadius;
        if (range <= 0f) return;

        float distanceFromHorizon = distance - _gRadius;
        float forceMultiplier = 1f - Mathf.Clamp01(distanceFromHorizon / range);

        targetRb.AddForce(pullDirection * _attractForce * forceMultiplier, ForceMode2D.Force);

        // Force drop if dragged by the user
        if (GameInputManager.Instance != null)
        {
            GameInputManager.Instance.ForceDrop();
        }
    }

    /// <summary>
    /// Destroys or recycles the entity and triggers the black hole growth.
    /// </summary>
    private void ConsumeEntity(GameObject targetObject)
    {
        if (targetObject.TryGetComponent(out BallEntity ball))
        {
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
            Destroy(machine.gameObject);
        }
        else
        {
            Destroy(targetObject);
        }

        GrowBlackHole();
    }

    /// <summary>
    /// Grows the black hole radius and triggers visual updates.
    /// </summary>
    private void GrowBlackHole()
    {
        _gRadius += _growthAmount;
        UpdateVisuals();
    }

    /// <summary>
    /// Synchronizes the visual rendering using pure additive differences.
    /// </summary>
    private void UpdateVisuals()
    {
        if (!_isInitialized) return;

        // Calcul du facteur proportionnel de base pour la force d'attraction physique
        float scaleFactor = _initialGRadius > 0f ? _gRadius / _initialGRadius : 1f;
        _attractForce = _initialAttractForce * scaleFactor;

        // 1. Shapes Disc : Centre (Rayon + Offset)
        if (_renderer != null)
        {
            _renderer.Radius = Mathf.Max(0.01f, _gRadius + _mainDiscOffset);
        }

        // 2. Shapes Disc : Background (Rayon + Offset)
        if (BackgroundDisc != null)
        {
            BackgroundDisc.Radius = Mathf.Max(0.01f, _gRadius + _backgroundOffset);
        }

        // 3. Custom Shader : Attraction (Rayon + Offset)
        if (AttractRenderer != null)
        {
            if (_propBlock == null) _propBlock = new MaterialPropertyBlock();
            _propBlock.Clear();
            AttractRenderer.GetPropertyBlock(_propBlock);
            
            _propBlock.SetFloat("_BlackHoleRadius", Mathf.Max(0.01f, _gRadius + _attractShaderOffset));
            
            AttractRenderer.SetPropertyBlock(_propBlock);
        }

        // 4. Custom Shader : Shader effect (Rayon + Offset)
        if (ShaderRenderer != null)
        {
            if (_propBlock == null) _propBlock = new MaterialPropertyBlock();
            _propBlock.Clear();
            ShaderRenderer.GetPropertyBlock(_propBlock);
            
            _propBlock.SetFloat("_BlackHoleRadius", Mathf.Max(0.01f, _gRadius + _shaderOffset));
            
            ShaderRenderer.SetPropertyBlock(_propBlock);
        }
    }

    /// <summary>
    /// Set the gRadius to a new value smoothly using DOTween.
    /// </summary>
    [Button("Set Radius Animated", ButtonSizes.Large)]
    public void SetRadiusAnimated(float targetRadius, float duration = 1f)
    {
        DOTween.To(() => _gRadius, x =>
        {
            _gRadius = x;
            UpdateVisuals();
        }, targetRadius, duration).SetEase(Ease.InOutSine);
    }

    /// <summary>
    /// Synchronizes visuals in real-time when modifying values in the editor during Play Mode.
    /// </summary>
    private void OnValidate()
    {
        if (Application.isPlaying && _isInitialized)
        {
            UpdateVisuals();
        }
    }

    /// <summary>
    /// Draws debug gizmos in the Unity Editor.
    /// </summary>
    private void OnDrawGizmos()
    {
        // Draw Event Horizon
        Gizmos.color = new Color(0f, 1f, 1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, _gRadius);

        // Draw Attraction Range
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, _gRadius + _attractRadiusOffset);
    }

    /// <summary>
    /// Draws highlighted gizmos when selected in the editor.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, _gRadius);

        Gizmos.color = new Color(1f, 0.6f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, _gRadius + _attractRadiusOffset);
    }
}