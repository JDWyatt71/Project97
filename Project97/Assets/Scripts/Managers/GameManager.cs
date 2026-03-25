using Unity.Services.Analytics;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using System;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.UnityConsent;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager I {private set; get;}
    public Inventory pInventory { private set; get; }
    private UpgradeScreenUI upgradeScreenUI;
    [SerializeField] private CharacterSO pCSO;
    [SerializeField] private List<CharacterSO> cCs;
    [SerializeField] private HealthBarUI playerHealthBar;
    [SerializeField] private HealthBarUI computerHealthBar;
    [SerializeField] private EndScreenUI endScreenUI;
    private string currentRunId;
    private float runStartTime;
    private int currentLevel = 0;
    private const int maxLevel = 10;
    private int attackAttempt = 0;
    private int attackSuccess = 0;
    private int defendAttempt = 0;
    private int defendSuccess = 0;
    private int hpLeft;
    [SerializeField] private Image computerImage;

    public GameObject pCharacter {private set; get;}
    public Character pC { private set; get; }
    private TurnManager turnManager;
    public int round {private set; get;} = 1;
    public string CurrentRunId => currentRunId;
    public string CurrentSessionId { get; private set; }

    void Awake()
    {
        I = this;
    }
    void Start()
    {
        TelemetryConsentManager.ApplyConsent(TelemetryConsentManager.IsEnabled());

        currentRunId = Guid.NewGuid().ToString();
        CurrentSessionId = Guid.NewGuid().ToString();
        //CurrentRunId = currentRunId;
        runStartTime = Time.time;
        Difficulty difficulty = UC.GetDifficulty();

        GameEvents.RaiseRunStarted(currentRunId, difficulty.ToString(), runStartTime, CurrentSessionId);
        TelemetryLogger.Instance.SaveToJson();
        Debug.Log("RunStarted event sent;");

        pCharacter = SetupCharacter("Player", pCSO, playerHealthBar);
        pC = pCharacter.GetComponent<Character>();
        pInventory = pCharacter.GetComponent<Inventory>();
        pInventory.SetupInventory(difficulty);

        int index = Mathf.Clamp(round - 1, 0, cCs.Count - 1);
        CharacterSO cSO = cCs[index];
        GameObject cCharacter = SetupCharacter(cSO.name, cSO, computerHealthBar);
        computerImage.sprite = cSO.sprite;

        turnManager = gameObject.AddComponent<TurnManager>();
        turnManager.Setup(pC, cCharacter.GetComponent<Character>());
        turnManager.RoundComplete += RoundComplete;
        GameEvents.FightEnded += OnFightEnded;
        upgradeScreenUI = GetComponent<UpgradeScreenUI>();
        upgradeScreenUI.UpgradeSelected += UpgradeSelected;
    }

    private GameObject SetupCharacter(String name, CharacterSO characterSO, HealthBarUI healthBarUI)
    {
        GameObject character = Instantiate(AssetsDatabase.I.characterPf);
        character.name = name;
        character.GetComponent<Character>().Setup(characterSO);
        healthBarUI.Setup(character.GetComponent<HealthSystem>());
        return character;
    }

    private void RoundComplete(bool playerWon)
    {
        if (!playerWon)
        {
            endScreenUI.DisplayEndScreen("Defeat", round, attackAttempt, attackSuccess, defendAttempt, defendSuccess, hpLeft, runStartTime);

        }
        if(round >= cCs.Count)
        {
            //All rounds complete show victory screen
            endScreenUI.DisplayEndScreen("Victory", round, attackAttempt, attackSuccess, defendAttempt, defendSuccess, hpLeft, runStartTime);
        }

        //upgradeScreenUI.DisplayItems(AssetsDatabase.I.items);
        round++;
        pC.ResetRestActions();
        pC.RemoveAllEffects();
        if(playerWon) upgradeScreenUI.DisplayItems(pInventory.GetInventory());
    }
    private void UpgradeSelected()
    {
        //Once upgrade selected at end of a fight, start the next round
        CharacterSO cSO = cCs[round-1];

        GameObject cCharacter = SetupCharacter(cSO.name, cSO, computerHealthBar);
        computerImage.sprite = cSO.sprite;

        //Passive upgrades between every round
        pC.ChangeAccuracy(2); pC.ChangeAttack(2); pC.ChangeEvasion(2);
        HealthSystem pHS = pCharacter.GetComponent<HealthSystem>();
        pHS.IncreaseMaxHealth(5);
        pHS.Heal(5);

        turnManager.StartFight(cCharacter.GetComponent<Character>());
    }

    public void PlayerAddItem(ItemSO item, int amount = 1)
    {
        pInventory.AddItem(item, amount);
    }

    private void OnFightEnded(FightResult fightResult)
    {
        currentLevel++;

        fightResult.level = currentLevel;

        attackAttempt += fightResult.AttackAttempts;
        attackSuccess += fightResult.AttackSuccess;
        defendAttempt += fightResult.DefendAttempts;
        defendSuccess += fightResult.DefendSuccess;
        hpLeft = fightResult.HpLeft;

        bool runSucessful = currentLevel >= maxLevel;
        string deathCause = fightResult.HpLeft <= 0 ? "death" : "";

        if (runSucessful || fightResult.HpLeft <= 0)
        {
            RunResult runResult = new RunResult()
            {
                RunId = currentRunId,
                Successful = runSucessful,
                Difficulty = UC.GetDifficulty().ToString(), // can be changed when difficulty selection is done
                RunStartTime = runStartTime,
                RunEndTime = Time.time,
                LevelFinish = currentLevel,
                AttackAttempts = attackAttempt,
                AttackSuccess = attackSuccess,
                DefendAttempts = defendAttempt,
                DefendSuccess = defendSuccess,
                DeathCause = deathCause,
                HpLeft = fightResult.HpLeft,
                sessionID = CurrentSessionId
            };

            GameEvents.RaiseRunEnded(runResult);
        }
    }

    private void OnApplicationQuit()
    {
        GameEvents.RaiseGameQuit(CurrentSessionId);
    }
    public void ReturnHome()
    {
        SceneManager.LoadScene(0);
    }
}
