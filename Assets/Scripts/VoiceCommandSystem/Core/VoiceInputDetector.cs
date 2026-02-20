using UnityEngine;
using System;
using TapHouse.Logging;

namespace VoiceCommandSystem.Core
{
    /// <summary>
    /// 3本指タッチによる音声入力検出
    /// タッチ開始/終了イベントを発火
    /// </summary>
    public class VoiceInputDetector : MonoBehaviour
    {
        [Header("設定")]
        [SerializeField] private int requiredFingers = 3;
        [SerializeField] private float minimumHoldTime = 0.3f; // 最低保持時間（秒）
        [SerializeField] private bool vibrationFeedback = true; // バイブレーションフィードバック

        [Header("デバッグトリガー制御")]
        [Tooltip("デバッグ用トリガー（3本指タッチ/Rキー）の有効/無効。本番ではfalseに設定")]
        [SerializeField] private bool enableDebugTrigger = true;

        [Header("デバッグ")]
        [SerializeField] private bool showDebugGUI = true;
        [SerializeField] private KeyCode desktopTestKey = KeyCode.R; // デスクトップテスト用

        // イベント
        public event Action OnRecordingStarted;
        public event Action OnRecordingStopped;
        public event Action<float> OnRecordingProgress; // 録音時間（秒）

        private bool isRecording = false;
        private float recordingStartTime = 0f;
        private int lastTouchCount = 0;

        void Update()
        {
            // デバッグトリガーが無効の場合は入力を処理しない
            if (!enableDebugTrigger) return;

            // デスクトップ環境でのテスト用
#if UNITY_EDITOR || UNITY_STANDALONE
            HandleDesktopInput();
#else
            HandleMobileTouch();
#endif

            // 録音中の経過時間を通知
            if (isRecording)
            {
                float elapsed = Time.time - recordingStartTime;
                OnRecordingProgress?.Invoke(elapsed);
            }
        }

        /// <summary>
        /// モバイル端末のタッチ入力処理
        /// </summary>
        private void HandleMobileTouch()
        {
            int currentTouchCount = Input.touchCount;

            // タッチ数が変化した場合のみ処理
            if (currentTouchCount == lastTouchCount)
            {
                lastTouchCount = currentTouchCount;
                return;
            }

            lastTouchCount = currentTouchCount;

            // 3本指タッチで録音開始
            if (currentTouchCount >= requiredFingers && !isRecording)
            {
                StartRecording();
            }
            // 指が離れたら録音停止
            else if (currentTouchCount < requiredFingers && isRecording)
            {
                StopRecording();
            }
        }

        /// <summary>
        /// デスクトップ環境でのテスト用入力処理
        /// </summary>
        private void HandleDesktopInput()
        {
            if (Input.GetKeyDown(desktopTestKey) && !isRecording)
            {
                StartRecording();
            }
            else if (Input.GetKeyUp(desktopTestKey) && isRecording)
            {
                StopRecording();
            }
        }

        /// <summary>
        /// 録音開始
        /// </summary>
        private void StartRecording()
        {
            isRecording = true;
            recordingStartTime = Time.time;

            GameLogger.Log(LogCategory.Voice,$"[VoiceInput] 🎙️ Recording started (fingers: {lastTouchCount})");

            // バイブレーションフィードバック
            if (vibrationFeedback)
            {
#if UNITY_ANDROID || UNITY_IOS
                Handheld.Vibrate();
#endif
            }

            OnRecordingStarted?.Invoke();
        }

        /// <summary>
        /// 録音停止
        /// </summary>
        private void StopRecording()
        {
            if (!isRecording) return;

            float recordingDuration = Time.time - recordingStartTime;

            // 最低保持時間に満たない場合はキャンセル
            if (recordingDuration < minimumHoldTime)
            {
                GameLogger.Log(LogCategory.Voice,$"[VoiceInput] ⚠️ Recording too short ({recordingDuration:F2}s < {minimumHoldTime}s) - Cancelled");
                isRecording = false;
                return;
            }

            isRecording = false;
            GameLogger.Log(LogCategory.Voice,$"[VoiceInput] 🛑 Recording stopped (duration: {recordingDuration:F2}s)");

            OnRecordingStopped?.Invoke();
        }

        /// <summary>
        /// 現在録音中かどうか
        /// </summary>
        public bool IsRecording => isRecording;

        /// <summary>
        /// 録音経過時間
        /// </summary>
        public float RecordingDuration => isRecording ? Time.time - recordingStartTime : 0f;

        /// <summary>
        /// デバッグトリガーが有効かどうか
        /// </summary>
        public bool IsDebugTriggerEnabled => enableDebugTrigger;

        /// <summary>
        /// デバッグトリガーの有効/無効を設定
        /// </summary>
        public void SetDebugTriggerEnabled(bool enabled)
        {
            enableDebugTrigger = enabled;
            GameLogger.Log(LogCategory.Voice,
                $"[VoiceInputDetector] Debug trigger {(enabled ? "ENABLED" : "DISABLED")}");
        }

        void OnGUI()
        {
            if (!showDebugGUI) return;

            GUIStyle style = new GUIStyle(GUI.skin.label);

            Texture2D bgTexture = new Texture2D(1, 1);
            bgTexture.SetPixel(0, 0, new Color(0, 0, 0, 0.5f)); // 半透明黒
            bgTexture.Apply();
            style.normal.background = bgTexture;

            style.fontSize = 24;
            style.normal.textColor = isRecording ? Color.red : Color.white;

            style.padding = new RectOffset(10, 10, 10, 10);

            string status = isRecording
                ? $"🎙️ 録音中... ({RecordingDuration:F1}s)"
                : $"タッチ数: {lastTouchCount}";

#if UNITY_EDITOR || UNITY_STANDALONE
            status += $"\n[{desktopTestKey}キー長押しでテスト]";
#endif

            GUI.Label(new Rect(Screen.width / 2 - 250, 10, 500, 100), status, style);
        }
    }
}