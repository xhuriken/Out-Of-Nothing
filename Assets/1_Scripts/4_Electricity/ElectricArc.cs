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
    [SerializeField] private Color _activeColor = new Color(0.2f, 0.6f, 1f, 1f); // Sky Blue
    [SerializeField] private Color _waitingColor = new Color(0.5f, 0.5f, 0.5f, 0.5f); // Grayish

    private LineRenderer _lineRenderer;
    private IEnergyNode _startNode;
    private IEnergyNode _endNode;
    private float _nextUpdateTime;
    private bool _isPreview;

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

    private void LateUpdate()
    {
        if (_startNode == null || _endNode == null) return;

        // Update jittery geometry at a fixed visual rate
        if (Time.time >= _nextUpdateTime)
        {
            UpdateArcGeometry();
            _nextUpdateTime = Time.time + _updateFrequency;
        }

        UpdateVisualState();
    }

    private void UpdateVisualState()
    {
        ApplyDynamicFade();

        // Determine if active or waiting
        bool isActive = !_isPreview && (Mathf.Abs(_startNode.EnergyAllocationRate) > 0.001f || Mathf.Abs(_endNode.EnergyAllocationRate) > 0.001f);
        
        Color targetColor = isActive ? _activeColor : _waitingColor;
        
        // Preserve alpha from ApplyDynamicFade
        float currentAlpha = _lineRenderer.startColor.a;
        targetColor.a *= currentAlpha;

        _lineRenderer.startColor = _lineRenderer.endColor = targetColor;
    }

    /// <summary>
    /// Calculates the positions of the LineRenderer points using anchor points on colliders.
    /// </summary>
    private void UpdateArcGeometry()
    {
        // Use the utility to find the closest points on the collider hulls
        Vector3 arcStart = EnergyCollisionUtility.GetAnchorPoint(_startNode, _endNode.Position);
        Vector3 arcEnd = EnergyCollisionUtility.GetAnchorPoint(_endNode, _startNode.Position);
        
        float dist = Vector2.Distance(arcStart, arcEnd);

        for (int i = 0; i < _segmentCount; i++)
        {
            float t = i / (float)(_segmentCount - 1);
            Vector3 targetPoint = Vector3.Lerp(arcStart, arcEnd, t);

            // Keep endpoints locked to the hulls, jitter the middle
            if (i > 0 && i < _segmentCount - 1)
            {
                Vector2 jitter = Random.insideUnitCircle * _jitterMagnitude;
                targetPoint += (Vector3)jitter;
            }

            _lineRenderer.SetPosition(i, targetPoint);
        }
    }

    /// <summary>
    /// Remaps the last 20% of range to shrink the width from 100% to 0%.
    /// </summary>
    private void ApplyDynamicFade()
    {
        float distance = Vector2.Distance(_startNode.Position, _endNode.Position);
        float maxRange = _startNode.ConnectionRadius + _endNode.ConnectionRadius;

        if (maxRange <= 0) return;

        float ratio = distance / maxRange;

        // Shrink width and alpha when reaching max range
        _lineRenderer.widthMultiplier = Mathf.Clamp01((1f - ratio) / 0.2f) * 0.1f;
        
        Color c = _lineRenderer.startColor;
        c.a = Mathf.Clamp01(1f - ratio);
        _lineRenderer.startColor = _lineRenderer.endColor = c;
    }
}