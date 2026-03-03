using Shapes;
using UnityEngine;

/// <summary>
/// Represents the border of the map. All object inside must bounce on collision.
/// </summary>
public class GameZone : MonoBehaviour
{
    // The global instance singleton
    public static GameZone Instance { get; private set; }

    [Header("Zone Settings")]
    [SerializeField] private float width = 18f;
    [SerializeField] private float height = 10f;
    [SerializeField] private Rectangle rendering;

    // Limits properties for easy access
    public float MinX => - width / 2f;
    public float MaxX => width / 2f;
    public float MinY => - height / 2f;
    public float MaxY => height / 2f;


    private void Awake()
    {
        // Singleton pattern: if an instance already exists, destroy this one
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        // Set the instance to this object
        Instance = this;
        // not useful for now
        // DontDestroyOnLoad(gameObject);

        // render the zone
        rendering.Width = width;
        rendering.Height = height;
    }

}
