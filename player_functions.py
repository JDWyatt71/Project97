import pandas as pd
import random
import time
import json
from pprint import pprint

Sessions = {
    "session_id": [], 
    "player_id": [],
    "game_version": [],
    "total_play_time": [], #  #
    "run_id": [],
    "start_time": [], 
    "end_time": [],  #  #
    "successful": [], 
    "death_cause": [], 
    "starting_move": [],
    "starting_items": [],
    "upgrades_chosen": [], # need diff code to account for two and blank initially,
    "items_bought": [] #  #
}

Fights = {
          "fight_id" : [],  
          "run_id": [], 
          "enemy_id" : [],
          "level" : [],  
          "duration_time" : [], 
          "player_died" : [],
          "health_remaining" : [], 
          "attacks_attempted" : [],
          "attacks_missed" : [],
          "blocks_attempted" : [],
          "blocks_successful" : [],
          "enemy_attacks_attempted": [],
          "enemy_attacks_missed": [],
          "player_move_usage_slash": [],
          "player_move_usage_parry": [],
          "player_move_usage_dash": [],
          "enemy_move_usage_stab": [],
          "enemy_move_usage_throw": [],
          "items_used": []
}



def Skilled_type(level, game_version): # Skilled: efficient, low retries, bigger funnel
    factor = difficulty_modifiers[game_version]

    duration_time = round(add_variance(35 + level * 6.0 * factor) * 2) #  first line calculates a value 
    duration_time = clamp(duration_time, 10, 9999) #  Second line checks for correctness 
    Fights['duration_time'].append(duration_time)

    attacks_attempted = round(add_variance(8 + level * 1.5 * (0.9 + 0.2 * factor)))
    attacks_attempted = clamp(attacks_attempted, 1, 9999)
    Fights['attacks_attempted'].append(attacks_attempted)

    player_miss_rate = clamp(0.10 * factor, 0.05, 0.30)  # bigger factor - bigger miss rate
    attacks_missed = round(add_variance(attacks_attempted * player_miss_rate))
    attacks_missed = clamp(attacks_missed, 0, attacks_attempted)
    Fights['attacks_missed'].append(attacks_missed)

    blocks_attempted = round(add_variance(3 + level * 0.8 * (0.9 + 0.2 * factor)))
    blocks_attempted = clamp(blocks_attempted, 0, 9999)
    Fights['blocks_attempted'].append(blocks_attempted)

    block_success_rate = clamp(0.88 / factor, 0.35, 0.95)  # bigger factor - lower success rate
    blocks_successful = round(add_variance(blocks_attempted * block_success_rate))
    blocks_successful = clamp(blocks_successful, 0, blocks_attempted)
    Fights['blocks_successful'].append( blocks_successful)

    enemy_attacks_attempted = round(add_variance(6 + level * 2.2 * factor))
    enemy_attacks_attempted = clamp(enemy_attacks_attempted, 1, 9999)
    Fights['enemy_attacks_attempted'].append(enemy_attacks_attempted)

    enemy_miss_rate = clamp(0.35 / factor, 0.10, 0.60)  # bigger factor - smaller miss rate
    enemy_attacks_missed = round(add_variance(enemy_attacks_attempted * enemy_miss_rate))
    enemy_attacks_missed = clamp(enemy_attacks_missed, 0, enemy_attacks_attempted)
    Fights['enemy_attacks_missed'].append(enemy_attacks_missed)

    enemy_hits = enemy_attacks_attempted - enemy_attacks_missed # Needed for health calculation 
    hits_blocked = min(enemy_hits, blocks_successful)
    hits_taken = max(0, enemy_hits - hits_blocked)

    damage_per_hit = add_variance(3.0 + level * 0.35 * factor)
    mitigation = 0.20  # skilled reduces damage
    damage = hits_taken * damage_per_hit * (1.0 - mitigation)

    health_remaining = round(100 - damage)
    health_remaining = clamp(health_remaining, 0, 100)
    Fights['health_remaining'].append(health_remaining)

    player_move_usage_slash = round(add_variance(attacks_attempted * 0.70))
    player_move_usage_slash = clamp(player_move_usage_slash, 0, attacks_attempted)
    Fights["player_move_usage_slash"].append(player_move_usage_slash)

    player_move_usage_parry = round(add_variance(blocks_attempted * 0.60))
    player_move_usage_parry = clamp(player_move_usage_parry, 0, blocks_attempted)
    Fights["player_move_usage_parry"].append(player_move_usage_parry)

    player_move_usage_dash = round(add_variance(attacks_attempted * 0.18 * (1.0 + 0.15 * factor)))
    player_move_usage_dash = clamp(player_move_usage_dash, 0, attacks_attempted)
    Fights["player_move_usage_dash"].append(player_move_usage_dash)

    enemy_move_usage_stab = round(add_variance(enemy_attacks_attempted * 0.65))
    enemy_move_usage_stab = clamp(enemy_move_usage_stab, 0, enemy_attacks_attempted)
    Fights["enemy_move_usage_stab"].append(enemy_move_usage_stab)

    enemy_move_usage_throw = enemy_attacks_attempted - enemy_move_usage_stab
    Fights["enemy_move_usage_throw"].append(enemy_move_usage_throw)

    # Fights["items_used"].append() need to add items used 

def Aggressive_type(level, game_version): # Aggressive: high attacks, low blocks, faster fights, higher death risk, funnel ranges in length
    factor = difficulty_modifiers[game_version]

    duration_time = round(add_variance(30 + level * 5.2 * factor) * 2)
    duration_time = clamp(duration_time, 10, 9999)
    Fights['duration_time'].append(duration_time)

    attacks_attempted = round(add_variance(10 + level * 2.2 * (0.95 + 0.25 * factor)))
    attacks_attempted = clamp(attacks_attempted, 1, 9999)
    Fights['attacks_attempted'].append(attacks_attempted)

    player_miss_rate = clamp(0.22 * factor, 0.10, 0.55)
    attacks_missed = round(add_variance(attacks_attempted * player_miss_rate))
    attacks_missed = clamp(attacks_missed, 0, attacks_attempted)
    Fights['attacks_missed'].append(attacks_missed)

    blocks_attempted = round(add_variance(2 + level * 0.55 * (0.9 + 0.20 * factor)))
    blocks_attempted = clamp(blocks_attempted, 0, 9999)
    Fights['blocks_attempted'].append(blocks_attempted)

    block_success_rate = clamp(0.70 / factor, 0.20, 0.88)
    blocks_successful = round(add_variance(blocks_attempted * block_success_rate))
    blocks_successful = clamp(blocks_successful, 0, blocks_attempted)
    Fights['blocks_successful'].append(blocks_successful)

    enemy_attacks_attempted = round(add_variance(7 + level * 2.4 * factor))
    enemy_attacks_attempted = clamp(enemy_attacks_attempted, 1, 9999)
    Fights['enemy_attacks_attempted'].append(enemy_attacks_attempted)

    enemy_miss_rate = clamp(0.28 / factor, 0.06, 0.50)
    enemy_attacks_missed = round(add_variance(enemy_attacks_attempted * enemy_miss_rate))
    enemy_attacks_missed = clamp(enemy_attacks_missed, 0, enemy_attacks_attempted)
    Fights['enemy_attacks_missed'].append(enemy_attacks_missed)

    enemy_hits = enemy_attacks_attempted - enemy_attacks_missed
    hits_blocked = min(enemy_hits, blocks_successful)
    hits_taken = max(0, enemy_hits - hits_blocked)

    damage_per_hit = add_variance(3.4 + level * 0.45 * factor)
    mitigation = 0.08
    damage = hits_taken * damage_per_hit * (1.0 - mitigation)

    health_remaining = round(100 - damage)
    health_remaining = clamp(health_remaining, 0, 100)
    Fights['health_remaining'].append(health_remaining)

    player_move_usage_slash = round(add_variance(attacks_attempted * 0.82))
    player_move_usage_slash = clamp(player_move_usage_slash, 0, attacks_attempted)
    Fights["player_move_usage_slash"].append(player_move_usage_slash)

    player_move_usage_parry = round(add_variance(blocks_attempted * 0.35))
    player_move_usage_parry = clamp(player_move_usage_parry, 0, blocks_attempted)
    Fights["player_move_usage_parry"].append(player_move_usage_parry)

    player_move_usage_dash = round(add_variance(attacks_attempted * 0.10 * (1.0 + 0.10 * factor)))
    player_move_usage_dash = clamp(player_move_usage_dash, 0, attacks_attempted)
    Fights["player_move_usage_dash"].append(player_move_usage_dash)

    enemy_move_usage_stab = round(add_variance(enemy_attacks_attempted * 0.70))
    enemy_move_usage_stab = clamp(enemy_move_usage_stab, 0, enemy_attacks_attempted)
    Fights["enemy_move_usage_stab"].append(enemy_move_usage_stab)

    enemy_move_usage_throw = enemy_attacks_attempted - enemy_move_usage_stab
    Fights["enemy_move_usage_throw"].append(enemy_move_usage_throw)
    # Fights["items_used"].append() need to add items used 

def Cautious_type(level, game_version): # Cautious: more blocks, longer fights, fewer deaths early, struggles late, funnel ranges in length
    factor = difficulty_modifiers[game_version]

    duration_time = round(add_variance(48 + level * 8.2 * factor) * 2)
    duration_time = clamp(duration_time, 12, 9999)
    Fights['duration_time'].append(duration_time)

    attacks_attempted = round(add_variance(7 + level * 1.25 * (0.9 + 0.18 * factor)))
    attacks_attempted = clamp(attacks_attempted, 1, 9999)
    Fights['attacks_attempted'].append(attacks_attempted)

    player_miss_rate = clamp(0.18 * factor, 0.08, 0.40)
    attacks_missed = round(add_variance(attacks_attempted * player_miss_rate))
    attacks_missed = clamp(attacks_missed, 0, attacks_attempted)
    Fights['attacks_missed'].append(attacks_missed)

    blocks_attempted = round(add_variance(5 + level * 1.1 * (0.9 + 0.22 * factor)))
    blocks_attempted = clamp(blocks_attempted, 0, 9999)
    Fights['blocks_attempted'].append(blocks_attempted)

    block_success_rate = clamp(0.82 / factor, 0.30, 0.93)
    blocks_successful = round(add_variance(blocks_attempted * block_success_rate))
    blocks_successful = clamp(blocks_successful, 0, blocks_attempted)
    Fights['blocks_successful'].append(blocks_successful)

    enemy_attacks_attempted = round(add_variance(6 + level * 2.0 * factor))
    enemy_attacks_attempted = clamp(enemy_attacks_attempted, 1, 9999)
    Fights['enemy_attacks_attempted'].append(enemy_attacks_attempted)

    enemy_miss_rate = clamp(0.32 / factor, 0.08, 0.55)
    enemy_attacks_missed = round(add_variance(enemy_attacks_attempted * enemy_miss_rate))
    enemy_attacks_missed = clamp(enemy_attacks_missed, 0, enemy_attacks_attempted)
    Fights['enemy_attacks_missed'].append(enemy_attacks_missed)

    enemy_hits = enemy_attacks_attempted - enemy_attacks_missed
    hits_blocked = min(enemy_hits, blocks_successful)
    hits_taken = max(0, enemy_hits - hits_blocked)

    damage_per_hit = add_variance(3.2 + level * 0.40 * factor)
    mitigation = 0.12
    damage = hits_taken * damage_per_hit * (1.0 - mitigation)

    health_remaining = round(100 - damage)
    health_remaining = clamp(health_remaining, 0, 100)
    Fights['health_remaining'].append(health_remaining)

    player_move_usage_slash = round(add_variance(attacks_attempted * 0.60))
    player_move_usage_slash = clamp(player_move_usage_slash, 0, attacks_attempted)
    Fights["player_move_usage_slash"].append(player_move_usage_slash)

    player_move_usage_parry = round(add_variance(blocks_attempted * 0.70))
    player_move_usage_parry = clamp(player_move_usage_parry, 0, blocks_attempted)
    Fights["player_move_usage_parry"].append(player_move_usage_parry)

    player_move_usage_dash = round(add_variance(attacks_attempted * 0.28 * (1.0 + 0.20 * factor)))
    player_move_usage_dash = clamp(player_move_usage_dash, 0, attacks_attempted)
    Fights["player_move_usage_dash"].append(player_move_usage_dash)

    enemy_move_usage_stab = round(add_variance(enemy_attacks_attempted * 0.62))
    enemy_move_usage_stab = clamp(enemy_move_usage_stab, 0, enemy_attacks_attempted)
    Fights["enemy_move_usage_stab"].append(enemy_move_usage_stab)

    enemy_move_usage_throw = enemy_attacks_attempted - enemy_move_usage_stab
    Fights["enemy_move_usage_throw"].append(enemy_move_usage_throw)
    # Fights["items_used"].append() need to add items used 

def Inexperienced_type(level, game_version): # Inexperienced: misses more, retries often, quits early, smaller funnel
    factor = difficulty_modifiers[game_version]

    duration_time = round(add_variance(45 + level * 7.5 * factor) * 2)
    duration_time = clamp(duration_time, 12, 9999)
    Fights['duration_time'].append(duration_time)

    attacks_attempted = round(add_variance(6 + level * 1.1 * (0.9 + 0.25 * factor)))
    attacks_attempted = clamp(attacks_attempted, 1, 9999)
    Fights['attacks_attempted'].append(attacks_attempted)

    player_miss_rate = clamp(0.35 * factor, 0.20, 0.70)
    attacks_missed = round(add_variance(attacks_attempted * player_miss_rate))
    attacks_missed = clamp(attacks_missed, 0, attacks_attempted)
    Fights['attacks_missed'].append(attacks_missed)

    blocks_attempted = round(add_variance(2 + level * 0.5 * (0.9 + 0.25 * factor)))
    blocks_attempted = clamp(blocks_attempted, 0, 9999)
    Fights['blocks_attempted'].append(blocks_attempted)

    block_success_rate = clamp(0.55 / factor, 0.15, 0.80)
    blocks_successful = round(add_variance(blocks_attempted * block_success_rate))
    blocks_successful = clamp(blocks_successful, 0, blocks_attempted)
    Fights['blocks_successful'].append(blocks_successful)

    enemy_attacks_attempted = round(add_variance(7 + level * 2.6 * factor))
    enemy_attacks_attempted = clamp(enemy_attacks_attempted, 1, 9999)
    Fights['enemy_attacks_attempted'].append(enemy_attacks_attempted)

    enemy_miss_rate = clamp(0.25 / factor, 0.05, 0.45)
    enemy_attacks_missed = round(add_variance(enemy_attacks_attempted * enemy_miss_rate))
    enemy_attacks_missed = clamp(enemy_attacks_missed, 0, enemy_attacks_attempted)
    Fights['enemy_attacks_missed'].append(enemy_attacks_missed)

    enemy_hits = enemy_attacks_attempted - enemy_attacks_missed
    hits_blocked = min(enemy_hits, blocks_successful)
    hits_taken = max(0, enemy_hits - hits_blocked)

    damage_per_hit = add_variance(3.6 + level * 0.45 * factor)
    mitigation = 0.05
    damage = hits_taken * damage_per_hit * (1.0 - mitigation)

    health_remaining = round(100 - damage)
    health_remaining = clamp(health_remaining, 0, 100)
    Fights['health_remaining'].append(health_remaining)

    player_move_usage_slash = round(add_variance(attacks_attempted * 0.55))
    player_move_usage_slash = clamp(player_move_usage_slash, 0, attacks_attempted)
    Fights["player_move_usage_slash"].append(player_move_usage_slash)

    player_move_usage_parry = round(add_variance(blocks_attempted * 0.35))
    player_move_usage_parry = clamp(player_move_usage_parry, 0, blocks_attempted)
    Fights["player_move_usage_parry"].append(player_move_usage_parry)

    player_move_usage_dash = round(add_variance(attacks_attempted * 0.22 * (1.0 + 0.35 * factor)))
    player_move_usage_dash = clamp(player_move_usage_dash, 0, attacks_attempted)
    Fights["player_move_usage_dash"].append(player_move_usage_dash)

    enemy_move_usage_stab = round(add_variance(enemy_attacks_attempted * 0.72))
    enemy_move_usage_stab = clamp(enemy_move_usage_stab, 0, enemy_attacks_attempted)
    Fights["enemy_move_usage_stab"].append(enemy_move_usage_stab)

    enemy_move_usage_throw = enemy_attacks_attempted - enemy_move_usage_stab
    Fights["enemy_move_usage_throw"].append(enemy_move_usage_throw)



    # Fights["items_used"].append() need to add items used 


ids = set()
attacks = ['Basic Medium Height Punch','Basic High Height Punch','Basic Kick','Leg Sweep','Uppercut','Foot Shuffle','Knee Grapple','Grapple','Jumping Kick','Knee Kick','High Kick']
defense = ['Low Height Guard','Medium Height Guard','High Height Guard','Block','Counter','Dodge']
items =['Energy Bar','Water','Bandage','Medicine','Special Sandals','Vitamins','Special Herbal Remedy']
levels = list(range(1, 11))

# each value represents a player losing at levels 1-9
Skilled_lose_probs = [0.05, 0.05, 0.06, 0.07, 0.08, 0.09, 0.10, 0.11, 0.12]
Aggressive_lose_probs = [0.22, 0.20, 0.18, 0.17, 0.16, 0.16, 0.15, 0.14, 0.13] 
Cautious_lose_probs = [0.15, 0.14, 0.13, 0.13, 0.14, 0.15, 0.16, 0.17, 0.18]
Inexperienced_lose_probs = [0.30, 0.28, 0.25, 0.22, 0.20, 0.18, 0.16, 0.14, 0.12] 

probs_by_type = {
    "Inexperienced": [Inexperienced_lose_probs,0.02], # Second value is the probability of a player winning level 10
    "Cautious": [Cautious_lose_probs,0.08],
    "Aggressive": [Aggressive_lose_probs,0.12],
    "Skilled": [Skilled_lose_probs,0.4]
}

difficulty_modifiers = { 
    "Easy": 0.75,
    "Normal": 1.0,   
    "Hard": 1.3
}

def add_variance(num, spread=0.10):  # Multiplicative jitter  spread=0.10 = + or - 10%
    return num * (1.0 + random.uniform(-spread, spread)) 

def clamp(x, lo, hi): #  Ensures values are valid 
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