using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// Handles a single jittery line segment between two balls for the crafting system.
/// Features high-fidelity spawn and despawn animations where endpoints grow outward from/shrink back to the midpoint.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class CraftArc : MonoBehaviour
{
    private LineRenderer _lineRenderer;
    private BallEntity _startBall;
    private BallEntity _endBall;
    
    private int _segments;
    private float _jitterMagnitude;
    private float _updateFrequency;
    
    private float _nextUpdateTime;
    private Vector3[] _jitterOffsets;

    private float _animProgress = 0f;
    private float _initialWidthMultiplier;
    private bool _isDestroying = false;

    private Vector3 _lastStartPos;
    private Vector3 _lastEndPos;
    private float _startRadius;
    private float _endRadius;

    /// <summary>
    /// Gets the starting ball for this line segment.
    /// </summary>
    public BallEntity StartBall => _startBall;

    /// <summary>
    /// Gets the ending ball for this line segment.
    /// </summary>
    public BallEntity EndBall => _endBall;

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _initialWidthMultiplier = _lineRenderer.widthMultiplier;
        _lineRenderer.widthMultiplier = 0f; // Start completely thin
    }

    /// <summary>
    /// Sets up the line segment between two selected balls and initiates its spawn animation.
    /// </summary>
    public void Setup(BallEntity start, BallEntity end, int segments, float jitter, float frequency)
    {
        _startBall = start;
        _endBall = end;
        _segments = segments;
        _jitterMagnitude = jitter;
        _updateFrequency = frequency;

        _startRadius = start != null && start.Data != null ? start.Data.radius : 0.5f;
        _endRadius = end != null && end.Data != null ? end.Data.radius : 0.5f;

        if (_lineRenderer.positionCount != segments)
        {
            _lineRenderer.positionCount = segments;
            _jitterOffsets = new Vector3[segments];
        }

        // snapy spawn animation: animate _animProgress from 0 to 1
        DOTween.Kill(this);
        _animProgress = 0f;
        DOTween.To(() => _animProgress, x => _animProgress = x, 1f, 0.25f)
            .SetEase(Ease.OutQuad)
            .SetTarget(this);
    }

    /// <summary>
    /// Initiates the despawn animation (shrinking back to midpoint) and destroys the line object on complete.
    /// </summary>
    public void Despawn()
    {
        if (_isDestroying) return;
        _isDestroying = true;

        DOTween.Kill(this);
        DOTween.To(() => _animProgress, x => _animProgress = x, 0f, 0.2f)
            .SetEase(Ease.InQuad)
            .SetTarget(this)
            .OnComplete(() =>
            {
                Destroy(gameObject);
            });
    }

    private void LateUpdate()
    {
        if (_startBall == null || _endBall == null)
        {
            if (_isDestroying)
            {
                UpdateGeometry();
            }
            return;
        }

        UpdateJitter();
        UpdateGeometry();
    }

    private void UpdateJitter()
    {
        if (Time.time < _nextUpdateTime) return;
        
        _nextUpdateTime = Time.time + _updateFrequency;
        
        for (int i = 1; i < _segments - 1; i++)
        {
            _jitterOffsets[i] = (Vector3)Random.insideUnitCircle * _jitterMagnitude;
        }
    }

    private void UpdateGeometry()
    {
        Vector3 pos1;
        Vector3 pos2;
        float radiusSum = _startRadius + _endRadius;

        if (_startBall != null && _endBall != null)
        {
            pos1 = _startBall.transform.position;
            pos2 = _endBall.transform.position;
            _lastStartPos = pos1;
            _lastEndPos = pos2;

            float dist = Vector2.Distance(pos1, pos2);
            // Hide if balls overlap too much (only if not already destroying)
            if (!_isDestroying && dist < radiusSum * 0.8f)
            {
                _lineRenderer.enabled = false;
                return;
            }
        }
        else
        {
            pos1 = _lastStartPos;
            pos2 = _lastEndPos;
        }
        
        _lineRenderer.enabled = true;

        Vector3 dir = (pos2 - pos1);
        float distance = dir.magnitude;
        dir = distance > 0.001f ? dir.normalized : Vector3.right;

        Vector3 startPos = pos1 + dir * _startRadius;
        Vector3 endPos = pos2 - dir * _endRadius;

        // Calculate midpoint between the outer edges
        Vector3 midpoint = (startPos + endPos) * 0.5f;

        // Slide the start and end positions outwards from the midpoint based on progress
        Vector3 currentStart = Vector3.Lerp(midpoint, startPos, _animProgress);
        Vector3 currentEnd = Vector3.Lerp(midpoint, endPos, _animProgress);

        // Adjust line thickness
        _lineRenderer.widthMultiplier = _initialWidthMultiplier * _animProgress;

        for (int i = 0; i < _segments; i++)
        {
            float t = i / (float)(_segments - 1);
            Vector3 point = Vector3.Lerp(currentStart, currentEnd, t);

            if (i > 0 && i < _segments - 1)
            {
                point += _jitterOffsets[i] * _animProgress;
            }

            _lineRenderer.SetPosition(i, point);
        }
    }
}
