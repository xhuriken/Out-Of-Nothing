using System;
using UnityEngine;

/// <summary>
/// Orchestrates the global timing for energy processing.
/// Separates logic from frame rate to ensure synchronized machine behavior.
/// </summary>
public class PowerTickManager : MonoBehaviour
{
    public static PowerTickManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float _tickRate = 0.2f; // Seconds between ticks
    [SerializeField] private bool _autoStart = true;

    private float _timer;
    private bool _isTicking;

    /// <summary>
    /// Event fired when a new energy cycle begins.
    /// </summary>
    public event Action OnPowerTick;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        _isTicking = _autoStart;
    }

    private void Update()
    {
        if (!_isTicking) return;

        // Ticky Time !
        _timer += Time.deltaTime;
        if (_timer >= _tickRate)
        {
            _timer = 0f;
            ExecuteTick();
        }
    }

    private void ExecuteTick()
    {
        // Trigger the synchronized update across all networks and machines
        OnPowerTick?.Invoke();
    }

    public void SetTickRate(float newRate) => _tickRate = newRate;
}