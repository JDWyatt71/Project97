using UnityEngine;

public class EffectData
{
    public int duration;
    public Scale height;
    public Sprite sprite {private set; get;}

    public EffectData(int duration, Sprite sprite, Scale height = Scale.None)
    {
        this.duration = duration;
        this.height = height;
        this.sprite = sprite;
    }
}