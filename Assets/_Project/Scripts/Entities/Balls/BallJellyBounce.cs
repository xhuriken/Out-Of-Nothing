using DG.Tweening;
using UnityEngine;

/// <summary>
/// Handles the squishy jelly-like bounce feedback upon collision.
/// Separated from BallEntity for cleaner code structure.
/// </summary>
public class BallJellyBounce : MonoBehaviour
{
    [Header("Jelly Collision Settings")]
    [SerializeField] private float _minCollisionVelocity = 0.1f;
    [SerializeField] private float _maxCollisionVelocity = 15f;

    [Header("Punch Intensity")]
    [SerializeField] private float _minJellyPunch = 0.001f;
    [SerializeField] private float _maxJellyPunch = 0.25f;

    [Header("Punch Duration")]
    [SerializeField] private float _minJellyDuration = 0.05f;
    [SerializeField] private float _maxJellyDuration = 1f;

    [Header("Simple Mode Settings")]
    [SerializeField] private int _jellyVibrato = 6;
    [SerializeField] private float _jellyElasticity = 2f;

    [Header("Optimization")]
    [SerializeField] private float _collisionCooldown = 0.05f;

    private float _lastCollisionTime = 0f;
    private Tween _jellyTween;

    // Tracks the total intensity of all running additive sequences
    private float _activeIntensitySum = 0f;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (Time.time - _lastCollisionTime < _collisionCooldown) return;

        float impact = collision.relativeVelocity.magnitude;
        if (impact >= _minCollisionVelocity)
        {
            _lastCollisionTime = Time.time;

            // TEST : Change this call to compare the Additive version and the Simple version
            ApplyJellyBounceAdditive(impact, collision.GetContact(0).normal);
            // ApplyJellyBounceSimple(impact, collision.GetContact(0).normal);
        }
    }

    /// <summary>
    /// Smoothly adds forces together without snapping the animation.
    /// Caps the total combined deformation to avoid exploding the ball's size.
    /// </summary>
    private void ApplyJellyBounceAdditive(float impact, Vector2 normal)
    {
        // 1. Align the Y axis to the collision normal.
        if (normal != Vector2.zero)
        {
            transform.up = normal;
        }

        // 2. Proportionally map the impact to intensity and duration
        float t = Mathf.InverseLerp(_minCollisionVelocity, _maxCollisionVelocity, impact);
        float rawIntensity = Mathf.Lerp(_minJellyPunch, _maxJellyPunch, t);
        float duration = Mathf.Lerp(_minJellyDuration, _maxJellyDuration, t);

        // 3. Strict Clamp: Ensure the SUM of all running intensities NEVER exceeds _maxJellyPunch
        float allowedIntensity = _maxJellyPunch - _activeIntensitySum;
        if (allowedIntensity <= 0.001f) return; // Cap reached, ignore this shock

        float finalIntensity = Mathf.Min(rawIntensity, allowedIntensity);
        _activeIntensitySum += finalIntensity;

        // 4. Create the squash vector (Stretch X, Squash Y)
        Vector3 squash = new Vector3(finalIntensity, -finalIntensity, 0f);

        // 5. Truly Additive Animation using DOBlendableScaleBy
        Sequence jellySeq = DOTween.Sequence();
        jellySeq.SetTarget(this); // Links sequence to this object for DOTween.Kill(this)

        jellySeq.Append(transform.DOBlendableScaleBy(squash, duration * 0.2f).SetEase(Ease.OutSine))
                .Append(transform.DOBlendableScaleBy(-squash * 1.2f, duration * 0.4f).SetEase(Ease.InOutSine))
                .Append(transform.DOBlendableScaleBy(squash * 0.5f, duration * 0.3f).SetEase(Ease.InOutSine))
                .Append(transform.DOBlendableScaleBy(-squash * 0.3f, duration * 0.1f).SetEase(Ease.InSine))
                .OnComplete(() => _activeIntensitySum -= finalIntensity);
    }

    /// <summary>
    /// Uses DOPunchScale. Snaps the animation back to 1 if a new shock happens,
    /// avoiding overlap but causing a visible reset.
    /// </summary>
    private void ApplyJellyBounceSimple(float impact, Vector2 normal)
    {
        if (normal != Vector2.zero) transform.up = normal;

        float t = Mathf.InverseLerp(_minCollisionVelocity, _maxCollisionVelocity, impact);
        float intensity = Mathf.Lerp(_minJellyPunch, _maxJellyPunch, t);
        float duration = Mathf.Lerp(_minJellyDuration, _maxJellyDuration, t);

        // Snap back to base scale if currently playing to prevent scale explosion
        if (_jellyTween != null && _jellyTween.IsActive())
        {
            _jellyTween.Complete(true);
        }

        Vector3 squash = new Vector3(intensity, -intensity, 0f);
        _jellyTween = transform.DOPunchScale(squash, duration, _jellyVibrato, _jellyElasticity);
    }

    public void ResetJellyState()
    {
        DOTween.Kill(this);
        if (_jellyTween != null) _jellyTween.Kill();
        _activeIntensitySum = 0f;
    }

    private void OnDisable()
    {
        ResetJellyState();
    }
}
