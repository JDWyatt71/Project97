from Seeded_Dataset_Generator import DEFAULT_PARAMS
from state_manager import ensure_loaded, get_sessions, get_runs, get_fights, get_active_params
import random


# simplified move data  

DAMAGE_TIERS = {
    "none": 0,
    "very_low": 2,
    "low": 4,
    "low_medium": 5,
    "medium": 7,
    "high": 10,
    "very_high": 14,
}

MOVE_META = {
    "Punch": {
        "category": "punch",
        "mode": "attack",
        "variants": {
            "high": {"height": "high", "damage": "low"},
            "mid": {"height": "mid", "damage": "low"},
        },
        "tags": []
    },
    "Uppercut": {
        "category": "punch",
        "mode": "attack",
        "variants": {
            "high": {"height": "high", "damage": "high"},
        },
        "tags": ["ignores_guard"]
    },
    "Push-Kick": {
        "category": "kick",
        "mode": "attack",
        "variants": {
            "high": {"height": "high", "damage": "medium"},
            "mid": {"height": "mid", "damage": "low"},
            "low": {"height": "low", "damage": "low"},
        },
        "tags": []
    },
    "Reverse-Kick": {
        "category": "kick",
        "mode": "attack",
        "variants": {
            "high": {"height": "high", "damage": "medium"},
            "mid": {"height": "mid", "damage": "medium"},
        },
        "tags": ["ignores_guard"]
    },
    "Spin Kick": {
        "category": "kick",
        "mode": "attack",
        "variants": {
            "high": {"height": "high", "damage": "medium"},
            "mid": {"height": "mid", "damage": "medium"},
            "low": {"height": "low", "damage": "medium"},
        },
        "tags": []
    },
    "Trip-Kick": {
        "category": "kick",
        "mode": "attack",
        "variants": {
            "low": {"height": "low", "damage": "very_low"},
        },
        "tags": []
    },
    "Floor Throwdown": {
        "category": "grapple",
        "mode": "attack",
        "variants": {
            "grapple": {"height": "grapple", "damage": "medium"},
        },
        "tags": ["grapple", "ignores_block", "ignores_counter"]
    },
    "Head-drop": {
        "category": "grapple",
        "mode": "attack",
        "variants": {
            "grapple": {"height": "grapple", "damage": "very_high"},
        },
        "tags": ["grapple", "ignores_block", "ignores_counter"]
    },
    "Over-back Throw": {
        "category": "grapple",
        "mode": "attack",
        "variants": {
            "grapple": {"height": "grapple", "damage": "high"},
        },
        "tags": ["grapple", "ignores_block", "ignores_counter"]
    },
    "Arm Twister": {
        "category": "grapple",
        "mode": "attack",
        "variants": {
            "grapple": {"height": "grapple", "damage": "medium"},
        },
        "tags": ["grapple", "ignores_block", "ignores_counter"]
    },
    "Choke": {
        "category": "grapple",
        "mode": "attack",
        "variants": {
            "grapple": {"height": "grapple", "damage": "low_medium"},
        },
        "tags": ["grapple", "ignores_block", "ignores_counter"]
    },
    "Block": {
        "category": "defense",
        "mode": "defense",
        "variants": {
            "high": {"height": "high", "damage": "none"},
            "mid": {"height": "mid", "damage": "none"},
            "low": {"height": "low", "damage": "none"},
        },
        "tags": ["negates_damage"]
    },
    "Guard": {
        "category": "defense",
        "mode": "defense",
        "variants": {
            "high": {"height": "high", "damage": "none"},
            "mid": {"height": "mid", "damage": "none"},
        },
        "tags": ["reduces_damage"]
    },
    "Counter": {
        "category": "defense",
        "mode": "defense",
        "variants": {
            "high": {"height": "high", "damage": "low"},
            "mid": {"height": "mid", "damage": "low"},
            "low": {"height": "low", "damage": "low"},
        },
        "tags": ["negates_damage", "return_damage"]
    },
    "Dodge": {
        "category": "defense",
        "mode": "defense",
        "variants": {
            "evade": {"height": "evade", "damage": "none"},
        },
        "tags": ["evasion"]
    },
    "Foot Shuffle": {
        "category": "defense",
        "mode": "defense",
        "variants": {
            "evade": {"height": "evade", "damage": "none"},
        },
        "tags": ["minor_evasion"]
    },
}

def get_run_metadata(): # get run-level information for the single generated run
    Runs     = get_runs()
    Sessions = get_sessions()
    return {
        "player_id": Sessions["player_id"][0],
        "session_id": Sessions["session_id"][0],
        "run_id": Runs["run_id"][0],
        "successful": Runs["successful"][0],
        "level_finish": Runs["level_finish"][0],
        "starting_moves": Runs["starting_moves"][0],
        "starting_items": Runs["starting_items"][0],
        "items_bought": Runs["items_bought"][0],
        "upgrade_choice_made_per_level": Runs["upgrade_choice_made_per_level"][0],
        "run_time": Runs["run_time"][0],
        "game_version": Runs["game_version"][0],
    }


def get_fight_stats(index):  # return one fight row as a dictionary
    Fights = get_fights()
    return {
        "level": Fights["level"][index],
        "enemy_id": Fights["enemy_id"][index],
        "duration_time": Fights["duration_time"][index],
        "player_died": Fights["player_died"][index],
        "health_remaining": Fights["health_remaining"][index],
        "items_used": Fights["items_used"][index],
        "moves_used": Fights["moves_used"][index],
        "enemy_moves_used": Fights["enemy_moves_used"][index],
    }


def choose_random_variant(move_name: str):
    """
    if move_name not in MOVE_META:  # need to tweak
        # fallback for moves not yet defined in the simplified dictionary
        return {
            "move_name": move_name,
            "variant_name": "default",
            "height": "mid",
            "damage_tier": "medium",
            "mode": "attack",
            "category": "unknown",
            "tags": [],
        }
 """
    meta = MOVE_META[move_name]
    variant_name = random.choice(list(meta["variants"].keys()))
    variant = meta["variants"][variant_name]

    return {
        "move_name": move_name,
        "variant_name": variant_name,
        "height": variant["height"],
        "damage_tier": variant["damage"],
        "mode": meta["mode"],
        "category": meta["category"],
        "tags": meta["tags"],
    }


def generate_plausible_move_pool(): # curated move pools to make the simulation feel believable
    player_pool = [
        "Punch", "Uppercut", "Push-Kick", "Reverse-Kick", "Spin Kick",
        "Trip-Kick", "Floor Throwdown", "Over-back Throw", "Arm Twister",
        "Choke", "Block", "Guard", "Counter", "Dodge", "Foot Shuffle"
    ]

    enemy_pool = [
        "Punch", "Push-Kick", "Reverse-Kick", "Spin Kick", "Trip-Kick",
        "Floor Throwdown", "Head-drop", "Over-back Throw",
        "Block", "Guard", "Dodge"
    ]

    return player_pool, enemy_pool


def resolve_exchange(player_move, enemy_move):  # simplified combat resolution for one paired move

    p_damage = DAMAGE_TIERS[player_move["damage_tier"]]
    e_damage = DAMAGE_TIERS[enemy_move["damage_tier"]]

    player_damage_taken = 0
    enemy_damage_taken = 0

    p_mode = player_move["mode"]
    e_mode = enemy_move["mode"]

    if p_mode == "attack" and e_mode == "attack": # attack vs attack
        enemy_damage_taken = p_damage
        player_damage_taken = e_damage
        log = (
            f"Player uses {player_move['move_name']} ({player_move['height']}) "
            f"while enemy uses {enemy_move['move_name']} ({enemy_move['height']}). "
            f"Both attacks connect."
        )

    elif p_mode == "attack" and e_mode == "defense":  # player attacks, enemy defends
        blocked = (
            enemy_move["height"] == player_move["height"]
            and "ignores_guard" not in player_move["tags"]
        )

        if blocked:
            if enemy_move["move_name"] == "Counter":
                player_damage_taken = 3
                log = (
                    f"Player uses {player_move['move_name']} ({player_move['height']}). "
                    f"Enemy counters correctly."
                )
            elif enemy_move["move_name"] == "Block":
                log = (
                    f"Player uses {player_move['move_name']} ({player_move['height']}). "
                    f"Enemy blocks the attack."
                )
            else:
                enemy_damage_taken = max(1, p_damage // 2)
                log = (
                    f"Player uses {player_move['move_name']} ({player_move['height']}). "
                    f"Enemy guards and reduces the damage."
                )
        else:
            enemy_damage_taken = p_damage
            log = (
                f"Player uses {player_move['move_name']} ({player_move['height']}). "
                f"Enemy fails to defend in time."
            )

    elif p_mode == "defense" and e_mode == "attack": # enemy attacks, player defends
        blocked = (
            player_move["height"] == enemy_move["height"]
            and "ignores_guard" not in enemy_move["tags"]
        )

        if blocked:
            if player_move["move_name"] == "Counter":
                enemy_damage_taken = 3
                log = (
                    f"Enemy uses {enemy_move['move_name']} ({enemy_move['height']}). "
                    f"Player counters successfully."
                )
            elif player_move["move_name"] == "Block":
                log = (
                    f"Enemy uses {enemy_move['move_name']} ({enemy_move['height']}). "
                    f"Player blocks the attack."
                )
            else:
                player_damage_taken = max(1, e_damage // 2)
                log = (
                    f"Enemy uses {enemy_move['move_name']} ({enemy_move['height']}). "
                    f"Player guards and reduces the damage."
                )
        else:
            player_damage_taken = e_damage
            log = (
                f"Enemy uses {enemy_move['move_name']} ({enemy_move['height']}). "
                f"Player's defense is mistimed."
            )

    else:  # defense vs defense
        log = (
            f"Player uses {player_move['move_name']} while enemy uses {enemy_move['move_name']}. "
            f"No meaningful damage is dealt."
        )

    return {
        "player_damage_taken": player_damage_taken,
        "enemy_damage_taken": enemy_damage_taken,
        "log": log,
    }


def simulate_level(fight_stats, player_hp):          # accepts current HP
    level = fight_stats["level"]
    enemy_id = fight_stats["enemy_id"]
    duration_time = fight_stats["duration_time"]
    player_died = fight_stats["player_died"]
    items_used = fight_stats["items_used"]

    print(f"\n=================== Level {level} ===================")
    print(f"Enemy ID: {enemy_id}")
    print(f"Fight Duration: {duration_time} seconds")

    if items_used:
        print(f"Items Used: {items_used}")
    else:
        print("Items Used: None")

    enemy_hp = 70 + (level * 12)

    target_turns = max(3, min(12, duration_time // 30))
    hard_cap    = target_turns * 2                   # hard cap defined here

    player_pool, enemy_pool = generate_plausible_move_pool()

    print(f"Starting HP -> Player: {player_hp}, Enemy: {enemy_hp}\n")

    turn = 1

    while player_hp > 0 and enemy_hp > 0:

        # force the fight to end 
        if turn > hard_cap:
            print(f"[Turn {turn}] Fight time limit reached — resolving outcome.")
            if player_died:
                player_hp = 0
            else:
                enemy_hp = 0
                player_hp = max(1, player_hp)
            break

        player_move_name = random.choice(player_pool)
        enemy_move_name  = random.choice(enemy_pool)

        player_move = choose_random_variant(player_move_name)
        enemy_move  = choose_random_variant(enemy_move_name)

        result = resolve_exchange(player_move, enemy_move)

        remaining_turns = max(1, target_turns - turn + 1)

        if player_died:
            player_target_chunk = max(4, player_hp // remaining_turns)
            enemy_target_chunk  = max(1, enemy_hp // (remaining_turns + 2))
        else:
            enemy_target_chunk  = max(4, enemy_hp // remaining_turns)
            player_target_chunk = max(1, player_hp // (remaining_turns + 2))

        player_damage = result["player_damage_taken"]
        enemy_damage  = result["enemy_damage_taken"]

        if player_damage > 0:
            player_damage = max(player_damage, player_target_chunk // 2)
        if enemy_damage > 0:
            enemy_damage  = max(enemy_damage,  enemy_target_chunk  // 2)

        if player_damage > 0:
            player_damage = max(1, int(player_damage * random.uniform(0.8, 1.2)))
        if enemy_damage > 0:
            enemy_damage  = max(1, int(enemy_damage  * random.uniform(0.8, 1.2)))

        player_hp -= player_damage
        p = get_active_params()
        enemy_hp = int(70 + level * p["enemy_base_damage"] * p["enemy_damage_per_level"] * 2)
        player_hp  = max(player_hp, 0)
        enemy_hp   = max(enemy_hp,  0)

        print(f"Turn {turn}")
        print(result["log"])
        print(f"Damage -> Player: {player_damage}, Enemy: {enemy_damage}")
        print(f"Player HP: {player_hp} | Enemy HP: {enemy_hp}\n")

        turn += 1

    seeded_health_remaining = fight_stats["health_remaining"]

    if player_died:
        player_hp = 0
        print("Final outcome: Player dies in this level.")
    else:
        player_hp = max(1, seeded_health_remaining)
        print("Final outcome: Enemy defeated.")

    print(f"Ending HP -> Player: {player_hp}, Enemy: {enemy_hp}")

    return player_hp                                 # returns HP to caller


CHECKPOINT_LEVELS = {
    "Easy":   {3, 6, 9},
    "Normal": {5},
    "Hard":   set(),          # no checkpoints on Hard
}
CHECKPOINT_HEAL_FRACTION = 0.25

def simulate_run():
    run_meta = get_run_metadata()
    game_version = run_meta["game_version"]

    print("===================================================")
    print("SIMULATION MODE")
    print("===================================================")
    print(f"Player ID      : {run_meta['player_id']}")
    print(f"Session ID     : {run_meta['session_id']}")
    print(f"Run ID         : {run_meta['run_id']}")
    print(f"Game Version   : {game_version}")
    print(f"Run Successful : {run_meta['successful']}")
    print(f"Final Level    : {run_meta['level_finish']}")
    print(f"Run Time       : {run_meta['run_time']} seconds")
    print(f"Starting Moves : {run_meta['starting_moves']}")
    print(f"Starting Items : {run_meta['starting_items']}")
    print("===================================================")

    # starting HP from generator constants
    current_player_hp = get_active_params()["base_hp"]        
    checkpoints = CHECKPOINT_LEVELS.get(game_version, set())

    for i in range(len(get_fights()["level"])):
        fight_stats = get_fight_stats(i)
        level       = fight_stats["level"]

        if level in checkpoints: # checkpoint heal 
            heal = int(current_player_hp * CHECKPOINT_HEAL_FRACTION)
            current_player_hp += heal
            print(f"\n[Checkpoint] Level {level} — healed {heal} HP. "
                  f"Player HP now {current_player_hp}.")

        current_player_hp = simulate_level(fight_stats, current_player_hp)

        if fight_stats["player_died"] == 1:
            print("\nRun ended: Player defeated.")
            break

    if run_meta["successful"] == 1:
        print("\nRun ended: Player won the run.")


if __name__ == "__main__":
    ensure_loaded()
    simulate_run()