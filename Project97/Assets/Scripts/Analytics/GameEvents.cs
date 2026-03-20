using UnityEngine;
using System;
using System.Globalization;

public static class GameEvents
{

    public static event Action<string, string, float> RunStarted;
    public static event Action<RunResult> RunEnded;

    public static event Action<string, float> FightStarted;
    public static event Action<FightResult> FightEnded;

    public static event Action<string, string, string> MoveUsed;

    public static event Action<int, string, string> UpgradeChosen;

    public static event Action<string, string, string> StatusApplied;

    public static event Action<string> ItemBought;
    public static event Action<string> ItemUsed;


    public static void RaiseRunStarted(string id,string difficulty, float time) => RunStarted?.Invoke(id, difficulty, time);
    public static void RaiseRunEnded(RunResult r) => RunEnded?.Invoke(r);
    public static void RaiseFightStarted(string id, float time) => FightStarted?.Invoke(id, time);
    public static void RaiseFightEnded(FightResult f) => FightEnded?.Invoke(f);
    public static void RaiseMoveUsed(string move, string user, string target) => MoveUsed?.Invoke(move, user, target);
    public static void RaiseUpgradeChosen(int level, string type, string value) => UpgradeChosen?.Invoke(level, type, value);
    public static void RaiseStatus(string status, string target, string source) => StatusApplied?.Invoke(status, target, source);
    public static void RaiseItemBought(string name) => ItemBought?.Invoke(name);
    public static void RaiseItemUsed(string name) => ItemUsed?.Invoke(name);
}
