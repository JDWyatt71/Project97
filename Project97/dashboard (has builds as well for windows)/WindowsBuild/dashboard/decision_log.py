# manages the balancing decision log.
# contains 30 seeded historical decisions and functions to
# read from / write to the decision_log table in the SQLite DB.

import sqlite3
import json
from datetime import datetime

DB_PATH = "Seeded_Dataset.db"


# SEEDED DECISIONS
# quality: GOOD / MID / BAD  


SEEDED_DECISIONS = [
    {
        "decision_id":  1,
        "timestamp":    "2026-02-22 09:14:00",
        "quality":      "BAD",
        "title":        "Increase enemy damage further",
        "rationale":    "Only elite players should win. Raising base damage to 4.5 will thin the player pool.",
        "change":       "enemy_base_damage: 2.5 → 4.5",
        "evidence":     json.dumps({"observed_completion_rate": 0.18, "target": "reduce casual completions"}),
        "outcome":      "Reverted. Overall session length dropped 40%. New players quit at level 2.",
        "game_version": "Hard",
        "level":        None,
        "rule_id":      None,
    },
    {
        "decision_id":  2,
        "timestamp":    "2026-02-22 14:30:00",
        "quality":      "GOOD",
        "title":        "Reduce level 10 enemy attack frequency",
        "rationale":    "Telemetry shows 90% of players die at level 10. Enemy move frequency is too high for a final fight.",
        "change":       "enemy_move_frequency: 0.90 → 0.72 at level 10",
        "evidence":     json.dumps({"level_10_fail_rate": 0.90, "avg_duration_L10": 187}),
        "outcome":      "Level 10 completion rate rose from 10% to 31%. Retained challenge without feeling unfair.",
        "game_version": "All",
        "level":        10,
        "rule_id":      "R1",
    },
    {
        "decision_id":  3,
        "timestamp":    "2026-02-23 10:05:00",
        "quality":      "GOOD",
        "title":        "Add checkpoint heal before level 10",
        "rationale":    "Players invest ~180s per fight. Reaching level 10 with near-zero HP feels unfair.",
        "change":       "checkpoint_heal_fraction: added heal trigger before level 10 on Normal",
        "evidence":     json.dumps({"avg_hp_entering_L10": 8.2, "session_abandonment_rate": 0.44}),
        "outcome":      "Player frustration reports dropped. Replayability metrics improved across all versions.",
        "game_version": "Normal",
        "level":        10,
        "rule_id":      "R6",
    },
    {
        "decision_id":  4,
        "timestamp":    "2026-02-24 11:22:00",
        "quality":      "MID",
        "title":        "Grant +20 HP bonus before final fight",
        "rationale":    "90% of players die at level 10. A flat HP bonus gives all players a better shot.",
        "change":       "base_hp effective bonus +20 before level 10 only",
        "evidence":     json.dumps({"level_10_fail_rate": 0.90, "avg_surviving_hp_L9": 11.3}),
        "outcome":      "Completion rose but difficulty curve flattened noticeably. Under review.",
        "game_version": "All",
        "level":        10,
        "rule_id":      "R6",
    },
    {
        "decision_id":  5,
        "timestamp":    "2026-02-25 09:45:00",
        "quality":      "BAD",
        "title":        "Remove Easy mode entirely",
        "rationale":    "Easy mode only has 3% success rate — players clearly find it frustrating anyway.",
        "change":       "Proposed removal of Easy difficulty configuration",
        "evidence":     json.dumps({"easy_completion_rate": 0.03}),
        "outcome":      "Rejected. Low success rate signals a tuning problem not a mode problem. Removing it abandons casual players.",
        "game_version": "Easy",
        "level":        None,
        "rule_id":      "R7",
    },
    {
        "decision_id":  6,
        "timestamp":    "2026-02-25 15:10:00",
        "quality":      "GOOD",
        "title":        "Rebalance Easy mode difficulty modifier",
        "rationale":    "Easy mode only has 3% success rate. The difficulty modifier is too close to Normal.",
        "change":       "difficulty_modifiers Easy: 0.75 → 0.55",
        "evidence":     json.dumps({"easy_completion_rate": 0.03, "normal_completion_rate": 0.21}),
        "outcome":      "Easy mode completion climbed to 19%. New player retention up 28% in follow-on session.",
        "game_version": "Easy",
        "level":        None,
        "rule_id":      "R7",
    },
    {
        "decision_id":  7,
        "timestamp":    "2026-02-26 13:00:00",
        "quality":      "GOOD",
        "title":        "Cap maximum fight duration at 150 seconds",
        "rationale":    "Late-game fights approach 3 minutes. Player fatigue is measurable in drop-off data.",
        "change":       "Added hard cap: duration_time max = 150s from level 7 onward",
        "evidence":     json.dumps({"avg_duration_L8": 194, "avg_duration_L9": 211, "post_L7_quit_rate": 0.37}),
        "outcome":      "Engagement beyond level 7 improved. Average session length +22 seconds.",
        "game_version": "All",
        "level":        7,
        "rule_id":      "R2",
    },
    {
        "decision_id":  8,
        "timestamp":    "2026-02-27 10:30:00",
        "quality":      "MID",
        "title":        "Increase fight length for skilled players",
        "rationale":    "Top-performing players clear fights too quickly. Longer fights would increase challenge.",
        "change":       "enemy_move_frequency: 0.90 → 1.10 for runs with high block success rate",
        "evidence":     json.dumps({"top_quartile_avg_duration": 48, "top_quartile_fail_rate": 0.04}),
        "outcome":      "Implemented cautiously. Some skilled players report boredom from drawn-out fights.",
        "game_version": "Hard",
        "level":        None,
        "rule_id":      None,
    },

    {
        "decision_id":  9,
        "timestamp":    "2026-03-01 09:00:00",
        "quality":      "GOOD",
        "title":        "Encourage blocking with early-game tutorial prompt",
        "rationale":    "Block attempts only increase meaningfully from level 5 onward. Early game players are not defending.",
        "change":       "Flagged to UX team: add blocking tutorial prompt at level 1",
        "evidence":     json.dumps({"avg_blocks_L1": 1.2, "avg_blocks_L5": 6.8, "L1_fail_rate": 0.19}),
        "outcome":      "Early deaths reduced. Block attempts at level 1 doubled after tutorial introduction.",
        "game_version": "All",
        "level":        1,
        "rule_id":      "R8",
    },
    {
        "decision_id":  10,
        "timestamp":    "2026-03-01 14:45:00",
        "quality":      "BAD",
        "title":        "Nerf blocking effectiveness",
        "rationale":    "Blocking dominates late-game strategy. Reducing its effectiveness would increase variety.",
        "change":       "Proposed reduction of block_success_rate cap from 0.95 to 0.60",
        "evidence":     json.dumps({"late_game_block_success_rate": 0.71, "late_game_fail_rate": 0.13}),
        "outcome":      "Rejected. Telemetry shows players rely on blocking as a core survival mechanic. Nerfing it would spike late-game deaths.",
        "game_version": "All",
        "level":        None,
        "rule_id":      "R8",
    },
    {
        "decision_id":  11,
        "timestamp":    "2026-03-02 11:00:00",
        "quality":      "GOOD",
        "title":        "Introduce adaptive damage reduction for levels 1–3",
        "rationale":    "Almost 20% of players die in the first 3 levels. Early deaths destroy retention.",
        "change":       "damage_variance: 0.20 → 0.10 for levels 1–3 (reduces unlucky death spikes)",
        "evidence":     json.dumps({"L1_fail_rate": 0.19, "L2_fail_rate": 0.17, "L3_fail_rate": 0.16}),
        "outcome":      "Early-level deaths dropped to ~11%. Players reporting more satisfying first runs.",
        "game_version": "All",
        "level":        None,
        "rule_id":      "R1",
    },
    {
        "decision_id":  12,
        "timestamp":    "2026-03-02 16:20:00",
        "quality":      "MID",
        "title":        "Guarantee player survival until level 3",
        "rationale":    "Protect brand-new players from early frustration entirely.",
        "change":       "Proposed: player cannot die at levels 1 and 2",
        "evidence":     json.dumps({"L1_fail_rate": 0.19, "session_one_quit_rate": 0.31}),
        "outcome":      "Under debate. Removes tension and may reduce sense of achievement. Adaptive approach (decision 11) preferred.",
        "game_version": "Easy",
        "level":        None,
        "rule_id":      None,
    },
    {
        "decision_id":  13,
        "timestamp":    "2026-03-03 10:15:00",
        "quality":      "GOOD",
        "title":        "Maintain current move pool scaling progression",
        "rationale":    "Telemetry validates that the move pool expanding 15→50 across levels is well-paced.",
        "change":       "No change — decision to preserve existing move scaling formula",
        "evidence":     json.dumps({"avg_moves_L1": 15.3, "avg_moves_L5": 31.2, "avg_moves_L10": 48.7}),
        "outcome":      "Confirmed. Move progression is one of the most positively received mechanics per session length data.",
        "game_version": "All",
        "level":        None,
        "rule_id":      None,
    },
    {
        "decision_id":  14,
        "timestamp":    "2026-03-03 14:00:00",
        "quality":      "BAD",
        "title":        "Cap maximum move pool at 10 moves",
        "rationale":    "Simplify late-game decision-making by limiting available moves.",
        "change":       "Proposed: max pool capped at 10 regardless of level",
        "evidence":     json.dumps({"avg_moves_L9": 47, "player_report_overwhelmed": 0.12}),
        "outcome":      "Rejected. Destroys late-game complexity that experienced players value. Would worsen retention for top quartile.",
        "game_version": "All",
        "level":        None,
        "rule_id":      None,
    },
    {
        "decision_id":  15,
        "timestamp":    "2026-03-04 09:30:00",
        "quality":      "GOOD",
        "title":        "Trigger dynamic assist after 3 deaths in a session",
        "rationale":    "Players who die repeatedly quit. A subtle assist after 3 deaths could retain them.",
        "change":       "Flagged to game team: detect 3 deaths in session, temporarily reduce enemy_base_damage by 15%",
        "evidence":     json.dumps({"three_death_session_quit_rate": 0.62, "one_death_session_quit_rate": 0.18}),
        "outcome":      "Implemented. Session continuation rate for struggling players up 34%.",
        "game_version": "Easy",
        "level":        None,
        "rule_id":      "R1",
    },
    {
        "decision_id":  16,
        "timestamp":    "2026-03-04 15:45:00",
        "quality":      "MID",
        "title":        "Full dynamic difficulty adjustment system",
        "rationale":    "Extend the assist system to continuously adjust difficulty in real time.",
        "change":       "Proposed: continuous difficulty_modifiers adjustment based on rolling death rate",
        "evidence":     json.dumps({"rolling_death_rate_threshold": 0.35}),
        "outcome":      "Deferred. Risk of inconsistent player experiences between sessions. Could undermine fairness perception.",
        "game_version": "All",
        "level":        None,
        "rule_id":      None,
    },
    {
        "decision_id":  17,
        "timestamp":    "2026-03-08 10:00:00",
        "quality":      "BAD",
        "title":        "Make wins extremely rare across all modes",
        "rationale":    "Exclusivity increases perceived value of success.",
        "change":       "Proposed: cap all win probabilities below 0.05",
        "evidence":     json.dumps({"current_normal_completion": 0.21}),
        "outcome":      "Rejected. Would destroy retention. Players need achievable success to stay engaged.",
        "game_version": "All",
        "level":        10,
        "rule_id":      None,
    },
    {
        "decision_id":  18,
        "timestamp":    "2026-03-09 11:30:00",
        "quality":      "MID",
        "title":        "Increase telemetry data granularity",
        "rationale":    "Current per-fight aggregation loses turn-level insights.",
        "change":       "Proposed: log per-turn damage and move data in addition to fight summaries",
        "evidence":     json.dumps({"current_event_types": 12, "proposed_event_types": 20}),
        "outcome":      "Partially implemented. Storage overhead acceptable. Analyst team flagged improved diagnostic value.",
        "game_version": "All",
        "level":        None,
        "rule_id":      None,
    },
    {
        "decision_id":  19,
        "timestamp":    "2026-03-10 09:15:00",
        "quality":      "GOOD",
        "title":        "Smooth level 7 difficulty spike",
        "rationale":    "22% of players die specifically at level 7 — above average for mid-game levels.",
        "change":       "enemy_move_frequency: 0.90 → 0.78 effective at level 7",
        "evidence":     json.dumps({"L7_fail_rate": 0.22, "L6_fail_rate": 0.13, "L8_fail_rate": 0.14}),
        "outcome":      "Level 7 fail rate dropped to 14%. Progression curve now smoother across mid-game.",
        "game_version": "Normal",
        "level":        7,
        "rule_id":      "R1",
    },
    {
        "decision_id":  20,
        "timestamp":    "2026-03-10 15:00:00",
        "quality":      "BAD",
        "title":        "Randomise boss damage per run",
        "rationale":    "Make each run feel unique by randomising enemy_base_damage ±50% per session.",
        "change":       "Proposed: randomise base damage multiplier at session start",
        "evidence":     json.dumps({"player_reported_unfairness": 0.08}),
        "outcome":      "Rejected. Destroys fairness perception. Players attribute random deaths to bugs rather than variance.",
        "game_version": "All",
        "level":        None,
        "rule_id":      None,
    },
    {
        "decision_id":  21,
        "timestamp":    "2026-03-11 10:45:00",
        "quality":      "GOOD",
        "title":        "Reward low-HP survival with improved item choices",
        "rationale":    "Many players finish fights with very low HP and then face the next fight under-resourced.",
        "change":       "Flagged to game team: trigger bonus item selection when HP < 20% after a fight",
        "evidence":     json.dumps({"avg_surviving_hp": 9.1, "pct_under_20pct_hp": 0.41}),
        "outcome":      "Implemented. Item usage rate increased. Players feel rewarded for surviving close fights.",
        "game_version": "All",
        "level":        None,
        "rule_id":      "R6",
    },
    {
        "decision_id":  22,
        "timestamp":    "2026-03-11 14:20:00",
        "quality":      "MID",
        "title":        "Add comeback mechanic for losing players",
        "rationale":    "Players on losing streaks should receive a damage boost to feel tension.",
        "change":       "Proposed: enemy_base_damage reduced 20% after player health drops below 15",
        "evidence":     json.dumps({"sub_15hp_death_rate": 0.71}),
        "outcome":      "Partially implemented with limits. Exploitability risk flagged — players intentionally dropping HP.",
        "game_version": "All",
        "level":        None,
        "rule_id":      "R6",
    },
    {
        "decision_id":  23,
        "timestamp":    "2026-03-15 09:00:00",
        "quality":      "GOOD",
        "title":        "Reduce Hard mode difficulty modifier",
        "rationale":    "Fewer than 3% of Hard runs reach level 8. The mode is effectively unwinnable.",
        "change":       "difficulty_modifiers Hard: 1.3 → 1.15",
        "evidence":     json.dumps({"hard_L8_reach_rate": 0.03, "hard_completion_rate": 0.01}),
        "outcome":      "Hard late-game reach rate rose to 9%. Perceived as more achievable without losing identity.",
        "game_version": "Hard",
        "level":        None,
        "rule_id":      "R7",
    },
    {
        "decision_id":  24,
        "timestamp":    "2026-03-16 11:00:00",
        "quality":      "BAD",
        "title":        "Remove all HP regeneration between levels",
        "rationale":    "HP regen between levels reduces tension. Removing it would make each fight feel critical.",
        "change":       "Proposed: hp_regen_choices → [0, 0]",
        "evidence":     json.dumps({"avg_hp_regen_per_level": 10, "inter_level_quit_rate": 0.07}),
        "outcome":      "Rejected. Testing showed players quit rather than continue when entering fights already depleted.",
        "game_version": "All",
        "level":        None,
        "rule_id":      None,
    },
    {
        "decision_id":  25,
        "timestamp":    "2026-03-17 10:30:00",
        "quality":      "GOOD",
        "title":        "Raise HP cap to support late-game survivability",
        "rationale":    "Late-game fights deal damage that outpaces the current HP cap of 120.",
        "change":       "hp_cap: 120 → 140",
        "evidence":     json.dumps({"L9_avg_damage_taken": 98, "L9_avg_max_hp": 118, "L9_fail_rate": 0.18}),
        "outcome":      "Late-game survivability improved without making earlier levels trivial.",
        "game_version": "All",
        "level":        9,
        "rule_id":      "R1",
    },
    {
        "decision_id":  26,
        "timestamp":    "2026-03-17 15:55:00",
        "quality":      "MID",
        "title":        "Increase base HP for all players",
        "rationale":    "Players consistently finish fights with dangerously low HP.",
        "change":       "base_hp: 50 → 65",
        "evidence":     json.dumps({"avg_surviving_hp_all_levels": 8.7, "pct_entering_fight_under_20hp": 0.38}),
        "outcome":      "Under review. Early levels become too easy as a side effect. May conflict with adaptive damage work.",
        "game_version": "All",
        "level":        None,
        "rule_id":      "R6",
    },
    {
        "decision_id":  27,
        "timestamp":    "2026-03-18 09:45:00",
        "quality":      "GOOD",
        "title":        "Reduce damage variance in levels 8–10",
        "rationale":    "High damage variance in late levels means deaths feel random rather than skill-based.",
        "change":       "damage_variance: 0.20 → 0.10 for levels 8, 9, 10",
        "evidence":     json.dumps({"L8_std_damage": 18.4, "L9_std_damage": 22.1, "player_reported_unfair_deaths": 0.19}),
        "outcome":      "Late-game deaths feel more skill-driven. Player satisfaction scores improved.",
        "game_version": "All",
        "level":        None,
        "rule_id":      None,
    },
    {
        "decision_id":  28,
        "timestamp":    "2026-03-19 13:00:00",
        "quality":      "BAD",
        "title":        "Increase enemy damage scaling per level",
        "rationale":    "Players are progressing too comfortably in mid-game.",
        "change":       "enemy_damage_per_level: 1.2 → 2.0",
        "evidence":     json.dumps({"L4_L6_avg_fail_rate": 0.12}),
        "outcome":      "Reverted after testing. Created an unplayable spike at level 5. Fail rate jumped to 61%.",
        "game_version": "Normal",
        "level":        None,
        "rule_id":      None,
    },
    {
        "decision_id":  29,
        "timestamp":    "2026-03-20 10:10:00",
        "quality":      "GOOD",
        "title":        "Balance attack-heavy playstyle death rate",
        "rationale":    "Attack-heavy players die at 2.4x the rate of defence-heavy players at level 6. This is a fairness issue.",
        "change":       "difficulty_modifiers Normal: 1.0 → 0.95 at level 6 to reduce enemy pressure",
        "evidence":     json.dumps({"attack_heavy_L6_fail_rate": 0.31, "defence_heavy_L6_fail_rate": 0.13, "ratio": 2.38}),
        "outcome":      "Ratio reduced to 1.6x. Still reflects playstyle risk but no longer feels punishing.",
        "game_version": "Normal",
        "level":        6,
        "rule_id":      "R5",
    },
    {
        "decision_id":  30,
        "timestamp":    "2026-03-21 14:30:00",
        "quality":      "MID",
        "title":        "Increase minimum HP regen per level",
        "rationale":    "The minimum regen of 5 HP per level is too low — players who roll low repeatedly are severely disadvantaged.",
        "change":       "hp_regen_choices: [5, 15] → [10, 15]",
        "evidence":     json.dumps({"pct_low_regen_runs": 0.48, "low_regen_fail_rate": 0.29, "high_regen_fail_rate": 0.14}),
        "outcome":      "Under review. Reduces variance but may make HP too predictable. Being monitored across next sprint.",
        "game_version": "All",
        "level":        None,
        "rule_id":      "R6",
    },
]



def seed_decision_log(db_path=DB_PATH): # write all 30 seeded decisions into the decision_log table.
    """
    Write all 30 seeded decisions into the decision_log table.
    Safe to call multiple times — uses INSERT OR IGNORE.
    """
    with sqlite3.connect(db_path) as conn:
        conn.execute("""
            CREATE TABLE IF NOT EXISTS decision_log (
                decision_id  INTEGER PRIMARY KEY,
                timestamp    TEXT,
                quality      TEXT,
                title        TEXT,
                rationale    TEXT,
                change       TEXT,
                evidence     TEXT,
                outcome      TEXT,
                game_version TEXT,
                level        INTEGER,
                rule_id      TEXT
            )
        """)
        for d in SEEDED_DECISIONS: # safe to call multiple times because of  INSERT OR IGNORE.
            conn.execute("""
                INSERT OR IGNORE INTO decision_log
                    (decision_id, timestamp, quality, title, rationale,
                     change, evidence, outcome, game_version, level, rule_id)
                VALUES
                    (:decision_id, :timestamp, :quality, :title, :rationale,
                     :change, :evidence, :outcome, :game_version, :level, :rule_id)
            """, d)
        conn.commit()


def get_all_decisions(db_path=DB_PATH): # return all decisions from the log, newest first
    with sqlite3.connect(db_path) as conn:
        rows = conn.execute("""
            SELECT decision_id, timestamp, quality, title, rationale,
                   change, evidence, outcome, game_version, level, rule_id
            FROM decision_log
            ORDER BY timestamp DESC
        """).fetchall()
    cols = ["decision_id", "timestamp", "quality", "title", "rationale",
            "change", "evidence", "outcome", "game_version", "level", "rule_id"]
    return [dict(zip(cols, r)) for r in rows]


def add_decision(title, rationale, change, evidence_dict,  # insert a new designer decision into the log, returns the new decision_id
                 game_version="All", level=None, rule_id=None,
                 quality="MID", db_path=DB_PATH):
    with sqlite3.connect(db_path) as conn:
        row = conn.execute("SELECT MAX(decision_id) FROM decision_log").fetchone() # get next id
        next_id = (row[0] or 0) + 1

        conn.execute("""
            INSERT INTO decision_log
                (decision_id, timestamp, quality, title, rationale,
                 change, evidence, outcome, game_version, level, rule_id)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
        """, (
            next_id,
            datetime.now().strftime("%Y-%m-%d %H:%M:%S"),
            quality,
            title,
            rationale,
            change,
            json.dumps(evidence_dict),
            "Pending — no outcome recorded yet.",
            game_version,
            level,
            rule_id,
        ))
        conn.commit()
    return next_id

def delete_decision(decision_id: int, db_path=DB_PATH): # remove a single decision from the log by its ID
    with sqlite3.connect(db_path) as conn:
        conn.execute("DELETE FROM decision_log WHERE decision_id = ?", (decision_id,))
        conn.commit()

if __name__ == "__main__":
    seed_decision_log()
    decisions = get_all_decisions()
    print(f"Decision log seeded with {len(decisions)} entries.")
    for d in decisions[:5]:
        print(f"  [{d['quality']}] {d['timestamp']} — {d['title']}")