import sys
import json
import sqlite3
from datetime import datetime
from pathlib import Path

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
        value TEXT
    )
    """)

    cursor.execute("""
    CREATE TABLE IF NOT EXISTS moves (
        timestamp TEXT,
        move_name TEXT,
        user_type TEXT
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
    with open(file_path, "r") as f:
        data = json.load(f)

    conn = sqlite3.connect(DB_PATH)
    create_tables(conn)

    cursor = conn.cursor()

    timestamps = []

    for event in data:
        event_type = event["event_type"]
        timestamp = event["timestamp_utc"]
        d = event["data"]
        ts = event.get("timestamp_utc")
        if ts:
            timestamps.append(datetime.fromisoformat(ts))

        if timestamps:
            start_time = min(timestamps)
            end_time = max(timestamps)
            total_play_time = (end_time - start_time).total_seconds()
        else:
            start_time = end_time = None
            total_play_time = 0

        split = file_path.split("-")
        session_id = split[0]

        cursor.execute("""
        INSERT INTO sessions VALUES (?, ?, ?, ?)
        """, (
            session_id,
            start_time.isoformat() if start_time else None,
            end_time.isoformat() if end_time else None,
            total_play_time
        ))

        if event_type == "run_end":
            cursor.execute("""
            INSERT INTO runs VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
            """, (
                d.get("run_id"),
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
            INSERT INTO fights VALUES (?, ?, ?, ?, ?, ?, ?, ?)
            """, (
                d.get("fight_id"),
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
            INSERT INTO upgrades VALUES (?, ?, ?, ?)
            """, (
                timestamp,
                d.get("level"),
                d.get("type"),
                d.get("value")
            ))
        elif event_type == "move_used":
            cursor.execute("INSERT INTO moves VALUES (?, ?, ?)", (
                timestamp,
                d.get("move_name"),
                d.get("user_type")
            ))

        elif event_type == "status_applied":
            cursor.execute("INSERT INTO status_effects VALUES (?, ?)", (
                timestamp,
                d.get("status_name")
            ))


    conn.commit()
    conn.close()
    print(f"Processed {file_path} into database.")

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("No JSON file provided.")
        sys.exit(1)

    json_file = sys.argv[1]
    process_json(json_file)