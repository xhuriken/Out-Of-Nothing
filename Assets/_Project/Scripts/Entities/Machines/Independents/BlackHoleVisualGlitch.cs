using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Controls the visual scale distortion (glitch) and spaghettification shrink factor of objects inside the attraction zone.
/// </summary>
[RequireComponent(typeof(BlackHole))]
[RequireComponent(typeof(BlackHolePhysics))]
public class BlackHoleVisualGlitch : MonoBehaviour
{
    [Header("Visual Glitch Settings")]
    [SerializeField]
    [Tooltip("Maximum scale deviation for balls during glitch effect.")]
    private float _maxGlitchIntensityBalls = 0.5f;

    [SerializeField]
    [Tooltip("Maximum scale deviation for machines during glitch effect.")]
    private float _maxGlitchIntensityMachines = 0.3f;

    [SerializeField]
    [Tooltip("How many times per second the glitch scale updates for balls. A value of 0 or high values update on every frame.")]
    private float _glitchFrequencyBalls = 30f;

    [SerializeField]
    [Tooltip("How many times per second the glitch scale updates for machines. A value of 0 or high values update on every frame.")]
    private float _glitchFrequencyMachines = 30f;

    [SerializeField]
    [Range(0.05f, 5f)]
    [Tooltip("Exponent for the shrink curve. A value of 1 is linear. Values < 1 make the entity shrink slower (stays larger longer), while values > 1 make the entity shrink faster (gets smaller sooner).")]
    private float _shrinkPower = 0.64f;

    private BlackHole _blackHole;
    private BlackHolePhysics _physics;
    private readonly Dictionary<Transform, GlitchState> _glitchedObjects = new Dictionary<Transform, GlitchState>();
    private readonly List<Transform> _toRemove = new List<Transform>();

    /// <summary>
    /// Tracks the current visual glitch state of an attracted object.
    /// </summary>
    private struct GlitchState
    {
        /// <summary>
        /// The current random scale offset applied by the glitch effect.
        /// </summary>
        public Vector3 GlitchOffset;

        /// <summary>
        /// The next timestamp (Time.time) when the glitch offset should be updated.
        /// </summary>
        public float NextGlitchTime;
    }

    /// <summary>
    /// Initializes cached physical values.
    /// </summary>
    private void Awake()
    {
        _blackHole = GetComponent<BlackHole>();
        _physics = GetComponent<BlackHolePhysics>();
    }

    /// <summary>
    /// Evaluates scale changes and applies random squash/stretch glitch patterns at the specified frequency.
    /// </summary>
    private void Update()
    {
        if (_blackHole == null || _physics == null)
        {
            return;
        }

        float gRadius = _blackHole.GRadius;
        float attractRadiusOffset = _physics.AttractRadiusOffset;
        var attractedObjects = _physics.AttractedObjects;

        // 1. Identify objects that left the attraction zone
        _toRemove.Clear();
        foreach (var tx in _glitchedObjects.Keys)
        {
            if (tx == null)
            {
                _toRemove.Add(tx);
                continue;
            }

            if (!attractedObjects.ContainsKey(tx))
            {
                tx.localScale = Vector3.one;
                
                var shop = tx.GetComponent<Shop>();
                if (shop != null)
                {
                    shop.SetAttractionVisualState(1f, 0f);
                }

                var ball = tx.GetComponent<BallEntity>();
                if (ball != null)
                {
                    ball.IsAttracted = false;
                    var jelly = ball.GetComponent<BallJellyBounce>();
                    if (jelly != null)
                    {
                        jelly.ResetJellyState();
                    }
                }

                _toRemove.Add(tx);
            }
        }

        foreach (var tx in _toRemove)
        {
            _glitchedObjects.Remove(tx);
        }

        // 2. Apply glitch and shrink scale to currently attracted objects
        foreach (var kvp in attractedObjects)
        {
            Transform tx = kvp.Key;
            if (tx == null)
            {
                continue;
            }

            AttractedObjectData data = kvp.Value;

            // If the object is currently processing/duplicating (e.g. inside a machine, orbit preview),
            // we do NOT apply the visual glitch/shrink to it, and we reset its scale if it was previously glitched.
            bool isProcessing = (data.Ball != null && (data.Ball.IsProcessing || data.Ball.IsDuplicating));

            if (isProcessing)
            {
                // Reset scale and clear glitch state for this object so that it gets a clean state
                tx.localScale = Vector3.one;
                if (data.Ball != null)
                {
                    data.Ball.IsAttracted = false;
                }
                _glitchedObjects.Remove(tx);
                continue;
            }

            // Mark the ball as attracted so it ignores collision jelly bounces
            if (data.Ball != null)
            {
                data.Ball.IsAttracted = true;
            }

            float depth = data.Depth;
            bool isBall = data.Ball != null;

            // Calculate distance to center to compute shrink factor
            float distanceToCenter = Vector2.Distance(transform.position, tx.position);
            
            // shrinkFactor goes from 1.0 (at outer edge) to 0.0 (at gRadius)
            float shrinkFactor = Mathf.Clamp01((distanceToCenter - gRadius) / attractRadiusOffset);

            // Apply customizable shrink exponent curve
            shrinkFactor = Mathf.Pow(shrinkFactor, _shrinkPower);

            // Choose glitch parameters based on object type
            float maxIntensity = isBall ? _maxGlitchIntensityBalls : _maxGlitchIntensityMachines;
            float frequency = isBall ? _glitchFrequencyBalls : _glitchFrequencyMachines;

            // Retrieve or initialize the glitch state for this object
            if (!_glitchedObjects.TryGetValue(tx, out GlitchState glitchState))
            {
                glitchState = new GlitchState
                {
                    GlitchOffset = Vector3.zero,
                    NextGlitchTime = 0f
                };
            }

            // Update glitch offset at the specified frequency (or every frame if frequency <= 0)
            if (Time.time >= glitchState.NextGlitchTime || frequency <= 0f)
            {
                float randomX = UnityEngine.Random.Range(-maxIntensity, maxIntensity) * depth;
                float randomY = UnityEngine.Random.Range(-maxIntensity, maxIntensity) * depth;
                glitchState.GlitchOffset = new Vector3(randomX, randomY, 0f);
                glitchState.NextGlitchTime = frequency > 0f ? Time.time + (1f / frequency) : 0f;
            }

            // Save the updated state back to the dictionary
            _glitchedObjects[tx] = glitchState;

            // Retrieve Shop if applicable to scale it using GRadius rather than localScale
            Shop shop = tx.GetComponent<Shop>();
            if (shop != null)
            {
                float randomOffset = (glitchState.GlitchOffset.x + glitchState.GlitchOffset.y) * 0.5f;
                shop.SetAttractionVisualState(shrinkFactor, randomOffset);
                tx.localScale = Vector3.one;
            }
            else
            {
                // Apply the current glitch offset and shrink factor to the scale
                float scaleX = (1f + glitchState.GlitchOffset.x) * shrinkFactor;
                float scaleY = (1f + glitchState.GlitchOffset.y) * shrinkFactor;

                // Prevent scale from going exactly to zero to avoid physics warning, keep a minimum of 0.01f
                tx.localScale = new Vector3(Mathf.Max(0.01f, scaleX), Mathf.Max(0.01f, scaleY), 1f);
            }
        }
    }

    /// <summary>
    /// Restores original scale to all glitched transforms when disabled.
    /// </summary>
    private void OnDisable()
    {
        foreach (var kvp in _glitchedObjects)
        {
            if (kvp.Key != null)
            {
                kvp.Key.localScale = Vector3.one;
                var shop = kvp.Key.GetComponent<Shop>();
                if (shop != null)
                {
                    shop.SetAttractionVisualState(1f, 0f);
                }

                var ball = kvp.Key.GetComponent<BallEntity>();
                if (ball != null)
                {
                    ball.IsAttracted = false;
                    var jelly = ball.GetComponent<BallJellyBounce>();
                    if (jelly != null)
                    {
                        jelly.ResetJellyState();
                    }
                }
            }
        }
        _glitchedObjects.Clear();
    }
}
