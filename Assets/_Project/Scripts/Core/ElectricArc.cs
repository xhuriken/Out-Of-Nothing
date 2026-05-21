using UnityEngine;

/// <summary>
/// Handles the visual representation of an electric arc between two energy nodes.
/// Implements jitter for electricity effect and distance-based alpha fading.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class ElectricArc : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int _segmentCount = 12;
    [SerializeField] private float _jitterMagnitude = 0.15f;
    [SerializeField] private float _updateFrequency = 0.04f;

    [Header("Colors")]
    [SerializeField] private Color _activeColor = new Color(1f, 0.9f, 0f, 1f); // Golden Yellow
    [SerializeField] private Color _waitingColor = new Color(0.5f, 0.5f, 0.5f, 0.6f); // Neutral Gray

    private LineRenderer _lineRenderer;
    private IEnergyNode _startNode;
    private IEnergyNode _endNode;
    private float _nextUpdateTime;
    private bool _isPreview;
    private bool _isActive;

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.positionCount = _segmentCount;
    }

    /// <summary>
    /// Connects the visual arc to two specific nodes.
    /// </summary>
    public void Initialize(IEnergyNode start, IEnergyNode end, bool isPreview = false)
    {
        _startNode = start;
        _endNode = end;
        _isPreview = isPreview;
        _nextUpdateTime = 0f;
        UpdateVisualState();
    }

    public bool IsConnectedTo(IEnergyNode node)
    {
        return _startNode == node || _endNode == node;
    }

    private void LateUpdate()
    {
        if (_startNode == null || _endNode == null) return;

        UpdateVisualState();

        // If the arc is active, update jittery geometry at a fixed visual rate.
        // If the arc is inactive (flat), update every frame so the straight line tracks moving nodes smoothly without any jitter movement.
        if (_isActive)
        {
            if (Time.time >= _nextUpdateTime)
            {
                UpdateArcGeometry();
                _nextUpdateTime = Time.time + _updateFrequency;
            }
        }
        else
        {
            UpdateArcGeometry();
        }
    }

    private void UpdateVisualState()
    {
        // 1. Calculate base color based on state
        // A node is "Powerable" if it belongs to a network with producers.
        bool startPowered = _startNode.CurrentNetwork != null && _startNode.CurrentNetwork.HasProducers;
        bool endPowered = _endNode.CurrentNetwork != null && _endNode.CurrentNetwork.HasProducers;

        // A node is "Ready" if it's a source (Producer) OR if it's demanding energy (Consumer/Cable not waiting).
        bool startReady = _startNode is IEnergyProducer || _startNode.IsDemanding;
        bool endReady = _endNode is IEnergyProducer || _endNode.IsDemanding;

        // An arc is Active (Yellow) if it's part of a powered path and both ends are ready.
        // We removed !_isPreview to allow yellow balls to show active connections during drag.
        bool isActive = startPowered && endPowered && startReady && endReady;
        _isActive = isActive;

        Color targetColor = isActive ? _activeColor : _waitingColor;

        if (EnergyManager.Instance.EnableLogs && isActive)
        {
            Debug.Log($"[Arc] Arc between {_startNode} and {_endNode} is ACTIVE (Allocation: {_startNode.EnergyAllocationRate:F3} / {_endNode.EnergyAllocationRate:F3})");
        }

        // 2. Calculate alpha based on distance
        float distance = Vector2.Distance(_startNode.Position, _endNode.Position);
        float maxRange = _startNode.ConnectionRadius + _endNode.ConnectionRadius;
        float ratio = maxRange > 0 ? distance / maxRange : 1f;
        float alpha = Mathf.Clamp01(1f - ratio);

        // 3. Apply width fading
        _lineRenderer.widthMultiplier = Mathf.Clamp01((1f - ratio) / 0.2f) * 0.1f;

        // 4. Apply final color with alpha
        targetColor.a *= alpha;

        // Force update the LineRenderer properties
        _lineRenderer.startColor = targetColor;
        _lineRenderer.endColor = targetColor;

        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] { new GradientColorKey(targetColor, 0.0f), new GradientColorKey(targetColor, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(targetColor.a, 0.0f), new GradientAlphaKey(targetColor.a, 1.0f) }
        );
        _lineRenderer.colorGradient = g;

        // NEW: If the material color is overriding vertex colors (common with some shaders),
        // we force the color on the material instance.
        if (_lineRenderer.material != null)
        {
            // We use .material (not .sharedMaterial) to get a unique instance for this arc
            _lineRenderer.material.color = targetColor;

            // Some particle shaders use _TintColor instead of _Color
            if (_lineRenderer.material.HasProperty("_TintColor"))
            {
                _lineRenderer.material.SetColor("_TintColor", targetColor);
            }
        }
    }

    /// <summary>
    /// Calculates the positions of the LineRenderer points using anchor points on physical radii.
    /// </summary>
    private void UpdateArcGeometry()
    {
        // Use the utility to find the anchor points on the visual edge (circle)
        Vector3 arcStart = EnergyCollisionUtility.GetAnchorPoint(_startNode, _endNode.Position);
        Vector3 arcEnd = EnergyCollisionUtility.GetAnchorPoint(_endNode, _startNode.Position);

        float dist = Vector2.Distance(arcStart, arcEnd);

        for (int i = 0; i < _segmentCount; i++)
        {
            float t = i / (float)(_segmentCount - 1);
            Vector3 targetPoint = Vector3.Lerp(arcStart, arcEnd, t);

            // Keep endpoints locked to the hulls, jitter the middle only if the arc is active (carrying energy)
            if (_isActive && i > 0 && i < _segmentCount - 1)
            {
                Vector2 jitter = Random.insideUnitCircle * _jitterMagnitude;
                targetPoint += (Vector3)jitter;
            }

            _lineRenderer.SetPosition(i, targetPoint);
        }
    }
}