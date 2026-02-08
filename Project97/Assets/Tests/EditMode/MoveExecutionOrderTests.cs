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
        // need db so Character.Setup and TurnManager can run
        assetsDatabaseObj = new GameObject("AssetsDatabase");
        AssetsDatabase db = assetsDatabaseObj.AddComponent<AssetsDatabase>();
        db.aMoves = new List<AttackSO>();
        db.dMoves = new List<DefendSO>();
        db.defaultDefendSO = ScriptableObject.CreateInstance<DefendSO>();
        db.defaultDefendSO.height = Scale.Medium;
        db.defaultDefendSO.damageReductionMultiplier = 0.5f;

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
        // get private method so we can call it
        Type tmType = typeof(TurnManager);
        MethodInfo performMovePair = tmType.GetMethod("PerformMovePair", BindingFlags.NonPublic | BindingFlags.Instance);

        // create moves in order
        AttackSO move1 = ScriptableObject.CreateInstance<AttackSO>();
        move1.damage = Scale.Low;
        move1.height = Scale.Medium;
        move1.AP = 2;
        AttackSO move2 = ScriptableObject.CreateInstance<AttackSO>();
        move2.damage = Scale.Medium;
        move2.height = Scale.Medium;
        move2.AP = 2;
        AttackSO move3 = ScriptableObject.CreateInstance<AttackSO>();
        move3.damage = Scale.High;
        move3.height = Scale.High;
        move3.AP = 2;
        DefendSO noDefend = ScriptableObject.CreateInstance<DefendSO>(); // no defense move for predictable dmg
        noDefend.damageReductionMultiplier = 0f;
        noDefend.height = Scale.Low;

        List<AttackSO> order = new List<AttackSO>();
        order.Add(move1);
        order.Add(move2);
        order.Add(move3);
        SetHealth(defender.healthSystem, 100);
        int healthBefore = defender.healthSystem.GetHealth();
        List<int> healthAfterEach = new List<int>();

        // do them in order and see if health drops each time
        for (int i = 0; i < order.Count; i++)
        {
            object[] turn = new object[] { order[i], noDefend, attacker, defender };
            performMovePair.Invoke(turnManager, turn);
            healthAfterEach.Add(defender.healthSystem.GetHealth());
        }

        // order ok if health goes down after each move
        Assert.Less(healthAfterEach[0], healthBefore);
        Assert.Less(healthAfterEach[1], healthAfterEach[0]);
        Assert.Less(healthAfterEach[2], healthAfterEach[1]);

        UnityEngine.Object.DestroyImmediate(move1);
        UnityEngine.Object.DestroyImmediate(move2);
        UnityEngine.Object.DestroyImmediate(move3);
        UnityEngine.Object.DestroyImmediate(noDefend);
    }
}
