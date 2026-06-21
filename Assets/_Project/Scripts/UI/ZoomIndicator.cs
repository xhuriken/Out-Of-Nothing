using UnityEngine;
using Shapes;
using DG.Tweening;

/// <summary>
/// Controls the Shapes.Line indicator's Y endpoint based on the camera zoom level.
/// Interpolates between 0 (fully zoomed out) and the Y position of the background line's endpoint (fully zoomed in).
/// Also manages the visibility of a ShapeGroup on the same object, making it transparent by default
/// and fading it in during zoom activity, then fading it out after a delay of inactivity.
/// </summary>
public class ZoomIndicator : MonoBehaviour
{
    [Header("Shapes References")]
    [Tooltip("The background line component used as the maximum reference height.")]
    [SerializeField] private Line _backgroundLine;

    [Tooltip("The indicator line component that scales with camera zoom.")]
    [SerializeField] private Line _indicatorLine;

    [Tooltip("The line representing the zoomed-in range ('TooMuchZoom').")]
    [SerializeField] private Line _tooMuchZoomLine;

    [Header("Visibility & Transition Settings")]
    [Tooltip("Ease function for the fade in and out transitions.")]
    [SerializeField] private Ease _fadeEase = Ease.OutQuad;

    [Tooltip("Duration in seconds of the fade in and out transitions.")]
    [SerializeField] private float _fadeDuration = 0.25f;

    [Tooltip("Time in seconds to wait after the last zoom change before fading out the indicator.")]
    [SerializeField] private float _hideDelay = 1.0f;

    private ShapeGroup _shapeGroup;
    private Color _initialColor;
    private float _lastOrthoSize;
    private float _lastZoomTime;
    private float _defaultDezoomSize;
    private bool _isVisible;
    private Tween _fadeTween;

    /// <summary>
    /// Initializes references, caches initial colors, and sets default transparent state.
    /// </summary>
    private void Awake()
    {
        _shapeGroup = GetComponent<ShapeGroup>();
    }

    /// <summary>
    /// Caches initial state and applies default transparency.
    /// </summary>
    private void Start()
    {
        if (_shapeGroup != null)
        {
            _initialColor = _shapeGroup.Color;
            
            // Set by default to transparent
            Color transparentColor = _initialColor;
            transparentColor.a = 0f;
            _shapeGroup.Color = transparentColor;
        }

        if (CameraController.Instance != null)
        {
            _lastOrthoSize = CameraController.Instance.CurrentOrthoSize;
            _defaultDezoomSize = CameraController.Instance.MaxDezoomSize;
        }
    }

    /// <summary>
    /// Updates the indicator line height based on camera zoom level relative to the background reference.
    /// Also manages the ShapeGroup fading on zoom updates.
    /// </summary>
    private void Update()
    {
        if (CameraController.Instance == null) return;

        float currentOrtho = CameraController.Instance.CurrentOrthoSize;

        // Initialize _lastOrthoSize and _defaultDezoomSize if not yet set (safety checks)
        if (_lastOrthoSize < 0.001f)
        {
            _lastOrthoSize = currentOrtho;
        }
        if (_defaultDezoomSize < 0.001f)
        {
            _defaultDezoomSize = CameraController.Instance.MaxDezoomSize;
        }

        bool orthoChanged = !Mathf.Approximately(currentOrtho, _lastOrthoSize);

        if (orthoChanged)
        {
            _lastOrthoSize = currentOrtho;
            _lastZoomTime = Time.time;

            if (!_isVisible)
            {
                FadeIn();
            }
        }
        else if (_isVisible && (Time.time - _lastZoomTime > _hideDelay))
        {
            FadeOut();
        }

        if (_backgroundLine == null || _indicatorLine == null) return;

        float minZoom = CameraController.Instance.MaxZoomSize;
        float maxZoom = CameraController.Instance.MaxDezoomSize;

        float range = maxZoom - minZoom;
        float t = range > 0.001f ? Mathf.Clamp01((maxZoom - currentOrtho) / range) : 0f;

        float backgroundMaxY = _backgroundLine.End.y;
        float targetY = t * backgroundMaxY;

        // Keep current X and Z components, only update the Y value
        Vector3 newEnd = _indicatorLine.End;
        newEnd.y = targetY;
        _indicatorLine.End = newEnd;

        // Manage the TooMuchZoom line bounds
        if (_tooMuchZoomLine != null && _defaultDezoomSize > 0.001f)
        {
            float x1Y = 0f;
            if (range > 0.001f)
            {
                // Calculate the Y boundary for defaultDezoomSize (x1) in the current zoom range
                float t_x1 = (maxZoom - _defaultDezoomSize) / range;
                t_x1 = Mathf.Clamp01(t_x1);
                x1Y = t_x1 * backgroundMaxY;
            }

            // Set top to match background reference top (backgroundMaxY)
            Vector3 tooMuchStart = _tooMuchZoomLine.Start;
            tooMuchStart.y = backgroundMaxY;
            _tooMuchZoomLine.Start = tooMuchStart;

            // Set bottom to match the x1 zoom threshold Y value
            Vector3 tooMuchEnd = _tooMuchZoomLine.End;
            tooMuchEnd.y = x1Y;
            _tooMuchZoomLine.End = tooMuchEnd;
        }
    }

    /// <summary>
    /// Smoothly fades in the ShapeGroup.
    /// </summary>
    private void FadeIn()
    {
        _isVisible = true;
        if (_shapeGroup == null) return;

        _fadeTween?.Kill();
        Color targetColor = _initialColor;

        _fadeTween = DOTween.To(() => _shapeGroup.Color, x => _shapeGroup.Color = x, targetColor, _fadeDuration)
            .SetEase(_fadeEase)
            .SetUpdate(true); // Ensure it functions even if timeScale is paused/altered
    }

    /// <summary>
    /// Smoothly fades out the ShapeGroup to transparent.
    /// </summary>
    private void FadeOut()
    {
        _isVisible = false;
        if (_shapeGroup == null) return;

        _fadeTween?.Kill();
        Color targetColor = _initialColor;
        targetColor.a = 0f;

        _fadeTween = DOTween.To(() => _shapeGroup.Color, x => _shapeGroup.Color = x, targetColor, _fadeDuration)
            .SetEase(_fadeEase)
            .SetUpdate(true); // Ensure it functions even if timeScale is paused/altered
    }

    /// <summary>
    /// Cleans up active tweens on destroy.
    /// </summary>
    private void OnDestroy()
    {
        _fadeTween?.Kill();
    }
}
