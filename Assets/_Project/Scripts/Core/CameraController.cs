using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

/// <summary>
/// Controls the camera's orthographic size dynamically via scroll input.
/// Zoom out is bounded by the current dimensions of the GameZone.
/// Also allows panning the camera using the middle mouse click, bounded by the map.
/// </summary>
public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [Header("Zoom Limits & Sensitivity")]
    [Tooltip("Maximum zoom (smallest orthographic size allowed).")]
    [SerializeField] private float _maxZoomSize = 3f;

    [Tooltip("Sensitivity of the scroll wheel zoom.")]
    [SerializeField] private float _zoomSpeed = 0.05f;

    [Tooltip("Smooth time for camera orthographic size transitions.")]
    [SerializeField] private float _smoothTime = 0.2f;

    [Tooltip("Multiplier applied to zoom speed when holding Control.")]
    [SerializeField] private float _ctrlZoomMultiplier = 5f;

    private Camera _camera;
    private float _targetOrthoSize;

    // Panning variables
    private Vector2 _dragStartMousePos;
    private Vector3 _dragStartCameraPos;
    private bool _isPanning;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _camera = GetComponent<Camera>();
        if (_camera == null)
        {
            _camera = Camera.main;
        }

        if (_camera != null)
        {
            _targetOrthoSize = _camera.orthographicSize;
        }
    }

    private void Update()
    {
        HandlePanning();
    }

    private void LateUpdate()
    {
        // Keep the camera position clamped within the GameZone boundaries
        transform.position = ClampCameraPosition(transform.position);
    }

    /// <summary>
    /// Adjusts the target orthographic size based on scroll input.
    /// Positive values zoom in, negative values zoom out.
    /// Zoom is centered on the player's custom cursor location.
    /// </summary>
    /// <param name="scrollDelta">The scroll wheel input value.</param>
    public void AdjustZoom(float scrollDelta)
    {
        if (_camera == null) return;
        if (Mathf.Approximately(scrollDelta, 0f)) return;

        // Auto-scale normalized/small scroll values (e.g. from trackpads or specific OS configurations)
        // A typical scroll wheel notch is +/-120. If we receive a small value (e.g., +/-1), we scale it up.
        float normalizedDelta = scrollDelta;
        if (Mathf.Abs(normalizedDelta) > 0f && Mathf.Abs(normalizedDelta) < 1.5f)
        {
            normalizedDelta *= 120f;
        }

        // Calculate the maximum dezoom limit based on the current GameZone size and camera aspect ratio
        float maxDezoomSize = GetMaxDezoomSize();
        float minZoomSize = _maxZoomSize;

        // Bounding check to avoid division/overflow issues
        if (maxDezoomSize < minZoomSize)
        {
            maxDezoomSize = minZoomSize;
        }

        // Ensure target ortho size starts clamped within bounds if it was out of bounds
        _targetOrthoSize = Mathf.Clamp(_targetOrthoSize, minZoomSize, maxDezoomSize);

        // Get the cursor world position using the custom GameCursor, with fallbacks
        Vector3 cursorWorldPos;
        if (GameCursor.Instance != null)
        {
            cursorWorldPos = GameCursor.Instance.transform.position;
        }
        else if (Mouse.current != null)
        {
            Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
            cursorWorldPos = _camera.ScreenToWorldPoint(mouseScreenPosition);
        }
        else
        {
            cursorWorldPos = transform.position;
        }
        cursorWorldPos.z = 0f;

        // Calculate new target orthographic size. 
        // We invert the sign of normalizedDelta because scrolling UP (positive) should zoom IN (reduce size).
        float speedMultiplier = 1f;
        if (Keyboard.current != null && (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed))
        {
            speedMultiplier = _ctrlZoomMultiplier;
        }
        float change = -normalizedDelta * _zoomSpeed * 0.1f * speedMultiplier;
        
        float newTargetOrthoSize = Mathf.Clamp(_targetOrthoSize + change, minZoomSize, maxDezoomSize);

        // Calculate target camera position to keep the cursor at the same screen viewport position
        float sizeOld = _camera.orthographicSize;
        if (sizeOld < 0.001f) sizeOld = 0.001f;

        Vector3 cameraPos = transform.position;
        Vector3 dir = cursorWorldPos - cameraPos;
        Vector3 targetCameraPos = cursorWorldPos - dir * (newTargetOrthoSize / sizeOld);

        // Clamp the calculated target position within GameZone boundaries
        Vector3 clampedTargetPos = ClampCameraPosition(targetCameraPos, newTargetOrthoSize);

        _targetOrthoSize = newTargetOrthoSize;

        // Smoothly animate the transition using DOTween (both orthographic size and position)
        DOTween.Kill(_camera);
        DOTween.Kill(transform);

        _camera.DOOrthoSize(_targetOrthoSize, _smoothTime)
            .SetEase(Ease.OutQuad)
            .SetTarget(_camera);

        transform.DOMove(clampedTargetPos, _smoothTime)
            .SetEase(Ease.OutQuad)
            .SetTarget(transform);
    }

    /// <summary>
    /// Handles camera panning using middle-click drag.
    /// </summary>
    private void HandlePanning()
    {
        if (Mouse.current == null) return;

        // Start drag
        if (Mouse.current.middleButton.wasPressedThisFrame)
        {
            _isPanning = true;
            _dragStartMousePos = Mouse.current.position.ReadValue();
            _dragStartCameraPos = transform.position;
        }

        // Drag update
        if (_isPanning && Mouse.current.middleButton.isPressed)
        {
            Vector2 currentMousePos = Mouse.current.position.ReadValue();
            Vector2 mouseDelta = currentMousePos - _dragStartMousePos;

            // Calculate world offset based on viewport and orthographic size
            float worldOffsetX = (mouseDelta.x / Screen.width) * (_camera.orthographicSize * 2f * _camera.aspect);
            float worldOffsetY = (mouseDelta.y / Screen.height) * (_camera.orthographicSize * 2f);

            Vector3 targetPos = _dragStartCameraPos - new Vector3(worldOffsetX, worldOffsetY, 0f);
            transform.position = ClampCameraPosition(targetPos);
        }

        // End drag
        if (Mouse.current.middleButton.wasReleasedThisFrame)
        {
            _isPanning = false;
        }
    }

    /// <summary>
    /// Clamps the camera position so that the viewport remains completely within the GameZone boundaries.
    /// </summary>
    private Vector3 ClampCameraPosition(Vector3 position)
    {
        return ClampCameraPosition(position, _camera != null ? _camera.orthographicSize : 5f);
    }

    /// <summary>
    /// Clamps the camera position so that the viewport remains completely within the GameZone boundaries for a given orthographic size.
    /// </summary>
    private Vector3 ClampCameraPosition(Vector3 position, float orthoSize)
    {
        if (GameZone.Instance == null || _camera == null) return position;

        float aspect = _camera.aspect;

        float minX = GameZone.Instance.MinX + orthoSize * aspect;
        float maxX = GameZone.Instance.MaxX - orthoSize * aspect;
        float minY = GameZone.Instance.MinY + orthoSize;
        float maxY = GameZone.Instance.MaxY - orthoSize;

        // If the viewport is larger than the game zone in any dimension (safety check),
        // clamp to the center of the zone in that dimension.
        if (minX > maxX)
        {
            float centerX = (GameZone.Instance.MinX + GameZone.Instance.MaxX) / 2f;
            minX = centerX;
            maxX = centerX;
        }
        if (minY > maxY)
        {
            float centerY = (GameZone.Instance.MinY + GameZone.Instance.MaxY) / 2f;
            minY = centerY;
            maxY = centerY;
        }

        float clampedX = Mathf.Clamp(position.x, minX, maxX);
        float clampedY = Mathf.Clamp(position.y, minY, maxY);

        return new Vector3(clampedX, clampedY, position.z);
    }

    /// <summary>
    /// Calculates the maximum orthographic size required to fully show the GameZone inside the viewport.
    /// </summary>
    private float GetMaxDezoomSize()
    {
        if (GameZone.Instance == null) return 10f; // Default fallback

        float zoneWidth = GameZone.Instance.Width;
        float zoneHeight = GameZone.Instance.Height;
        float aspect = _camera.aspect;

        // Height constraint: camera half-height must cover half of the zone height
        float limitHeight = zoneHeight / 2f;

        // Width constraint: camera half-width (orthoSize * aspect) must cover half of the zone width
        float limitWidth = zoneWidth / (2f * aspect);

        // Take the maximum of both constraints so the entire zone is visible
        return Mathf.Max(limitHeight, limitWidth);
    }

    /// <summary>
    /// Gets the maximum zoom size (smallest orthographic size allowed).
    /// </summary>
    public float MaxZoomSize => _maxZoomSize;

    /// <summary>
    /// Gets the maximum dezoom size (largest orthographic size allowed based on GameZone).
    /// </summary>
    public float MaxDezoomSize => GetMaxDezoomSize();

    /// <summary>
    /// Gets the current orthographic size of the camera.
    /// </summary>
    public float CurrentOrthoSize => _camera != null ? _camera.orthographicSize : _targetOrthoSize;

    /// <summary>
    /// Smoothly forces the camera orthographic size to a target value.
    /// Can configure ease parameters (like amplitude and period for elastic ease).
    /// </summary>
    public void AnimateOrthoSize(float targetSize, float duration, Ease ease = Ease.OutQuad, float easeAmplitude = -1f, float easePeriod = -1f)
    {
        if (_camera == null) return;

        _targetOrthoSize = targetSize;
        DOTween.Kill(_camera);

        var tween = _camera.DOOrthoSize(_targetOrthoSize, duration)
            .SetEase(ease)
            .SetTarget(_camera);

        if (easeAmplitude >= 0f && easePeriod >= 0f)
        {
            tween.SetEase(ease, easeAmplitude, easePeriod);
        }
    }
}
