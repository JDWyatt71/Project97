using UnityEngine;
using System;
using System.Globalization;

public static class GameEvents
{

    public static event Action<string, string, float, string> RunStarted;
    public static event Action<RunResult> RunEnded;

    public static event Action<string> GameQuit;

    public static event Action<string, float, string> FightStarted;
    public static event Action<FightResult> FightEnded;
    public static event Action<string, string> StageComplete;
    public static event Action<string, string> StageFail;

    public static event Action<string, string, string, string> MoveUsed;

    public static event Action<int, string, string, string, string> UpgradeChosen;

    public static event Action<string, string, string, string> StatusApplied;

    public static event Action<string> ItemBought;
    public static event Action<string> ItemUsed;


    public static void RaiseRunStarted(string id,string difficulty, float time, string sessionId) => RunStarted?.Invoke(id, difficulty, time, sessionId);
    public static void RaiseRunEnded(RunResult r) => RunEnded?.Invoke(r);
    public static void RaiseGameQuit(string sessionId) => GameQuit?.Invoke(sessionId);
    public static void RaiseFightStarted(string id, float time, string sessionId) => FightStarted?.Invoke(id, time, sessionId);
    public static void RaiseFightEnded(FightResult f) => FightEnded?.Invoke(f);
    public static void RaiseStageComplet(string fightID, string sessionId) => StageComplete?.Invoke(fightID, sessionId); 
    public static void RaiseStageFail(string fightID, string sessionId) => StageFail?.Invoke(fightID, sessionId);
    public static void RaiseMoveUsed(string move, string sessionId, string user, string target) => MoveUsed?.Invoke(move, sessionId, user, target);
    public static void RaiseUpgradeChosen(int level, string type, string value, string runId, string sessionId) => UpgradeChosen?.Invoke(level, type, value, runId, sessionId);
    public static void RaiseStatus(string status, string sessionId, string target, string source) => StatusApplied?.Invoke(status, sessionId, target, source);
    public static void RaiseItemBought(string name) => ItemBought?.Invoke(name);
    public static void RaiseItemUsed(string name) => ItemUsed?.Invoke(name);
}
