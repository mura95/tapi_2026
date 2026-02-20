using System;

[Flags]
public enum EmotionState
{
    None = 0,
    Hungry = 1 << 0,
    Sleeping = 1 << 1,
    Crying = 1 << 2,
    Happy = 1 << 3,
    Angry = 1 << 4,
}
