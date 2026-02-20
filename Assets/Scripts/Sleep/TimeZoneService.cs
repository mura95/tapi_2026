using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TapHouse.Logging;

public class TimeZoneService : MonoBehaviour
{
    [SerializeField] private SleepController _sleepController;

    public void UpdateTimeZone(int newTimeZoneIndex)
    {
        var timeZones = TimeZoneProvider.AvailableTimeZones;

        if (timeZones == null || timeZones.Count == 0)
        {
            // 初期化されていない場合は初期化
            TimeZoneProvider.Initialize();
            _sleepController.UpdateTimeZone(TimeZoneProvider.Current);
            return;
        }

        if (newTimeZoneIndex >= 0 && newTimeZoneIndex < timeZones.Count)
        {
            TimeZoneProvider.UpdateByIndex(newTimeZoneIndex);
            GameLogger.Log(LogCategory.Sleep,$"タイムゾーンが更新されました: {TimeZoneProvider.Current.DisplayName}");

            // SleepControllerに新しいTimeZoneを設定
            _sleepController.UpdateTimeZone(TimeZoneProvider.Current);
        }
        else
        {
            GameLogger.LogError(LogCategory.Sleep,$"更新インデックスが無効: {newTimeZoneIndex}, count: {timeZones.Count}");
        }
    }
}

