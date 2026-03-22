using System.Collections.Generic;
using System.Linq;
using UnityEngine;


//  EnemyFactory
//  Builds all EnemySO instances in memory at runtime by looking
//  moves up from AssetsDatabase by name.

public class EnemyFactory : MonoBehaviour
{
    public static EnemyFactory I;

    void Awake()
    {
        I = this;
    }

    public List<EnemySO> BuildAllEnemies()
    {
        return new List<EnemySO>
        {
            BuildDojoChallenger(),
            BuildComebackFighter(),
            BuildDodger(),
            BuildFighterFavouringKicks(),
            BuildUnderhandedFighter(),
            BuildGrappler(),
            BuildTankySumoWrestler(),
            BuildOldFighter(),
            BuildRival(),
            BuildStrikeSaint(),
        };
    }

    // ── 1. Dojo Challenger ───────────────────────────────────
    EnemySO BuildDojoChallenger()
    {
        var e = Make("Dojo Challenger", hp: 40, atk: 7, acc: 15, eva: 15, ap: 5);
        e.defendRate = 0.75f;

        e.aMoves = Attacks(
            "High Punch", "Medium Punch", "Taunt", "Pose",
            "Medium Push-Kick", "Medium Spin-Kick", "Low Spin-Kick", "Grab & Pummel");

        e.dMoves = Defends("High Guard", "Medium Guard", "Low Guard", "Dodge");

        e.favouredMoves = Attacks("High Punch", "Medium Punch");
        e.rareMoves     = Attacks("Grab & Pummel");

        return e;
    }

    // ── 2. Comeback Fighter ──────────────────────────────────
    EnemySO BuildComebackFighter()
    {
        var e = Make("Comeback Fighter", hp: 50, atk: 22, acc: 12, eva: 16, ap: 7);
        e.defendRate = 0.6f;

        e.aMoves = Attacks(
            "Jump-Kick", "Uppercut", "Cross-Cut", "Side-Fist",
            "High Push-Kick", "Medium Push-Kick", "Low Push-Kick",
            "Reverse Spin-Kick", "Grab & Pummel");

        e.dMoves = Defends("Foot Shuffle");

        e.rareMoves = Attacks("Grab & Pummel", "Jump-Kick", "Floor Throwdown");

        return e;
    }

    // ── 3. Dodger ────────────────────────────────────────────
    EnemySO BuildDodger()
    {
        var e = Make("Dodger", hp: 60, atk: 18, acc: 20, eva: 40, ap: 6);
        e.defendRate = 1f;

        e.aMoves = Attacks(
            "High Punch", "Medium Punch", "Medium Spin-Kick", "Low Spin-Kick",
            "Side Knuckle Strike", "Medium Push-Kick", "High Elbow", "Medium Elbow");

        e.dMoves = Defends("Dodge", "Foot Shuffle");

        e.rareMoves = Attacks("High Elbow", "Medium Elbow");

        e.defendWeights = DefendWeights(
            ("Dodge",        0.75f),
            ("Foot Shuffle", 0.25f));

        return e;
    }

    // ── 4. Fighter Favouring Kicks ───────────────────────────
    EnemySO BuildFighterFavouringKicks()
    {
        var e = Make("Fighter Favouring Kicks", hp: 90, atk: 26, acc: 35, eva: 20, ap: 8);
        e.defendRate = 0.9f;

        e.aMoves = Attacks(
            "Knee Strike", "Medium Spin-Kick", "Low Spin-Kick", "High Spin Kick",
            "Medium Spinning Knee Kick", "High Spinning Knee Kick",
            "Sweep Kick", "Reverse Kick", "Reverse Spin Kick",
            "Medium Push-Kick", "High Push Kick");

        e.dMoves = Defends("Dodge", "High Guard", "Medium Guard", "Low Guard");

        e.favouredMoves = Attacks("High Spin Kick", "Medium Spin-Kick", "Low Spin-Kick");
        e.rareMoves     = Attacks("Knee Strike", "Reverse Spin Kick");

        e.defendWeights = DefendWeights(
            ("High Guard",   0.1f),
            ("Medium Guard", 0.3f),
            ("Low Guard",    0.4f),
            ("Dodge",        0.2f));

        return e;
    }

    // ── 5. Underhanded Fighter ───────────────────────────────
    EnemySO BuildUnderhandedFighter()
    {
        var e = Make("Underhanded Fighter", hp: 100, atk: 28, acc: 25, eva: 25, ap: 7);
        e.defendRate = 0.75f;

        e.aMoves = Attacks(
            "Sweep Kick", "Eye Strike", "Underpunch", "High Spin Kick", "Low Spin Kick",
            "Side Knuckle Strike", "Trip-Throw", "Trip Kick", "Arm-Twister", "Choke");

        e.dMoves = Defends("High Block", "Medium Block", "Low Block");

        e.rareMoves = Attacks("Eye Strike", "High Spin Kick", "Low Spin Kick");

        e.defendWeights = DefendWeights(
            ("Medium Block", 0.5f),
            ("High Block",   0.25f),
            ("Low Block",    0.25f));

        e.moveConditions = Conditions(
            ("Choke", MoveUseCondition.OnlyWhenPlayerProne));

        return e;
    }

    // ── 6. Grappler ──────────────────────────────────────────
    EnemySO BuildGrappler()
    {
        var e = Make("Grappler", hp: 110, atk: 36, acc: 20, eva: 24, ap: 6);
        e.defendRate = 0.8f;

        e.aMoves = AllGrappleMoves();

        e.dMoves = Defends("Foot Shuffle", "High Guard", "Medium Guard", "Low Guard");

        e.rareMoves = Attacks("Arm Breaker", "Head Drop");

        e.defendWeights = DefendWeights(
            ("High Guard",   0.3f),
            ("Medium Guard", 0.2f),
            ("Low Guard",    0.2f),
            ("Foot Shuffle", 0.3f));

        e.moveConditions = Conditions(
            ("Choke",          MoveUseCondition.OnlyWhenPlayerProne),
            ("Arm Cross Hold", MoveUseCondition.OnlyWhenPlayerProne));

        return e;
    }

    // ── 7. Tanky Sumo Wrestler ───────────────────────────────
    EnemySO BuildTankySumoWrestler()
    {
        var e = Make("Tanky Sumo Wrestler", hp: 150, atk: 26, acc: 30, eva: 10, ap: 8);
        e.defendRate = 0f;

        e.aMoves = Attacks("High Palm", "Medium Palm", "Lift", "Push");
        e.dMoves = new List<DefendSO>();

        e.favouredMoves = Attacks("High Palm", "Medium Palm");
        e.rareMoves     = Attacks("Lift");

        return e;
    }

    // ── 8. Old Fighter ───────────────────────────────────────
    EnemySO BuildOldFighter()
    {
        var e = Make("Old Fighter", hp: 100, atk: 32, acc: 80, eva: 20, ap: 8);
        e.defendRate = 1f;

        e.aMoves = Attacks(
            "High Punch", "Medium Punch", "High Elbow", "Low Elbow",
            "Medium Spin Kick", "Low Spin Kick", "Medium Push Kick", "Low Push Kick",
            "Arm Twister", "Floor Throwdown", "Underpunch", "Side Fist",
            "Crosscut", "Trip-Kick");

        e.dMoves = Defends(
            "Medium Counter", "Low Counter", "High Block", "Medium Block", "Low Block");

        e.rareMoves = Attacks("Trip-Kick", "Arm Twister", "Floor Throwdown");

        e.defendWeights = DefendWeights(
            ("High Block",     0.4f),
            ("Medium Block",   0.2f),
            ("Low Block",      0.1f),
            ("Medium Counter", 0.1f),
            ("Low Counter",    0.2f));

        return e;
    }

    // ── 9. Rival ─────────────────────────────────────────────
    public EnemySO BuildRival(CharacterSO playerCSO = null)
    {
        var e = Make("Rival", hp: 100, atk: 20, acc: 20, eva: 20, ap: 7);
        e.defendRate = 0.9f;

        if (playerCSO != null)
        {
            e.aMoves = new List<AttackSO>(playerCSO.aMoves);
            e.dMoves = new List<DefendSO>(playerCSO.dMoves);
        }
        else
        {
            Debug.LogWarning("[EnemyFactory] BuildRival called without playerCSO — Rival will have no moves.");
            e.aMoves = new List<AttackSO>();
            e.dMoves = new List<DefendSO>();
        }

        // Fully random — no favoured, rare, or weighted moves

        return e;
    }

    // ── 10. Strike-Saint ─────────────────────────────────────
    EnemySO BuildStrikeSaint()
    {
        var e = Make("Strike-Saint", hp: 140, atk: 40, acc: 40, eva: 40, ap: 9);
        e.defendRate = 0.9f;

        e.aMoves = Attacks(
            "Crosscut", "Knee Strike", "High Palm Strike", "Medium Palm Strike",
            "Overback Throw", "Arm Breaker", "Shin Breaker",
            "High Push Kick", "Low Push Kick",
            "Front Knuckle Strike", "Side Fist", "Arm Cross Hold", "Dash Throw");

        e.dMoves = Defends(
            "High Counter", "Medium Counter", "Low Counter",
            "High Block", "Medium Block", "Low Block");

        e.rareMoves = Attacks("Knee Strike", "Shin Breaker", "Arm Breaker");

        return e;
    }

    // ── Helpers ──────────────────────────────────────────────

    EnemySO Make(string name, int hp, int atk, float acc, float eva, int ap)
    {
        var e = ScriptableObject.CreateInstance<EnemySO>();
        e.enemyName    = name;
        e.hitPoints    = hp;
        e.attack       = atk;
        e.accuracy     = acc;
        e.evasion      = eva;
        e.actionPoints = ap;

        e.favouredMoves  = new List<AttackSO>();
        e.rareMoves      = new List<AttackSO>();
        e.defendWeights  = new List<DefendWeight>();
        e.moveConditions = new List<MoveCondition>();

        return e;
    }

    /// Looks up AttackSO assets from AssetsDatabase by name.
    /// Logs a warning if a move isn't found.
    List<AttackSO> Attacks(params string[] names)
    {
        var result = new List<AttackSO>();
        foreach (var name in names)
        {
            var found = AssetsDatabase.I.aMoves.FirstOrDefault(m => m.name == name);
            if (found != null)
                result.Add(found);
            else
                Debug.LogWarning($"[EnemyFactory] AttackSO not found: '{name}'");
        }
        return result;
    }

    /// Looks up DefendSO assets from AssetsDatabase by name.
    List<DefendSO> Defends(params string[] names)
    {
        var result = new List<DefendSO>();
        foreach (var name in names)
        {
            var found = AssetsDatabase.I.dMoves.FirstOrDefault(m => m.name == name);
            if (found != null)
                result.Add(found);
            else
                Debug.LogWarning($"[EnemyFactory] DefendSO not found: '{name}'");
        }
        return result;
    }

    /// Returns all AttackSOs in AssetsDatabase with MoveType.Grapple.
    List<AttackSO> AllGrappleMoves()
    {
        var result = AssetsDatabase.I.aMoves
            .Where(m => m.moveType == MoveType.Grapple)
            .ToList();

        if (result.Count == 0)
            Debug.LogWarning("[EnemyFactory] No Grapple moves found in AssetsDatabase for Grappler.");

        return result;
    }

    List<DefendWeight> DefendWeights(params (string name, float weight)[] entries)
    {
        var result = new List<DefendWeight>();
        foreach (var (name, weight) in entries)
        {
            var move = AssetsDatabase.I.dMoves.FirstOrDefault(m => m.name == name);
            if (move == null)
            {
                Debug.LogWarning($"[EnemyFactory] DefendSO not found for weight entry: '{name}'");
                continue;
            }
            result.Add(new DefendWeight { move = move, weight = weight });
        }
        return result;
    }

    List<MoveCondition> Conditions(params (string name, MoveUseCondition condition)[] entries)
    {
        var result = new List<MoveCondition>();
        foreach (var (name, condition) in entries)
        {
            var move = AssetsDatabase.I.aMoves.FirstOrDefault(m => m.name == name);
            if (move == null)
            {
                Debug.LogWarning($"[EnemyFactory] AttackSO not found for condition: '{name}'");
                continue;
            }
            result.Add(new MoveCondition { move = move, condition = condition });
        }
        return result;
    }
}
