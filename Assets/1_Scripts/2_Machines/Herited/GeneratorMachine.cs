using UnityEngine;
using System.Collections.Generic;

public class GeneratorMachine : MachineEntity
{
    [SerializeField] private float _currentEnergy;
    [SerializeField] private float _maxEnergy = 300f;

    [Header("Energy Transfer")]
    //transfer range
    [SerializeField] private float _transferRadius = 5f;
    //energie transfer speed
    [SerializeField] private float _transferInterval = 0.1f;
    //quantity sent
    [SerializeField] private float _energyPerTransfer = 1f;

    private float _transferTimer;
    private float _generationTimer;

    private Collider2D[] _results = new Collider2D[20];
    private List<MachineEntity> _targets = new List<MachineEntity>();

    void Update()
    {
        //if drag NO ENERGIEEEE
        if (!_isRunning || IsBeingDragged)
            return;

        GenerateEnergy();
        TransferEnergy();
    }

    /// <summary>
    /// Generate 10 energie every second ( for the moment).
    /// </summary>
    void GenerateEnergy()
    {
        _generationTimer += Time.deltaTime;

        if (_generationTimer >= 1f)
        {
            _generationTimer = 0f;

            if (_currentEnergy < _maxEnergy)
                _currentEnergy += 10f;
        }
    }

    /// <summary>
    /// Transfer energy at the nearby machine.
    /// </summary>
    void TransferEnergy()
    {
        if (_currentEnergy <= 0f)
            return;

        _transferTimer += Time.deltaTime;

        if (_transferTimer < _transferInterval)
            return;

        _transferTimer = 0f;

        _targets.Clear();

        //circle collider for nearby machine 
        int count = Physics2D.OverlapCircleNonAlloc(
            transform.position,
            _transferRadius,
            _results
        );

        for (int i = 0; i < count; i++)
        {
            MachineEntity machine = _results[i].GetComponent<MachineEntity>();

            if (machine == null || machine == this)
                continue;

            if (machine.CanReceiveEnergy)
            {
                machine.AddEnergy(_energyPerTransfer);
                _currentEnergy -= _energyPerTransfer;

                _targets.Add(machine);

                if (_currentEnergy <= 0f)
                    break;
            }
        }
    }

    /// <summary>
    /// Draw line generator to nearby machine.
    /// </summary>
    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _transferRadius);

        Gizmos.color = Color.yellow;

        foreach (var target in _targets)
        {
            if (target == null) continue;

            Gizmos.DrawLine(transform.position, target.transform.position);
        }
    }
}