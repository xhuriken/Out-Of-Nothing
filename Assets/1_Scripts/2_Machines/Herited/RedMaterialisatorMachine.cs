using UnityEngine;
using UnityEngine.UIElements;

public class RedMaterialisatorMachine :  MachineEntity
{

    //local vars
    [SerializeField]
    private float _currentEnergy;
    [SerializeField]
    private float _maxEnergy = 100f;

    [Header("Ball to generate")]
    [SerializeField] private BallDataSO _redBallData;

    [Header("Spawn Settings")]
    [SerializeField] private float _spawnForce = 5f;
    [SerializeField] private float _spawnDelay = 5f;
    [SerializeField] private float _consumption = 1f;

    private float _timer;
    private float _timerconsumption;

    public override void AddEnergy(float amount)
    {
        _currentEnergy += amount;
        _currentEnergy = Mathf.Clamp(_currentEnergy, 0f, _maxEnergy);
    }

    void Awake()
    {
        _consumptiondemand = true;
    }
    void Update()
    {
        if (!_isRunning || IsBeingDragged)
        {
            return;
        }

        if (_currentEnergy <= 0f)
        {
            return;
        }

        if (_currentEnergy > 0f)
            {
            _timer += Time.deltaTime;
            _timerconsumption += Time.deltaTime;

            if (_timerconsumption >= _consumption)
            {
                _timerconsumption = 0f;
                _currentEnergy -= 10f;
             
            }

            if (_timer >= _spawnDelay)
            {
                _timer = 0f;
                SpawnBall();
            }
        }
    }

    /// <summary>
    /// Transfer energy at the nearby machine.
    /// </summary>
    private void SpawnBall()
    {

        BallEntity newBall = BallPoolManager.Instance.SpawnBall(_redBallData, transform.position);

        newBall.Rb.AddForce(Vector2.right * _spawnForce, ForceMode2D.Impulse);
    }
}
