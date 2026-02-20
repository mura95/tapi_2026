using UnityEngine;

/// <summary>
/// 特定の状態に基づいて要求度を調整する汎用インターフェース
/// 他の状態（睡眠、健康など）でも再利用可能
/// </summary>
public interface IDemandAdjuster
{
    /// <summary>
    /// 条件を満たしているか判定
    /// </summary>
    bool ShouldAdjust();

    /// <summary>
    /// 要求度の調整量を返す（マイナスで減少）
    /// </summary>
    int GetAdjustmentAmount();

    /// <summary>
    /// 調整理由のログ用文字列
    /// </summary>
    string GetReasonText();
}

/// <summary>
/// 空腹状態に基づく要求度減少
/// </summary>
public class HungerDemandAdjuster : IDemandAdjuster
{
    private int decreaseAmount;

    public HungerDemandAdjuster(int decreaseAmount = 5)
    {
        this.decreaseAmount = decreaseAmount;
    }

    public bool ShouldAdjust()
    {
        return GlobalVariables.CurrentHungerState == HungerState.Hungry;
    }

    public int GetAdjustmentAmount()
    {
        return -decreaseAmount;
    }

    public string GetReasonText()
    {
        return $"空腹状態が続いているため、要求度を{decreaseAmount}減少";
    }
}

/// <summary>
/// 将来の拡張例：睡眠不足による要求度減少
/// </summary>
public class SleepDeprivationDemandAdjuster : IDemandAdjuster
{
    private int decreaseAmount;

    public SleepDeprivationDemandAdjuster(int decreaseAmount = 8)
    {
        this.decreaseAmount = decreaseAmount;
    }

    public bool ShouldAdjust()
    {
        // 将来実装: GlobalVariables.CurrentSleepState == SleepState.Exhausted
        return false; // 未実装のためfalse
    }

    public int GetAdjustmentAmount()
    {
        return -decreaseAmount;
    }

    public string GetReasonText()
    {
        return $"睡眠不足のため、要求度を{decreaseAmount}減少";
    }
}