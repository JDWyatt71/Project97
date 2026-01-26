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
    public int AP {private set; get;}

    void Awake()
    {
        Setup();
        
    }
    private void Setup()
    {
        healthSystem = GetComponent<HealthSystem>();
        SetupMoves(AssetsDatabase.I.aMoves, AssetsDatabase.I.dMoves);
        AP = 4;
    }
    private void SetupMoves(List<AttackSO> initialAMoves, List<DefendSO> initialDMoves)
    {
        aMoves = new List<AttackSO>(initialAMoves);
        dMoves = new List<DefendSO>(initialDMoves);

    }
}
