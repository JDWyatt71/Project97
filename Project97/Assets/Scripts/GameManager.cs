using Unity.Services.Analytics;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using System;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.UnityConsent;

public class GameManager : MonoBehaviour
{
    public static GameManager I {private set; get;}
    private Inventory pInventory;
    private UpgradeScreenUI upgradeScreenUI;
    [SerializeField] private CharacterSO pCSO;
    [SerializeField] private CharacterSO cCSO1;
    [SerializeField] private CharacterSO cCSO2;

    [SerializeField] private HealthBarUI playerHealthBar;
    [SerializeField] private HealthBarUI computerHealthBar;
    private string currentRunId;
    private float runStartTime;
    private int currentLevel = 0;
    private const int maxLevel = 10;
    private int attackAttempt = 0;
    private int attackSuccess = 0;
    private int defendAttempt = 0;
    private int defendSuccess = 0;
    [SerializeField] private Image computerImage;

    public GameObject pCharacter {private set; get;}
    private TurnManager turnManager;

    void Awake()
    {
        I = this;
    }
    void Start()
    {
        EndUserConsent.SetConsentState(new ConsentState
        {
            AnalyticsIntent = ConsentStatus.Granted,
            AdsIntent = ConsentStatus.Denied
        });

        currentRunId = Guid.NewGuid().ToString();
        runStartTime = Time.time;

        GameEvents.RaiseRunStarted(currentRunId, "normal", runStartTime);
        TelemetryLogger.Instance.SaveToJson();
        Debug.Log("RunStarted event sent;");

        pCharacter = SetupCharacter("Player", pCSO, playerHealthBar);
        pInventory = pCharacter.GetComponent<Inventory>();
        GameObject cCharacter = SetupCharacter("Dojo Challenger", cCSO1, computerHealthBar);
        computerImage.sprite = cCSO1.sprite;

        turnManager = gameObject.AddComponent<TurnManager>();
        turnManager.Setup(pCharacter.GetComponent<Character>(), cCharacter.GetComponent<Character>());
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
        //upgradeScreenUI.DisplayItems(AssetsDatabase.I.items);

        if(playerWon) upgradeScreenUI.DisplayUpgrades();
    }
    private void UpgradeSelected()
    {
        //Once upgrade selected at end of a fight, start the next round
        GameObject cCharacter = SetupCharacter("Comeback Fighter", cCSO2, computerHealthBar);
        computerImage.sprite = cCSO2.sprite;


        turnManager.StartFight(cCharacter.GetComponent<Character>());
    }

    public void PlayerAddItem(ItemSO item, int amount = 1)
    {
        pInventory.AddItem(item, amount);
    }

    private void OnFightEnded(FightResult fightResult)
    {
        currentLevel++;

        attackAttempt += fightResult.AttackAttempts;
        attackSuccess += fightResult.AttackSuccess;
        defendAttempt += fightResult.DefendAttempts;
        defendSuccess += fightResult.DefendSuccess;

        bool runSucessful = currentLevel >= maxLevel;
        string deathCause = fightResult.HpLeft <= 0 ? "death" : "";

        if (runSucessful || fightResult.HpLeft <= 0)
        {
            RunResult runResult = new RunResult()
            {
                RunId = currentRunId,
                Successful = runSucessful,
                Difficulty = "normal", // can be changed when difficulty selection is done
                RunStartTime = runStartTime,
                RunEndTime = Time.time,
                LevelFinish = currentLevel,
                AttackAttempts = attackAttempt,
                AttackSuccess = attackSuccess,
                DefendAttempts = defendAttempt,
                DefendSuccess = defendSuccess,
                DeathCause = deathCause,
                HpLeft = fightResult.HpLeft,
            };

            GameEvents.RaiseRunEnded(runResult);
        }
    }
}
