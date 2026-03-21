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
    [SerializeField] private GameObject itemTemplate;
    [SerializeField] private Transform itemContainerTransform;
    [SerializeField] private GameObject itemScreen;
    public event Action UpgradeSelected;
    private bool upgradesCreated = false;
    public void DisplayItems(List<ItemSO> items)
    {
        itemScreen.SetActive(true);
        foreach (ItemSO item in items)
        {
            RectTransform itemSlotRectTransform = Instantiate(itemTemplate, itemContainerTransform).GetComponent<RectTransform>();
            itemSlotRectTransform.gameObject.SetActive(true);

            Transform imageTransform = itemSlotRectTransform.Find("image");

            imageTransform.GetComponent<Button>().onClick.AddListener(() =>
            {
                StartCoroutine(SelectItem(item, itemSlotRectTransform.Find("selectImage").gameObject));
            });
            
            Image image = itemSlotRectTransform.Find("image").GetComponent<Image>();
            image.preserveAspect = true;
            if (item.sprite != null)
            {
                image.sprite = item.sprite;
            }

            TextMeshProUGUI priceForNextActionText = itemSlotRectTransform.Find("text").GetComponent<TextMeshProUGUI>();
            priceForNextActionText.SetText(string.Format("{0}\n", item.name));
        }
    } 
   private List<string> upgrades = new List<string>()
    {
        "Hit Points +15",
        "Attack +8 & Evasion -2",
    };
    public void ApplyUpgrade(string upgradeName)
    {
        var character = GameManager.I.pCharacter;
        var pC = character.GetComponent<Character>();
        var pHS = character.GetComponent<HealthSystem>();

        switch (upgradeName)
        {
            case "Hit Points +15":
                pHS.IncreaseMaxHealth(15);
                pHS.Heal(15);
                break;

            case "Attack +8 & Evasion -2":
                pC.ChangeAttack(8);
                pC.ChangeEvasion(-2);
                break;

            default:
                Debug.LogWarning($"Upgrade {upgradeName} not found!");
                break;
        }
    }
    //pC.AddAMoves(AssetsDatabase.I.aMoves[0]);

    public void DisplayUpgrades()
    {
        Character pC = GameManager.I.pCharacter.GetComponent<Character>();

        itemScreen.SetActive(true);
        if (upgradesCreated) return;
        foreach (string upgrade in upgrades)
        {
            MakeUpgradeBtn(upgrade).onClick.AddListener(() =>
            {
                ApplyUpgrade(upgrade);
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

        CreateUpgrade(pC.GetDMoves().Contains(AssetsDatabase.I.dMoves[2]), "Block", (pC) => pC.AddDMoves(AssetsDatabase.I.dMoves.ToArray()[2..5]));
        CreateUpgrade(pC.GetDMoves().Contains(AssetsDatabase.I.dMoves[5]),"Guard", (pC) => pC.AddDMoves(AssetsDatabase.I.dMoves.ToArray()[5..8]));

        //Add all rest defend moves if applicable (like in level 1 and onwards)
        if (upgradesSO.allDefendAvailable)
        {
            CreateUpgrade(pC.GetDMoves().Contains(AssetsDatabase.I.dMoves[8]), "Counter", (pC) => pC.AddDMoves(AssetsDatabase.I.dMoves.ToArray()[8..11]));

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
            Character pC = GameManager.I.pCharacter.GetComponent<Character>();

            upgradeLogic?.Invoke(pC);

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

    private IEnumerator SelectItem(ItemSO item, GameObject selectImage)
    {
        //Optional delay
        selectImage.SetActive(true);
        yield return new WaitForSeconds(1f);
        GameManager.I.PlayerAddItem(item);
        itemScreen.SetActive(false);
    }
}
