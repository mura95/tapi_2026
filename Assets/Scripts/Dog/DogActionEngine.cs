using UnityEngine;
using System;
using TapHouse.Logging;

/// <summary>
/// アクション実行エンジン
/// すべてのプレイヤーアクション（なでる、餌、おやつなど）を統一的に処理
/// </summary>
public class DogActionEngine
{
    private LoveManager loveManager;
    private DemandManager demandManager;
    private DogPersonalityData personality;

    public DogActionEngine(LoveManager loveManager, DemandManager demandManager, DogPersonalityData personality)
    {
        this.loveManager = loveManager;
        this.demandManager = demandManager;
        this.personality = personality;
    }

    /// <summary>
    /// アクションを実行（メインメソッド）
    /// すべてのアクションはこのメソッド1つで処理される
    /// </summary>
    public ActionResult ExecuteAction(DogActionData actionData)
    {
        var result = new ActionResult
        {
            actionName = actionData.actionName,
            success = true
        };

        // Love（愛情度）の変化を計算
        if (actionData.baseEffect.loveChange != 0)
        {
            int loveChange = CalculateLoveChange(actionData);

            if (loveChange > 0)
            {
                loveManager.Increase(loveChange);
                result.loveChange = loveChange;
            }
            else if (loveChange < 0)
            {
                loveManager.Decrease(-loveChange);
                result.loveChange = loveChange;
            }
        }

        // Demand（要求度）の変化を計算
        if (actionData.baseEffect.demandChange != 0)
        {
            int demandChange = CalculateDemandChange(actionData);

            if (demandChange > 0)
            {
                demandManager.Increase(demandChange);
                result.demandChange = demandChange;
            }
            else if (demandChange < 0)
            {
                demandManager.Decrease(-demandChange);
                result.demandChange = demandChange;
            }
        }

        // Hunger（空腹度）の回復
        if (actionData.baseEffect.feedsHunger)
        {
            // HungerManager側で処理される想定
            result.hungerFed = true;
        }

        // ログ出力
        LogActionResult(result);

        return result;
    }

    /// <summary>
    /// Loveの変化量を計算
    /// </summary>
    private int CalculateLoveChange(DogActionData actionData)
    {
        float finalChange = actionData.baseEffect.loveChange;

        // 1. 性格による倍率適用
        if (actionData.applyPersonalityToLove)
        {
            if (actionData.baseEffect.loveChange > 0)
            {
                finalChange *= personality.loveIncreaseMultiplier;
            }
            else
            {
                finalChange *= personality.loveDecreaseMultiplier;
            }
        }

        // 2. 愛情レベルによる倍率適用
        if (actionData.scaleWithLoveLevel)
        {
            float loveLevelMultiplier = GetLoveLevelMultiplier(actionData);
            finalChange *= loveLevelMultiplier;
        }

        // 3. 食事ボーナス（性格による）
        if (actionData.baseEffect.usePersonalityFeedBonus && actionData.baseEffect.loveChange > 0)
        {
            finalChange += personality.feedLoveBonus;
        }

        // 4. 理想的な時間ボーナス
        if (actionData.baseEffect.useIdealTimingBonus && actionData.baseEffect.loveChange > 0)
        {
            if (IsIdealMealTime())
            {
                finalChange += 1; // ボーナス+1
                GameLogger.Log(LogCategory.Dog,"⭐ 理想的な時間！ボーナス+1");
            }
        }

        return Mathf.RoundToInt(finalChange);
    }

    /// <summary>
    /// Demandの変化量を計算
    /// </summary>
    private int CalculateDemandChange(DogActionData actionData)
    {
        float finalChange = actionData.baseEffect.demandChange;

        // 1. 性格による倍率適用
        if (actionData.applyPersonalityToDemand)
        {
            if (actionData.baseEffect.demandChange > 0)
            {
                finalChange *= personality.demandIncreaseMultiplier;
            }
            else
            {
                finalChange *= personality.demandDecreaseMultiplier;
            }
        }

        // 2. 愛情レベルによる倍率適用
        if (actionData.scaleWithLoveLevel)
        {
            float loveLevelMultiplier = GetLoveLevelMultiplier(actionData);
            finalChange *= loveLevelMultiplier;
        }

        return Mathf.RoundToInt(finalChange);
    }

    /// <summary>
    /// 愛情レベルに応じた倍率を取得
    /// </summary>
    private float GetLoveLevelMultiplier(DogActionData actionData)
    {
        switch (loveManager.CurrentLoveLevel)
        {
            case LoveLevel.Low:
                return actionData.loveLowMultiplier;
            case LoveLevel.Medium:
                return actionData.loveMediumMultiplier;
            case LoveLevel.High:
                return actionData.loveHighMultiplier;
            default:
                return 1.0f;
        }
    }

    /// <summary>
    /// 理想的な食事時間かチェック（朝7-9時、夕方17-19時）
    /// </summary>
    private bool IsIdealMealTime()
    {
        int currentHour = TimeZoneProvider.Now.Hour;
        return (currentHour >= 7 && currentHour <= 9) || (currentHour >= 17 && currentHour <= 19);
    }

    /// <summary>
    /// 結果をログ出力
    /// </summary>
    private void LogActionResult(ActionResult result)
    {
        string log = $"[{result.actionName}] ";

        if (result.loveChange != 0)
        {
            log += $"Love: {(result.loveChange > 0 ? "+" : "")}{result.loveChange} (現在: {loveManager.Value}) ";
        }

        if (result.demandChange != 0)
        {
            log += $"Demand: {(result.demandChange > 0 ? "+" : "")}{result.demandChange} (現在: {demandManager.Value}) ";
        }

        if (result.hungerFed)
        {
            log += "Hunger: 回復 ";
        }

        GameLogger.Log(LogCategory.Dog,log);
    }

    /// <summary>
    /// 性格を更新（動的に変更する場合）
    /// </summary>
    public void UpdatePersonality(DogPersonalityData newPersonality)
    {
        this.personality = newPersonality;
        GameLogger.Log(LogCategory.Dog,$"性格を変更: {newPersonality.personalityName}");
    }
}

/// <summary>
/// アクション実行結果
/// </summary>
public class ActionResult
{
    public string actionName;
    public bool success;
    public int loveChange;
    public int demandChange;
    public bool hungerFed;
}