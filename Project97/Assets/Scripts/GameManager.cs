using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager I;
    private Inventory pInventory;
    private UpgradeScreenUI upgradeScreenUI;
    [SerializeField] private CharacterSO pCSO;
    [SerializeField] private CharacterSO cCSO1;
    [SerializeField] private CharacterSO cCSO2;

    [SerializeField] private HealthBarUI playerHealthBar;
    [SerializeField] private HealthBarUI computerHealthBar;

    public GameObject pCharacter {private set; get;}
    private TurnManager turnManager;

    void Awake()
    {
        I = this;
    }
    void Start()
    {
        pCharacter = SetupCharacter("Player", pCSO, playerHealthBar);
        pInventory = pCharacter.GetComponent<Inventory>();
        GameObject cCharacter = SetupCharacter("Dojo Challenger", cCSO1, computerHealthBar);

        turnManager = gameObject.AddComponent<TurnManager>();
        turnManager.Setup(pCharacter.GetComponent<Character>(), cCharacter.GetComponent<Character>());
        turnManager.RoundComplete += RoundComplete;
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

        turnManager.StartFight(cCharacter.GetComponent<Character>());
    }

    public void PlayerAddItem(ItemSO item, int amount = 1)
    {
        pInventory.AddItem(item, amount);
    }
}
