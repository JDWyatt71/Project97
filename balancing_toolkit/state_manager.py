
import copy
import sqlite3
import pandas as pd
import Seeded_Dataset_Generator as gen 
from Seeded_Dataset_Generator import (
    Sessions, Runs, Fights, DEFAULT_PARAMS,
    table_builder, reset_tables,
    compute_run_and_session_times, compute_run_combat_stats,
    jsonify_columns
)

_loaded = False
_active_params = None
_mem_conn = sqlite3.connect(":memory:", check_same_thread=False)

def ensure_loaded():
    global _loaded
    if not _loaded:
        regenerate(DEFAULT_PARAMS)

def regenerate(params: dict):
    global _loaded, _active_params

    old_params = copy.deepcopy(gen.DEFAULT_PARAMS) # save the current globals so we can restore them 

    gen.DEFAULT_PARAMS["difficulty_modifiers"].update(params["difficulty_modifiers"])  # write slider values directly into the generators
    gen.DEFAULT_PARAMS["base_hp"]               = params["base_hp"]
    gen.DEFAULT_PARAMS["hp_cap"]                = params["hp_cap"]
    gen.DEFAULT_PARAMS["hp_regen_choices"]      = params["hp_regen_choices"]
    gen.DEFAULT_PARAMS["enemy_base_damage"]     = params["enemy_base_damage"]
    gen.DEFAULT_PARAMS["enemy_damage_per_level"]= params["enemy_damage_per_level"]
    gen.DEFAULT_PARAMS["enemy_move_frequency"]  = params["enemy_move_frequency"]
    gen.DEFAULT_PARAMS["damage_variance"]       = params["damage_variance"]

    try:
        reset_tables()
        table_builder(80) 
        compute_run_and_session_times(Sessions, Runs, Fights)
        compute_run_combat_stats(Runs, Fights)
        _write_to_memory_db()
    finally:
        gen.DEFAULT_PARAMS["difficulty_modifiers"].update(old_params["difficulty_modifiers"]) # always restore originals, even if generation crashes
        gen.DEFAULT_PARAMS["base_hp"]               = old_params["base_hp"]
        gen.DEFAULT_PARAMS["hp_cap"]                = old_params["hp_cap"]
        gen.DEFAULT_PARAMS["hp_regen_choices"]      = old_params["hp_regen_choices"]
        gen.DEFAULT_PARAMS["enemy_base_damage"]     = old_params["enemy_base_damage"]
        gen.DEFAULT_PARAMS["enemy_damage_per_level"]= old_params["enemy_damage_per_level"]
        gen.DEFAULT_PARAMS["enemy_move_frequency"]  = old_params["enemy_move_frequency"]
        gen.DEFAULT_PARAMS["damage_variance"]       = old_params["damage_variance"]

    _active_params = copy.deepcopy(params)
    _loaded = True

def _write_to_memory_db():
    sessions_df = pd.DataFrame(Sessions)
    runs_df     = pd.DataFrame(Runs)
    fights_df   = pd.DataFrame(Fights)

    runs_df   = jsonify_columns(runs_df,   ["starting_moves", "starting_items",
                                             "items_bought", "upgrade_choice_made_per_level"])
    fights_df = jsonify_columns(fights_df, ["items_used", "moves_used",
                                             "enemy_moves_used", "Status_effects"])

    sessions_df.to_sql("sessions", _mem_conn, if_exists="replace", index=False)
    runs_df.to_sql("runs",         _mem_conn, if_exists="replace", index=False)
    fights_df.to_sql("fights",     _mem_conn, if_exists="replace", index=False)

def get_memory_conn():
    ensure_loaded()
    return _mem_conn

def get_active_params():
    ensure_loaded()
    return copy.deepcopy(_active_params)

def get_sessions():
    ensure_loaded()
    return Sessions

def get_runs():
    ensure_loaded()
    return Runs

def get_fights():
    ensure_loaded()
    return Fights