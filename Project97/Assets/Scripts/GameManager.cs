using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager I;
    private Inventory pInventory;
    private UpgradeScreenUI upgradeScreenUI;
    [SerializeField] private CharacterSO pCSO;
    [SerializeField] private CharacterSO cCSO;

    void Awake()
    {
        I = this;
    }
    void Start()
    {
        GameObject pCharacter = SetupCharacter("Player", pCSO);
        pInventory = pCharacter.GetComponent<Inventory>();
        GameObject cCharacter = SetupCharacter("Computer", cCSO);

        TurnManager turnManager = gameObject.AddComponent<TurnManager>();
        turnManager.Setup(pCharacter.GetComponent<Character>(), cCharacter.GetComponent<Character>());
        turnManager.RoundComplete += RoundComplete;
        upgradeScreenUI = GetComponent<UpgradeScreenUI>();
    }

    private GameObject SetupCharacter(String name, CharacterSO characterSO)
    {
        GameObject pCharacter = Instantiate(AssetsDatabase.I.characterPf);
        pCharacter.name = name;
        pCharacter.GetComponent<Character>().Setup(characterSO);
        return pCharacter;
    }

    private void RoundComplete()
    {
        upgradeScreenUI.DisplayItems(AssetsDatabase.I.items);
    }
    public void PlayerAddItem(ItemSO item, int amount = 1)
    {
        pInventory.AddItem(item, amount);
    }
}
