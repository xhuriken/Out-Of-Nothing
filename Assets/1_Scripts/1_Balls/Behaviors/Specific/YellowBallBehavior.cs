using System.Collections.Generic;
using UnityEngine;

public class YellowBallBehavior: BallBehavior
{
    [SerializeField] float _range = 3f;
    public float Range => _range;

    public override BallBehavior Clone()
    {
        return (BallBehavior)MemberwiseClone();
    }

    void Start()
    {
        ElectricManager.Instance.Register(this);
    }
}