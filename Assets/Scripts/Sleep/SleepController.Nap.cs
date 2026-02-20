using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TapHouse.Logging;

public partial class SleepController : MonoBehaviour
{
    private const int NAP_COUNT_MAX = 15;
    private const string NAP_START_HOUR_KEY = "NapSleepHour";
    private const string NAP_WAKE_HOUR_KEY = "NapWakeHour";
    private const string NAP_COUNT_KEY = "NapCount";
    private const string LAST_NAP_END_TIME_KEY = "LastNapEndTime";

    [SerializeField] private int NapCount = 0;
    private Coroutine _sleepyReactionCoroutine = null;
    private bool _isInSleepyReaction = false;

    public int CurrentNapCount => NapCount;

    public bool IsInSleepyReaction()
    {
        return _isInSleepyReaction;
    }

    private void InitializeNap()
    {
        NapCount = PlayerPrefs.GetInt(NAP_COUNT_KEY, 0);

        string lastEndTimeStr = PlayerPrefs.GetString(LAST_NAP_END_TIME_KEY, "");
        DateTime now = TimeZoneProvider.Now;

        if (!string.IsNullOrEmpty(lastEndTimeStr))
        {
            if (DateTime.TryParse(lastEndTimeStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime lastEndTime))
            {
                int napWakeHour = PlayerPrefs.GetInt(NAP_WAKE_HOUR_KEY, 15);

                // 今日のnapWakeHour時点を計算
                DateTime todayNapEnd = new DateTime(now.Year, now.Month, now.Day, napWakeHour, 0, 0);

                // 前回リセットから今日のnapWakeHourを超えていればリセット
                if (lastEndTime < todayNapEnd && now >= todayNapEnd)
                {
                    ResetNapCount("napWakeHour passed since last reset");
                }
            }
        }

        GameLogger.Log(LogCategory.Sleep, $"[Nap] Initialized: NapCount={NapCount}");
    }

    /// <summary>
    /// NapCountをリセットし、リセット時刻を保存
    /// </summary>
    private void ResetNapCount(string reason)
    {
        NapCount = 0;
        PlayerPrefs.SetInt(NAP_COUNT_KEY, 0);
        PlayerPrefs.SetString(LAST_NAP_END_TIME_KEY, TimeZoneProvider.Now.ToString("o"));
        PlayerPrefs.Save();
        GameLogger.Log(LogCategory.Sleep, $"[Nap] NapCount reset: {reason}");
    }

    public void OnYawn()
    {
        if (GlobalVariables.CurrentState == PetState.sleeping)
        {
            return;
        }

        if (NapCount < NAP_COUNT_MAX)
        {
            NapCount++;
            PlayerPrefs.SetInt(NAP_COUNT_KEY, NapCount);
            PlayerPrefs.Save();
            GameLogger.Log(LogCategory.Sleep, $"[Nap] Yawn detected: NapCount {NapCount}/{NAP_COUNT_MAX}");
        }

        CheckAndStartNap();
    }

    public void OnPetTouched()
    {
        if (GlobalVariables.CurrentState != PetState.napping)
        {
            GameLogger.Log(LogCategory.Sleep, $"[Nap] Touch ignored: Current state={GlobalVariables.CurrentState}");
            return;
        }

        if (_sleepyReactionCoroutine != null)
        {
            GameLogger.Log(LogCategory.Sleep, "[Nap] Touch ignored: Coroutine already running");
            return;
        }

        float randomValue = UnityEngine.Random.Range(0f, 1f);

        if (randomValue < 0.5f)
        {
            if (NapCount > 0)
            {
                NapCount--;
                PlayerPrefs.SetInt(NAP_COUNT_KEY, NapCount);
                PlayerPrefs.Save();
                GameLogger.Log(LogCategory.Sleep, $"[Nap] Touch detected (decrease): NapCount {NapCount + 1} -> {NapCount}");

                if (NapCount < NAP_COUNT_MAX)
                {
                    EndNap("NapCount<15");
                }
            }
        }
        else
        {
            GameLogger.Log(LogCategory.Sleep, "[Nap] Touch detected (sleepy reaction): Will wake briefly then sleep again");
            _sleepyReactionCoroutine = StartCoroutine(SleepyReaction());
        }
    }

    private IEnumerator SleepyReaction()
    {
        _isInSleepyReaction = true;

        if (GlobalVariables.CurrentState != PetState.napping)
        {
            GameLogger.Log(LogCategory.Sleep, "[Nap] Sleepy reaction cancelled: Not in napping state");
            _sleepyReactionCoroutine = null;
            _isInSleepyReaction = false;
            yield break;
        }

        _dogController.Sleeping(false);
        GameLogger.Log(LogCategory.Sleep, "[Nap] Sleepy reaction: Woke up");

        float waitTime = UnityEngine.Random.Range(2f, 3f);
        yield return new WaitForSeconds(waitTime);
        _dogController.UpdateTransitionState(1);
        if (GlobalVariables.CurrentState == PetState.napping)
        {
            _dogController.Sleeping(true);
            GameLogger.Log(LogCategory.Sleep, "[Nap] Sleepy reaction: Went back to sleep");
        }
        else
        {
            GameLogger.Log(LogCategory.Sleep, "[Nap] Sleepy reaction: Nap already ended, skipping sleep");
        }
        _sleepyReactionCoroutine = null;
        _isInSleepyReaction = false;
    }

    private void CheckAndStartNap()
    {
        if (GlobalVariables.CurrentState == PetState.napping ||
            GlobalVariables.CurrentState == PetState.sleeping)
        {
            return;
        }

        if (NapCount < NAP_COUNT_MAX)
        {
            return;
        }

        DateTime now = TimeZoneProvider.Now;
        int napStartHour = PlayerPrefs.GetInt(NAP_START_HOUR_KEY, 13);
        int napWakeHour = PlayerPrefs.GetInt(NAP_WAKE_HOUR_KEY, 15);

        if (now.Hour >= napStartHour && now.Hour < napWakeHour)
        {
            StartNap(now, napWakeHour, TimeZoneProvider.Current);
        }
    }

    private void StartNap(DateTime now, int napEndHour, TimeZoneInfo tz)
    {
        if (GlobalVariables.CurrentState == PetState.napping)
        {
            return;
        }

        GlobalVariables.CurrentState = PetState.napping;
        _dogController.Sleeping(true);
        _firebaseManager.UpdatePetState("napping");
        _mainUiButtons.UpdateButtonVisibility(false);

        BrightnessUtil.SetBrightness.DoAction(-1f);

        DateTime napEnd = new DateTime(now.Year, now.Month, now.Day, napEndHour, 0, 0);
        if (now >= napEnd)
        {
            napEnd = napEnd.AddDays(1);
        }

        TimeSpan timeUntilEnd = napEnd - now;
        float secondsUntilEnd = (float)timeUntilEnd.TotalSeconds;
        Invoke(nameof(OnNapEndTimeReached), secondsUntilEnd);

        GameLogger.Log(LogCategory.Sleep, $"[Nap] Nap started: Time={now.Hour}h, NapCount={NapCount}, End scheduled={napEnd}");
    }

    private void OnNapEndTimeReached()
    {
        GameLogger.Log(LogCategory.Sleep, $"[Nap] OnNapEndTimeReached called. Current state: {GlobalVariables.CurrentState}");

        // 追加: 夜の睡眠中はスキップ
        if (GlobalVariables.CurrentState == PetState.sleeping)
        {
            GameLogger.Log(LogCategory.Sleep, "[Nap] Currently in night sleep, skipping nap cleanup");
            ResetNapCount("napEndHour during night sleep");
            return;
        }

        // 実行中のコルーチンを停止
        if (_sleepyReactionCoroutine != null)
        {
            StopCoroutine(_sleepyReactionCoroutine);
            _sleepyReactionCoroutine = null;
            _isInSleepyReaction = false;
            GameLogger.Log(LogCategory.Sleep, "[Nap] Forced stop of sleepy reaction coroutine");
        }

        // NapCountをリセット
        ResetNapCount("napEndHour reached");

        // 状態がnappingの場合は通常のEndNap
        if (GlobalVariables.CurrentState == PetState.napping)
        {
            EndNap("napEndHour reached");
        }
        else
        {
            // 状態がidleなど他の状態でも、見た目と設定をクリーンアップ
            GameLogger.Log(LogCategory.Sleep, $"[Nap] Forcing cleanup from {GlobalVariables.CurrentState} state");

            // 見た目を起こす
            _dogController.Sleeping(false);

            // UIを表示（idleならすでに表示されているはず）
            if (GlobalVariables.CurrentState != PetState.idle)
            {
                GlobalVariables.CurrentState = PetState.idle;
                _firebaseManager.UpdatePetState("idle");
            }
            _mainUiButtons.UpdateButtonVisibility(true);

            // 画面を明るく
            BrightnessUtil.SetBrightness.DoAction(1.0f);

            GameLogger.Log(LogCategory.Sleep, "[Nap] Forced nap cleanup completed");
        }
    }

    private void EndNap(string reason)
    {
        if (GlobalVariables.CurrentState != PetState.napping)
        {
            return;
        }

        if (_sleepyReactionCoroutine != null)
        {
            StopCoroutine(_sleepyReactionCoroutine);
            _sleepyReactionCoroutine = null;
            _isInSleepyReaction = false;
            GameLogger.Log(LogCategory.Sleep, "[Nap] Stopped sleepy reaction coroutine");
        }

        GlobalVariables.CurrentState = PetState.idle;
        _dogController.Sleeping(false);
        _firebaseManager.UpdatePetState("idle");
        _mainUiButtons.UpdateButtonVisibility(true);

        BrightnessUtil.SetBrightness.DoAction(1.0f);

        CancelInvoke(nameof(OnNapEndTimeReached));

        GameLogger.Log(LogCategory.Sleep, $"[Nap] Nap ended (reason: {reason})");
    }

    public void CancelNapSchedule()
    {
        CancelInvoke(nameof(OnNapEndTimeReached));

        if (_sleepyReactionCoroutine != null)
        {
            StopCoroutine(_sleepyReactionCoroutine);
            _sleepyReactionCoroutine = null;
            _isInSleepyReaction = false;
            GameLogger.Log(LogCategory.Sleep, "[Nap] CancelNapSchedule: Stopped sleepy reaction coroutine");
        }
    }
}