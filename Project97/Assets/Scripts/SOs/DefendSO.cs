using UnityEngine;
[CreateAssetMenu(menuName = "ScriptableObjects/Defend")]

public class DefendSO : MoveSO 
{
    public float damageReductionMultiplier = 0f; //0f damage reduction is assumed to be a block
    public bool deflect = false;
    public bool block = false;
    public bool duck = false;
    public bool guard = false;
    public float dodgeBonusPercent = 0f;
    private Scale heightTwo = Scale.None;

    public void setHeightTwo(Scale heightTwo)
    {
        this.heightTwo = heightTwo;
    }

    public Scale getHeightTwo()
    {
        return this.heightTwo;
    }

    //Block, guard, deflect
}
