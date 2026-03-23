

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;

public class UpgradeScreenUI : MonoBehaviour
{
    [SerializeField] private int itemsRequired = 2;
    [SerializeField] private GameObject itemTemplate;
    [SerializeField] private Transform itemContainerTransform;
    [SerializeField] private GameObject itemScreen;
    private int itemsSelected = 0;
    private bool selectingItems = false;
    public event Action UpgradeSelected;
    private bool upgradesCreated = false;
    public void DisplayItems(Dictionary<ItemSO, int> items)
    {
        Clear();
        itemScreen.SetActive(true);

        itemsSelected = 0;
        selectingItems = true;

        foreach (var itemtype in items)
        {
            ItemSO item = itemtype.Key;
            int count = itemtype.Value;

            RectTransform itemSlotRectTransform =
                Instantiate(itemTemplate, itemContainerTransform)
                .GetComponent<RectTransform>();

            itemSlotRectTransform.gameObject.SetActive(true);

            Transform imageTransform = itemSlotRectTransform.Find("image");

            imageTransform.GetComponent<Button>().onClick.AddListener(() =>
            {
                StartCoroutine(
                    SelectItem(item,
                    itemSlotRectTransform.Find("selectImage").gameObject,
                    imageTransform.GetComponent<Button>())
                );
            });

            Image image = imageTransform.GetComponent<Image>();
            image.preserveAspect = true;

            if (item.sprite != null)
                image.sprite = item.sprite;

            TextMeshProUGUI text =
                itemSlotRectTransform.Find("text").GetComponent<TextMeshProUGUI>();

            text.SetText($"{item.name} x{count}");
        }
    } 
    /*
    Standard increase: HP +15, attack/accuracy/evasion +6, AP +1

    Increase and decrease. HP +20, attack/accuracy/evasion +8. Decrease is 1/4 of this for respective type.
    increase attack+decrease hp
    Increase hp+decrease attack
    Increase accuracy+decrease evasion
    Increase evasion+decrease accuracy
    */
   private Dictionary<string, int[]> upgrades = new Dictionary<string, int[]>
    {
        { "HP", new[] { 15, 20 } },
        { "AP", new[] { 1, 0 } },
        { "attack", new[] { 6, 8 } },
        { "evasion", new[] { 6, 8 } },
        { "accuracy", new[] { 6, 8 } }
    };
    private List<(string inc, string dec)> comboPairs = new List<(string, string)>
    {
        ("attack", "HP"),
        ("HP", "attack"),
        ("accuracy", "evasion"),
        ("evasion", "accuracy")
    };
    private void IncreaseStat(string name)
    {
        ApplyUpgrade(name, upgrades[name][0]);
    }
    private void IncreaseDecreaseStats(string iN, string dN)
    {
        ApplyUpgrade(iN, upgrades[iN][1]);
        ApplyUpgrade(dN, CalculateDecreaseAmount(dN));
    }

    private int CalculateDecreaseAmount(string dN)
    {
        return -1 * Mathf.CeilToInt(upgrades[dN][1] * 0.25f);
    }

    private void ApplyUpgrade(string name, int amount) 
    {
        var pC = GameManager.I.pC;
        var pHS = pC.healthSystem;
        Debug.Log($"upgraded {name} {amount}");
        switch (name)
        {
            case "HP":
                pHS.IncreaseMaxHealth(amount);
                pHS.Heal(amount);
                break;
            case "AP":
                pC.ChangeActionPoints(amount);
                break;
            case "attack":
                pC.ChangeAttack(amount);
                break;
            case "evasion":
                pC.ChangeEvasion(amount);
                break;
            case "accuracy":
                pC.ChangeAccuracy(amount);
                break;
            default:
                Debug.LogWarning($"Upgrade {name} not found!");
                break;
        }
    }

    public void DisplayUpgrades()
    {
        Character pC = GameManager.I.pC;

        Clear();
        itemScreen.SetActive(true);
        //if (upgradesCreated) return;
        //Increase upgrades
        foreach (string upgrade in upgrades.Keys)
        {
            MakeUpgradeBtn($"{upgrade} +{upgrades[upgrade][0]}").onClick.AddListener(() =>
            {
                IncreaseStat(upgrade);

                TrackUpgradeChosen("stat", $"{upgrade}+{upgrades[upgrade][0]}");

                itemScreen.SetActive(false);
                UpgradeSelected?.Invoke();
            });
        }
        //Increase & decrease upgrades
        foreach ((string inc, string dec) in comboPairs)
        {
            string name = $"{inc} +{upgrades[inc][1]} & {dec} {CalculateDecreaseAmount(dec)}";
            MakeUpgradeBtn(name).onClick.AddListener(() =>
            {
                IncreaseDecreaseStats(inc, dec);

                TrackUpgradeChosen("combo", $"{inc}+{upgrades[inc][1]} & {dec}{CalculateDecreaseAmount(dec)}");

                itemScreen.SetActive(false);
                UpgradeSelected?.Invoke();
            });
        }

        //Round will be 2 for first in game upgrade screen. Which results in index 1 (the second upgradeSOs, after the starting one)
        UpgradesSO upgradesSO = AssetsDatabase.I.upgradesSOs[GameManager.I.round - 1];
        List<AttackSO> aMovePool = new List<AttackSO>(upgradesSO.aSOs); //Shallow copy
        List<AttackSO> upgradeAMovePool = new List<AttackSO>();
        while (upgradeAMovePool.Count < 2) { //Get two new random attackSOs
            int r = UnityEngine.Random.Range(0,aMovePool.Count);

            AttackSO attackSO = aMovePool[r];

            aMovePool.RemoveAt(r);

            if(!pC.GetAMoves().Contains(attackSO)){
                upgradeAMovePool.Add(attackSO);
            }
            
        }
        

        List<DefendSO> dMovePool = AssetsDatabase.I.dMoves;
        
        foreach (AttackSO move in upgradeAMovePool)
        {
            AttackSO localMove = move; //Safety copy for closure
            CreateUpgrade(pC.GetAMoves().Contains(localMove), localMove.name, (pC) => pC.AddAMoves(localMove));
        }

        foreach (DefendSO move in dMovePool.Take(2))
        {
            DefendSO localMove = move;
            CreateUpgrade(pC.GetDMoves().Contains(localMove), localMove.name, (pC) => pC.AddDMoves(localMove));
        }

        CreateUpgrade(pC.GetDMoves().Contains(dMovePool[2]), "Block", (pC) => pC.AddDMoves(dMovePool.ToArray()[2..5]));
        CreateUpgrade(pC.GetDMoves().Contains(dMovePool[5]),"Guard", (pC) => pC.AddDMoves(dMovePool.ToArray()[5..8]));

        //Add all rest defend moves if applicable (like in level 1 and onwards)
        if (upgradesSO.allDefendAvailable)
        {
            CreateUpgrade(pC.GetDMoves().Contains(dMovePool[8]), "Counter", (pC) => pC.AddDMoves(dMovePool.ToArray()[8..11]));

            for(int i = 11; i < dMovePool.Count; i++) //Adds all remaining that have no height - currently only Duck
            {
                DefendSO localMove = dMovePool[i];
                CreateUpgrade(pC.GetDMoves().Contains(localMove), localMove.name, (pC) => pC.AddDMoves(localMove));
            }
        }
        upgradesCreated = true;
    }

    
    private void CreateUpgrade(bool haveMove, string name, Action<Character> upgradeLogic)
    {
        if(haveMove) return;
        MakeUpgradeBtn("Unlock " + name).onClick.AddListener(() =>
        {
            Character pC = GameManager.I.pC;

            upgradeLogic?.Invoke(pC);

            TrackUpgradeChosen("move_unlock", name);

            itemScreen.SetActive(false);
            UpgradeSelected?.Invoke();
        });
    }
    private Button MakeUpgradeBtn(string upgradeName)
    {
        RectTransform itemSlotRectTransform = Instantiate(itemTemplate, itemContainerTransform).GetComponent<RectTransform>();
        itemSlotRectTransform.gameObject.SetActive(true);

        TextMeshProUGUI priceForNextActionText = itemSlotRectTransform.Find("text").GetComponent<TextMeshProUGUI>();
        priceForNextActionText.SetText(string.Format("{0}\n", upgradeName));

        Transform imageTransform = itemSlotRectTransform.Find("image");
        return imageTransform.GetComponent<Button>();

        /*Image image = itemSlotRectTransform.Find("image").GetComponent<Image>();
        image.preserveAspect = true;
        if (item.sprite != null)
        {
            image.sprite = item.sprite;
        }*/
        
    }

    private IEnumerator SelectItem(ItemSO item, GameObject selectImage, Button button)
    {
        if (!selectingItems)
            yield break;

        Inventory inventory = GameManager.I.pInventory;
        Character pC = GameManager.I.pC;

        selectImage.SetActive(true);

        //yield return new WaitForSeconds(1f);

        if (inventory.HasAmountOfItem(item))
        {
            GameManager.I.pInventory.UseItem(item, pC);

            itemsSelected++;
            button.interactable = false;
        
            if (itemsSelected >= itemsRequired)
            {
                selectingItems = false;
                DisplayUpgrades();
            }
        }
        else
        {
            selectImage.SetActive(false);
        }
    }

    private void Clear()
    {
        foreach (Transform child in itemContainerTransform)
        {
            Destroy(child.gameObject);
        }
    }

    private void TrackUpgradeChosen(string type, string value)
    {
        int level = GameManager.I.round;
        string runId = GameManager.I.CurrentRunId;
        GameEvents.RaiseUpgradeChosen(level, type, value, runId, GameManager.I.CurrentSessionId);
        Debug.Log($"Upgrade Tracked: level: {level}|| type: {type}|| value: {value}");
    }
}
