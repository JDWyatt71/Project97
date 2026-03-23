using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Services.Core;
using Unity.Services.Analytics;
using Unity.Services.Authentication;
using Unity.Services.Core.Environments;

using AnalyticsEvent = Unity.Services.Analytics.Event;

public class AnalyticsManager : MonoBehaviour
{
    public static AnalyticsManager Instance { get; private set; }

    bool _initialized;
    string _sessionId;

    const float FLUSH_INTERVAL = 2f;

    private Queue<AnalyticsEvent> _eventQueue = new Queue<AnalyticsEvent>();

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
        Debug.Log("The analytics is being intialized.");
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

            AuthenticationService.Instance.SignedIn += () =>
            {
                Debug.Log($"SIGNED IN! PlayerID: {AuthenticationService.Instance.PlayerId}");
            };
            AuthenticationService.Instance.SignInFailed += (err) =>
            {
                Debug.LogError($"Sign‑in failed: {err}");
            };

            Debug.Log("Signing in...");
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Signed in");

            _sessionId = $"session_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
            _initialized = true;
            while (_eventQueue.Count > 0)
            {
                AnalyticsService.Instance.RecordEvent(_eventQueue.Dequeue());
            }

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
        if (!_initialized)
        {
            _eventQueue.Enqueue(e);
            return;
        }
        AnalyticsService.Instance.RecordEvent(e);
    }

    System.Collections.IEnumerator FlushRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(FLUSH_INTERVAL);
            try
            {
                AnalyticsService.Instance.Flush();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Analytics] Flush failed: {e}");
            }
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

    public void TrackRunStart(string runId, string difficulty, float runStartTime, string sessionId)
    {
        Record(new RunStartEvent(runId, difficulty, runStartTime, sessionId));
    }

    public void TrackRunEnd(RunResult r)
    {
        Record(new RunEndEvent(r));
    }

    public void TrackFightStart(string id, float time, string sessionId)
    {
        Record(new FightStartEvent(id, time, sessionId));
    }

    public void TrackFightEnd(FightResult f)
    {
        Record(new FightEndEvent(f));
    }

    public void TrackItemUsed(string itemName)
    {
        Record(new ItemUsedEvent(itemName));
    }

    public void TrackItemBought(string itemName)
    { 
        Record(new ItemBoughtEvent(itemName));
    }

    public void TrackUpgradeChosen(int level, string type, string value, string run_id)
    {
        Record(new UpgradeChosenEvent(level, type, value, run_id));
    }

    public void TrackMoveUsed(string moveName, string userType, string target, string attackResult, int damage, string sessionId)
    {
        Record(new MoveUsedEvent(moveName, userType, target, attackResult, damage, sessionId));
    }

    public void TrackStatusApplied(string statusName, string sessionId, string targetType, string sourceMove = "")
    {
        Record(new StatusAppliedEvent(statusName, sessionId, targetType, sourceMove));
    }
}

#region Typed Analytics Events

abstract class GameAnalyticsEvent : AnalyticsEvent
{
    protected GameAnalyticsEvent(string name) : base(name)
    {
        /*SetParameter("my_session_id", sessionId);*/
    }
}

// ---------------- RUN ----------------
class RunStartEvent : GameAnalyticsEvent
{
    public RunStartEvent(string runId, string difficulty, float runStartTime, string sessionId)
        : base("run_start")
    {
        SetParameter("run_id", runId);
        SetParameter("difficulty", difficulty);
        SetParameter("start_time", runStartTime);
        SetParameter("sessionId", sessionId);
    }
}

class RunEndEvent : GameAnalyticsEvent
{
    public RunEndEvent(RunResult r)
        : base("run_end")
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
        SetParameter("sessionId", r.sessionID);
    }
}

// ---------------- FIGHT ----------------
class FightStartEvent : GameAnalyticsEvent
{
    public FightStartEvent(string fightId, float time, string sessionId)
        : base("fight_start")
    {
        SetParameter("fight_id", fightId);
        SetParameter("start_time", time);
        SetParameter("sessionId", sessionId);
    }
}
class FightEndEvent : GameAnalyticsEvent
{
    public FightEndEvent(FightResult f)
        : base("fight_end")
    {
        SetParameter("fight_id", f.FightId);
        SetParameter("battle_time", f.BattleTimeSeconds);
        SetParameter("turns", f.Turns);
        SetParameter("attack_attempts", f.AttackAttempts);
        SetParameter("attack_success", f.AttackSuccess);
        SetParameter("defend_attempts", f.DefendAttempts);
        SetParameter("defend_success", f.DefendSuccess);
        SetParameter("hp_left", f.HpLeft);
        SetParameter("player_died", f.playerDied);
        SetParameter("level", f.level);
        SetParameter("sessionId", f.sessionId);
    }
}

// ---------------- ITEMS ----------------
class ItemUsedEvent : GameAnalyticsEvent
{
    public ItemUsedEvent(string itemName)
        : base("item_used")
    {
        SetParameter("item_name", itemName);
    }
}

class ItemBoughtEvent : GameAnalyticsEvent
{
    public ItemBoughtEvent(string itemName)
        : base("item_bought")
    {
        SetParameter("item_name", itemName);
    }
}

// ---------------- UPGRADE ----------------
class UpgradeChosenEvent : GameAnalyticsEvent
{
    public UpgradeChosenEvent(int level, string type, string value, string run_id)
        : base("upgrade_chosen")
    {
        SetParameter("level", level);
        SetParameter("type", type);
        SetParameter("value", value);
        SetParameter("run_id", run_id);
    }
}

// ---------------- MOVE USED ----------------
class MoveUsedEvent : GameAnalyticsEvent
{
    public MoveUsedEvent(string moveName, string sessionID, string userType, string attackResult, int damage, string targetType = "")
        : base("move_used")
    {
        SetParameter("move_name", moveName);
        SetParameter("user_type", userType);       // "player" or "enemy"
        SetParameter("attackRes", attackResult);
        SetParameter("totalDamage", damage);
        if (!string.IsNullOrEmpty(targetType))
            SetParameter("target_type", targetType); // optional
        SetParameter("sessionId", sessionID);
    }
}

// ---------------- STATUS ----------------
class StatusAppliedEvent : GameAnalyticsEvent
{
    public StatusAppliedEvent(string statusName, string sessionId, string targetType, string sourceMove = "")
        : base("status_applied")
    {
        SetParameter("status_name", statusName);   // e.g., "bleed", "stun", "poison"
        SetParameter("sessionId", sessionId);
        SetParameter("target_type", targetType);   // "player" or "enemy"
        if (!string.IsNullOrEmpty(sourceMove))
            SetParameter("source_move", sourceMove);
    }
}

#endregion
