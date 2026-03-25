using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/Character")]
public class CharacterSO : ScriptableObject
{
    public Sprite sprite;
    public int hitPoints;
    public int attack;
    public float accuracy;
    public float evasion;
    public List<AttackSO> aMoves;
    public List<DefendSO> dMoves;

    public int actionPoints;
    
    [Header("Computer specific attributes")]
    public float defendRate = 1f;
    public List<AttackChance> attackChances;
    public List<DefendChance> defendChances;
    public bool sameMovesAsPlayer = false;
}
