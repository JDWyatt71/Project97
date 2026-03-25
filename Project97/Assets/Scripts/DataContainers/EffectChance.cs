[System.Serializable]
public class EffectChance
{
    public Effect effect;
    public Scale chance;
    public override string ToString()
    {
        return $"{effect}: {chance}";
    }
}