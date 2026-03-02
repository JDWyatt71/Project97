using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine.UI;
[RequireComponent(typeof(MovesUIScreen))]
public class TurnManager : MonoBehaviour
{
    private string fightID;
    /*private float fightStartTime;
    private FightResult currentFight;*/

    private FightAnalyticsTracker analytics;
    private bool submittedMoves;


    public void Setup(Character pCharacter, Character cCharacter)
    {
        I = this;
        movesUIScreen = GetComponent<MovesUIScreen>();
        this.playerCharacter = pCharacter;

        StartFight(cCharacter);
    }

    private void RunningIsFalse()
    {
        running = false;
    }

    /// <summary>
    /// Starts a fight with current player character and the given computer character.
    /// </summary>
    /// <param name="cCharacter"></param>
    public void StartFight(Character cCharacter)
    {
        this.computerCharacter = cCharacter;

        fightID = Guid.NewGuid().ToString();
        /*
        fightStartTime = Time.time;
        currentFight = new FightResult {
            FightId = fightID,
            status = new Dictionary<string, int>(),
            moves = new Dictionary<string, int>()};*/

        analytics = new FightAnalyticsTracker();
        analytics.StartFight(fightID);
        Debug.Log("Fight start tracker");

        GameEvents.RaiseFightStarted(fightID, analytics.fightStartTime);

        cM = new CombatManager(analytics);
        playerCharacter.GetComponent<HealthSystem>().RunningIsFalse += RunningIsFalse;
        computerCharacter.GetComponent<HealthSystem>().RunningIsFalse += RunningIsFalse;


        StartCoroutine(Turns(playerCharacter, computerCharacter));
         
    }
    public static TurnManager I;
    private CombatManager cM;
    private Character playerCharacter;
    private Character computerCharacter;

    private bool running;
    private float moveDelay = 2f;
    private MovesUIScreen movesUIScreen;
    public event Action<bool> RoundComplete; //If player won then call with True, otherwise if player has died we call with false.
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Update()
    {
        //Temporary for testing, to end player move selection
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (SelectMoveUI.I.CanAffordMoves())
            {
                submittedMoves = true;
                SelectMoveUI.I.ResetMoveSelection();
            }
        }
    }

    

    private IEnumerator Turns(Character pCharacter, Character cCharacter)
    {
        running = true;
        movesUIScreen.DisplayMoves(pCharacter.GetAllMoves());

        while(running){
            Debug.Log("Turn start");
            DoEffects();
            yield return StartCoroutine(Turn(pCharacter, cCharacter)); //Waits for sub coroutine to finish before continuing to next turn.
        }
        //Debug.Log("End game");
        if (playerCharacter == null)
        {
            RoundComplete?.Invoke(false);
            Debug.Log("Computer wins");
        }
        else if (computerCharacter == null)
        {
            RoundComplete?.Invoke(true);
            Debug.Log("Player wins");
        }

        /*currentFight.BattleTimeSeconds = Mathf.RoundToInt(Time.time - fightStartTime);*/
        int HpLeft = playerCharacter != null ? playerCharacter.healthSystem.GetHealth() : 0;

        FightResult result = analytics.EndFight(HpLeft);
        Debug.Log("Raised Fight End Tracker");
        GameEvents.RaiseFightEnded(result);
    }

    private void DoEffects()
    {
        computerCharacter.DoEffects(playerCharacter.attack);
        playerCharacter.DoEffects(computerCharacter.attack);
    }

    private IEnumerator Turn(Character pCharacter, Character cCharacter)
    {
        analytics.RegisterTurn();
        //currentFight.Turns++; to keep track of turns.

        SelectMoveUI.I.SchedulePlayerMoves(pCharacter);

        submittedMoves = pCharacter.actionPoints == 0; //Skips turn without waiting if have no AP

        yield return new WaitUntil(() => submittedMoves);
        submittedMoves = false;

        //Get player moves from
        List<MoveSO> pMoves = new List<MoveSO>(SelectMoveUI.I.GetSelectedMoves()); //Shallow copy of list
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
        int cAP = c.actionPoints;
        List<MoveSO> moves = new List<MoveSO>();
        int movesAP = 999999999;
        int i = 1;
        int maxIterations = 1000;

        while(movesAP > cAP){
            List<MoveSO> attackMoves = c.GetAMoves().OrderBy(x => rnd.Next()).Take(3).ToList<MoveSO>();

            MoveSO defenceMove = c.GetDMoves().OrderBy(x => rnd.Next()).Take(1).ToList()[0];

            moves = new List<MoveSO>(attackMoves);
            moves.Insert(0, defenceMove);

            //Count AP of all moves
            movesAP = 0;
            foreach(MoveSO moveSO in moves)
            {
                movesAP += moveSO.AP;
            }
            if (i > maxIterations)
            {
                Debug.LogError($"ScheduleRandomMoves exceeded {maxIterations} iterations for character {c.name}");
                break; // Use current moves, even though can't afford
            }
            i+=1;
        }
        return moves;
    }

    private IEnumerator PerformMoves(List<AttackSO> ms1, List<AttackSO> ms2, DefendSO d1, DefendSO d2, Character c1, Character c2)
    {
        int maxMoves = Mathf.Max(ms1.Count, ms2.Count);

        for(int i = 0; i < maxMoves; i++)
        {
            if(!running) yield break;
            if (i < ms1.Count)
            {
                //Note: RunningIsFalse event may be triggered in CM which is listened to in this script (running = false; executed).
                cM.PerformMovePair(ms1[i], d2, c1, c2, c1.name); 
                OutputHealth(c1, c2);

                yield return new WaitForSeconds(moveDelay);
            }


            if (!running) yield break;
            if (i < ms2.Count)
            {
                cM.PerformMovePair(ms2[i], d1, c2, c1, c2.name);
                OutputHealth(c1, c2);

                yield return new WaitForSeconds(moveDelay);
            }

        }
    }

    private static void OutputHealth(Character c1, Character c2)
    {
        Debug.Log($"{c1.name}'s Health: {c1.healthSystem.GetHealth()}, {c2.name}'s Health: {c2.healthSystem.GetHealth()}");
    }
    
    
}
