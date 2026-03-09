using UnityEngine;
using System.Collections.Generic;

public class GeneratorMachine : MachineEntity
{
    [SerializeField] private float _currentEnergy;
    [SerializeField] private float _maxEnergy = 300f;

    [Header("Energy Settings")]
    [SerializeField] private float _transferAmount = 5f;
    [SerializeField] private float _transferRadius = 5f;

    private float _timer;

    private Collider2D[] _results = new Collider2D[20];
    private List<MachineEntity> _targets = new List<MachineEntity>();

    void Update()
    {
        if (!_isRunning || IsBeingDragged)
            return;

        GenerateEnergy();

        TransferEnergy();
    }

    void GenerateEnergy()
    {
        _timer += Time.deltaTime;

        if (_timer >= 1f)
        {
            _timer = 0f;

            if (_currentEnergy < _maxEnergy)
                _currentEnergy += 10f;
        }
    }

    void TransferEnergy()
    {
        if (_currentEnergy <= 0f)
            return;

        _targets.Clear();

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
                machine.AddEnergy(_transferAmount);
                _currentEnergy -= _transferAmount;

                _targets.Add(machine);

                if (_currentEnergy <= 0f)
                    break;
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _transferRadius);

        if (_targets == null)
            return;

        Gizmos.color = Color.yellow;

        foreach (var target in _targets)
        {
            if (target == null) continue;

            Gizmos.DrawLine(transform.position, target.transform.position);
        }
    }
}