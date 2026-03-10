using System.Collections.Generic;
using UnityEngine;

public class YellowBallBehavior: BallBehavior, IEnergyRelay
{
    [SerializeField] float _range = 3f;
    public Vector2 Position => transform.position;
    public float Range => _range;

   

    void Start()
    {
        ElectricManager.Instance.Register(this);
    }
}