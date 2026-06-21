using System;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Behavior for the White Ball.
/// Expands the scale of the playfield when it receives its maximum clicks (which triggers OnDuplicate).
/// </summary>
[Serializable]
public class WhiteBallBehavior : BallBehavior
{
    [SerializeField] private float _expansionDuration = 1.5f;

    [Header("Camera Animation Settings")]
    [Tooltip("The Ease type used for the camera zoom out animation on explosion.")]
    [SerializeField] private Ease _cameraEaseType = Ease.OutElastic;

    [Tooltip("The bounce strength/overshoot of the elastic camera animation (if using an elastic ease).")]
    [SerializeField] private float _cameraElasticAmplitude = 1.0f;

    [Tooltip("The frequency of bounces in the elastic camera animation (if using an elastic ease).")]
    [SerializeField] private float _cameraElasticPeriod = 0.3f;

    public override void ExecuteFixedUpdate(BallEntity ball, float fixedDeltaTime)
    {
        if (ball.IsBeingDragged || ball.IsProcessing) return;
        // No special physics behavior required
    }

    public override void OnDuplicate(BallEntity ball)
    {
        if (GameZone.Instance != null)
        {
            // Expand by 25% (1.25x) instead of doubling (2.0x)
            GameZone.Instance.ExpandScale(1.25f, _expansionDuration);

            // Expel the camera at the same speed/duration to fit the new GameZone size
            if (CameraController.Instance != null)
            {
                float targetOrtho = CameraController.Instance.MaxDezoomSize * 1.25f;
                CameraController.Instance.AnimateOrthoSize(targetOrtho, _expansionDuration, _cameraEaseType, _cameraElasticAmplitude, _cameraElasticPeriod);
            }
        }
        else
        {
            Debug.LogWarning("[WhiteBallBehavior] GameZone.Instance is missing!");
        }

        // Lock the ball physics, disable its collider, and start the expansion animation
        ball.IsProcessing = true;
        
        if (ball.Collider != null)
        {
            ball.Collider.enabled = false;
        }

        if (ball.Rb != null)
        {
            ball.Rb.bodyType = RigidbodyType2D.Kinematic;
            ball.Rb.linearVelocity = Vector2.zero;
            ball.Rb.angularVelocity = 0f;
        }

        float duration = _expansionDuration;
        
        // Cache start values for restoring before recycling
        Color startColorInner = ball.Renderer.ColorInner;
        Color startColorOuter = ball.Renderer.ColorOuter;

        // Tween scale to grow "to infinity" (very large value)
        ball.transform.DOScale(Vector3.one * 150f, duration).SetEase(Ease.OutQuad);

        // Tween colors to fade out (alpha -> 0)
        DOTween.To(() => ball.Renderer.ColorInner, x => ball.Renderer.ColorInner = x, new Color(startColorInner.r, startColorInner.g, startColorInner.b, 0f), duration).SetEase(Ease.OutQuad);
        DOTween.To(() => ball.Renderer.ColorOuter, x => ball.Renderer.ColorOuter = x, new Color(startColorOuter.r, startColorOuter.g, startColorOuter.b, 0f), duration).SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                // Reset scale and colors so the ball is clean inside the pool
                ball.transform.localScale = Vector3.one;
                ball.Renderer.ColorInner = startColorInner;
                ball.Renderer.ColorOuter = startColorOuter;

                // Release/recycle the white ball back to the pool
                if (BallPoolManager.Instance != null)
                {
                    BallPoolManager.Instance.ReleaseBall(ball);
                }
                else
                {
                    Destroy(ball.gameObject);
                }
            });
    }
}
