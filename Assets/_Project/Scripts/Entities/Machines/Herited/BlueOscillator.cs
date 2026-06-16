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

    [Header("Energy Settings")]
    [SerializeField] private float _inputTransferSpeed = 5f;
    [SerializeField] private float _consumptionPerAction = 10f;

    private BallCaptureHandler _captureHandler;
    private bool _isTransforming;

    public float InputTransferSpeed => _inputTransferSpeed;
    public float ConsumptionPerAction => _consumptionPerAction;

    protected override void Start()
    {
        base.Start();
        _captureHandler = GetComponent<BallCaptureHandler>();
        if (_targetCenterTransform == null)
        {
            _targetCenterTransform = transform;
        }
    }

    public override bool OnDragStart()
    {
        // Prevent drag if we have a ball inside or are performing a transformation
        if (_captureHandler != null && _captureHandler.CapturedBall != null)
        {
            return false;
        }
        return base.OnDragStart();
    }

    private void FixedUpdate()
    {
        if (IsBeingDragged || !_isRunning) return;

        // If we have a captured ball, check if we can start the transformation
        if (_captureHandler != null && _captureHandler.CapturedBall != null && !_isTransforming)
        {
            // Verify if the captured ball is indeed a red ball
            if (_captureHandler.CapturedBall.Data == _redBallData)
            {
                if (CurrentEnergy >= _consumptionPerAction)
                {
                    StartTransformation();
                }
            }
        }
    }

    private void StartTransformation()
    {
        _isTransforming = true;

        // Deduct energy
        CurrentEnergy = Mathf.Max(0f, CurrentEnergy - _consumptionPerAction);

        var redBall = _captureHandler.CapturedBall;
        Vector3 centerPos = _targetCenterTransform.position;

        float squeezeDur = _transformationDuration * 0.2f;
        float shrinkDur = _transformationDuration * 0.3f;
        float expandDur = _transformationDuration * 0.5f;

        // Sequence: Scale down the red ball, swap it with blue ball at scale 0, scale it up, then eject it!
        Sequence transformSeq = DOTween.Sequence();
        
        // Squeeze/squash slightly, then shrink to 0
        transformSeq.Append(redBall.transform.DOScale(new Vector3(1.2f, 0.8f, 1f), squeezeDur).SetEase(Ease.OutQuad));
        transformSeq.Append(redBall.transform.DOScale(Vector3.zero, shrinkDur).SetEase(Ease.InBack));
        
        transformSeq.OnComplete(() =>
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
                        _isTransforming = false;
                    });
            }
            else
            {
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
            if (ball.Data == _redBallData && _captureHandler.CapturedBall == null && !_isTransforming)
            {
                _captureHandler.Capture(ball, _targetCenterTransform.position);
            }
        }
    }

    protected override void OnTickExecuted()
    {
        // Synchronized tick callback if needed
    }
}
