
import streamlit as st
import random
import pandas as pd

def add_variance(num, spread=0.10):
    return num * (1 + random.uniform(-spread, spread))

def clamp(x, lo, hi):
    return max(lo, min(x, hi))

def bias_true_or_false(p):
    return random.random() < p


# fight Sim
def simulate_fight(level, difficulty):

    duration_time = round(add_variance(40 + level * 6 * difficulty) * 2)
    duration_time = clamp(duration_time, 10, 9999)

    player_attacks_attempted = round(add_variance(8 + level * 1.8 * difficulty))
    player_attacks_attempted = clamp(player_attacks_attempted, 1, 9999)

    player_miss_rate = clamp(0.15 * difficulty, 0.05, 0.6)
    player_attacks_missed = round(player_attacks_attempted * player_miss_rate)
    player_attacks_missed = clamp(player_attacks_missed, 0, player_attacks_attempted)

    blocks_attempted = round(add_variance(4 + level * 0.9 * difficulty))
    blocks_attempted = clamp(blocks_attempted, 0, 9999)

    block_success_rate = clamp(0.85 / difficulty, 0.2, 0.95)
    blocks_successful = round(blocks_attempted * block_success_rate)
    blocks_successful = clamp(blocks_successful, 0, blocks_attempted)

    enemy_attacks_attempted = round(add_variance(6 + level * 2.2 * difficulty))
    enemy_attacks_attempted = clamp(enemy_attacks_attempted, 1, 9999)

    # higher difficulty + higher level = higher death probability
    death_probability = clamp(0.03 * level * difficulty, 0.01, 0.95)
    player_died = 1 if bias_true_or_false(death_probability) else 0

    return {
        "level": level,
        "duration_time": duration_time,
        "player_attacks_attempted": player_attacks_attempted,
        "player_attacks_missed": player_attacks_missed,
        "blocks_attempted": blocks_attempted,
        "blocks_successful": blocks_successful,
        "enemy_attacks_attempted": enemy_attacks_attempted,
        "player_died": player_died,
    }


# generate data
def generate_dataset(difficulty, sessions=7):
    random.seed(43)
    rows = []

    for _ in range(sessions):
        for level in range(1, 11):
            rows.append(simulate_fight(level, difficulty))

    return pd.DataFrame(rows)

# streamlit UI
st.title("Balancing Demonstration")

st.markdown("""
Adjust difficulty and observe how combat metrics and **average deaths per level** change.
""")

difficulty = st.slider("Difficulty", 0.5, 2.0, 1.0, 0.05)

df = generate_dataset(difficulty)

averages = (
    df.groupby("level")[
        [
            "duration_time",
            "player_attacks_attempted",
            "player_attacks_missed",
            "blocks_attempted",
            "blocks_successful",
            "enemy_attacks_attempted",
            "player_died",
        ]
    ]
    .mean()
    .round(2)
    .reset_index()
)

# rename death column for clarity
averages = averages.rename(columns={"player_died": "average_death_rate"})

st.subheader("Average Metrics Per Level")

st.dataframe(averages, use_container_width=True)
# run UI via:

#streamlit run rule_balance.py