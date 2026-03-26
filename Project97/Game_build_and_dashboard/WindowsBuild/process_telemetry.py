import sys
import json
import sqlite3
from datetime import datetime
from pathlib import Path
import pandas as pd

DB_PATH = Path(__file__).parent / "telemetry.db"

def create_tables(conn):
    cursor = conn.cursor()

    cursor.execute("""
    CREATE TABLE IF NOT EXISTS sessions (
        session_id TEXT,
        start_time TEXT,
        end_time TEXT,
        total_play_time REAL
    )
    """)

    cursor.execute("""
    CREATE TABLE IF NOT EXISTS runs (
        run_id TEXT,
        sessionId TEXT,
        difficulty TEXT,
        start_time REAL,
        end_time REAL,
        duration INTEGER,
        successful INTEGER,
        level_finish INTEGER,
        death_cause TEXT,
        hp_left INTEGER
    )
    """)

    cursor.execute("""
    CREATE TABLE IF NOT EXISTS fights (
        fight_id TEXT,
        run_id TEXT,
        battle_time REAL,
        turns INTEGER,
        attack_attempts INTEGER,
        attack_success INTEGER,
        defend_attempts INTEGER,
        defend_success INTEGER,
        hp_left INTEGER,
        player_died BOOLEAN,
        level INTEGER
    )
    """)

    cursor.execute("""
    CREATE TABLE IF NOT EXISTS upgrades (
        timestamp TEXT,
        level INTEGER,
        type TEXT,
        value TEXT,
        run_id TEXT
    )
    """)

    cursor.execute("""
    CREATE TABLE IF NOT EXISTS moves (
        timestamp TEXT,
        move_name TEXT,
        user_type TEXT,
        attack_result TEXT,
        total_damage INTEGER,
        target_type TEXT,
        session_id TEXT,
    )
    """)

    cursor.execute("""
    CREATE TABLE IF NOT EXISTS status_effects (
        timestamp TEXT,
        status_name TEXT
    )
    """)

    cursor.execute("""
    CREATE TABLE IF NOT EXISTS items (
        timestamp TEXT,
        item_name TEXT)""")

    conn.commit()

def process_json(file_path):
    runs_df = []
    with open(file_path, "r") as f:
        data = json.load(f)

        runs = []
        fights = []
        timestamps = []

        for event in data:
            d = event["data"]
            ts = event.get("timestamp_utc")

            if ts:
                timestamps.append(datetime.fromisoformat(ts))

            if event["event_type"] == "run_end":
                runs.append({
                    "run_id": d.get("run_id"),
                    "difficulty": d.get("difficulty"),
                    "start_time": d.get("start_time"),
                    "end_time": d.get("end_time"),
                    "duration": d.get("duration"),
                    "successful": int(d.get("successful", False)),
                    "level_finish": d.get("level_finish"),
                    "death_cause": d.get("death_cause"),
                    "hp_left": d.get("hp_left"),
                    "session_id": d.get("sessionId") 
                })

            elif event["event_type"] == "fight_end":
                fights.append({
                    "fight_id": d.get("fight_id"),
                    "run_id": d.get("run_id"),
                    "battle_time": d.get("battle_time"),
                    "turns": d.get("turns"),
                    "attack_attempts": d.get("attack_attempts"),
                    "attack_success": d.get("attack_success"),
                    "defend_attempts": d.get("defend_attempts"),
                    "defend_success": d.get("defend_success"),
                    "hp_left": d.get("hp_left"),
                    "player_died": d.get("player_died"),
                    "level": d.get("level"),
                    "sessionId": d.get("sessionId")
                })
        
        runs_df = pd.DataFrame(runs)
        fights_df = pd.DataFrame(fights)

        issues = []
        if not fights_df.empty():
            issues.extend(validate_event_fights(fights_df))
        if not runs_df.empty():
            issues.extend(validate_event_runs(runs_df))

        if issues:
            print("Data validation failed:")
            for issue in issues:
                print("-", issue)
            return # STOP precessing since there is dad data.

    if timestamps:
        start_time = min(timestamps)
        end_time = max(timestamps)
        total_play_time = (end_time - start_time).total_seconds()
    else:
        start_time = end_time = None
        total_play_time = 0

    conn = sqlite3.connect(DB_PATH)
    create_tables(conn)
    cursor = conn.cursor()

    session_id = runs_df["session_id"].iloc[0] if not runs_df.empty else None

    cursor.execute("""
        INSERT INTO sessions VALUES (?, ?, ?, ?)
        """, (
            session_id,
            start_time.isoformat() if start_time else None,
            end_time.isoformat() if end_time else None,
            total_play_time
        ))

    for event in data:
        event_type = event["event_type"]
        timestamp = event["timestamp_utc"]
        d = event["data"]

        if event_type == "run_end":
            cursor.execute("""
            INSERT INTO runs VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            """, (
                d.get("run_id"),
                d.get("sessionId"),
                d.get("difficulty"),
                d.get("start_time"),
                d.get("end_time"),
                d.get("duration"),
                int(d.get("successful", False)),
                d.get("level_finish"),
                d.get("death_cause"),
                d.get("hp_left")
            ))

        elif event_type == "fight_end":
            cursor.execute("""
            INSERT INTO fights VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            """, (
                d.get("fight_id"),
                d.get("run_id"),
                d.get("battle_time"),
                d.get("turns"),
                d.get("attack_attempts"),
                d.get("attack_success"),
                d.get("defend_attempts"),
                d.get("defend_success"),
                d.get("hp_left"),
                d.get("player_died"),
                d.get("level")
            ))

        elif event_type == "upgrade_chosen":
            cursor.execute("""
            INSERT INTO upgrades VALUES (?, ?, ?, ?, ?)
            """, (
                timestamp,
                d.get("level"),
                d.get("type"),
                d.get("value"),
                d.get("run_id")
            ))
        elif event_type == "move_used":
            cursor.execute("INSERT INTO moves VALUES (?, ?, ?, ?, ?, ?, ?)", (
                timestamp,
                d.get("move_name"),
                d.get("user_type"),
                d.get("attack_result"),
                d.get("total_damage"),
                d.get("target_type"),
                d.get("sessionId")
            ))

        elif event_type == "status_applied":
            cursor.execute("INSERT INTO status_effects VALUES (?, ?)", (
                timestamp,
                d.get("status_name")
            ))


    conn.commit()
    conn.close()
    print(f"Processed {file_path} into database.")

def validate_event_fights(fights_df):
    issues = []

    if fights_df.empty:
        return issues

    if fights_df["fight_id"].isnull().any():
        issues.append("Missing fight_id")

    if (fights_df["battle_time"] < 0).any():
        issues.append("Negative battle time")

    if (fights_df["attack_success"] > fights_df["attack_attempts"]).any():
        issues.append("Invalid attack values in fight data")

    if (fights_df["defend_success"] > fights_df["defend_attempts"]).any():
        issues.append("Invalid defend values in fight data")

    if ((fights_df["attack_attempts"] == 0) & (fights_df["attack_success"] > 0)).any():
        issues.append("Attack success figures with zero attempts")
    
    if fights_df["turns"].isnull().any():
        issues.append("Missing turns in fight")

    if (fights_df["turns"] <= 0).any():
        issues.append("Invalid turns (<= 0)")

    if (fights_df["hp_left"] < 0).any():
        issues.append("Negative HP in fights")

    if (fights_df["level"] < 0).any():
        issues.append("Invalid level in fights")

    if fights_df["player_died"].isnull().any():
        issues.append("Missing player_died flag")

    if ((fights_df["player_died"] == True) & (fights_df["hp_left"] > 0)).any():
        issues.append("Player died but Hp is greater than 0")

    return issues

def validate_event_runs(runs_df):
    issues = []

    if runs_df.empty():
        return issues

    if runs_df["run_id"].isnull().any():
        issues.append("Missing run_id")

    if (runs_df["duration"] < 0).any():
        issues.append("Negative run duration")

    if (runs_df["hp_left"] < 0).any():
        issues.append("Negative HP left in runs")

    if (runs_df["level_finish"] < 0).any():
        issues.append("Invalid level_finish")

    if ((runs_df["start_time"].notnull()) & (runs_df["end_time"].notnull()) & (runs_df["start_time"] > runs_df["end_time"])).any():
        issues.append("Run start_time after end_time")

    return issues

#not needed, first attempt at validation, keeping, might need later.
def validate_data(runs_df, fights_df):
    issues = []

    #fight checks
    if fights_df["fight_id"].isnull().any():
        issues.append("Missing fight_id")

    if (fights_df["battle_time"] < 0).any():
        issues.append("Negative battle time")

    if (fights_df["attack_success"] > fights_df["attack_attempts"]).any():
        issues.append("Invalid attack values in fight data")

    if (fights_df["defend_success"] > fights_df["defend_attempts"]).any():
        issues.append("Invalid defend values in fight data")

    if ((fights_df["attack_attempts"] == 0) & (fights_df["attack_success"] > 0)):
        issues.append("Attack success figures with zero attempts")
    
    if fights_df["turns"].isnull().any():
        issues.append("Missing turns in fight")

    if (fights_df["turns"] <= 0).any():
        issues.append("Invalid turns (<= 0)")

    if (fights_df["hp_left"] < 0).any():
        issues.append("Negative HP in fights")

    if (fights_df["level"] < 0).any():
        issues.append("Invalid level in fights")

    if fights_df["player_died"].isnull().any():
        issues.append("Missing player_died flag")

    #logic check
    if ((fights_df["player_died"] == True) & (fights_df["hp_left"] > 0)).any():
        issues.append("Player died but Hp is greater than 0")

    #runs checks
    if runs_df["run_id"].isnull().any():
        issues.append("Missing run_id")

    if (runs_df["duration"] < 0).any():
        issues.append("Negative run duration")

    if (runs_df["hp_left"] < 0).any():
        issues.append("Negative HP left in runs")

    if (runs_df["level_finish"] < 0).any():
        issues.append("Invalid level_finish")

    if (runs_df["start_time"] > runs_df["end_time"]).any():
        issues.append("Run start_time after end_time")

    return issues

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("No JSON file provided.")
        sys.exit(1)

    json_file = sys.argv[1]
    process_json(json_file)