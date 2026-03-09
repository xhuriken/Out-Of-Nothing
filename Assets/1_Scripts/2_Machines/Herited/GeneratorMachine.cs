using UnityEngine;
using UnityEngine.UIElements;

public class GeneratorMachine :  MachineEntity
{

    //local vars
    [SerializeField]
    private float _currentEnergy;
    [SerializeField]
    private float _maxEnergy = 300f;

    private float _timer;

    void Update()
    {

        if (!_isRunning || IsBeingDragged)
        {
            return;
        }

        _timer += Time.deltaTime;

        if (_timer >= 1f)
        {
            _timer = 0f;

            if (_currentEnergy < _maxEnergy)
            {
                _currentEnergy += 10;
            }
        }
    }

}
