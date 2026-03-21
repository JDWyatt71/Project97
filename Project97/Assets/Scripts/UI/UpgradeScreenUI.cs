

using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
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
        Clear();
        itemScreen.SetActive(true);
        //if(upgradesCreated) return;
        foreach (KeyValuePair<string, Action> pair in upgrades)
        {
            RectTransform itemSlotRectTransform = Instantiate(itemTemplate, itemContainerTransform).GetComponent<RectTransform>();
            itemSlotRectTransform.gameObject.SetActive(true);

            Transform imageTransform = itemSlotRectTransform.Find("image");

            imageTransform.GetComponent<Button>().onClick.AddListener(() =>
            {
                pair.Value(); //Run function for upgrade
                itemScreen.SetActive(false);
                UpgradeSelected?.Invoke();
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
        upgradesCreated = true;
    }
    private IEnumerator SelectItem(ItemSO item, GameObject selectImage, Button button)
    {
        if (!selectingItems)
            yield break;

        Inventory inventory = GameManager.I.pInventory;
        Character pC = GameManager.I.pCharacter.GetComponent<Character>();

        selectImage.SetActive(true);

        yield return new WaitForSeconds(1f);

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
}
