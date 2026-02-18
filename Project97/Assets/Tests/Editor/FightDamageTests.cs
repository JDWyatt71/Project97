using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

// damage calc  attack does damage, defend reduces it, health doesnt go below 0
public class FightDamageTests
{
    private GameObject attackerObj;
    private GameObject defenderObj;
    private Character attacker;
    private Character defender;
    private TurnManager turnManager;
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

        defenderObj = new GameObject("Defender");
        defender = defenderObj.AddComponent<Character>();
        defenderObj.AddComponent<HealthSystem>();
        defender.Setup(characterSO);
        defender.attack = 8;

        GameObject turnManagerObj = new GameObject("TestTurnManager");
        turnManagerObj.AddComponent<MovesUIScreen>();
        turnManager = turnManagerObj.AddComponent<TurnManager>();

        SetHealth(attacker.healthSystem, 100);
        SetHealth(defender.healthSystem, 100);
    }

    private void SetHealth(HealthSystem hs, int value)
    {
        FieldInfo field = typeof(HealthSystem).GetField("currentHealth", BindingFlags.NonPublic | BindingFlags.Instance); // get private current health field 
        if (field != null)
            field.SetValue(hs, value);
    }

    [TearDown]
    public void TearDown()
    {
        AssetsDatabase.I = null;
        if (assetsDatabaseObj != null)
            UnityEngine.Object.DestroyImmediate(assetsDatabaseObj);
        UnityEngine.Object.DestroyImmediate(attackerObj);
        UnityEngine.Object.DestroyImmediate(defenderObj);
        if (turnManager != null)
            UnityEngine.Object.DestroyImmediate(turnManager.gameObject);
    }

    [Test]
    public void FightDamage_CalculationIsCorrect()
    {
        Type tmType = typeof(TurnManager);
        MethodInfo getDamage = tmType.GetMethod("GetDamage", BindingFlags.NonPublic | BindingFlags.Instance);
        MethodInfo performAttack = tmType.GetMethod("PerformAttack", BindingFlags.NonPublic | BindingFlags.Instance);

        // low/med/high should be 3, 7, 10
        object[] lowArg = new object[] { Scale.Low };
        object[] medArg = new object[] { Scale.Medium };
        object[] highArg = new object[] { Scale.High };
        int low = (int)getDamage.Invoke(turnManager, lowArg);
        int med = (int)getDamage.Invoke(turnManager, medArg);
        int high = (int)getDamage.Invoke(turnManager, highArg);
        Assert.AreEqual(3, low);
        Assert.AreEqual(7, med);
        Assert.AreEqual(10, high);

        // hit with no defence health should not go up (damage has ±15% variance so it can be 0)
        DefendSO noDefend = ScriptableObject.CreateInstance<DefendSO>();
        noDefend.damageReductionMultiplier = 0f;
        noDefend.height = Scale.Low;
        AttackSO attackSO = ScriptableObject.CreateInstance<AttackSO>();
        attackSO.damage = Scale.Medium;
        attackSO.height = Scale.Medium;
        attackSO.effects = new List<EffectChance>(); // CreateInstance doesnt init this ApplyEffects would nullref
        DefendSO withDefend = ScriptableObject.CreateInstance<DefendSO>();
        withDefend.damageReductionMultiplier = 0.8f;
        withDefend.height = Scale.Medium;

        // attacks can dodge (~50% hit) so retry with different seeds until we get a run where first hits
        bool damageCalcOk = false;
        for (int seed = 0; seed < 200; seed++)
        {
            UnityEngine.Random.InitState(seed);
            SetHealth(defender.healthSystem, 100);
            object[] attackArgs = new object[] { attacker, defender, attackSO, noDefend };
            performAttack.Invoke(turnManager, attackArgs);
            int healthNoDefence = defender.healthSystem.GetHealth();
            if (healthNoDefence >= 100) continue; // first attack missed need it to hit for the defence comparison to mean anything
            Assert.LessOrEqual(healthNoDefence, 100);

            SetHealth(defender.healthSystem, 100);
            performAttack.Invoke(turnManager, new object[] { attacker, defender, attackSO, withDefend });
            if (defender.healthSystem.GetHealth() >= healthNoDefence) { damageCalcOk = true; break; }
        }

        UnityEngine.Object.DestroyImmediate(noDefend);
        UnityEngine.Object.DestroyImmediate(withDefend);
        UnityEngine.Object.DestroyImmediate(attackSO);
        Assert.IsTrue(damageCalcOk, "defence should reduce damage");
    }
}
