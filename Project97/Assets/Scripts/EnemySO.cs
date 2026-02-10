using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "EnemySO", menuName = "ScriptableObjects/Enemy")]
public class EnemySO : ScriptableObject
{
    public Sprite sprite;
    public int hitPoints;
    public int attack;
    public float accuracy;
    public float evasion;
    public List<AttackSO> aMoves;
    public List<DefendSO> dMoves;
}
