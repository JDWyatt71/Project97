using System.Collections.Generic;
using UnityEngine;

//  EnemySO  –  ScriptableObject that holds an enemy's stats,
//  move lists, and all AI behaviour data.

[CreateAssetMenu(menuName = "ScriptableObjects/Enemy")]
public class EnemySO : CharacterSO
{
    [Header("Identity")]
    public string enemyName;

    [Header("AI Behaviour")]
    [Tooltip("0 = never defends, 1 = always tries to defend every turn.")]
    [Range(0f, 1f)]
    public float defendRate = 0.75f;

    [Tooltip("Moves the enemy heavily favours. Leave empty for equal weighting.")]
    public List<AttackSO> favouredMoves = new();

    [Tooltip("Moves the enemy uses rarely (low weight). Leave empty if unused.")]
    public List<AttackSO> rareMoves = new();

    [Tooltip("Weighted chances for each defensive move. " +
             "If empty, all dMoves are treated as equal. " +
             "Entries must match items in dMoves by name.")]
    public List<DefendWeight> defendWeights = new();

    [Tooltip("Conditions that restrict when certain moves can be used.")]
    public List<MoveCondition> moveConditions = new();
}

[System.Serializable]
public class DefendWeight
{
    public DefendSO move;
    [Range(0f, 1f)] public float weight = 0.25f;
}

[System.Serializable]
public class MoveCondition
{
    public AttackSO move;
    public MoveUseCondition condition;
}

public enum MoveUseCondition
{
    Always,
    OnlyWhenPlayerProne,
    OnlyWhenPlayerStanding,
}
