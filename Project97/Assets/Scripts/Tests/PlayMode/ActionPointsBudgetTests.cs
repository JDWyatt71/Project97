using System;
using System.Collections.Generic;
using System.Reflection;
#if UNITY_EDITOR
using NUnit.Framework;
#endif
using UnityEngine;

// test that AP cant go negative cant select move if too expensive AP goes down when u select and back up when u deselect
public class ActionPointsBudgetTests
{
    private GameObject testGameObject;
    private Character testCharacter;
    private AttackSO lowCostAttack;
    private AttackSO highCostAttack;
    private DefendSO defendMove;
    private GameObject assetsDatabaseObj;
    private SelectMoveUI selectMoveUI;

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

        // simple AP / selection UI (no TurnManager needed now)
        GameObject selectUIObj = new GameObject("SelectMoveUI");
        selectMoveUI = selectUIObj.AddComponent<SelectMoveUI>();

        // initialise private AP fields on SelectMoveUI
        var uiType = typeof(SelectMoveUI);
        BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
        FieldInfo apField = uiType.GetField("pAPRemaining", flags);
        FieldInfo selectedMovesField = uiType.GetField("selectedMoves", flags);
        FieldInfo selectedObjsField = uiType.GetField("selectedObjs", flags);

        apField.SetValue(selectMoveUI, testCharacter.actionPoints);
        selectedMovesField.SetValue(selectMoveUI, new List<MoveSO>());
        selectedObjsField.SetValue(selectMoveUI, new List<GameObject>());
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
        if (selectMoveUI != null)
            UnityEngine.Object.DestroyImmediate(selectMoveUI.gameObject);
    }

    [Test]
    public void AP_DontGoNegative()
    {
        Type uiType = typeof(SelectMoveUI);
        BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
        FieldInfo apField = uiType.GetField("pAPRemaining", flags);

        // pick a move that fits AP should go down
        GameObject ui1 = new GameObject("UI1");
        selectMoveUI.TrySelectMove(lowCostAttack, ui1);
        int ap = (int)apField.GetValue(selectMoveUI);
        Assert.GreaterOrEqual(ap, 0);
        Assert.AreEqual(3, ap); // 5 - 2

        // unselecting should give AP back
        selectMoveUI.TrySelectMove(lowCostAttack, ui1);
        ap = (int)apField.GetValue(selectMoveUI);
        Assert.AreEqual(5, ap);

        // now try to pick something that cant be afforded (only 2 AP left need 5)
        selectMoveUI.TrySelectMove(lowCostAttack, ui1);
        selectMoveUI.TrySelectMove(defendMove, new GameObject("UI2"));
        int apBefore = (int)apField.GetValue(selectMoveUI);
        selectMoveUI.TrySelectMove(highCostAttack, new GameObject("UI3"));
        int apAfter = (int)apField.GetValue(selectMoveUI);
        Assert.GreaterOrEqual(apAfter, 0);
        Assert.AreEqual(apBefore, apAfter); // shouldnt have taken the expensive move
    }
}
