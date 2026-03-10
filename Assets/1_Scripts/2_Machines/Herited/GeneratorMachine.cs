using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class GeneratorMachine : MachineEntity
{
    [Header("Energy")]
    [SerializeField] private float _currentEnergy;
    [SerializeField] private float _maxEnergy = 300f;

    [Header("Generation")]
    [SerializeField] private float _generationInterval = 1f;
    [SerializeField] private float _energyPerGeneration = 10f;

    [Header("Transfer")]
    [SerializeField] private float _transferRadius = 2.5f;
    [SerializeField] private float _transferInterval = 0.2f;
    [SerializeField] private float _energyPerTransfer = 1f;

    private float _generationTimer;
    private float _transferTimer;

    private Collider2D[] _results = new Collider2D[20];

    private BallEntity _currentTarget;
    private List<MachineEntity> _targets = new List<MachineEntity>();

    void Update()
    {
        if (!_isRunning || IsBeingDragged) return;

        GenerateEnergy();
        TransferEnergy();
    }

    void GenerateEnergy()
    {
        _generationTimer += Time.deltaTime;

        if (_generationTimer >= _generationInterval)
        {
            _generationTimer = 0f;

            if (_currentEnergy < _maxEnergy)
            {
                _currentEnergy += _energyPerGeneration;
                _currentEnergy = Mathf.Clamp(_currentEnergy, 0f, _maxEnergy);
            }
        }
    }

    void TransferEnergy()
    {
        if (_currentEnergy <= 0f) return;

        _transferTimer += Time.deltaTime;
        if (_transferTimer < _transferInterval) return;

        _transferTimer = 0f;
        _currentTarget = null;

        int count = Physics2D.OverlapCircleNonAlloc(transform.position, _transferRadius, _results);
        float closestDistance = float.MaxValue;

        // Trouve la YellowBall la plus proche
        for (int i = 0; i < count; i++)
        {
            BallEntity ball = _results[i].GetComponent<BallEntity>();
            if (ball == null || ball.Data.id != "YellowBall") continue;

            float dist = Vector2.Distance(transform.position, ball.transform.position);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                _currentTarget = ball;
            }
        }

        // Transfert d'énergie à la YellowBall
        if (_currentTarget != null)
        {
            _currentTarget.ReceiveEnergy(_energyPerTransfer);
            _currentEnergy -= _energyPerTransfer;
        } 

        //// Transfert d'énergie aux machines qui peuvent recevoir
        //for (int i = 0; i < count; i++)
        //{
        //    MachineEntity machine = _results[i].GetComponent<MachineEntity>();
        //    if (machine == null || machine == this) continue;

        //    if (machine.CanReceiveEnergy)
        //    {
        //        machine.AddEnergy(_energyPerTransfer);
        //        _currentEnergy -= _energyPerTransfer;
        //        _targets.Add(machine);

        //        if (_currentEnergy <= 0f) break;
        //    }
        //}
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _transferRadius);

        // Lignes vers les machines
        foreach (var target in _targets)
        {
            if (target == null) continue;
            Gizmos.DrawLine(transform.position, target.transform.position);
        }

        // Ligne vers la YellowBall la plus proche
        if (_currentTarget != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, _currentTarget.transform.position);
        }
    }
}