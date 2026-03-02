using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
[RequireComponent(typeof(HealthSystem))]
public class Character : MonoBehaviour
{
    public HealthSystem healthSystem {private set; get;}
    private List<AttackSO> aMoves; //Attack moves pool
    public List<AttackSO> GetAMoves()
    {
        return aMoves;
    }
    public void AddAMove(AttackSO attackSO)
    {
        aMoves.Add(attackSO);
    }
    private List<DefendSO> dMoves; //Defensive moves pool

    public List<DefendSO> GetDMoves()
    {
        return dMoves;
    }
    public void AddDMove(DefendSO defendSO)
    {
        dMoves.Add(defendSO);
    }
    public List<MoveSO> GetAllMoves()
    {
        List<MoveSO> allMoves = new List<MoveSO>(aMoves);
        allMoves.AddRange(dMoves);
        return allMoves;
    }
    private int baseActionPoints;
    public int actionPoints {private set; get;}
    public int hitPoints; //Not used
    public int attack;
    public void ChangeAttack(int amount)
    {
        attack += amount;
    }
    private float baseAccuracy;
    public float accuracy {private set; get;}
    public void ChangeAccuracy(float amount)
    {
        accuracy += amount;
    }
    private float baseEvasion;
    public float evasion {private set; get;}
    public void ChangeEvasion(float amount)
    {
        evasion += amount;
    }

    private Dictionary<Effect, int> effects = new Dictionary<Effect, int>();
    public void AddEffect(Effect effect)
    {
        effects[effect] = EffectDefaults.Durations[effect];
        if(effect == Effect.Bind)
        {
            bindDPercentage = 0.03f;
        }
    }
    public void Setup(CharacterSO characterSO = default(CharacterSO))
    {
        healthSystem = GetComponent<HealthSystem>();
        healthSystem.Setup(characterSO.hitPoints);
        SetupMoves(characterSO.aMoves, characterSO.dMoves);
        attack = characterSO.attack;

        baseAccuracy = characterSO.accuracy;
        baseEvasion = characterSO.evasion;
        baseActionPoints = characterSO.actionPoints;

        ResetCurrentStats();
    }

    private void ResetCurrentStats()
    {
        accuracy = baseAccuracy;
        evasion = baseEvasion;
        actionPoints = baseActionPoints;
    }

    private void SetupMoves(List<AttackSO> initialAMoves, List<DefendSO> initialDMoves)
    {
        //Copy the character's starting moves to this character
        aMoves = new List<AttackSO>(initialAMoves);
        dMoves = new List<DefendSO>(initialDMoves);

    }
    /// <summary>
    /// At start of each turn, TurnManager calls for each Character
    /// </summary>
    public void DoEffects(float binderAttack)
    {
        ResetCurrentStats();
        foreach (var effect in effects.Keys.ToList()) //Loops through a copy, so safe to modify dictionary in this loop
        {
            DoEffect(effect);
            effects[effect] -= 1;
            if (effects[effect] == 0) //Doesn't remove -1 or below which signifies unlimited
            {
                effects.Remove(effect);
                
            }
            if(effect != Effect.Bind && effects[effect] <= 2)
            {
                if (UC.RandomEventPercentage( ((evasion - binderAttack) / 100f) + 0.5f) )
                {
                    effects.Remove(effect);
                    Debug.Log("Bind removed early");
                }
            }
        }
    }
    private float bindDPercentage;
    private void DoEffect(Effect effect)
    {
        switch (effect)
        {
            case Effect.AdrenalineRush:

                break;

            case Effect.Enraged:

                break;

            case Effect.Blindness:
                accuracy -= 0.1f * baseAccuracy;
                evasion -= 0.1f * baseEvasion;
                break;

            case Effect.Slow:
                evasion -= 0.15f * baseEvasion;
                break;

            case Effect.Bind:
                healthSystem.TakeDamage(Mathf.RoundToInt(bindDPercentage * healthSystem.GetMaxHealth()));
                bindDPercentage += 0.03f;
                break;

            case Effect.Wind:
                actionPoints = Mathf.RoundToInt(baseActionPoints / 3);
                break;

            case Effect.Prone:
                actionPoints = 0;
                break;

            case Effect.BrokenBones:
                
                break;

            case Effect.Bleed:
                healthSystem.TakeDamage(5);
                Debug.Log("5 damage taken to bleed effect");
                break;

            default:
                Debug.LogError($"{effect} not defined");
                break;
        }
    }
}
