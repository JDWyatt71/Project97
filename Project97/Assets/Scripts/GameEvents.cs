using UnityEngine;
using System;

public static class GameEvents
{

    public static event Action<string, string, float> RunStarted;
    public static event Action<RunResult> RunEnded;

    public static event Action<string, float> FightStarted;
    public static event Action<FightResult> FightEnded;

    public static event Action<string, string> MoveUsed;

    public static event Action<string, string, string> StatusApplied;

    public static event Action<string> ItemBought;


    public static void RaiseRunStarted(string id,string difficulty, float time) => RunStarted?.Invoke(id, difficulty, time);
    public static void RaiseRunEnded(RunResult r) => RunEnded?.Invoke(r);
    public static void RaiseFightStarted(string id, float time) => FightStarted?.Invoke(id, time);
    public static void RaiseFightEnded(FightResult f) => FightEnded?.Invoke(f);
    public static void RaiseMoveUsed(string move, string user) => MoveUsed?.Invoke(move, user);
    public static void RaiseStatus(string status, string target, string source) => StatusApplied?.Invoke(status, target, source);
    public static void RaiseItemBought(string name) => ItemBought?.Invoke(name);
}
