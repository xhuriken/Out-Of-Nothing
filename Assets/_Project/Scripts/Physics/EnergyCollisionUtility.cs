using UnityEngine;

/// <summary>
/// Centralized utility for handling energy-related geometric calculations.
/// Handles Collider-based distance checks and anchor point calculations for arcs.
/// </summary>
public static class EnergyCollisionUtility
{
    /// <summary>
    /// Checks if two energy nodes are within connection range using radius-to-radius logic.
    /// A connection is valid if node A's ConnectionRadius touches node B's physical radius.
    /// </summary>
    public static bool AreConnected(IEnergyNode a, IEnergyNode b)
    {
        if (a == null || (a is UnityEngine.Object objA && objA == null) ||
            b == null || (b is UnityEngine.Object objB && objB == null))
        {
            return false;
        }

        float dist = Vector2.Distance(a.Position, b.Position);
        
        // Attraction Radius of A touches Physical Radius of B
        // OR Attraction Radius of B touches Physical Radius of A
        bool aTouchesB = dist <= (a.ConnectionRadius + b.PhysicalRadius);
        bool bTouchesA = dist <= (b.ConnectionRadius + a.PhysicalRadius);

        return aTouchesB || bTouchesA;
    }

    /// <summary>
    /// Checks if a connection is maintained using Collider-based logic.
    /// The arc stays as long as the Connection Radius touches the actual Collider.
    /// </summary>
    public static bool IsConnectionMaintained(IEnergyNode a, IEnergyNode b)
    {
        if (a == null || (a is UnityEngine.Object objA && objA == null) ||
            b == null || (b is UnityEngine.Object objB && objB == null))
        {
            return false;
        }
        if (a.PhysicsCollider == null || b.PhysicsCollider == null) return false;

        bool aTouchesB = IsPointNearCollider(a.Position, a.ConnectionRadius, b.PhysicsCollider);
        bool bTouchesA = IsPointNearCollider(b.Position, b.ConnectionRadius, a.PhysicsCollider);

        return aTouchesB || bTouchesA;
    }

    /// <summary>
    /// Checks if a point with a radius overlaps a given collider.
    /// </summary>
    public static bool IsPointNearCollider(Vector2 point, float radius, Collider2D collider)
    {
        if (collider == null) return false;
        Vector2 closest = collider.ClosestPoint(point);
        float distanceSq = (closest - point).sqrMagnitude;
        return distanceSq <= (radius * radius);
    }

    /// <summary>
    /// Calculates the anchor point on the visual edge (Physical Radius) of a node.
    /// </summary>
    public static Vector2 GetAnchorPoint(IEnergyNode node, Vector2 targetPosition)
    {
        if (node == null || (node is UnityEngine.Object obj && obj == null)) return targetPosition;
        Vector2 direction = (targetPosition - node.Position).normalized;
        return node.Position + direction * node.PhysicalRadius;
    }
}
