using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TapHouse.Logging;

public partial class SleepController : MonoBehaviour
{

    /// <summary>
    /// 睡眠のスケジュールを設定
    /// </summary>
    private void ScheduleSleep(int sleepHour = 22, TimeZoneInfo tz = null, DateTime? now = null)
    {
        tz ??= TimeZoneProvider.Current;
        now ??= TimeZoneProvider.Now;

        // 既存のスケジュールをキャンセル
        CancelSleepCoroutine();

        DateTime sleepTime = new DateTime(now.Value.Year, now.Value.Month, now.Value.Day, sleepHour, 0, 0);

        if (now > sleepTime)
        {
            sleepTime = sleepTime.AddDays(1);
        }

        // TimeSpanの差分を計算
        TimeSpan timeDifference = sleepTime - now.Value;
        float secondsUntilSleep = (float)timeDifference.TotalSeconds;

        // Coroutineでスケジュール（WaitForSecondsRealtimeを使用、バックグラウンドでも動作）
        _sleepScheduleCoroutine = StartCoroutine(ScheduleCoroutine(secondsUntilSleep, StartSleepingBridge));

        // 次回睡眠予定時間を保存（デバッグ用）
        nextScheduledSleepTime = sleepTime;
        GameLogger.Log(LogCategory.Sleep, $"次のスリープスケジュール: {sleepTime} (残り {secondsUntilSleep} 秒)");
    }

    /// <summary>
    /// 汎用スケジュールCoroutine（WaitForSecondsRealtimeを使用）
    /// </summary>
    private IEnumerator ScheduleCoroutine(float seconds, Action callback)
    {
        yield return new WaitForSecondsRealtime(seconds);
        callback?.Invoke();
    }

    /// <summary>
    /// 睡眠スケジュールCoroutineをキャンセル
    /// </summary>
    private void CancelSleepCoroutine()
    {
        if (_sleepScheduleCoroutine != null)
        {
            StopCoroutine(_sleepScheduleCoroutine);
            _sleepScheduleCoroutine = null;
        }
    }

    /// <summary>
    /// 起床スケジュールCoroutineをキャンセル
    /// </summary>
    private void CancelWakeCoroutine()
    {
        if (_wakeScheduleCoroutine != null)
        {
            StopCoroutine(_wakeScheduleCoroutine);
            _wakeScheduleCoroutine = null;
        }
    }

    public void ResetAllSchedules(TimeZoneInfo tz = null)
    {
        tz ??= TimeZoneProvider.Current;

        // すべてのスケジュールをキャンセル
        CancelSleepCoroutine();
        CancelWakeCoroutine();
        CancelInvoke(nameof(OnNapEndTimeReached));

        if (_sleepyReactionCoroutine != null)
        {
            StopCoroutine(_sleepyReactionCoroutine);
            _sleepyReactionCoroutine = null;
            _isInSleepyReaction = false;
        }

        // 現在の状態に基づいて次のスケジュールを設定
        ManageSleepCycle(tz);
    }

    public void ManageSleepCycle(TimeZoneInfo tz)
    {
        if (tz == null)
        {
            GameLogger.LogError(LogCategory.Sleep, "[ManageSleepCycle] tz が null です！");
            tz = TimeZoneProvider.Current;
        }

        DateTime now = TimeZoneInfo.ConvertTime(DateTime.UtcNow, tz);
        int sleepHour = PlayerPrefs.GetInt(SLEEP_HOUR_KEY, 22);
        int wakeHour = PlayerPrefs.GetInt(WAKE_HOUR_KEY, 6);

        bool isSleepTime = (now.Hour >= sleepHour || now.Hour < wakeHour);
        bool isSleeping = _dogController != null && _dogController.GetSleeping();

        GameLogger.Log(LogCategory.Sleep, $"[ManageSleepCycle] 現在時刻: {now.Hour}時 / 寝る時間帯か？ {isSleepTime} / ペットは寝ているか？ {isSleeping}");

        if (isSleepTime && !isSleeping)
        {
            GameLogger.Log(LogCategory.Sleep, "[ManageSleepCycle] 現在は就寝時間で、ペットが起きているので StartSleeping() を呼びます");
            StartSleeping(tz);
        }
        else if (!isSleepTime && isSleeping)
        {
            GameLogger.Log(LogCategory.Sleep, "[ManageSleepCycle] 現在は起床時間で、ペットが寝ているので WakeUp() を呼びます");
            WakeUp();
        }
        else if (!isSleepTime)
        {
            // 起きている時間 → 次の sleep のみ予約
            GameLogger.Log(LogCategory.Sleep, "[ManageSleepCycle] 起きている時間帯なので、次回の睡眠スケジュールを組みます");
            ScheduleSleep(sleepHour, tz, now);
        }
        // 寝ている時間中（isSleepTime && isSleeping）は何もしない
        // → AndroidアラームがWakeUpを担当
    }
}
