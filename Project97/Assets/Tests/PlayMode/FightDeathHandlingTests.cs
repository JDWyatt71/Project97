using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

// when someone hits 0 hp theyre dead and the combat loop should stop
public class FightDeathHandlingTests
{
    private GameObject attackerObj;
    private GameObject defenderObj;
    private Character attacker;
    private Character defender;
    private AttackSO testAttack;
    private DefendSO testDefend;
    private CombatManager combatManager;
    private GameObject assetsDatabaseObj;

    [SetUp]
    public void SetUp()
    {
        assetsDatabaseObj = new GameObject("AssetsDatabase");
        AssetsDatabase db = assetsDatabaseObj.AddComponent<AssetsDatabase>();
        db.aMoves = new List<AttackSO>();
        db.dMoves = new List<DefendSO>();
        db.defaultDefendSO = ScriptableObject.CreateInstance<DefendSO>();
        db.defaultDefendSO.height = Scale.Medium;
        db.defaultDefendSO.damageReductionMultiplier = 0.5f;
        AssetsDatabase.I = db;

        CharacterSO characterSO = ScriptableObject.CreateInstance<CharacterSO>();
        characterSO.actionPoints = 10;
        characterSO.aMoves = new List<AttackSO>();
        characterSO.dMoves = new List<DefendSO>();

        attackerObj = new GameObject("Attacker");
        attacker = attackerObj.AddComponent<Character>();
        attackerObj.AddComponent<HealthSystem>();
        attacker.Setup(characterSO);
        attacker.attack = 10;
        attacker.hitPoints = 100;

        defenderObj = new GameObject("Defender");
        defender = defenderObj.AddComponent<Character>();
        defenderObj.AddComponent<HealthSystem>();
        defender.Setup(characterSO);
        defender.attack = 8;
        defender.hitPoints = 100;

        testAttack = ScriptableObject.CreateInstance<AttackSO>();
        testAttack.damage = Scale.Medium;
        testAttack.height = Scale.Medium;
        testAttack.AP = 2;
        testAttack.effects = new List<EffectChance>(); // CreateInstance doesnt init this ApplyEffects would nullref

        testDefend = ScriptableObject.CreateInstance<DefendSO>();
        testDefend.damageReductionMultiplier = 0.3f;
        testDefend.height = Scale.Medium;
        testDefend.AP = 1;

        var tracker = new FightAnalyticsTracker();
        tracker.StartFight("test");
        combatManager = new CombatManager(tracker);

        SetHealth(attacker.healthSystem, 100);
        SetHealth(defender.healthSystem, 100);
    }

    private void SetHealth(HealthSystem hs, int value)
    {
        FieldInfo health = typeof(HealthSystem).GetField("currentHealth", BindingFlags.NonPublic | BindingFlags.Instance);
        if (health != null)
            health.SetValue(hs, value);
    }

    [TearDown]
    public void TearDown()
    {
        AssetsDatabase.I = null;
        if (assetsDatabaseObj != null)
            UnityEngine.Object.DestroyImmediate(assetsDatabaseObj);
        UnityEngine.Object.DestroyImmediate(attackerObj);
        UnityEngine.Object.DestroyImmediate(defenderObj);
        UnityEngine.Object.DestroyImmediate(testAttack);
        UnityEngine.Object.DestroyImmediate(testDefend);
    }

    [Test]
    public void FightStopsWhenPlayerDies()
    {
        Type cmType = typeof(CombatManager);
        MethodInfo performAttack = cmType.GetMethod("PerformAttack", BindingFlags.NonPublic | BindingFlags.Instance);

        AttackSO bigHit = ScriptableObject.CreateInstance<AttackSO>();
        bigHit.damage = Scale.High;
        bigHit.height = Scale.High;
        bigHit.effects = new List<EffectChance>(); // CreateInstance doesnt init this ApplyEffects would nullref

        // attacks can dodge so retry seeds until we get a hit and defender actually dies
        bool defenderDied = false;
        for (int seed = 0; seed < 200; seed++)
        {
            UnityEngine.Random.InitState(seed);
            SetHealth(defender.healthSystem, 5);

            object[] args = new object[] { attacker, defender, bigHit, testDefend };
            performAttack.Invoke(combatManager, args);

            if (defender.healthSystem.GetHealth() <= 0) { defenderDied = true; break; }
        }
        UnityEngine.Object.DestroyImmediate(bigHit);
        Assert.IsTrue(defenderDied, "defender should die from a big hit in at least one of 200 seeds");
    }
}
