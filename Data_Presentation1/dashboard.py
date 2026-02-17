

import sqlite3
import json
import pandas as pd
import streamlit as st
from datetime import datetime, timedelta
from pathlib import Path


# config
DEFAULT_DB_PATH = str(Path(__file__).parent / "game_telemetry_dash.db")


st.set_page_config(page_title="Telemetry Dashboard", layout="wide")

# helpers
def safe_json_load(x):
    if x is None:
        return None
    if isinstance(x, (list, dict)):
        return x
    if isinstance(x, str):
        x = x.strip()
        if x == "":
            return None
        try:
            return json.loads(x)
        except Exception:
            return None
    return None

def seconds_to_hours_str(seconds):
    if seconds is None:
        return "N/A"
    try:
        return f"{float(seconds) / 3600.0:.2f} hours"
    except Exception:
        return "N/A"

def classify_move_height(move_name: str) -> str:
    """
    Heuristic: classify a move into low/mid/high based on keywords.
    """
    if not isinstance(move_name, str):
        return "Mid"

    m = move_name.lower()

    if "low" in m or "leg" in m or "sweep" in m or "trip" in m or "floor" in m:
        return "Low"
    if "high" in m or "head" in m or "jump" in m or "knee" in m or "uppercut" in m:
        return "High"
    return "Mid"

def load_tables(db_path: str):
    with sqlite3.connect(db_path) as conn:
        sessions = pd.read_sql_query("SELECT * FROM sessions", conn)
        runs = pd.read_sql_query("SELECT * FROM runs", conn)
        fights = pd.read_sql_query("SELECT * FROM fights", conn)
    return sessions, runs, fights

def compute_accuracy_per_fight(fights_df: pd.DataFrame) -> float:
    # accuracy = successful_attacks / attempted_attacks
    # successful_attacks = attempted - missed
    attempted = fights_df["player_attacks_attempted"].fillna(0)
    missed = fights_df["player_attacks_missed"].fillna(0)
    successful = (attempted - missed).clip(lower=0)

    total_attempted = attempted.sum()
    if total_attempted <= 0:
        return 0.0
    return float(successful.sum() / total_attempted)

def build_sessions_over_time(sessions_df: pd.DataFrame) -> pd.DataFrame:

    n = len(sessions_df)
    if n == 0:
        return pd.DataFrame(columns=["date", "sessions"])

    start_date = datetime.now().date() - timedelta(days=max(0, n - 1))
    dates = [start_date + timedelta(days=i) for i in range(n)]
    tmp = pd.DataFrame({"date": dates, "sessions": [1] * n})
    out = tmp.groupby("date", as_index=False)["sessions"].sum()
    return out

def move_height_distribution(fights_df: pd.DataFrame) -> pd.DataFrame:
    moves_col = fights_df.get("moves_used")
    if moves_col is None:
        return pd.DataFrame({"height": ["Low", "Mid", "High"], "count": [0, 0, 0]})

    counts = {"Low": 0, "Mid": 0, "High": 0}

    for x in moves_col:
        moves = safe_json_load(x)  # list of moves or None
        if not moves:
            continue
        # moves might sometimes be nested so flatten lightly
        flat = []
        for item in moves:
            if isinstance(item, list):
                flat.extend(item)
            else:
                flat.append(item)

        for mv in flat:
            h = classify_move_height(mv)
            counts[h] += 1

    return pd.DataFrame({"height": list(counts.keys()), "count": list(counts.values())})

def status_effect_frequency(fights_df: pd.DataFrame) -> pd.DataFrame:
    col = fights_df.get("Status_effects")
    if col is None:
        return pd.DataFrame(columns=["status", "count"])

    freq = {}
    for x in col:
        effects = safe_json_load(x)
        if not effects:
            continue
        if not isinstance(effects, list):
            continue
        for e in effects:
            if e in (None, "None"):
                continue
            freq[e] = freq.get(e, 0) + 1

    out = pd.DataFrame({"status": list(freq.keys()), "count": list(freq.values())})
    if len(out) == 0:
        return out
    return out.sort_values("count", ascending=False)

def most_frequent_item_bought(runs_df: pd.DataFrame) -> str:
    col = runs_df.get("items_bought")
    if col is None:
        return "N/A"

    freq = {}
    for x in col:
        items = safe_json_load(x)
        if not items:
            continue
        if not isinstance(items, list):
            continue
        for it in items:
            if it is None:
                continue
            freq[it] = freq.get(it, 0) + 1

    if not freq:
        return "N/A"
    return max(freq.items(), key=lambda kv: kv[1])[0]

def bucket_hour(h: int) -> str:
    if 5 <= h <= 11:
        return "Morning (05-11)"
    elif 12 <= h <= 16:
        return "Afternoon (12-16)"
    elif 17 <= h <= 21:
        return "Evening (17-21)"
    else:
        return "Night (22-04)"

# sidebar
st.sidebar.title("Telemetry")

db_path = DEFAULT_DB_PATH
page = st.sidebar.radio("View", ["Overview", "Runs", "Combat & Mechanics"])


# load DB
try:
    sessions_df, runs_df, fights_df = load_tables(db_path)
except Exception as e:
    st.error(f"Could not load database: {e}")
    st.stop()


# common KPIs
avg_level_reached = float(runs_df["level_finish"].mean()) if len(runs_df) else 0.0
avg_session_hours = (sessions_df["total_play_time"].mean() / 3600.0) if len(sessions_df) else 0.0
acc_fight = compute_accuracy_per_fight(fights_df)  # 0..1


# pages
if page == "Overview":
    st.title("Overview")

    left, right = st.columns([2, 1])

    with left:

        st.subheader("When do players start sessions? (Time of Day)")
        # Parse to hour
        sessions_df["start_hour"] = pd.to_datetime(
            sessions_df["start_time"], format="%H:%M", errors="coerce"
        ).dt.hour
        sessions_df["start_bucket"] = sessions_df["start_hour"].apply(bucket_hour)
        bucket_counts = (
            sessions_df["start_bucket"]
            .value_counts()
            .reindex(["Morning (05-11)", "Afternoon (12-16)", "Evening (17-21)", "Night (22-04)"], fill_value=0)
        )

        bucket_pct = (bucket_counts / bucket_counts.sum() * 100).round(1)
        bucket_plot = pd.DataFrame({"% of sessions": bucket_pct})

        st.bar_chart(bucket_plot)
        st.caption("Binned start times make patterns easier to interpret than individual hours.")


        st.subheader("Move Height Distribution")
        dist = move_height_distribution(fights_df)
        st.bar_chart(dist.set_index("height")["count"])

    with right:
        st.subheader("Key Stats")
        st.metric("Avg Level Reached", f"{avg_level_reached:.2f}")
        st.metric("Avg Accuracy / Fight", f"{acc_fight*100:.1f}%")
        st.metric("Avg Session Duration", f"{avg_session_hours:.2f} hours")
        

elif page == "Runs":
    st.title("Runs")

    total_runs = int(len(runs_df))
    completed = int((runs_df["successful"] == 1).sum()) if "successful" in runs_df else 0
    failed = total_runs - completed
    avg_run_hours = (runs_df["run_time"].mean() / 3600.0) if len(runs_df) else 0.0

    c1, c2, c3, c4 = st.columns(4)
    c1.metric("Total", f"{total_runs}")
    c2.metric("Completed", f"{completed}")
    c3.metric("Failed", f"{failed}")
    c4.metric("Average Duration", f"{avg_run_hours:.2f} hours")

    st.subheader("Completion Split")
    pie_df = pd.DataFrame(
        {"result": ["Completed", "Failed"], "count": [completed, failed]}
    ).set_index("result")
    st.pyplot(pie_df.plot.pie(y="count", legend=False, ylabel="").figure)

    st.subheader("Level Finish Distribution")
    if "level_finish" in runs_df and len(runs_df):
        hist = runs_df["level_finish"].value_counts().sort_index()
        st.bar_chart(hist)

elif page == "Combat & Mechanics":
    st.title("Combat & Mechanics")

    most_item = most_frequent_item_bought(runs_df)
    avg_hp_left = float(fights_df["health_remaining"].mean()) if len(fights_df) else 0.0
    avg_fight_time = float(fights_df["duration_time"].mean()) if len(fights_df) else 0.0

    top = st.columns(4)
    top[0].metric("Average Move Accuracy / Fight", f"{acc_fight*100:.1f}%")
    top[1].metric("Average HP left after fights", f"{avg_hp_left:.1f}")
    top[2].metric("Average Fight Time", f"{avg_fight_time:.1f} sec")
    top[3].metric("Most Frequently Bought Item", f"{most_item}")

    st.subheader("Average Status Frequency Per Fight")
    freq = status_effect_frequency(fights_df)
    if len(freq) == 0:
        st.info("No status effects found (or they are all 'None').")
    else:
        st.bar_chart(freq.set_index("status")["count"].head(10))

    st.subheader("Per-Level Combat Averages")
    cols = [
        "duration_time",
        "player_attacks_attempted",
        "player_attacks_missed",
        "blocks_attempted",
        "blocks_successful",
        "enemy_attacks_attempted",
        "player_died",
        "health_remaining",
    ]
    cols = [c for c in cols if c in fights_df.columns]
    if "level" in fights_df.columns and cols:
        per_level = fights_df.groupby("level")[cols].mean().round(2).reset_index()
        st.dataframe(per_level, use_container_width=True)


# Run: streamlit run dashboard.py
