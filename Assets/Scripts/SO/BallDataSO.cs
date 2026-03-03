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
    public float clickCooldown = 0.3f;

    /// <summary>
    /// Display color of the ball.
    /// </summary>
    public Color color = Color.white;

    /// <summary>
    /// Physical and visual radius of the ball.
    /// </summary>
    public float radius = 0.5f;

    /// <summary>
    /// Template behavior to be cloned by instances.
    /// </summary>
    [SerializeReference]
    public BallBehavior behaviorTemplate;
}