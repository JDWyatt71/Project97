using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.VisualScripting;
[RequireComponent(typeof(MovesUIScreen))]
public class TurnManager : MonoBehaviour
{
    public void Setup(Character pCharacter, Character cCharacter)
    {
        I = this;
        movesUIScreen = GetComponent<MovesUIScreen>();
        this.playerCharacter = pCharacter;
        this.computerCharacter = cCharacter;
        StartCoroutine(Turns(playerCharacter, computerCharacter));
    }
    public static TurnManager I;
    
    private Character playerCharacter;
    private Character computerCharacter;

    private bool running;
    private float moveDelay = 1f;
    private MovesUIScreen movesUIScreen;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Update()
    {
        //Temporary for testing, to end player move selection
        if (Input.GetKeyDown(KeyCode.Space))
        {
            submittedMoves = true;
            DeselectAllObjs();
        }


    }
    private IEnumerator Turns(Character pCharacter, Character cCharacter)
    {
        running = true;
        movesUIScreen.DisplayMoves(pCharacter.GetAllMoves());

        while(running){
            Debug.Log("Turn start");
            yield return StartCoroutine(Turn(pCharacter, cCharacter)); //Waits for sub coroutine to finish before continuing to next turn.
        }
        Debug.Log("End game");
        if (playerCharacter == null)
        {
            Debug.Log("Computer wins");
        }
        else if (computerCharacter == null)
        {
            Debug.Log("Player wins");
        }

    }

    private IEnumerator Turn(Character pCharacter, Character cCharacter)
    {
        SchedulePlayerMoves(pCharacter);
        yield return new WaitUntil(() => submittedMoves);
        submittedMoves = false;

        //Get player moves from
        List<MoveSO> pMoves = selectedMoves.Where(kvp => kvp.Value).Select(kvp => kvp.Key).ToList();
        List<MoveSO> cMoves = ScheduleRandomMoves(cCharacter);
        Debug.Log($"Player moves chosen: {pMoves.Count}, Computer moves chosen: {cMoves.Count}");

        yield return StartCoroutine(PerformMoves(pMoves, cMoves, pCharacter, cCharacter));
        Debug.Log("Turn end");
    }

    private List<MoveSO> ScheduleRandomMoves(Character c)
    {
        System.Random rnd = new System.Random();
        List<MoveSO> attackMoves = c.GetAMoves().OrderBy(x => rnd.Next()).Take(3).ToList<MoveSO>();

        MoveSO defenceMove = c.GetDMoves().OrderBy(x => rnd.Next()).Take(1).ToList()[0];
        int rndI = rnd.Next(attackMoves.Count);

        List<MoveSO> moves = attackMoves;
        moves.Insert(rndI, defenceMove);

        return moves;
    }

    private void SchedulePlayerMoves(Character c)
    {
        pAP = playerCharacter.AP;
        //Enable UI
        submittedMoves = false;
        selectedMoves = c.GetAllMoves().ToDictionary(m => m, m => false);
        //Wait until done-condition?
        //Get

    }
    private IEnumerator PerformMoves(List<MoveSO> ms1, List<MoveSO> ms2, Character c1, Character c2)
    {
        for(int i = 0; i < ms1.Count; i++)
        {
            if(!running) yield break;
            yield return StartCoroutine(PerformMovePair(ms1[i], ms2[i], c1, c2));
            yield return new WaitForSeconds(moveDelay);

        }
    }

    private IEnumerator PerformMovePair(MoveSO m1, MoveSO m2, Character c1, Character c2)
    {
        switch (m1, m2)
        {
            case (AttackSO a1, AttackSO a2):
                PerformAttack(c2, a1);

                //In this case we don't allow dead character to attack back when both attacking moves on turn.
                if(!running) yield break; 

                yield return new WaitForSeconds(moveDelay);
                PerformAttack(c1, a2);
                break;

            case (DefendSO, DefendSO):
                //Nothing happens logically, only visuals.
                break;

            case (AttackSO a, DefendSO d):
                PerformAttack(c2, a, d);
                break;

            case (DefendSO d, AttackSO a):
                PerformAttack(c1, a, d);
                break;
            default:
                Debug.Log("Should not be output.");
                break;
        }
        //Temporary debug details for testing and no UI.
        string m1Info = m1 switch
        {
            AttackSO a => $"Attack: {a.name}, Damage: {a.damage}",
            DefendSO d => $"Defend: {d.name}, Damage Reduction: {d.damageReduction}",
        };

        string m2Info = m2 switch
        {
            AttackSO a => $"Attack: {a.name}, Damage: {a.damage}",
            DefendSO d => $"Defend: {d.name}, Damage Reduction: {d.damageReduction}",
        };

        Debug.Log($"Move Details:\nPlayer Move: {m1Info}\nEnemy Move: {m2Info}\nPlayer Health: {c1.healthSystem.GetHealth()}, Enemy health: {c2.healthSystem.GetHealth()}");

    }

    private void PerformAttack(Character target, AttackSO attackSO, DefendSO defenseSO = null)
    {
        int defense = defenseSO != null ? defenseSO.damageReduction : 0;

        int calculatedDamage = attackSO.damage - defense;
        if (target.healthSystem.TakeDamage(calculatedDamage))
        {
            running = false;
        }
    }
    #region Select player moves
    private int pAP;
    private bool submittedMoves;
    private Dictionary<MoveSO, bool> selectedMoves;
    private List<GameObject> selectedObjs = new List<GameObject>();
    
    /// <summary>
    /// Trys to select a move if unselected, otherwise unselects move. 
    /// Checking and updating available player AP. 
    /// </summary>
    /// <param name="move"></param>
    /// <param name="SelectGameObject"></param>
    public void TrySelectMove(MoveSO move, GameObject SelectGameObject)
    {
        if (!selectedMoves[move])
        {
            if (CanSelectMove(move))
            {
                SelectGameObject.SetActive(true);
                pAP -= move.AP;
                selectedMoves[move] = true;
                selectedObjs.Add(SelectGameObject);
            }
        }
        else
        {
            SelectGameObject.SetActive(false);
            pAP += move.AP;
            selectedMoves[move] = false;
            selectedObjs.Remove(SelectGameObject);


        }
        
    }
    private void DeselectAllObjs()
    {
        foreach(GameObject selectedObj in selectedObjs)
        {
            selectedObj.SetActive(false);

        }
    }
    private bool CanSelectMove(MoveSO move)
    {
        return move.AP <= pAP; 
    }
    #endregion
}
