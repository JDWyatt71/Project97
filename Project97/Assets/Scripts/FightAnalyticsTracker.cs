using UnityEngine;
using System;
using System.Collections.Generic;

public class FightAnalyticsTracker
{
    private FightResult result;

    public float fightStartTime;
    private int currentTurn;

    public void StartFight(string fightId)
    {
        result = new FightResult();
        result.FightId = fightId;

        result.moves = new Dictionary<string, int>();
        result.status = new Dictionary<string, int>();

        fightStartTime = Time.time;
        currentTurn = 0;
    }

    public FightResult EndFight(int hpLeft)
    {
        result.BattleTimeSeconds = Mathf.RoundToInt(Time.time - fightStartTime);
        result.Turns = currentTurn;
        result.HpLeft = hpLeft;

        return result;
    }

    public void RegisterTurn()
    {
        currentTurn++;
    }

    public void RegisterMoveUsed(string moveName)
    {
        if (!result.moves.ContainsKey(moveName))
        {
            result.moves[moveName] = 0;
        }
        result.moves[moveName]++;
    }

    public void RegisterAttackAttempt()
    {
        result.AttackAttempts++;
    }

    public void RegisterAttackSuccess()
    {
        result.AttackSuccess++;
    }

    public void RegisterDefendAttempt()
    {
        result.DefendAttempts++;
    }

    public void RegisterDefendSuccess()
    {
        result.DefendSuccess++;
    }

    public void RegisterEffectApplied(string effectName)
    {
        if (result.status.ContainsKey(effectName))
        {
            result.status[effectName] = 0;
        }
        result.status[effectName]++;
    }
}
