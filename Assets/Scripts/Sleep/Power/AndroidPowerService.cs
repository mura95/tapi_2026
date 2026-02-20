#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine;
using TapHouse.Logging;

public sealed class AndroidPowerService : IPowerService
{
    private const string WAKELOCK_TAG = "tap:morning_wake";
    private AndroidJavaObject _wakeLock;

    public void ClearKeepScreenOn()
    {
        try
        {
            // WakeLockをリリース（スリープを許可するため）
            ReleaseWakeLock();

            var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var activity    = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
            {
                try
                {
                    var window = activity.Call<AndroidJavaObject>("getWindow");
                    int FLAG_KEEP_SCREEN_ON = 0x00000080;
                    window.Call("clearFlags", FLAG_KEEP_SCREEN_ON);

                    var decor = window.Call<AndroidJavaObject>("getDecorView");
                    if (decor != null) decor.Call("setKeepScreenOn", false);

                    GameLogger.Log(LogCategory.Sleep,"[Power] KEEP_SCREEN_ON cleared");
                }
                catch (System.Exception ex) { GameLogger.LogWarning(LogCategory.Sleep,$"[Power] Clear failed: {ex.Message}"); }
            }));
        }
        catch (System.Exception e) { GameLogger.LogWarning(LogCategory.Sleep,$"[Power] Clear outer failed: {e.Message}"); }
    }

    private void ReleaseWakeLock()
    {
        try
        {
            if (_wakeLock != null && _wakeLock.Call<bool>("isHeld"))
            {
                _wakeLock.Call("release");
                GameLogger.Log(LogCategory.Sleep, "[Power] WakeLock released in ClearKeepScreenOn");
            }
        }
        catch (System.Exception e)
        {
            GameLogger.LogWarning(LogCategory.Sleep, $"[Power] ReleaseWakeLock failed: {e.Message}");
        }
    }

    public void TurnScreenOn()
    {
        try
        {
            var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var activity    = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
            {
                try
                {
                    try { activity.Call("setShowWhenLocked", true); } catch {}
                    try { activity.Call("setTurnScreenOn",  true); } catch {}

                    var window = activity.Call<AndroidJavaObject>("getWindow");
                    int FLAG_SHOW_WHEN_LOCKED = 0x00080000;
                    int FLAG_TURN_SCREEN_ON   = 0x00200000;
                    int FLAG_KEEP_SCREEN_ON   = 0x00000080;
                    window.Call("addFlags", FLAG_SHOW_WHEN_LOCKED);
                    window.Call("addFlags", FLAG_TURN_SCREEN_ON);
                    window.Call("addFlags", FLAG_KEEP_SCREEN_ON);

                    GameLogger.Log(LogCategory.Sleep,"[Power] Screen-on flags set");
                }
                catch (System.Exception ex) { GameLogger.LogWarning(LogCategory.Sleep,$"[Power] Flags failed: {ex.Message}"); }
            }));

            AcquireShortWakeLock(activity, 5000);
        }
        catch (System.Exception e) { GameLogger.LogWarning(LogCategory.Sleep,$"[Power] TurnScreenOn outer failed: {e.Message}"); }
    }

    public void Release()
    {
        ReleaseWakeLock();
        _wakeLock = null;
    }

    private void AcquireShortWakeLock(AndroidJavaObject activity, int durationMs)
    {
        try
        {
            var pm = activity.Call<AndroidJavaObject>("getSystemService", "power");
            int SCREEN_BRIGHT_WAKE_LOCK = 0x0000000a;
            int ACQUIRE_CAUSES_WAKEUP   = 0x10000000;

            _wakeLock = pm.Call<AndroidJavaObject>("newWakeLock",
                SCREEN_BRIGHT_WAKE_LOCK | ACQUIRE_CAUSES_WAKEUP, WAKELOCK_TAG);

            if (_wakeLock != null && !_wakeLock.Call<bool>("isHeld"))
            {
                _wakeLock.Call("acquire");
                GameLogger.Log(LogCategory.Sleep,$"[Power] WakeLock acquired");
            }
        }
        catch (System.Exception e) { GameLogger.LogWarning(LogCategory.Sleep,$"[Power] Acquire failed: {e.Message}"); }
    }
}
#endif
