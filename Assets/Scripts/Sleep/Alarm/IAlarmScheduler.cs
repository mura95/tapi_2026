using System;

public interface IAlarmScheduler
{
    void ScheduleWakeAt(DateTime targetLocal, TimeZoneInfo tz);
    void CancelWake();
    DateTime? GetNextWakeLocal(TimeZoneInfo tz);
    bool ShouldAutoWakeFromAlarm();
}
