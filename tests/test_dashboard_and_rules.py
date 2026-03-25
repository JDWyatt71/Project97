import math
import sys
from pathlib import Path

import pandas as pd

# allow importing from Data_Presentation1
ROOT = Path(__file__).resolve().parent.parent
if str(ROOT) not in sys.path:
    sys.path.append(str(ROOT))

try:
    from Data_Presentation1 import dashboard
except ModuleNotFoundError:
    # if streamlit deps are missing skip the tests that need dashboard
    dashboard = None

try:
    from Data_Presentation1.rule_balance import generate_dataset
except ModuleNotFoundError:
    # if rule_balance deps are missing skip the dataset generator test
    generate_dataset = None


def test_fight_accuracy():
    """Accuracy gives back the right ratio."""
    if dashboard is None:
        import pytest

        pytest.skip("dashboard.py import failed (streamlit not installed)")
    df = pd.DataFrame(
        {
            "player_attacks_attempted": [10, 5],
            "player_attacks_missed": [2, 1],
        }
    )
    # quick  check on the math
    acc = dashboard.compute_accuracy_per_fight(df)
    # 12 landed out of 15 tries -> 0.8
    assert math.isclose(acc, 0.8)


def test_move_height_buckets():
    """Move height bucketing counts low/mid/high correctly."""
    if dashboard is None:
        import pytest

        pytest.skip("dashboard.py import failed (streamlit not installed)")
    fights = pd.DataFrame(
        {
            "moves_used": [
                ["Low kick", "High Punch", "mid jab"],
                ["Floor sweep", "HEAD SMASH"],
            ]
        }
    )
    dist = dashboard.move_height_distribution(fights)
    counts = dict(zip(dist["height"], dist["count"]))
    # should count low/mid/high based on keywords
    assert counts.get("Low") == 2
    assert counts.get("High") == 2
    assert counts.get("Mid") == 1


def test_helpers_handle_empty_data():
    """Helpers don't crash on empty inputs."""
    if dashboard is None:
        import pytest

        pytest.skip("dashboard.py import failed (streamlit not installed)")
    empty_fights = pd.DataFrame({})
    empty_runs = pd.DataFrame({})

    # empty inputs should just produce empty outputs
    status_df = dashboard.status_effect_frequency(empty_fights)
    most_item = dashboard.most_frequent_item_bought(empty_runs)

    assert status_df.empty or list(status_df.columns) == ["status", "count"]
    assert most_item == "N/A"


def test_deaths_increase_with_difficulty():
    """Hard should be at least as deadly as easy."""
    if generate_dataset is None:
        import pytest

        pytest.skip("rule_balance.py import failed (streamlit not installed)")
    easy = generate_dataset(difficulty=0.5, sessions=10)
    hard = generate_dataset(difficulty=2.0, sessions=10)

    easy_death_rate = easy["player_died"].mean()
    hard_death_rate = hard["player_died"].mean()

    # generator uses a fixed seed so  should be repeatable
    assert hard_death_rate >= easy_death_rate

