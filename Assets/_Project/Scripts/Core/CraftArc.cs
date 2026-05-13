using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Handles a single jittery line segment between two balls for the crafting system.
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

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
    }

    public void Setup(BallEntity start, BallEntity end, int segments, float jitter, float frequency)
    {
        _startBall = start;
        _endBall = end;
        _segments = segments;
        _jitterMagnitude = jitter;
        _updateFrequency = frequency;

        if (_lineRenderer.positionCount != segments)
        {
            _lineRenderer.positionCount = segments;
            _jitterOffsets = new Vector3[segments];
        }
    }

    private void LateUpdate()
    {
        if (_startBall == null || _endBall == null) return;

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
        Vector2 pos1 = _startBall.transform.position;
        Vector2 pos2 = _endBall.transform.position;
        
        float dist = Vector2.Distance(pos1, pos2);
        float radiusSum = _startBall.Data.radius + _endBall.Data.radius;

        // Hide if balls overlap too much
        if (dist < radiusSum * 0.8f)
        {
            _lineRenderer.enabled = false;
            return;
        }
        
        _lineRenderer.enabled = true;

        Vector2 dir = (pos2 - pos1).normalized;
        Vector3 startPos = (Vector3)(pos1 + dir * _startBall.Data.radius);
        Vector3 endPos = (Vector3)(pos2 - dir * _endBall.Data.radius);

        for (int i = 0; i < _segments; i++)
        {
            float t = i / (float)(_segments - 1);
            Vector3 point = Vector3.Lerp(startPos, endPos, t);

            if (i > 0 && i < _segments - 1)
            {
                point += _jitterOffsets[i];
            }

            _lineRenderer.SetPosition(i, point);
        }
    }
}
