import random
import time
import sqlite3
import pandas as pd
import json

"""
This script generates a seeded telemetry dataset and writes it into a SQLite database. 
It also injects seeded data quality anomalies and logs them in a dedicated table.
The script generates 80 sessions by default (table_builder(80)).
Output:
- Seeded_Dataset.db
  Contains tables:
  - sessions
  - runs
  - fights
  - anomalies_sample  (records which rows were corrupted and how)

Dependencies:
- Python 3.10+ recommended
- pandas
- numpy
Expected console output:
- "Wrote SQLite database (with anomalies): Seeded_Dataset.db"
- "Added anomalies_sample table with 150 rows"
"""

Sessions = {
    "session_id": [], 
    "player_id": [],
    "total_play_time": [], #  
    "start_time": [], 
    "end_time": [],  #  
    
}

Runs = {
    "run_id" : [],
    "session_id": [],
    "run_time": [],
    "game_version": [],
    "successful": [],
    "starting_moves": [],
    "starting_items": [],
    "upgrade_choice_made_per_level": [],
    "items_bought": [],
    "level_finish": [],
    "attack_move_attempted": [],
    "attack_move_successful": [],
    "defend_move_attempt": [],
    "defend_move_successful": [],
    "death_cause": []

}

Fights = {
          "fight_id" : [],  
          "run_id": [], 
          "enemy_id" : [],
          "level" : [],  
          "duration_time" : [], 
          "player_died" : [],
          "health_remaining" : [], 
          "player_attacks_attempted" : [],
          "player_attacks_missed" : [],
          "blocks_attempted" : [],
          "blocks_successful" : [],
          "enemy_attacks_attempted": [],
          "enemy_attacks_missed": [],
          "items_used": [],
          "moves_used": [],
          "enemy_moves_used": [],
          "Status_effects" : []
}



def Skilled_type(level, game_version): # skilled: efficient, low retries, bigger funnel
    factor = DEFAULT_PARAMS["difficulty_modifiers"][game_version]

    duration_time = round(add_variance(35 + level * 6.0 * factor) * 2) #  first line calculates a value 
    duration_time = clamp(duration_time, 10, 9999) #  second line checks for correctness 
    Fights['duration_time'].append(duration_time)

    total_player_moves = clamp(moves_range_for_level(level), 15, 50)
    attack_ratio = 0.65
    # split into attack vs defend attempts depending on type
    player_attacks_attempted = clamp(int(round(total_player_moves * attack_ratio)), 1, 25)
    blocks_attempted = clamp(total_player_moves - player_attacks_attempted, 0, 25)

    Fights['player_attacks_attempted'].append(player_attacks_attempted)

    player_miss_rate = clamp(0.10 * factor, 0.05, 0.30)  # bigger factor - bigger miss rate
    player_attacks_missed = round(add_variance(player_attacks_attempted * player_miss_rate))
    player_attacks_missed = clamp(player_attacks_missed, 0, player_attacks_attempted)
    Fights["player_attacks_missed"].append(player_attacks_missed)

    Fights['blocks_attempted'].append(blocks_attempted)
    block_success_rate = clamp(0.88 / factor, 0.35, 0.95)  # bigger factor - lower success rate
    blocks_successful = round(add_variance(blocks_attempted * block_success_rate))
    blocks_successful = clamp(blocks_successful, 0, blocks_attempted)
    Fights['blocks_successful'].append( blocks_successful)

    enemy_total_moves = clamp(
    int(round(total_player_moves * (DEFAULT_PARAMS["enemy_move_frequency"] + 0.25 * factor))),
    10, 60
)
    enemy_attacks_attempted = enemy_total_moves
    Fights['enemy_attacks_attempted'].append(enemy_attacks_attempted)

    enemy_miss_rate = clamp(0.35 / factor, 0.10, 0.60)  # bigger factor - smaller miss rate
    enemy_attacks_missed = round(add_variance(enemy_attacks_attempted * enemy_miss_rate))
    enemy_attacks_missed = clamp(enemy_attacks_missed, 0, enemy_attacks_attempted)
    Fights['enemy_attacks_missed'].append(enemy_attacks_missed)

    enemy_hits = enemy_attacks_attempted - enemy_attacks_missed # needed for health calculation 
    hits_blocked = min(enemy_hits, blocks_successful)
    hits_taken = max(0, enemy_hits - hits_blocked)

    damage_per_hit = add_variance(
    DEFAULT_PARAMS["enemy_base_damage"] + level * DEFAULT_PARAMS["enemy_damage_per_level"] * factor,
    spread=DEFAULT_PARAMS["damage_variance"]
)
    mitigation = 0.20
    damage = hits_taken * damage_per_hit * (1.0 - mitigation)

   
    health_remaining = compute_hp_remaining(level, damage)
    Fights['health_remaining'].append(health_remaining)
    
def Aggressive_type(level, game_version): # aggressive: high attacks, low blocks, faster fights, higher death risk, funnel ranges in length
    factor = DEFAULT_PARAMS["difficulty_modifiers"][game_version]

    duration_time = round(add_variance(30 + level * 5.2 * factor) * 2)
    duration_time = clamp(duration_time, 10, 9999)
    Fights['duration_time'].append(duration_time)

    total_player_moves = clamp(moves_range_for_level(level), 15, 50)

    # split into attack vs defend attempts depending on type
    attack_ratio = 0.85
    player_attacks_attempted = clamp(int(round(total_player_moves * attack_ratio)), 1, 25)
    blocks_attempted = clamp(total_player_moves - player_attacks_attempted, 0, 25)
    Fights['player_attacks_attempted'].append(player_attacks_attempted)

    player_miss_rate = clamp(0.22 * factor, 0.10, 0.55)
    player_attacks_missed = round(add_variance(player_attacks_attempted * player_miss_rate))
    player_attacks_missed = clamp(player_attacks_missed, 0, player_attacks_attempted)
    Fights['player_attacks_missed'].append(player_attacks_missed)
    Fights['blocks_attempted'].append(blocks_attempted)

    block_success_rate = clamp(0.70 / factor, 0.20, 0.88)
    blocks_successful = round(add_variance(blocks_attempted * block_success_rate))
    blocks_successful = clamp(blocks_successful, 0, blocks_attempted)
    Fights['blocks_successful'].append(blocks_successful)

    enemy_total_moves = clamp(
    int(round(total_player_moves * (DEFAULT_PARAMS["enemy_move_frequency"] + 0.25 * factor))),
    10, 60
)
    enemy_attacks_attempted = enemy_total_moves
    Fights['enemy_attacks_attempted'].append(enemy_attacks_attempted)

    enemy_miss_rate = clamp(0.28 / factor, 0.06, 0.50)
    enemy_attacks_missed = round(add_variance(enemy_attacks_attempted * enemy_miss_rate))
    enemy_attacks_missed = clamp(enemy_attacks_missed, 0, enemy_attacks_attempted)
    Fights['enemy_attacks_missed'].append(enemy_attacks_missed)

    enemy_hits = enemy_attacks_attempted - enemy_attacks_missed
    hits_blocked = min(enemy_hits, blocks_successful)
    hits_taken = max(0, enemy_hits - hits_blocked)

    damage_per_hit = add_variance(
    DEFAULT_PARAMS["enemy_base_damage"] + level * DEFAULT_PARAMS["enemy_damage_per_level"] * factor,
    spread=DEFAULT_PARAMS["damage_variance"]
)  
    mitigation = 0.08
    damage = hits_taken * damage_per_hit * (1.0 - mitigation)


    health_remaining = compute_hp_remaining(level, damage)
    Fights['health_remaining'].append(health_remaining)

def Cautious_type(level, game_version): # cautious: more blocks, longer fights, fewer deaths early, struggles late, funnel ranges in length
    factor = DEFAULT_PARAMS["difficulty_modifiers"][game_version]

    duration_time = round(add_variance(48 + level * 8.2 * factor) * 2)
    duration_time = clamp(duration_time, 12, 9999)
    Fights['duration_time'].append(duration_time)

    total_player_moves = clamp(moves_range_for_level(level), 15, 50)
    attack_ratio = 0.5

    # split into attack vs defend attempts depending on type
    player_attacks_attempted = clamp(int(round(total_player_moves * attack_ratio)), 1, 25)
    blocks_attempted = clamp(total_player_moves - player_attacks_attempted, 0, 25)

    Fights['player_attacks_attempted'].append(player_attacks_attempted)

    player_miss_rate = clamp(0.18 * factor, 0.08, 0.40)
    player_attacks_missed = round(add_variance(player_attacks_attempted * player_miss_rate))
    player_attacks_missed = clamp(player_attacks_missed, 0,player_attacks_attempted)
    Fights['player_attacks_missed'].append(player_attacks_missed)

    Fights['blocks_attempted'].append(blocks_attempted)

    block_success_rate = clamp(0.82 / factor, 0.30, 0.93)
    blocks_successful = round(add_variance(blocks_attempted * block_success_rate))
    blocks_successful = clamp(blocks_successful, 0, blocks_attempted)
    Fights['blocks_successful'].append(blocks_successful)

    enemy_total_moves = clamp(
    int(round(total_player_moves * (DEFAULT_PARAMS["enemy_move_frequency"] + 0.25 * factor))),
    10, 60
)
    enemy_attacks_attempted = enemy_total_moves
    Fights['enemy_attacks_attempted'].append(enemy_attacks_attempted)

    enemy_miss_rate = clamp(0.32 / factor, 0.08, 0.55)
    enemy_attacks_missed = round(add_variance(enemy_attacks_attempted * enemy_miss_rate))
    enemy_attacks_missed = clamp(enemy_attacks_missed, 0, enemy_attacks_attempted)
    Fights['enemy_attacks_missed'].append(enemy_attacks_missed)

    enemy_hits = enemy_attacks_attempted - enemy_attacks_missed
    hits_blocked = min(enemy_hits, blocks_successful)
    hits_taken = max(0, enemy_hits - hits_blocked)

   
    damage_per_hit = add_variance(
    DEFAULT_PARAMS["enemy_base_damage"] + level * DEFAULT_PARAMS["enemy_damage_per_level"] * factor,
    spread=DEFAULT_PARAMS["damage_variance"]
) 
    mitigation = 0.12
    damage = hits_taken * damage_per_hit * (1.0 - mitigation)

    health_remaining = compute_hp_remaining(level, damage)
    Fights['health_remaining'].append(health_remaining)

def Inexperienced_type(level, game_version): # inexperienced: misses more, retries often, quits early, smaller funnel
    factor = DEFAULT_PARAMS["difficulty_modifiers"][game_version]

    duration_time = round(add_variance(45 + level * 7.5 * factor) * 2)
    duration_time = clamp(duration_time, 12, 9999)
    Fights['duration_time'].append(duration_time)

    total_player_moves = clamp(moves_range_for_level(level), 15, 50)
    attack_ratio = 0.6

    # split into attack vs defend attempts depending on type
    player_attacks_attempted = clamp(int(round(total_player_moves * attack_ratio)), 1, 25)
    blocks_attempted = clamp(total_player_moves - player_attacks_attempted, 0, 25)

    Fights['player_attacks_attempted'].append(player_attacks_attempted)

    player_miss_rate = clamp(0.35 * factor, 0.20, 0.70)
    player_attacks_missed = round(add_variance(player_attacks_attempted * player_miss_rate))
    player_attacks_missed = clamp(player_attacks_missed, 0,player_attacks_attempted)
    Fights['player_attacks_missed'].append(player_attacks_missed)

    Fights['blocks_attempted'].append(blocks_attempted)

    block_success_rate = clamp(0.55 / factor, 0.15, 0.80)
    blocks_successful = round(add_variance(blocks_attempted * block_success_rate))
    blocks_successful = clamp(blocks_successful, 0, blocks_attempted)
    Fights['blocks_successful'].append(blocks_successful)

    enemy_total_moves = clamp(
    int(round(total_player_moves * (DEFAULT_PARAMS["enemy_move_frequency"] + 0.25 * factor))),
    10, 60
)
    enemy_attacks_attempted = enemy_total_moves
    Fights['enemy_attacks_attempted'].append(enemy_attacks_attempted)

    enemy_miss_rate = clamp(0.25 / factor, 0.05, 0.45)
    enemy_attacks_missed = round(add_variance(enemy_attacks_attempted * enemy_miss_rate))
    enemy_attacks_missed = clamp(enemy_attacks_missed, 0, enemy_attacks_attempted)
    Fights['enemy_attacks_missed'].append(enemy_attacks_missed)

    enemy_hits = enemy_attacks_attempted - enemy_attacks_missed
    hits_blocked = min(enemy_hits, blocks_successful)
    hits_taken = max(0, enemy_hits - hits_blocked)


    damage_per_hit = add_variance(
    DEFAULT_PARAMS["enemy_base_damage"] + level * DEFAULT_PARAMS["enemy_damage_per_level"] * factor,
    spread=DEFAULT_PARAMS["damage_variance"]
) 
    mitigation = 0.6
    damage = hits_taken * damage_per_hit * (1.0 - mitigation)

    health_remaining = compute_hp_remaining(level, damage)
    Fights['health_remaining'].append(health_remaining)

ids = set()
attacks = ['Basic Medium Height Punch','Basic High Height Punch','Basic Kick','Leg Sweep','Uppercut','Foot Shuffle','Knee Grapple','Grapple','Jumping Kick','Knee Kick','High Kick','Reverse-Kick','Trip-Kick','Floor Throwdown','Head-drop','Over-back Throw','Arm Cross Hold','Choke','Dash Throw','Trip Throw','Arm Breaker','Pummel','Arm Twister']
defenses = ['Low Height Guard','Medium Height Guard','High Height Guard','Block','Counter','Dodge']
items =['Energy Bar','Water','Bandage','Medicine','Special Sandals','Vitamins','Special Herbal Remedy']
Status_effects =['Adrenaline Rush','Enraged','Blindness','Slow','Wind','Prone','Broken Bones','Bleed','None']
levels = list(range(1, 11))
all_moves = attacks + defenses

# each value represents a player losing at levels 1-9
Skilled_lose_probs = [0.05, 0.05, 0.06, 0.07, 0.08, 0.09, 0.10, 0.11, 0.12]
Aggressive_lose_probs = [0.22, 0.20, 0.18, 0.17, 0.16, 0.16, 0.15, 0.14, 0.13] 
Cautious_lose_probs = [0.15, 0.14, 0.13, 0.13, 0.14, 0.15, 0.16, 0.17, 0.18]
Inexperienced_lose_probs = [0.30, 0.28, 0.25, 0.22, 0.20, 0.18, 0.16, 0.14, 0.12] 

probs_by_type = {
    "Inexperienced": [Inexperienced_lose_probs,0.02], # second value is the probability of a player winning level 10
    "Cautious": [Cautious_lose_probs,0.08],
    "Aggressive": [Aggressive_lose_probs,0.12],
    "Skilled": [Skilled_lose_probs,0.4]
}

DEFAULT_PARAMS = {
    "difficulty_modifiers": {
        "Easy":   0.75,
        "Normal": 1.0,
        "Hard":   1.3,
    },
    "base_hp":           50,            # 50
    "hp_cap":            120,             # 120
    "hp_regen_choices":  [5, 15],  # [5, 15]
    "enemy_base_damage":     2.5,   # base damage per enemy hit before level scaling
    "enemy_damage_per_level": 1.2,  # how much harder enemies hit each level
    "enemy_move_frequency":  0.90,  # ratio of enemy attacks to player moves (base)
    "damage_variance":       0.20,  # ±spread on all damage rolls

    "lose_probs": {
        "Skilled":       list(Skilled_lose_probs),
        "Aggressive":    list(Aggressive_lose_probs),
        "Cautious":      list(Cautious_lose_probs),
        "Inexperienced": list(Inexperienced_lose_probs),
    },
    # win probability at level 10 per player type
    "win_probs": {
        "Skilled":       0.40,
        "Aggressive":    0.12,
        "Cautious":      0.08,
        "Inexperienced": 0.02,
    },
}
 
player_types= {  # trick to execute correct code without elif statements 
    "Inexperienced": Inexperienced_type,
    "Cautious": Cautious_type,
    "Aggressive": Aggressive_type,
    "Skilled": Skilled_type
}
 



def compute_hp_remaining(level: int, damage: float) -> int:  # HP starts at 50 and passively increases +5 or +15 each level.
    hp_regen = random.choice(DEFAULT_PARAMS["hp_regen_choices"]) 
    HP_CAP =DEFAULT_PARAMS["hp_cap"]
    BASE_HP = DEFAULT_PARAMS["base_hp"]
    max_hp = min(HP_CAP, BASE_HP + hp_regen * (level - 1))
    hp_remaining = round(max_hp - damage)
    return clamp(hp_remaining, 0, max_hp)




def moves_range_for_level(level: int) -> int:  # total moves should range 15 -> 50 as level increases.

    base = 15 + (level - 1) * (35 / 9)  # 15 to 50
    return int(round(add_variance(base, spread=0.15)))



def add_variance(num, spread=0.10):  # multiplicative jitter. spread=0.10 => ±10%
    return num * (1.0 + random.uniform(-spread, spread)) 

def clamp(x, lo, hi): #  ensures values are valid 
    return max(lo, min(x, hi))

def bias_true_or_false(bias):  
    random_number = random.random()
    if random_number < bias:
        return True
    else:
        return False
    
def unique_num():
    while True:
        random_int = random.randint(1000, 9999)
        if random_int not in ids:
            ids.add(random_int)
            return random_int

def random_time_generator():
    random_seconds = random.randint(0, 86399)
    time_struct = time.gmtime(random_seconds)
    military_time = time.strftime("%H:%M", time_struct)
    return military_time

def time_to_seconds(t):
    h, m = map(int, t.split(":"))
    return h * 3600 + m * 60

def seconds_to_time(seconds):
    seconds = seconds % 86400  # wrap over 24h
    h = seconds // 3600
    m = (seconds % 3600) // 60
    return f"{h:02d}:{m:02d}"

def compute_run_times(Sessions, Fights):
    run_duration = {}
    for run_id, duration in zip(Fights["run_id"], Fights["duration_time"]):
        run_duration[run_id] = run_duration.get(run_id, 0) + duration

    # ensure lists are correct size
    n = len(Sessions["session_id"])
    Sessions["total_play_time"] = [0] * n
    Sessions["end_time"] = ["00:00"] * n

    for i in range(n):
        run_id = Sessions["run_id"][i]
        start_time = Sessions["start_time"][i]

        total_play_time = run_duration.get(run_id, 0)
        Sessions["total_play_time"][i] = total_play_time

        start_seconds = time_to_seconds(start_time)
        end_seconds = start_seconds + total_play_time
        Sessions["end_time"][i] = seconds_to_time(end_seconds)

def pick_moves(): # randomly select moves, could be tweaked to take into account player type
    moves=[]
    while len(moves) < 3:
        move = random.choice(attacks)
        if move in moves:
            continue 
        moves.append(move)
    moves.append(random.choice(defenses))
    return moves

def pick_x_itemshop(x): #  can be changed to take player type into acount i.e. better players will pick better items
    player_items = []
    for i in range(x):
        player_items.append(random.choice(items))
    return player_items

def items_used(starting_items: list[str], p_use_any: float = 0.35) -> list[str]:
   
    if not starting_items:
        return []
    if random.random() > p_use_any:
        return []

    max_k = min(2, len(starting_items))
    k = 1 if max_k == 1 else random.choice([1, 2])

    return random.sample(starting_items, k=k)

def items_bought(levels_completed: int, starting_items: list[str]) -> list[str]: #  needs to take into account starting items
    bought = []
    for _ in range(levels_completed):
        bought.extend(pick_x_itemshop(random.randint(0, 2)))  # flat
    return starting_items + bought

def pick_moves_for_each_level(player_moves: set[str]):  # Logic ensuers two choices are made
    chosen = []
    attack_choices = [a for a in attacks if a not in player_moves]
    if attack_choices:
        a = random.choice(attack_choices)
        chosen.append(a)
        player_moves.add(a)

    defense_choices = [d for d in defenses if d not in player_moves]
    if defense_choices:
        d = random.choice(defense_choices)
        chosen.append(d)
        player_moves.add(d)
    return chosen

def upgrade_choice_made_per_level_calculator(final_level, starting_moves ):  # called when player dies
    upgrade_choice_made_per_level = {} 
    player_moves = set(starting_moves)
    for i in range(1,final_level):
        upgrade_choice_made_per_level[i] = pick_moves_for_each_level(player_moves)

    return upgrade_choice_made_per_level


def status():  # called to give player a status effect in a given fight 
    k = random.randint(1, 2)  
    return random.sample(Status_effects, k=k)
    

def num_of_runs_generator(player_type): # assuming player type dictates number of possible runs
    if player_type == 'Aggressive':
       runs = random.randrange(1,15)
    elif player_type == 'Cautious':
       runs = random.randrange(1,9)
    elif player_type == 'Inexperienced':
       runs = random.randrange(1,3)
    else:
       runs = random.randrange(1,2)
    return runs


def assert_equal_lengths(d: dict, name: str):
    lens = {k: len(v) for k, v in d.items()}
    if len(set(lens.values())) != 1:
        raise ValueError(f"{name} column length mismatch:\n{lens}")
                         

def scale_probs(loss_probs, win_prob, game_version): #  tweak probabilities in dictionary to take into account game version (difficulty and level)
    factor = DEFAULT_PARAMS["difficulty_modifiers"][game_version]
    scaled_loss_probs = [min(p * factor, 0.95) for p in loss_probs]
    scaled_win_prob = max(min(win_prob / factor, 0.95),0.01)
    return scaled_loss_probs, scaled_win_prob

def player_stuff_used(stuff,x,y):
        k = random.randint(x, y)  # up to 2 effects for a fight
        return random.sample(stuff, k=k)

def enemy_stuff_used(stuff,k):  # called when calculating moves used 
    return random.sample(stuff, k=k)

def table_builder(num_of_sessions, params=None): #   called to make both tables
    for i in range(num_of_sessions):
        player_type = random.choice(['Aggressive','Cautious','Inexperienced','Skilled'])
        session_builder(player_type, params)

def session_builder(player_type,params=None): # called to make info for one player
   num_of_runs = num_of_runs_generator(player_type)
   session_id = unique_num()
   player_id = unique_num()
   Sessions["session_id"].append(session_id)
   Sessions["player_id"].append(player_id)
   Sessions["start_time"].append(random_time_generator())
   for i in range(num_of_runs):
      run_id = unique_num()
      fight_table_builder(player_type, run_id, session_id, params)

def fight_table_builder(player_type, run_id, session_id, params=None): 
    game_version = random.choice(['Easy','Normal','Hard'])
    loss_probs, win_prob = DEFAULT_PARAMS["lose_probs"][player_type],DEFAULT_PARAMS["win_probs"][player_type]
    loss_probs, win_prob = scale_probs(loss_probs, win_prob, game_version)

    starting_items = pick_x_itemshop(2)
    starting_moves = pick_moves()

    successful = 0
    final_level = 10 # assume player makes it to level 10 

    #  Levels 1 to 9 
    for level in range(1, 10):
        Fights["level"].append(level)
        Fights["run_id"].append(run_id)
        Fights["fight_id"].append(unique_num())
        Fights["enemy_id"].append(unique_num())
        Fights["player_died"].append(0)
        Fights["Status_effects"].append(status())

        #  Create placeholders 
        Fights["items_used"].append([])
        Fights["moves_used"].append([])
        Fights["enemy_moves_used"].append([])
        Fights["items_used"][-1] =items_used(starting_items, p_use_any=0.35)

        player_types[player_type](level, game_version)

        if Fights["health_remaining"][-1] == 0:
            Fights["player_died"][-1] = 1
            final_level = 10
            successful = level
            upgrade_choice_made_per_level = upgrade_choice_made_per_level_calculator(final_level, starting_moves)
            run_table_builder(run_id, session_id, game_version, upgrade_choice_made_per_level,
                      starting_items, final_level, starting_moves, successful)
            return

        upgrades_so_far = upgrade_choice_made_per_level_calculator(level, starting_moves)
        upgrades_flat = [m for lst in upgrades_so_far.values() for m in lst]
        player_move_pool = starting_moves + upgrades_flat
        moves_used = player_stuff_used(player_move_pool, 1, min(4, len(player_move_pool)))
        enemy_moves = enemy_stuff_used(all_moves, len(moves_used))
        Fights["moves_used"][-1] = moves_used
        Fights["enemy_moves_used"][-1] = enemy_moves

        if bias_true_or_false(loss_probs[level - 1]): #  checks to see if player died at a current level
            Fights["player_died"][-1] = 1
            Fights["health_remaining"][-1] = 0
            final_level = level
            successful = 0
            upgrade_choice_made_per_level = upgrade_choice_made_per_level_calculator(final_level, starting_moves)

            run_table_builder(
                run_id, session_id, game_version,
                upgrade_choice_made_per_level, starting_items,
                final_level, starting_moves, successful
            )
            return  

    # level 10 
    Fights["level"].append(10)
    Fights["run_id"].append(run_id)
    Fights["fight_id"].append(unique_num())
    Fights["enemy_id"].append(unique_num())
    Fights["player_died"].append(0)
    Fights["Status_effects"].append(status())
    Fights["items_used"].append([])
    Fights["moves_used"].append([])
    Fights["enemy_moves_used"].append([])
    Fights["items_used"][-1] =items_used(starting_items, p_use_any=0.35)
    player_types[player_type](10, game_version)

    if Fights["health_remaining"][-1] == 0:
        Fights["player_died"][-1] = 1
        final_level = 10
        successful = 0
        upgrade_choice_made_per_level = upgrade_choice_made_per_level_calculator(final_level, starting_moves)
        run_table_builder(run_id, session_id, game_version, upgrade_choice_made_per_level,
                      starting_items, final_level, starting_moves, successful)
        return
    
    upgrade_choice_made_per_level = upgrade_choice_made_per_level_calculator(10, starting_moves)
    upgrades_flat = [m for lst in upgrade_choice_made_per_level.values() for m in lst]
    player_move_pool = starting_moves + upgrades_flat

    moves_used = player_stuff_used(player_move_pool, 1, min(4, len(player_move_pool)))
    enemy_moves = enemy_stuff_used(all_moves, len(moves_used))
    Fights["moves_used"][-1] = moves_used
    Fights["enemy_moves_used"][-1] = enemy_moves
    final_level = 10
    if bias_true_or_false(win_prob):
        successful = 1
    else:
        successful = 0
        Fights["player_died"][-1] = 1
        Fights["health_remaining"][-1] = 0

    run_table_builder(
        run_id, session_id, game_version,
        upgrade_choice_made_per_level, starting_items,
        final_level, starting_moves, successful
    )

def run_table_builder(run_id, session_id, game_version, upgrade_choice_made_per_level, starting_items, final_level, starting_moves,successful):
    Runs["run_id"].append(run_id)
    Runs["session_id"].append(session_id)
    Runs["game_version"].append(game_version)
    Runs["successful"].append(successful)
    Runs["starting_moves"].append(starting_moves)
    Runs["starting_items"].append(starting_items)


    Runs["upgrade_choice_made_per_level"].append(upgrade_choice_made_per_level)
    Runs["level_finish"].append(final_level)
    Runs["death_cause"].append(random.choice(attacks))

    levels_completed = max(0, final_level - 1)
    Runs["items_bought"].append(items_bought(levels_completed,starting_items))

    Runs["run_time"].append(None)  # filled later by compute_run_and_session_times


def compute_run_combat_stats(Runs, Fights):
    # accumulator per run
    combat_map = {}
    for i in range(len(Fights["run_id"])):
        run_id = Fights["run_id"][i]

        attacks_attempted = Fights["player_attacks_attempted"][i]
        attacks_missed = Fights["player_attacks_missed"][i]
        blocks_attempted = Fights["blocks_attempted"][i]
        blocks_successful = Fights["blocks_successful"][i]

        if run_id not in combat_map:
            combat_map[run_id] = {
                "attack_attempted": 0,
                "attack_successful": 0,
                "defend_attempt": 0,
                "defend_successful": 0
            }

        combat_map[run_id]["attack_attempted"] += attacks_attempted
        combat_map[run_id]["attack_successful"] += (attacks_attempted - attacks_missed)
        combat_map[run_id]["defend_attempt"] += blocks_attempted
        combat_map[run_id]["defend_successful"] += blocks_successful


    # now write into Runs table in order
    Runs["attack_move_attempted"] = []
    Runs["attack_move_successful"] = []
    Runs["defend_move_attempt"] = []
    Runs["defend_move_successful"] = []
    for run_id in Runs["run_id"]:
        stats = combat_map.get(run_id, None)
        Runs["attack_move_attempted"].append(stats["attack_attempted"])
        Runs["attack_move_successful"].append(stats["attack_successful"])
        Runs["defend_move_attempt"].append(stats["defend_attempt"])
        Runs["defend_move_successful"].append(stats["defend_successful"])

def compute_run_and_session_times(Sessions, Runs, Fights):
    # sum duration_time per run_id
    run_time_map = {}
    for rid, dur in zip(Fights["run_id"], Fights["duration_time"]):
        run_time_map[rid] = run_time_map.get(rid, 0) + int(dur)

    # fill Runs.run_time in the same order as Runs.run_id
    Runs["run_time"] = [run_time_map.get(rid, 0) for rid in Runs["run_id"]]

    # sum run_time per session_id
    session_total_map = {}
    for sid, rid in zip(Runs["session_id"], Runs["run_id"]):
        session_total_map[sid] = session_total_map.get(sid, 0) + run_time_map.get(rid, 0)

    # fill Sessions totals and end_time
    Sessions["total_play_time"] = []
    Sessions["end_time"] = []
    for sid, start in zip(Sessions["session_id"], Sessions["start_time"]):
        total = session_total_map.get(sid, 0)
        Sessions["total_play_time"].append(total)
        Sessions["end_time"].append(seconds_to_time(time_to_seconds(start) + total))


def jsonify_columns(df: pd.DataFrame, cols: list[str]) -> pd.DataFrame:# used to convert telemetry values into JSON-formatted strings for storage
    for c in cols:
        if c in df.columns:
            df[c] = df[c].apply(lambda x: None if x is None else json.dumps(x))
    return df

def write_to_sqlite(Sessions: dict, Runs: dict, Fights: dict, db_path: str = "Seeded_Dataset.db") -> str: # converts in-memory telemetry dictionaries into SQLite relational tables
   # ensure all columns within each table have equal lengths
    assert_equal_lengths(Sessions, "Sessions")
    assert_equal_lengths(Runs, "Runs")
    assert_equal_lengths(Fights, "Fights")

    # SQLite expects row-oriented data, so we convert to DataFrames
    sessions_df = pd.DataFrame(Sessions)
    runs_df = pd.DataFrame(Runs)
    fights_df = pd.DataFrame(Fights)

    # SQLite does not support native list/dict types so we convert some telemetry values
    runs_df = jsonify_columns(runs_df, ["starting_moves", "starting_items", "items_bought", "upgrade_choice_made_per_level"])
    fights_df = jsonify_columns(fights_df, ["items_used", "moves_used", "enemy_moves_used", "Status_effects"])

    # write tables into SQLite database
    with sqlite3.connect(db_path) as conn:
        sessions_df.to_sql("sessions", conn, if_exists="replace", index=False)
        runs_df.to_sql("runs", conn, if_exists="replace", index=False)
        fights_df.to_sql("fights", conn, if_exists="replace", index=False)

        conn.execute("CREATE INDEX IF NOT EXISTS idx_runs_run_id     ON runs(run_id)")
        conn.execute("CREATE INDEX IF NOT EXISTS idx_runs_session_id ON runs(session_id)")
        conn.execute("CREATE INDEX IF NOT EXISTS idx_fights_run_id   ON fights(run_id)")
        conn.commit()

    return db_path

def _row_to_dict(table_dict: dict, row_index: int) -> dict: # extract one row from dict-of-columns into a normal dict
    return {col: table_dict[col][row_index] for col in table_dict.keys()}

def reset_tables():
   # clear all telemetry dicts before a fresh run
    for table in [Sessions, Runs, Fights]:
        for key in table:
            table[key].clear()
    ids.clear() 

def inject_and_extract_anomalies_sample(
    Sessions, Runs, Fights,
    total_bad_rows: int = 150,
    max_anomalies_per_row: int = 3
):
    tables = {
        "sessions": Sessions,
        "runs": Runs,
        "fights": Fights
    }

    numeric_fields = {
        "sessions": ["total_play_time"],  # fields that can be numericallu changed
        "runs": [
            "run_time", "attack_move_attempted", "attack_move_successful",
            "defend_move_attempt", "defend_move_successful", "level_finish"
        ],
        "fights": [
            "duration_time", "player_attacks_attempted", "player_attacks_missed",
            "blocks_attempted", "blocks_successful", "enemy_attacks_attempted",
            "enemy_attacks_missed", "health_remaining"
        ]
    }

    nullable_fields = {
        "sessions": ["end_time", "start_time"],  # fields that can be swapped with none
        "runs": ["starting_items", "starting_moves", "items_bought", "upgrade_choice_made_per_level"],
        "fights": ["items_used", "moves_used", "enemy_moves_used", "Status_effects"]
    }

    # build all possible rows
    all_rows = []
    for name, t in tables.items():
        n = len(next(iter(t.values()), []))
        all_rows.extend([(name, i) for i in range(n)])

    if not all_rows:
        return []

    total_bad_rows = min(total_bad_rows, len(all_rows))
    chosen = random.sample(all_rows, k=total_bad_rows)

    sample_rows = []

    for table_name, row in chosen:
        t = tables[table_name]

        # only columns that exist
        num_cols = [c for c in numeric_fields.get(table_name, []) if c in t]
        nul_cols = [c for c in nullable_fields.get(table_name, []) if c in t]

        anomalies_applied = []

        k = random.randint(1, max_anomalies_per_row)
        # insert anomalies
        for _ in range(k):
            can_missing = len(nul_cols) > 0
            can_negative = len(num_cols) > 0
            if not can_missing and not can_negative:
                break

            do_missing = can_missing and (not can_negative or random.random() < 0.5)

            if do_missing:
                col = random.choice(nul_cols)
                t[col][row] = None
                anomalies_applied.append({"type": "missing", "column": col})
            else:
                col = random.choice(num_cols)
                val = t[col][row]
                if isinstance(val, (int, float)) and val is not None:
                    new_val = -abs(random.randint(1, max(5, int(abs(val)) + 5)))
                    t[col][row] = new_val
                    anomalies_applied.append({"type": "negative", "column": col, "new_value": new_val})
                elif can_missing:
                    col2 = random.choice(nul_cols)
                    t[col2][row] = None
                    anomalies_applied.append({"type": "missing_fallback", "column": col2})

        # after corruption, snapshot the row
        row_dict = _row_to_dict(t, row)

        # some useful IDs for linkage
        primary_id = None
        session_id = None

        if table_name == "sessions":
            primary_id = row_dict.get("session_id")
            session_id = row_dict.get("session_id")
        elif table_name == "runs":
            primary_id = row_dict.get("run_id")
            session_id = row_dict.get("session_id")
        elif table_name == "fights":
            primary_id = row_dict.get("fight_id")
            # best effort is when fights have run_id and session_id is not in fights

        sample_rows.append({
            "source_table": table_name,
            "row_index": row,
            "primary_id": primary_id,
            "session_id": session_id,
            "anomalies_found": json.dumps(anomalies_applied),
            "row_json": json.dumps(row_dict, default=str)
        })

    return sample_rows

def write_anomalies_sample_table(db_path: str, sample_rows: list[dict]):
    df = pd.DataFrame(sample_rows)
    with sqlite3.connect(db_path) as conn:
        df.to_sql("anomalies_sample", conn, if_exists="replace", index=False)

        conn.execute("CREATE INDEX IF NOT EXISTS idx_anom_source ON anomalies_sample(source_table)")
        conn.execute("CREATE INDEX IF NOT EXISTS idx_anom_primary ON anomalies_sample(primary_id)")
        conn.execute("CREATE INDEX IF NOT EXISTS idx_anom_session ON anomalies_sample(session_id)")
        conn.commit()
       
if __name__ == "__main__":
    table_builder(80)  

    compute_run_and_session_times(Sessions, Runs, Fights)
    compute_run_combat_stats(Runs, Fights)
    sample_rows = inject_and_extract_anomalies_sample(
        Sessions, Runs, Fights,
        total_bad_rows=150,
        max_anomalies_per_row=3
    ) 
    db_file = write_to_sqlite(Sessions, Runs, Fights, db_path="Seeded_Dataset.db")
    print("Wrote SQLite database (with anomalies):", db_file)


    write_anomalies_sample_table(db_file, sample_rows)
    print("Added anomalies_sample table with", len(sample_rows), "rows")

    from decision_log import seed_decision_log
    seed_decision_log(db_file)
    print("Seeded decision log with 30 entries.")