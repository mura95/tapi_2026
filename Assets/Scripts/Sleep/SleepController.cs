using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TapHouse.Logging;

/// <summary>
/// SleepController - デバッグログ強化版 + 自動3サイクルテスト対応
/// アラームの動作状況を詳細に記録し、自動テストも実行可能
/// </summary>
public partial class SleepController : MonoBehaviour
{
    private IAlarmScheduler _alarm;
    private IPowerService _power;
    private DailyWakeScheduler _dailyWake;

    [Header("自動テストモード")]
    [SerializeField] private bool _autoTestMode = false;
    [SerializeField] private int _testCycleCount = 3;
    [SerializeField] private float _sleepDelayMinutes = 2f;
    [SerializeField] private float _wakeDelayMinutes = 3f;

    private int _currentTestCycle = 0;
    private bool _isTestRunning = false;
    public bool IsAutoTestMode => _autoTestMode;

    // 参照
    [SerializeField] private DogController _dogController;
    [SerializeField] private FirebaseManager _firebaseManager;
    [SerializeField] private HungerManager _hungryManager;
    [SerializeField] private FirebaseUIManager _firebaseUIManager;
    [SerializeField] private MainUIButtons _mainUiButtons;
    [SerializeField] private DebugCanvasManager _debugCanvasManager;

    // 時刻系
    private const string SLEEP_HOUR_KEY = "SleepHour";
    private const string WAKE_HOUR_KEY = "WakeHour";

    // スケジュール用Coroutine
    private Coroutine _sleepScheduleCoroutine;
    private Coroutine _wakeScheduleCoroutine;

    // TimeZoneProviderからアクセス（後方互換性のため）
    private TimeZoneInfo selectedTimeZone => TimeZoneProvider.Current;
    private List<TimeZoneInfo> timeZones => TimeZoneProvider.AvailableTimeZones;

    private DateTime? nextScheduledSleepTime = null;
    public DateTime? NextScheduledSleepTime => nextScheduledSleepTime;

    private DateTime? nextScheduledWakeTime = null;
    public DateTime? NextScheduledWakeTime => nextScheduledWakeTime;

    void Awake()
    {
        GameLogger.Log(LogCategory.Sleep, "========================================");
        GameLogger.Log(LogCategory.Sleep, "Awake() called");
        GameLogger.Log(LogCategory.Sleep, "========================================");

#if UNITY_ANDROID && !UNITY_EDITOR
        GameLogger.Log(LogCategory.Sleep, "Platform: Android (Real Device)");
        _alarm = new AndroidAlarmScheduler();
        _power = new AndroidPowerService();
#else
        GameLogger.Log(LogCategory.Sleep, "Platform: Editor");
        _alarm = new EditorAlarmScheduler(this);
        _power = new NoopPowerService();
#endif

        GameLogger.Log(LogCategory.Sleep, $"Alarm scheduler: {_alarm.GetType().Name}");
        GameLogger.Log(LogCategory.Sleep, $"Power service: {_power.GetType().Name}");
        GameLogger.Log(LogCategory.Sleep, $"Auto Test Mode: {_autoTestMode}");
    }

    void Start()
    {
        GameLogger.Log(LogCategory.Sleep, "========================================");
        GameLogger.Log(LogCategory.Sleep, "Start() called");
        GameLogger.Log(LogCategory.Sleep, "========================================");

        // Android 12以降: アラーム権限を起動時にチェック
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!AndroidAlarmScheduler.EnsureAlarmPermissionOnStartup())
        {
            GameLogger.LogWarning(LogCategory.Sleep, "Alarm permission not granted. User directed to settings.");
        }
#endif

        StartOverdueMonitoring();
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        GameLogger.Log(LogCategory.Sleep, "Screen sleep timeout set to NeverSleep");

        // TimeZoneProviderで一元管理
        TimeZoneProvider.Initialize((idx, tzList) =>
        {
            _firebaseUIManager.setDropdownValue(idx, tzList);
        });
        GameLogger.Log(LogCategory.Sleep, $"Selected TimeZone: {selectedTimeZone.DisplayName}");
        GameLogger.Log(LogCategory.Sleep, $"TimeZone ID: {selectedTimeZone.Id}");
        GameLogger.Log(LogCategory.Sleep, $"Current offset: {selectedTimeZone.GetUtcOffset(DateTime.UtcNow)}");

        int sleepHour = PlayerPrefs.GetInt(SLEEP_HOUR_KEY, 22);
        int wakeHour = PlayerPrefs.GetInt(WAKE_HOUR_KEY, 6);
        GameLogger.Log(LogCategory.Sleep, $"Sleep hour: {sleepHour}");
        GameLogger.Log(LogCategory.Sleep, $"Wake hour: {wakeHour}");

        if (_autoTestMode)
        {
            GameLogger.Log(LogCategory.Sleep, "========================================");
            GameLogger.LogWarning(LogCategory.Sleep, "AUTO TEST MODE ACTIVE");
            GameLogger.Log(LogCategory.Sleep, $"Test cycles: {_testCycleCount}");
            GameLogger.Log(LogCategory.Sleep, $"Sleep delay: {_sleepDelayMinutes} minutes");
            GameLogger.Log(LogCategory.Sleep, $"Wake delay: {_wakeDelayMinutes} minutes");
            GameLogger.Log(LogCategory.Sleep, "========================================");

            _debugCanvasManager.ShowDebugCanvas();
            StartCoroutine(AutoTestCycle());
        }
        else
        {
            GameLogger.Log(LogCategory.Sleep, "Normal mode");
            _dailyWake = new DailyWakeScheduler(_alarm, selectedTimeZone);
            _dailyWake.EnsureDailyWakeAtHour();
            ManageSleepCycle(selectedTimeZone);
        }

        // 昼寝機能の初期化
        InitializeNap();
        ResetAllSchedules();
        GameLogger.Log(LogCategory.Sleep, "Nap functionality initialized");

        GameLogger.Log(LogCategory.Sleep, "========================================");
        GameLogger.Log(LogCategory.Sleep, "Start() completed");
        GameLogger.Log(LogCategory.Sleep, "========================================");
    }

    /// <summary>
    /// 自動テストサイクルを実行（3回のスリープ→起床を自動実行）
    /// </summary>
    private IEnumerator AutoTestCycle()
    {
        _isTestRunning = true;
        DateTime startTime = TimeZoneProvider.Now;

        GameLogger.Log(LogCategory.Sleep, "========================================");
        GameLogger.Log(LogCategory.Sleep, $"[AutoTest] Starting automatic test cycle");
        GameLogger.Log(LogCategory.Sleep, $"[AutoTest] Start time: {startTime:yyyy-MM-dd HH:mm:ss}");
        GameLogger.Log(LogCategory.Sleep, "========================================");

        for (int cycle = 1; cycle <= _testCycleCount; cycle++)
        {
            _currentTestCycle = cycle;

            GameLogger.Log(LogCategory.Sleep, "========================================");
            GameLogger.Log(LogCategory.Sleep, $"[AutoTest] CYCLE {cycle}/{_testCycleCount} START");
            GameLogger.Log(LogCategory.Sleep, "========================================");

            // 現在時刻を取得
            DateTime now = TimeZoneProvider.Now;

            // スリープ時刻と起床時刻を計算
            DateTime sleepTime = now.AddMinutes(_sleepDelayMinutes);
            DateTime wakeTime = now.AddMinutes(_sleepDelayMinutes + _wakeDelayMinutes);

            GameLogger.Log(LogCategory.Sleep, $"[AutoTest] Current time: {now:HH:mm:ss}");
            GameLogger.Log(LogCategory.Sleep, $"[AutoTest] Sleep scheduled: {sleepTime:HH:mm:ss} (+{_sleepDelayMinutes:F0} min)");
            GameLogger.Log(LogCategory.Sleep, $"[AutoTest] Wake scheduled: {wakeTime:HH:mm:ss} (+{(_sleepDelayMinutes + _wakeDelayMinutes):F0} min)");

            // スリープをスケジュール
            float sleepDelaySeconds = (float)(sleepTime - now).TotalSeconds;

            GameLogger.Log(LogCategory.Sleep, $"[AutoTest] Starting sleep in {sleepDelaySeconds:F1} seconds");

            CancelInvoke(nameof(StartSleepingBridge));
            Invoke(nameof(StartSleepingBridge), sleepDelaySeconds);

            // スリープが実行されるまで待つ
            yield return new WaitForSeconds(sleepDelaySeconds + 5f); // 5秒のバッファ

            // スリープが実行されたか確認
            if (GlobalVariables.CurrentState == PetState.sleeping)
            {
                GameLogger.Log(LogCategory.Sleep, $"[AutoTest] Cycle {cycle}: Sleep successful");
            }
            else
            {
                GameLogger.LogWarning(LogCategory.Sleep, $"[AutoTest] Cycle {cycle}: Sleep may have failed");
            }

            // 起床時刻まで待つ
            float wakeWaitSeconds = (float)(wakeTime - TimeZoneProvider.Now).TotalSeconds;

            if (wakeWaitSeconds > 0)
            {
                GameLogger.Log(LogCategory.Sleep, $"[AutoTest] Waiting {wakeWaitSeconds:F1} seconds for wake alarm...");
                yield return new WaitForSeconds(wakeWaitSeconds + 10f); // 10秒のバッファ
            }

            // 起床が実行されたか確認
            if (GlobalVariables.CurrentState == PetState.idle)
            {
                GameLogger.Log(LogCategory.Sleep, $"[AutoTest] Cycle {cycle}: Wake successful");
            }
            else
            {
                GameLogger.LogWarning(LogCategory.Sleep, $"[AutoTest] Cycle {cycle}: Wake may have failed, current state: {GlobalVariables.CurrentState}");

                // 手動で起床を試みる
                GameLogger.Log(LogCategory.Sleep, $"[AutoTest] Attempting manual wake...");
                WakeUp();
            }

            GameLogger.Log(LogCategory.Sleep, "========================================");
            GameLogger.Log(LogCategory.Sleep, $"[AutoTest] CYCLE {cycle}/{_testCycleCount} COMPLETE");
            GameLogger.Log(LogCategory.Sleep, "========================================");

            // 次のサイクルまで少し待つ（状態安定のため）
            if (cycle < _testCycleCount)
            {
                GameLogger.Log(LogCategory.Sleep, $"[AutoTest] Preparing for next cycle...");
                yield return new WaitForSeconds(5f);
            }
        }

        DateTime endTime = TimeZoneProvider.Now;
        TimeSpan totalDuration = endTime - startTime;

        GameLogger.Log(LogCategory.Sleep, "========================================");
        GameLogger.Log(LogCategory.Sleep, $"[AutoTest] ALL CYCLES COMPLETED");
        GameLogger.Log(LogCategory.Sleep, $"[AutoTest] Start time: {startTime:HH:mm:ss}");
        GameLogger.Log(LogCategory.Sleep, $"[AutoTest] End time: {endTime:HH:mm:ss}");
        GameLogger.Log(LogCategory.Sleep, $"[AutoTest] Total duration: {totalDuration.TotalMinutes:F1} minutes");
        GameLogger.Log(LogCategory.Sleep, $"[AutoTest] Cycles completed: {_testCycleCount}");
        GameLogger.Log(LogCategory.Sleep, "========================================");

        _isTestRunning = false;

        // テスト完了後、通常モードに戻す
        GameLogger.Log(LogCategory.Sleep, "[AutoTest] Switching to normal mode...");
        _dailyWake = new DailyWakeScheduler(_alarm, selectedTimeZone);
        _dailyWake.EnsureDailyWakeAtHour();

        int sleepHour = PlayerPrefs.GetInt(SLEEP_HOUR_KEY, 22);
        ManageSleepCycle(selectedTimeZone);

        GameLogger.Log(LogCategory.Sleep, "[AutoTest] Test completed. App is now in normal mode.");
    }

    // Invokeで引数を渡せないのでブリッジ
    private void StartSleepingBridge() => StartSleeping(selectedTimeZone);

    public void StartSleeping(TimeZoneInfo tz = null)
    {
        GameLogger.Log(LogCategory.Sleep, "========================================");
        GameLogger.Log(LogCategory.Sleep, "StartSleeping() called");
        if (_autoTestMode && _isTestRunning)
        {
            GameLogger.Log(LogCategory.Sleep, $"Auto Test Mode - Cycle {_currentTestCycle}/{_testCycleCount}");
        }
        GameLogger.Log(LogCategory.Sleep, "========================================");

        if (_dogController.GetSleeping())
        {
            GameLogger.Log(LogCategory.Sleep, "Dog is already sleeping. Exiting StartSleeping().");
            return;
        }

        _dogController.UpdateTransitionState(1);
        GlobalVariables.CurrentState = PetState.sleeping;
        _dogController.Sleeping(true);
        _firebaseManager.UpdatePetState("sleeping");
        _mainUiButtons.UpdateButtonVisibility(false);
        GameLogger.Log(LogCategory.Sleep, "UI updated for sleep mode");

        tz ??= TimeZoneProvider.Current;
        DateTime now = TimeZoneInfo.ConvertTime(DateTime.UtcNow, tz);
        GameLogger.Log(LogCategory.Sleep, $"Current time: {now:yyyy-MM-dd HH:mm:ss} ({tz.Id})");

        // 画面を暗くする
        BrightnessUtil.SetBrightness.DoAction(-1f);
        GameLogger.Log(LogCategory.Sleep, "Brightness reduced");

        // 起床時刻を計算
        DateTime nextWake;

        if (_autoTestMode && _isTestRunning)
        {
            // 自動テストモード: 現在時刻 + _wakeDelayMinutes
            nextWake = now.AddMinutes(_wakeDelayMinutes);
            GameLogger.Log(LogCategory.Sleep, $"Auto Test: Wake scheduled at {nextWake:HH:mm:ss} (+{_wakeDelayMinutes:F0} min)");
        }
        else
        {
            // 通常モード
            int wakeHour = PlayerPrefs.GetInt(WAKE_HOUR_KEY, 6);
            nextWake = new DateTime(now.Year, now.Month, now.Day, wakeHour, 0, 0);

            if (now >= nextWake)
            {
                nextWake = nextWake.AddDays(1);
                GameLogger.Log(LogCategory.Sleep, "Wake time is past today, moved to tomorrow");
            }
        }

        GameLogger.Log(LogCategory.Sleep, $"Scheduling wake at: {nextWake:yyyy-MM-dd HH:mm:ss}");
        _alarm.ScheduleWakeAt(nextWake, tz);
        nextScheduledWakeTime = nextWake;
        nextScheduledSleepTime = null;

        Screen.sleepTimeout = SleepTimeout.SystemSetting;
        _power.ClearKeepScreenOn();
        GameLogger.Log(LogCategory.Sleep, "StartSleeping() completed");
    }

    public void WakeUp()
    {
        GameLogger.Log(LogCategory.Sleep, "========================================");
        GameLogger.Log(LogCategory.Sleep, "WakeUp() called");
        if (_autoTestMode && _isTestRunning)
        {
            GameLogger.Log(LogCategory.Sleep, $"Auto Test Mode - Cycle {_currentTestCycle}/{_testCycleCount}");
        }
        GameLogger.Log(LogCategory.Sleep, "========================================");

        _power.TurnScreenOn();
        GameLogger.Log(LogCategory.Sleep, "Screen turned on");

        if (_dogController.GetSleeping())
        {
            GlobalVariables.CurrentState = PetState.idle;
            _dogController.Sleeping(false);
            _firebaseManager.UpdatePetState("idle");
            _mainUiButtons.UpdateButtonVisibility(true);
            GameLogger.Log(LogCategory.Sleep, "UI updated for wake mode");

            // 夜間睡眠からの起床時は空腹状態をHungryに強制変更
            if (_hungryManager != null)
            {
                _hungryManager.ForceHungry();
                GameLogger.Log(LogCategory.Sleep, "Hunger forced to Hungry on night wake");
            }
        }
        else
        {
            GameLogger.Log(LogCategory.Sleep, $"State was not sleeping (current: {GlobalVariables.CurrentState})");
        }

        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        GameLogger.Log(LogCategory.Sleep, "Screen sleep disabled (NeverSleep)");

        BrightnessUtil.SetBrightness.DoAction(1.0f);
        GameLogger.Log(LogCategory.Sleep, "Brightness restored");

        // 自動テストモードでない場合のみ次回スケジュールを設定
        if (!_autoTestMode || !_isTestRunning)
        {
            // 次回のスリープスケジュールを設定
            int sleepHour = PlayerPrefs.GetInt(SLEEP_HOUR_KEY, 22);
            GameLogger.Log(LogCategory.Sleep, $"Scheduling next sleep at {sleepHour}:00");
            ScheduleSleep(sleepHour, selectedTimeZone);
            nextScheduledWakeTime = null;
        }
        else
        {
            GameLogger.Log(LogCategory.Sleep, "Auto Test Mode: Skipping next sleep schedule (handled by AutoTestCycle)");
        }

        GameLogger.Log(LogCategory.Sleep, "========================================");
        GameLogger.Log(LogCategory.Sleep, "WakeUp() completed");
        GameLogger.Log(LogCategory.Sleep, "========================================");
    }

    public void TryAutoWake(string source)
    {
        GameLogger.Log(LogCategory.Sleep, $"[TryAutoWake] Source: {source}");

        if (_dogController == null)
        {
            GameLogger.LogWarning(LogCategory.Sleep, "[TryAutoWake] DogController is null");
            return;
        }

        var now = TimeZoneProvider.Now;

        // 条件：現在が起床時間を過ぎている AND ペットがまだ寝ているなら起こす
        if (now >= nextScheduledWakeTime && _dogController.GetSleeping())
        {
            GameLogger.Log(LogCategory.Sleep, "[TryAutoWake] 条件を満たしたため WakeUp() を実行します");
            WakeUp();
        }
        else
        {
            GameLogger.Log(LogCategory.Sleep, "[TryAutoWake] WakeUp条件を満たしていません");
        }
    }


    /// <summary>
    /// 睡眠スケジュールのみをキャンセル
    /// </summary>
    public void CancelSleepSchedule()
    {
        GameLogger.Log(LogCategory.Sleep, "CancelSleepSchedule() called");
        CancelSleepCoroutine();
        CancelWakeCoroutine();
        CancelNapSchedule();
        nextScheduledSleepTime = null;
        nextScheduledWakeTime = null;
        GameLogger.Log(LogCategory.Sleep, "All sleep schedules cancelled");
    }

    void OnApplicationPause(bool pause)
    {
        GameLogger.Log(LogCategory.Sleep, "========================================");
        GameLogger.Log(LogCategory.Sleep, $"OnApplicationPause: {pause}");
        GameLogger.Log(LogCategory.Sleep, $"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        GameLogger.Log(LogCategory.Sleep, "========================================");

        if (!pause)
        {
            GameLogger.Log(LogCategory.Sleep, "App resumed from pause");
            TryAutoWake("resume");
        }
        else
        {
            GameLogger.Log(LogCategory.Sleep, "App paused");
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        GameLogger.Log(LogCategory.Sleep, "========================================");
        GameLogger.Log(LogCategory.Sleep, $"OnApplicationFocus: {hasFocus}");
        GameLogger.Log(LogCategory.Sleep, $"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        GameLogger.Log(LogCategory.Sleep, "========================================");

        if (hasFocus)
        {
            GameLogger.Log(LogCategory.Sleep, "App gained focus");
            TryAutoWake("focus");
        }
        else
        {
            GameLogger.Log(LogCategory.Sleep, "App lost focus");
        }
    }

    private void OnDestroy()
    {
        GameLogger.Log(LogCategory.Sleep, "OnDestroy() called");
        CancelSleepCoroutine();
        CancelWakeCoroutine();
        CancelNapSchedule();
        Screen.sleepTimeout = SleepTimeout.SystemSetting;

        if (_power != null)
        {
            _power.Release();
        }

        if (_autoTestMode && _isTestRunning)
        {
            StopAllCoroutines();
            GameLogger.Log(LogCategory.Sleep, "Auto test stopped");
        }

        nextScheduledSleepTime = null;
        nextScheduledWakeTime = null;
        StopOverdueMonitoring();
        GameLogger.Log(LogCategory.Sleep, "Cleanup completed");
    }

    public void UpdateTimeZone(TimeZoneInfo newTz)
    {
        TimeZoneProvider.Current = newTz;
        ResetAllSchedules(newTz);
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private void RequestIgnoreBatteryOptimizations()
    {
        GameLogger.Log(LogCategory.Sleep, "RequestIgnoreBatteryOptimizations() called");

        try
        {
            var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            var pm = activity.Call<AndroidJavaObject>("getSystemService", "power");
            var pkg = activity.Call<string>("getPackageName");

            bool isIgnoring = pm.Call<bool>("isIgnoringBatteryOptimizations", pkg);
            GameLogger.Log(LogCategory.Sleep, $"[Battery] isIgnoringBatteryOptimizations: {isIgnoring}");

            if (!isIgnoring)
            {
                var intent = new AndroidJavaObject("android.content.Intent");
                intent.Call<AndroidJavaObject>("setAction", "android.settings.REQUEST_IGNORE_BATTERY_OPTIMIZATIONS");
                var uri = new AndroidJavaClass("android.net.Uri")
                    .CallStatic<AndroidJavaObject>("parse", "package:" + pkg);
                intent.Call<AndroidJavaObject>("setData", uri);
                activity.Call("startActivity", intent);
                GameLogger.Log(LogCategory.Sleep, "[Battery] Requested battery optimization exemption");
            }
            else
            {
                GameLogger.Log(LogCategory.Sleep, "[Battery] Already ignoring battery optimizations");
            }
        }
        catch (System.Exception e)
        {
            GameLogger.LogWarning(LogCategory.Sleep, $"[Battery] RequestIgnoreBatteryOptimizations failed: {e.Message}");
        }
    }
#endif
}