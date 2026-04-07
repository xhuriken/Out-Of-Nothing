using DG.Tweening;
using UnityEngine;

public class PressMachine : MachineEntity, IEnergyConsumer
{ 
    [Header("References")]
    /// <summary>
    /// Represents the ball that is currently inside the piston. (Its the one who'll be destroy)
    /// </summary>
    [SerializeField] private BallEntity _ballInside;
    /// <summary>
    /// Represents the ball entity used who'll be instanciated when we have enough energy.
    /// </summary>
    [SerializeField] private BallEntity _ballOut;
    /// <summary>
    /// Represent the transform where the ballInside must be.
    /// </summary>
    [SerializeField] private Transform _TargetTransformBall;
    /// <summary>
    /// Read the var !
    /// </summary>
    [SerializeField] private float _animationDuration = 0.4f;

    private bool _canEjectBall;

    public bool NeedsEnergy => throw new System.NotImplementedException();

    public float EnergyRequest => throw new System.NotImplementedException();

    public float MaxFlowRate => throw new System.NotImplementedException();

    public void ProvideEnergy(float amount)
    {
        throw new System.NotImplementedException();
    }

    public override bool OnDragStart()
    {
        if (_ballInside != null)
            return false;

        return base.OnDragStart();

    }
    private void FixedUpdate()
    {
        //Todo  Return when dragged.

        
            // play SFX, animation, etc...
            if (_ballInside != null && _ballInside.Data.id == "RedBall" && _canEjectBall)
            {
                Debug.Log("[PressMachine]  => Ejecting ball !");
                // Animation
              
                // destroy the ball inside with the BallPoolManager
                // Instanciate the ballOut with the BallPoolManager
                // Eject Her !
                _canEjectBall = false;
                BallPoolManager.Instance.ReleaseBall(_ballInside);
                //var ball = BallPoolManager.Instance.SpawnBall(, _TargetTransformBall);
                _ballInside = null;
            }
        
    }

    public override void OnPartTriggerEnter(string partId, Collider2D collider)
    {
        if (IsBeingDragged) return;

        // If the ball touch de box part
        if (partId == "CENTER" && collider.gameObject.TryGetComponent(out BallEntity useBall))
        {
            if (_ballInside == null && useBall.Data.id == "RedBall")
            {
                Debug.Log($"[PressMachine] Line Center triggered. Capturing {useBall.Data.id}.");
                _ballInside = useBall;
                _ballInside.IsProcessing = true;
                GameInputManager.Instance.ForceDrop();
                // TODO: Stop collision & physics
                _ballInside.transform.DOMove(_TargetTransformBall.position, _animationDuration).SetEase(Ease.OutElastic).OnComplete(() =>
                {
                    // hum, something ? i got a theory
               
                    _canEjectBall = true;
                    Debug.Log("[PressMachine] Capture animation completed ! Ready for ejection");
                });
            }

            
        }
    }

    protected override void OnTickExecuted()
    {
    }
}
