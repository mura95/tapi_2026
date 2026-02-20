using UnityEngine;
using System;
using TapHouse.Logging;

/// <summary>
/// 犬の体型管理システム
/// 空腹状態の継続で痩せ、満腹時の過剰な餌やりで太る
/// </summary>
public class DogBodyShapeManager : MonoBehaviour
{
    [Header("体型設定")]
    [SerializeField] private Transform dogTransform; // 犬のTransform
    [SerializeField, Range(0.8f, 1.2f)] private float minScale = 0.95f; // 最小スケール（痩せ）
    [SerializeField, Range(0.8f, 1.2f)] private float maxScale = 1.05f; // 最大スケール（太り）
    [SerializeField, Range(0.8f, 1.2f)] private float normalScale = 1.0f; // 標準スケール

    [Header("変化速度設定")]
    [SerializeField] private float thinningRate = 0.01f; // 痩せる速度（1時間あたり）
    [SerializeField] private float fatteningRate = 0.005f; // 太る速度（餌1回あたり）
    [SerializeField] private float recoveryRate = 0.008f; // 標準体型への回復速度（1時間あたり）

    [Header("時間設定")]
    [SerializeField] private float realTimeHour = 3600f; // 実時間1時間（秒）
    [SerializeField] private float hungerCheckInterval = 3600f; // 空腹チェック間隔（1時間）

    [Header("参照")]
    [SerializeField] private DogStateController dogStateController;

    // 現在の体型スケール
    private float currentBodyScale = 1.0f;

    // タイマー
    private float hungerTimer = 0f;
    private float recoveryTimer = 0f;

    // 状態追跡
    private DateTime lastHungryCheckTime;
    private int consecutiveFullFeedings = 0; // 連続で満腹時に餌を与えた回数

    // データ保存用キー
    private const string BODY_SCALE_KEY = "DogBodyScale";
    private const string LAST_HUNGRY_CHECK_KEY = "LastHungryCheckTime";
    private const string CONSECUTIVE_FEEDINGS_KEY = "ConsecutiveFullFeedings";

    public float CurrentBodyScale => currentBodyScale;
    public BodyShapeLevel CurrentBodyShape => GetBodyShapeLevel();

    void Awake()
    {
        ValidateReferences();
        LoadBodyData();
    }

    void Start()
    {
        ApplyBodyScale();
    }

    void Update()
    {
        hungerTimer += Time.deltaTime;
        recoveryTimer += Time.deltaTime;

        // 空腹状態のチェック（30分ごと）
        if (hungerTimer >= hungerCheckInterval)
        {
            CheckHungerEffect();
            hungerTimer = 0f;
        }

        // 標準体型への回復（1時間ごと）
        if (recoveryTimer >= realTimeHour)
        {
            RecoverToNormalShape();
            recoveryTimer = 0f;
        }
    }

    /// <summary>
    /// 参照の検証
    /// </summary>
    private void ValidateReferences()
    {
        if (dogTransform == null)
        {
            GameLogger.LogError(LogCategory.Dog,"⚠️ DogTransform が設定されていません！");
        }

        if (dogStateController == null)
        {
            dogStateController = FindObjectOfType<DogStateController>();
            if (dogStateController == null)
            {
                GameLogger.LogError(LogCategory.Dog,"⚠️ DogStateController が見つかりません！");
            }
        }
    }

    /// <summary>
    /// 空腹状態の影響をチェック
    /// </summary>
    private void CheckHungerEffect()
    {
        if (GlobalVariables.CurrentHungerState == HungerState.Hungry)
        {
            // 空腹状態が続いている場合、痩せる
            TimeSpan hungryDuration = DateTime.UtcNow - lastHungryCheckTime;

            if (hungryDuration.TotalHours >= 1) // 1時間以上空腹
            {
                float thinAmount = thinningRate * (float)hungryDuration.TotalHours;
                AdjustBodyScale(-thinAmount);
                GameLogger.LogWarning(LogCategory.Dog,$"😰 空腹状態が{hungryDuration.TotalHours:F1}時間続いています。体型: {currentBodyScale:F3}");
                lastHungryCheckTime = DateTime.UtcNow;
            }
        }
        else
        {
            // 空腹でない場合、タイマーをリセット
            lastHungryCheckTime = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 餌やり時の体型変化（外部から呼ばれる）
    /// </summary>
    public void OnFeed()
    {
        HungerState hungerState = GlobalVariables.CurrentHungerState;

        switch (hungerState)
        {
            case HungerState.Full:
                // 満腹時に餌を与える = 太る
                consecutiveFullFeedings++;
                float fattenAmount = fatteningRate * consecutiveFullFeedings;
                AdjustBodyScale(fattenAmount);
                GameLogger.Log(LogCategory.Dog,$"🍖 満腹時に餌やり（{consecutiveFullFeedings}回目）。少し太りました: {currentBodyScale:F3}");
                break;

            case HungerState.MediumHigh:
                // 少し空腹 = 適切なタイミング、連続カウントリセット
                consecutiveFullFeedings = 0;
                GameLogger.Log(LogCategory.Dog,"✅ 適切なタイミングで餌やり");
                break;

            case HungerState.MediumLow:
                // かなり空腹 = 適切、連続カウントリセット
                consecutiveFullFeedings = 0;
                GameLogger.Log(LogCategory.Dog,"✅ 良いタイミングで餌やり");
                break;

            case HungerState.Hungry:
                // 空腹 = 体型回復のチャンス、連続カウントリセット
                consecutiveFullFeedings = 0;

                // 痩せている場合は少し回復
                if (currentBodyScale < normalScale)
                {
                    AdjustBodyScale(recoveryRate * 2); // 通常の2倍の速度で回復
                    GameLogger.Log(LogCategory.Dog,$"🍴 空腹時の餌やり。体型が回復: {currentBodyScale:F3}");
                }
                break;
        }

        SaveBodyData();
    }

    /// <summary>
    /// 体型スケールを調整
    /// </summary>
    private void AdjustBodyScale(float amount)
    {
        currentBodyScale = Mathf.Clamp(currentBodyScale + amount, minScale, maxScale);
        ApplyBodyScale();
        SaveBodyData();

        // 体型レベルの変化を通知
        BodyShapeLevel newLevel = GetBodyShapeLevel();
        NotifyBodyShapeChange(newLevel);
    }

    /// <summary>
    /// 体型を実際のTransformに適用
    /// </summary>
    private void ApplyBodyScale()
    {
        if (dogTransform != null)
        {
            Vector3 scale = dogTransform.localScale;
            scale.x = currentBodyScale;
            dogTransform.localScale = scale;
        }
    }

    /// <summary>
    /// 標準体型への自然回復
    /// </summary>
    private void RecoverToNormalShape()
    {
        if (Mathf.Abs(currentBodyScale - normalScale) < 0.001f)
        {
            return; // すでに標準体型
        }

        // 愛情度が高いほど回復が早い
        float recoveryMultiplier = 1.0f;
        if (dogStateController != null && dogStateController.LoveManager != null)
        {
            switch (dogStateController.LoveManager.CurrentLoveLevel)
            {
                case LoveLevel.High:
                    recoveryMultiplier = 1.5f; // 愛情高い = 1.5倍の回復
                    break;
                case LoveLevel.Medium:
                    recoveryMultiplier = 1.0f;
                    break;
                case LoveLevel.Low:
                    recoveryMultiplier = 0.5f; // 愛情低い = 回復遅い
                    break;
            }
        }

        float adjustedRecoveryRate = recoveryRate * recoveryMultiplier;

        if (currentBodyScale < normalScale)
        {
            // 痩せている → 標準へ
            AdjustBodyScale(adjustedRecoveryRate);
            GameLogger.Log(LogCategory.Dog,$"📈 体型が標準に向けて回復中: {currentBodyScale:F3}");
        }
        else if (currentBodyScale > normalScale)
        {
            // 太っている → 標準へ
            AdjustBodyScale(-adjustedRecoveryRate);
            GameLogger.Log(LogCategory.Dog,$"📉 体型が標準に向けて回復中: {currentBodyScale:F3}");
        }
    }

    /// <summary>
    /// 現在の体型レベルを取得
    /// </summary>
    private BodyShapeLevel GetBodyShapeLevel()
    {
        if (currentBodyScale <= 0.97f)
        {
            return BodyShapeLevel.VeryThin;
        }
        else if (currentBodyScale <= 0.99f)
        {
            return BodyShapeLevel.Thin;
        }
        else if (currentBodyScale >= 1.03f)
        {
            return BodyShapeLevel.Fat;
        }
        else if (currentBodyScale >= 1.01f)
        {
            return BodyShapeLevel.Chubby;
        }
        else
        {
            return BodyShapeLevel.Normal;
        }
    }

    /// <summary>
    /// 体型変化を通知
    /// </summary>
    private void NotifyBodyShapeChange(BodyShapeLevel newLevel)
    {
        // ここでイベントを発火したり、UIを更新したりできる
        GameLogger.Log(LogCategory.Dog,$"体型レベル: {newLevel} (スケール: {currentBodyScale:F3})");
    }

    /// <summary>
    /// 体型データを保存
    /// </summary>
    private void SaveBodyData()
    {
        PlayerPrefs.SetFloat(BODY_SCALE_KEY, currentBodyScale);
        long unixSeconds = new DateTimeOffset(lastHungryCheckTime, TimeSpan.Zero).ToUnixTimeSeconds();
        PlayerPrefs.SetString(LAST_HUNGRY_CHECK_KEY, unixSeconds.ToString());
        PlayerPrefs.SetInt(CONSECUTIVE_FEEDINGS_KEY, consecutiveFullFeedings);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 体型データを読み込み
    /// </summary>
    private void LoadBodyData()
    {
        if (PlayerPrefs.HasKey(BODY_SCALE_KEY))
        {
            currentBodyScale = PlayerPrefs.GetFloat(BODY_SCALE_KEY, normalScale);
        }
        else
        {
            currentBodyScale = normalScale;
        }

        if (PlayerPrefs.HasKey(LAST_HUNGRY_CHECK_KEY))
        {
            try
            {
                long storedValue = long.Parse(PlayerPrefs.GetString(LAST_HUNGRY_CHECK_KEY));

                // Unix秒（新形式）かBinary（旧形式）かを判別
                if (storedValue > 0 && storedValue < 100_000_000_000L)
                {
                    // 新形式: Unix秒
                    lastHungryCheckTime = DateTimeOffset.FromUnixTimeSeconds(storedValue).UtcDateTime;
                }
                else
                {
                    // 旧形式: Binary → UTCに変換
                    lastHungryCheckTime = DateTime.FromBinary(storedValue).ToUniversalTime();
                }
            }
            catch
            {
                lastHungryCheckTime = DateTime.UtcNow;
            }
        }
        else
        {
            lastHungryCheckTime = DateTime.UtcNow;
        }

        if (PlayerPrefs.HasKey(CONSECUTIVE_FEEDINGS_KEY))
        {
            consecutiveFullFeedings = PlayerPrefs.GetInt(CONSECUTIVE_FEEDINGS_KEY);
        }

        GameLogger.Log(LogCategory.Dog,$"体型データ読み込み完了: Scale={currentBodyScale:F3}, Level={GetBodyShapeLevel()}");
    }

    /// <summary>
    /// 体型をリセット（標準に戻す）
    /// </summary>
    [ContextMenu("体型をリセット")]
    public void ResetBodyShape()
    {
        currentBodyScale = normalScale;
        consecutiveFullFeedings = 0;
        ApplyBodyScale();
        SaveBodyData();
        GameLogger.Log(LogCategory.Dog,"体型を標準にリセットしました");
    }

    /// <summary>
    /// デバッグ用：現在の体型情報を表示
    /// </summary>
    [ContextMenu("体型情報を表示")]
    public void ShowBodyInfo()
    {
        GameLogger.Log(LogCategory.Dog,"=== 犬の体型情報 ===");
        GameLogger.Log(LogCategory.Dog,$"現在のスケール: {currentBodyScale:F3}");
        GameLogger.Log(LogCategory.Dog,$"体型レベル: {GetBodyShapeLevel()}");
        GameLogger.Log(LogCategory.Dog,$"連続満腹餌やり: {consecutiveFullFeedings}回");
        GameLogger.Log(LogCategory.Dog,$"最終空腹チェック: {lastHungryCheckTime}");
        GameLogger.Log(LogCategory.Dog,"==================");
    }

    /// <summary>
    /// デバッグ用：強制的に痩せさせる
    /// </summary>
    [ContextMenu("デバッグ: 痩せさせる")]
    public void DebugMakeThin()
    {
        AdjustBodyScale(-0.03f);
        GameLogger.Log(LogCategory.Dog,$"強制的に痩せさせました: {currentBodyScale:F3}");
    }

    /// <summary>
    /// デバッグ用：強制的に太らせる
    /// </summary>
    [ContextMenu("デバッグ: 太らせる")]
    public void DebugMakeFat()
    {
        AdjustBodyScale(0.03f);
        GameLogger.Log(LogCategory.Dog,$"強制的に太らせました: {currentBodyScale:F3}");
    }

    void OnDestroy()
    {
        SaveBodyData();
    }
}

/// <summary>
/// 体型レベル
/// </summary>
public enum BodyShapeLevel
{
    VeryThin,   // とても痩せている (≤0.97)
    Thin,       // 痩せている (0.97-0.99)
    Normal,     // 標準 (0.99-1.01)
    Chubby,     // ぽっちゃり (1.01-1.03)
    Fat         // 太っている (≥1.03)
}