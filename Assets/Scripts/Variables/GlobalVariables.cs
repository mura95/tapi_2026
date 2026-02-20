using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GlobalVariables
{
    // ユーザー名入力中かどうか
    public static bool IsInputUserName = false;
    // Hunger System
    public static HungerState CurrentHungerState = HungerState.Hungry;

    //飽きの設定数値
    public static int AttentionCount = 0;
    public static bool isMoving = false;

    // Pet State
    private static PetState currentState = PetState.idle;
    public static PetState CurrentState
    {
        get => currentState;
        set
        {
            currentState = value;
            CurrentStateIndex = (int)value;
        }
    }
    public static int CurrentStateIndex { get; private set; }

    // For compatibility with existing code
    public static int CurrentHungerStateIndex => (int)CurrentHungerState;
}

public enum HungerState
{
    Full,
    MediumHigh,
    MediumLow,
    Hungry
}

public enum PetState
{
    idle,
    feeding,
    sleeping,
    ball,
    snack,
    napping,
    ready,
    moving,
    toy,
    action,
    reminder,
    walk,       // メタバース散歩中
}