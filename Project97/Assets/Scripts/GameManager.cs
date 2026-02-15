using System;
using Unity.Services.Analytics;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.UnityConsent;

public class GameManager : MonoBehaviour
{
    public static GameManager I;
    private Inventory pInventory;
    private UpgradeScreenUI upgradeScreenUI;
    [SerializeField] private CharacterSO pCSO;
    [SerializeField] private CharacterSO cCSO;
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
        UnityServices.InitializeAsync();

        currentRunId = Guid.NewGuid().ToString();
        runStartTime = Time.time;

        GameEvents.RaiseRunStarted(currentRunId, "normal", runStartTime);
        Debug.Log("RunStarted event sent;");

        GameObject pCharacter = SetupCharacter("Player", pCSO, playerHealthBar);
        pInventory = pCharacter.GetComponent<Inventory>();
        GameObject cCharacter = SetupCharacter("Computer", cCSO, computerHealthBar);

        TurnManager turnManager = gameObject.AddComponent<TurnManager>();
        turnManager.Setup(pCharacter.GetComponent<Character>(), cCharacter.GetComponent<Character>());
        turnManager.RoundComplete += RoundComplete;
        GameEvents.FightEnded += OnFightEnded;
        upgradeScreenUI = GetComponent<UpgradeScreenUI>();
    }
    private GameObject SetupCharacter(String name, CharacterSO characterSO, HealthBarUI healthBarUI)
    {
        GameObject character = Instantiate(AssetsDatabase.I.characterPf);
        character.name = name;
        character.GetComponent<Character>().Setup(characterSO);
        healthBarUI.Setup(character.GetComponent<HealthSystem>());
        return character;
    }

    private void RoundComplete()
    {
        upgradeScreenUI.DisplayItems(AssetsDatabase.I.items);
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
