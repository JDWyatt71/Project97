

import sqlite3
from balancing_rules import RULES

DB_PATH = "Seeded_Dataset.db"


def get_level_stats(conn, game_version=None):  # returns a list of dicts, one per (level, game_version) combination, checks rules against computed metrics
    version_filter = "AND r.game_version = ?" if game_version else ""
    params = (game_version,) if game_version else ()

    query = f"""
        SELECT
            f.level,
            r.game_version,

            -- R1, R2: fail rate per level
            ROUND(AVG(f.player_died), 4)                              AS fail_rate,

            -- R2: median duration (SQLite has no MEDIAN, use AVG as proxy)
            ROUND(AVG(f.duration_time), 2)                            AS avg_duration,

            -- R4: player miss rate
            ROUND(
                SUM(f.player_attacks_missed) * 1.0 /
                NULLIF(SUM(f.player_attacks_attempted), 0)
            , 4)                                                       AS miss_rate,

            -- R6: average HP remaining on survival (only survived fights)
            ROUND(AVG(CASE WHEN f.player_died = 0
                      THEN f.health_remaining END), 2)                AS avg_surviving_hp,

            -- R8: block success rate
            ROUND(
                SUM(f.blocks_successful) * 1.0 /
                NULLIF(SUM(f.blocks_attempted), 0)
            , 4)                                                       AS block_success_rate,

            COUNT(*)                                                   AS total_fights

        FROM fights f
        JOIN runs r ON f.run_id = r.run_id
        WHERE 1=1 {version_filter}
        GROUP BY f.level, r.game_version
        ORDER BY f.level
    """
    rows = conn.execute(query, params).fetchall()
    cols = ["level", "game_version", "fail_rate", "avg_duration",
            "miss_rate", "avg_surviving_hp", "block_success_rate", "total_fights"]
    return [dict(zip(cols, row)) for row in rows]


def get_fairness_stats(conn):  # find attack ratio for R5
    query = """
        SELECT
            f.level,
            r.game_version,
            ROUND(AVG(CASE
                WHEN CAST(f.player_attacks_attempted AS FLOAT) /
                     NULLIF(f.player_attacks_attempted + f.blocks_attempted, 0) > 0.75
                THEN f.player_died END), 4)  AS attack_heavy_fail_rate,
            ROUND(AVG(CASE
                WHEN CAST(f.player_attacks_attempted AS FLOAT) /
                     NULLIF(f.player_attacks_attempted + f.blocks_attempted, 0) < 0.40
                THEN f.player_died END), 4)  AS defence_heavy_fail_rate,
            COUNT(*) AS total_fights
        FROM fights f
        JOIN runs r ON f.run_id = r.run_id
        GROUP BY f.level, r.game_version
    """
    rows = conn.execute(query).fetchall()
    cols = ["level", "game_version", "attack_heavy_fail_rate",
            "defence_heavy_fail_rate", "total_fights"]
    return [dict(zip(cols, row)) for row in rows]


def get_hard_mode_reach_rate(conn):  # find fraction of Hard runs that reach level 8+, R7
    query = """
        SELECT
            ROUND(
                SUM(CASE WHEN level_finish >= 8 THEN 1.0 ELSE 0 END) /
                NULLIF(COUNT(*), 0)
            , 4) AS late_game_reach_rate
        FROM runs
        WHERE game_version = 'Hard'
    """
    row = conn.execute(query).fetchone()
    return row[0] if row else 0.0


def _run_checks(conn): # Shared rule evaluation logic.

    triggered    = []
    level_stats  = get_level_stats(conn)
    hard_reach   = get_hard_mode_reach_rate(conn)
    fairness     = get_fairness_stats(conn)

    
    for stats in level_stats: # per-level rules 
        level   = stats["level"]
        version = stats["game_version"]

       
        if stats["fail_rate"] is not None and stats["fail_rate"] > 0.40:  # R1  high fail rate
            triggered.append(_build_trigger("R1", level, version, stats))

   
        if (stats["fail_rate"] is not None and stats["fail_rate"] > 0.40 # R2  high fail rate + long fight
                and stats["avg_duration"] is not None
                and stats["avg_duration"] > 120):
            triggered.append(_build_trigger("R2", level, version, stats))

   
        if stats["miss_rate"] is not None and stats["miss_rate"] > 0.50:  # R4  high miss rate
            triggered.append(_build_trigger("R4", level, version, stats))

      
        if (stats["avg_surviving_hp"] is not None and stats["avg_surviving_hp"] < 7.5): # R6  low surviving HP (15% of BASE_HP=50 → threshold = 7.5)
            triggered.append(_build_trigger("R6", level, version, stats))

        if (stats["block_success_rate"] is not None and stats["block_success_rate"] < 0.30): # R8  low block success rate
            triggered.append(_build_trigger("R8", level, version, stats))

    #  R3  three consecutive easy levels 
    all_levels = get_level_stats(conn) # run against a fresh copy so the window check is independent
    for i in range(len(all_levels) - 2):
        window = all_levels[i:i+3]
        if all(s["fail_rate"] is not None
               and s["fail_rate"] < 0.10 for s in window):
            triggered.append(_build_trigger("R3",
                level=window[0]["level"],
                version=window[0]["game_version"],
                stats={
                    "levels":     [s["level"]     for s in window],
                    "fail_rates": [s["fail_rate"] for s in window]
                }))

  
    for row in fairness: # R5  behavioural fairness 
        a = row["attack_heavy_fail_rate"]
        d = row["defence_heavy_fail_rate"]
        if a is not None and d is not None and d > 0 and (a / d) > 2.0:
            triggered.append(_build_trigger("R5",
                level=row["level"],
                version=row["game_version"],
                stats={
                    "attack_heavy_fail_rate":  a,
                    "defence_heavy_fail_rate": d,
                    "ratio": round(a / d, 2)
                }))

    if hard_reach < 0.05:  # R7 hard mode reach rate 
        triggered.append(_build_trigger("R7",
            level=None, version="Hard",
            stats={"hard_late_game_reach_rate": hard_reach}))

    return triggered


def check_rules(db_path=DB_PATH): # runs all rules against the on-disk SQLite DB
    with sqlite3.connect(db_path) as conn:
        return _run_checks(conn)


def check_rules_from_memory(): # used by the Streamlit app so results always reflect the current parameter state

    from state_manager import get_memory_conn
    conn = get_memory_conn()
    return _run_checks(conn)


def _build_trigger(rule_id, level, version, stats): # packages a triggered rule into a dict ready for the decision log
    rule = next(r for r in RULES if r["rule_id"] == rule_id)
    return {
        "rule_id":      rule_id,
        "name":         rule["name"],
        "level":        level,
        "game_version": version,
        "param":        rule["param"],
        "delta":        rule["delta"],
        "explanation":  rule["explanation"],
        "evidence":     stats,
    }


if __name__ == "__main__":
    results = check_rules()
    for r in results:
        print(f"[{r['rule_id']}] {r['name']} "
              f"— Level {r['level']} ({r['game_version']})")
        print(f"  → {r['explanation']}")
        print(f"  Evidence: {r['evidence']}\n")