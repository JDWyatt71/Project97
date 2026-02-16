using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeScreenUI : MonoBehaviour
{
    [SerializeField] private GameObject itemTemplate;
    [SerializeField] private Transform itemContainerTransform;
    [SerializeField] private GameObject itemScreen;
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
   private Dictionary<string, Action> upgrades = new Dictionary<string, Action>()
    {
        { "Hit Points +15", () =>
            {
                HealthSystem pHS = GameManager.I.pCharacter.GetComponent<HealthSystem>();
                pHS.IncreaseMaxHealth(15);
                pHS.Heal(15);
            }
        },

        { "Attack +8 & Evasion -2", () =>
            {
                Character pC = GameManager.I.pCharacter.GetComponent<Character>();
                pC.ChangeAttack(+8);
                pC.ChangeEvasion(-2);
            }
        },

        { "Unlock Floor Throwdown", () =>
            {
                Character pC = GameManager.I.pCharacter.GetComponent<Character>();
                pC.AddAMove(AssetsDatabase.I.aMoves[0]);
            }
        },

        { "Unlock High Crosscut", () =>
            {
                Character pC = GameManager.I.pCharacter.GetComponent<Character>();
                pC.AddAMove(AssetsDatabase.I.aMoves[1]);

            }
        }
    };

    public void DisplayUpgrades()
    {
        itemScreen.SetActive(true);
        foreach (KeyValuePair<string, Action> pair in upgrades)
        {
            RectTransform itemSlotRectTransform = Instantiate(itemTemplate, itemContainerTransform).GetComponent<RectTransform>();
            itemSlotRectTransform.gameObject.SetActive(true);

            Transform imageTransform = itemSlotRectTransform.Find("image");

            imageTransform.GetComponent<Button>().onClick.AddListener(() =>
            {
                pair.Value(); //Run function for upgrade
                itemScreen.SetActive(false);

            });
            
            /*Image image = itemSlotRectTransform.Find("image").GetComponent<Image>();
            image.preserveAspect = true;
            if (item.sprite != null)
            {
                image.sprite = item.sprite;
            }*/

            TextMeshProUGUI priceForNextActionText = itemSlotRectTransform.Find("text").GetComponent<TextMeshProUGUI>();
            priceForNextActionText.SetText(string.Format("{0}\n", pair.Key));
        }
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
