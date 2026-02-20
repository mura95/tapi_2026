using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TapHouse.Logging;

/// <summary>
/// SleepController - Overdue監視機能
/// スケジュールのOverdue状態を監視し、必要に応じて自動修正を行う
/// </summary>
public partial class SleepController
{
    [Header("Overdue Detection")]
    [SerializeField] private float _overdueCheckIntervalSeconds = 60f; // チェック間隔
    [SerializeField] private float _overdueThresholdMinutes = 10f; // Overdue判定の閾値

    private Coroutine _overdueCheckCoroutine;
    private DateTime? _overdueDetectedTime = null;

    /// <summary>
    /// Overdue監視を開始
    /// </summary>
    private void StartOverdueMonitoring()
    {
        if (_overdueCheckCoroutine != null)
        {
            StopCoroutine(_overdueCheckCoroutine);
        }
        _overdueCheckCoroutine = StartCoroutine(MonitorOverdueStatus());
        GameLogger.Log(LogCategory.Sleep, "Overdue monitoring started");
    }

    /// <summary>
    /// Overdue監視を停止
    /// </summary>
    private void StopOverdueMonitoring()
    {
        if (_overdueCheckCoroutine != null)
        {
            StopCoroutine(_overdueCheckCoroutine);
            _overdueCheckCoroutine = null;
            _overdueDetectedTime = null;
            GameLogger.Log(LogCategory.Sleep, "Overdue monitoring stopped");
        }
    }

    /// <summary>
    /// Overdue状態を定期的に監視
    /// </summary>
    private IEnumerator MonitorOverdueStatus()
    {
        GameLogger.Log(LogCategory.Sleep, "[OverdueMonitor] Monitoring started");

        while (true)
        {
            yield return new WaitForSeconds(_overdueCheckIntervalSeconds);

            // 自動テストモード中はスキップ
            if (_autoTestMode && _isTestRunning)
            {
                continue;
            }

            CheckAndHandleOverdue();
        }
    }

    /// <summary>
    /// Overdue状態をチェックして必要に応じて対処
    /// </summary>
    private void CheckAndHandleOverdue()
    {
        DateTime now = TimeZoneProvider.Now;

        // Sleep Overdue チェック
        CheckSleepOverdue(now);

        // Wake Overdue チェック
        CheckWakeOverdue(now);
    }

    /// <summary>
    /// Sleep Overdue状態をチェック
    /// </summary>
    private void CheckSleepOverdue(DateTime now)
    {
        if (!nextScheduledSleepTime.HasValue) return;

        TimeSpan timeSinceSleep = now - nextScheduledSleepTime.Value;

        if (timeSinceSleep.TotalMinutes >= _overdueThresholdMinutes)
        {
            bool isSleeping = _dogController.GetSleeping();

            if (!isSleeping)
            {
                GameLogger.LogWarning(LogCategory.Sleep, "[OverdueMonitor] Executing missed sleep NOW");
                nextScheduledSleepTime = null; // 重複実行防止のため先にクリア
                StartSleepingBridge();
            }
            else
            {
                GameLogger.Log(LogCategory.Sleep, "[OverdueMonitor] Already sleeping, clearing overdue flag");
                nextScheduledSleepTime = null;
            }
        }
    }

    /// <summary>
    /// Wake Overdue状態をチェック
    /// </summary>
    private void CheckWakeOverdue(DateTime now)
    {
        // 起床時刻が未設定、または既に起きている場合はチェック不要
        if (!nextScheduledWakeTime.HasValue)
        {
            return;
        }

        TimeSpan timeSinceWake = now - nextScheduledWakeTime.Value;

        if (timeSinceWake.TotalMinutes > 0) // Overdue状態
        {
            GameLogger.LogWarning(LogCategory.Sleep, $"[OverdueMonitor] Wake time passed but still sleeping!");
            GameLogger.LogWarning(LogCategory.Sleep, $"[OverdueMonitor] Scheduled: {nextScheduledWakeTime.Value:HH:mm:ss}");
            GameLogger.LogWarning(LogCategory.Sleep, $"[OverdueMonitor] Current: {now:HH:mm:ss}");
            GameLogger.LogWarning(LogCategory.Sleep, $"[OverdueMonitor] Overdue by: {timeSinceWake.TotalMinutes:F1} minutes");
            GameLogger.LogWarning(LogCategory.Sleep, $"[OverdueMonitor] Current State: {GlobalVariables.CurrentState}");

            if (timeSinceWake.TotalMinutes >= _overdueThresholdMinutes)
            {
                GameLogger.LogError(LogCategory.Sleep, "========================================");
                GameLogger.LogError(LogCategory.Sleep, $"[OverdueMonitor] Wake Overdue threshold exceeded!");
                GameLogger.LogError(LogCategory.Sleep, $"[OverdueMonitor] Forcing wake up...");
                GameLogger.LogError(LogCategory.Sleep, "========================================");

                nextScheduledWakeTime = null; // 重複実行防止のため先にクリア
                WakeUp();
            }
        }
    }

    /// <summary>
    /// 次回のSleep時刻が正常範囲内かチェック
    /// </summary>
    public bool IsSleepScheduleValid()
    {
        if (!nextScheduledSleepTime.HasValue) return false;

        DateTime now = TimeZoneProvider.Now;
        TimeSpan timeUntil = nextScheduledSleepTime.Value - now;

        // 過去または24時間以上先の場合は異常
        return timeUntil.TotalSeconds > 0 && timeUntil.TotalHours < 24;
    }

    /// <summary>
    /// 次回のWake時刻が正常範囲内かチェック
    /// </summary>
    public bool IsWakeScheduleValid()
    {
        if (!nextScheduledWakeTime.HasValue) return false;

        DateTime now = TimeZoneProvider.Now;
        TimeSpan timeUntil = nextScheduledWakeTime.Value - now;

        // 過去または24時間以上先の場合は異常
        return timeUntil.TotalSeconds > 0 && timeUntil.TotalHours < 24;
    }

    /// <summary>
    /// 現在のOverdue状態を取得（デバッグ用）
    /// </summary>
    public (bool isSleepOverdue, double sleepOverdueMinutes, bool isWakeOverdue, double wakeOverdueMinutes) GetOverdueStatus()
    {
        DateTime now = TimeZoneProvider.Now;

        bool isSleepOverdue = false;
        double sleepOverdueMinutes = 0;

        if (nextScheduledSleepTime.HasValue)
        {
            TimeSpan timeSince = now - nextScheduledSleepTime.Value;
            if (timeSince.TotalMinutes > 0)
            {
                isSleepOverdue = true;
                sleepOverdueMinutes = timeSince.TotalMinutes;
            }
        }

        bool isWakeOverdue = false;
        double wakeOverdueMinutes = 0;

        if (nextScheduledWakeTime.HasValue && GlobalVariables.CurrentState == PetState.sleeping)
        {
            TimeSpan timeSince = now - nextScheduledWakeTime.Value;
            if (timeSince.TotalMinutes > 0)
            {
                isWakeOverdue = true;
                wakeOverdueMinutes = timeSince.TotalMinutes;
            }
        }

        return (isSleepOverdue, sleepOverdueMinutes, isWakeOverdue, wakeOverdueMinutes);
    }
}