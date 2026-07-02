using System.Collections;
using UnityEngine;
using DG.Tweening;

public class NightmareBall : BallBehavior
{
    [Header("Spooky Settings")]
    [SerializeField] private float _baseScaleMultiplier = 1.2f;

    private int _clicks = 0;
    private bool _isDeleting = false;
    private Vector3 _originalScale;
    private BallEntity _ball;
    private Coroutine _timerCoroutine;

    public override void Initialize(BallEntity ball)
    {
        _ball = ball;
        _originalScale = ball.transform.localScale;
        if (_originalScale == Vector3.zero)
        {
            _originalScale = Vector3.one * _baseScaleMultiplier;
        }

        // Dragging is blocked natively in BallEntity, keep IsProcessing false so it is clickable
        ball.IsProcessing = false;

        // Force Kinematic immediately so it is unpushable by other balls
        if (ball.Rb != null)
        {
            ball.Rb.bodyType = RigidbodyType2D.Kinematic;
            ball.Rb.linearVelocity = Vector2.zero;
            ball.Rb.angularVelocity = 0f;
        }

        // Spooky elastic entry animation
        ball.transform.localScale = Vector3.zero;
        ball.transform.DOScale(_originalScale, 1.2f)
            .SetEase(Ease.OutElastic)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                // Start creepy breathing scale pulse using DOTween loop only when entry completes
                StartBreathingLoop(1f);
            });

        ball.transform.DOShakePosition(1f, 0.2f, 15).SetUpdate(true);

        if (Camera.main != null)
        {
            Camera.main.transform.DOKill();
            Camera.main.transform.DOShakePosition(0.8f, 0.3f, 12).SetUpdate(true);
        }

        // Start the 10-second expiration timer in real time
        ResetExpirationTimer();
    }

    private void StartBreathingLoop(float speedMultiplier)
    {
        if (_ball == null || _isDeleting) return;

        float duration = 1.5f / speedMultiplier;
        float minScale = 0.96f;
        float maxScale = 1.04f + (_clicks * 0.015f);

        // Bound the breathing loop so it only oscillates close to original scale
        _ball.transform.localScale = _originalScale * minScale;

        _ball.transform.DOScale(_originalScale * maxScale, duration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true)
            .SetId(this);
    }

    private void ResetExpirationTimer()
    {
        if (_timerCoroutine != null)
        {
            StopCoroutine(_timerCoroutine);
        }
        if (!_isDeleting)
        {
            _timerCoroutine = StartCoroutine(AutoExpirationTimer());
        }
    }

    private IEnumerator AutoExpirationTimer()
    {
        // 10 seconds of absolute real-time waiting
        yield return new WaitForSecondsRealtime(10f);

        _isDeleting = true;
        _ball.transform.DOKill();
        
        // Disable physics interactions during exit transition
        if (_ball.Collider != null) _ball.Collider.enabled = false;
        if (_ball.Rb != null) _ball.Rb.linearVelocity = Vector2.zero;

        // Visual build-up before pop-out: shake violently and swell
        _ball.transform.DOShakePosition(0.4f, 0.25f, 30).SetUpdate(true);
        _ball.transform.DOScale(_originalScale * 1.35f, 0.4f)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);

        yield return new WaitForSecondsRealtime(0.4f);

        // Implosion: spin fast and contract to 0
        _ball.transform.DORotate(new Vector3(0, 0, 720f), 0.35f, RotateMode.FastBeyond360)
            .SetEase(Ease.InBack)
            .SetUpdate(true);
            
        _ball.transform.DOScale(Vector3.zero, 0.35f)
            .SetEase(Ease.InBack)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                // Restore the black hole with a cool shockwave rebound!
                if (SaveManager.Instance != null)
                {
                    SaveManager.Instance.RestoreBlackHole();
                }
                
                Destroy(_ball.gameObject);
            });
    }

    public override void OnClick(BallEntity ball)
    {
        if (_isDeleting) return;

        _clicks++;

        // Trigger monologue on first click
        if (_clicks == 1)
        {
            if (MonologueManager.Instance != null)
            {
                MonologueManager.Instance.TriggerMonologueDirect("Are you sure you want to do that...", 3f);
            }
        }
        // Trigger monologue on 10th click
        else if (_clicks == 10)
        {
            if (MonologueManager.Instance != null)
            {
                MonologueManager.Instance.TriggerMonologueDirect("You will end up doing it again, you can be sure of that...", 4f);
            }
        }

        // Reset the 10-second timer so it starts over at 0
        ResetExpirationTimer();

        // Temporarily stop the breathing loop to perform the click feedback
        _ball.transform.DOKill();

        // Punch scale and shake position
        _ball.transform.DOShakePosition(0.2f, 0.15f * _clicks, 20).SetUpdate(true);
        _ball.transform.DOPunchScale(Vector3.one * 0.25f, 0.15f)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                // Restart breathing loop with higher frequency and larger amplitude
                if (!_isDeleting && _ball != null)
                {
                    StartBreathingLoop(1f + _clicks * 0.12f);
                }
            });

        // Make it progressively redder/darker
        if (ball.Renderer != null)
        {
            float redRatio = (float)_clicks / 20f;
            Color baseColor = ball.Data != null ? ball.Data.color : Color.black;
            ball.Renderer.ColorOuter = Color.Lerp(baseColor, new Color(0.7f, 0f, 0f, 1f), redRatio);
            ball.Renderer.ColorInner = Color.Lerp(baseColor * 0.7f, new Color(0.3f, 0f, 0f, 1f), redRatio);
        }

        // Camera shake matching click count
        if (Camera.main != null)
        {
            Camera.main.transform.DOKill();
            Camera.main.transform.DOShakePosition(0.15f, 0.05f * _clicks, 25).SetUpdate(true);
        }

        if (_clicks >= 20)
        {
            _isDeleting = true;
            if (_timerCoroutine != null)
            {
                StopCoroutine(_timerCoroutine);
            }
            _ball.transform.DOKill();
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.StartNightmareDeleteSequence(_ball, _originalScale);
            }
        }
    }

    public override void OnDuplicate(BallEntity ball)
    {
        // Disable duplication for the Nightmare Ball
    }

    public override void ExecuteFixedUpdate(BallEntity ball, float fixedDeltaTime)
    {
        if (ball == null || _isDeleting) return;

        if (ball.Rb != null)
        {
            if (ball.Rb.bodyType != RigidbodyType2D.Kinematic)
            {
                ball.Rb.bodyType = RigidbodyType2D.Kinematic;
            }
            ball.Rb.linearVelocity = Vector2.zero;
            ball.Rb.angularVelocity = 0f;
        }
    }
}
