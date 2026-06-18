

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Collections;
using Unity.VisualScripting;


#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Tilemaps;

#endif
using UnityEngine;
using UnityEngine.UI;

public class UpgradeScreenUI : MonoBehaviour
{
    [SerializeField] private int itemsRequired = 2;
    [SerializeField] private int movesRequired = 3;
    [SerializeField] private GameObject itemTemplate;
    [SerializeField] private Transform itemContainerTransform;
    [SerializeField] private GameObject itemScreen;

    private int itemsSelected = 0;
    private bool selectingItems = false;
    private int movesSelected = 0;
    private bool selectingMoves = false;
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
    private Dictionary<string, string> comboPairs = new Dictionary<string, string>
    {
        {"attack", "HP" },
        {"HP", "attack" },
        {"accuracy", "evasion" },
        {"evasion", "accuracy" }
    };
    //List of all defensive moves not considering heights and their index ranges within AssetDatabase
    private Dictionary<string, Range> Dmoves = new Dictionary<string, Range>
    {
        { "Dodge",  0..1 },
        { "Foot Shuffle",  1..2 },
        { "Block",  2..5 },
        { "Guard",  5..8 },
        { "Counter",  8..11 },
        { "Duck",  11..12 },
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



    private Dictionary<string, int[]> GenerateStatUpgrades()
    {
        Dictionary<string, int[]> selectedUpgrades = new Dictionary<string, int[]>();
        Dictionary<string, int[]> possibleUpgrades = new Dictionary<string, int[]>(upgrades);
        for (int i=0; i < 2; i++) 
        {
            int statselection = UnityEngine.Random.Range(0, possibleUpgrades.Count());
            selectedUpgrades.Add(possibleUpgrades.ElementAt(statselection).Key, possibleUpgrades.ElementAt(statselection).Value);
            possibleUpgrades.Remove(possibleUpgrades.ElementAt(statselection).Key); 
        }
        //foreach (string upgrade in selectedUpgrades.Keys) print(upgrade);
        return selectedUpgrades;
    }

    private List<AttackSO> GenerateAttackMoves(List<AttackSO> availableMoves, bool defendAppears)
    {
        List<AttackSO> selectedASOs = new List<AttackSO>();
        int aSOCount = defendAppears ? 1 : 2; //If defend appears, only 1 attack move is offered per upgrade screen, otherwise 2.
        for (int i = 0; i < aSOCount; i++)
        {
            int moveselection = UnityEngine.Random.Range(0, availableMoves.Count());
            selectedASOs.Add(availableMoves[moveselection]);
            availableMoves.RemoveAt(moveselection);
        }
        return selectedASOs;
    }

    private (string, Range) GenerateDefensiveMove(Dictionary<string, Range> availableMoves, bool allDefendAvailable)
    {
        //Removes advanced defensive options for early levels
        if (!allDefendAvailable) 
        {
            availableMoves.Remove("Counter");
            availableMoves.Remove("Duck");
        }

        int moveselection = UnityEngine.Random.Range(0, availableMoves.Count());
        string movename = availableMoves.ElementAt(moveselection).Key;
        Range pos = availableMoves.ElementAt(moveselection).Value;

        return (movename, pos);
    }

    public void DisplayUpgrades()
    {
        Character pC = GameManager.I.pC;
        UpgradesSO upgradesSO = AssetsDatabase.I.upgradesSOs[GameManager.I.round - 1];
        Debug.Log(upgradesSO.name);
        Clear();
        itemScreen.SetActive(true);

        if(upgradesSO.statsUpgradesPossible) {
  
            Dictionary<string, int[]> selectedUpgrades = GenerateStatUpgrades();
            foreach (string upgrade in upgrades.Keys) { print(upgrade); }
            foreach (string upgrade in selectedUpgrades.Keys)
            {
                //50% chance to get a standard increase upgrade vs an increase/decrease combo upgrade
                if (UnityEngine.Random.value < 0.5f || upgrade == "AP")
                //Standard increase upgrade
                {
                    MakeUpgradeBtn($"{upgrade} +{upgrades[upgrade][0]}").onClick.AddListener(() =>
                    {
                        IncreaseStat(upgrade);

                        TrackUpgradeChosen("stat", $"{upgrade}+{upgrades[upgrade][0]}");

                        itemScreen.SetActive(false);
                        UpgradeSelected?.Invoke();
                    });
                }
                else
                //Increase/decrease combo upgrade
                {
                    string inc = upgrade;
                    string dec = comboPairs[upgrade];
                    string name = $"{inc} +{upgrades[inc][1]} & {dec} {CalculateDecreaseAmount(dec)}";

                    MakeUpgradeBtn(name).onClick.AddListener(() =>
                    {
                        IncreaseDecreaseStats(inc, dec);

                        TrackUpgradeChosen("combo", $"{inc}+{upgrades[inc][1]} & {dec}{CalculateDecreaseAmount(dec)}");

                        itemScreen.SetActive(false);
                        UpgradeSelected?.Invoke();
                    });
                }
            }

            //Round will be 2 for first in game upgrade screen. Which results in index 1 (the second upgradeSOs, after the starting one)
            List<AttackSO> availableAttackMoves = new List<AttackSO>();

            foreach (var move in upgradesSO.aSOs)
            {
                if (!pC.GetAMoves().Contains(move))
                {
                    availableAttackMoves.Add(move);
                }
            }

            List<AttackSO> selectedASOs = GenerateAttackMoves(availableAttackMoves, upgradesSO.defendAppears); //randomly selects attack moves and adds them to upgrade screen

            foreach (AttackSO move in selectedASOs)
            {
                AttackSO localMove = move; //Safety copy for closure
                CreateUpgrade(false, localMove.name, (pC) => pC.AddAMoves(localMove));
            }



            if (upgradesSO.defendAppears)
            {
                List<DefendSO> dMovePool = AssetsDatabase.I.dMoves;
                Dictionary<string, Range> availableDefendMoves = new Dictionary<string, Range>(Dmoves);

                foreach (var move in dMovePool)
                {
                    if (pC.GetDMoves().Contains(move))
                        availableDefendMoves.Remove(move.name);
                }

                //randomly selects defend moves and adds them to upgrade screen

                (string move, Range pos) selectedDSO = GenerateDefensiveMove(availableDefendMoves, upgradesSO.allDefendAvailable);
                CreateUpgrade(false, selectedDSO.move, (pC) => pC.AddDMoves(dMovePool.ToArray()[selectedDSO.pos]));
            }

        }
        else
        {
            List<DefendSO> dMovePool = AssetsDatabase.I.dMoves;
            Dictionary<string, Range> availableDefendMoves = new Dictionary<string, Range>(Dmoves);

            availableDefendMoves.Remove("Counter");
            availableDefendMoves.Remove("Duck");

            foreach (var movedata in availableDefendMoves)
            {
                string move = movedata.Key;
                Range pos = movedata.Value;
                CreateUpgrade(false, move, (pC) => pC.AddDMoves(dMovePool.ToArray()[pos]));
            }
        }
        upgradesCreated = true;
    }

    public void DisplayInitialUpgrades()
    {
        Clear();
        itemScreen.SetActive(true);
        Character pC = GameManager.I.pC;

        movesSelected = 0;
        selectingMoves = true;

        List<AttackSO> startingMoves = AssetsDatabase.I.upgradesSOs[0].aSOs; //Starting moves

        foreach (var move in startingMoves)
        {
            if (pC.GetAMoves().Contains(move)) continue;

            RectTransform itemSlotRectTransform =
                Instantiate(itemTemplate, itemContainerTransform)
                .GetComponent<RectTransform>();

            itemSlotRectTransform.gameObject.SetActive(true);

            Transform imageTransform = itemSlotRectTransform.Find("image");

            imageTransform.GetComponent<Button>().onClick.AddListener(() =>
            {
                StartCoroutine(
                    SelectInitialAttack(move,
                    itemSlotRectTransform.Find("selectImage").gameObject,
                    imageTransform.GetComponent<Button>())
                );
            });

            Image image = imageTransform.GetComponent<Image>();
            image.preserveAspect = true;

            if (move.sprite != null)
                image.sprite = move.sprite;

            TextMeshProUGUI text =
                itemSlotRectTransform.Find("text").GetComponent<TextMeshProUGUI>();

            text.SetText($"{move.name}");
        }
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

    private IEnumerator SelectInitialAttack(AttackSO move, GameObject selectImage, Button button)
    {
        if (!selectingMoves)
            yield break;

        Character pC = GameManager.I.pC;

        selectImage.SetActive(true);

        pC.AddAMoves(move);

        movesSelected++;
        button.interactable = false;

        if (movesSelected >= movesRequired)
        {
            selectingMoves = false;
            DisplayUpgrades(); //defensive later
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

/*
            * foreach (string upgrade in upgrades.Keys)
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
            */


/*
         * foreach (DefendSO move in dMovePool.Take(2))
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
         */

/*
        int picksNeeded = Mathf.Min(2, availableMoves.Count); //Caps if player unlocked all but 0/1 moves.

       List<AttackSO> upgradeAMovePool = new List<AttackSO>();
       while (upgradeAMovePool.Count < picksNeeded) { //Get two new random attackSOs
           int r = UnityEngine.Random.Range(0,availableMoves.Count);
           upgradeAMovePool.Add(availableMoves[r]);
           availableMoves.RemoveAt(r);
       }
        */