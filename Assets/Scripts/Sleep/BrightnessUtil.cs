using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using TapHouse.Logging;

public class BrightnessUtil : MonoBehaviour
{
    public static class SetBrightness
    {
#if !UNITY_EDITOR && UNITY_IOS
    [DllImport("__Internal")]
    private static extern void setBrightness(float brightness);
#endif

        /// <summary>
        /// 画面の明るさを変更する。
        /// brightness が 0 ～ 1 の場合は暗い状態ではなく、自動明るさをまず試み、
        /// 自動明るさが利用できなければ 0.7f にフォールバックします。
        /// brightness が負の場合は暗め（例として 0.2f）に設定します。
        /// </summary>
        /// <param name="brightness">明るさ (0～1)。0 以下は暗く、1.0 超はクランプされます。</param>
        public static void DoAction(float brightness)
        {
            // 0.0 ～ 1.0 の範囲に Clamp（iOS用として）
            float clampedBrightness = Mathf.Clamp(brightness, 0.0f, 1.0f);

#if UNITY_EDITOR
            // エディタ上では実際の明るさ変更は行わない
            GameLogger.Log(LogCategory.Sleep,$"[SetBrightness] UnityEditor上では明るさを変更しません。指定された値: {brightness}");
#elif UNITY_IOS
        // iOS では自動明るさの変更は推奨されないので、手動の場合のみ設定します
        if (clampedBrightness >= 0.0f && clampedBrightness <= 1.0f)
        {
            try
            {
                setBrightness(clampedBrightness);
                GameLogger.Log(LogCategory.Sleep,$"[SetBrightness] iOSで明るさを {clampedBrightness} に設定しました。");
            }
            catch (Exception e)
            {
                GameLogger.LogWarning(LogCategory.Sleep,$"[SetBrightness] iOS setBrightness でエラーが発生: {e.Message}");
            }
        }
        else
        {
            GameLogger.Log(LogCategory.Sleep,"[SetBrightness] iOSでは自動明るさはOSの標準設定を使用します。");
        }
#elif UNITY_ANDROID
        var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

        activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
        {
            try
            {
                var window = activity.Call<AndroidJavaObject>("getWindow");
                var lp = window.Call<AndroidJavaObject>("getAttributes");

                if (brightness < 0)
                {
                    // 暗い状態の場合は例として 0.1f に設定
                    lp.Set("screenBrightness", 0.1f);
                    GameLogger.Log(LogCategory.Sleep,"[SetBrightness] Androidで画面をやや暗くしました(0.1f)。");
                }
                else
                {
                    // 暗い状態以外は、まず自動明るさ（システム設定に任せる）を試みる
                    try
                    {
                        // -1f を設定すると、システム既定（自動明るさなど）に従います。
                        lp.Set("screenBrightness", -1f);
                        GameLogger.Log(LogCategory.Sleep,"[SetBrightness] Androidで自動明るさを設定しました。");
                    }
                    catch (Exception e)
                    {
                        GameLogger.LogWarning(LogCategory.Sleep,$"[SetBrightness] 自動明るさの設定に失敗: {e.Message}. 0.7f に設定します。");
                        lp.Set("screenBrightness", 0.7f);
                    }
                }
                window.Call("setAttributes", lp);
            }
            catch (Exception ex)
            {
                GameLogger.LogError(LogCategory.Sleep,$"[SetBrightness] Androidでの明るさ調整中に例外発生: {ex.Message}");
            }
        }));
#endif
        }
    }
}
