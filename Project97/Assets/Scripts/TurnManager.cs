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
    public event Action RoundComplete;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Update()
    {
        //Temporary for testing, to end player move selection
        if (Input.GetKeyDown(KeyCode.Space))
        {
            submittedMoves = true;
            ResetMoveSelection();
        }


    }

    private void ResetMoveSelection()
    {
        DeselectAllObjs();
        defenseMoves = 0;
        attackMoves = 0;
    }

    private IEnumerator Turns(Character pCharacter, Character cCharacter)
    {
        running = true;
        movesUIScreen.DisplayMoves(pCharacter.GetAllMoves());

        while(running){
            Debug.Log("Turn start");
            yield return StartCoroutine(Turn(pCharacter, cCharacter)); //Waits for sub coroutine to finish before continuing to next turn.
        }
        RoundComplete?.Invoke();
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
        List<MoveSO> pMoves = new List<MoveSO>(selectedMoves); //Shallow copy of list
        //Extracts AttackSO and single DefendSO from pMoves
        List<AttackSO> pAMoves = pMoves.OfType<AttackSO>().ToList();
        DefendSO pDMove = pMoves.OfType<DefendSO>().FirstOrDefault();

        List<MoveSO> cMoves = ScheduleRandomMoves(cCharacter);
        List<AttackSO> cAMoves = cMoves.OfType<AttackSO>().ToList();
        DefendSO cDMove = cMoves.OfType<DefendSO>().FirstOrDefault();
        
        Debug.Log($"Player moves chosen: {pMoves.Count}, Computer moves chosen: {cMoves.Count}");

        yield return StartCoroutine(PerformMoves(pAMoves, cAMoves, pDMove, cDMove, pCharacter, cCharacter));
        Debug.Log("Turn end");
    }
    /// <summary>
    /// Returns defense move first
    /// </summary>
    /// <param name="c"></param>
    /// <returns></returns>
    private List<MoveSO> ScheduleRandomMoves(Character c)
    {
        System.Random rnd = new System.Random();
        List<MoveSO> attackMoves = c.GetAMoves().OrderBy(x => rnd.Next()).Take(3).ToList<MoveSO>();

        MoveSO defenceMove = c.GetDMoves().OrderBy(x => rnd.Next()).Take(1).ToList()[0];

        List<MoveSO> moves = attackMoves;
        moves.Insert(0, defenceMove);

        return moves;
    }

    private void SchedulePlayerMoves(Character c)
    {
        pAP = playerCharacter.AP;
        //Enable UI
        submittedMoves = false;
        selectedMoves = new List<MoveSO>();
        //Wait until done-condition?
        //Get

    }
    private IEnumerator PerformMoves(List<AttackSO> ms1, List<AttackSO> ms2, DefendSO d1, DefendSO d2, Character c1, Character c2)
    {
        for(int i = 0; i < ms1.Count; i++)
        {
            if(!running) yield break;
            PerformMovePair(ms1[i], d2, c1, c2);
            
            yield return new WaitForSeconds(moveDelay);

            PerformMovePair(ms2[i], d1, c2, c1);

            yield return new WaitForSeconds(moveDelay);

        }
    }

    private void PerformMovePair(AttackSO a, DefendSO d, Character attacker, Character target)
    {
        PerformAttack(attacker, target, a, d);

        string aS = $"Attack on {target.name}: {a.name}, Damage: {a.damage}";
          

        string dS = $"Defend: {d.name}, Damage Reduction percentage: {d.damageReductionPercentage}";

        Debug.Log($"Move Details:\n{aS}\n{dS}\nTarget Health: {target.healthSystem.GetHealth()}, Defender health: {attacker.healthSystem.GetHealth()}");

    }

    private void PerformAttack(Character attacker, Character target, AttackSO attackSO, DefendSO defendSO = null)
    {
        if(defendSO == null)
        {
            defendSO = AssetsDatabase.I.defaultDefendSO;
        }
        //player attacking damage
        //Base damage of move
        //Successful Block
        int totalDamage;
        if(defendSO.damageReductionPercentage == 0f && attackSO.height == defendSO.height) //Here I assume 0 damage reduction implies block defense type
        {
            totalDamage = 0;
        }
        else
        {
            //Calculate damage as non-zero damage inflicted on a character
            totalDamage = totalDam(calculateInitialDamage(GetDamage(attackSO.damage), attacker.attack), 1-defendSO.damageReductionPercentage);

        }
        if (defendSO.deflect)
        {
            if (attacker.healthSystem.TakeDamage(totalDamage)) //Add Calculation on totalDamage
            {
                running = false;
            }
        }
        else
        {
            if (target.healthSystem.TakeDamage(totalDamage))
            {
                running = false;
            }
        }
        
    }
    
    #region Damage Calculation
    private int GetDamage(Damage damage) //Not implemented completely
    {
        switch (damage)
        {
            case(Damage.Low):
                return 5;
            case(Damage.Medium):
                return 6;
            case(Damage.High):
                return 7;
        }
        return 0;
    }
    private float calculateAttackMultiplier(float x)
    {
         return ( 1/1960 )*x*x + ( 5/196 )*x + 34/49;
    }
    private float calculateInitialDamage(int basedam, float attack)
    {
        return basedam * calculateAttackMultiplier(attack);
    }
    //(where grdred is 1 or 0.6 or 0)
    private int totalDam(float initdam, float multiplier)
    {
        return (int)Mathf.Round( multiplier * ( initdam +  UnityEngine.Random.Range( -0.15f*initdam, 0.15f*initdam ) ));

    }
    #endregion
    #region Select player moves
    private int pAP;
    private bool submittedMoves;
    private int maxAttackMoves = 3;
    private int attackMoves;
    private int defenseMoves;
    private List<MoveSO> selectedMoves = new List<MoveSO>();
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
                pAP -= move.AP;
                selectedMoves.Add(move);

                selectedObjs.Add(SelectGameObject);
                switch (move)
                {
                    case AttackSO a:
                        attackMoves+=1;
                        break;
                    case DefendSO d:
                        defenseMoves+=1;
                        break;
                }
            }
        }
        else
        {
            SelectGameObject.SetActive(false);
            pAP += move.AP;
            selectedMoves.Remove(move);
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
        return move.AP <= pAP && !limitMet; 
    }
    #endregion
}
