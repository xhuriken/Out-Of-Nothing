using Shapes;
using UnityEngine;

/// <summary>
/// Controls the size and properties of the black hole's visual renders (Shapes Disc and Sprite Shaders) relative to the radius.
/// </summary>
[RequireComponent(typeof(BlackHole))]
public class BlackHoleVisuals : MonoBehaviour
{
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
    private float _backgroundOffset = 0.09f;

    [SerializeField]
    [Tooltip("Offset added to _gRadius for the BlackHoleShader _BlackHoleRadius.")]
    private float _shaderOffset = -0.1f;

    [SerializeField]
    [Tooltip("Offset added to _gRadius for the Attract shader _BlackHoleRadius.")]
    private float _attractShaderOffset = 2.5f;

    /// <summary>
    /// Gets the offset added to _gRadius for the Main Disc radius.
    /// </summary>
    public float MainDiscOffset => _mainDiscOffset;

    /// <summary>
    /// Gets the offset added to _gRadius for the Attract shader _BlackHoleRadius.
    /// </summary>
    public float AttractShaderOffset => _attractShaderOffset;

    private BlackHole _blackHole;
    private Disc _renderer;
    private MaterialPropertyBlock _propBlock;

    /// <summary>
    /// Initializes cached physical values and performs auto-find references.
    /// </summary>
    private void Awake()
    {
        _blackHole = GetComponent<BlackHole>();
        _renderer = GetComponent<Disc>();
        _propBlock = new MaterialPropertyBlock();
        
        AutoFindReferences();
    }

    /// <summary>
    /// Subscribes to the radius change event.
    /// </summary>
    private void OnEnable()
    {
        if (_blackHole != null)
        {
            _blackHole.OnRadiusChanged += UpdateVisuals;
            UpdateVisuals(_blackHole.GRadius);
        }
    }

    /// <summary>
    /// Unsubscribes from the radius change event.
    /// </summary>
    private void OnDisable()
    {
        if (_blackHole != null)
        {
            _blackHole.OnRadiusChanged -= UpdateVisuals;
        }
    }

    /// <summary>
    /// Synchronizes the visual rendering using pure additive differences.
    /// </summary>
    public void UpdateVisuals(float currentGRadius)
    {
        if (_renderer != null && (_blackHole == null || !_blackHole.OverrideMainDisc))
        {
            _renderer.Radius = Mathf.Max(0.01f, currentGRadius + _mainDiscOffset);
        }

        if (BackgroundDisc != null)
        {
            BackgroundDisc.Radius = Mathf.Max(0.01f, currentGRadius + _backgroundOffset);
        }

        if (AttractRenderer != null && (_blackHole == null || !_blackHole.OverrideAttractShader))
        {
            if (_propBlock == null)
            {
                _propBlock = new MaterialPropertyBlock();
            }
            _propBlock.Clear();
            AttractRenderer.GetPropertyBlock(_propBlock);
            _propBlock.SetFloat("_BlackHoleRadius", Mathf.Max(0.01f, currentGRadius + _attractShaderOffset));
            AttractRenderer.SetPropertyBlock(_propBlock);
        }

        if (ShaderRenderer != null)
        {
            if (_propBlock == null)
            {
                _propBlock = new MaterialPropertyBlock();
            }
            _propBlock.Clear();
            ShaderRenderer.GetPropertyBlock(_propBlock);
            _propBlock.SetFloat("_BlackHoleRadius", Mathf.Max(0.01f, currentGRadius + _shaderOffset));
            ShaderRenderer.SetPropertyBlock(_propBlock);
        }
    }

    /// <summary>
    /// Explicitly sets the _BlackHoleRadius parameter on the attract shader renderer.
    /// </summary>
    public void SetAttractShaderRadius(float radius)
    {
        if (AttractRenderer != null)
        {
            if (_propBlock == null)
            {
                _propBlock = new MaterialPropertyBlock();
            }
            _propBlock.Clear();
            AttractRenderer.GetPropertyBlock(_propBlock);
            _propBlock.SetFloat("_BlackHoleRadius", Mathf.Max(0.01f, radius));
            AttractRenderer.SetPropertyBlock(_propBlock);
        }
    }

    /// <summary>
    /// Synchronizes visuals in real-time in the editor during play mode.
    /// </summary>
    private void OnValidate()
    {
        if (Application.isPlaying && _blackHole != null)
        {
            UpdateVisuals(_blackHole.GRadius);
        }
    }

    /// <summary>
    /// Automatically finds child references when reset in the editor.
    /// </summary>
    private void Reset()
    {
        AutoFindReferences();
    }

    /// <summary>
    /// Searches child objects for the required visual components.
    /// </summary>
    public void AutoFindReferences()
    {
        if (AttractRenderer == null)
        {
            foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr.name.Contains("Attract"))
                {
                    AttractRenderer = sr;
                    break;
                }
            }
        }

        if (ShaderRenderer == null)
        {
            foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr.name.Contains("BlackHoleShader") || sr.name.Contains("Shader"))
                {
                    if (sr != AttractRenderer)
                    {
                        ShaderRenderer = sr;
                        break;
                    }
                }
            }
        }

        if (BackgroundDisc == null)
        {
            var parentDisc = GetComponent<Disc>();
            foreach (var disc in GetComponentsInChildren<Disc>(true))
            {
                if (disc != parentDisc)
                {
                    BackgroundDisc = disc;
                    break;
                }
            }
        }
    }
}
