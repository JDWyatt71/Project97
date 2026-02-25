using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Collections;

public class MoveQueueUI : MonoBehaviour
{
    [SerializeField] private Transform moveQueuePanel;
    [SerializeField] private GameObject moveIcon;
    private Dictionary<MoveSO, GameObject> moveIcons = new Dictionary<MoveSO, GameObject>();

    private void Start()
    {
        StartCoroutine(WaitForTurnManager());
    }

    private IEnumerator WaitForTurnManager()
    {
        while (TurnManager.I == null)
            yield return null;

        TurnManager.I.OnMoveSelected += AddIcon;
        TurnManager.I.OnMoveDeselected += RemoveIcon;
    }

    private void AddIcon(MoveSO move)
    {
        GameObject icon = Instantiate(moveIcon, moveQueuePanel);
        icon.SetActive(true);
        icon.GetComponent<Image>().sprite = move.sprite;
        icon.transform.SetAsFirstSibling();
        moveIcons.Add(move,icon);
        Debug.Log("AddIcon");
    }

    private void RemoveIcon(MoveSO move)
    {
        if (moveIcons.TryGetValue(move, out GameObject icon))
        {
            Destroy(icon);
            moveIcons.Remove(move);
        }
    }


}
