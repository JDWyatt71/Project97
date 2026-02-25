using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine.UI;
public class CombatManager
{
    public event Action RunningIsFalse;
    private FightAnalyticsTracker analytics;
    public CombatManager(FightAnalyticsTracker analytics)
    {
        this.analytics = analytics;
    }
    public void PerformMovePair(AttackSO a, DefendSO d, Character attacker, Character target, string turnName)
    {
        string status = PerformAttack(attacker, target, a, d);

        d ??= AssetsDatabase.I?.defaultDefendSO;

        if (d == null)
        {
            Debug.Log("No DefendSo avaliable");
        }
        //Damage displayed here is approximate, and doesn't factor randomness or defense reduction percentage like TotalDam() calculated.
        string aS = $"Attack: {a.name}, Damage: {a.damage} ≈ {CalculateInitialDamage(GetDamage(a.damage), attacker.attack)}"; 
        string dS;
        if(d != null){
            dS = $"Defend: {d.name}";
        }
        else
        {
            dS = "No defense";
        }

        Debug.Log($"{turnName}'s Turn:\n{aS}\n{dS}\nAttack {status}");
    }
    /// <summary>
    /// Returns a string of what happened with the move: dodge, block, hit, deflect
    /// </summary>
    /// <param name="attacker"></param>
    /// <param name="target"></param>
    /// <param name="attackSO"></param>
    /// <param name="defendSO"></param>
    /// <returns></returns>
    private string PerformAttack(Character attacker, Character target, AttackSO attackSO, DefendSO defendSO = null)
    {
        string moveName = attackSO.name;
        GameEvents.RaiseMoveUsed(moveName, attacker.ToString());
        analytics.RegisterAttackAttempt();

        if (defendSO == null)
        {
            defendSO = AssetsDatabase.I.defaultDefendSO;
        }
        //Implement attacker.accuracy and target.evasion
        float moveAccuracy = CalculateMoveAccuracy(attackSO.accuracy, attacker.accuracy, target.evasion, defendSO.dodgeBonusPercent);
        //Debug.Log($"Move accuracy: {moveAccuracy}");
        if (!RandomEvent(moveAccuracy))
        {
            return "dodged"; //So don't do any damage.
        }
        //player attacking damage
        //Base damage of move
        //Successful Block
        int totalDamage;
        
        //Calculate damage as non-zero damage inflicted on a character
        int basedam = GetDamage(attackSO.damage);
        float initialDamage = CalculateInitialDamage(basedam, attacker.attack);
        string guarded = "";
        if(attackSO.height == defendSO.height){
            //Guard
            totalDamage = TotalDam(initialDamage, 1-defendSO.damageReductionMultiplier);
            guarded = " and guarded";
        }
        else
        {
            //No guard
            totalDamage = TotalDam(initialDamage, 1);

        }
        //Debug.Log($"basedam: {basedam}, initialDamage: {initialDamage}, total damage: {totalDamage}");
        
        if(defendSO.block && attackSO.height == defendSO.height && attackSO.moveType != MoveType.Grapple) 
        {
            totalDamage = 0;

            analytics.RegisterDefendSuccess();
            return "blocked";
        }
        if (defendSO.deflect && attackSO.moveType != MoveType.Grapple)
        {
            if (attacker.healthSystem.TakeDamage(totalDamage)) //Add Calculation on totalDamage
            {
                RunningIsFalse?.Invoke();
            }
            ApplyEffects(attacker, attackSO);

            analytics.RegisterDefendSuccess();
            return "deflected";
        }
        else
        {
            if (target.healthSystem.TakeDamage(totalDamage))
            {
                RunningIsFalse?.Invoke();
            }
            ApplyEffects(target, attackSO);
            analytics.RegisterAttackSuccess();
            return "hit" + guarded;

        }
        /*if ((defendSO.deflect && attacker.healthSystem.TakeDamage(totalDamage)) || target.healthSystem.TakeDamage(totalDamage)) //Add Calculation on totalDamage
        {
            running = false;
        }*/

    }
    #region  Effects

    private void ApplyEffects(Character character, AttackSO attackSO)
    {
        foreach(EffectChance eC in attackSO.effects)
        {
            if (RandomEvent(GetEffectChance(eC.chance)))
            {
                character.AddEffect(eC.effect);

                //string effectName = eC.effect.name; // once we figure out the effects.
                //analytics.RegisterEffectApplied(effectName);
            }
        }
    }

    private static readonly float[] chances = { 0.2f, 0.3f, 0.4f, 0.55f, 0.7f, 1f };

    private float GetEffectChance(Scale chance)
    {
        return chances[(int)chance]; //Only works as Scale is ordered.
    }
    #endregion
    #region Move Accuracy
    private bool RandomEvent(float moveAccuracy)
    {
        return UnityEngine.Random.value < moveAccuracy;
    }

    private float CalculateAccuracyEvasionMultiplier(float attackerAccuracy, float defenderEvasion)
    {
        return (attackerAccuracy-defenderEvasion) / 2f;
    }
    private float CalculateMoveAccuracy(Accuracy accuracy, float attackerAccuracy, float defenderEvasion, float dodgeBonusPercent)
    {
        float baseAccuracy = GetBaseAccuracy(accuracy) * 100f;
        return Mathf.Round(baseAccuracy + CalculateAccuracyEvasionMultiplier(attackerAccuracy, defenderEvasion) - dodgeBonusPercent) / 100f;
    }
    private static readonly float[] accuracyValues = { 0.4f, 0.65f, 0.8f, 0.88f, 0.95f};

    private float GetBaseAccuracy(Accuracy accuracy) //Not implemented completely, add actual accuracy values
    {
        return accuracyValues[(int)accuracy];

    }
    #endregion
    #region Damage Calculation
    private float damageMultiplier = 1f;
    private static readonly float[] damageValues = { 3f, 5f, 7f, 8.5f, 10f };

    private int GetDamage(Scale damage)
    {
        return Mathf.RoundToInt(damageValues[(int)damage] * damageMultiplier);
    }
    
    private float CalculateAttackMultiplier(int x)
    {
        float y = (float)x;
        float ans = (y*y / 1960f) + (5f / 196f * y) + 34f/49f;
        return ans;
    }
    private float CalculateInitialDamage(int basedam, int attack)
    {
        float attackMultiplier = CalculateAttackMultiplier(attack);
        return basedam * attackMultiplier;
    }
    //(where grdred is 1 or 0.6 or 0)
    private int TotalDam(float initdam, float multiplier)
    {
        float rand = UnityEngine.Random.Range( -0.15f*initdam, 0.15f*initdam );
        float ans = multiplier * ( initdam + rand  );
        int roundedAns = (int)Mathf.Round( ans );
        return roundedAns;

    }
    #endregion
}