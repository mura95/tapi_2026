using UnityEngine;
using System;
using TapHouse.Logging;

namespace VoiceCommandSystem.Core
{
    /// <summary>
    /// 顔認識トリガー音声入力
    /// - 顔が認識されている間のみマイクを有効化
    /// - 音量がしきい値を超えたら録音開始（VAD経由）
    /// - 顔がいなくなったらマイクを無効化（バッテリー節約）
    /// </summary>
    public class FaceTriggeredVoiceInput : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] private FacePresenceDetector faceDetector;
        [SerializeField] private AudioRecorder audioRecorder;

        [Header("設定")]
        [SerializeField] private bool enableFaceTrigger = true;
        [Tooltip("顔検出後、マイク有効化までの遅延（秒）")]
        [SerializeField] private float activationDelay = 0.5f;
        [Tooltip("顔消失後、マイク無効化までの猶予時間（秒）")]
        [SerializeField] private float deactivationGracePeriod = 2.0f;

        [Header("デバッグ")]
        [SerializeField] private bool showDebugLog = true;

        // イベント
        public event Action OnVoiceInputStarted;
        public event Action<float[]> OnVoiceInputEnded;

        // 状態
        private bool isFacePresent = false;
        private bool isMicrophoneActive = false;
        private float faceDetectedTime = 0f;
        private float faceLostTime = 0f;
        private bool isSubscribedToFace = false;
        private bool isSubscribedToAudio = false;

        // 公開プロパティ
        public bool IsFacePresent => isFacePresent;
        public bool IsMicrophoneActive => isMicrophoneActive;
        public bool IsEnabled => enableFaceTrigger;

        private void Start()
        {
            ValidateReferences();
            SubscribeToFaceDetection();
            SubscribeToAudioRecorder();
        }

        private void OnEnable()
        {
            if (!isSubscribedToFace) SubscribeToFaceDetection();
            if (!isSubscribedToAudio) SubscribeToAudioRecorder();
        }

        private void OnDisable()
        {
            if (isMicrophoneActive)
            {
                DeactivateMicrophone();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromFaceDetection();
            UnsubscribeFromAudioRecorder();
        }

        private void Update()
        {
            if (!enableFaceTrigger) return;

            // 顔検出後の遅延処理
            if (isFacePresent && !isMicrophoneActive)
            {
                if (Time.time - faceDetectedTime >= activationDelay)
                {
                    ActivateMicrophone();
                }
            }

            // 顔消失後の猶予時間処理
            if (!isFacePresent && isMicrophoneActive)
            {
                if (Time.time - faceLostTime >= deactivationGracePeriod)
                {
                    DeactivateMicrophone();
                }
            }
        }

        #region 顔検出イベント処理

        private void SubscribeToFaceDetection()
        {
            if (faceDetector != null && !isSubscribedToFace)
            {
                faceDetector.onFacePresenceChanged.AddListener(OnFacePresenceChanged);
                isSubscribedToFace = true;
                Log("Subscribed to FacePresenceDetector");
            }
        }

        private void UnsubscribeFromFaceDetection()
        {
            if (faceDetector != null && isSubscribedToFace)
            {
                faceDetector.onFacePresenceChanged.RemoveListener(OnFacePresenceChanged);
                isSubscribedToFace = false;
            }
        }

        private void OnFacePresenceChanged(bool isPresent)
        {
            if (!enableFaceTrigger) return;

            if (isPresent)
            {
                isFacePresent = true;
                faceDetectedTime = Time.time;
                Log("Face detected - will activate microphone after delay");
            }
            else
            {
                isFacePresent = false;
                faceLostTime = Time.time;
                Log("Face lost - will deactivate microphone after grace period");
            }
        }

        #endregion

        #region マイク制御

        private void ActivateMicrophone()
        {
            if (isMicrophoneActive) return;
            if (audioRecorder == null)
            {
                GameLogger.LogError(LogCategory.Voice,
                    "[FaceTriggeredVoiceInput] AudioRecorder is null!");
                return;
            }

            isMicrophoneActive = true;

            // AudioRecorderをContinuousVADモードで開始
            audioRecorder.SetMode(AudioRecorder.RecordingMode.ContinuousVAD);
            audioRecorder.StartContinuousListening();

            OnVoiceInputStarted?.Invoke();
            Log("Microphone ACTIVATED (face present)");
        }

        private void DeactivateMicrophone()
        {
            if (!isMicrophoneActive) return;
            if (audioRecorder == null) return;

            isMicrophoneActive = false;

            // AudioRecorderを停止
            audioRecorder.StopContinuousListening();

            Log("Microphone DEACTIVATED (face absent)");
        }

        #endregion

        #region AudioRecorderイベント処理

        private void SubscribeToAudioRecorder()
        {
            if (audioRecorder != null && !isSubscribedToAudio)
            {
                audioRecorder.OnAudioDataReady += OnAudioDataReady;
                isSubscribedToAudio = true;
                Log("Subscribed to AudioRecorder");
            }
        }

        private void UnsubscribeFromAudioRecorder()
        {
            if (audioRecorder != null && isSubscribedToAudio)
            {
                audioRecorder.OnAudioDataReady -= OnAudioDataReady;
                isSubscribedToAudio = false;
            }
        }

        private void OnAudioDataReady(float[] audioData)
        {
            if (!enableFaceTrigger || !isMicrophoneActive) return;

            Log($"Voice input received: {audioData.Length} samples ({audioData.Length / 16000f:F2}s)");
            OnVoiceInputEnded?.Invoke(audioData);
        }

        #endregion

        #region 公開API

        public void SetEnabled(bool enabled)
        {
            enableFaceTrigger = enabled;

            if (!enabled && isMicrophoneActive)
            {
                DeactivateMicrophone();
            }

            Log($"FaceTriggeredVoiceInput {(enabled ? "ENABLED" : "DISABLED")}");
        }

        /// <summary>
        /// 手動でマイクを強制起動（テスト用）
        /// </summary>
        public void ForceActivateMicrophone()
        {
            if (!isMicrophoneActive)
            {
                isFacePresent = true;
                faceDetectedTime = Time.time - activationDelay; // 遅延をスキップ
                ActivateMicrophone();
            }
        }

        /// <summary>
        /// 手動でマイクを強制停止（テスト用）
        /// </summary>
        public void ForceDeactivateMicrophone()
        {
            if (isMicrophoneActive)
            {
                isFacePresent = false;
                DeactivateMicrophone();
            }
        }

        #endregion

        #region ユーティリティ

        private void ValidateReferences()
        {
            if (faceDetector == null)
            {
                faceDetector = FindObjectOfType<FacePresenceDetector>();
                if (faceDetector == null)
                {
                    GameLogger.LogWarning(LogCategory.Voice,
                        "[FaceTriggeredVoiceInput] FacePresenceDetector not found!");
                }
            }

            if (audioRecorder == null)
            {
                audioRecorder = GetComponent<AudioRecorder>();
                if (audioRecorder == null)
                {
                    audioRecorder = FindObjectOfType<AudioRecorder>();
                }
                if (audioRecorder == null)
                {
                    GameLogger.LogWarning(LogCategory.Voice,
                        "[FaceTriggeredVoiceInput] AudioRecorder not found!");
                }
            }
        }

        private void Log(string message)
        {
            if (showDebugLog)
            {
                GameLogger.Log(LogCategory.Voice, $"[FaceTriggeredVoiceInput] {message}");
            }
        }

        #endregion
    }
}
