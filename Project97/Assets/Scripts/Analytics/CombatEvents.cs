using UnityEngine;
using System;

public static class CombatEvents
{
    public static event Action<string> OnLogUpdate;
    public static event Action<int, Character> OnDamageDealt; // int: amount of damage, Character: the character who took damage
    public static event System.Action OnTurnStart;

    public static void RaiseLogUpdate(string message)
    {
        OnLogUpdate?.Invoke(message);
    }

    public static void RaiseDamageDealt(int amount, Character character)
    {
        OnDamageDealt?.Invoke(amount, character);
    }

    public static void RaiseTurnStart()
    {
        OnTurnStart?.Invoke();
    }
}
