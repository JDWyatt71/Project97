using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HandbookUI : MonoBehaviour
{
    [SerializeField] private GameObject contentGroup;
    [SerializeField] private GameObject menuOption;
    [SerializeField] private Transform hLGTopMenu;
    [SerializeField] private Transform gridContainerTransform;
    [SerializeField] private GridLayoutGroup gridLayoutGroup;
    [SerializeField] private ScrollRect scrollRect;
    public void DisplaySO<T>(List<T> ts)
    {
        //Destroy previous moves in display if applicable.
        foreach (Transform child in gridContainerTransform)
        {
            Destroy(child.gameObject);
        }
        float maxHeight = 0f;
        foreach (T t in ts)
        {
            RectTransform itemSlotRectTransform = Instantiate(contentGroup, gridContainerTransform).GetComponent<RectTransform>();

            TextMeshProUGUI tmpPUGUI = itemSlotRectTransform.Find("text").GetComponent<TextMeshProUGUI>();
            tmpPUGUI.SetText(t.ToString());
        
            LayoutRebuilder.ForceRebuildLayoutImmediate(gridContainerTransform.GetComponent<RectTransform>());

            Vector2 size = tmpPUGUI.GetPreferredValues();

            maxHeight = Mathf.Max(maxHeight, size.y);
        }
        gridLayoutGroup.cellSize = new Vector2(600, maxHeight);
    }
    private void CreateMenus()
    {
        Dictionary<string, Action> strings = new Dictionary<string, Action>
        {
        { "Character", () => DisplaySO(GameManager.I.GetCCs()) },
        { "Items", () => DisplaySO(AssetsDatabase.I.items) },
        { "Attack Moves", () => DisplaySO(AssetsDatabase.I.aMoves) },
        { "Defend Moves", () => DisplaySO(AssetsDatabase.I.dMoves) }
        };
        foreach(string s in strings.Keys)
        {
            GameObject obj = Instantiate(menuOption, hLGTopMenu);
            obj.transform.SetSiblingIndex(hLGTopMenu.childCount - 2);
            RectTransform itemSlotRectTransform = obj.GetComponent<RectTransform>();
            TextMeshProUGUI tmpPUGUI = itemSlotRectTransform.Find("text").GetComponent<TextMeshProUGUI>();
            tmpPUGUI.text = s;
            obj.GetComponent<Button>().onClick.AddListener(() =>
            {
                strings[s].Invoke();
            });
        }
    }
    
    void Start()
    {
        CreateMenus();
    }
}
