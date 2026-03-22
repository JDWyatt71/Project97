using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// ──────────────────────────────────────────────────────────────
//  EnemyAI  –  Picks attack and defend moves each turn
//  based on the behaviour data stored in an EnemySO.
//
//  Usage (call from TurnManager or equivalent):
//      AttackSO attack = EnemyAI.PickAttack(enemySO, playerState);
//      DefendSO defend = EnemyAI.PickDefend(enemySO);
// ──────────────────────────────────────────────────────────────
public static class EnemyAI
{
    // ── Relative weights ─────────────────────────────────────
    private const float FAVOURED_WEIGHT = 4f;   // 4× more likely than normal
    private const float NORMAL_WEIGHT   = 1f;
    private const float RARE_WEIGHT     = 0.25f; // 4× less likely than normal

    // ── Public API ───────────────────────────────────────────

    /// <summary>
    /// Decides whether the enemy defends this turn.
    /// Returns true if the enemy should attempt a defensive move.
    /// </summary>
    public static bool ShouldDefend(EnemySO enemy)
    {
        if (enemy.dMoves == null || enemy.dMoves.Count == 0) return false;
        return Random.value < enemy.defendRate;
    }

    /// <summary>
    /// Picks an attack move, respecting favoured/rare weights
    /// and any move conditions (e.g. only use Choke when player is prone).
    /// </summary>
    public static AttackSO PickAttack(EnemySO enemy, PlayerState playerState)
    {
        var candidates = BuildAttackPool(enemy, playerState);
        if (candidates.Count == 0)
        {
            Debug.LogWarning($"[EnemyAI] {enemy.enemyName} has no valid attack candidates!");
            return null;
        }
        return WeightedRandom(candidates);
    }

    /// <summary>
    /// Picks a defensive move using the configured weights,
    /// or equally at random if no weights are defined.
    /// </summary>
    public static DefendSO PickDefend(EnemySO enemy)
    {
        if (enemy.dMoves == null || enemy.dMoves.Count == 0) return null;

        // If explicit weights are configured, use them
        if (enemy.defendWeights != null && enemy.defendWeights.Count > 0)
            return WeightedRandomDefend(enemy);

        // Otherwise equal chance for all defensive moves
        return enemy.dMoves[Random.Range(0, enemy.dMoves.Count)];
    }

    // ── Internal helpers ─────────────────────────────────────

    private static List<WeightedMove<AttackSO>> BuildAttackPool(EnemySO enemy, PlayerState playerState)
    {
        var pool = new List<WeightedMove<AttackSO>>();

        foreach (AttackSO move in enemy.aMoves)
        {
            // Check move conditions first
            if (!IsMoveAllowed(move, enemy, playerState)) continue;

            float weight = GetAttackWeight(move, enemy);
            pool.Add(new WeightedMove<AttackSO>(move, weight));
        }

        return pool;
    }

    private static float GetAttackWeight(AttackSO move, EnemySO enemy)
    {
        if (enemy.favouredMoves != null && enemy.favouredMoves.Contains(move))
            return FAVOURED_WEIGHT;

        if (enemy.rareMoves != null && enemy.rareMoves.Contains(move))
            return RARE_WEIGHT;

        return NORMAL_WEIGHT;
    }

    private static bool IsMoveAllowed(AttackSO move, EnemySO enemy, PlayerState playerState)
    {
        if (enemy.moveConditions == null) return true;

        foreach (var condition in enemy.moveConditions)
        {
            if (condition.move != move) continue;

            switch (condition.condition)
            {
                case MoveUseCondition.OnlyWhenPlayerProne:
                    return playerState == PlayerState.Prone;
                case MoveUseCondition.OnlyWhenPlayerStanding:
                    return playerState == PlayerState.Standing;
                case MoveUseCondition.Always:
                default:
                    return true;
            }
        }

        return true; // No condition found for this move → always allowed
    }

    private static T WeightedRandom<T>(List<WeightedMove<T>> pool)
    {
        float total = pool.Sum(e => e.weight);
        float roll  = Random.value * total;
        float cumulative = 0f;

        foreach (var entry in pool)
        {
            cumulative += entry.weight;
            if (roll <= cumulative) return entry.move;
        }

        return pool[pool.Count - 1].move; // Fallback (floating-point safety)
    }

    private static DefendSO WeightedRandomDefend(EnemySO enemy)
    {
        float total = enemy.defendWeights.Sum(w => w.weight);
        float roll  = Random.value * total;
        float cumulative = 0f;

        foreach (var dw in enemy.defendWeights)
        {
            cumulative += dw.weight;
            if (roll <= cumulative) return dw.move;
        }

        return enemy.defendWeights[enemy.defendWeights.Count - 1].move;
    }

    // ── Tiny helper struct ───────────────────────────────────
    private struct WeightedMove<T>
    {
        public T move;
        public float weight;
        public WeightedMove(T m, float w) { move = m; weight = w; }
    }
}

// ──────────────────────────────────────────────────────────────
//  PlayerState  –  minimal enum the AI needs to check conditions.
//  If your TurnManager already has this, remove this definition
//  and update the using reference accordingly.
// ──────────────────────────────────────────────────────────────
public enum PlayerState
{
    Standing,
    Prone,
}
