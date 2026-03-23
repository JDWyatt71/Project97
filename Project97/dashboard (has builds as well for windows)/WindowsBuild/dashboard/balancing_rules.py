# contains rule based suggestions

RULES = [
    {
        "rule_id": "R1",
        "name": "Difficulty Spike — High Fail Rate",
        "description": "A level's fail rate exceeds 40%, indicating a difficulty spike.",
        "param": "difficulty_hard",          
        "delta": -0.10,
        "explanation": (
            "More than 40% of players are dying at this level. "
            "Consider reducing the difficulty modifier by 10% for this version."
        ),
        "scope": "per_level",
        "threshold": {"fail_rate": 0.40},
    },
    {
        "rule_id": "R2",
        "name": "Prolonged Fight + High Fail Rate — Enemy Too Tanky",
        "description": (
            "Fail rate exceeds 40% AND median fight duration exceeds 120 seconds. "
            "Players are lasting long but still losing — the enemy likely has too much HP or AP."
        ),
        "param": "difficulty_hard",
        "delta": -0.15,
        "explanation": (
            "Players are surviving long fights but still dying. "
            "The enemy may have too many moves per turn. "
            "Reducing the difficulty modifier further is suggested."
        ),
        "scope": "per_level",
        "threshold": {"fail_rate": 0.40, "median_duration": 120},
    },
    {
        "rule_id": "R3",
        "name": "Progression Too Easy — Low Fail Rate Streak",
        "description": (
            "Three or more consecutive levels each have a fail rate below 10%, "
            "suggesting the game is not challenging enough."
        ),
        "param": "difficulty_normal",
        "delta": 0.10,
        "explanation": (
            "Players are clearing multiple levels with almost no deaths. "
            "Consider increasing the difficulty modifier by 10%."
        ),
        "scope": "global",
        "threshold": {"fail_rate": 0.10, "consecutive_levels": 3},
    },
    {
        "rule_id": "R4",
        "name": "High Player Miss Rate — Accuracy Imbalance",
        "description": (
            "Players are missing more than 50% of their attacks at a given level, "
            "suggesting enemy evasion or player accuracy scaling is off."
        ),
        "param": "lose_prob_aggressive_L5",
        "delta": -0.05,
        "explanation": (
            "Players are missing over half their attacks at this level. "
            "Enemy evasion may be too high or player accuracy scaling needs reviewing."
        ),
        "scope": "per_level",
        "threshold": {"miss_rate": 0.50},
    },
{
    "rule_id": "R5",
    "name": "Fairness — Attack-Heavy Players Dying Disproportionately",
    "description": (
        "Players whose attack ratio exceeds 0.75 are dying at more than double "
        "the rate of players below 0.40 at the same level, indicating the game "
        "punishes aggressive playstyles unfairly."
    ),
    "param": "difficulty_modifiers",
    "delta": -0.05,
    "explanation": (
        "Attack-heavy players are dying at more than 2x the rate of defence-heavy "
        "players at this level. Enemy damage or accuracy may be overtuned against "
        "players who invest fewer moves into blocking."
    ),
    "scope": "per_level",
    "threshold": {"attack_heavy_to_defence_heavy_death_ratio": 2.0},
},

    {
        "rule_id": "R6",
        "name": "Survival HP Too Low — Fights Are Too Close",
        "description": (
            "Players who survive a level are doing so with very low HP remaining "
            "(average below 15% of BASE_HP). This makes subsequent levels nearly impossible."
        ),
        "param": "base_hp",
        "delta": 5,
        "explanation": (
            "Surviving players are finishing fights with almost no HP. "
            "Consider raising base HP by 5 to give more buffer into the next level."
        ),
        "scope": "per_level",
        "threshold": {"avg_surviving_hp_fraction": 0.15},
    },
    {
        "rule_id": "R7",
        "name": "Hard Mode Too Punishing — No Late-Game Completions",
        "description": (
            "Fewer than 5% of Hard mode runs reach level 8 or beyond, "
            "suggesting Hard is effectively unwinnable."
        ),
        "param": "difficulty_hard",
        "delta": -0.10,
        "explanation": (
            "Almost no Hard mode players are reaching the late game. "
            "Reducing the Hard difficulty modifier from 1.3 toward 1.2 is suggested."
        ),
        "scope": "per_version",
        "threshold": {"hard_late_game_reach_rate": 0.05},
    },
    {
        "rule_id": "R8",
        "name": "Defence Ineffective — Low Block Success Rate",
        "description": (
            "Block success rate falls below 30% at a level, meaning defensive play "
            "is not rewarding players and may be discouraging strategic variety."
        ),
        "param": "difficulty_normal",
        "delta": -0.05,
        "explanation": (
            "Players are attempting blocks but fewer than 30% are succeeding. "
            "This may indicate enemy attack speed or accuracy is overtuned for this level."
        ),
        "scope": "per_level",
        "threshold": {"block_success_rate": 0.30},
    },
]