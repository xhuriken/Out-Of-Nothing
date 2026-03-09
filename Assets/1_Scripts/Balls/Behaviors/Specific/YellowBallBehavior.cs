using Assets.Scripts.Interfaces;
using Strix.VirtualInspector.Demo;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;


/// <summary>
/// Used for FirstBall and RedBall. Standard Unity physics handles collisions and bounces
/// </summary>
[Serializable]
public class YellowBallBehavior : BallBehavior
{
    private List<BallEntity> _connections = new List<BallEntity>();
    private Collider2D[] _results = new Collider2D[20];

    /// <summary>
    /// Clones the behavior to ensure independent runtime state.
    /// </summary>
    public override BallBehavior Clone()
    {
        // Shallow copy is sufficient for basic value types
        return (BallBehavior) MemberwiseClone();
    }

    public override void ExecuteFixedUpdate(BallEntity ball, float fixedDeltaTime)
    {
        if (ball.IsBeingDragged) return;

        _connections.Clear();

        Vector2 currentPosition = ball.transform.position;

        int count = Physics2D.OverlapCircleNonAlloc(currentPosition, 3f, _results);

        for (int i = 0; i < count; i++)
        {
            BallEntity other = _results[i].GetComponent<BallEntity>();

            if (other == null || other == ball)
                continue;

            if (other.Data.id == "YellowBall")
            {
                _connections.Add(other);
            }
        }

        ball.SetConnections(_connections);
    }


}

