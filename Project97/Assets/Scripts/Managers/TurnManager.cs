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
    private float fightStartTime;
    /*private FightResult currentFight;*/

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
        
        fightStartTime = Time.time;
        /*currentFight = new FightResult {
            FightId = fightID,
            status = new Dictionary<string, int>(),
            moves = new Dictionary<string, int>()};*/

        analytics = new FightAnalyticsTracker();
        analytics.StartFight(fightID);
        Debug.Log("Fight start tracker");

        GameEvents.RaiseFightStarted(fightID, analytics.fightStartTime, GameManager.I.CurrentSessionId);

        cM = new CombatManager(analytics);
        playerCharacter.GetComponent<HealthSystem>().RunningIsFalse += RunningIsFalse;
        computerCharacter.GetComponent<HealthSystem>().RunningIsFalse += RunningIsFalse;

        SelectMoveUI.I.ResetMoveSelection();
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
    private bool turnsProcessing;
    void Update()
    {
        //Temporary for testing, to end player move selection
        if (!turnsProcessing && Input.GetKeyDown(KeyCode.Space))
        {
            AttemptStartTurn();
        }
    }

    public void AttemptStartTurn()
    {
        if (SelectMoveUI.I.CanAffordMoves())
        {
            submittedMoves = true;
        }
    }


    private IEnumerator Turns(Character pCharacter, Character cCharacter)
    {
        running = true;
        movesUIScreen.DisplayMoves(pCharacter.GetAllMoves());

        while(running){
            Debug.Log("Turn start");
            CombatEvents.RaiseTurnStart();
            DoEffects();
            yield return StartCoroutine(Turn(pCharacter, cCharacter)); //Waits for sub coroutine to finish before continuing to next turn.
        }

        /*currentFight.BattleTimeSeconds = Mathf.RoundToInt(Time.time - fightStartTime);*/
        int HpLeft = playerCharacter != null ? playerCharacter.healthSystem.GetHealth() : 0;

        bool playerDied = playerCharacter == null;
        float duration = Time.time - fightStartTime;
        FightResult result = analytics.EndFight(HpLeft);
        result.runID = GameManager.I.CurrentRunId;
        result.playerDied = playerDied;
        result.sessionId = GameManager.I.CurrentSessionId;
        result.BattleTimeSeconds = Mathf.RoundToInt(duration);
        Debug.Log("Raised Fight End Tracker");
        GameEvents.RaiseFightEnded(result);

        //Debug.Log("End game");
        if (playerCharacter == null)
        {
            RoundComplete?.Invoke(false);
            //telemetry for a player failing a level
            GameEvents.RaiseStageFail(fightID, GameManager.I.CurrentSessionId);
            Debug.Log("Computer wins");
        }
        else if (computerCharacter == null)
        {
            RoundComplete?.Invoke(true);
            //telemtry for a player completing a level
            GameEvents.RaiseStageComplete(fightID, GameManager.I.CurrentSessionId);
            Debug.Log("Player wins");
        }

        
    }

    private void DoEffects()
    {
        computerCharacter.DoEffects(playerCharacter.attack);
        playerCharacter.DoEffects(computerCharacter.attack);
    }

    private IEnumerator Turn(Character pCharacter, Character cCharacter)
    {
        turnsProcessing = false;
        analytics.RegisterTurn();
        //currentFight.Turns++; to keep track of turns.

        submittedMoves = pCharacter.actionPoints == 0; //Skips turn without waiting if have no AP

        yield return new WaitUntil(() => submittedMoves);
        submittedMoves = false;
        turnsProcessing = true;

        //Get player moves from
        List<MoveSO> pMoves = new List<MoveSO>(SelectMoveUI.I.GetSelectedMoves()); //Shallow copy of list
        TryRestAction(pCharacter, SelectMoveUI.I.GetCurrentAP());
        //Extracts AttackSO and single DefendSO from pMoves
        List<AttackSO> pAMoves = pMoves.OfType<AttackSO>().ToList();
        DefendSO pDMove = pMoves.OfType<DefendSO>().FirstOrDefault();

        List<MoveSO> cMoves = ScheduleRandomMoves(cCharacter);

        List<AttackSO> cAMoves = cMoves.OfType<AttackSO>().ToList();
        DefendSO cDMove = cMoves.OfType<DefendSO>().FirstOrDefault();
        
        Debug.Log($"Player moves chosen: {pMoves.Count}, Computer moves chosen: {cMoves.Count}");

        CombatEvents.AllMovesSelected(pDMove, cDMove);
        SelectMoveUI.I.ResetMoveSelection();

        yield return StartCoroutine(PerformMoves(pAMoves, cAMoves, pDMove, cDMove, pCharacter, cCharacter));
        Debug.Log("Turn end");
    }

    private void TryRestAction(Character c, int remainingAP)
    {
        if(remainingAP >= 1)
        {
            c.TryUseRestAction();
        }
    }

    /// <summary>
    /// Returns defense move first
    /// </summary>
    /// <param name="c"></param>
    /// <returns></returns>
    private List<MoveSO> ScheduleRandomMoves(Character c)
    {
        System.Random rnd = new System.Random();
        int remainingAP = c.actionPoints;
        List<MoveSO> moves = new List<MoveSO>();
        if (UC.RandomEvent(c.cSO.defendRate))
        {
            MoveSO defenceMove = UC.GetRandomDefendSO(c.cSO.defendChances, c.GetDMoves()); //It is assumed all defensive moves can be afforded always
            moves.Insert(0, defenceMove);
            remainingAP -= defenceMove.AP;
        }
        
        //Offensive
        //Copy attackChances locally, so can remove moves locally once selected or cannot afford the AP.
        List<AttackChance> attackChances = new List<AttackChance>(c.cSO.attackChances); //Shallow copy, so each AttackChance is reference of original
        
        //Add all CharacterSO attacking moves that aren't specified in attack chance as Neutral
        HashSet<AttackSO> allMoves = new HashSet<AttackSO>(attackChances.Select(ac => ac.attackSO));
        foreach(AttackSO attackSO in c.GetAMoves())
        {
            if (!allMoves.Contains(attackSO)) //O(1) lookup with Contains for a HashSet
            {
                attackChances.Add(new AttackChance(attackSO, MoveWeight.Neutral));
            }
        }

        bool noFavouredMoves = !attackChances.Any(a => a.weight == MoveWeight.Favoured);
        Dictionary<MoveWeight, float> categoryProbabilities = new Dictionary<MoveWeight, float>
        {
            {MoveWeight.Favoured, noFavouredMoves ? 0f : 0.5f},
            {MoveWeight.Rare, 0.15f},
            {MoveWeight.Neutral, noFavouredMoves ? 0.85f : 0.35f}
        };


        while(moves.Count < 4){
            attackChances.RemoveAll(item => item.attackSO.AP > remainingAP); //Remove all moves that cannot be afforded
            
            //If there are no moves in a category due to being unaffordable or already selected, set that category probability to 0.
            foreach (MoveWeight weight in System.Enum.GetValues(typeof(MoveWeight)))
            {
                if(!attackChances.Any(a => a.weight == weight)) categoryProbabilities[weight] = 0f;
            }

            //If there are no moves left in the attacking move pool, because none can be afforded, then break
            if (attackChances.Count == 0) break; 

            #region Move selection
            //E.g. There are 3 rare moves, total probability of picking a rare move is 0.15/3. Code implements in two stages: First category selected with 0.15 probability. Then 1/3 chance of specific rare move.
            //Get random category using category weights which is as initialised or 0 when no moves in that category
            MoveWeight moveWeight = UC.GetWeightedRandomItem(categoryProbabilities); 

            Dictionary<AttackSO, MoveWeight> dict = attackChances.ToDictionary(x => x.attackSO, x => x.weight); //Convert to dictionary
            List<AttackSO> selectedCategoryMoves = dict.Where(kvp => kvp.Value == moveWeight).Select(kvp => kvp.Key).ToList(); 
            
            AttackSO randomAMove = selectedCategoryMoves[UnityEngine.Random.Range(0, selectedCategoryMoves.Count)];
            MoveSO randomMove = (MoveSO)randomAMove;
            moves.Add(randomMove);
            remainingAP -= randomAMove.AP;
            #endregion

            //Remove the randomly selected move from 'attackChances'
            attackChances.RemoveAll(item => item.attackSO == randomAMove);            
        }
        TryRestAction(c, remainingAP);

        return moves;
        /*while(movesAP > cAP){
            List<MoveSO> attackMoves = c.GetAMoves().OrderBy(x => rnd.Next()).Take(3).ToList<MoveSO>();

            moves = new List<MoveSO>(attackMoves);

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
        }*/
        
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
