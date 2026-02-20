using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TapHouse.Logging;

public sealed class DailyWakeScheduler
{
    private readonly IAlarmScheduler _alarm;
    private readonly string KEY = "WakeHour";
    private readonly TimeZoneInfo _tz;

    public DailyWakeScheduler(IAlarmScheduler alarm, TimeZoneInfo tz)
    {
        _alarm = alarm;
        _tz = tz;
    }

    public void EnsureDailyWakeAtHour()
    {
        int wakeHour = PlayerPrefs.GetInt(KEY, 6);
        DateTime nowLocal = TimeZoneInfo.ConvertTime(DateTime.UtcNow, _tz);
        DateTime next = NextLocalTimeAtHour(nowLocal, wakeHour);
        _alarm.ScheduleWakeAt(next, _tz);
        GameLogger.Log(LogCategory.Sleep, $"[DailyWake] scheduled at {next} ({_tz.StandardName})");
    }

    private static DateTime NextLocalTimeAtHour(DateTime baseLocal, int hour)
    {
        var target = new DateTime(baseLocal.Year, baseLocal.Month, baseLocal.Day, hour, 0, 0);
        if (baseLocal >= target) target = target.AddDays(1);
        return target;
    }
}
