using UnityEngine;
using System.Collections.Generic;
using TapHouse.Logging;

/// <summary>
/// 要求度管理クラス（拡張版）
/// 愛情度（Love）に応じて、要求度の変動幅が変わる
/// </summary>
public class DemandManager
{
    private int demandValue;
    private const int MinValue = 0;
    private const int MaxValue = 100;
    private const int DefaultInitialValue = 50;

    // 登録された調整ルール
    private List<IDemandAdjuster> adjusters = new List<IDemandAdjuster>();

    public int Value => demandValue;

    public DemandLevel CurrentDemandLevel
    {
        get
        {
            if (demandValue <= 33)
            {
                return DemandLevel.Low;
            }

            if (demandValue <= 66)
            {
                return DemandLevel.Medium;
            }

            return DemandLevel.High;
        }
    }

    public DemandManager(int initialValue = DefaultInitialValue)
    {
        demandValue = Mathf.Clamp(initialValue, MinValue, MaxValue);
    }

    public void Increase(int amount)
    {
        demandValue = Mathf.Clamp(demandValue + amount, MinValue, MaxValue);
    }

    public void Decrease(int amount)
    {
        demandValue = Mathf.Clamp(demandValue - amount, MinValue, MaxValue);
    }

    /// <summary>
    /// 1時間ごとの要求度増加（愛情度で変動）
    /// </summary>
    public void TickHourPassed(LoveLevel loveLevel)
    {
        int increaseAmount = GetHourlyIncreaseAmount(loveLevel);
        Increase(increaseAmount);
        GameLogger.Log(LogCategory.Dog,$"1時間経過。要求度+{increaseAmount}（愛情: {loveLevel}）");
    }

    /// <summary>
    /// なでる行動による要求度減少（愛情度で変動）
    /// </summary>
    public void OnPet(LoveLevel loveLevel)
    {
        int decreaseAmount = GetPetDecreaseAmount(loveLevel);
        Decrease(decreaseAmount);
        GameLogger.Log(LogCategory.Dog,$"なでました。要求度-{decreaseAmount}（愛情: {loveLevel}）");
    }

    /// <summary>
    /// おやつによる要求度減少（愛情度で変動）
    /// </summary>
    public void OnGiveSnack(SnackType snack, LoveLevel loveLevel)
    {
        int baseDecrease = snack == SnackType.Meat ? 25 : 15;
        int decreaseAmount = GetAdjustedDecrease(baseDecrease, loveLevel);
        Decrease(decreaseAmount);
        GameLogger.Log(LogCategory.Dog,$"おやつあげました。要求度-{decreaseAmount}（愛情: {loveLevel}）");
    }

    /// <summary>
    /// 愛情度に応じた1時間あたりの要求度上昇量
    /// 愛情が高いほど、要求度の上昇が緩やか
    /// </summary>
    private int GetHourlyIncreaseAmount(LoveLevel loveLevel)
    {
        switch (loveLevel)
        {
            case LoveLevel.Low:
                return 15; // 慣れてないので構ってほしい
            case LoveLevel.Medium:
                return 10; // 標準
            case LoveLevel.High:
                return 5;  // 信頼しているので落ち着いている
            default:
                return 10;
        }
    }

    /// <summary>
    /// 愛情度に応じたなでる効果
    /// 愛情が高いほど、少しのなでるで満足
    /// </summary>
    private int GetPetDecreaseAmount(LoveLevel loveLevel)
    {
        switch (loveLevel)
        {
            case LoveLevel.Low:
                return 5;  // まだ警戒中、効果薄い
            case LoveLevel.Medium:
                return 15; // 標準
            case LoveLevel.High:
                return 30; // 大好きな飼い主のなでるで大満足！
            default:
                return 15;
        }
    }

    /// <summary>
    /// 愛情度に応じた減少量調整（共通処理）
    /// </summary>
    private int GetAdjustedDecrease(int baseAmount, LoveLevel loveLevel)
    {
        float multiplier = 1.0f;

        switch (loveLevel)
        {
            case LoveLevel.Low:
                multiplier = 0.7f; // 70%の効果
                break;
            case LoveLevel.Medium:
                multiplier = 1.0f; // 100%の効果
                break;
            case LoveLevel.High:
                multiplier = 1.5f; // 150%の効果
                break;
        }

        return Mathf.RoundToInt(baseAmount * multiplier);
    }

    /// <summary>
    /// 調整ルールを登録（他の状態でも再利用可能）
    /// </summary>
    public void RegisterAdjuster(IDemandAdjuster adjuster)
    {
        if (!adjusters.Contains(adjuster))
        {
            adjusters.Add(adjuster);
        }
    }

    /// <summary>
    /// 調整ルールを解除
    /// </summary>
    public void UnregisterAdjuster(IDemandAdjuster adjuster)
    {
        adjusters.Remove(adjuster);
    }

    /// <summary>
    /// 登録された全ての調整ルールを評価して適用
    /// </summary>
    public void ApplyAllAdjusters()
    {
        foreach (var adjuster in adjusters)
        {
            if (adjuster.ShouldAdjust())
            {
                int adjustment = adjuster.GetAdjustmentAmount();
                if (adjustment > 0)
                {
                    Increase(adjustment);
                }
                else
                {
                    Decrease(-adjustment);
                }

                GameLogger.Log(LogCategory.Dog,$"{adjuster.GetReasonText()}。現在の要求度: {Value}");
            }
        }
    }
}