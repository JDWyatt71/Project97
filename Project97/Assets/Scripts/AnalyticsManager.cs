using System;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Analytics;
using Unity.Services.Authentication;
using Unity.Services.Core.Environments;

using AnalyticsEvent = Unity.Services.Analytics.Event;
using System.Collections.Generic;

public class AnalyticsManager : MonoBehaviour
{
    public static AnalyticsManager Instance { get; private set; }

    bool _initialized;
    string _sessionId;

    const float FLUSH_INTERVAL = 30f;

    #region Initialization

    async void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        try
        {
            await InitializeUGS();
        }
        catch (Exception ex)
        {
            Debug.Log($"Awake not working, error: {ex}");
        }
    }

    async System.Threading.Tasks.Task InitializeUGS()
    {
        try
        {


#if UNITY_EDITOR || DEVELOPMENT_BUILD
        var options = new InitializationOptions().SetEnvironmentName("development");
#else
        var options = new InitializationOptions().SetEnvironmentName("production");
#endif

            Debug.Log("Initializing UnityServices...");
            await UnityServices.InitializeAsync(options);
            Debug.Log("UnityServices initialized");

            Debug.Log("Signing in...");
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Signed in");

            _sessionId = $"session_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
            _initialized = true;

            StartCoroutine(FlushRoutine());

            Debug.Log($"Analytics initialized | Session: {_sessionId}");
        }
        catch (Exception ex)
        {
            Debug.Log($"Analytics initialization failed: {ex}");
        }
    }

    void Record(AnalyticsEvent e)
    {
        if (!_initialized) return;
        AnalyticsService.Instance.RecordEvent(e);
    }

    System.Collections.IEnumerator FlushRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(FLUSH_INTERVAL);
            AnalyticsService.Instance.Flush();
        }
    }

    void OnApplicationPause(bool pause)
    {
        if (pause && _initialized)
            AnalyticsService.Instance.Flush();
    }

    void OnApplicationQuit()
    {
        if (_initialized)
            AnalyticsService.Instance.Flush();
    }

    void OnEnable()
    {
        GameEvents.RunStarted -= TrackRunStart;
        GameEvents.RunEnded -= TrackRunEnd;
        GameEvents.FightStarted -= TrackFightStart;
        GameEvents.FightEnded -= TrackFightEnd;
        GameEvents.MoveUsed -= TrackMoveUsed;
        GameEvents.StatusApplied -= TrackStatusApplied;
        GameEvents.ItemBought -= TrackItemBought;


        GameEvents.RunStarted += TrackRunStart;
        GameEvents.RunEnded += TrackRunEnd;
        GameEvents.FightStarted += TrackFightStart;
        GameEvents.FightEnded += TrackFightEnd;
        GameEvents.MoveUsed += TrackMoveUsed;
        GameEvents.StatusApplied += TrackStatusApplied;
        GameEvents.ItemBought += TrackItemBought;
    }

#endregion

    // ================= RUN / FIGHT / ITEM / UPGRADE EVENTS =================

    public void TrackRunStart(string runId, string difficulty, float runStartTime)
    {
        Record(new RunStartEvent(_sessionId, runId, difficulty, runStartTime));
    }

    public void TrackRunEnd(RunResult r)
    {
        Record(new RunEndEvent(_sessionId, r));
    }

    public void TrackFightStart(string id, float time)
    {
        Record(new FightStartEvent(_sessionId, id, time));
    }

    public void TrackFightEnd(FightResult f)
    {
        Record(new FightEndEvent(_sessionId, f));
    }

    public void TrackItemUsed(string itemName)
    {
        Record(new ItemUsedEvent(_sessionId, itemName));
    }

    public void TrackItemBought(string itemName)
    {
        Record(new ItemBoughtEvent(_sessionId, itemName));
    }

    public void TrackUpgradeChosen(int level, string type, string value)
    {
        Record(new UpgradeChosenEvent(_sessionId, level, type, value));
    }

    public void TrackMoveUsed(string moveName, string userType)
    {
        Record(new MoveUsedEvent(_sessionId, moveName, userType));
    }

    public void TrackStatusApplied(string statusName, string targetType, string sourceMove = "")
    {
        Record(new StatusAppliedEvent(_sessionId, statusName, targetType, sourceMove));
    }
}

#region Typed Analytics Events

abstract class GameAnalyticsEvent : AnalyticsEvent
{
    protected GameAnalyticsEvent(string name, string sessionId) : base(name)
    {
        SetParameter("session_id", sessionId);
    }
}

// ---------------- RUN ----------------
class RunStartEvent : GameAnalyticsEvent
{
    public RunStartEvent(string sessionId, string runId, string difficulty, float runStartTime)
        : base("run_start", sessionId)
    {
        SetParameter("run_id", runId);
        SetParameter("difficulty", difficulty);
        SetParameter("start_time", runStartTime);
    }
}

class RunEndEvent : GameAnalyticsEvent
{
    public RunEndEvent(string sessionId, RunResult r)
        : base("run_end", sessionId)
    {
        int duration = Mathf.RoundToInt(r.RunEndTime - r.RunStartTime);

        SetParameter("run_id", r.RunId);
        SetParameter("successful", r.Successful);
        SetParameter("difficulty", r.Difficulty);
        SetParameter("start_time", r.RunStartTime);
        SetParameter("end_time", r.RunEndTime);
        SetParameter("duration", duration);
        SetParameter("level_finish", r.LevelFinish);
        SetParameter("attack_attempts", r.AttackAttempts);
        SetParameter("attack_success", r.AttackSuccess);
        SetParameter("defend_attempts", r.DefendAttempts);
        SetParameter("defend_success", r.DefendSuccess);
        SetParameter("death_cause", r.DeathCause);
        SetParameter("hp_left", r.HpLeft);
    }
}

// ---------------- FIGHT ----------------
class FightStartEvent : GameAnalyticsEvent
{
    public FightStartEvent(string sessionId, string fightId, float time)
        : base("fight_start", sessionId)
    {
        SetParameter("fight_id", fightId);
        SetParameter("start_time", time);
    }
}
class FightEndEvent : GameAnalyticsEvent
{
    public FightEndEvent(string sessionId, FightResult f)
        : base("fight_end", sessionId)
    {
        SetParameter("fight_id", f.FightId);
        SetParameter("battle_time", f.BattleTimeSeconds);
        SetParameter("turns", f.Turns);
        SetParameter("attack_attempts", f.AttackAttempts);
        SetParameter("attack_success", f.AttackSuccess);
        SetParameter("defend_attempts", f.DefendAttempts);
        SetParameter("defend_success", f.DefendSuccess);
        SetParameter("hp_left", f.HpLeft);
    }
}

// ---------------- ITEMS ----------------
class ItemUsedEvent : GameAnalyticsEvent
{
    public ItemUsedEvent(string sessionId, string itemName)
        : base("item_used", sessionId)
    {
        SetParameter("item_name", itemName);
    }
}

class ItemBoughtEvent : GameAnalyticsEvent
{
    public ItemBoughtEvent(string sessionId, string itemName)
        : base("item_bought", sessionId)
    {
        SetParameter("item_name", itemName);
    }
}

// ---------------- UPGRADE ----------------
class UpgradeChosenEvent : GameAnalyticsEvent
{
    public UpgradeChosenEvent(string sessionId, int level, string type, string value)
        : base("upgrade_chosen", sessionId)
    {
        SetParameter("level", level);
        SetParameter("type", type);
        SetParameter("value", value);
    }
}

// ---------------- MOVE USED ----------------
class MoveUsedEvent : GameAnalyticsEvent
{
    public MoveUsedEvent(string sessionId, string moveName, string userType, string targetType = "")
        : base("move_used", sessionId)
    {
        SetParameter("move_name", moveName);
        SetParameter("user_type", userType);       // "player" or "enemy"
        if (!string.IsNullOrEmpty(targetType))
            SetParameter("target_type", targetType); // optional
    }
}

// ---------------- STATUS ----------------
class StatusAppliedEvent : GameAnalyticsEvent
{
    public StatusAppliedEvent(string sessionId, string statusName, string targetType, string sourceMove = "")
        : base("status_applied", sessionId)
    {
        SetParameter("status_name", statusName);   // e.g., "bleed", "stun", "poison"
        SetParameter("target_type", targetType);   // "player" or "enemy"
        if (!string.IsNullOrEmpty(sourceMove))
            SetParameter("source_move", sourceMove);
    }
}

#endregion
