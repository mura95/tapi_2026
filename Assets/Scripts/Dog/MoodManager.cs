using UnityEngine;
using BehaviorDesigner.Runtime;
using UnityEngine.Events;

public class MoodManager
{
    private int moodValue;
    private const int MinValue = 0;
    private const int MaxValue = 100;

    public int Value => moodValue;

    public MoodLevel CurrentMoodLevel
    {
        get
        {
            if (moodValue <= 33) 
            {
                return MoodLevel.Low;
            }

            if (moodValue <= 66) 
            {
                return MoodLevel.Medium;
            }

            return MoodLevel.High;
        }
    }

    public void Increase(int amount)
    {
        moodValue = Mathf.Clamp(moodValue + amount, MinValue, MaxValue);
    }

    public void Decrease(int amount)
    {
        moodValue = Mathf.Clamp(moodValue - amount, MinValue, MaxValue);
    }

    public void ApplySnack(SnackType snack)
    {
        switch (snack)
        {
            case SnackType.Biscuit:
                Increase(10);
                break;
            case SnackType.Meat:
                Increase(20);
                break;
        }
    }
}

// dummy
public enum SnackType
{
    Biscuit,
    Meat
}