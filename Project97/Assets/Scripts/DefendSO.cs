using UnityEngine;
[CreateAssetMenu(menuName = "ScriptableObjects/Defend")]

public class DefendSO : MoveSO 
{
    public float damageReductionPercentage = 0f; //0f damage reduction is assumed to be a block
    public bool deflect = false;
    //Block, guard, deflect
}
