using System;
using System.Collections.Generic;
using UnityEngine;
using TapHouse.Logging;

/// <summary>
/// タイムゾーンを一元管理するstaticクラス
/// アプリ起動時に一度だけ初期化し、以降はどこからでもアクセス可能
/// </summary>
public static class TimeZoneProvider
{
    private static TimeZoneInfo _currentTimeZone;
    private static List<TimeZoneInfo> _availableTimeZones;
    private static bool _isInitialized = false;

    /// <summary>
    /// 現在選択されているタイムゾーン（null安全）
    /// </summary>
    public static TimeZoneInfo Current
    {
        get => _currentTimeZone ?? TimeZoneInfo.Local;
        set
        {
            _currentTimeZone = value;
            GameLogger.Log(LogCategory.Sleep,$"[TimeZoneProvider] TimeZone set to: {value?.DisplayName ?? "Local"}");
        }
    }

    /// <summary>
    /// 利用可能なタイムゾーン一覧
    /// </summary>
    public static List<TimeZoneInfo> AvailableTimeZones
    {
        get
        {
            if (_availableTimeZones == null || _availableTimeZones.Count == 0)
            {
                _availableTimeZones = TimeZoneUtility.SafeGetSystemTimeZones();
            }
            return _availableTimeZones;
        }
    }

    /// <summary>
    /// 初期化済みかどうか
    /// </summary>
    public static bool IsInitialized => _isInitialized;

    /// <summary>
    /// 現在のローカル時刻（選択されたタイムゾーンで変換済み）
    /// </summary>
    public static DateTime Now => TimeZoneInfo.ConvertTime(DateTime.UtcNow, Current);

    /// <summary>
    /// 初期化処理（アプリ起動時に一度だけ呼び出す）
    /// </summary>
    /// <param name="onDropdownReady">ドロップダウン更新用コールバック（index, timeZoneList）</param>
    /// <returns>選択されたTimeZoneInfo</returns>
    public static TimeZoneInfo Initialize(Action<int, List<TimeZoneInfo>> onDropdownReady = null)
    {
        if (_isInitialized)
        {
            GameLogger.Log(LogCategory.Sleep,"[TimeZoneProvider] Already initialized, returning current timezone");
            onDropdownReady?.Invoke(GetCurrentIndex(), AvailableTimeZones);
            return Current;
        }

        GameLogger.Log(LogCategory.Sleep,"[TimeZoneProvider] Initializing...");

        // タイムゾーン一覧を取得
        _availableTimeZones = TimeZoneUtility.SafeGetSystemTimeZones();
        GameLogger.Log(LogCategory.Sleep,$"[TimeZoneProvider] Found {_availableTimeZones.Count} timezones");

        // 保存されたタイムゾーンを復元
        _currentTimeZone = TimeZoneUtility.InitializeTimeZone((idx, tzList) =>
        {
            _availableTimeZones = tzList;
            onDropdownReady?.Invoke(idx, tzList);
        });

        _isInitialized = true;
        GameLogger.Log(LogCategory.Sleep,$"[TimeZoneProvider] Initialized with: {_currentTimeZone.DisplayName}");

        return _currentTimeZone;
    }

    /// <summary>
    /// タイムゾーンをインデックスで更新
    /// </summary>
    public static void UpdateByIndex(int index)
    {
        if (_availableTimeZones == null || _availableTimeZones.Count == 0)
        {
            GameLogger.LogWarning(LogCategory.Sleep,"[TimeZoneProvider] AvailableTimeZones is empty, cannot update");
            return;
        }

        if (index >= 0 && index < _availableTimeZones.Count)
        {
            _currentTimeZone = _availableTimeZones[index];
            SaveCurrentTimeZone();
            GameLogger.Log(LogCategory.Sleep,$"[TimeZoneProvider] Updated to: {_currentTimeZone.DisplayName} (index: {index})");
        }
        else
        {
            GameLogger.LogWarning(LogCategory.Sleep,$"[TimeZoneProvider] Invalid index: {index}, count: {_availableTimeZones.Count}");
        }
    }

    /// <summary>
    /// 現在のタイムゾーンのインデックスを取得
    /// </summary>
    public static int GetCurrentIndex()
    {
        if (_availableTimeZones == null || _currentTimeZone == null)
            return 0;

        int index = _availableTimeZones.FindIndex(tz => tz.Id == _currentTimeZone.Id);
        return index >= 0 ? index : 0;
    }

    /// <summary>
    /// 現在のタイムゾーンをPlayerPrefsに保存
    /// </summary>
    private static void SaveCurrentTimeZone()
    {
        if (_currentTimeZone != null)
        {
            PlayerPrefs.SetString(PrefsKeys.TimezoneId, _currentTimeZone.Id);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// リセット（テスト用）
    /// </summary>
    public static void Reset()
    {
        _currentTimeZone = null;
        _availableTimeZones = null;
        _isInitialized = false;
        GameLogger.Log(LogCategory.Sleep,"[TimeZoneProvider] Reset");
    }
}
