using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager I;
    private Inventory pInventory;
    [SerializeField] private UpgradeScreenUI upgradeScreenUI;
    void Awake()
    {
        I = this;
    }
    void Start()
    {
        GameObject pCharacter = Instantiate(AssetsDatabase.I.characterPf);
        pCharacter.name = "Player";
        pInventory = pCharacter.GetComponent<Inventory>();
        GameObject cCharacter = Instantiate(AssetsDatabase.I.characterPf);
        cCharacter.name = "Computer";
        TurnManager turnManager = gameObject.AddComponent<TurnManager>();
        turnManager.Setup(pCharacter.GetComponent<Character>(), cCharacter.GetComponent<Character>());
        turnManager.RoundComplete += RoundComplete;
    }

    private void RoundComplete()
    {
        //upgradeScreenUI.DisplayItems();
    }
    public void PlayerAddItem(ItemSO item, int amount = 1)
    {
        pInventory.AddItem(item, amount);
    }
}
