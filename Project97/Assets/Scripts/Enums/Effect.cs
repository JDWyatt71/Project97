using System.Collections.Generic;

public enum Effect{ //Stores the duration in turns of each effect
    AdrenalineRush,
    Enraged,
    Blindness,
    Slow,
    Bind, 
    Wind ,
    Prone,
    BrokenBones,
    Bleed,
}
public static class EffectDefaults
{
    public static readonly Dictionary<Effect, int> Durations = new()
    {
        {Effect.AdrenalineRush, 2},
        {Effect.Enraged, 2},
        {Effect.Blindness, 2},
        {Effect.Slow, 3},
        {Effect.Bind, 4}, //Special case of 2-4, is guaranteed 2 turns then chance of removing after 2nd and 3rd turns.
        {Effect.Wind, 1},
        {Effect.Prone, 1},
        {Effect.BrokenBones, -1}, //-1 signifies unlimited turns
        {Effect.Bleed, -1}
    };
}