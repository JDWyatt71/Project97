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
        int cAP = c.actionPoints;
        List<MoveSO> moves = new List<MoveSO>();
        int movesAP = 999999999;

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
          

        string dS = $"Defend: {d.name}, Damage Reduction multiplier: {d.damageReductionMultiplier}";

        Debug.Log($"Move Details:\n{aS}\n{dS}\nTarget Health: {target.healthSystem.GetHealth()}, Defender health: {attacker.healthSystem.GetHealth()}");

    }

    private void PerformAttack(Character attacker, Character target, AttackSO attackSO, DefendSO defendSO = null)
    {
        if(defendSO == null)
        {
            defendSO = AssetsDatabase.I.defaultDefendSO;
        }
        //Implement attacker.accuracy and target.evasion
        float moveAccuracy = CalculateMoveAccuracy(attackSO.accuracy, attacker.accuracy, target.evasion, defendSO.dodgeBonusPercent);
        Debug.Log($"Move accuracy: {moveAccuracy}");
        if (!RandomEvent(moveAccuracy))
        {
            Debug.Log("Dodge"); //So don't do any damage.
            return;
        }
        //player attacking damage
        //Base damage of move
        //Successful Block
        int totalDamage;
        
        //Calculate damage as non-zero damage inflicted on a character
        int basedam = GetDamage(attackSO.damage);
        float initialDamage = CalculateInitialDamage(basedam, attacker.attack);
        totalDamage = TotalDam(initialDamage, 1-defendSO.damageReductionMultiplier);
        //Debug.Log($"basedam: {basedam}, initialDamage: {initialDamage}, total damage: {totalDamage}");
        
        if(defendSO.block && attackSO.height == defendSO.height) 
        {
            Debug.Log("Successful block");
            totalDamage = 0;
            return;
        }
        if (defendSO.deflect)
        {
            if (attacker.healthSystem.TakeDamage(totalDamage)) //Add Calculation on totalDamage
            {
                running = false;
            }
            ApplyEffects(attacker, attackSO);
        }
        else
        {
            if (target.healthSystem.TakeDamage(totalDamage))
            {
                running = false;
            }
            ApplyEffects(target, attackSO);

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
            }
        }
    }

    private static readonly float[] chances = { 0.2f, 0.3f, 0.4f, 0.55f, 0.7f };

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
    private float CalculateMoveAccuracy(Scale accuracy, float attackerAccuracy, float defenderEvasion, float dodgeBonusPercent)
    {
        float dodgeBonus = dodgeBonusPercent / 100f;
        float baseAccuracy = GetBaseAccuracy(accuracy);
        return baseAccuracy + (CalculateAccuracyEvasionMultiplier(attackerAccuracy, defenderEvasion) / 2) - dodgeBonus;
    }
    private float GetBaseAccuracy(Scale accuracy) //Not implemented completely, add actual accuracy values
    {
        float d = 0f;
        switch (accuracy)
        {
            case Scale.Low:
                d = 0.5f; //Placeholders
                break;
            case Scale.Medium:
                d = 0.7f;
                break;
            case Scale.High:
                d = 0.9f;
                break;
        }
        return d;
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
                pAPRemaining -= move.AP;
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
            pAPRemaining += move.AP;
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
        APChanged?.Invoke(pAPRemaining, playerCharacter.actionPoints);
        Debug.Log($"Remaining AP: {pAPRemaining}");

        
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
        return move.AP <= pAPRemaining && !limitMet; 
    }
    private bool CanAffordMoves()
    {
        return pAPRemaining >= 0;

    }
    #endregion
}
