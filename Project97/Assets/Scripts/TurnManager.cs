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

    public void Setup(Character pCharacter, Character cCharacter)
    {
        I = this;
        movesUIScreen = GetComponent<MovesUIScreen>();
        this.playerCharacter = pCharacter;
        StartFight(cCharacter);
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

        StartCoroutine(Turns(playerCharacter, computerCharacter));

        

        selectedMoves = new List<MoveSO>();
        selectedObjs = new List<GameObject>();   
    }
    public static TurnManager I;
    
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
            if (CanAffordMoves())
            {
                submittedMoves = true;
                ResetMoveSelection();
            }
            
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
        computerCharacter.DoEffects();
        playerCharacter.DoEffects();
    }

    private IEnumerator Turn(Character pCharacter, Character cCharacter)
    {
        analytics.RegisterTurn();
        //currentFight.Turns++; to keep track of turns.

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

    private void SchedulePlayerMoves(Character c)
    {
        pAPRemaining = playerCharacter.actionPoints;
        GetComponent<APBarUI>().Setup(this);

        //Enable UI
        //apSlider.maxValue = playerCharacter.actionPoints;

        submittedMoves = false;
        selectedMoves = new List<MoveSO>();
        //Wait until done-condition?
        //Get

    }
    private IEnumerator PerformMoves(List<AttackSO> ms1, List<AttackSO> ms2, DefendSO d1, DefendSO d2, Character c1, Character c2)
    {
        int maxMoves = Mathf.Max(ms1.Count, ms2.Count);

        for(int i = 0; i < maxMoves; i++)
        {
            if(!running) yield break;
            if (i < ms1.Count){
                PerformMovePair(ms1[i], d2, c1, c2, c1.name);
                Debug.Log($"{c1.name}'s Health: {c1.healthSystem.GetHealth()}, {c2.name}'s Health: {c2.healthSystem.GetHealth()}");

                
                yield return new WaitForSeconds(moveDelay);
            }


            if(!running) yield break;
            if (i < ms2.Count)
            {
                PerformMovePair(ms2[i], d1, c2, c1, c2.name);
                Debug.Log($"{c1.name}'s Health: {c1.healthSystem.GetHealth()}, {c2.name}'s Health: {c2.healthSystem.GetHealth()}");


                yield return new WaitForSeconds(moveDelay);
            }

        }
    }

    private void PerformMovePair(AttackSO a, DefendSO d, Character attacker, Character target, string turnName)
    {
        string status = PerformAttack(attacker, target, a, d);

        /*string aS = $"Attack on {target.name}: {a.name}, Damage: {a.damage}";*/

        d ??= AssetsDatabase.I?.defaultDefendSO;

        if (d == null)
        {
            Debug.Log("No DefendSo avaliable");
        }
        //Damage displayed here is approximate, and doesn't factor randomness or defense reduction percentage like TotalDam() calculated.
        string aS = $"Attack: {a.name}, Damage: {a.damage} ≈ {CalculateInitialDamage(GetDamage(a.damage), attacker.attack)}"; 
        string dS;
        if(d != null){
            dS = $"Defend: {d.name}";
        }
        else
        {
            dS = "No defense";
        }

        Debug.Log($"{turnName}'s Turn:\n{aS}\n{dS}\nAttack {status}");

    }
    /// <summary>
    /// Returns a string of what happened with the move: dodge, block, hit, deflect
    /// </summary>
    /// <param name="attacker"></param>
    /// <param name="target"></param>
    /// <param name="attackSO"></param>
    /// <param name="defendSO"></param>
    /// <returns></returns>
    private string PerformAttack(Character attacker, Character target, AttackSO attackSO, DefendSO defendSO = null)
    {
        string moveName = attackSO.name;
        GameEvents.RaiseMoveUsed(moveName, attacker.ToString());
        analytics.RegisterAttackAttempt();

        if (defendSO == null)
        {
            defendSO = AssetsDatabase.I.defaultDefendSO;
        }
        //Implement attacker.accuracy and target.evasion
        Debug.Log(defendSO);
        Debug.Log(AssetsDatabase.I);
        Debug.Log(AssetsDatabase.I?.defaultDefendSO);
        float moveAccuracy = CalculateMoveAccuracy(attackSO.accuracy, attacker.accuracy, target.evasion, defendSO.dodgeBonusPercent);
        //Debug.Log($"Move accuracy: {moveAccuracy}");
        if (!RandomEvent(moveAccuracy))
        {
            return "dodged"; //So don't do any damage.
        }
        //player attacking damage
        //Base damage of move
        //Successful Block
        int totalDamage;
        
        //Calculate damage as non-zero damage inflicted on a character
        int basedam = GetDamage(attackSO.damage);
        float initialDamage = CalculateInitialDamage(basedam, attacker.attack);
        string guarded = "";
        if(attackSO.height == defendSO.height){
            //Guard
            totalDamage = TotalDam(initialDamage, 1-defendSO.damageReductionMultiplier);
            guarded = " and guarded";
        }
        else
        {
            //No guard
            totalDamage = TotalDam(initialDamage, 1);

        }
        //Debug.Log($"basedam: {basedam}, initialDamage: {initialDamage}, total damage: {totalDamage}");
        
        if(defendSO.block && attackSO.height == defendSO.height && attackSO.moveType != MoveType.Grapple) 
        {
            totalDamage = 0;

            analytics.RegisterDefendSuccess();
            return "blocked";
        }
        if (defendSO.deflect && attackSO.moveType != MoveType.Grapple)
        {
            if (attacker.healthSystem.TakeDamage(totalDamage)) //Add Calculation on totalDamage
            {
                running = false;
            }
            ApplyEffects(attacker, attackSO);

            analytics.RegisterDefendSuccess();
            return "deflected";
        }
        else
        {
            if (target.healthSystem.TakeDamage(totalDamage))
            {
                running = false;
            }
            ApplyEffects(target, attackSO);
            analytics.RegisterAttackSuccess();
            return "hit" + guarded;

        }
        /*if ((defendSO.deflect && attacker.healthSystem.TakeDamage(totalDamage)) || target.healthSystem.TakeDamage(totalDamage)) //Add Calculation on totalDamage
        {
            running = false;
        }*/

    }
    #region  Effects

    private void ApplyEffects(Character character, AttackSO attackSO)
    {
        foreach(EffectChance eC in attackSO.effects)
        {
            if (RandomEvent(GetEffectChance(eC.chance)))
            {
                character.AddEffect(eC.effect);

                //string effectName = eC.effect.name; // once we figure out the effects.
                //analytics.RegisterEffectApplied(effectName);
            }
        }
    }

    private static readonly float[] chances = { 0.2f, 0.3f, 0.4f, 0.55f, 0.7f, 1f };

    private float GetEffectChance(Scale chance)
    {
        return chances[(int)chance]; //Only works as Scale is ordered.
    }
    #endregion

    private bool RandomEvent(float moveAccuracy)
    {
        return UnityEngine.Random.value < moveAccuracy;
    }

    private float CalculateAccuracyEvasionMultiplier(float attackerAccuracy, float defenderEvasion)
    {
        return (attackerAccuracy-defenderEvasion) / 2f;
    }
    private float CalculateMoveAccuracy(Accuracy accuracy, float attackerAccuracy, float defenderEvasion, float dodgeBonusPercent)
    {
        float baseAccuracy = GetBaseAccuracy(accuracy) * 100f;
        return Mathf.Round(baseAccuracy + CalculateAccuracyEvasionMultiplier(attackerAccuracy, defenderEvasion) - dodgeBonusPercent) / 100f;
    }
    private static readonly float[] accuracyValues = { 0.4f, 0.65f, 0.8f, 0.88f, 0.95f};

    private float GetBaseAccuracy(Accuracy accuracy) //Not implemented completely, add actual accuracy values
    {
        return accuracyValues[(int)accuracy];

    }

    #region Damage Calculation
    private float damageMultiplier = 1f;
    private static readonly float[] damageValues = { 3f, 5f, 7f, 8.5f, 10f };

    private int GetDamage(Scale damage)
    {
        return Mathf.RoundToInt(damageValues[(int)damage] * damageMultiplier);
    }
    
    private float CalculateAttackMultiplier(int x)
    {
        float y = (float)x;
        float ans = (y*y / 1960f) + (5f / 196f * y) + 34f/49f;
        return ans;
    }
    private float CalculateInitialDamage(int basedam, int attack)
    {
        float attackMultiplier = CalculateAttackMultiplier(attack);
        return basedam * attackMultiplier;
    }
    //(where grdred is 1 or 0.6 or 0)
    private int TotalDam(float initdam, float multiplier)
    {
        float rand = UnityEngine.Random.Range( -0.15f*initdam, 0.15f*initdam );
        float ans = multiplier * ( initdam + rand  );
        int roundedAns = (int)Mathf.Round( ans );
        return roundedAns;

    }
    #endregion
    #region Select player moves
    private int pAPRemaining;
    public int GetCurrentAP()
    {
        return pAPRemaining;
    }
    public int GetMaxAP()
    {
        return playerCharacter.actionPoints;
    }
    public delegate void OnAPChanged(int current, int max);
    public event OnAPChanged APChanged;
    public event Action<MoveSO> OnMoveSelected;
    public event Action<MoveSO> OnMoveDeselected;
    private bool submittedMoves;
    private int maxAttackMoves = 3;
    private int attackMoves;
    private int defenseMoves;
    private List<MoveSO> selectedMoves = new List<MoveSO>();
    private List<GameObject> selectedObjs = new List<GameObject>();
    private List<MoveSO> selectedMoves;
    private List<GameObject> selectedObjs; 
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

                        analytics.RegisterAttackAttempt();
                        analytics.RegisterMoveUsed(move.name);
                        break;
                    case DefendSO d:
                        defenseMoves+=1;

                        analytics.RegisterDefendAttempt();
                        analytics.RegisterMoveUsed(move.name);
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
        APChanged?.Invoke(pAPRemaining, playerCharacter.actionPoints);
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
    private bool CanAffordMoves()
    {
        return pAPRemaining >= 0;

    }
    #endregion
}
