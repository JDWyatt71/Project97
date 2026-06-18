using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class SelectMoveUI : MonoBehaviour
{
    public static SelectMoveUI I {private set; get;}
    void Awake()
    {
        I = this;
    }
    #region Select player moves
    
    private int pAPRemaining;
    public int GetCurrentAP()
    {
        return pAPRemaining;
    }
    public delegate void OnAPChanged(int current);
    public event OnAPChanged APChanged;
    public event Action<MoveSO> OnMoveSelected;
    public event Action<MoveSO> OnMoveDeselected;
    private int maxAttackMoves = 4;
    private int maxDefendMoves = 2;
    private int attackMoves;
    private int defenseMoves;
    private bool multipleTypesSelected = false;
    private List<MoveSO> selectedMoves = new List<MoveSO>();
    public List<MoveSO> GetSelectedMoves()
    {
        return selectedMoves;
    }
    private List<GameObject> selectedObjs = new List<GameObject>();
    /// <summary>
    /// Trys to select a move if unselected, otherwise unselects move. 
    /// Checking and updating available player AP. 
    /// </summary>
    /// <param name="move"></param>
    /// <param name="SelectGameObject"></param>
    public void TrySelectMove(MoveSO move, GameObject SelectGameObject)
    {
        if (!selectedMoves.Contains(move))
        {
            if (CanSelectMove(move))
            {
                SelectGameObject.SetActive(true);
                pAPRemaining -= move.AP;
                selectedMoves.Add(move);
                OnMoveSelected?.Invoke(move);

                selectedObjs.Add(SelectGameObject);
                //If attack moves of different types are selected, reduces total AP consumed
                if (!multipleTypesSelected && GetMultipleTypesSelected(selectedMoves))
                {
                    multipleTypesSelected = true;
                    pAPRemaining++;
                }
                switch (move)
                {
                    case AttackSO a:
                        attackMoves+=1;

                        //analytics.RegisterAttackAttempt();
                        //analytics.RegisterMoveUsed(move.name);
                        break;
                    case DefendSO d:
                        defenseMoves+=1;

                        //analytics.RegisterDefendAttempt();
                        //analytics.RegisterMoveUsed(move.name);
                        break;
                }
            }
        }
        else
        {
            SelectGameObject.SetActive(false);

            pAPRemaining += move.AP;
            selectedMoves.Remove(move);
            OnMoveDeselected?.Invoke(move);

            selectedObjs.Remove(SelectGameObject);
            //Removes AP bonus if multiple types of attack moves are no longer selected
            if (multipleTypesSelected && !GetMultipleTypesSelected(selectedMoves))
            {
                multipleTypesSelected = false;
                pAPRemaining--;
            }
            switch (move)
            {
                case AttackSO a:
                    attackMoves+=-1;
                    break;
                case DefendSO d:
                    defenseMoves+=-1;
                    break;
            }
        }
        APChanged?.Invoke(pAPRemaining);
        //Debug.Log($"Remaining AP: {pAPRemaining}");

        
    }
    
    private bool CanSelectMove(MoveSO move)
    {
        bool limitMet = false;
        switch (move)
        {
            case AttackSO a:
                limitMet = attackMoves == maxAttackMoves;
                break;
            case DefendSO d:
                limitMet = defenseMoves == maxDefendMoves;
                break;
        }
    
        int bonusAP = !multipleTypesSelected && GetMultipleTypesSelected(selectedMoves, move) ? 1 : 0;
        return move.AP <= (pAPRemaining + bonusAP) && !limitMet; 
    }
    public bool CanAffordMoves()
    {
        return pAPRemaining >= 0;

    }
    public void ResetMoveSelection()
    {
        DeselectAllObjs();
        selectedMoves = new List<MoveSO>();

        defenseMoves = 0;
        attackMoves = 0;
        multipleTypesSelected = false;

        GetComponent<APBarUI>().Setup(this);
        pAPRemaining = GameManager.I.pC.actionPoints;
        APChanged?.Invoke(pAPRemaining);
    }

    private void DeselectAllObjs()
    {
        if (selectedObjs == null)
        {
            return;
        }
        foreach(GameObject selectedObj in selectedObjs)
        {
            selectedObj.SetActive(false);
        }
        selectedObjs.Clear();
        if (multipleTypesSelected)
        {
            multipleTypesSelected = false;
            pAPRemaining--;
        }

    }

    private bool GetMultipleTypesSelected(List<MoveSO> selectedMoves, MoveSO newMove = null)
    {
        if (selectedMoves.Count < 2) return false;

        //Adds new move if check is done before it is added to the selected moves list
        List<MoveSO> selectedMovesCopy = new List<MoveSO>(selectedMoves);
        if (newMove != null) selectedMovesCopy.Add(newMove);

        //Excludes defensive moves as these are not considered for combo
        List<AttackSO> selectedASOs = selectedMovesCopy.OfType<AttackSO>().ToList();
        MoveType firstType = selectedASOs.First().moveType;

        //there is more than one type of attack move selected
        if (selectedASOs.Any(a => a.moveType != firstType)) return true;
        
        return false;
    }
    #endregion
}