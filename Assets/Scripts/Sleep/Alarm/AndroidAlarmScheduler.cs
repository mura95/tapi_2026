#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine;
using System;
using TapHouse.Logging;

/// <summary>
/// Android用のアラームスケジューラー（デバッグ強化版）
/// setAlarmClockを使用して確実にアラームを発火させ、詳細なログを出力
/// </summary>
public sealed class AndroidAlarmScheduler : IAlarmScheduler
{
    private const int    WAKE_ALARM_REQ_CODE    = 1001;
    private const int    NOTIFICATION_ID        = 12345;
    private const string NOTIFICATION_CHANNEL_ID = "wake_alarm_channel";
    private const string NEXT_WAKE_EPOCH_MS_KEY = "NextWakeEpochMs";
    private const string ACTION_ALARM_WAKE      = "jp.co.pichipichi.petdisplay.ACTION_ALARM_WAKE";
    // PendingIntent flags (Android 定数の値)
    private const int FLAG_IMMUTABLE      = 0x04000000; // PendingIntent.FLAG_IMMUTABLE
    private const int FLAG_UPDATE_CURRENT = 0x08000000; // PendingIntent.FLAG_UPDATE_CURRENT
    private const int FLAG_CANCEL_CURRENT = 0x00000020; // PendingIntent.FLAG_CANCEL_CURRENT
    private const int FLAG_NO_CREATE      = 0x00000008; // PendingIntent.FLAG_NO_CREATE

    /// <summary>
    /// アラーム権限の状態をチェック
    /// Android 12以降で SCHEDULE_EXACT_ALARM 権限が許可されているかを返す
    /// Android 11以下では常にtrue
    /// </summary>
    public static bool CheckAlarmPermission()
    {
        try
        {
            int sdkInt = new AndroidJavaClass("android.os.Build$VERSION").GetStatic<int>("SDK_INT");

            // Android 11以下は権限不要
            if (sdkInt < 31)
            {
                GameLogger.Log(LogCategory.Sleep, "[AlarmPermission] SDK < 31, permission not required");
                return true;
            }

            var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            var alarmMgr = activity.Call<AndroidJavaObject>("getSystemService", "alarm");
            bool canSchedule = alarmMgr.Call<bool>("canScheduleExactAlarms");

            GameLogger.Log(LogCategory.Sleep, $"[AlarmPermission] canScheduleExactAlarms: {canSchedule}");
            return canSchedule;
        }
        catch (Exception e)
        {
            GameLogger.LogError(LogCategory.Sleep, $"[AlarmPermission] Check failed: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// アラーム権限の設定画面を開く
    /// Android 12以降でのみ有効
    /// </summary>
    public static void RequestAlarmPermission()
    {
        try
        {
            int sdkInt = new AndroidJavaClass("android.os.Build$VERSION").GetStatic<int>("SDK_INT");

            if (sdkInt < 31)
            {
                GameLogger.Log(LogCategory.Sleep, "[AlarmPermission] SDK < 31, no permission request needed");
                return;
            }

            var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            var intent = new AndroidJavaObject("android.content.Intent");
            intent.Call<AndroidJavaObject>("setAction", "android.settings.REQUEST_SCHEDULE_EXACT_ALARM");

            // パッケージURIを設定（自アプリの設定画面に直接遷移）
            var uriClass = new AndroidJavaClass("android.net.Uri");
            var uri = uriClass.CallStatic<AndroidJavaObject>("parse", "package:" + activity.Call<string>("getPackageName"));
            intent.Call<AndroidJavaObject>("setData", uri);

            activity.Call("startActivity", intent);
            GameLogger.Log(LogCategory.Sleep, "[AlarmPermission] Opened alarm permission settings");
        }
        catch (Exception e)
        {
            GameLogger.LogError(LogCategory.Sleep, $"[AlarmPermission] Request failed: {e.Message}");
        }
    }

    /// <summary>
    /// アプリ起動時に呼び出す：権限を確認し、なければ設定画面へ誘導
    /// </summary>
    /// <returns>権限がある場合true、設定画面へ遷移した場合false</returns>
    public static bool EnsureAlarmPermissionOnStartup()
    {
        if (CheckAlarmPermission())
        {
            GameLogger.Log(LogCategory.Sleep, "[AlarmPermission] Permission already granted");
            return true;
        }

        GameLogger.LogWarning(LogCategory.Sleep, "[AlarmPermission] Permission not granted, opening settings...");
        RequestAlarmPermission();
        return false;
    }


        AndroidJavaObject BuildExplicitWakeIntent(AndroidJavaObject activity)
        {
        var clazz = new AndroidJavaClass("java.lang.Class")
            .CallStatic<AndroidJavaObject>("forName",
                "jp.co.pichipichi.petdisplay.WakeAlarmReceiver");

        var intent = new AndroidJavaObject("android.content.Intent", activity, clazz);
        intent.Call<AndroidJavaObject>("setAction", ACTION_ALARM_WAKE);
        return intent;
        }


    public void ScheduleWakeAt(DateTime targetLocal, TimeZoneInfo tz)
    {
        GameLogger.Log(LogCategory.Sleep, "========================================");
        GameLogger.Log(LogCategory.Sleep, "[Alarm] ScheduleWakeAt() START");
        GameLogger.Log(LogCategory.Sleep, "========================================");

        try
        {
            var nowLocal = TimeZoneInfo.ConvertTime(DateTime.UtcNow, tz);
            var deltaSec = (targetLocal - nowLocal).TotalSeconds;

            GameLogger.Log(LogCategory.Sleep, $"[Alarm] Target time: {targetLocal:yyyy-MM-dd HH:mm:ss}");
            GameLogger.Log(LogCategory.Sleep, $"[Alarm] Current time: {nowLocal:yyyy-MM-dd HH:mm:ss}");
            GameLogger.Log(LogCategory.Sleep, $"[Alarm] Time delta: {deltaSec:F1} seconds");

            if (deltaSec <= 0)
            {
                targetLocal = nowLocal.AddMinutes(1);
                GameLogger.LogWarning(LogCategory.Sleep, $"[Alarm] Past time specified. Fallback to +1min: {targetLocal:HH:mm:ss}");
            }

            // UTC epoch(ms) へ変換
            DateTime targetUtc = TimeZoneInfo.ConvertTimeToUtc(targetLocal, tz);
            long triggerAtMs   = ((DateTimeOffset)targetUtc).ToUnixTimeMilliseconds();

            GameLogger.Log(LogCategory.Sleep, $"[Alarm] Target UTC: {targetUtc:yyyy-MM-dd HH:mm:ss}");
            GameLogger.Log(LogCategory.Sleep, $"[Alarm] Trigger epoch: {triggerAtMs}");

            // Android 取得
            var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var activity    = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            var pm          = activity.Call<AndroidJavaObject>("getPackageManager");
            var pkg         = activity.Call<string>("getPackageName");

            GameLogger.Log(LogCategory.Sleep, $"[Alarm] Package: {pkg}");

            // Android SDKバージョンチェック
            int sdkInt = new AndroidJavaClass("android.os.Build$VERSION").GetStatic<int>("SDK_INT");
            GameLogger.Log(LogCategory.Sleep, $"[Alarm] Android SDK version: {sdkInt}");

            // Android 12以降では SCHEDULE_EXACT_ALARM 権限が必要
            if (sdkInt >= 31)
            {
                var alarmMgr = activity.Call<AndroidJavaObject>("getSystemService", "alarm");
                bool canSchedule = alarmMgr.Call<bool>("canScheduleExactAlarms");
                GameLogger.Log(LogCategory.Sleep, $"[Alarm] Can schedule exact alarms: {canSchedule}");

                if (!canSchedule)
                {
                    GameLogger.LogError(LogCategory.Sleep, "[Alarm] SCHEDULE_EXACT_ALARM permission not granted!");
                    GameLogger.LogError(LogCategory.Sleep, "[Alarm] Please enable exact alarm permission in system settings.");

                    // 設定画面を開くIntentを作成（ユーザーに通知）
                    try
                    {
                        var settingsIntent = new AndroidJavaObject("android.content.Intent");
                        settingsIntent.Call<AndroidJavaObject>("setAction", "android.settings.REQUEST_SCHEDULE_EXACT_ALARM");
                        activity.Call("startActivity", settingsIntent);
                    }
                    catch (Exception ex)
                    {
                        GameLogger.LogWarning(LogCategory.Sleep, $"[Alarm] Failed to open settings: {ex.Message}");
                    }
                    return;
                }
            }

            // ---------- PendingIntent(Broadcast用) ----------
            int wakeHour = targetLocal.Hour;
            int wakeMin  = targetLocal.Minute;
            int tzOffsetMin = (int)Math.Round(tz.GetUtcOffset(targetLocal).TotalMinutes);

            GameLogger.Log(LogCategory.Sleep, $"[Alarm] Creating broadcast intent:");
            GameLogger.Log(LogCategory.Sleep, $"[Alarm]   wake_hour = {wakeHour}");
            GameLogger.Log(LogCategory.Sleep, $"[Alarm]   wake_min = {wakeMin}");
            GameLogger.Log(LogCategory.Sleep, $"[Alarm]   tz_id = {tz.Id}");
            GameLogger.Log(LogCategory.Sleep, $"[Alarm]   tz_offset_min = {tzOffsetMin}");

            // 1) WakeAlarmReceiver の java.lang.Class を取得
            var receiverClass = new AndroidJavaClass("java.lang.Class")
                .CallStatic<AndroidJavaObject>("forName", "jp.co.pichipichi.petdisplay.WakeAlarmReceiver");

            // 2) 明示的 Intent(Context, Class) で生成
            var brIntent = new AndroidJavaObject("android.content.Intent", activity, receiverClass);

            // （必要なら自アクションを付与：Manifest の <action> と一致）
            brIntent.Call<AndroidJavaObject>("setAction", "jp.co.pichipichi.petdisplay.ACTION_ALARM_WAKE");

            // 3) これまで通り extras を積む
            brIntent.Call<AndroidJavaObject>("putExtra", "wake_hour", wakeHour);
            brIntent.Call<AndroidJavaObject>("putExtra", "wake_min",  wakeMin);
            brIntent.Call<AndroidJavaObject>("putExtra", "tz_id",     tz.Id);
            brIntent.Call<AndroidJavaObject>("putExtra", "tz_offset_min", tzOffsetMin);

            // 4) PendingIntent を作成（フラグは現状のままでOK）
            var piClass = new AndroidJavaClass("android/app/PendingIntent");
            var op = piClass.CallStatic<AndroidJavaObject>(
                "getBroadcast",
                activity,
                WAKE_ALARM_REQ_CODE,
                brIntent,
                FLAG_IMMUTABLE | FLAG_CANCEL_CURRENT
            );


            GameLogger.Log(LogCategory.Sleep, "[Alarm] Broadcast PendingIntent created");

            // ---------- ステータスバーの「目覚ましアイコン」タップ時に開く showIntent ----------
            var launchIntent = pm.Call<AndroidJavaObject>("getLaunchIntentForPackage", pkg);
            int FLAG_ACTIVITY_NEW_TASK   = 0x10000000;
            int FLAG_ACTIVITY_SINGLE_TOP = 0x20000000;
            int FLAG_ACTIVITY_CLEAR_TOP  = 0x04000000;
            launchIntent.Call<AndroidJavaObject>("addFlags", FLAG_ACTIVITY_NEW_TASK | FLAG_ACTIVITY_SINGLE_TOP | FLAG_ACTIVITY_CLEAR_TOP);

            var showPi = piClass.CallStatic<AndroidJavaObject>(
                "getActivity", activity, 0, launchIntent, FLAG_IMMUTABLE | FLAG_UPDATE_CURRENT);

            GameLogger.Log(LogCategory.Sleep, "[Alarm] Show PendingIntent created");

            // ---------- AlarmManager セット ----------
            var alarmMgr2 = activity.Call<AndroidJavaObject>("getSystemService", "alarm");

            // 既存をキャンセル
            try
            {
                alarmMgr2.Call("cancel", op);
                GameLogger.Log(LogCategory.Sleep, "[Alarm] Previous alarm cancelled");
            }
            catch (Exception ex)
            {
                GameLogger.LogWarning(LogCategory.Sleep, $"[Alarm] Cancel failed (may not exist): {ex.Message}");
            }

            // "目覚ましアイコン"+Doze貫通
            var alarmClockInfo = new AndroidJavaObject("android/app/AlarmManager$AlarmClockInfo", triggerAtMs, showPi);
            alarmMgr2.Call("setAlarmClock", alarmClockInfo, op);

            GameLogger.Log(LogCategory.Sleep, "[Alarm] setAlarmClock() called successfully");

            // UnityフォールバックチェックのためPlayerPrefsに保存
            PlayerPrefs.SetString(NEXT_WAKE_EPOCH_MS_KEY, triggerAtMs.ToString());
            PlayerPrefs.Save();
            GameLogger.Log(LogCategory.Sleep, $"[Alarm] Saved to PlayerPrefs: {NEXT_WAKE_EPOCH_MS_KEY} = {triggerAtMs}");

            // 端末再起動・時刻変更復旧用に SharedPreferences へも保存
            var prefs  = activity.Call<AndroidJavaObject>("getSharedPreferences", "tap_alarm_cfg", 0);
            var editor = prefs.Call<AndroidJavaObject>("edit");
            editor.Call<AndroidJavaObject>("putInt",   "wake_hour",     wakeHour);
            editor.Call<AndroidJavaObject>("putInt",   "wake_min",      wakeMin);
            editor.Call<AndroidJavaObject>("putString","tz_id",         tz.Id);
            editor.Call<AndroidJavaObject>("putInt",   "tz_offset_min", tzOffsetMin);
            editor.Call<AndroidJavaObject>("putLong",  "next_epoch_ms", triggerAtMs);
            editor.Call("apply");

            GameLogger.Log(LogCategory.Sleep, "[Alarm] Saved to SharedPreferences");

            // ★ フォールバック通知を表示
            ShowWakeupNotification(targetLocal, activity);

            GameLogger.Log(LogCategory.Sleep, "========================================");
            GameLogger.Log(LogCategory.Sleep, "[Alarm] ScheduleWakeAt() COMPLETE");
            GameLogger.Log(LogCategory.Sleep, $"[Alarm] Next wake: {targetLocal:yyyy-MM-dd HH:mm:ss} ({tz.Id})");
            GameLogger.Log(LogCategory.Sleep, "========================================");
        }
        catch (System.Exception e)
        {
            GameLogger.LogError(LogCategory.Sleep, $"[Alarm] ScheduleWakeAt failed: {e.Message}");
            GameLogger.LogError(LogCategory.Sleep, $"[Alarm] Stack trace: {e.StackTrace}");
        }
    }

    /// <summary>
    /// 目覚ましアイコンが表示されない端末用のフォールバック通知
    /// </summary>
    private void ShowWakeupNotification(DateTime wakeTime, AndroidJavaObject activity)
    {
        try
        {
            GameLogger.Log(LogCategory.Sleep, "[Alarm] Showing fallback notification...");

            var notificationManager = activity.Call<AndroidJavaObject>("getSystemService", "notification");

            // Android 8.0以上で通知チャンネル作成
            int sdkInt = new AndroidJavaClass("android.os.Build$VERSION").GetStatic<int>("SDK_INT");
            if (sdkInt >= 26)
            {
                CreateNotificationChannel(notificationManager);
            }

            int iconResId = activity.Call<AndroidJavaObject>("getApplicationInfo").Get<int>("icon");

            var builder = new AndroidJavaObject("android.app.Notification$Builder", activity, NOTIFICATION_CHANNEL_ID);
            builder.Call<AndroidJavaObject>("setSmallIcon", iconResId);
            builder.Call<AndroidJavaObject>("setContentTitle", "起床アラーム設定済み");
            builder.Call<AndroidJavaObject>("setContentText", $"起床時刻: {wakeTime:HH:mm}");
            builder.Call<AndroidJavaObject>("setOngoing", true);
            builder.Call<AndroidJavaObject>("setPriority", 2);
            builder.Call<AndroidJavaObject>("setAutoCancel", false);

            var pm = activity.Call<AndroidJavaObject>("getPackageManager");
            var pkg = activity.Call<string>("getPackageName");
            var launchIntent = pm.Call<AndroidJavaObject>("getLaunchIntentForPackage", pkg);


            var piClass = new AndroidJavaClass("android/app/PendingIntent");
            var contentIntent = piClass.CallStatic<AndroidJavaObject>(
                "getActivity", activity, 0, launchIntent, FLAG_IMMUTABLE | FLAG_UPDATE_CURRENT);

            builder.Call<AndroidJavaObject>("setContentIntent", contentIntent);

            var notification = builder.Call<AndroidJavaObject>("build");
            notificationManager.Call("notify", NOTIFICATION_ID, notification);

            GameLogger.Log(LogCategory.Sleep, $"[Alarm] Fallback notification shown for {wakeTime:HH:mm}");
        }
        catch (Exception e)
        {
            GameLogger.LogWarning(LogCategory.Sleep, $"[Alarm] ShowWakeupNotification failed: {e.Message}");
        }
    }

    private void CreateNotificationChannel(AndroidJavaObject notificationManager)
    {
        try
        {
            var channelName = "起床アラーム通知";
            var channelDescription = "設定された起床時刻を通知します";
            int importance = 4; // IMPORTANCE_HIGH

            var channel = new AndroidJavaObject(
                "android.app.NotificationChannel",
                NOTIFICATION_CHANNEL_ID,
                channelName,
                importance
            );

            channel.Call("setDescription", channelDescription);
            channel.Call("setShowBadge", true);
            channel.Call("enableLights", true);
            channel.Call("setLightColor", unchecked((int)0xFF0000FF));

            notificationManager.Call("createNotificationChannel", channel);
            GameLogger.Log(LogCategory.Sleep, "[Alarm] Notification channel created");
        }
        catch (Exception e)
        {
            GameLogger.LogWarning(LogCategory.Sleep, $"[Alarm] CreateNotificationChannel failed: {e.Message}");
        }
    }

    private void CancelNotification(AndroidJavaObject activity)
    {
        try
        {
            var notificationManager = activity.Call<AndroidJavaObject>("getSystemService", "notification");
            notificationManager.Call("cancel", NOTIFICATION_ID);
            GameLogger.Log(LogCategory.Sleep, "[Alarm] Notification cancelled");
        }
        catch (Exception e)
        {
            GameLogger.LogWarning(LogCategory.Sleep, $"[Alarm] CancelNotification failed: {e.Message}");
        }
    }

public void CancelWake()
{
    var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
    var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

    // ① 同じ Intent を再現（component + action 一致）
    var intent = BuildExplicitWakeIntent(activity);

    // ③ 既存の PendingIntent をそのまま取得（無ければ null）
    var piClass = new AndroidJavaClass("android/app/PendingIntent");
    var op = piClass.CallStatic<AndroidJavaObject>(
        "getBroadcast",
        activity,
        WAKE_ALARM_REQ_CODE,
        intent,
        FLAG_IMMUTABLE | FLAG_NO_CREATE
    );

    // ④ 無ければ終了（キャンセル済み）
    if (op == null)
    {
        GameLogger.Log(LogCategory.Sleep, "[Alarm] No existing wake PendingIntent. Already canceled.");
        return;
    }

    // ⑤ AlarmManager.cancel(op) → op.cancel() の順で潰す
    var alarmManager = activity.Call<AndroidJavaObject>("getSystemService", "alarm");
    alarmManager.Call("cancel", op);
    op.Call("cancel");

    GameLogger.Log(LogCategory.Sleep, "[Alarm] Wake alarm canceled.");

    // ⑥ もし setAlarmClock で「時計アイコン表示用」の showIntent を出しているなら、同様に cancel（同じ requestCode or 異なる requestCode を使っていればその番号で）

    // ⑦ 通知を消す（番号は Schedule 側で使ったものに合わせる）
    try
    {
        var ns = activity.Call<AndroidJavaObject>("getSystemService", "notification");
        // 例: 通知IDを 2100 にしていた場合
        ns.Call("cancel", 2100);
    }
    catch { /* なくてもOK */ }

    // ⑧ 保存値（PlayerPrefs / SharedPreferences）をクリア
    try
    {
        UnityEngine.PlayerPrefs.DeleteKey("NextWakeEpochMs");
        UnityEngine.PlayerPrefs.Save();
    }
    catch { /* なくてもOK */ }
}


    public DateTime? GetNextWakeLocal(TimeZoneInfo tz)
    {
        try
        {
            string saved = PlayerPrefs.GetString(NEXT_WAKE_EPOCH_MS_KEY, string.Empty);
            if (long.TryParse(saved, out long ms))
            {
                var utc = DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime;
                var local = TimeZoneInfo.ConvertTimeFromUtc(utc, tz);
                GameLogger.Log(LogCategory.Sleep, $"[Alarm] GetNextWakeLocal: {local:yyyy-MM-dd HH:mm:ss}");
                return local;
            }
            else
            {
                GameLogger.LogWarning(LogCategory.Sleep, "[Alarm] GetNextWakeLocal: No saved wake time");
            }
        }
        catch (Exception e)
        {
            GameLogger.LogWarning(LogCategory.Sleep, $"[Alarm] GetNextWakeLocal failed: {e.Message}");
        }
        return null;
    }

    public bool ShouldAutoWakeFromAlarm()
    {
        GameLogger.Log(LogCategory.Sleep, "========================================");
        GameLogger.Log(LogCategory.Sleep, "[Alarm] ShouldAutoWakeFromAlarm() START");
        GameLogger.Log(LogCategory.Sleep, "========================================");

        try
        {
            var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var activity    = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            var intent      = activity.Call<AndroidJavaObject>("getIntent");

            if (intent == null)
            {
                GameLogger.LogWarning(LogCategory.Sleep, "[Alarm] Intent is null!");
                return CheckTimeWindowFallback();
            }

            // ★ Intentの詳細情報をログ出力
            try
            {
                var action = intent.Call<string>("getAction");
                GameLogger.Log(LogCategory.Sleep, $"[Alarm] Intent action: {action}");
            }
            catch (Exception ex)
            {
                GameLogger.LogWarning(LogCategory.Sleep, $"[Alarm] Failed to get action: {ex.Message}");
            }

            // ★ すべての Extra をログ出力
            try
            {
                var extras = intent.Call<AndroidJavaObject>("getExtras");
                if (extras != null)
                {
                    var keySet = extras.Call<AndroidJavaObject>("keySet");
                    var iterator = keySet.Call<AndroidJavaObject>("iterator");

                    GameLogger.Log(LogCategory.Sleep, "[Alarm] Intent extras:");
                    while (iterator.Call<bool>("hasNext"))
                    {
                        var key = iterator.Call<string>("next");
                        try
                        {
                            var value = extras.Call<AndroidJavaObject>("get", key);
                            GameLogger.Log(LogCategory.Sleep, $"[Alarm]   {key} = {value}");
                        }
                        catch (Exception ex)
                        {
                            GameLogger.Log(LogCategory.Sleep, $"[Alarm]   {key} = (failed to get: {ex.Message})");
                        }
                    }
                }
                else
                {
                    GameLogger.Log(LogCategory.Sleep, "[Alarm] No extras in intent");
                }
            }
            catch (Exception ex)
            {
                GameLogger.LogWarning(LogCategory.Sleep, $"[Alarm] Failed to enumerate extras: {ex.Message}");
            }

            // ★ フラグチェック
            bool fromAlarm = intent.Call<bool>("getBooleanExtra", "tap_alarm_wake", false);
            GameLogger.Log(LogCategory.Sleep, $"[Alarm] tap_alarm_wake flag: {fromAlarm}");

            if (fromAlarm)
            {
                GameLogger.Log(LogCategory.Sleep, "========================================");
                GameLogger.Log(LogCategory.Sleep, "[Alarm] ALARM DETECTED FROM INTENT!");
                GameLogger.Log(LogCategory.Sleep, "========================================");

                // アラーム発火時刻もログ出力
                long triggerTime = intent.Call<long>("getLongExtra", "alarm_trigger_time", 0L);
                if (triggerTime > 0)
                {
                    var triggerDate = DateTimeOffset.FromUnixTimeMilliseconds(triggerTime);
                    GameLogger.Log(LogCategory.Sleep, $"[Alarm] Triggered at: {triggerDate.ToLocalTime()}");
                }

                // フラグ消費
                try {
                    var extras = intent.Call<AndroidJavaObject>("getExtras");
                    if (extras != null) extras.Call("remove", "tap_alarm_wake");
                    else intent.Call<AndroidJavaObject>("putExtra", "tap_alarm_wake", false);
                    GameLogger.Log(LogCategory.Sleep, "[Alarm] Flag consumed");
                } catch (Exception ex) {
                    GameLogger.LogWarning(LogCategory.Sleep, $"[Alarm] consume extra failed: {ex.Message}");
                }

                // 通知キャンセル
                CancelNotification(activity);

                return true;
            }

            // 時刻フォールバック
            return CheckTimeWindowFallback();
        }
        catch (Exception e)
        {
            GameLogger.LogError(LogCategory.Sleep, $"[Alarm] ShouldAutoWakeFromAlarm exception: {e.Message}");
            GameLogger.LogError(LogCategory.Sleep, $"[Alarm] Stack trace: {e.StackTrace}");
            return false;
        }
    }

    /// <summary>
    /// 時刻ウィンドウチェック（フォールバック）
    /// Intentフラグが取得できない場合の保険
    /// </summary>
    private bool CheckTimeWindowFallback()
    {
        GameLogger.Log(LogCategory.Sleep, "[Alarm] Checking time window fallback...");

        string saved = PlayerPrefs.GetString(NEXT_WAKE_EPOCH_MS_KEY, string.Empty);
        if (string.IsNullOrEmpty(saved))
        {
            GameLogger.LogWarning(LogCategory.Sleep, "[Alarm] No saved epoch time");
            return false;
        }

        if (!long.TryParse(saved, out long epochMs))
        {
            GameLogger.LogWarning(LogCategory.Sleep, $"[Alarm] Failed to parse epoch: {saved}");
            return false;
        }

        long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        long diff = nowMs - epochMs;

        GameLogger.Log(LogCategory.Sleep, $"[Alarm] Time window check:");
        GameLogger.Log(LogCategory.Sleep, $"[Alarm]   Saved epoch: {epochMs}");
        GameLogger.Log(LogCategory.Sleep, $"[Alarm]   Current epoch: {nowMs}");
        GameLogger.Log(LogCategory.Sleep, $"[Alarm]   Difference: {diff}ms ({diff / 1000.0:F1}s)");

        // 時刻ウィンドウ: -30秒 〜 +120秒
        if (diff >= -30_000 && diff <= 120_000)
        {
            GameLogger.Log(LogCategory.Sleep, "========================================");
            GameLogger.Log(LogCategory.Sleep, "[Alarm] TIME WINDOW MATCHED!");
            GameLogger.Log(LogCategory.Sleep, "========================================");

            PlayerPrefs.DeleteKey(NEXT_WAKE_EPOCH_MS_KEY);
            PlayerPrefs.Save();

            try
            {
                var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                CancelNotification(activity);
            }
            catch (Exception ex)
            {
                GameLogger.LogWarning(LogCategory.Sleep, $"[Alarm] Failed to cancel notification: {ex.Message}");
            }

            return true;
        }

        // 大幅に過ぎている場合はクリーンアップ
        if (diff > 300_000)
        {
            GameLogger.LogWarning(LogCategory.Sleep, $"[Alarm] Alarm time passed long ago ({diff / 1000.0:F1}s). Cleanup key.");
            PlayerPrefs.DeleteKey(NEXT_WAKE_EPOCH_MS_KEY);
            PlayerPrefs.Save();
        }
        else
        {
            GameLogger.Log(LogCategory.Sleep, $"[Alarm] Outside time window (need -30s~+120s, got {diff / 1000.0:F1}s)");
        }

        return false;
    }
}
#endif