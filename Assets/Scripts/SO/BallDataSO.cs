using Assets.Scripts.Interfaces;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "BallDataSO", menuName = "Scriptable Objects/BallDataSO")]

public class BallDataSO : ScriptableObject
{
    public string id;
    public Color displayColor = Color.white;
    public float radius = 0.5f;
    public float mass = 1f;

    [SerializeReference]
    public IBallBehavior behavior;
}
