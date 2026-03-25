using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine.UI;
public class CombatManager
{
    private FightAnalyticsTracker analytics;
    public CombatManager(FightAnalyticsTracker analytics)
    {
        this.analytics = analytics;
    }
    public void PerformMovePair(AttackSO a, DefendSO d, Character attacker, Character target, string turnName)
    {
        AttackResult status = PerformAttack(attacker, target, a, d);
        string strStatus = status.ToString();
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

        Debug.Log($"{turnName}'s Turn:\n{aS}\n{dS}\nAttack {strStatus}");
        CombatEvents.RaiseLogUpdate($"{turnName} used {a.name}! {strStatus.ToUpper()}!");
    }
    public enum AttackResult
    {
        dodged,
        blocked,
        deflected,
        hit,
        guardedHit,
        ducked
    }
    /// <summary>
    /// Returns AttackResult of what happened with the move: dodge, block, deflect, hit, guarded hit
    /// </summary>
    /// <param name="attacker"></param>
    /// <param name="target"></param>
    /// <param name="attackSO"></param>
    /// <param name="defendSO"></param>
    /// <returns></returns>
    private AttackResult PerformAttack(Character attacker, Character target, AttackSO attackSO, DefendSO defendSO = null)
    {
        string moveName = attackSO.name;
        analytics.RegisterAttackAttempt();

        if (defendSO == null)
        {
            defendSO = AssetsDatabase.I.defaultDefendSO;
        }

        //Is guarded?
        int totalDamage;
        int basedam = GetDamage(attackSO.damage);
        float initialDamage = CalculateInitialDamage(basedam, attacker.attack);
        bool guarded = false;
        if(attackSO.height == defendSO.height && !attackSO.ignoresGuard){ //Guard
            totalDamage = TotalDam(initialDamage, 1-defendSO.damageReductionMultiplier);
            guarded = true;
        }
        else //No guard
        {          
            totalDamage = TotalDam(initialDamage, 1);
        }

        //Is dodge / duck
        float moveAccuracy = CalculateMoveAccuracy(attackSO.accuracy, attacker.accuracy, target.evasion, defendSO.dodgeBonusPercent);
        if (!UC.RandomEvent(moveAccuracy))
        {
            //Check catches dodge
            bool caughtDodge = attackSO.catchesDodge && UC.RandomEvent(GetEffectChance(attackSO.catchesDodgeChance));
            if(!caughtDodge){
                if (defendSO.duck)
                {
                    //Ducked successfully 
                    ApplyAttackDamageAndEffects(attacker, attackSO, totalDamage);
                    
                    analytics.RegisterDefendSuccess();

                    GameEvents.RaiseMoveUsed(moveName, GameManager.I.CurrentSessionId, attacker.ToString(), AttackResult.ducked.ToString(), totalDamage, target.ToString());
                    return AttackResult.ducked;
                } 
                else
                {
                    GameEvents.RaiseMoveUsed(moveName, GameManager.I.CurrentSessionId, attacker.ToString(), AttackResult.dodged.ToString(), 0, target.ToString());
                    return AttackResult.dodged; //So don't do any damage.
                }
            }
            
        }

        //Is block?
        if(defendSO.block && attackSO.height == defendSO.height && attackSO.moveType != MoveType.Grapple) 
        {
            analytics.RegisterDefendSuccess();
            GameEvents.RaiseMoveUsed(moveName, GameManager.I.CurrentSessionId, attacker.ToString(), AttackResult.blocked.ToString(), totalDamage, target.ToString());
            return AttackResult.blocked;
        }

        //Is deflected/countered?
        if (defendSO.deflect && attackSO.moveType != MoveType.Grapple)
        {
            ApplyAttackDamageAndEffects(attacker, attackSO, totalDamage);

            analytics.RegisterDefendSuccess();
            GameEvents.RaiseMoveUsed(moveName, GameManager.I.CurrentSessionId, attacker.ToString(), AttackResult.deflected.ToString(), totalDamage, target.ToString());
            return AttackResult.deflected;
        }
        else //Not deflected, hit - is it guardedHit or standard hit
        {
            ApplyAttackDamageAndEffects(target, attackSO, totalDamage);

            analytics.RegisterAttackSuccess();
            if (guarded)
            {
                GameEvents.RaiseMoveUsed(moveName, GameManager.I.CurrentSessionId, attacker.ToString(), AttackResult.guardedHit.ToString(), totalDamage, target.ToString());
                return AttackResult.guardedHit;
            } else
            {
                GameEvents.RaiseMoveUsed(moveName, GameManager.I.CurrentSessionId, attacker.ToString(), AttackResult.hit.ToString(), totalDamage, target.ToString());
                return AttackResult.hit;
            }
        }
    }

    private void ApplyAttackDamageAndEffects(Character target, AttackSO attackSO, int totalDamage)
    {
        if (attackSO.height == target.TryGetEffect(Effect.BrokenBones)?.height) //If has broken bones at same height as attack
        {
            totalDamage = Mathf.CeilToInt(totalDamage * 1.15f);
        }
        target.healthSystem.TakeDamage(totalDamage);
        CombatEvents.RaiseDamageDealt(totalDamage, target);

        ApplyEffects(target, attackSO, attackSO.height);
    }
    #region Effects

    private void ApplyEffects(Character character, AttackSO attackSO, Scale moveHeight)
    {
        foreach(EffectChance eC in attackSO.effects)
        {
            if (UC.RandomEvent(GetEffectChance(eC.chance)))
            {
                character.AddEffect(eC.effect, moveHeight);

                GameEvents.RaiseStatus(eC.effect.ToString(), GameManager.I.CurrentSessionId, character.ToString(), attackSO.name.ToString());

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
    private float CalculateAccuracyEvasionMultiplier(float attackerAccuracy, float defenderEvasion)
    {
        return (attackerAccuracy-defenderEvasion) / 2f;
    }
    private float CalculateMoveAccuracy(Accuracy accuracy, float attackerAccuracy, float defenderEvasion, float dodgeBonusPercent)
    {
        float baseAccuracy = GetBaseAccuracy(accuracy) * 100f;
        return Mathf.Round(baseAccuracy + CalculateAccuracyEvasionMultiplier(attackerAccuracy, defenderEvasion) - dodgeBonusPercent) / 100f;
    }
    private static readonly float[] accuracyValues = { 0.4f, 0.65f, 0.8f, 0.88f, 0.95f, 1f};

    private float GetBaseAccuracy(Accuracy accuracy)
    {
        return accuracyValues[(int)accuracy];
    }
    #endregion
    #region Damage Calculation
    private float damageMultiplier = 1f;
    private static readonly float[] damageValues = { 3f, 5f, 7f, 8.5f, 10f, 0f, 0f, 13f /*Very high*/, 1.5f /*Very low*/ }; 
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