using DG.Tweening;
using UnityEngine;

/// <summary>
/// Handles the squishy jelly-like bounce feedback upon collision.
/// Separated from BallEntity for cleaner code structure.
/// </summary>
public class BallJellyBounce : MonoBehaviour
{
    [Header("Jelly Collision Settings")]
    [SerializeField] private float _minCollisionVelocity = 1f;
    [SerializeField] private float _maxCollisionVelocity = 15f;
    [SerializeField] private float _jellyPunchMultiplier = 0.03f;
    [SerializeField] private float _maxJellyPunch = 0.4f;
    [SerializeField] private float _jellyDurationMultiplier = 0.05f;
    [SerializeField] private float _minJellyDuration = 0.3f;
    [SerializeField] private float _maxJellyDuration = 1.0f;

    [Header("Simple Mode Settings")]
    [SerializeField] private int _jellyVibrato = 6;
    [SerializeField] private float _jellyElasticity = 1f;

    [Header("Optimization")]
    [SerializeField] private float _collisionCooldown = 0.05f;

    private float _lastCollisionTime = 0f;
    private Tween _jellyTween;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (Time.time - _lastCollisionTime < _collisionCooldown) return;

        float impact = collision.relativeVelocity.magnitude;
        if (impact >= _minCollisionVelocity)
        {
            _lastCollisionTime = Time.time;


            ApplyJellyBounceSimple(impact, collision.GetContact(0).normal);
        }
    }

    private void ApplyJellyBounceSimple(float impact, Vector2 normal)
    {
        // Aligne l'écrasement sur la normale du choc
        if (normal != Vector2.zero) transform.up = normal;

        float clampedImpact = Mathf.Clamp(impact, _minCollisionVelocity, _maxCollisionVelocity);


        float intensity = Mathf.Clamp(clampedImpact * _jellyPunchMultiplier, 0f, _maxJellyPunch);
        float duration = Mathf.Clamp(clampedImpact * _jellyDurationMultiplier, _minJellyDuration, _maxJellyDuration);

        // Si une animation joue déjà, on la snap à la fin pour éviter que le scale n'explose
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
    }

    private void OnDisable()
    {
        ResetJellyState();
    }
}
