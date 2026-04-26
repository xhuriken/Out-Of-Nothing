using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class PistonMachine : MachineEntity, IEnergyStorage
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

    //local vars
    private float _currentEnergy;
    private float _maxEnergy = 100f;
    private bool _canEjectBall;
    private bool _isProcessing;

    public float CurrentEnergy => _currentEnergy;

    public float MaxEnergy => _maxEnergy;

    // This is local, did i need to make it in the IEnergyStorage ?
    public float AddEnergy(float amount)
    {
        // We clamp between 0 and Max ! 
        _currentEnergy = Mathf.Clamp(_currentEnergy + amount, 0f, _maxEnergy);
        Debug.Log($"[PistonMachine] Added {amount} energy. Actually {_currentEnergy}/{_maxEnergy}");
        return amount;
    }

    public float ExtractEnergy(float amount)
    {
        float available = Mathf.Min(amount, _currentEnergy);
        _currentEnergy -= available;
        return available;
    }

    public void Start()
    {
        _currentEnergy = MaxEnergy;
    }

    // I choose the fixed update because if we have a lag, i dont want the machine to be desync with other...
    private void FixedUpdate()
    {
        //Todo  Return when dragged.

        if (_currentEnergy >= _maxEnergy)
        {
            // play SFX, animation, etc...
            if (_ballInside != null && _ballInside.Data.id == "RedBall" && _canEjectBall)
            {
                Debug.Log("[PistonMachine] Max energy and ball => Ejecting ball !");
                // Animation
                // remove the actual energy (with dotween animation too)
                _currentEnergy = 0f;
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
                GameInputManager.Instance.ForceDrop();

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

    protected override void OnTickExecuted()
    {
        // Add synchronized SFX or particle triggers here
    }
}