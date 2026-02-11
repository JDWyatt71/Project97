# test to checkjson has the right structure and types

import os
import json
import pytest


def load_session_json(file_path):
    f = open(file_path, "r")
    data = json.load(f)
    f.close()
    return data


def test_telemetry_session():
    # path to example.json just for testing
    root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    example_path = os.path.join(root, "example.json")
    if not os.path.exists(example_path):
        pytest.skip("example.json not found")

    data = load_session_json(example_path)

    # make sure key fields are present and are the correct type
    assert "session_id" in data
    assert "timestamp_start" in data
    assert "timestamp_end" in data
    assert "player_id" in data
    assert "runs" in data
    assert type(data["session_id"]) == str
    assert type(data["timestamp_start"]) == str
    assert type(data["timestamp_end"]) == str
    assert type(data["player_id"]) == str
    assert type(data["runs"]) == list

    # check each run
    for i in range(len(data["runs"])):
        run = data["runs"][i]
        assert "run_id" in run
        assert "successful" in run
        assert "run_duration" in run
        assert "level_finish" in run
        assert "fights" in run
        assert type(run["run_id"]) == str
        assert type(run["successful"]) == bool
        assert type(run["run_duration"]) in (int, float)
        assert type(run["level_finish"]) in (int, float)
        assert type(run["fights"]) == list

        # check each fight in the run
        for j in range(len(run["fights"])):
            fight = run["fights"][j]
            assert "fight_id" in fight
            assert "battle_time_seconds" in fight
            assert "number_of_turns" in fight
            assert "hp_left_over" in fight
            assert type(fight["fight_id"]) == str
            assert type(fight["number_of_turns"]) in (int, float)
            assert type(fight["hp_left_over"]) in (int, float)


if __name__ == "__main__":
    pytest.main([__file__, "-v"])
