using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MovesUIScreen : MonoBehaviour
{
    [SerializeField] private GameObject itemTemplate;
    [SerializeField] private Transform itemContainerTransform;
    public void DisplayMoves(List<MoveSO> moves)
    {
        //Destroy previous moves in display if applicable.
        foreach (Transform child in itemContainerTransform)
        {
            Destroy(child.gameObject);
        }
        foreach (MoveSO move in moves)
        {
            RectTransform itemSlotRectTransform = Instantiate(itemTemplate, itemContainerTransform).GetComponent<RectTransform>();
            itemSlotRectTransform.gameObject.SetActive(true);

            Transform imageTransform = itemSlotRectTransform.Find("image");

            imageTransform.GetComponent<Button>().onClick.AddListener(() =>
            {
                SelectMoveUI.I.TrySelectMove(move, itemSlotRectTransform.Find("selectImage").gameObject);
            });
            
            Image image = itemSlotRectTransform.Find("image").GetComponent<Image>();
            image.preserveAspect = true;
            if (move.sprite != null)
            {
                image.sprite = move.sprite;
            }

            TextMeshProUGUI priceForNextActionText = itemSlotRectTransform.Find("text").GetComponent<TextMeshProUGUI>();
            priceForNextActionText.SetText(string.Format("{0}\nAP: {1}", move.name, move.AP));
        }
    }
}