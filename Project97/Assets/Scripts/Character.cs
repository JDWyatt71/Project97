using System.Collections.Generic;
using System.Linq;
using UnityEngine;
[RequireComponent(typeof(HealthSystem))]
public class Character : MonoBehaviour
{
    public HealthSystem healthSystem {private set; get;}
    private List<AttackSO> aMoves; //Attack moves pool
    public List<AttackSO> GetAMoves()
    {
        return aMoves;
    }
    private List<DefendSO> dMoves; //Defensive moves pool

    public List<DefendSO> GetDMoves()
    {
        return dMoves;
    }
    public List<MoveSO> GetAllMoves()
    {
        List<MoveSO> allMoves = new List<MoveSO>(aMoves);
        allMoves.AddRange(dMoves);
        return allMoves;
    }
    public int actionPoints {private set; get;}
    public int hitPoints;
    public int attack;
    public float accuracy;
    public float evasion;
   

    private List<Effect> effects = new List<Effect>();
    public void AddEffect(Effect effect)
    {
        effects.Add(effect);
    }
    public void Setup(CharacterSO characterSO = default(CharacterSO))
    {
        healthSystem = GetComponent<HealthSystem>();
        SetupMoves(AssetsDatabase.I.aMoves, AssetsDatabase.I.dMoves);
        actionPoints = characterSO.actionPoints;
        aMoves = new List<AttackSO>(characterSO.aMoves); //Copy the character's starting moves to this character
        dMoves = new List<DefendSO>(characterSO.dMoves);
    }
    private void SetupMoves(List<AttackSO> initialAMoves, List<DefendSO> initialDMoves)
    {
        aMoves = new List<AttackSO>(initialAMoves);
        dMoves = new List<DefendSO>(initialDMoves);

    }
}
