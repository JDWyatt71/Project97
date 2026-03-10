using System;
using System.Collections.Generic;
using UnityEngine;

public class SelectMoveUI : MonoBehaviour
{
    public static SelectMoveUI I {private set; get;}
    void Awake()
    {
        I = this;
    }
    public void ResetSelectedMoves()
    {
        selectedMoves = new List<MoveSO>();
        selectedObjs = new List<GameObject>();  
    }
    #region Select player moves
    public void ResetMoveSelection()
    {
        DeselectAllObjs();

        defenseMoves = 0;
        attackMoves = 0;
    }

    private int pAPRemaining;
    public int GetCurrentAP()
    {
        return pAPRemaining;
    }
    public delegate void OnAPChanged(int current);
    public event OnAPChanged APChanged;
    public event Action<MoveSO> OnMoveSelected;
    public event Action<MoveSO> OnMoveDeselected;
    private int maxAttackMoves = 3;
    private int attackMoves;
    private int defenseMoves;
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
                limitMet = defenseMoves == 1;
                break;
        }
        return move.AP <= pAPRemaining && !limitMet; 
    }
    public bool CanAffordMoves()
    {
        return pAPRemaining >= 0;

    }
    public void SchedulePlayerMoves(Character c)
    {
        pAPRemaining = c.actionPoints;
        selectedMoves = new List<MoveSO>();
        GetComponent<APBarUI>().Setup(this);
    }
    #endregion
}