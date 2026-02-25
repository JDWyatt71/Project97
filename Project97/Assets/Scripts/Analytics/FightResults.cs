using System.Collections.Generic;

//[Serializable]
public class FightResult
{
    public string FightId;

    public int BattleTimeSeconds;
    public int Turns;

    public int AttackAttempts;
    public int AttackSuccess;

    public int DefendAttempts;
    public int DefendSuccess;

    public int HpLeft;

    public Dictionary<string, int> status;
    public Dictionary<string, int> moves;
}
