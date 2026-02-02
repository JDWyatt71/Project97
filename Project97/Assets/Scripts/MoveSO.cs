using System.Collections.Generic;
using UnityEngine;

public abstract class MoveSO : ScriptableObject
{
    public Sprite sprite;
    public int AP = 1;
    public Scale height;
    public List<EffectChance> effects;
}
