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
    public void AddAMoves(params AttackSO[] attackSOs) //Automatically wraps single item to array
    {
        aMoves.AddRange(attackSOs);
    }
    private List<DefendSO> dMoves; //Defensive moves pool

    public List<DefendSO> GetDMoves()
    {
        return dMoves;
    }
    public void AddDMoves(params DefendSO[] defendSOs) 
    {
        dMoves.AddRange(defendSOs);
    }
    public List<MoveSO> GetAllMoves()
    {
        List<MoveSO> allMoves = new List<MoveSO>(aMoves);
        allMoves.AddRange(dMoves);
        return allMoves;
    }
    private int baseActionPoints;
    public int actionPoints {private set; get;}
    public void ChangeActionPoints(int amount)
    {
        baseActionPoints += amount;
        baseActionPoints = Mathf.Max(10, baseActionPoints); //Max AP is 10
    }
    private Inventory inventory;
    private int baseAttack;
    public int attack;
    public void ChangeAttack(int amount)
    {
        baseAttack += amount;
    }
    private float baseAccuracy;
    public float accuracy {private set; get;}
    public void ChangeAccuracy(float amount)
    {
        baseAccuracy += amount;
    }
    private float baseEvasion;
    public float evasion {private set; get;}
    public void ChangeEvasion(float amount)
    {
        baseEvasion += amount;
    }



    public int bonusAttack=0;
    public void ChangeBonusAttack(int amount)
    {
        bonusAttack += amount;
    }
    private float bonusAccuracy=0;
    public void ChangeBonusAccuracy(float amount)
    {
        bonusAccuracy += amount;
    }
    private float bonusEvasion=0;
    public void ChangeBonusEvasion(float amount)
    {
        bonusEvasion += amount;
    }
    public int bonusActionPoints = 0;
    public void ChangeBonusAP(int amount)
    {
        bonusActionPoints += amount;
    }



    public int GetAttack()
    {
        return attack;
    }
    public CharacterSO cSO {private set; get;}
    public int restAction {private set; get;}
    public void TryUseRestAction()
    {
        if(restAction > 0)
        {
            restAction -= 1;
            healthSystem.Heal(Mathf.CeilToInt(healthSystem.GetMaxHealth() * 0.03f)); //Heal 3% of max health
        }
    }
    public void ResetRestActions()
    {
        restAction = 3;
    }
    public void Setup(CharacterSO characterSO = default(CharacterSO))
    {
        cSO = characterSO;

        healthSystem = GetComponent<HealthSystem>();
        healthSystem.Setup(characterSO.hitPoints);
        SetupMoves(characterSO.aMoves, characterSO.dMoves);
        //inventory.SetupInventory(Difficulty difficulty);
        baseAttack = characterSO.attack;
        baseAccuracy = characterSO.accuracy;
        baseEvasion = characterSO.evasion;
        baseActionPoints = characterSO.actionPoints;
        //ResetBonusStats();

        ResetCurrentStats();
        ResetRestActions();
    }

    private void ResetCurrentStats()
    {
        accuracy = baseAccuracy + bonusAccuracy;
        evasion = baseEvasion + bonusEvasion;
        actionPoints = baseActionPoints + bonusActionPoints;
        attack = baseAttack + bonusAttack;
    }

    public void ResetBonusStats()
    {
        bonusAccuracy = 0;
        bonusAttack = 0;
        bonusActionPoints = 0;
        bonusEvasion = 0;
    }
    private void TrySetActionPoints(int amount)
    {
        //Prone effect sets action points to 0 to skip turn, therefore if turn has been skipped do not update action points from another effect.
        if(actionPoints != 0) actionPoints = amount;  
    }
    private void SetupMoves(List<AttackSO> initialAMoves, List<DefendSO> initialDMoves)
    {
        //Copy the character's starting moves to this character
        aMoves = new List<AttackSO>(initialAMoves);
        dMoves = new List<DefendSO>(initialDMoves);

    }
    #region Effects
    private Dictionary<Effect, EffectData> effects = new Dictionary<Effect, EffectData>();
    public void RemoveAllEffects()
    {
        effects = new Dictionary<Effect, EffectData>();
    }
    public void AddEffect(Effect effect, Scale height)
    {
        effects[effect] = new EffectData(EffectDefaults.Durations[effect], height);
        if(effect == Effect.Bind)
        {
            bindDPercentage = 0.03f;
        }
    }
    public EffectData TryGetEffect(Effect effect)
    {
        return effects.TryGetValue(effect, out EffectData data) ? data : null;
    }
    /// <summary>
    /// At start of each turn, TurnManager calls for each Character
    /// </summary>
    public void HealEffects()
    {
        effects.Clear();
    }
    public void DoEffects(float binderAttack)
    {
        ResetCurrentStats();
        foreach (Effect effect in effects.Keys.ToList()) //Loops through a copy, so safe to modify dictionary in this loop
        {
            DoEffect(effect);
            effects[effect].duration -= 1;
            if (effects[effect].duration == 0) //Doesn't remove -1 or below which signifies unlimited
            {
                effects.Remove(effect);
                
            }
            else if(effect == Effect.Bind && effects[effect].duration <= 2)
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
                TrySetActionPoints(actionPoints+2);
                break;

            case Effect.Enraged:
                accuracy -= 0.2f * baseAccuracy;
                attack += Mathf.CeilToInt(0.1f * baseAttack);
                break;

            case Effect.Blindness:
                accuracy -= 0.1f * baseAccuracy;
                evasion -= 0.1f * baseEvasion;
                break;

            case Effect.Slow:
                evasion -= 0.15f * baseEvasion;
                break;

            case Effect.Bind:
                healthSystem.TakeDamage(Mathf.CeilToInt(bindDPercentage * healthSystem.GetMaxHealth()));
                bindDPercentage += 0.03f;
                break;

            case Effect.Wind:
                TrySetActionPoints(Mathf.CeilToInt(baseActionPoints / 3));
                break;

            case Effect.Prone:
                TrySetActionPoints(0);
                break;

            case Effect.BrokenBones:
                //Does nothing at start of turn
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
    #endregion
}
