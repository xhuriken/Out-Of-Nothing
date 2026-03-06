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
    /// <summary>
    /// Is the machine currently processing a ball and producing energy ?
    /// </summary>
    [SerializeField] private bool _isProcessing = false;

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

    // local vars
    private float _currentEnergy;
    private float _maxEnergy;
    [SerializeField] private bool _canEjectBall;

    public float CurrentEnergy => _currentEnergy;

    public float MaxEnergy => _maxEnergy;

    public float AddEnergy(float amount)
    {
        return 0f;
    }

    public float ExtractEnergy(float amount)
    {
        return 0f; 
    }

    public void Start()
    {
        _currentEnergy = MaxEnergy;
    }

    // I choose the fixed update because if we have a lag, i dont want the machine to be desync with other...
    private void FixedUpdate()
    {
        if (_currentEnergy >= _maxEnergy)
        {
            // play SFX, animation, etc...
            if (_ballInside != null && _ballInside.Data.id == "RedBall" && _canEjectBall)
            {
                // Animation
                // remove the actual energy (with dotween animation too)
                _currentEnergy = 0f;
                // destroy the ball inside with the BallPoolManager
                // Instanciate the ballOut with the BallPoolManager
                // Eject Her !
                _canEjectBall = false;
            }
        }
    }

    /// <summary>
    /// Handles collisions specifically for the bumper mechanics.
    /// </summary>
    public override void OnPartCollisionEnter(string partId, Collision2D collision)
    {
        if (!_isRunning)
        {
            return;
        }

        // If an ball touch the piston part
        if (partId == "Piston" && collision.gameObject.TryGetComponent(out BallEntity pusherBall))
        {
            // Get the velocity of the ball, Get the velocity magnitude of the good axis (x or y depending on the piston orientation)
            // and calculate the energy produced with the multiplier
        }
    }

    public override void OnPartTriggerEnter(string partId, Collider2D collider)
    {
        // If the ball touch de box part
        if (partId == "Box" && collider.gameObject.TryGetComponent(out BallEntity useBall))
        {
            if (_ballInside == null && useBall.Data.id == "RedBall")
            {
                Debug.Log("A red ball touch me !");
                _ballInside = useBall;
                _ballInside.IsProcessing = true;
                GameInputManager.Instance.EndDrag();
                // TODO: Stop collision & physics
                _ballInside.transform.DOMove(_TargetTransformBall.position, _animationDuration).SetEase(Ease.OutElastic).OnComplete(() =>
                {
                    // hum, something ? i got a theory
                    _canEjectBall = true;
                    Debug.Log($"{_ballInside}, {_ballInside.Data.id}, {_canEjectBall}");
                });
            }
        }
    }
}