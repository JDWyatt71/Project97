# test to check game_telemetry SQL DB has the right structure and types

import sqlite3
import pytest
from pathlib import Path


def get_db_path():
    """Path to game_telemetry DB (project root)."""
    root = Path(__file__).resolve().parent.parent
    return str(root / "game_telemetry_clean.db")


def get_table_info(conn, table_name):
    """Return column info for a table."""
    cur = conn.execute(f"PRAGMA table_info({table_name})")
    return {row[1]: row[2] for row in cur.fetchall()}  # name =  type


def test_telemetry_db_exists():
    """game_telemetry DB must exist at project root."""
    db_path = get_db_path()
    assert Path(db_path).exists(), f"game_telemetry_clean.db not found at {db_path}"


def test_telemetry_sessions_table():
    """sessions table must have required columns."""
    db_path = get_db_path()
    if not Path(db_path).exists():
        pytest.skip("game_telemetry_clean.db not found")
    with sqlite3.connect(db_path) as conn:
        tables = [t[0] for t in conn.execute(
            "SELECT name FROM sqlite_master WHERE type='table'"
        ).fetchall()]
        assert "sessions" in tables, "sessions table missing"
        info = get_table_info(conn, "sessions")
        # dashboard uses start_time, total_play_time
        assert "start_time" in info or "session_id" in info, "sessions needs start_time or session_id"
        assert "total_play_time" in info or "player_id" in info, "sessions needs total_play_time or player_id"


def test_telemetry_runs_table():
    """runs table must have required columns."""
    db_path = get_db_path()
    if not Path(db_path).exists():
        pytest.skip("game_telemetry_clean.db not found")
    with sqlite3.connect(db_path) as conn:
        tables = [t[0] for t in conn.execute(
            "SELECT name FROM sqlite_master WHERE type='table'"
        ).fetchall()]
        assert "runs" in tables, "runs table missing"
        info = get_table_info(conn, "runs")
        assert "successful" in info, "runs needs successful"
        assert "level_finish" in info, "runs needs level_finish"
        assert "run_time" in info or "run_duration" in info, "runs needs run_time or run_duration"


def test_telemetry_fights_table():
    """fights table must have required columns."""
    db_path = get_db_path()
    if not Path(db_path).exists():
        pytest.skip("game_telemetry_clean.db not found")
    with sqlite3.connect(db_path) as conn:
        tables = [t[0] for t in conn.execute(
            "SELECT name FROM sqlite_master WHERE type='table'"
        ).fetchall()]
        assert "fights" in tables, "fights table missing"
        info = get_table_info(conn, "fights")
        assert "duration_time" in info or "battle_time_seconds" in info, "fights needs duration_time or battle_time_seconds"
        assert "health_remaining" in info or "hp_left_over" in info, "fights needs health_remaining or hp_left_over"
        assert "player_attacks_attempted" in info or "number_of_turns" in info, "fights needs combat metrics"


def test_telemetry_has_seeded_data():
    """DB should have seeded rows for dashboard to show analytics (spec: seeded dataset)."""
    db_path = get_db_path()
    if not Path(db_path).exists():
        pytest.skip("game_telemetry_clean.db not found")
    with sqlite3.connect(db_path) as conn:
        total = (
            conn.execute("SELECT COUNT(*) FROM sessions").fetchone()[0]
            + conn.execute("SELECT COUNT(*) FROM runs").fetchone()[0]
            + conn.execute("SELECT COUNT(*) FROM fights").fetchone()[0]
        )
        assert total > 0, "game_telemetry should have seeded data (sessions, runs, or fights)"


if __name__ == "__main__":
    pytest.main([__file__, "-v"])
