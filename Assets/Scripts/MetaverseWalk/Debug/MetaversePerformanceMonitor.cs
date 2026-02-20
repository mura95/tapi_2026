using UnityEngine;
using TMPro;
using TapHouse.MetaverseWalk.Network;

namespace TapHouse.MetaverseWalk.DebugTools
{
    /// <summary>
    /// メタバース散歩のパフォーマンスモニタリング
    /// テスト段階でのデバッグ情報表示・ログ出力を行う
    ///
    /// 使い方: MetaverseシーンのCanvasにアタッチし、TMP_Textを設定
    /// Inspector上の showOverlay をONにするとリアルタイム表示
    /// </summary>
    public class MetaversePerformanceMonitor : MonoBehaviour
    {
        [Header("表示設定")]
        [SerializeField] private bool showOverlay = true;
        [SerializeField] private TMP_Text overlayText;

        [Header("ログ設定")]
        [SerializeField] private bool logToConsole = false;
        [SerializeField] private float logIntervalSec = 5f;

        // FPS計測
        private float fpsAccumulator;
        private int fpsFrameCount;
        private float fpsUpdateInterval = 0.5f;
        private float fpsNextUpdate;
        private float currentFps;
        private float minFps = float.MaxValue;
        private float maxFps;

        // フレーム時間
        private float worstFrameTime;

        // ログ出力タイミング
        private float nextLogTime;

        // 累積統計
        private float sessionStartTime;
        private int totalFrames;

        private void Start()
        {
            sessionStartTime = Time.realtimeSinceStartup;
            fpsNextUpdate = Time.realtimeSinceStartup + fpsUpdateInterval;
            nextLogTime = Time.realtimeSinceStartup + logIntervalSec;
        }

        private void Update()
        {
            totalFrames++;
            float frameTime = Time.unscaledDeltaTime;

            // ワーストフレーム時間を追跡
            if (frameTime > worstFrameTime)
            {
                worstFrameTime = frameTime;
            }

            // FPS計算（0.5秒間隔で更新）
            fpsAccumulator += Time.timeScale / frameTime;
            fpsFrameCount++;

            if (Time.realtimeSinceStartup >= fpsNextUpdate)
            {
                currentFps = fpsAccumulator / fpsFrameCount;
                fpsAccumulator = 0f;
                fpsFrameCount = 0;
                fpsNextUpdate = Time.realtimeSinceStartup + fpsUpdateInterval;

                if (currentFps < minFps && totalFrames > 30) // 最初の数フレームは除外
                {
                    minFps = currentFps;
                }
                if (currentFps > maxFps)
                {
                    maxFps = currentFps;
                }
            }

            // オーバーレイ更新
            if (showOverlay && overlayText != null)
            {
                UpdateOverlay();
            }

            // 定期ログ出力
            if (logToConsole && Time.realtimeSinceStartup >= nextLogTime)
            {
                LogPerformanceSnapshot();
                nextLogTime = Time.realtimeSinceStartup + logIntervalSec;
            }
        }

        private void UpdateOverlay()
        {
            var networkManager = PhotonNetworkManager.Instance;
            string networkStatus = "Offline";
            int playerCount = 0;
            string roomName = "-";

            if (networkManager != null)
            {
                networkStatus = networkManager.IsConnected ? "Connected" : "Disconnected";
                if (networkManager.IsReconnecting) networkStatus = "Reconnecting";
                playerCount = networkManager.PlayerCount;
                roomName = networkManager.RoomName ?? "-";
            }

            // エラー情報
            string errorInfo = "";
            var lastError = MetaverseErrorHandler.LastError;
            if (lastError.HasValue)
            {
                errorInfo = $"\n<color=red>Error: {lastError.Value.Code}</color>";
            }

            // 位置同期エラー（デバッグ用）
            string syncInfo = GetSyncDebugInfo();

            overlayText.text =
                $"<b>Performance Monitor</b>\n" +
                $"FPS: {currentFps:F0} (min:{minFps:F0} max:{maxFps:F0})\n" +
                $"Frame: {frameTime * 1000:F1}ms (worst:{worstFrameTime * 1000:F1}ms)\n" +
                $"Network: {networkStatus}\n" +
                $"Players: {playerCount}\n" +
                $"Room: {roomName}\n" +
                $"Session: {Time.realtimeSinceStartup - sessionStartTime:F0}s" +
                syncInfo +
                errorInfo;
        }

        // ローカル変数の値を表示に使うためのフィールド
        private float frameTime => Time.unscaledDeltaTime;

        private string GetSyncDebugInfo()
        {
            // NetworkDogController/NetworkPlayerControllerのDebugPositionErrorを取得
            var dogControllers = FindObjectsByType<NetworkDogController>(FindObjectsSortMode.None);
            var playerControllers = FindObjectsByType<NetworkPlayerController>(FindObjectsSortMode.None);

            float maxDogError = 0f;
            float maxPlayerError = 0f;

            foreach (var dog in dogControllers)
            {
                if (dog.DebugPositionError > maxDogError)
                    maxDogError = dog.DebugPositionError;
            }

            foreach (var player in playerControllers)
            {
                if (player.DebugPositionError > maxPlayerError)
                    maxPlayerError = player.DebugPositionError;
            }

            if (maxDogError > 0.01f || maxPlayerError > 0.01f)
            {
                return $"\nSync: Dog={maxDogError:F2}m Player={maxPlayerError:F2}m";
            }
            return "";
        }

        private void LogPerformanceSnapshot()
        {
            float sessionDuration = Time.realtimeSinceStartup - sessionStartTime;
            var networkManager = PhotonNetworkManager.Instance;

            string log =
                $"[MetaversePerf] " +
                $"FPS:{currentFps:F0} " +
                $"Min:{minFps:F0} " +
                $"Worst:{worstFrameTime * 1000:F1}ms " +
                $"Players:{networkManager?.PlayerCount ?? 0} " +
                $"Connected:{networkManager?.IsConnected ?? false} " +
                $"Session:{sessionDuration:F0}s " +
                $"Memory:{SystemInfo.systemMemorySize}MB";

            UnityEngine.Debug.Log(log);
        }

        /// <summary>
        /// オーバーレイ表示の切り替え（HUDのデバッグボタンから呼べる）
        /// </summary>
        public void ToggleOverlay()
        {
            showOverlay = !showOverlay;
            if (overlayText != null)
            {
                overlayText.gameObject.SetActive(showOverlay);
            }
        }

        /// <summary>
        /// パフォーマンス統計をリセット
        /// </summary>
        public void ResetStats()
        {
            minFps = float.MaxValue;
            maxFps = 0f;
            worstFrameTime = 0f;
            sessionStartTime = Time.realtimeSinceStartup;
            totalFrames = 0;
        }
    }
}
