import ast
import io
import zipfile
from pathlib import Path

import pandas as pd


def load_download_all_data():
    #  only need the helper function, not the full streamlit app
    dash_path = Path(__file__).resolve().parent.parent / "Project97" / "dashboard_stuff" / "dash.py"
    src = dash_path.read_text(encoding="utf-8")

    tree = ast.parse(src)
    fn_node = None
    for node in tree.body:
        if isinstance(node, ast.FunctionDef) and node.name == "download_all_data":
            fn_node = node
            break
    assert fn_node is not None, "download_all_data() wasn't found in dashboard_stuff/dash.py"

    mod = ast.Module(body=[fn_node], type_ignores=[])
    code = compile(mod, filename=str(dash_path), mode="exec")

    namespace = {
        "io": io,
        "zipfile": zipfile,
    }
    #  justrun the extracted function code
    exec(code, namespace)
    return namespace["download_all_data"]


def test_export_download_all_data_returns_zip_bytes():
    download_all_data = load_download_all_data()

    # small inputs just to make the helper run
    sessions_df = pd.DataFrame({"session_id": [1], "total_play_time": [10]})
    runs_df = pd.DataFrame({"run_id": [1], "successful": [1], "level_finish": [1], "run_time": [5]})
    fights_df = pd.DataFrame(
        {"fight_id": [1], "duration_time": [1], "health_remaining": [100], "player_attacks_attempted": [3]}
    )
    upgrades_df = pd.DataFrame({"run_id": [1], "level": [1], "value": [2]})
    moves_df = pd.DataFrame({"move_id": [1], "height": ["Low"]})
    status_df = pd.DataFrame({"fight_id": [1], "status": ["None"], "count": [0]})

    zip_bytes = download_all_data(sessions_df, runs_df, fights_df, upgrades_df, moves_df, status_df)
    assert isinstance(zip_bytes, (bytes, bytearray))
    assert len(zip_bytes) > 0

    # it should unzip
    with zipfile.ZipFile(io.BytesIO(zip_bytes), "r") as z:
        names = z.namelist()
    # if unzip worked, there should be some files in it
    assert len(names) > 0


def test_export_zip_contains_expected_csv_files():
    download_all_data = load_download_all_data()

    sessions_df = pd.DataFrame({"session_id": [1]})
    runs_df = pd.DataFrame({"run_id": [1], "successful": [1], "level_finish": [1], "run_time": [5]})
    fights_df = pd.DataFrame({"fight_id": [1], "duration_time": [1], "health_remaining": [100], "player_attacks_attempted": [3]})
    upgrades_df = pd.DataFrame({"run_id": [1], "level": [1], "value": [2]})
    moves_df = pd.DataFrame({"move_id": [1], "height": ["Low"]})
    status_df = pd.DataFrame({"fight_id": [1], "status": ["None"], "count": [0]})

    zip_bytes = download_all_data(sessions_df, runs_df, fights_df, upgrades_df, moves_df, status_df)

    with zipfile.ZipFile(io.BytesIO(zip_bytes), "r") as z:
        names = set(z.namelist())

    expected = {
        "sessions.csv",
        "runs.csv",
        "fights.csv",
        "upgrades.csv",
        "moves.csv",
        "status_effects.csv",
    }
    # the csv files the func writes into the zip
    assert expected.issubset(names)

