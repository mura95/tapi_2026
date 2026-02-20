using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TapHouse.Logging;

public class TimeZoneUtility : MonoBehaviour
{
    private const string TIMEZONE_ID_KEY = PrefsKeys.TimezoneId;

    /// <summary>
    /// できれば .NET から一覧取得。ダメなら Android は java.util.TimeZone で代替し、それでもダメなら Local のみ。
    /// </summary>
    public static List<TimeZoneInfo> SafeGetSystemTimeZones()
    {
        // まずは標準の取得を試す（PCや一部端末ではこれで十分）
        try
        {
            var list = TimeZoneInfo.GetSystemTimeZones().ToList();
            if (list != null && list.Count > 1)
                return list;
        }
        catch (Exception e)
        {
            GameLogger.LogWarning(LogCategory.Sleep,$"[TimeZoneUtility] GetSystemTimeZones 失敗: {e.GetType().Name} - {e.Message}");
        }

        // ---- Android フォールバック ----
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            var androidList = GetAndroidTimeZoneInfos();
            if (androidList.Count > 0)
                return androidList;
        }
        catch (Exception e)
        {
            GameLogger.LogWarning(LogCategory.Sleep,$"[TimeZoneUtility] Android フォールバック失敗: {e.GetType().Name} - {e.Message}");
        }
#endif

        // 最終フォールバック：Local のみ
        GameLogger.LogWarning(LogCategory.Sleep,"[TimeZoneUtility] タイムゾーン一覧を取得できなかったため、Local のみを返します。");
        return new List<TimeZoneInfo> { TimeZoneInfo.Local };
    }

    /// <summary>
    /// IANA/Windows の簡易フォールバック検索
    /// </summary>
    public static TimeZoneInfo FindTimeZoneByIdWithFallback(string id, List<TimeZoneInfo> list)
    {
        var tz = list.FirstOrDefault(t => t.Id == id);
        if (tz != null) return tz;

        // 最低限の相互マッピング（必要に応じて拡張）
        if (id == "Asia/Tokyo")
            tz = list.FirstOrDefault(t => t.Id == "Tokyo Standard Time");
        else if (id == "Tokyo Standard Time")
            tz = list.FirstOrDefault(t => t.Id == "Asia/Tokyo");

        return tz;
    }

    /// <summary>
    /// タイムゾーンを初期化して PlayerPrefs に ID を保存。UIドロップダウンも更新。
    /// </summary>
    public static TimeZoneInfo InitializeTimeZone(Action<int, List<TimeZoneInfo>> setDropdownValue)
    {
        var timeZones = SafeGetSystemTimeZones();
        string savedId = PlayerPrefs.GetString(TIMEZONE_ID_KEY, string.Empty);

        var preferredIds = new[]
        {
            string.IsNullOrEmpty(savedId) ? null : savedId, // 保存済み最優先
            TimeZoneInfo.Local.Id                           // デバイスのTZ
        }.Where(id => !string.IsNullOrEmpty(id));

        var selectedTimeZone = preferredIds
            .Select(id => FindTimeZoneByIdWithFallback(id, timeZones))
            .FirstOrDefault(tz => tz != null) ?? TimeZoneInfo.Local;

        PlayerPrefs.SetString(TIMEZONE_ID_KEY, selectedTimeZone.Id);
        PlayerPrefs.Save();

        // UIインデックス化（なければ追加）
        int index = timeZones.FindIndex(tz => tz.Id == selectedTimeZone.Id);
        if (index < 0)
        {
            timeZones.Add(selectedTimeZone);
            index = timeZones.Count - 1;
        }

        setDropdownValue?.Invoke(index, timeZones);
        return selectedTimeZone;
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    /// <summary>
    /// Android の java.util.TimeZone から IANA ID 群を取得し、TimeZoneInfo に変換。
    /// 変換できないIDはスキップ。重複を除去し、表示名でソート。
    /// </summary>
    private static List<TimeZoneInfo> GetAndroidTimeZoneInfos()
    {
        var result = new List<TimeZoneInfo>();

        try
        {
            using (var tzClass = new AndroidJavaClass("java.util.TimeZone"))
            {
                // 端末が知っている IANA タイムゾーンID一覧を取得
                var ids = tzClass.CallStatic<string[]>("getAvailableIDs");
                if (ids != null && ids.Length > 0)
                {
                    foreach (var id in ids)
                    {
                        if (string.IsNullOrEmpty(id)) continue;

                        try
                        {
                            var tz = TimeZoneInfo.FindSystemTimeZoneById(id);
                            if (tz != null && !result.Any(x => x.Id == tz.Id))
                                result.Add(tz);
                        }
                        catch
                        {
                            // 生成できないIDはスキップ
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            GameLogger.LogWarning(LogCategory.Sleep,$"[TimeZoneUtility] Android TimeZone fetch failed: {e.Message}");
        }

        // Androidで取得できなかった場合、主要なタイムゾーンをハードコードで追加
        if (result.Count == 0)
        {
            GameLogger.Log(LogCategory.Sleep,"[TimeZoneUtility] Using hardcoded timezone list for Android");
            result = GetHardcodedTimeZones();
        }

        // 表示名で並べる
        result = result
            .OrderBy(tz => tz.BaseUtcOffset)
            .ThenBy(tz => tz.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Local を先頭に差し込み
        if (!result.Any(tz => tz.Id == TimeZoneInfo.Local.Id))
            result.Insert(0, TimeZoneInfo.Local);

        return result;
    }

    /// <summary>
    /// 主要なタイムゾーンのハードコードリスト（フォールバック用）
    /// </summary>
    private static List<TimeZoneInfo> GetHardcodedTimeZones()
    {
        var result = new List<TimeZoneInfo>();

        // 主要なタイムゾーンID（IANA形式）
        var commonIds = new[]
        {
            "Asia/Tokyo",
            "Asia/Seoul",
            "Asia/Shanghai",
            "Asia/Hong_Kong",
            "Asia/Singapore",
            "Asia/Bangkok",
            "Asia/Jakarta",
            "Asia/Kolkata",
            "Asia/Dubai",
            "Europe/London",
            "Europe/Paris",
            "Europe/Berlin",
            "Europe/Moscow",
            "America/New_York",
            "America/Chicago",
            "America/Denver",
            "America/Los_Angeles",
            "America/Sao_Paulo",
            "Australia/Sydney",
            "Australia/Melbourne",
            "Pacific/Auckland",
            "Pacific/Honolulu",
            "UTC"
        };

        foreach (var id in commonIds)
        {
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById(id);
                if (tz != null && !result.Any(x => x.Id == tz.Id))
                    result.Add(tz);
            }
            catch
            {
                // 取得できないものはスキップ
            }
        }

        // それでも空なら、カスタムTimeZoneInfoを作成
        if (result.Count == 0)
        {
            GameLogger.Log(LogCategory.Sleep,"[TimeZoneUtility] Creating custom TimeZoneInfo for JST");
            try
            {
                var jst = TimeZoneInfo.CreateCustomTimeZone(
                    "Asia/Tokyo",
                    TimeSpan.FromHours(9),
                    "Japan Standard Time",
                    "Japan Standard Time"
                );
                result.Add(jst);
            }
            catch (Exception e)
            {
                GameLogger.LogWarning(LogCategory.Sleep,$"[TimeZoneUtility] Failed to create custom timezone: {e.Message}");
            }
        }

        return result;
    }
#endif
}
