using UnityEngine;

/// <summary>
/// Centralized utility for handling energy-related geometric calculations.
/// Handles Collider-based distance checks and anchor point calculations for arcs.
/// </summary>
public static class EnergyCollisionUtility
{
    /// <summary>
    /// Checks if two energy nodes are within connection range using a Collider-based logic.
    /// A connection is valid if node A's ConnectionRadius touches node B's physical Collider.
    /// </summary>
    public static bool AreConnected(IEnergyNode a, IEnergyNode b)
    {
        if (a == null || b == null || a.PhysicsCollider == null || b.PhysicsCollider == null) return false;

        // Condition: Connection Radius of A touches Collider of B
        // OR Connection Radius of B touches Collider of A (symmetric)
        
        // Let's implement it precisely:
        // Does Circle(a.Position, a.ConnectionRadius) overlap b.PhysicsCollider?
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
        
        // ClosestPoint returns the point on the edge or inside.
        Vector2 closest = collider.ClosestPoint(point);
        float distanceSq = (closest - point).sqrMagnitude;
        
        return distanceSq <= (radius * radius);
    }

    /// <summary>
    /// Calculates the anchor point on the edge of a node's collider closest to a target position.
    /// Used for placing arc endpoints precisely on the "hull" of machines.
    /// </summary>
    public static Vector2 GetAnchorPoint(IEnergyNode node, Vector2 targetPosition)
    {
        if (node == null || node.PhysicsCollider == null) return targetPosition;
        return node.PhysicsCollider.ClosestPoint(targetPosition);
    }
}
