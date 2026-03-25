import sqlite3
import json
import pandas as pd
import streamlit as st
from datetime import datetime, timedelta
from pathlib import Path
from collections import Counter
import plotly.graph_objects as go
import zipfile
import io

DEFAULT_DB_PATH = str(Path(__file__).parent.parent / "telemetry.db")

#FOR TESTING PURPOSES
PATH = "mock_telemetry.db"
Issues_PATH = "mock_telemetry_with_issues.db"

st.set_page_config(page_title="Telemetry Dashboard", layout="wide")

# HELPERS

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
        except:
            return None
    return None

def classify_move_height(move_name: str) -> str:
    if not isinstance(move_name, str):
        return "Mid"

    m = move_name.lower()

    if "low" in m or "leg" in m or "sweep" in m:
        return "Low"
    elif "high" in m or "head" in m or "uppercut" in m:
        return "High"

    return "Mid"

def bucket_hour(h):
    if 5 <= h <= 11:
        return "Morning"
    elif 12 <= h <= 16:
        return "Afternoon"
    elif 17 <= h <= 21:
        return "Evening"
    
    return "Night"

def format_paths(paths_dicts):
    formatted = []

    for run_id, path in paths_dicts.items():
        formatted.append({
            "run_id": run_id,
            "path": " → ".join(path),
            "length": len(path)
        })

    return pd.DataFrame(formatted)

def build_sankey_data(upgrades_df):
    transitions = []
    upgrades_df = upgrades_df.sort_values(["run_id", "level"])

    for run_id, group in upgrades_df.groupby("run_id"):
        group = group.sort_values("level")
        upgrades = list(zip(group["level"], group["value"]))

        for i in range(len(upgrades) - 1):
            src = f"L{upgrades[i][0]}: {upgrades[i][1]}"
            tgt = f"L{upgrades[i+1][0]}: {upgrades[i+1][1]}"

            transitions.append((src, tgt))

    df = pd.DataFrame(transitions, columns=["source", "target"])
    flow = df.groupby(["source", "target"]).size().reset_index(name="count")

    return flow

def prepare_sankey(flow_df):
    labels = list(set(flow_df["source"]).union(set(flow_df["target"])))
    label_index = {label: i for i, label in enumerate(labels)}

    source = flow_df["source"].map(label_index)
    target = flow_df["target"].map(label_index)
    value = flow_df["count"]

    return labels, source, target, value

def plot_sankey(labels, source, target, value):
    fig = go.Figure(data=[go.Sankey(
        node=dict(
            pad=15,
            thickness=20,
            line=dict(width=0.5),
            label=labels
        ),
        link=dict(
            source =source,
            target = target,
            value = value
        )
    )])

    return fig

def download_csv(df, name):
    csv = df.to_csv(index=False).encode("utf-8")
    st.download_button(
        label=f"Download {name}",
        data=csv,
        file_name=f"{name}.csv",
        mime="text/csv"
    )

def download_all_data(sessions_df, runs_df, fights_df, upgrades_df, moves_df, status_df):
    zip_buffer = io.BytesIO()

    with zipfile.ZipFile(zip_buffer, "w") as z:
        z.writestr("sessions.csv", sessions_df.to_csv(index=False))
        z.writestr("runs.csv", runs_df.to_csv(index=False))
        z.writestr("fights.csv", fights_df.to_csv(index=False))
        z.writestr("upgrades.csv", upgrades_df.to_csv(index=False))
        z.writestr("moves.csv", moves_df.to_csv(index=False))
        z.writestr("status_effects.csv", status_df.to_csv(index=False))
    
    return zip_buffer.getvalue()

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

def funnel_data(fights_df):
    total = len(fights_df)
    completed = fights_df[fights_df["player_died"] == False]

    return pd.DataFrame({
        "Stage": ["Start", "Complete"],
        "Count": [total, len(completed)]
    })

# DATABASE LOADING

def load_tables(db_path):
    with sqlite3.connect(db_path) as conn:
        sessions = pd.read_sql_query("SELECT * FROM sessions", conn)
        runs = pd.read_sql_query("SELECT * FROM runs", conn)
        fights = pd.read_sql_query("SELECT * FROM fights", conn)
        upgrades = pd.read_sql_query("SELECT * FROM upgrades", conn)
        moves = pd.read_sql_query("SELECT * FROM moves", conn)
        status = pd.read_sql_query("SELECT * FROM status_effects", conn)

    return sessions, runs, fights, upgrades, moves, status

# METRIC CALCULATIONS

def compute_accuracy_per_fight(fights_df):
    attempted = fights_df["attack_attempts"].fillna(0)
    successful = fights_df["attack_success"].fillna(0)
    #failed = (attempted - successful).clip(lower=0)
    if attempted.sum() == 0:
        return 0
    return successful.sum() / attempted.sum()

def compute_move_accuracy(moves_df):
    hits = (moves_df["attack_result"] == "hit").sum()
    total = len(moves_df)

    return hits / total if total > 0 else 0

def damage_per_move(moves_df):
    total_damage = moves_df["total_damage"].sum()
    total_moves = len(moves_df)

    return total_damage / total_moves if total_moves > 0 else 0

def player_vs_computer_stats(moves_df):
    player_moves = moves_df[moves_df["user_type"] == "Player (Character)"]
    enemy_moves = moves_df[moves_df["user_type"] != "Player (Character)"]

    results = []

    for enemy, enemy_group in enemy_moves.groupby("user_type"):
        playerVSenemy = player_moves[player_moves["target_type"] == enemy]

        pHits = (playerVSenemy["attack_result"] == "hit").sum()
        pTotal = len(playerVSenemy)
        pAccuracy = pHits / pTotal if pTotal > 0 else 0
        pDamage = playerVSenemy["total_damage"].mean()

        # --- Enemy stats ---
        eHits = (enemy_group["attack_result"] == "hit").sum()
        eTotal = len(enemy_group)
        eAccuracy = eHits / eTotal if eTotal > 0 else 0
        e_dmg = enemy_group["total_damage"].mean()

        results.append({
            "enemy": enemy,
            "player_accuracy": pAccuracy,
            "enemy_accuracy": eAccuracy,
            "player_avg_damage": pDamage,
            "enemy_avg_damage": e_dmg
        })

    return pd.DataFrame(results)

def block_rate(moves_df):
    blocked = (moves_df["attack_result"] == "blocked").sum()
    total = len(moves_df)

    return blocked / total if total > 0 else 0

def move_height_distribution(moves_df):
    counts = {"Low": 0, "Mid": 0, "High": 0}
    for move in moves_df["move_name"].dropna():
        h = classify_move_height(move)
        counts[h] += 1
    return pd.DataFrame(
        {"height": counts.keys(), "count": counts.values()}
    )

def status_effect_frequency(status_df):
    freq = {}
    for s in status_df["status_name"].dropna():
        if s == "None":
            continue
        freq[s] = freq.get(s, 0) + 1
    return pd.DataFrame(
        {"status": freq.keys(), "count": freq.values()}
    )

def most_frequent_item_bought(runs_df):
    if "item_bought" not in runs_df.columns:
        return "N/A"

    freq = {}
    for x in runs_df["items_bought"]:
        items = safe_json_load(x)
        if not items:
            continue
        for it in items:
            freq[it] = freq.get(it, 0) + 1
    if not freq:
        return "N/A"
    return max(freq, key=freq.get)

def upgrade_frequency(upgrades_df):
    return upgrades_df["value"].value_counts()

def upgrade_impact_analysis(upgrades_df, fights_df):
    results = []

    for upgrade in upgrades_df["value"].unique():
        upgrade_rows = upgrades_df[upgrades_df["value"] == upgrade]
        fight_subset = pd.DataFrame()

        for _, row in upgrade_rows.iterrows():
            level = row["level"]
            subset = fights_df[(fights_df["run_id"] == row["run_id"]) & (fights_df["level"] >= level)]
            fight_subset = pd.concat([fight_subset, subset])
        
        if len(fight_subset) == 0:
            continue

        win_rate = (fight_subset["player_died"] == 0).mean()
        attempts = fight_subset["attack_attempts"].sum()
        accuracy = (fight_subset["attack_success"].sum() / attempts if attempts > 0 else 0)
        avg_hp = fight_subset["hp_left"].mean()

        results.append({
            "upgrade": upgrade,
            "win_rate": win_rate,
            "accuracy": accuracy,
            "avg_hp": avg_hp,
            "sample": len(fight_subset)
        })

    return pd.DataFrame(results)
    
def build_progression_paths(upgrades_df):
    paths = {}

    upgrades_df = upgrades_df.sort_values(["run_id", "level"])
    for run_id, group in upgrades_df.groupby("run_id"):
        path = list(group.sort_values("level")["value"])
        paths[run_id] = path

    return paths

def most_common_paths(paths_dict):
    path_strings = [" → ".join(p) for p in paths_dict.values()]

    return pd.Series(path_strings).value_counts().head(5)

def upgrade_by_level(upgrades_df):
    return upgrades_df.groupby(["level", "type"]).size().unstack(fill_value=0)

def upgrade_popularity_by_position(paths_dict):
    position_counts = {}

    for path in paths_dict.values():
        for i, upgrade in enumerate(path):
            if i not in position_counts:
                position_counts[i] = Counter()
            position_counts[i][upgrade] += 1
    
    return position_counts

# FAIRNESS INDICATORS

def compute_win_rate(fights_df):
    if "player_died" not in fights_df:
        return 0
    return (fights_df["player_died"] == 0).mean()

def compute_block_success(fights_df):
    attempted = fights_df["defend_attempts"].fillna(0)
    success = fights_df["defend_success"].fillna(0)
    total = attempted.sum()
    if total == 0:
        return 0
    return success.sum() / total

def level_death_rate(fights_df):
    if "level" not in fights_df:
        return pd.DataFrame()
    return fights_df.groupby("level")["player_died"].mean()

def item_frequency(runs_df):
    if "items_bought" not in runs_df.columns:
        return pd.DataFrame(columns=["item", "count"])

    freq = {}
    for x in runs_df["items_bought"]:
        items = safe_json_load(x)
        if not items:
            continue
        for it in items:
            freq[it] = freq.get(it, 0) + 1

    return pd.DataFrame(
        {"item": freq.keys(), "count": freq.values()}
    )

# LOAD DATA

try:
    sessions_df, runs_df, fights_df, upgrades_df, moves_df, status_df = load_tables(PATH)

except Exception as e:
    st.error(f"Database error: {e}")
    st.stop()

if "difficulty" not in st.session_state:
    st.session_state["difficulty"] = "All"

# SIDEBAR

st.sidebar.title("Telemetry Dashboard")

page = st.sidebar.radio(
    "View",
    [
        "Overview",
        "Runs",
        "Combat & Mechanics",
        "Player Behaviour",
        "Fairness Analysis",
        "Issues",
    ],
)

Difficulty_pages = {"Overview", "Runs", "Fairness Analysis"}
if page in Difficulty_pages:
    difficulty_options = ["ALL"] + sorted(runs_df["difficulty"].unique().tolist())
    st.sidebar.selectbox(
        "Difficulty",
        difficulty_options,
        key="difficulty"
    )

if st.session_state["difficulty"] == "ALL":
    runs_df_filtered = runs_df
else:
    runs_df_filtered = runs_df[runs_df["difficulty"] == st.session_state["difficulty"]]

filtered_run_id = runs_df_filtered["run_id"]
fights_df_filtered = fights_df[fights_df["run_id"].isin(filtered_run_id)]
upgrades_df_filtered = upgrades_df[upgrades_df["run_id"].isin(filtered_run_id)]

# COMMON KPIs

accuracy = compute_accuracy_per_fight(fights_df_filtered)
win_rate = compute_win_rate(fights_df_filtered)
block_rate_value = compute_block_success(fights_df_filtered)
avg_level = runs_df_filtered["level_finish"].mean()
avg_session = sessions_df["total_play_time"].mean() / 3600

# OVERVIEW

if page == "Overview":

    st.title("Game Telemetry Overview")
    c1, c2, c3, c4 = st.columns(4)
    c1.metric("Avg Level Reached", f"{avg_level:.2f}")
    c2.metric("Accuracy", f"{accuracy*100:.1f}%")
    c3.metric("Session Length", f"{avg_session:.2f} hrs")
    c4.metric("Win Rate", f"{win_rate*100:.1f}%")

    st.subheader("Move Height Distribution")
    dist = move_height_distribution(moves_df)
    st.bar_chart(dist.set_index("height")["count"])

    zip_data = download_all_data(sessions_df, runs_df, fights_df, upgrades_df, moves_df, status_df)
    st.download_button(
        label="Download Full Dataset (ZIP)",
        data=zip_data,
        file_name="telemetry_data.zip",
        mime="application/zip"
    )



# RUNS

elif page == "Runs":

    st.title("Run Statistics")
    total_runs = len(runs_df_filtered)
    completed = (runs_df_filtered["successful"] == 1).sum()
    failed = total_runs - completed
    c1, c2, c3 = st.columns(3)
    c1.metric("Total Runs", total_runs)
    c2.metric("Completed", completed)
    c3.metric("Failed", failed)

    st.subheader("Stage Funnel")
    funnel = funnel_data(fights_df_filtered)
    st.bar_chart(funnel.set_index("Stage"))

    st.subheader("Level Completion Distribution")
    hist = runs_df_filtered["level_finish"].value_counts().sort_index()
    st.bar_chart(hist)

    st.subheader("Upgrade Popularity")
    st.bar_chart(upgrade_frequency(upgrades_df_filtered))

# COMBAT

elif page == "Combat & Mechanics":

    st.title("Combat Mechanics")
    most_item = most_frequent_item_bought(runs_df)
    avg_hp = fights_df["hp_left"].mean()
    avg_fight_time = fights_df["battle_time"].mean()
    c1, c2, c3, c4 = st.columns(4)
    c1.metric("Accuracy Per fight", f"{accuracy*100:.1f}%")
    c2.metric("Avg HP After Fight", f"{avg_hp:.1f}")
    c3.metric("Fight Duration", f"{avg_fight_time:.1f}s")
    c4.metric("Most Bought Item", most_item)

    st.subheader("Status Effect Frequency")
    freq = status_effect_frequency(status_df)
    if len(freq):
        st.bar_chart(freq.set_index("status")["count"])

    move_accuracy = compute_move_accuracy(moves_df)
    dmg_per_move = damage_per_move(moves_df)
    block = block_rate(moves_df)
    c5, c6, c7 = st.columns(3)
    c5.metric("Move Accuracy", f"{move_accuracy*100:.1f}%")
    c6.metric("Damage / Move", f"{dmg_per_move:.1f}")
    c7.metric("Block Rate", f"{block*100:.1f}%")

    stats_df = player_vs_computer_stats(moves_df)
    if not stats_df.empty:
        st.subheader("Player vs Computer Balance")
        st.dataframe(stats_df)
        st.subheader("Accuracy Comparison")
        st.bar_chart(stats_df.set_index("enemy")[["player_accuracy", "enemy_accuracy"]])

        st.subheader("Damage Comparison")
        st.bar_chart(stats_df.set_index("enemy")[["player_avg_damage", "enemy_avg_damage"]])

# PLAYER BEHAVIOUR

elif page == "Player Behaviour":

    st.title("Player Behaviour Analysis")
    sessions_df["start_hour"] = pd.to_datetime(
        sessions_df["start_time"],
        errors="coerce",
    ).dt.hour
    sessions_df["bucket"] = sessions_df["start_hour"].apply(bucket_hour)

    st.subheader("Session Start Time")
    bucket_counts = sessions_df["bucket"].value_counts()
    st.bar_chart(bucket_counts)

    st.subheader("Session Duration Distribution")
    st.bar_chart(sessions_df["total_play_time"])

    st.subheader("Fight Duration Distribution")
    st.bar_chart(fights_df["battle_time"])

    st.subheader("HP Remaining Distribution")
    st.bar_chart(fights_df["hp_left"])

    st.subheader("Player Progression Paths")
    paths = build_progression_paths(upgrades_df)
    paths_df = format_paths(paths)
    st.dataframe(paths_df)

    st.subheader("Most Common Upgrade Paths")
    common_paths = most_common_paths(paths)
    st.bar_chart(common_paths)

    st.subheader("Upgrade Type by Level")
    level_dist = upgrade_by_level(upgrades_df)
    st.bar_chart(level_dist)

    st.subheader("Upgrade Popularity by Position")
    pos_counts = upgrade_popularity_by_position(paths)
    for pos, counter in pos_counts.items():
        st.write(f"Step {pos+1}")
        st.bar_chart(pd.Series(counter))

    st.subheader("Upgrade Progression Flow (Sankey Diagram)")
    flow_df = build_sankey_data(upgrades_df)
    if len(flow_df):
        labels, source, target, value = prepare_sankey(flow_df)
        fig = plot_sankey(labels, source, target, value)
        st.plotly_chart(fig, use_container_width=True)

    st.subheader("Items Purchased")
    items = item_frequency(runs_df)
    if items.empty:
        st.info("No item data avialiable yet.")
    else:
        st.bar_chart(items.set_index("item")["count"])

# FAIRNESS ANALYSIS

elif page == "Fairness Analysis":

    st.title("Game Fairness Indicators")
    c1, c2, c3 = st.columns(3)
    c1.metric("Combat Win Rate", f"{win_rate*100:.1f}%")
    c2.metric("Attack Accuracy", f"{accuracy*100:.1f}%")
    c3.metric("Block Success Rate", f"{block_rate_value*100:.1f}%")

    st.subheader("Deaths by Level (Difficulty Spikes)")
    death_rate = level_death_rate(fights_df_filtered)
    if len(death_rate):
        st.bar_chart(death_rate)
        threshold = death_rate.mean() + death_rate.std()
        spikes = death_rate[death_rate > threshold]
        if len(spikes):
            st.warning("Spikes detected:")
            st.write(spikes)

    st.subheader("Upgrade Impact Analysis")
    impact_df = upgrade_impact_analysis(upgrades_df_filtered, fights_df_filtered)

    if len(impact_df):
        st.dataframe(impact_df)

        st.subheader("Win rate by upgrade")
        st.bar_chart(impact_df.set_index("upgrade")["win_rate"])

        st.subheader("Average Accuracy After Upgrade")
        st.bar_chart(impact_df.set_index("upgrade")["accuracy"])

    st.subheader("Top Performing Upgrades")
    top = impact_df.sort_values("win_rate", ascending=False).head(3)
    st.table(top[["upgrade", "win_rate", "accuracy"]])

    fights_df_filtered_copy = fights_df_filtered.copy()
    fights_df_filtered_copy["playstyle"] = fights_df_filtered_copy.apply(
        lambda x: "Aggressive" if x["attack_attempts"] > x["defend_attempts"] else "Defensive",
        axis = 1
    )
    st.subheader("Win Rate by Playstyle")
    seg = fights_df_filtered_copy.groupby("playstyle")["player_died"].mean()
    st.bar_chart(seg)

    st.subheader("Item Usage (Balance Indicator)")
    items = item_frequency(runs_df_filtered)
    if len(items):
        st.bar_chart(items.set_index("item")["count"])

elif page == "Issues":

    st.title("Data Issues")

    issues = validate_data(runs_df, fights_df)

    if issues:
        st.subheader("Data Quality Issues")
        for i in issues:
            st.warning(i)
    else:
        st.success("No data quality issuses found")