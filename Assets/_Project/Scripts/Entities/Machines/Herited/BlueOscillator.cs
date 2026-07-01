using DG.Tweening;
using UnityEngine;

/// <summary>
/// Machine that captures a Red ball and converts it into a Blue ball, then ejects it.
/// </summary>
[RequireComponent(typeof(BallCaptureHandler))]
public class BlueOscillator : MachineEntity, IEnergyConsumer
{
    [Header("Oscillator Settings")]
    [SerializeField] private BallDataSO _redBallData;
    [SerializeField] private BallDataSO _blueBallData;
    [SerializeField] private Transform _targetCenterTransform;
    [SerializeField] private float _ejectionForce = 6.0f;
    [SerializeField] private float _transformationDuration = 1.0f;

    [Header("Animation Settings")]
    [SerializeField] private Animator _animator;
    [SerializeField] private string _convertTriggerName = "Convert";
    [SerializeField] private float _animationSpeed = 1.0f;

    [Header("Energy Settings")]
    [SerializeField] private float _inputTransferSpeed = 1.0f;
    [SerializeField] private float _consumptionPerAction = 20f;

    private BallCaptureHandler _captureHandler;
    private bool _isTransforming;
    private Sequence _transformSeq;

    public override Vector3 CapturePosition => _targetCenterTransform != null ? _targetCenterTransform.position : transform.position;

    public float InputTransferSpeed
    {
        get
        {
            if (CurrentEnergy >= MaxStorage - 0.0001f) return 0f;
            return _inputTransferSpeed;
        }
    }
    public float ConsumptionPerAction => _consumptionPerAction;
    public override bool IsDemanding => true;

    protected override void Start()
    {
        base.Start();
        if (_maxStorage == 100f)
        {
            _maxStorage = 20f;
        }
        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
        }
        _captureHandler = GetComponent<BallCaptureHandler>();
        if (_targetCenterTransform == null)
        {
            _targetCenterTransform = transform;
        }
    }

    public override void ReleaseCapturedBalls()
    {
        if (_isTransforming)
        {
            if (_transformSeq != null && _transformSeq.IsActive())
            {
                _transformSeq.Kill();
            }

            if (_captureHandler != null && _captureHandler.CapturedBall != null)
            {
                DOTween.Kill(_captureHandler.CapturedBall.transform);
                _captureHandler.CapturedBall.transform.localScale = Vector3.one;
            }

            _isTransforming = false;
            if (_animator != null)
            {
                _animator.speed = 1f;
            }
        }
        base.ReleaseCapturedBalls();
    }

    public override bool OnDragStart()
    {
        // Prevent drag if we are performing a transformation
        if (_isTransforming)
        {
            return false;
        }
        return base.OnDragStart();
    }

    private void StartTransformation()
    {
        _isTransforming = true;

        // Deduct energy
        CurrentEnergy = Mathf.Max(0f, CurrentEnergy - _consumptionPerAction);

        if (_animator != null)
        {
            _animator.speed = _animationSpeed * NetworkEfficiency;
            _animator.SetTrigger(_convertTriggerName);
        }

        var redBall = _captureHandler.CapturedBall;
        Vector3 centerPos = _targetCenterTransform.position;

        if (redBall != null)
        {
            redBall.transform.position = centerPos;
            if (redBall.Rb != null)
            {
                redBall.Rb.linearVelocity = Vector2.zero;
            }
        }

        float efficiency = NetworkEfficiency;

        float squeezeDur = (_transformationDuration * 0.2f) / efficiency;
        float shrinkDur = (_transformationDuration * 0.3f) / efficiency;
        float expandDur = (_transformationDuration * 0.5f) / efficiency;

        // Sequence: Scale down the red ball, swap it with blue ball at scale 0, scale it up, then eject it!
        _transformSeq = DOTween.Sequence();
        _transformSeq.SetLink(gameObject);
        
        // Squeeze/squash slightly, then shrink to 0
        _transformSeq.Append(redBall.transform.DOScale(new Vector3(1.2f, 0.8f, 1f), squeezeDur).SetEase(Ease.OutQuad));
        _transformSeq.Append(redBall.transform.DOScale(Vector3.zero, shrinkDur).SetEase(Ease.InBack));
        
        _transformSeq.OnComplete(() =>
        {
            Vector2 savedEntryDir = _captureHandler.EntryDirection;

            // Release the red ball to the pool
            BallPoolManager.Instance.ReleaseBall(redBall);
            _captureHandler.ClearReference(); // Clear reference in the handler to allow new capture

            // Spawn the blue ball at the center position
            BallEntity blueBall = BallPoolManager.Instance.SpawnBall(_blueBallData, centerPos);
            if (blueBall != null)
            {
                // Start blue ball at scale 0
                blueBall.transform.localScale = Vector3.zero;

                // Capture the blue ball instantly (which locks it and disables collider)
                _captureHandler.Capture(blueBall, centerPos);

                // Restore the entry direction so it is ejected in the correct direction
                _captureHandler.EntryDirection = savedEntryDir;

                // Scale it up
                blueBall.transform.DOScale(Vector3.one, expandDur)
                    .SetEase(Ease.OutBack)
                    .OnComplete(() =>
                    {
                        // Eject the blue ball
                        _captureHandler.EjectCapturedBall(_ejectionForce);
                        if (_animator != null)
                        {
                            _animator.speed = 1f;
                        }
                        _isTransforming = false;
                    });
            }
            else
            {
                if (_animator != null)
                {
                    _animator.speed = 1f;
                }
                _isTransforming = false;
            }
        });
    }

    public override void OnPartTriggerEnter(string partId, Collider2D collider)
    {
        if (IsBeingDragged || !_isRunning) return;

        // Capture only when entering the CENTER trigger
        if (partId == "CENTER" && collider.gameObject.TryGetComponent(out BallEntity ball))
        {
            // Forbid Yellow Balls
            if ((ball.Data != null && ball.Data.id == "YellowBall") || ball.Behavior is YellowBallBehavior)
            {
                return;
            }

            if (ball.Data == _redBallData && _captureHandler.CapturedBall == null && !_isTransforming)
            {
                _captureHandler.Capture(ball, _targetCenterTransform.position);
            }
        }
    }

    public override void OnPartTriggerStay(string partId, Collider2D collider)
    {
        OnPartTriggerEnter(partId, collider);
    }

    protected override void OnTickExecuted()
    {
        if (IsBeingDragged || !_isRunning) return;

        // Synchronized action check on global tick
        if (_captureHandler != null && _captureHandler.CapturedBall != null && !_isTransforming && !_captureHandler.IsCentering)
        {
            if (_captureHandler.CapturedBall.Data == _redBallData)
            {
                if (CurrentEnergy >= _consumptionPerAction - 0.001f)
                {
                    StartTransformation();
                }
            }
        }
    }
}
