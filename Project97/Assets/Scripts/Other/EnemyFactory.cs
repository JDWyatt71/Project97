using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using UnityEditor;
using System.IO;


//  EnemyFactory
//  Builds all CharacterSO instances in memory at runtime by looking
//  moves up from AssetsDatabase by name.

public class EnemyFactory : MonoBehaviour
{
    public static EnemyFactory I;

    void Start()
    {
        I = this;
        BuildAllEnemies();
    }

    public List<CharacterSO> BuildAllEnemies()
    {
        return new List<CharacterSO>
        {
          
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

    

    // ── 3. Dodger ────────────────────────────────────────────
    CharacterSO BuildDodger()
    {
        var e = Make("Dodger", hp: 60, atk: 18, acc: 20, eva: 40, ap: 6);
        e.defendRate = 1f;

        e.aMoves = Attacks(
            "High Punch", "Medium Punch", "Medium Spin Kick", "Low Spin Kick",
            "High Side Knuckle Strike", "medium push kick", "High Elbow", "Medium Elbow");

        e.dMoves = Defends("Dodge", "Foot Shuffle");

       

        return e;
    }

    // ── 4. Fighter Favouring Kicks ───────────────────────────
    CharacterSO BuildFighterFavouringKicks()
    {
        var e = Make("Fighter Favouring Kicks", hp: 90, atk: 26, acc: 35, eva: 20, ap: 8);
        e.defendRate = 0.9f;

        e.aMoves = Attacks(
            "medium knee strike", "Medium Spin Kick", "Low Spin Kick", "High Spin Kick",
            "Medium Spinning Knee Kick", "Medium Spinning Knee Kick",
            "low sweep kick", "medium Reverse-Kick", "medium Reverse Spin Kick",
            "Medium Push Kick", "High Push Kick");

        e.dMoves = Defends("Dodge", "High Guard", "Medium Guard", "Low Guard");

       

        return e;
    }

    // ── 5. Underhanded Fighter ───────────────────────────────
    CharacterSO BuildUnderhandedFighter()
    {
        var e = Make("Underhanded Fighter", hp: 100, atk: 28, acc: 25, eva: 25, ap: 7);
        e.defendRate = 0.75f;

        e.aMoves = Attacks(
            "low sweep kick", "High Eye Strike", "Medium Underpunch", "High Spin Kick", "Low Spin Kick",
            "High Side Knuckle Strike", "Trip Throw", "Low Trip-Kick", "arm twister", "Choke");

        e.dMoves = Defends("High Block", "Medium Block", "Low Block");

        

        return e;
    }

    // ── 6. Grappler ──────────────────────────────────────────
    CharacterSO BuildGrappler()
    {
        var e = Make("Grappler", hp: 110, atk: 36, acc: 20, eva: 24, ap: 6);
        e.defendRate = 0.8f;

        e.aMoves = AllGrappleMoves();

        e.dMoves = Defends("Foot Shuffle", "High Guard", "Medium Guard", "Low Guard");

        

        return e;
    }

    // ── 7. Tanky Sumo Wrestler ───────────────────────────────
    CharacterSO BuildTankySumoWrestler()
    {
        var e = Make("Tanky Sumo Wrestler", hp: 150, atk: 26, acc: 30, eva: 10, ap: 8);
        e.defendRate = 0f;

        e.aMoves = Attacks("High Palm Strike", "Medium Palm Strike", "Lift", "Medium Push");
        e.dMoves = new List<DefendSO>();


        return e;
    }

    // ── 8. Old Fighter ───────────────────────────────────────
    CharacterSO BuildOldFighter()
    {
        var e = Make("Old Fighter", hp: 100, atk: 32, acc: 80, eva: 20, ap: 8);
        e.defendRate = 1f;

        e.aMoves = Attacks(
            "High Punch", "Medium Punch", "High Elbow", "Low Elbow",
            "Medium Spin Kick", "Low Spin Kick", "Medium Push Kick", "Low Push Kick",
            "Arm Twister", "Floor Throwdown", "Medium Underpunch", "medium side fist",
            "High Crosscut", "Low Trip-Kick");

        e.dMoves = Defends(
            "Medium Counter", "Low Counter", "High Block", "Medium Block", "Low Block");

       

        return e;
    }

    // ── 9. Rival ─────────────────────────────────────────────
    public CharacterSO BuildRival(CharacterSO playerCSO = null)
    {
        var e = Make("Rival", hp: 100, atk: 20, acc: 20, eva: 20, ap: 7);
        e.defendRate = 0.9f;

        

        // Fully random — no favoured, rare, or weighted moves

        return e;
    }

    // ── 10. Strike-Saint ─────────────────────────────────────
    CharacterSO BuildStrikeSaint()
    {
        var e = Make("Strike-Saint", hp: 140, atk: 40, acc: 40, eva: 40, ap: 9);
        e.defendRate = 0.9f;

        e.aMoves = Attacks(
            "High Crosscut", "medium knee strike", "High Palm Strike", "Medium Palm Strike",
            "Over-back Throw", "Arm Breaker", "Low Shin Breaker",
            "High Push Kick", "Low Push Kick",
            "High Front Knuckle Strike", "medium side fist", "Arm Cross Hold", "Dash Throw");

        e.dMoves = Defends(
            "High Counter", "Medium Counter", "Low Counter",
            "High Block", "Medium Block", "Low Block");

        

        return e;
    }

    // ── Helpers ──────────────────────────────────────────────

    CharacterSO Make(string name, int hp, int atk, float acc, float eva, int ap)
    {
        var e = ScriptableObject.CreateInstance<CharacterSO>();
        AssetDatabase.CreateAsset(e, $"Assets/SOs/CharacterSO/{name}.asset");

        e.name    = name;
        e.hitPoints    = hp;
        e.attack       = atk;
        e.accuracy     = acc;
        e.evasion      = eva;
        e.actionPoints = ap;

        return e;
    }
    

    /// Looks up AttackSO assets from AssetsDatabase by name.
    /// Logs a warning if a move isn't found.
    List<AttackSO> Attacks(params string[] names)
    {
        var result = new List<AttackSO>();
        foreach (var name in names)
        {
            var found = AssetsDatabase.I.aMoves.FirstOrDefault(m => m.name.ToLower() == name.ToLower());
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
            var found = AssetsDatabase.I.dMoves.FirstOrDefault(m => m.name.ToLower() == name.ToLower());
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

    
}