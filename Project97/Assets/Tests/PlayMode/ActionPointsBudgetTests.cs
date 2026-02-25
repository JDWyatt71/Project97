using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

// test that AP cant go negative cant select move if too expensive AP goes down when u select and back up when u deselect
public class ActionPointsBudgetTests
{
    private GameObject testGameObject;
    private Character testCharacter;
    private TurnManager turnManager;
    private AttackSO lowCostAttack;
    private AttackSO highCostAttack;
    private DefendSO defendMove;
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

        testGameObject = new GameObject("TestCharacter");
        testCharacter = testGameObject.AddComponent<Character>();
        testGameObject.AddComponent<HealthSystem>();

        lowCostAttack = ScriptableObject.CreateInstance<AttackSO>();
        lowCostAttack.AP = 2;
        lowCostAttack.damage = Scale.Medium;
        lowCostAttack.height = Scale.Medium;

        highCostAttack = ScriptableObject.CreateInstance<AttackSO>();
        highCostAttack.AP = 5;
        highCostAttack.damage = Scale.High;
        highCostAttack.height = Scale.High;

        defendMove = ScriptableObject.CreateInstance<DefendSO>();
        defendMove.AP = 1;
        defendMove.height = Scale.Medium;
        defendMove.damageReductionMultiplier = 0.5f;

        CharacterSO characterSO = ScriptableObject.CreateInstance<CharacterSO>();
        characterSO.actionPoints = 5;
        characterSO.aMoves = new List<AttackSO>();
        characterSO.dMoves = new List<DefendSO>();
        testCharacter.Setup(characterSO);

        GameObject turnManagerObj = new GameObject("TestTurnManager");
        turnManagerObj.AddComponent<MovesUIScreen>();
        turnManagerObj.AddComponent<APBarUI>(); // TurnManager.SchedulePlayerMoves expects this
        turnManager = turnManagerObj.AddComponent<TurnManager>();
    }

    [TearDown]
    public void TearDown()
    {
        AssetsDatabase.I = null;
        if (assetsDatabaseObj != null)
            UnityEngine.Object.DestroyImmediate(assetsDatabaseObj);
        UnityEngine.Object.DestroyImmediate(testGameObject);
        UnityEngine.Object.DestroyImmediate(lowCostAttack);
        UnityEngine.Object.DestroyImmediate(highCostAttack);
        UnityEngine.Object.DestroyImmediate(defendMove);
        if (turnManager != null)
            UnityEngine.Object.DestroyImmediate(turnManager.gameObject);
    }

    [Test]
    public void AP_DontGoNegative()
    {
        // need reflection to read/write private fields
        Type turnManagerType = typeof(TurnManager);
        BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
        FieldInfo apField = turnManagerType.GetField("pAPRemaining", flags);
        FieldInfo movesField = turnManagerType.GetField("selectedMoves", flags);
        FieldInfo playerCharField = turnManagerType.GetField("playerCharacter", flags);
        FieldInfo selectedObjsField = turnManagerType.GetField("selectedObjs", flags);

        playerCharField.SetValue(turnManager, testCharacter);
        selectedObjsField.SetValue(turnManager, new List<GameObject>());
        apField.SetValue(turnManager, testCharacter.actionPoints);
        movesField.SetValue(turnManager, new List<MoveSO>());
        // analytics is used in TrySelectMove but only set in StartFight(); init for EditMode tests
        var analyticsField = turnManagerType.GetField("analytics", flags);
        var tracker = new FightAnalyticsTracker();
        tracker.StartFight("test");
        analyticsField.SetValue(turnManager, tracker);

        // pick a move that fits AP should go down
        GameObject ui1 = new GameObject("UI1");
        SelectMoveUI.I.TrySelectMove(lowCostAttack, ui1);
        int ap = (int)apField.GetValue(turnManager);
        Assert.GreaterOrEqual(ap, 0);
        Assert.AreEqual(3, ap); // 5 - 2

        // unselecting should give AP back
        SelectMoveUI.I.TrySelectMove(lowCostAttack, ui1);
        ap = (int)apField.GetValue(turnManager);
        Assert.AreEqual(5, ap);

        // now try to pick something that cant be afforded (only 2 AP left need 5)
        SelectMoveUI.I.TrySelectMove(lowCostAttack, ui1);
        SelectMoveUI.I.TrySelectMove(defendMove, new GameObject("UI2"));
        int apBefore = (int)apField.GetValue(turnManager);
        SelectMoveUI.I.TrySelectMove(highCostAttack, new GameObject("UI3"));
        int apAfter = (int)apField.GetValue(turnManager);
        Assert.GreaterOrEqual(apAfter, 0);
        Assert.AreEqual(apBefore, apAfter); // shouldnt have taken the expensive move
    }
}
