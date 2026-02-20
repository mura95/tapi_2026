#if !UNITY_ANDROID || UNITY_EDITOR
using System;
using UnityEngine;
using TapHouse.Logging;

public sealed class EditorAlarmScheduler : IAlarmScheduler
{
    private MonoBehaviour _host;
    private float _pendingSeconds = -1f;
    private const string NEXT_WAKE_EPOCH_MS_KEY = "NextWakeEpochMs";

    public EditorAlarmScheduler(MonoBehaviour host) { _host = host; }

    public void ScheduleWakeAt(DateTime targetLocal, TimeZoneInfo tz)
    {
        var nowLocal = TimeZoneInfo.ConvertTime(DateTime.UtcNow, tz);
        _pendingSeconds = (float)(targetLocal - nowLocal).TotalSeconds;
        if (_pendingSeconds < 0) _pendingSeconds = 60f;

        long epochMs = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(targetLocal, tz)).ToUnixTimeMilliseconds();
        PlayerPrefs.SetString(NEXT_WAKE_EPOCH_MS_KEY, epochMs.ToString());
        PlayerPrefs.Save();

        _host.CancelInvoke(nameof(EditorWake));
        _host.Invoke(nameof(EditorWake), _pendingSeconds);
        GameLogger.Log(LogCategory.Sleep, $"[Alarm(Editor)] Invoke in {_pendingSeconds:F1}s");
    }

    public void CancelWake()
    {
        _host.CancelInvoke(nameof(EditorWake));
        _pendingSeconds = -1f;
        PlayerPrefs.DeleteKey(NEXT_WAKE_EPOCH_MS_KEY);
    }

    public DateTime? GetNextWakeLocal(TimeZoneInfo tz)
    {
        string saved = PlayerPrefs.GetString(NEXT_WAKE_EPOCH_MS_KEY, string.Empty);
        if (long.TryParse(saved, out long ms))
        {
            var utc = DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime;
            return TimeZoneInfo.ConvertTimeFromUtc(utc, tz);
        }
        return null;
    }

    public bool ShouldAutoWakeFromAlarm()
    {
        // Editorでは時間経過で代用
        string saved = PlayerPrefs.GetString(NEXT_WAKE_EPOCH_MS_KEY, string.Empty);
        if (long.TryParse(saved, out long epochMs))
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            bool past = now >= epochMs - 30_000;
            if (past) PlayerPrefs.DeleteKey(NEXT_WAKE_EPOCH_MS_KEY);
            return past;
        }
        return false;
    }

    private void EditorWake()
    {
        // 実際のWakeUp呼びは SleepController 側の OnApplicationFocus などで判断して行う
        GameLogger.Log(LogCategory.Sleep, "[Alarm(Editor)] Timer fired");
    }
}
#endif
