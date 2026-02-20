using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TapHouse.Logging;

public class HungerManager : MonoBehaviour
{
    [SerializeField] private HungerStatusChangedEvent hungerStatusChangedEvent;
    private const string LAST_EAT_TIME = "LastEatTime";
    private const string HUNGER_STATE_KEY = "HungerState";
    private const int HOURS_TO_FULL = 2;
    private const int HOURS_TO_MEDIUM_HIGH = 4;
    private const int HOURS_TO_MEDIUM_LOW = 6;
    private const float HUNGER_UPDATE_INTERVAL = 2 * 3600f;

    private Coroutine hungerCoroutine;

    void Awake()
    {
        RestoreHungerState();
    }

    void Start()
    {
        if (PulseSpawner.Instance == null)
        {
            GameLogger.LogError(LogCategory.Dog,"PulseSpawner.Instance is null. Ensure PulseSpawner is added to the scene and initialized.");
            return;
        }
        StartHungerCoroutine();
        // Awake時点ではPulseSpawnerが未初期化の可能性があるため、ここで初期表示を確定
        RefreshPulseDisplay();
    }

    void OnDestroy()
    {
        StopHungerCoroutine();
    }

    /// <summary>
    /// 空腹処理のコルーチンを開始
    /// </summary>
    private void StartHungerCoroutine()
    {
        StopHungerCoroutine();
        hungerCoroutine = StartCoroutine(HungerLoop());
        GameLogger.Log(LogCategory.Dog,"空腹度管理のコルーチンを開始しました。");
    }

    /// <summary>
    /// 空腹処理のコルーチンを停止
    /// </summary>
    private void StopHungerCoroutine()
    {
        if (hungerCoroutine != null)
        {
            StopCoroutine(hungerCoroutine);
            hungerCoroutine = null;
            GameLogger.Log(LogCategory.Dog,"空腹度管理のコルーチンを停止しました。");
        }
    }

    /// <summary>
    /// 空腹度を定期的に減少させるループ
    /// </summary>
    private IEnumerator HungerLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(HUNGER_UPDATE_INTERVAL);

            if (GlobalVariables.CurrentHungerState != HungerState.Hungry && GlobalVariables.CurrentState != PetState.sleeping)
            {
                DecreaseHungerState();
            }
            else
            {
                GameLogger.Log(LogCategory.Dog,"空腹度はすでに最大か寝ているかです。次回のチェックまで待機します。");
            }
        }
    }

    /// <summary>
    /// 空腹状態と最終エサやり時間をローカルに保存
    /// </summary>
    private void SaveHungerState()
    {
        PlayerPrefs.SetInt(HUNGER_STATE_KEY, (int)GlobalVariables.CurrentHungerState);
        PlayerPrefs.SetString(LAST_EAT_TIME, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        PlayerPrefs.Save();
    }

    /// <summary>
    /// ローカルから空腹状態と最終エサやり時間を復元し、現在の空腹状態を計算
    /// </summary>
    private void RestoreHungerState()
    {
        if (PlayerPrefs.HasKey(HUNGER_STATE_KEY) && PlayerPrefs.HasKey(LAST_EAT_TIME))
        {
            int savedHungerState = PlayerPrefs.GetInt(HUNGER_STATE_KEY);
            long lastEatTime = long.Parse(PlayerPrefs.GetString(LAST_EAT_TIME));
            CalculateHungerState(lastEatTime);
        }
        else
        {
            GlobalVariables.CurrentHungerState = HungerState.Hungry;
            SaveHungerState();
        }
    }

    /// <summary>
    /// 空腹状態をHungryに強制変更（夜間睡眠からの起床時）
    /// </summary>
    public void ForceHungry()
    {
        SetHungerState(HungerState.Hungry);
        StartHungerCoroutine();
    }

    /// <summary>
    /// 空腹状態を+1進める（満腹方向）
    /// </summary>
    public void IncreaseHungerState()
    {
        if (GlobalVariables.CurrentHungerState == HungerState.Full)
        {
            return;
        }
        int currentIndex = (int)GlobalVariables.CurrentHungerState;
        SetHungerState((HungerState)(currentIndex - 1));

        // 餌を与えた後、コルーチンを再スタートして次のサイクルをリセット
        StartHungerCoroutine();
    }

    /// <summary>
    /// 空腹状態を-1進める（空腹方向）
    /// </summary>
    private void DecreaseHungerState()
    {
        if (GlobalVariables.CurrentHungerState == HungerState.Hungry)
        {
            GameLogger.Log(LogCategory.Dog,"空腹度はすでに最大です。");
            return;
        }
        int currentIndex = (int)GlobalVariables.CurrentHungerState;
        SetHungerState((HungerState)(currentIndex + 1));
        GameLogger.Log(LogCategory.Dog,$"空腹度が減少しました。現在の状態: {GlobalVariables.CurrentHungerState}");
    }

    /// <summary>
    /// 現在の空腹状態をセット
    /// </summary>
    private void SetHungerState(HungerState newState)
    {
        GlobalVariables.CurrentHungerState = newState;
        hungerStatusChangedEvent.Raise(GlobalVariables.CurrentHungerStateIndex);
        SaveHungerState();
        RefreshPulseDisplay();
    }

    /// <summary>
    /// 現在の空腹状態に基づいてパルス表示を更新
    /// Hungryなら表示、それ以外なら非表示
    /// </summary>
    public void RefreshPulseDisplay()
    {
        if (PulseSpawner.Instance == null) return;

        if (GlobalVariables.CurrentHungerState == HungerState.Hungry)
        {
            PulseSpawner.Instance.ShowPulse(-100f, -100f, 2.5f, 1.8f);
        }
        else
        {
            PulseSpawner.Instance.HidePulse();
        }
    }

    /// <summary>
    /// 現在のタイムスタンプから空腹状態を計算
    /// </summary>
    private void CalculateHungerState(long timestamp)
    {
        long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long hoursElapsed = (currentTimestamp - timestamp) / 3600;

        HungerState newState;
        if (hoursElapsed < HOURS_TO_FULL)
        {
            newState = HungerState.Full;
        }
        else if (hoursElapsed < HOURS_TO_MEDIUM_HIGH)
        {
            newState = HungerState.MediumHigh;
        }
        else if (hoursElapsed < HOURS_TO_MEDIUM_LOW)
        {
            newState = HungerState.MediumLow;
        }
        else
        {
            newState = HungerState.Hungry;
        }

        SetHungerState(newState);
    }

    /// <summary>
    /// デバッグ用：空腹状態を手動で設定
    /// </summary>
    [ContextMenu("Set Hunger State to Hungry")]
    public void DebugSetHungerToHungry()
    {
        SetHungerState(HungerState.Hungry);
        GameLogger.Log(LogCategory.Dog,"空腹状態がHungryに設定されました。");
    }

    [ContextMenu("Set Hunger State to Full")]
    public void DebugSetHungerToFull()
    {
        SetHungerState(HungerState.Full);
        StartHungerCoroutine(); // フルに戻したら新しいサイクルを開始
        GameLogger.Log(LogCategory.Dog,"空腹状態がFullに設定されました。");
    }

    /// <summary>
    /// 最後に食べた時間を更新し、PlayerPrefsに保存
    /// </summary>
    public void UpdateLastEatTime(long newEatTime)
    {
        long lastEatTime = PlayerPrefs.HasKey(LAST_EAT_TIME)
            ? long.Parse(PlayerPrefs.GetString(LAST_EAT_TIME))
            : 0;

        if (newEatTime > lastEatTime)
        {
            CalculateHungerState(newEatTime);
            StartHungerCoroutine(); // 新しいサイクルを開始
        }
    }
}