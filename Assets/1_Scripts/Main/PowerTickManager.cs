using System;
using UnityEngine;

/// <summary>
/// Orchestrates the global timing for energy processing.
/// Separates logic from frame rate to ensure synchronized machine behavior.
/// </summary>
[DefaultExecutionOrder(-200)]
public class PowerTickManager : MonoBehaviour
{
    public static PowerTickManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float _tickRate = 1f;
    [SerializeField] private bool _autoStart = true;
    [SerializeField] private bool _enableLogs = false;

    public float TickRate => _tickRate;
    public bool EnableLogs => _enableLogs;

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

    private void FixedUpdate()
    {
        if (!_isTicking) return;

        _timer += Time.fixedDeltaTime;
        if (_timer >= (_tickRate - 0.0001f))
        {
            _timer = 0f;
            ExecuteTick();
        }
    }

    private void ExecuteTick()
    {
        if (EnergyManager.Instance != null && EnergyManager.Instance.EnableLogs) 
            Debug.Log($"[PowerTick] --- Cycle Start at {Time.time:F2} ---");
            
        // Trigger the synchronized update across all networks and machines
        OnPowerTick?.Invoke();
    }

    public void SetTickRate(float newRate) => _tickRate = newRate;
}