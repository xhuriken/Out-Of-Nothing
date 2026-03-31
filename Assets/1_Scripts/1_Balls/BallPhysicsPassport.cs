using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Priority levels for physics overrides. Higher values override lower ones.
/// </summary>
public enum PhysicsPriority
{
    Default = 0,    // Natural physics / gravity
    Behavior = 10,  // Oscillation, attraction, etc.
    Surface = 20,   // Bounciness / Friction from specific tiles
    Drag = 30,      // Player interaction
    Machine = 40    // Piston capture, transport, etc. (Highest)
}

/// <summary>
/// Defines how a velocity request should be merged with the current state.
/// </summary>
public enum VelocityMode
{
    Override,   // Replaces the velocity entirely
    Add,        // Adds to the existing velocity
    Multiply    // Scales the current velocity (e.g., dampening)
}

/// <summary>
/// Middleman between external logic and the Rigidbody2D.
/// Resolves conflicting movement requests based on priority.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class BallPhysicsPassport : MonoBehaviour
{
    private Rigidbody2D _rb;
    private PhysicsPriority _currentMaxPriority = PhysicsPriority.Default;

    private Vector2 _nextVelocity;
    private bool _hasOverrideThisFrame;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    /// <summary>
    /// Submits a request to modify the ball's velocity.
    /// Only the highest priority request per frame is fully honored for 'Override' mode.
    /// </summary>
    public void RequestVelocity(Vector2 velocity, PhysicsPriority priority, VelocityMode mode)
    {
        if (priority < _currentMaxPriority && mode == VelocityMode.Override) return;

        switch (mode)
        {
            case VelocityMode.Override:
                _nextVelocity = velocity;
                _currentMaxPriority = priority;
                _hasOverrideThisFrame = true;
                break;
            case VelocityMode.Add:
                // We add if it's high enough priority or if no override exists
                _rb.linearVelocity += velocity;
                break;
            case VelocityMode.Multiply:
                _rb.linearVelocity *= velocity;
                break;
        }
    }

    /// <summary>
    /// Forces the ball to a kinematic-like state where only the owner can move it.
    /// Useful for Machines (Piston) or specific animations.
    /// </summary>
    public void SetLockState(bool isLocked)
    {
        _rb.isKinematic = isLocked;
        if (isLocked) _rb.linearVelocity = Vector2.zero;
    }

    private void FixedUpdate()
    {
        // Apply the resolved override if one was requested
        if (_hasOverrideThisFrame)
        {
            _rb.linearVelocity = _nextVelocity;
        }

        // Reset for next frame
        _hasOverrideThisFrame = false;
        _currentMaxPriority = PhysicsPriority.Default;
    }

    /// <summary>
    /// Helper to apply impulses directly while respecting the priority system.
    /// </summary>
    public void ApplyImpulse(Vector2 force, PhysicsPriority priority)
    {
        if (priority >= _currentMaxPriority)
        {
            _rb.AddForce(force, ForceMode2D.Impulse);
        }
    }
}