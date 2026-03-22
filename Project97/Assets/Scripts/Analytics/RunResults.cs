//[Serializable]
public class RunResult
{
    public string RunId;
    public bool Successful;
    public string Difficulty;

    public float RunStartTime;
    public float RunEndTime;
    public int LevelFinish;

    public int AttackAttempts;
    public int AttackSuccess;

    public int DefendAttempts;
    public int DefendSuccess;

    public string DeathCause;
    public int HpLeft;

    public string sessionID;
}

