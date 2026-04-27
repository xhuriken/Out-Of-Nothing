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
    [SerializeField] private float _updateFrequency = 0.04f; // ~25 FPS for the "twitchy" look

    private LineRenderer _lineRenderer;
    private IEnergyNode _startNode;
    private IEnergyNode _endNode;
    private float _nextUpdateTime;

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.positionCount = _segmentCount;
    }

    /// <summary>
    /// Connects the visual arc to two specific nodes.
    /// </summary>
    public void Initialize(IEnergyNode start, IEnergyNode end)
    {
        _startNode = start;
        _endNode = end;
        _nextUpdateTime = 0f;
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

        ApplyDynamicFade();
    }

    /// <summary>
    /// Calculates the positions of the LineRenderer points with random offsets.
    /// Points are placed on the circumferences of the nodes (Shortest Path).
    /// </summary>
    private void UpdateArcGeometry()
    {
        Vector2 startPos = _startNode.Position;
        Vector2 endPos = _endNode.Position;
        
        Vector2 direction = (endPos - startPos).normalized;
        float dist = Vector2.Distance(startPos, endPos);

        // Calculate points on circumference
        Vector3 arcStart = startPos + direction * _startNode.PhysicalRadius;
        Vector3 arcEnd = endPos - direction * _endNode.PhysicalRadius;

        // If nodes overlap, don't Draw or use centers
        if (dist < (_startNode.PhysicalRadius + _endNode.PhysicalRadius))
        {
            arcStart = startPos;
            arcEnd = endPos;
        }

        for (int i = 0; i < _segmentCount; i++)
        {
            float t = i / (float)(_segmentCount - 1);
            Vector3 targetPoint = Vector3.Lerp(arcStart, arcEnd, t);

            // Keep endpoints locked to the circumferences, jitter the middle
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
    /// Uses the sum of connection radii as the limit.
    /// </summary>
    private void ApplyDynamicFade()
    {
        float distance = Vector2.Distance(_startNode.Position, _endNode.Position);
        float maxRange = _startNode.ConnectionRadius + _endNode.ConnectionRadius;

        if (maxRange <= 0) return;

        float ratio = distance / maxRange;

        // Math: (1 - ratio) / 0.2f keeps multiplier at 1.0 until ratio hits 0.8
        _lineRenderer.widthMultiplier = Mathf.Clamp01((1f - ratio) / 0.2f) * 0.1f;

        // Alpha fade (Linear)
        Color c = _lineRenderer.startColor;
        c.a = Mathf.Clamp01(1f - ratio);
        _lineRenderer.startColor = _lineRenderer.endColor = c;
    }
}