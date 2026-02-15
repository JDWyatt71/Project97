using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class MoveQueueUI : MonoBehaviour
{
    private TurnManager turnManager;

    [SerializeField] private Transform moveQueuePanel;
    [SerializeField] private GameObject moveIcon;
    List<GameObject> moveIcons = new List<GameObject>();

    public void Setup(TurnManager turnManager)
    {
        this.turnManager = turnManager;
        turnManager.OnMoveSelected += AddIcon;
        turnManager.OnMoveDeselected += RemoveIcon;
    }

    // Update is called once per frame
    private void AddIcon()
    {
        GameObject icon = Instantiate(moveIcon, moveQueuePanel);
        icon.GetComponent<Image>().sprite = move.sprite;
        moveIcons.Insert(0, icon);
        icon.transform.SetAsFirstSibling();
    }

    private void RemoveIcon(MoveSO move)
    {
        if (moveIcons.Count == 0) return;

        GameObject icon = moveIcons[0];
        moveIcons.RemoveAt(0);
        Destroy(icon);
    }
}
