public static class UC //Utils Class
{
    public static bool RandomEvent(float moveAccuracy)
    {
        return UnityEngine.Random.value < moveAccuracy;
    }
    public static bool RandomEventPercentage(float moveAccuracy)
    {
        return RandomEvent(moveAccuracy / 100f);
    }
}