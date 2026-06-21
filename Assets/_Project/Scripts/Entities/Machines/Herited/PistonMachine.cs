using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class PistonMachine : MachineEntity
{
    // No energy required
    // Stock energy
    // Can take a ball.

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

    [Header("Settings")]
    /// <summary>
    /// When the piston is hit, the energy produced is proportional to the velocity of the ball. 
    /// This variable is a multiplier for that energy calculation.
    /// </summary>
    [SerializeField] private float _energyProducedIntensity;

    /// <summary>
    /// Read the var !
    /// </summary>
    [SerializeField] private float _animationDuration = 0.4f;

    private bool _canEjectBall;
    private bool _isProcessing;

    // This is local, did i need to make it in the IEnergyStorage ?
    public float AddEnergy(float amount)
    {
        // We clamp between 0 and Max ! 
        CurrentEnergy = Mathf.Clamp(CurrentEnergy + amount, 0f, _maxStorage);
        Debug.Log($"[PistonMachine] Added {amount} energy. Actually {CurrentEnergy}/{_maxStorage}");
        return amount;
    }

    protected override void Start()
    {
        base.Start();
        CurrentEnergy = _maxStorage;
    }

    public override void ReleaseCapturedBalls()
    {
        base.ReleaseCapturedBalls();
        if (_ballInside != null)
        {
            DOTween.Kill(_ballInside.transform);
            if (_ballInside.Collider != null)
            {
                _ballInside.Collider.enabled = true;
            }
            _ballInside.IsProcessing = false;
            if (_ballInside.Passport != null)
            {
                _ballInside.Passport.SetLockState(false);
            }
            _ballInside = null;
        }
    }

    public override bool OnDragStart()
    {
        ReleaseCapturedBalls();
        return base.OnDragStart();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (_ballInside != null)
        {
            BallEntity ball = _ballInside;
            _ballInside = null;
            if (BallPoolManager.Instance != null)
            {
                BallPoolManager.Instance.ReleaseBall(ball);
            }
            else
            {
                Destroy(ball.gameObject);
            }
        }
    }

    // I choose the fixed update because if we have a lag, i dont want the machine to be desync with other...
    private void FixedUpdate()
    {
        //Todo  Return when dragged.

        if (CurrentEnergy >= _maxStorage)
        {
            // play SFX, animation, etc...
            if (_ballInside != null && _ballInside.Data.id == "RedBall" && _canEjectBall)
            {
                Debug.Log("[PistonMachine] Max energy and ball => Ejecting ball !");
                // Animation
                // remove the actual energy (with dotween animation too)
                CurrentEnergy = 0f;
                // destroy the ball inside with the BallPoolManager
                // Instanciate the ballOut with the BallPoolManager
                // Eject Her !
                _canEjectBall = false;
                BallPoolManager.Instance.ReleaseBall(_ballInside);
                //var ball = BallPoolManager.Instance.SpawnBall(, _TargetTransformBall);
                _ballInside = null;
            }
        }
    }

    /// <summary>
    /// Handles collisions specifically for the bumper mechanics.
    /// </summary>
    public override void OnPartCollisionEnter(string partId, Collision2D collision)
    {
        if (!_isRunning || IsBeingDragged)
        {
            return;
        }

        // If an ball touch the piston part
        if (partId == "Piston" && collision.gameObject.TryGetComponent(out BallEntity pusherBall))
        {
            // Get the velocity of the ball, Get the velocity magnitude of the good axis (x or y depending on the piston orientation)
            // and calculate the energy produced with the multiplier

            Debug.Log($"[PistonMachine] Piston hit by {pusherBall.Data.id}. Calculating energy...");

            // its temp TODO MAKE AN BETTER THING
            float force = collision.relativeVelocity.magnitude;
            float energyGenerated = force * _energyProducedIntensity;

            AddEnergy(energyGenerated);
        }
    }

    public override void OnPartTriggerEnter(string partId, Collider2D collider)
    {
        if (IsBeingDragged) return;

        // If the ball touch de box part
        if (partId == "Box" && collider.gameObject.TryGetComponent(out BallEntity useBall))
        {
            if (_ballInside == null && useBall.Data.id == "RedBall")
            {
                Debug.Log($"[PistonMachine] Box triggered. Capturing {useBall.Data.id}.");
                _ballInside = useBall;
                _ballInside.IsProcessing = true;
                GameInputManager.Instance.ForceDrop(useBall);

                // Use Passport to take full control
                _ballInside.Passport.SetLockState(true);

                // TODO: Stop collision & physics
                _ballInside.transform.DOMove(_TargetTransformBall.position, _animationDuration)
                    .SetEase(Ease.OutElastic)
                    .OnComplete(() => {
                        // hum, something ? i got a theory
                        _canEjectBall = true;
                        Debug.Log("[PistonMachine] Capture animation completed ! Ready for ejection");
                    });
            }
        }
    }

    public override void OnPartTriggerStay(string partId, Collider2D collider)
    {
        OnPartTriggerEnter(partId, collider);
    }

    protected override void OnTickExecuted()
    {
        // Add synchronized SFX or particle triggers here
    }
}