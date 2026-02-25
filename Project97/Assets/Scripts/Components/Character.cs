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
    public int actionPoints {private set; get;}
    public int hitPoints; //Not used
    public int attack;
    public void ChangeAttack(int amount)
    {
        attack += amount;
    }
    public float accuracy {private set; get;}
    public void ChangeAccuracy(float amount)
    {
        accuracy += amount;
    }
    public float evasion {private set; get;}
    public void ChangeEvasion(float amount)
    {
        evasion += amount;
    }

    private List<Effect> effects = new List<Effect>();
    public void AddEffect(Effect effect)
    {
        if(!effects.Contains(effect)){ //Adds non duplicate effects only.
            effects.Add(effect);
        }
    }
    public void Setup(CharacterSO characterSO = default(CharacterSO))
    {
        healthSystem = GetComponent<HealthSystem>();
        healthSystem.Setup(characterSO.hitPoints);
        SetupMoves(characterSO.aMoves, characterSO.dMoves);
        actionPoints = characterSO.actionPoints;
        attack = characterSO.attack;
        accuracy = characterSO.accuracy;
        evasion = characterSO.evasion;
    }
    private void SetupMoves(List<AttackSO> initialAMoves, List<DefendSO> initialDMoves)
    {
        //Copy the character's starting moves to this character
        aMoves = new List<AttackSO>(initialAMoves);
        dMoves = new List<DefendSO>(initialDMoves);

    }

    public void DoEffects()
    {
        foreach(Effect effect in effects)
        {
            if(effect == Effect.Bleed)
            {
                healthSystem.TakeDamage(2);
                Debug.Log("2 damage taken to bleed effect");
            }
        }
    }
}
