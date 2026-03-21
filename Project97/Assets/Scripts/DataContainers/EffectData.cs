public class EffectData
{
    public int duration;
    public Scale height;

    public EffectData(int duration, Scale height = Scale.None)
    {
        this.duration = duration;
        this.height = height;
    }
}