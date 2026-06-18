using DG.Tweening;
using System.Collections;
using UnityEngine;

/// <summary>
/// Machine that captures any ball, consumes energy to click it once,
/// and then ejects it. If the ball duplicates, it waits for mitosis
/// and ejects the parent ball normally and the child ball downwards.
/// </summary>
[RequireComponent(typeof(BallCaptureHandler))]
public class ClickerMachine : MachineEntity, IEnergyConsumer
{
    [Header("Clicker Settings")]
    [SerializeField] private Transform _targetCenterTransform;
    [SerializeField] private float _ejectionForce = 6.0f;

    [Header("Energy Settings")]
    [SerializeField] private float _inputTransferSpeed = 0.5f;
    [SerializeField] private float _consumptionPerAction = 10f;

    [Header("Animation Settings")]
    [SerializeField] private Animator _animator;

    private BallCaptureHandler _captureHandler;
    private bool _isProcessingAction;

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
            _maxStorage = 10f;
        }
        _captureHandler = GetComponent<BallCaptureHandler>();
        if (_targetCenterTransform == null)
        {
            _targetCenterTransform = transform;
        }
        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
        }
    }

    public override bool OnDragStart()
    {
        // Prevent drag if we have a ball inside or are processing
        if ((_captureHandler != null && _captureHandler.CapturedBall != null) || _isProcessingAction)
        {
            return false;
        }
        return base.OnDragStart();
    }

    public override void OnPartTriggerEnter(string partId, Collider2D collider)
    {
        if (IsBeingDragged || !_isRunning) return;

        // Capture any BallEntity when entering the CENTER trigger
        if (partId == "CENTER" && collider.gameObject.TryGetComponent(out BallEntity ball))
        {
            if (_captureHandler.CapturedBall == null && !_isProcessingAction)
            {
                _captureHandler.Capture(ball, _targetCenterTransform.position);
            }
        }
    }

    protected override void OnTickExecuted()
    {
        if (IsBeingDragged || !_isRunning) return;

        // Action check on global tick when a ball is captured
        if (_captureHandler != null && _captureHandler.CapturedBall != null && !_isProcessingAction)
        {
            if (CurrentEnergy >= _consumptionPerAction - 0.0001f)
            {
                StartAction();
            }
        }
    }

    private void StartAction()
    {
        // Deduct energy
        CurrentEnergy = Mathf.Max(0f, CurrentEnergy - _consumptionPerAction);

        BallEntity ball = _captureHandler.CapturedBall;
        StartCoroutine(PerformClickerSequence(ball));
    }

    private IEnumerator PerformClickerSequence(BallEntity ball)
    {
        _isProcessingAction = true;

        float efficiency = NetworkEfficiency;

        // 1. Wait 1 second after entering and centering before the click happens, scaled by efficiency
        yield return new WaitForSeconds(1.0f / efficiency);

        // Determine if this click will trigger duplication (mitosis)
        bool willDuplicate = (ball.CurrentClickCount + 1 >= ball.Data.clicksRequiredForDuplication);

        if (_animator != null)
        {
            _animator.speed = efficiency;
            _animator.SetTrigger("Click");
        }

        if (willDuplicate)
        {
            // Reset click count on parent
            ball.CurrentClickCount = 0;

            // Trigger click visual
            ball.PerformDefaultClick();

            // 2. Wait 1 second after clicking before starting the duplication visual mitosis, scaled by efficiency
            yield return new WaitForSeconds(1.0f / efficiency);

            // Execute duplication
            yield return StartCoroutine(PerformClickerDuplication(ball));
        }
        else
        {
            // Trigger single click
            ball.IsProcessing = false;
            ball.ReceiveClick();
            ball.IsProcessing = true;

            // 2. Wait 1 second after clicking before ejecting, scaled by efficiency
            yield return new WaitForSeconds(1.0f / efficiency);

            // Expel parent ball in opposite direction of entry
            _captureHandler.EjectCapturedBall(_ejectionForce);
        }

        if (_animator != null)
        {
            _animator.speed = 1f; // Reset animator speed
        }

        _isProcessingAction = false;
    }

    private IEnumerator PerformClickerDuplication(BallEntity parentBall)
    {
        Vector3 centerPos = _targetCenterTransform.position;
        float efficiency = NetworkEfficiency;

        // Force parent to be exactly at center
        parentBall.transform.position = centerPos;

        // Squash and stretch scale preparation for duplication (mitosis feel), scaled by efficiency
        Sequence prepSeq = DOTween.Sequence();
        prepSeq.Append(parentBall.transform.DOScale(new Vector3(1.4f, 0.6f, 1f), 0.35f / efficiency).SetEase(Ease.OutQuad));
        prepSeq.Join(parentBall.transform.DOShakePosition(0.35f / efficiency, 0.08f, 30, 90f, false, false));
        yield return prepSeq.WaitForCompletion();

        // Ensure parent returns to center after shake
        parentBall.transform.position = centerPos;

        // Spawn child ball
        BallEntity childBall = BallPoolManager.Instance.SpawnBall(parentBall.Data, centerPos);
        if (childBall != null)
        {
            // Set up child ball in kinematic/non-interactive state during animation
            childBall.transform.position = centerPos;
            childBall.transform.localScale = Vector3.zero;
            childBall.IsProcessing = true;
            if (childBall.Passport != null)
            {
                childBall.Passport.SetLockState(true);
            }
            if (childBall.Collider != null)
            {
                childBall.Collider.enabled = false;
            }

            // Animate both parent and child recovering to normal scale (split feel), scaled by efficiency
            Sequence splitSeq = DOTween.Sequence();
            splitSeq.Append(parentBall.transform.DOScale(Vector3.one, 0.45f / efficiency).SetEase(Ease.OutElastic));
            splitSeq.Join(childBall.transform.DOScale(Vector3.one, 0.45f / efficiency).SetEase(Ease.OutElastic));
            yield return splitSeq.WaitForCompletion();

            // Ignore collision between parent and child ball temporarily so overlapping resolution doesn't deflect them sideways
            if (parentBall.Collider != null && childBall.Collider != null)
            {
                Physics2D.IgnoreCollision(parentBall.Collider, childBall.Collider, true);
            }

            // Expel parent ball in opposite direction of entry (handled by capture handler)
            _captureHandler.EjectCapturedBall(_ejectionForce);

            // Expel child ball downwards
            childBall.IsProcessing = false;
            if (childBall.Passport != null)
            {
                childBall.Passport.SetLockState(false);
            }
            if (childBall.Collider != null)
            {
                childBall.Collider.enabled = true;
            }

            // Downward direction relative to the machine (pointing along -transform.up)
            Vector2 childEjectDir = -transform.up;
            childBall.transform.position = centerPos + (Vector3)childEjectDir * 1.2f;

            // Ignore collision with machine colliders for a short duration
            _captureHandler.IgnoreCollisionWithMachine(childBall.Collider, true);

            float massMultiplier = childBall.SetTemporaryHeavyMass(0.4f / efficiency, 50f);
            if (childBall.Rb != null)
            {
                childBall.Rb.linearVelocity = Vector2.zero;
                childBall.Rb.AddForce(childEjectDir * (_ejectionForce * massMultiplier), ForceMode2D.Impulse);
            }

            // Restore collisions after a short delay
            BallEntity savedParent = parentBall;
            BallEntity savedChild = childBall;
            DOVirtual.DelayedCall(0.5f / efficiency, () =>
            {
                if (savedChild != null && _captureHandler != null)
                {
                    _captureHandler.IgnoreCollisionWithMachine(savedChild.Collider, false);
                }
                if (savedParent != null && savedChild != null && savedParent.Collider != null && savedChild.Collider != null)
                {
                    Physics2D.IgnoreCollision(savedParent.Collider, savedChild.Collider, false);
                }
            });
        }
        else
        {
            // Fallback if child spawn failed
            parentBall.transform.localScale = Vector3.one;
            _captureHandler.EjectCapturedBall(_ejectionForce);
        }
    }
}
