using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Diagnostics;

public class TelemetryLogger : MonoBehaviour
{
    public static TelemetryLogger Instance { get; private set; }

    private string _sessionId;
    private List<EventEntry> _events = new List<EventEntry>();
    private string _savePath;
    private string exeSave;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _sessionId = $"session_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
        string persistentfolderPath = Path.Combine(Application.persistentDataPath, "Data");
        if (!Directory.Exists(persistentfolderPath))
        {
            Directory.CreateDirectory(persistentfolderPath);
            UnityEngine.Debug.Log($"Created folder for analytics: {persistentfolderPath}");
        }
        _savePath = Path.Combine(persistentfolderPath, $"{_sessionId}-analytics.json");

        string checking = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Data");
        if (!Directory.Exists(checking)){
            Directory.CreateDirectory(checking);
            UnityEngine.Debug.Log($"Created folder for analytics: {checking}");
        }

        exeSave = Path.Combine(Directory.GetParent(Application.dataPath).FullName, $"Data/{_sessionId}_analytics.json");

        UnityEngine.Debug.Log($"JSON Analytics Initialized | Save Path: {_savePath}");
    }

    void OnEnable()
    {
        GameEvents.RunStarted -= TrackRunStart;
        GameEvents.RunEnded -= TrackRunEnd;
        GameEvents.GameQuit -= TrackGameQuit;
        GameEvents.FightStarted -= TrackFightStart;
        GameEvents.FightEnded -= TrackFightEnd;
        GameEvents.StageComplete -= TrackStageComplete;
        GameEvents.StageFail -= TrackStageFail;
        GameEvents.MoveUsed -= TrackMoveUsed;
        GameEvents.StatusApplied -= TrackStatusApplied;
        GameEvents.ItemBought -= TrackItemBought;
        GameEvents.ItemUsed -= TrackItemUsed;
        GameEvents.UpgradeChosen -= TrackUpgradeChosen;

        GameEvents.RunStarted += TrackRunStart;
        GameEvents.RunEnded += TrackRunEnd;
        GameEvents.GameQuit += TrackGameQuit;
        GameEvents.FightStarted += TrackFightStart;
        GameEvents.FightEnded += TrackFightEnd;
        GameEvents.StageComplete += TrackStageComplete;
        GameEvents.StageFail += TrackStageFail;
        GameEvents.MoveUsed += TrackMoveUsed;
        GameEvents.StatusApplied += TrackStatusApplied;
        GameEvents.ItemBought += TrackItemBought;
        GameEvents.ItemUsed += TrackItemUsed;
        GameEvents.UpgradeChosen += TrackUpgradeChosen;
    }

    #region Event Handlers

    private void TrackRunStart(string runId, string difficulty, float runStartTime, string sessionId)
    {
        AddEvent("run_start", new { run_id = runId, difficulty, start_time = runStartTime, sessionId });
    }

    private void TrackGameQuit(string sessionId)
    {
        AddEvent("game_quit", new { sessionId });
    }

    private void TrackRunEnd(RunResult r)
    {
        var duration = Mathf.RoundToInt(r.RunEndTime - r.RunStartTime);
        AddEvent("run_end", new
        {
            run_id = r.RunId,
            successful = r.Successful,
            difficulty = r.Difficulty,
            start_time = r.RunStartTime,
            end_time = r.RunEndTime,
            duration,
            level_finish = r.LevelFinish,
            attack_attempts = r.AttackAttempts,
            attack_success = r.AttackSuccess,
            defend_attempts = r.DefendAttempts,
            defend_success = r.DefendSuccess,
            death_cause = r.DeathCause,
            hp_left = r.HpLeft,
            sessionId = r.sessionID
        });
    }

    private void TrackFightStart(string fightId, float time, string sessionId)
    {
        AddEvent("fight_start", new { fight_id = fightId, stage_id = fightId, start_time = time , sessionId});
    }

    private void TrackFightEnd(FightResult f)
    {
        AddEvent("fight_end", new
        {
            fight_id = f.FightId,
            run_id = f.runID,
            battle_time = f.BattleTimeSeconds,
            turns = f.Turns,
            attack_attempts = f.AttackAttempts,
            attack_success = f.AttackSuccess,
            defend_attempts = f.DefendAttempts,
            defend_success = f.DefendSuccess,
            hp_left = f.HpLeft,
            player_died = f.playerDied,
            level = f.level,
            sessionId = f.sessionId
        });
    }

    private void TrackStageComplete(string fightId, string sessionId)
    {
        AddEvent("stage_complete", new { fight_id = fightId, sessionId });
    }

    public void TrackStageFail(string fightId, string sessionId)
    {
        AddEvent("stage_fail", new { fight_id = fightId, sessionId });
    }

    private void TrackItemUsed(string itemName)
    {
        AddEvent("item_used", new { item_name = itemName });
    }

    private void TrackItemBought(string itemName)
    {
        AddEvent("item_bought", new { item_name = itemName });
    }

    private void TrackUpgradeChosen(int level, string type, string value, string run_id, string sessionId)
    {
        AddEvent("upgrade_chosen", new { level, type, value, run_id, sessionId});
    }

    private void TrackMoveUsed(string moveName, string sessionId , string userType, string attackResult, int damage, string targetType = "")
    {
        AddEvent("move_used", new
        {
            move_name = moveName,
            sessionId = sessionId,
            user_type = userType,
            attack_result = attackResult,
            total_damage = damage,
            target_type = string.IsNullOrEmpty(targetType) ? null : targetType
        });
    }

    private void TrackStatusApplied(string statusName, string sessionId, string targetType, string sourceMove = "")
    {
        AddEvent("status_applied", new
        {
            status_name = statusName,
            sessionId = sessionId,
            target_type = targetType,
            source_move = string.IsNullOrEmpty(sourceMove) ? null : sourceMove
        });
    }

    #endregion

    #region JSON Save

    private void AddEvent(string eventType, object parameters)
    {
        if (!TelemetryConsentManager.IsEnabled())
            return; //BLOCKS EVENTS

        _events.Add(new EventEntry
        {
            event_type = eventType,
            timestamp_utc = DateTime.UtcNow.ToString("o"), // ISO 8601 UTC timestamp
            data = parameters
        });
    }

    public void SaveToJson()
    {
        if (!TelemetryConsentManager.IsEnabled())
            return; //BLOCKS EVENTS

        try
        {
            var json = JsonConvert.SerializeObject(_events, Formatting.Indented);
            File.WriteAllText(_savePath, json);
            UnityEngine.Debug.Log($"Analytics saved to JSON: {_savePath}");
            File.WriteAllText(exeSave, json);
            UnityEngine.Debug.Log($"Analytics saved to JSON: {exeSave}");
            RunPython(_savePath);
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"Failed to save analytics JSON: {ex}");
        }

        void RunPython(string jsonPath)
        {
            // THE PYTHON SCRIPT MUST BE IN THE SAME FOLDER AS THE EXE OF THE GAME TO WORK.
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    //PYTHON MUST BE INSTALED, IF THIS DOESN'T WORK THEN HAVE TO PUT THE PATH TO THE PYTHON EXE.
                    FileName = "python",
                    Arguments = $"process_telemetry.py \"{jsonPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                Process.Start(psi);
                UnityEngine.Debug.LogError("Python script got triggered.");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Failed to run Python script: {ex}");
            }
        }
    }

    void OnApplicationQuit()
    {
        SaveToJson();
    }

    #endregion

    [Serializable]
    private class EventEntry
    {
        public string event_type;
        public string timestamp_utc;
        public object data;
    }
}