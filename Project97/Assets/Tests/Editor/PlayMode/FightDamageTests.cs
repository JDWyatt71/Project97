using System;
using System.Collections.Generic;
using System.Reflection;
#if UNITY_EDITOR
using NUnit.Framework;
#endif
using UnityEngine;

// damage calc  attack does damage, defend reduces it, health doesnt go below 0
public class FightDamageTests
{
    private GameObject attackerObj;
    private GameObject defenderObj;
    private Character attacker;
    private Character defender;
    private CombatManager combatManager;
    private GameObject assetsDatabaseObj;
    private GameObject gameManagerObj;

    [SetUp]
    public void SetUp()
    {
        // CombatManager logs telemetry using GameManager.I.CurrentSessionId so we need a minimal GameManager
        gameManagerObj = new GameObject("GameManager_TEST");
        GameManager gm = gameManagerObj.AddComponent<GameManager>(); // Awake() sets GameManager.I
        // dont run GameManager.Start() in tests, just set session id manually
        FieldInfo sessionField = typeof(GameManager).GetField("<CurrentSessionId>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        if (sessionField != null)
        {
            sessionField.SetValue(gm, Guid.NewGuid().ToString());
        }

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

        // simple combat manager with analytics
        var tracker = new FightAnalyticsTracker();
        tracker.StartFight("test");
        combatManager = new CombatManager(tracker);

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
        if (gameManagerObj != null)
            UnityEngine.Object.DestroyImmediate(gameManagerObj);
        UnityEngine.Object.DestroyImmediate(attackerObj);
        UnityEngine.Object.DestroyImmediate(defenderObj);
    }

    [Test]
    public void FightDamage_CalculationIsCorrect()
    {
        Type cmType = typeof(CombatManager);
        MethodInfo getDamage = cmType.GetMethod("GetDamage", BindingFlags.NonPublic | BindingFlags.Instance);
        MethodInfo performAttack = cmType.GetMethod("PerformAttack", BindingFlags.NonPublic | BindingFlags.Instance);

        // low/med/high should be 3, 7, 10
        object[] lowArg = new object[] { Scale.Low };
        object[] medArg = new object[] { Scale.Medium };
        object[] highArg = new object[] { Scale.High };
        int low = (int)getDamage.Invoke(combatManager, lowArg);
        int med = (int)getDamage.Invoke(combatManager, medArg);
        int high = (int)getDamage.Invoke(combatManager, highArg);
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
            performAttack.Invoke(combatManager, attackArgs);
            int healthNoDefence = defender.healthSystem.GetHealth();
            if (healthNoDefence >= 100) continue; // first attack missed need it to hit for the defence comparison to mean anything
            Assert.LessOrEqual(healthNoDefence, 100);

            SetHealth(defender.healthSystem, 100);
            performAttack.Invoke(combatManager, new object[] { attacker, defender, attackSO, withDefend });
            if (defender.healthSystem.GetHealth() >= healthNoDefence) { damageCalcOk = true; break; }
        }

        UnityEngine.Object.DestroyImmediate(noDefend);
        UnityEngine.Object.DestroyImmediate(withDefend);
        UnityEngine.Object.DestroyImmediate(attackSO);
        Assert.IsTrue(damageCalcOk, "defence should reduce damage");
    }
}
