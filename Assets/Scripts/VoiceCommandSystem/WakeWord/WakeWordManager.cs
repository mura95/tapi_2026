using UnityEngine;
using System.Collections;
using VoiceCommandSystem.Audio;
using TapHouse.Logging;

namespace VoiceCommandSystem.WakeWord
{
    /// <summary>
    /// Wake Word統合マネージャー（スタンドアロン版）
    /// 独立したWake Word検出システム（バックアップ用）
    /// ※ VoiceCommandManagerとの連携は切断済み
    /// </summary>
    [RequireComponent(typeof(WakeWordDetector))]
    public class WakeWordManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Core.AudioRecorder audioRecorder;

        [Header("Wake Word Behavior")]
        [SerializeField] private bool enableWakeWord = true;
        [SerializeField] private bool requireWakeWordForCommands = false;
        [SerializeField] private float commandWindowDuration = 5f;

        [Header("Feedback")]
        [SerializeField] private AudioClip wakeWordDetectedSound;
        [SerializeField] private GameObject wakeWordIndicator;

        [Header("Debug")]
        [SerializeField] private bool showDebugLog = true;

        private WakeWordDetector detector;
        private AudioSource audioSource;
        private bool isCommandWindowActive;
        private float commandWindowEndTime;

        // 状態
        public bool IsWakeWordEnabled => enableWakeWord;
        public bool IsCommandWindowActive => isCommandWindowActive;
        public float CurrentDetectionScore => detector?.CurrentScore ?? 0f;

        private void Awake()
        {
            detector = GetComponent<WakeWordDetector>();
            audioSource = GetComponent<AudioSource>();

            if (audioSource == null && wakeWordDetectedSound != null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            if (wakeWordIndicator != null)
            {
                wakeWordIndicator.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (detector != null)
            {
                detector.OnWakeWordDetected += OnWakeWordDetected;
                detector.OnDetectionScore += OnDetectionScore;
            }

            if (audioRecorder != null)
            {
                audioRecorder.OnAudioDataReady += OnAudioDataReady;
            }
        }

        private void OnDisable()
        {
            if (detector != null)
            {
                detector.OnWakeWordDetected -= OnWakeWordDetected;
                detector.OnDetectionScore -= OnDetectionScore;
            }

            if (audioRecorder != null)
            {
                audioRecorder.OnAudioDataReady -= OnAudioDataReady;
            }
        }

        private void Update()
        {
            // コマンドウィンドウの時間制限チェック
            if (isCommandWindowActive && Time.time >= commandWindowEndTime)
            {
                CloseCommandWindow();
            }
        }

        /// <summary>
        /// 音声データを受信してWake Word検出に渡す
        /// ★ AudioRecorderで既に処理済みのデータを受け取る
        /// </summary>
        private void OnAudioDataReady(float[] audioData)
        {
            if (!enableWakeWord || detector == null)
                return;

            if (audioData == null || audioData.Length == 0)
            {
                Log("Empty audio data received");
                return;
            }

            // ★ AudioRecorderで既にリアルタイム処理 + 最終処理が適用されているので
            // ここでは追加の処理は不要。そのままWake Word検出に渡す
            Log($"Processing audio data: {audioData.Length} samples");

            // Wake Word検出処理
            detector.ProcessAudioData(audioData);
        }

        /// <summary>
        /// Wake Word検出時のコールバック
        /// </summary>
        private void OnWakeWordDetected(float score)
        {
            Log($"Wake word detected! Score: {score:F3}");

            // フィードバック再生
            PlayWakeWordFeedback();

            // コマンドウィンドウを開く
            if (requireWakeWordForCommands)
            {
                OpenCommandWindow();
            }
        }

        /// <summary>
        /// 検出スコア更新時のコールバック
        /// </summary>
        private void OnDetectionScore(float score)
        {
            // UI更新などに使用可能
            // 例: スコアバーの表示
        }

        /// <summary>
        /// コマンドウィンドウを開く
        /// </summary>
        private void OpenCommandWindow()
        {
            isCommandWindowActive = true;
            commandWindowEndTime = Time.time + commandWindowDuration;

            if (wakeWordIndicator != null)
            {
                wakeWordIndicator.SetActive(true);
            }

            Log($"Command window opened for {commandWindowDuration}s");
        }

        /// <summary>
        /// コマンドウィンドウを閉じる
        /// </summary>
        private void CloseCommandWindow()
        {
            isCommandWindowActive = false;

            if (wakeWordIndicator != null)
            {
                wakeWordIndicator.SetActive(false);
            }

            Log("Command window closed");
        }

        /// <summary>
        /// Wake Wordフィードバックを再生
        /// </summary>
        private void PlayWakeWordFeedback()
        {
            if (audioSource != null && wakeWordDetectedSound != null)
            {
                audioSource.PlayOneShot(wakeWordDetectedSound);
            }

            // インジケータ表示
            if (wakeWordIndicator != null && !requireWakeWordForCommands)
            {
                StartCoroutine(ShowTemporaryIndicator());
            }
        }

        /// <summary>
        /// 一時的にインジケータを表示
        /// </summary>
        private IEnumerator ShowTemporaryIndicator()
        {
            wakeWordIndicator.SetActive(true);
            yield return new WaitForSeconds(1f);
            wakeWordIndicator.SetActive(false);
        }

        /// <summary>
        /// Wake Word有効/無効を切り替え
        /// </summary>
        public void SetWakeWordEnabled(bool enabled)
        {
            enableWakeWord = enabled;
            Log($"Wake word {(enabled ? "enabled" : "disabled")}");
        }

        /// <summary>
        /// デバッグログ出力
        /// </summary>
        private void Log(string message)
        {
            if (showDebugLog)
            {
                GameLogger.Log(LogCategory.Voice, $"[WakeWordManager] {message}");
            }
        }

        // 公開API
        public void ForceOpenCommandWindow() => OpenCommandWindow();
        public void ForceCloseCommandWindow() => CloseCommandWindow();
    }
}