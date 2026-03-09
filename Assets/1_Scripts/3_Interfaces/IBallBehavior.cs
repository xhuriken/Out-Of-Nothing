using UnityEngine;
using System.Collections;

namespace Assets.Scripts.Interfaces
{
    /// <summary>
    /// Contract for ball behaviors using Unity physics
    /// </summary>
    public interface IBallBehavior
    {
        // Executed inside FixedUpdate for physics manipulation
        void ExecuteFixedUpdate(BallEntity ball, float fixedDeltaTime);
    }
}