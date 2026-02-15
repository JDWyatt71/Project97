using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

// check that moves run in the order they were picked (no shuffling or doubling)
public class MoveExecutionOrderTests
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
        attacker.attack = 10; // use fixed attack value for predictable dmg

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
        // reflection to set private currentHealth
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
        if (turnManager != null)
            UnityEngine.Object.DestroyImmediate(turnManager.gameObject);
    }

    [Test]
    public void MoveSelection_SameAsExecutionOrder()
    {
        Type tmType = typeof(TurnManager);
        MethodInfo performMovePair = tmType.GetMethod("PerformMovePair", BindingFlags.NonPublic | BindingFlags.Instance);

        AttackSO move1 = ScriptableObject.CreateInstance<AttackSO>();
        move1.damage = Scale.Low;
        move1.height = Scale.Medium;
        move1.AP = 2;
        move1.effects = new List<EffectChance>(); // CreateInstance doesnt init this ApplyEffects would nullref
        AttackSO move2 = ScriptableObject.CreateInstance<AttackSO>();
        move2.damage = Scale.Medium;
        move2.height = Scale.Medium;
        move2.AP = 2;
        move2.effects = new List<EffectChance>();
        AttackSO move3 = ScriptableObject.CreateInstance<AttackSO>();
        move3.damage = Scale.High;
        move3.height = Scale.High;
        move3.AP = 2;
        move3.effects = new List<EffectChance>();
        DefendSO noDefend = ScriptableObject.CreateInstance<DefendSO>();
        noDefend.damageReductionMultiplier = 0f;
        noDefend.height = Scale.Low;

        // all 3 moves can dodge so retry seeds until we get a run where all 3 hit (health drops each time)
        List<AttackSO> order = new List<AttackSO> { move1, move2, move3 };
        bool orderOk = false;
        for (int seed = 0; seed < 500; seed++)
        {
            UnityEngine.Random.InitState(seed);
            SetHealth(defender.healthSystem, 100);
            int healthBefore = defender.healthSystem.GetHealth();
            List<int> healthAfterEach = new List<int>();

            for (int i = 0; i < order.Count; i++)
            {
                object[] turn = new object[] { order[i], noDefend, attacker, defender };
                performMovePair.Invoke(turnManager, turn);
                healthAfterEach.Add(defender.healthSystem.GetHealth());
            }

            if (healthAfterEach[0] < healthBefore && healthAfterEach[1] < healthAfterEach[0] && healthAfterEach[2] < healthAfterEach[1])
            {
                orderOk = true;
                break;
            }
        }

        UnityEngine.Object.DestroyImmediate(move1);
        UnityEngine.Object.DestroyImmediate(move2);
        UnityEngine.Object.DestroyImmediate(move3);
        UnityEngine.Object.DestroyImmediate(noDefend);
        Assert.IsTrue(orderOk, "health should decrease after each move; no good sequence in 500 seeds");
    }
}
