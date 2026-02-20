using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TapHouse.Logging;
using TapHouse.MetaverseWalk.Core;

/// <summary>
/// 犬の状態を総合管理するコントローラー（完全版 + Love時間経過対応）
/// Love、Demand、Hungerをすべて統合管理
/// LoveManagerの時間経過ペナルティシステムに対応
/// </summary>
public class DogStateController : MonoBehaviour
{
    [Header("犬の性格設定")]
    [SerializeField] private DogPersonalityData personality;

    [Header("初期値設定")]
    [SerializeField, Range(0, 100)]
    private int initialLoveValue = 20;

    [SerializeField, Range(0, 100)]
    private int initialDemandValue = 50;

    [Header("アクション定義（ScriptableObject）")]
    [SerializeField] private DogActionData actionPet;      // なでる
    [SerializeField] private DogActionData actionFeed;     // ごはん
    [SerializeField] private DogActionData actionSnack;    // おやつ
    [SerializeField] private DogActionData actionPlay;     // 遊ぶ
    [SerializeField] private DogActionData actionWalk;     // 散歩

    [Header("他のマネージャー参照")]
    [SerializeField] private HungerManager hungerManager;  // 空腹管理
    [SerializeField] private DogBodyShapeManager bodyShapeManager;  // 体型管理

    [Header("タイマー設定")]
    [SerializeField] private float realTimeHour = 3600f;
    [SerializeField] private float stateCheckInterval = 30f;
    [SerializeField] private float loveCheckInterval = 3600f; // 愛情度チェック間隔（1時間 = 3600秒）

    // マネージャー
    private LoveManager _loveManager;
    private DemandManager _demandManager;
    private DogActionEngine _actionEngine;

    public LoveManager LoveManager => _loveManager;
    public DemandManager DemandManager => _demandManager;
    public DogPersonalityData Personality => personality;

    // タイマー
    private float hourTimer = 0f;
    private float stateCheckTimer = 0f;
    private float loveCheckTimer = 0f;

    // 調整ルール
    private HungerDemandAdjuster hungerAdjuster;

    void Awake()
    {
        InitializeManagers();
        InitializeActionEngine();
        InitializeAdjusters();
        ValidateReferences();

        GameLogger.Log(LogCategory.Dog,$"=== Dog State Controller 初期化 ===");
        GameLogger.Log(LogCategory.Dog,$"性格: {personality.personalityName}");
        GameLogger.Log(LogCategory.Dog,$"愛情度: {_loveManager.Value} ({_loveManager.CurrentLoveLevel})");
        GameLogger.Log(LogCategory.Dog,$"要求度: {_demandManager.Value} ({_demandManager.CurrentDemandLevel})");
    }

    /// <summary>
    /// マネージャーの初期化
    /// </summary>
    private void InitializeManagers()
    {
        // 性格による初期値調整
        int adjustedLove = Mathf.RoundToInt(initialLoveValue * personality.loveIncreaseMultiplier);
        int adjustedDemand = Mathf.RoundToInt(initialDemandValue * personality.demandIncreaseMultiplier);

        _loveManager = new LoveManager(adjustedLove);
        _demandManager = new DemandManager(adjustedDemand);
    }

    /// <summary>
    /// アクションエンジンの初期化
    /// </summary>
    private void InitializeActionEngine()
    {
        _actionEngine = new DogActionEngine(_loveManager, _demandManager, personality);
    }

    /// <summary>
    /// 調整ルールの初期化
    /// </summary>
    private void InitializeAdjusters()
    {
        // 空腹による要求度減少（性格の影響を適用）
        float hungerMultiplier = personality.hungerProgressMultiplier;
        int hungerDecreaseAmount = Mathf.RoundToInt(5 * hungerMultiplier);
        hungerAdjuster = new HungerDemandAdjuster(hungerDecreaseAmount);
        _demandManager.RegisterAdjuster(hungerAdjuster);
    }

    /// <summary>
    /// 必要な参照の検証
    /// </summary>
    private void ValidateReferences()
    {
        if (personality == null)
        {
            GameLogger.LogError(LogCategory.Dog,"⚠️ Personality が設定されていません！Inspector で設定してください。");
        }

        if (hungerManager == null)
        {
            GameLogger.LogWarning(LogCategory.Dog,"⚠️ HungerManager が設定されていません。食事機能が正しく動作しない可能性があります。");
        }

        if (bodyShapeManager == null)
        {
            GameLogger.LogWarning(LogCategory.Dog,"⚠️ BodyShapeManager が設定されていません。体型変化機能が動作しません。");
        }
    }

    void Update()
    {
        hourTimer += Time.deltaTime;
        stateCheckTimer += Time.deltaTime;
        loveCheckTimer += Time.deltaTime;

        // 1時間ごとの処理
        if (hourTimer >= realTimeHour)
        {
            TickHourPassed();
            hourTimer = 0f;
        }

        // 定期的な状態チェック
        if (stateCheckTimer >= stateCheckInterval)
        {
            StateCheck();
            stateCheckTimer = 0f;
        }

        // 愛情度の時間経過チェック（1時間ごと）
        if (loveCheckTimer >= loveCheckInterval)
        {
            CheckLoveTimePenalty();
            loveCheckTimer = 0f;
        }
    }

    /// <summary>
    /// 1時間経過時の処理
    /// </summary>
    private void TickHourPassed()
    {
        // 要求度増加（性格の影響を適用）
        _demandManager.TickHourPassed(_loveManager.CurrentLoveLevel);

        // 性格による追加調整
        float demandMultiplier = personality.demandIncreaseMultiplier;
        if (demandMultiplier != 1.0f)
        {
            int adjustment = Mathf.RoundToInt(5 * (demandMultiplier - 1.0f));
            if (adjustment != 0)
            {
                _demandManager.Increase(adjustment);
            }
        }
    }

    /// <summary>
    /// 定期的な状態チェック
    /// </summary>
    private void StateCheck()
    {
        _demandManager.ApplyAllAdjusters();

        // 散歩スケジュールチェック（30秒間隔で十分）
        WalkScheduler.Instance?.CheckSchedule();
    }

    /// <summary>
    /// 愛情度の時間経過ペナルティをチェック
    /// </summary>
    private void CheckLoveTimePenalty()
    {
        // 通常の時間経過ペナルティ
        _loveManager.CheckTimePenalty();

        // 空腹状態での放置ペナルティ
        if (GlobalVariables.CurrentHungerState == HungerState.Hungry)
        {
            _loveManager.ApplyHungerNeglectPenalty();
        }
    }

    // ===== プレイヤーのアクション =====

    /// <summary>
    /// なでる
    /// </summary>
    public void OnPet()
    {
        if (actionPet != null)
        {
            _actionEngine.ExecuteAction(actionPet);
        }
        else
        {
            GameLogger.LogWarning(LogCategory.Dog,"actionPet が設定されていません");
        }
    }

    /// <summary>
    /// ごはんを与える（完全実装版）
    /// Love/Demand + Hunger + BodyShape を統合管理
    /// </summary>
    public void OnFeed()
    {
        if (actionFeed != null)
        {
            // 1. Love/Demandの処理（ActionEngineで自動）
            var result = _actionEngine.ExecuteAction(actionFeed);

            // 2. Hungerの回復処理
            if (result.hungerFed)
            {
                if (hungerManager != null)
                {
                    hungerManager.IncreaseHungerState();
                    GameLogger.Log(LogCategory.Dog,"✅ 空腹度を回復しました");
                }
                else
                {
                    GameLogger.LogWarning(LogCategory.Dog,"⚠️ HungerManager が設定されていません。Inspector で設定してください。");
                }

                // 3. 体型の変化処理
                if (bodyShapeManager != null)
                {
                    bodyShapeManager.OnFeed();
                }
            }
        }
        else
        {
            GameLogger.LogWarning(LogCategory.Dog,"actionFeed が設定されていません");
        }
    }

    /// <summary>
    /// おやつを与える
    /// </summary>
    public void OnGiveSnack()
    {
        if (actionSnack != null)
        {
            _actionEngine.ExecuteAction(actionSnack);
        }
        else
        {
            GameLogger.LogWarning(LogCategory.Dog,"actionSnack が設定されていません");
        }
    }

    /// <summary>
    /// 遊ぶ
    /// </summary>
    public void OnPlay()
    {
        if (actionPlay != null)
        {
            _actionEngine.ExecuteAction(actionPlay);
        }
        else
        {
            GameLogger.LogWarning(LogCategory.Dog,"actionPlay が設定されていません");
        }
    }

    /// <summary>
    /// 散歩
    /// </summary>
    public void OnWalk()
    {
        if (actionWalk != null)
        {
            _actionEngine.ExecuteAction(actionWalk);
        }
        else
        {
            GameLogger.LogWarning(LogCategory.Dog,"actionWalk が設定されていません");
        }
    }

    /// <summary>
    /// 汎用アクション実行（外部から任意のアクションを実行可能）
    /// </summary>
    public void ExecuteCustomAction(DogActionData customAction)
    {
        if (customAction != null)
        {
            var result = _actionEngine.ExecuteAction(customAction);

            // カスタムアクションでも Hunger 回復に対応
            if (result.hungerFed && hungerManager != null)
            {
                hungerManager.IncreaseHungerState();
            }
        }
    }

    /// <summary>
    /// 性格を変更（ゲーム中に性格が変わる場合）
    /// </summary>
    public void ChangePersonality(DogPersonalityData newPersonality)
    {
        if (newPersonality != null)
        {
            personality = newPersonality;
            _actionEngine.UpdatePersonality(newPersonality);
            GameLogger.Log(LogCategory.Dog,$"性格を変更しました: {newPersonality.personalityName}");
        }
    }

    /// <summary>
    /// デバッグ用：現在の状態を表示
    /// </summary>
    [ContextMenu("現在の状態を表示")]
    public void ShowCurrentState()
    {
        GameLogger.Log(LogCategory.Dog,"=== 現在の犬の状態 ===");
        GameLogger.Log(LogCategory.Dog,$"性格: {(personality != null ? personality.personalityName : "未設定")}");
        GameLogger.Log(LogCategory.Dog,$"愛情度: {_loveManager.Value} / 100 ({_loveManager.CurrentLoveLevel})");
        GameLogger.Log(LogCategory.Dog,$"最後の交流: {_loveManager.GetTimeSinceLastInteraction()}");
        GameLogger.Log(LogCategory.Dog,$"要求度: {_demandManager.Value} / 100 ({_demandManager.CurrentDemandLevel})");
        GameLogger.Log(LogCategory.Dog,$"空腹度: {GlobalVariables.CurrentHungerState}");

        if (bodyShapeManager != null)
        {
            GameLogger.Log(LogCategory.Dog,$"体型: {bodyShapeManager.CurrentBodyShape} (スケール: {bodyShapeManager.CurrentBodyScale:F3})");
        }

        GameLogger.Log(LogCategory.Dog,"==================");
    }

    /// <summary>
    /// デバッグ用：すべての参照を確認
    /// </summary>
    [ContextMenu("設定を確認")]
    public void ValidateSetup()
    {
        GameLogger.Log(LogCategory.Dog,"=== 設定確認 ===");

        GameLogger.Log(LogCategory.Dog,$"Personality: {(personality != null ? "✅ " + personality.personalityName : "❌ 未設定")}");
        GameLogger.Log(LogCategory.Dog,$"HungerManager: {(hungerManager != null ? "✅ 設定済み" : "❌ 未設定")}");
        GameLogger.Log(LogCategory.Dog,$"BodyShapeManager: {(bodyShapeManager != null ? "✅ 設定済み" : "❌ 未設定")}");

        GameLogger.Log(LogCategory.Dog,$"Action Pet: {(actionPet != null ? "✅ " + actionPet.actionName : "❌ 未設定")}");
        GameLogger.Log(LogCategory.Dog,$"Action Feed: {(actionFeed != null ? "✅ " + actionFeed.actionName : "❌ 未設定")}");
        GameLogger.Log(LogCategory.Dog,$"Action Snack: {(actionSnack != null ? "✅ " + actionSnack.actionName : "❌ 未設定")}");
        GameLogger.Log(LogCategory.Dog,$"Action Play: {(actionPlay != null ? "✅ " + actionPlay.actionName : "❌ 未設定")}");
        GameLogger.Log(LogCategory.Dog,$"Action Walk: {(actionWalk != null ? "✅ " + actionWalk.actionName : "❌ 未設定")}");

        GameLogger.Log(LogCategory.Dog,"===============");
    }

    /// <summary>
    /// デバッグ用：愛情度の時間ペナルティを強制チェック
    /// </summary>
    [ContextMenu("愛情度ペナルティをチェック")]
    public void DebugCheckLovePenalty()
    {
        CheckLoveTimePenalty();
        GameLogger.Log(LogCategory.Dog,$"愛情度ペナルティチェック完了。現在の愛情度: {_loveManager.Value}");
    }

    void OnDestroy()
    {
        if (_demandManager != null && hungerAdjuster != null)
        {
            _demandManager.UnregisterAdjuster(hungerAdjuster);
        }
    }
}