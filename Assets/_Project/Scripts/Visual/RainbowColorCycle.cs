using UnityEngine;
using Shapes;

/// <summary>
/// Cycles the outer color of a Shapes Disc through the rainbow colors using HSV space.
/// </summary>
[RequireComponent(typeof(Disc))]
public class RainbowColorCycle : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Speed of the color cycle transition.")]
    private float _speed = 0.5f;

    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("Saturation of the colors (1 is fully saturated, 0 is grayscale).")]
    private float _saturation = 1f;

    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("Brightness/value of the colors (1 is fully bright, 0 is black).")]
    private float _value = 0.6f;

    private Disc _disc;

    /// <summary>
    /// Caches the reference to the Disc component.
    /// </summary>
    private void Awake()
    {
        _disc = GetComponent<Disc>();
    }

    /// <summary>
    /// Smoothly updates the outer color of the disc using HSV mapping.
    /// </summary>
    private void Update()
    {
        if (_disc != null)
        {
            float hue = (Time.time * _speed) % 1.0f;
            _disc.ColorOuter = Color.HSVToRGB(hue, _saturation, _value);
        }
    }
}
