using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Configuration data for a specific type of ball. (Blue, Red, Brown...)
/// </summary>
[CreateAssetMenu(menuName = "Data/BallData")]
public class BallDataSO : ScriptableObject
{
    /// <summary>
    /// Unique identifier for the ball.
    /// </summary>
    public string id;

    /// <summary>
    /// Number of clicks required before duplication triggers.
    /// </summary>
    public int clicksRequiredForDuplication = 5;

    /// <summary>
    /// Anti-spam delay between clicks (in seconds.)
    /// </summary>
    public float clickCooldown = 0.1f;

    /// <summary>
    /// Displayed color of the ball.
    /// </summary>
    public Color color = Color.white;

    /// <summary>
    /// Physical and visual radius of the ball.
    /// </summary>
    public float radius = 0.3f;

    /// <summary>
    /// Points awarded when this ball is consumed by the black hole.
    /// </summary>
    [Tooltip("Points awarded when this ball is consumed by the black hole.")]
    public int pointValue = 1;

    /// <summary>
    /// The specific prefab containing the visual components (ParticleSystem, Shaders, etc.) for this ball.
    /// Must contain a BallEntity component.
    /// </summary>
    public BallEntity prefab;
}