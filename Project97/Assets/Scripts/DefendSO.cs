using UnityEngine;
[CreateAssetMenu(menuName = "ScriptableObjects/Defend")]

public class DefendSO : MoveSO 
{
    public float damageReductionMultiplier = 0f; //0f damage reduction is assumed to be a block
    public bool deflect = false;
    public bool block = false;
    public float dodgeBonusPercent = 0f;
    //Block, guard, deflect
}
