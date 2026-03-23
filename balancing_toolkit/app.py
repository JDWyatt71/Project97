
import streamlit as st
from state_manager import ensure_loaded, regenerate, get_active_params
from rule_checker import check_rules_from_memory
from simulation_mode import simulate_run
from decision_log import seed_decision_log, get_all_decisions, add_decision, delete_decision
import io, sys
import plotly.express as px
import pandas as pd


ensure_loaded()  # runs once on startup

st.title("Balancing Toolkit")

# parameter Editor 
st.sidebar.header("Parameter Editor")
params = get_active_params()

st.sidebar.subheader("Enemy Stats")
params["enemy_base_damage"]      = st.sidebar.slider("Enemy base damage per hit",  1.0, 6.0,  params["enemy_base_damage"],      step=0.1)
params["enemy_damage_per_level"] = st.sidebar.slider("Enemy damage scaling/level", 0.5, 3.0,  params["enemy_damage_per_level"],  step=0.1)
params["enemy_move_frequency"]   = st.sidebar.slider("Enemy move frequency",        0.5, 1.5,  params["enemy_move_frequency"],    step=0.05)

st.sidebar.subheader("Player Stats")
params["base_hp"]  = st.sidebar.slider("Player starting HP", 30,  80,  params["base_hp"])
params["hp_cap"]   = st.sidebar.slider("Player max HP",       80,  200, params["hp_cap"])

st.sidebar.subheader("Global")
params["damage_variance"]                  = st.sidebar.slider("Damage variance (±%)", 0.05, 0.50, params["damage_variance"], step=0.05)
params["difficulty_modifiers"]["Easy"]     = st.sidebar.slider("Easy difficulty modifier",   0.3, 1.0, params["difficulty_modifiers"]["Easy"],   step=0.05)
params["difficulty_modifiers"]["Normal"] = st.sidebar.slider("Normal difficulty modifier", 0.5, 1.5, params["difficulty_modifiers"]["Normal"], step=0.05) 
params["difficulty_modifiers"]["Hard"]     = st.sidebar.slider("Hard difficulty modifier",   1.0, 2.0, params["difficulty_modifiers"]["Hard"],   step=0.05)

if st.sidebar.button("Apply & Regenerate"):
    regenerate(params)
    st.success("Dataset regenerated with new parameters.")

# rule Checker
st.header("Rule-Based Suggestions")
if st.button("Check Rules"):
    triggered = check_rules_from_memory()
    if not triggered:
        st.success("No rules triggered — balance looks healthy.")
    for t in triggered:
        st.warning(f"[{t['rule_id']}] {t['name']} — Level {t['level']} ({t['game_version']})")
        st.caption(t["explanation"])
        st.json(t["evidence"])

# simulation 
st.header("Simulation Mode")
if st.button("Run Simulation"):
    buffer = io.StringIO()
    old_stdout = sys.stdout
    sys.stdout = buffer
    try:
        simulate_run()
    finally:
        sys.stdout = old_stdout
    output = buffer.getvalue()
    with st.expander("Simulation output", expanded=True):
        st.text(output)

# decision log
from decision_log import seed_decision_log, get_all_decisions, add_decision
import json

# seed on first load
seed_decision_log()

st.header("Decision Log")

# view existing decisions 
decisions = get_all_decisions()
quality_filter = st.selectbox("Filter by quality", ["All", "GOOD", "MID", "BAD"])
filtered = decisions if quality_filter == "All" else [d for d in decisions if d["quality"] == quality_filter]

QUALITY_COLOUR = {"GOOD": "GREEN", "MID": "YELLOW", "BAD": "RED"}
for d in filtered:
    icon = QUALITY_COLOUR.get(d["quality"], "")
    with st.expander(f"{icon} [{d['timestamp']}] {d['title']}"):
        st.write(f"**Rationale:** {d['rationale']}")
        st.write(f"**Change made:** {d['change']}")
        st.write(f"**Outcome:** {d['outcome']}")
        if d["rule_id"]:
            st.caption(f"Triggered by rule {d['rule_id']} | Version: {d['game_version']} | Level: {d['level']}")
        try:
            st.json(json.loads(d["evidence"]))
        except Exception:
            pass
        if st.button(f"🗑️ Delete", key=f"del_{d['decision_id']}"):
            delete_decision(d["decision_id"])
            st.warning(f"Decision '{d['title']}' deleted.")
            st.rerun()

st.divider()

# record a new decision
st.subheader("Record a New Decision")
with st.form("new_decision"):
    d_title    = st.text_input("Decision title")
    d_rationale = st.text_area("Rationale / what was observed")
    d_change   = st.text_input("Parameter change made (e.g. enemy_base_damage: 2.5 → 2.0)")
    d_version  = st.selectbox("Game version affected", ["All", "Easy", "Normal", "Hard"])
    d_level    = st.number_input("Level (leave 0 for global)", min_value=0, max_value=10, value=0)
    d_quality  = st.selectbox("Decision quality", ["GOOD", "MID", "BAD"])
    d_rule     = st.text_input("Linked rule ID (optional, e.g. R1)")
    submitted  = st.form_submit_button("Save Decision")
    if submitted and d_title:
        add_decision(
            title=d_title,
            rationale=d_rationale,
            change=d_change,
            evidence_dict={"recorded_by": "designer", "params": get_active_params()},
            game_version=d_version,
            level=int(d_level) if d_level > 0 else None,
            rule_id=d_rule or None,
            quality=d_quality,
        )
        st.success(f"Decision '{d_title}' saved.")
        st.rerun()

import plotly.express as px

#  data preview
st.header("Data Preview")
st.caption("All charts reflect the current in-memory dataset — regenerate to see parameter changes.")

from state_manager import get_fights, get_runs, get_sessions

fights_df   = pd.DataFrame(get_fights())
runs_df     = pd.DataFrame(get_runs())
sessions_df = pd.DataFrame(get_sessions())

tab1, tab2, tab3 = st.tabs(["Funnel & Deaths", "Combat Stats", "Raw Tables"])

# tab 1: funnel and deaths
with tab1:
    col1, col2 = st.columns(2)

    with col1:
        st.subheader("Stage Completion Funnel")
        if "level" in fights_df.columns and "player_died" in fights_df.columns:
            funnel = (
                fights_df.groupby("level")
                .agg(total=("player_died", "count"),
                     deaths=("player_died", "sum"))
                .reset_index()
            )
            funnel["survival_rate"] = ((1 - funnel["deaths"] / funnel["total"]) * 100).round(1)
            funnel["fail_rate_pct"] = (funnel["deaths"] / funnel["total"] * 100).round(1)

            fig = px.bar(funnel, x="level", y="survival_rate",
                         labels={"survival_rate": "Survival %", "level": "Level"},
                         color="fail_rate_pct",
                         color_continuous_scale="RdYlGn_r",
                         title="Survival rate per level")
            fig.update_layout(coloraxis_colorbar_title="Fail %")
            st.plotly_chart(fig, use_container_width=True)

    with col2:
        st.subheader("Fail Rate by Game Version")
        if "level" in fights_df.columns and "run_id" in fights_df.columns:
            merged = fights_df.merge(runs_df[["run_id", "game_version"]], on="run_id", how="left")
            version_level = (
                merged.groupby(["game_version", "level"])
                .agg(fail_rate=("player_died", "mean"))
                .reset_index()
            )
            version_level["fail_rate"] = (version_level["fail_rate"] * 100).round(1)
            fig2 = px.line(version_level, x="level", y="fail_rate",
                           color="game_version",
                           labels={"fail_rate": "Fail %", "level": "Level"},
                           title="Fail rate per level by difficulty",
                           markers=True)
            st.plotly_chart(fig2, use_container_width=True)

    st.subheader("Level Finish Distribution")
    if "level_finish" in runs_df.columns:
        hist = runs_df["level_finish"].value_counts().sort_index().reset_index()
        hist.columns = ["Level", "Count"]
        fig3 = px.bar(hist, x="Level", y="Count",
                      title="How far do players get?",
                      color="Count", color_continuous_scale="Blues")
        st.plotly_chart(fig3, use_container_width=True)

# tab 2: combat stats
with tab2:
    col3, col4 = st.columns(2)

    with col3:
        st.subheader("Average HP Remaining per Level")
        if "health_remaining" in fights_df.columns:
            hp_per_level = (
                fights_df[fights_df["player_died"] == 0]
                .groupby("level")["health_remaining"]
                .mean()
                .round(1)
                .reset_index()
            )
            fig4 = px.line(hp_per_level, x="level", y="health_remaining",
                           markers=True,
                           labels={"health_remaining": "Avg HP Remaining", "level": "Level"},
                           title="Surviving player HP by level")
            # draw a reference line at the threshold used by R6
            fig4.add_hline(y=7.5, line_dash="dash", line_color="red",
                           annotation_text="R6 threshold (7.5)")
            st.plotly_chart(fig4, use_container_width=True)

    with col4:
        st.subheader("Block Success Rate per Level")
        if "blocks_attempted" in fights_df.columns and "blocks_successful" in fights_df.columns:
            block_data = (
                fights_df.groupby("level")
                .agg(attempted=("blocks_attempted", "sum"),
                     successful=("blocks_successful", "sum"))
                .reset_index()
            )
            block_data["success_rate"] = (
                block_data["successful"] / block_data["attempted"].replace(0, 1) * 100
            ).round(1)
            fig5 = px.bar(block_data, x="level", y="success_rate",
                          labels={"success_rate": "Block Success %", "level": "Level"},
                          title="How often do blocks succeed?",
                          color="success_rate", color_continuous_scale="Teal")
            fig5.add_hline(y=30, line_dash="dash", line_color="red",
                           annotation_text="R8 threshold (30%)")
            st.plotly_chart(fig5, use_container_width=True)

    st.subheader("Per-Level Combat Averages")
    cols_wanted = ["level", "duration_time", "player_attacks_attempted",
                   "player_attacks_missed", "blocks_attempted",
                   "blocks_successful", "health_remaining", "player_died"]
    cols_present = [c for c in cols_wanted if c in fights_df.columns]
    if cols_present:
        per_level = fights_df.groupby("level")[cols_present[1:]].mean().round(2).reset_index()
        per_level.columns = [c.replace("_", " ").title() for c in per_level.columns]
        st.dataframe(per_level, use_container_width=True)

# raw tables 
with tab3:
    table_choice = st.selectbox("Table", ["fights", "runs", "sessions"])
    if table_choice == "fights":
        st.dataframe(fights_df.head(100), use_container_width=True)
    elif table_choice == "runs":
        st.dataframe(runs_df.head(100), use_container_width=True)
    else:
        st.dataframe(sessions_df.head(100), use_container_width=True)
    st.caption(f"Showing first 100 rows of {table_choice}.")