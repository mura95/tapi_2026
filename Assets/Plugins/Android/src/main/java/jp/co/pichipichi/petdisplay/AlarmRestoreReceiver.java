package jp.co.pichipichi.petdisplay;

import android.app.AlarmManager;
import android.app.PendingIntent;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.util.Log;
import java.util.Calendar;
import java.util.TimeZone;

/**
 * 端末再起動・時刻変更時にアラームを復旧する BroadcastReceiver
 * BOOT_COMPLETED と TIME_SET を受信して、SharedPreferences から設定を読み込んでアラームを再設定
 */
public class AlarmRestoreReceiver extends BroadcastReceiver {
    private static final String TAG = "AlarmRestoreReceiver";
    private static final String PREFS_NAME = "tap_alarm_cfg";
    private static final int WAKE_ALARM_REQ_CODE = 1001;
    private static final String ACTION_ALARM_WAKE = "jp.co.pichipichi.petdisplay.ACTION_ALARM_WAKE";

    @Override
    public void onReceive(Context context, Intent intent) {
        if (intent == null || context == null) {
            Log.e(TAG, "Intent or Context is null");
            return;
        }

        String action = intent.getAction();
        Log.i(TAG, "onReceive called with action: " + action);

        // BOOT_COMPLETED または TIME_SET のみ処理
        if (!Intent.ACTION_BOOT_COMPLETED.equals(action) 
            && !"android.intent.action.LOCKED_BOOT_COMPLETED".equals(action)
            && !Intent.ACTION_TIME_CHANGED.equals(action)) {
            Log.w(TAG, "Ignoring action: " + action);
            return;
        }

        try {
            restoreAlarm(context);
        } catch (Exception e) {
            Log.e(TAG, "Failed to restore alarm: " + e.getMessage(), e);
        }
    }

    /**
     * SharedPreferences からアラーム情報を読み込んで再設定
     */
    private void restoreAlarm(Context context) {
        SharedPreferences prefs = context.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE);

        // アラーム情報が保存されているか確認
        if (!prefs.contains("wake_hour") || !prefs.contains("wake_min")) {
            Log.w(TAG, "No alarm data found in SharedPreferences");
            return;
        }

        int wakeHour = prefs.getInt("wake_hour", -1);
        int wakeMin = prefs.getInt("wake_min", -1);
        String tzId = prefs.getString("tz_id", TimeZone.getDefault().getID());
        int tzOffsetMin = prefs.getInt("tz_offset_min", 0);

        if (wakeHour < 0 || wakeMin < 0 || wakeHour > 23 || wakeMin > 59) {
            Log.e(TAG, "Invalid alarm time: " + wakeHour + ":" + wakeMin);
            return;
        }

        Log.i(TAG, String.format("Restoring alarm: %02d:%02d (tz: %s)", wakeHour, wakeMin, tzId));

        // タイムゾーンを設定
        TimeZone tz;
        try {
            tz = TimeZone.getTimeZone(tzId);
        } catch (Exception e) {
            Log.w(TAG, "Invalid timezone ID: " + tzId + ", using default");
            tz = TimeZone.getDefault();
        }

        // 次回のアラーム時刻を計算
        Calendar calendar = Calendar.getInstance(tz);
        calendar.set(Calendar.HOUR_OF_DAY, wakeHour);
        calendar.set(Calendar.MINUTE, wakeMin);
        calendar.set(Calendar.SECOND, 0);
        calendar.set(Calendar.MILLISECOND, 0);

        // 過去の時刻なら翌日に設定
        long now = System.currentTimeMillis();
        if (calendar.getTimeInMillis() <= now) {
            calendar.add(Calendar.DAY_OF_MONTH, 1);
            Log.d(TAG, "Alarm time is in the past, scheduling for tomorrow");
        }

        long triggerAtMs = calendar.getTimeInMillis();
        Log.i(TAG, "Next alarm: " + calendar.getTime() + " (epoch: " + triggerAtMs + ")");

        // AlarmManager でアラームを再設定
        AlarmManager alarmManager = (AlarmManager) context.getSystemService(Context.ALARM_SERVICE);
        if (alarmManager == null) {
            Log.e(TAG, "AlarmManager is null");
            return;
        }

        // Broadcast用のIntent作成
        Intent alarmIntent = new Intent(ACTION_ALARM_WAKE);
        alarmIntent.setPackage(context.getPackageName());
        alarmIntent.putExtra("wake_hour", wakeHour);
        alarmIntent.putExtra("wake_min", wakeMin);
        alarmIntent.putExtra("tz_id", tzId);
        alarmIntent.putExtra("tz_offset_min", tzOffsetMin);

        PendingIntent pendingIntent = PendingIntent.getBroadcast(
            context,
            WAKE_ALARM_REQ_CODE,
            alarmIntent,
            PendingIntent.FLAG_IMMUTABLE | PendingIntent.FLAG_UPDATE_CURRENT
        );

        // showIntent（目覚ましアイコンタップ時にアプリを起動）
        Intent launchIntent = context.getPackageManager()
            .getLaunchIntentForPackage(context.getPackageName());
        
        if (launchIntent != null) {
            launchIntent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK 
                | Intent.FLAG_ACTIVITY_SINGLE_TOP 
                | Intent.FLAG_ACTIVITY_CLEAR_TOP);
        }

        PendingIntent showIntent = PendingIntent.getActivity(
            context, 
            0, 
            launchIntent != null ? launchIntent : new Intent(), 
            PendingIntent.FLAG_IMMUTABLE | PendingIntent.FLAG_UPDATE_CURRENT
        );

        // setAlarmClock で再設定（目覚ましアイコン付き）
        try {
            AlarmManager.AlarmClockInfo alarmClockInfo = 
                new AlarmManager.AlarmClockInfo(triggerAtMs, showIntent);
            alarmManager.setAlarmClock(alarmClockInfo, pendingIntent);

            // SharedPreferences を更新
            prefs.edit().putLong("next_epoch_ms", triggerAtMs).apply();

            Log.i(TAG, "Alarm restored successfully: " + calendar.getTime());
        } catch (SecurityException e) {
            Log.e(TAG, "SecurityException: Missing SCHEDULE_EXACT_ALARM permission", e);
        } catch (Exception e) {
            Log.e(TAG, "Failed to set alarm: " + e.getMessage(), e);
        }
    }
}