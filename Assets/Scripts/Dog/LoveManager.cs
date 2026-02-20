using UnityEngine;
using System;
using TapHouse.Logging;

/// <summary>
/// 愛情度管理クラス（時間経過による減少機能付き）
/// </summary>
public class LoveManager
{
    private int loveValue;
    private const int MinValue = 0;
    private const int MaxValue = 100;
    private const int DefaultInitialValue = 20;

    // 時間管理用
    private DateTime lastInteractionTime;
    private const string LAST_INTERACTION_KEY = "LastInteractionTime";

    public int Value => loveValue;

    public LoveLevel CurrentLoveLevel
    {
        get
        {
            if (loveValue <= 33)
            {
                return LoveLevel.Low;
            }

            if (loveValue <= 66)
            {
                return LoveLevel.Medium;
            }

            return LoveLevel.High;
        }
    }

    public LoveManager(int initialValue = DefaultInitialValue)
    {
        loveValue = Mathf.Clamp(initialValue, MinValue, MaxValue);

        // 最後の交流時間を復元または初期化
        RestoreLastInteractionTime();
    }

    /// <summary>
    /// 愛情度を増加（ゆっくり蓄積）
    /// </summary>
    public void Increase(int amount)
    {
        int oldValue = loveValue;
        loveValue = Mathf.Clamp(loveValue + amount, MinValue, MaxValue);

        // 交流したので時間を更新
        UpdateLastInteractionTime();

        // レベルアップ時の通知
        if (GetLevelFromValue(oldValue) != GetLevelFromValue(loveValue))
        {
            GameLogger.Log(LogCategory.Dog,$"愛情レベルアップ！ {GetLevelFromValue(oldValue)} → {GetLevelFromValue(loveValue)}");
        }
    }

    /// <summary>
    /// 愛情度を減少（めったに起こらない）
    /// </summary>
    public void Decrease(int amount)
    {
        int oldValue = loveValue;
        loveValue = Mathf.Clamp(loveValue - amount, MinValue, MaxValue);

        // レベルダウン時の警告
        if (GetLevelFromValue(oldValue) != GetLevelFromValue(loveValue))
        {
            GameLogger.LogWarning(LogCategory.Dog,$"愛情レベルダウン... {GetLevelFromValue(oldValue)} → {GetLevelFromValue(loveValue)}");
        }
    }

    /// <summary>
    /// 時間経過による愛情度の自然減少をチェック
    /// DogStateController の Update() や定期チェックから呼ぶ
    /// </summary>
    public void CheckTimePenalty()
    {
        TimeSpan timeSinceLastInteraction = DateTime.UtcNow - lastInteractionTime;
        double hoursSinceLastInteraction = timeSinceLastInteraction.TotalHours;

        // 6時間ごとにチェック
        if (hoursSinceLastInteraction >= 6)
        {
            int penaltyAmount = CalculateTimePenalty(hoursSinceLastInteraction);

            if (penaltyAmount > 0)
            {
                Decrease(penaltyAmount);
                GameLogger.LogWarning(LogCategory.Dog,$"⏰ {hoursSinceLastInteraction:F1}時間放置されたため、愛情度-{penaltyAmount}");

                // ペナルティを適用したので、時間を更新（次のペナルティまでリセット）
                UpdateLastInteractionTime();
            }
        }
    }

    /// <summary>
    /// 放置時間に応じたペナルティ量を計算
    /// </summary>
    private int CalculateTimePenalty(double hours)
    {
        if (hours < 6) return 0;           // 6時間未満: ペナルティなし
        if (hours < 12) return 1;          // 6-12時間: -1
        if (hours < 24) return 2;          // 12-24時間: -2
        if (hours < 48) return 5;          // 24-48時間: -5
        return 10;                         // 48時間以上: -10（深刻）
    }

    /// <summary>
    /// 空腹状態で放置された場合の追加ペナルティ
    /// HungerState が Hungry の時に呼ぶ
    /// </summary>
    public void ApplyHungerNeglectPenalty()
    {
        TimeSpan timeSinceLastInteraction = DateTime.UtcNow - lastInteractionTime;

        // 空腹状態で2時間以上放置
        if (timeSinceLastInteraction.TotalHours >= 2)
        {
            Decrease(2);
            GameLogger.LogWarning(LogCategory.Dog,$"😢 空腹状態で{timeSinceLastInteraction.TotalHours:F1}時間放置されたため、愛情度-2");
            UpdateLastInteractionTime();
        }
    }

    /// <summary>
    /// 最後の交流時間を更新
    /// </summary>
    private void UpdateLastInteractionTime()
    {
        lastInteractionTime = DateTime.UtcNow;
        long unixSeconds = new DateTimeOffset(lastInteractionTime, TimeSpan.Zero).ToUnixTimeSeconds();
        PlayerPrefs.SetString(LAST_INTERACTION_KEY, unixSeconds.ToString());
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 最後の交流時間を復元
    /// </summary>
    private void RestoreLastInteractionTime()
    {
        if (PlayerPrefs.HasKey(LAST_INTERACTION_KEY))
        {
            try
            {
                long storedValue = long.Parse(PlayerPrefs.GetString(LAST_INTERACTION_KEY));

                // Unix秒（新形式）かBinary（旧形式）かを判別
                // Unix秒は概ね10桁、Binary形式は18桁前後
                if (storedValue > 0 && storedValue < 100_000_000_000L)
                {
                    // 新形式: Unix秒
                    lastInteractionTime = DateTimeOffset.FromUnixTimeSeconds(storedValue).UtcDateTime;
                }
                else
                {
                    // 旧形式: Binary → UTCに変換して再保存
                    lastInteractionTime = DateTime.FromBinary(storedValue).ToUniversalTime();
                    UpdateLastInteractionTime();
                }
                GameLogger.Log(LogCategory.Dog,$"最後の交流時間を復元: {lastInteractionTime} (UTC)");
            }
            catch
            {
                GameLogger.LogWarning(LogCategory.Dog,"最後の交流時間の復元に失敗。現在時刻で初期化します。");
                lastInteractionTime = DateTime.UtcNow;
                UpdateLastInteractionTime();
            }
        }
        else
        {
            // 初回起動
            lastInteractionTime = DateTime.UtcNow;
            UpdateLastInteractionTime();
            GameLogger.Log(LogCategory.Dog,"初回起動: 最後の交流時間を初期化");
        }
    }

    /// <summary>
    /// なでる行動による愛情度増加（小）
    /// </summary>
    public void OnPet()
    {
        Increase(1);
    }

    /// <summary>
    /// おやつによる愛情度増加（中）
    /// </summary>
    public void OnGiveSnack(SnackType snack)
    {
        switch (snack)
        {
            case SnackType.Biscuit:
                Increase(2);
                break;
            case SnackType.Meat:
                Increase(3);
                break;
        }
    }

    /// <summary>
    /// ごはんによる愛情度増加（中）
    /// </summary>
    public void OnFeed()
    {
        Increase(2);
    }

    /// <summary>
    /// デバッグ用：最後の交流からの経過時間を取得
    /// </summary>
    public string GetTimeSinceLastInteraction()
    {
        TimeSpan elapsed = DateTime.UtcNow - lastInteractionTime;
        return $"{elapsed.Hours}時間{elapsed.Minutes}分前";
    }

    private LoveLevel GetLevelFromValue(int value)
    {
        if (value <= 33) return LoveLevel.Low;
        if (value <= 66) return LoveLevel.Medium;
        return LoveLevel.High;
    }
}

/// <summary>
/// 愛情レベル（3段階）
/// </summary>
public enum LoveLevel
{
    Low,    // 0-33: まだ慣れてない、警戒中
    Medium, // 34-66: そこそこ仲良し
    High    // 67-100: 完全に信頼、大好き
}