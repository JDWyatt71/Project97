using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
public static class UC //Utils Class
{
    public static bool RandomEvent(float moveAccuracy)
    {
        return UnityEngine.Random.value < moveAccuracy;
    }
    public static bool RandomEventPercentage(float moveAccuracy)
    {
        return RandomEvent(moveAccuracy / 100f);
    }
    public static MoveSO GetRandomDefendSO(List<DefendChance> defendChances, List<DefendSO> defendSOs)
    {
        if (defendChances.Count == 0) //If no chances are defined in the scriptable object, then an even chance of each defending move is used for random selection.
        {
            return defendSOs[UnityEngine.Random.Range(0,defendSOs.Count)];
        }

        Dictionary<DefendSO, float> dict = defendChances.ToDictionary(x => x.defendSO, x => x.probability);

        return (MoveSO)GetWeightedRandomItem(dict);
    }
    public static T GetWeightedRandomItem<T>(Dictionary<T, float> dict)
    {
        float totalWeight = 0f;
        foreach (float w in dict.Values) totalWeight += w;
        
        float rand = UnityEngine.Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach(KeyValuePair<T, float> kvp in dict)
        {
            cumulative += kvp.Value;
            if(rand <= cumulative) return kvp.Key;
        }
        Debug.Log($"Weighted random failed! Total Probability: {cumulative}, Rand Value: {rand}");
        return dict.Keys.Last();
    }
    private static readonly float[] attackWeightValues = { 0.5f, 0.15f, 0.35f };

    private static float GetAttackProbability(MoveWeight moveWeight)
    {
        return attackWeightValues[(int)moveWeight];
    }
}