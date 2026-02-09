using System.Collections;
using System.Collections.Generic;
using TMPro;
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
    private IEnumerator SelectItem(ItemSO item, GameObject selectImage)
    {
        //Optional delay
        selectImage.SetActive(true);
        yield return new WaitForSeconds(1f);
        GameManager.I.PlayerAddItem(item);
        itemScreen.SetActive(false);
    }
}
