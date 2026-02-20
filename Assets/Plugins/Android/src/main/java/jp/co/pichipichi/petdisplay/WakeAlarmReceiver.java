package jp.co.pichipichi.petdisplay;

import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.os.PowerManager;
import android.util.Log;

/**
 * アラームが発火した時にアプリを起動し、画面も確実に点灯させる BroadcastReceiver。
 * AndroidManifest.xml で "jp.co.pichipichi.petdisplay.ACTION_ALARM_WAKE" を受信。
 */
public class WakeAlarmReceiver extends BroadcastReceiver {
    private static final String TAG = "WakeAlarmReceiver";
    private static final String ACTION_ALARM_WAKE = "jp.co.pichipichi.petdisplay.ACTION_ALARM_WAKE";

    @Override
    public void onReceive(Context context, Intent intent) {
        Log.i(TAG, "==================== ALARM RECEIVED ====================");
        if (intent == null || context == null) {
            Log.e(TAG, "Intent or Context is null");
            return;
        }

        String action = intent.getAction();
        Log.i(TAG, "Action: " + action);
        Log.i(TAG, "Time (ms): " + System.currentTimeMillis());

        if (!ACTION_ALARM_WAKE.equals(action)) {
            Log.w(TAG, "Unknown action: " + action);
            return;
        }

        // ★ 画面を点灯させる
        try {
            PowerManager powerManager = (PowerManager) context.getSystemService(Context.POWER_SERVICE);
            if (powerManager != null) {
                PowerManager.WakeLock wakeLock = powerManager.newWakeLock(
                    PowerManager.SCREEN_BRIGHT_WAKE_LOCK | PowerManager.ACQUIRE_CAUSES_WAKEUP,
                    "petdisplay::WakeLock"
                );
                wakeLock.acquire(10000); // 10秒間画面を明るく保つ
                Log.i(TAG, "WakeLock acquired: screen should light up");
            } else {
                Log.w(TAG, "PowerManager not available, cannot acquire WakeLock");
            }
        } catch (Exception e) {
            Log.e(TAG, "Failed to acquire WakeLock: " + e.getMessage(), e);
        }

        try {
            Intent launchIntent = context.getPackageManager()
                .getLaunchIntentForPackage(context.getPackageName());

            if (launchIntent == null) {
                Log.e(TAG, "Launch intent is null for package: " + context.getPackageName());
                return;
            }

            // 受け取ったアラーム情報を抽出
            int wakeHour = intent.getIntExtra("wake_hour", -1);
            int wakeMin = intent.getIntExtra("wake_min", -1);
            String tzId = intent.getStringExtra("tz_id");
            int tzOffsetMin = intent.getIntExtra("tz_offset_min", 0);

            // 起動用フラグ追加
            launchIntent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK 
                                | Intent.FLAG_ACTIVITY_SINGLE_TOP 
                                | Intent.FLAG_ACTIVITY_CLEAR_TOP);

            // Unity 側に渡す extra データ
            launchIntent.putExtra("tap_alarm_wake", true);
            launchIntent.putExtra("wake_hour", wakeHour);
            launchIntent.putExtra("wake_min", wakeMin);
            launchIntent.putExtra("tz_id", tzId);
            launchIntent.putExtra("tz_offset_min", tzOffsetMin);
            launchIntent.putExtra("alarm_trigger_time", System.currentTimeMillis());

            Log.i(TAG, "Launching Unity with alarm data:");
            Log.i(TAG, "  wake_hour = " + wakeHour);
            Log.i(TAG, "  wake_min = " + wakeMin);
            Log.i(TAG, "  tz_id = " + tzId);
            Log.i(TAG, "  alarm_trigger_time = " + System.currentTimeMillis());

            context.startActivity(launchIntent);
            Log.i(TAG, "App launch triggered");

        } catch (Exception e) {
            Log.e(TAG, "Failed to launch Unity app: " + e.getMessage(), e);
        }
    }
}
