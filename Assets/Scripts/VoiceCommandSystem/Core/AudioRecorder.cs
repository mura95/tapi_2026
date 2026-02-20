using UnityEngine;
using System;
using TapHouse.Logging;

namespace VoiceCommandSystem.Core
{
    /// <summary>
    /// 音声録音システム - メインクラス
    /// 責任: 録音、Pipeline適用、保存、イベント配信
    /// </summary>
    public partial class AudioRecorder : MonoBehaviour
    {
        #region Inspector Settings

        [Header("録音設定")]
        [SerializeField] private int sampleRate = 16000;
        [SerializeField] private int maxRecordSeconds = 30;
        [SerializeField] private int selectedMicrophoneIndex = 0;

        [Header("★ 録音モード選択")]
        [SerializeField] private RecordingMode recordingMode = RecordingMode.PushToTalk;

        [Header("Push-to-Talk設定")]
        [SerializeField] private KeyCode pushToTalkKey = KeyCode.Tab;
        [SerializeField] private bool useMouseButton = false;
        [SerializeField] private int mouseButtonIndex = 0;

        [Header("VAD設定（常時録音モード用）")]
        [SerializeField] private float vadEnergyThreshold = 0.01f;
        [SerializeField] private float vadSilenceDuration = 0.5f;
        [SerializeField] private float vadMaxRecordingDuration = 3f;
        [SerializeField] private int chunkSize = 1024;

        [Header("音声処理")]
        [Tooltip("テスト結果: 処理なし(Raw)が最も良い音質のため、デフォルトはOFF")]
        [SerializeField] private bool applyRealtimeProcessing = false;
        [SerializeField] private bool applyProcessing = false;

        [Header("★ 自動保存設定")]
        [SerializeField] private bool autoSaveRecordings = true;
        [SerializeField] private string saveFolderName = "ProcessedRecordings";
        [SerializeField] private string filePrefix = "processed";

        [Header("デバッグ")]
        [SerializeField] private bool showPTTStatus = true;
        [SerializeField] private bool showDebugLog = true;

        #endregion

        #region Enums

        public enum RecordingMode
        {
            PushToTalk,      // キー押下で録音
            ContinuousVAD    // 常時監視、音声検出で録音
        }

        #endregion

        #region Events

        /// <summary>
        /// Pipeline適用後の音声データ配信イベント
        /// </summary>
        public event Action<float[]> OnAudioDataReady;

        #endregion

        #region Private Fields

        private AudioClip micClip;
        private bool isRecording = false;
        private int lastMicPosition = 0;
        private string microphoneDevice = null;
        private bool isPTTPressed = false;
        private string savePath;
        private int recordingCounter = 0;

        // VAD & Processing
        private Audio.VoiceActivityDetector vad;
        private Audio.RealtimeAudioProcessor realtimeProcessor;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // ★重要: テスト結果により、音声処理なし（Raw）が最良と判明
            // Inspectorのシリアライズ値を強制的にオーバーライド
            ForceDisableAudioProcessing();

            InitializeMicrophone();
            InitializeFileSystem();
            InitializeProcessing();
        }

        /// <summary>
        /// 音声処理を強制的に無効化
        /// テスト結果: 処理なし(Raw)が最も良い音質のため
        /// </summary>
        private void ForceDisableAudioProcessing()
        {
            if (applyRealtimeProcessing || applyProcessing)
            {
                GameLogger.Log(LogCategory.Voice,
                    "════════════════════════════════════════════════════");
                GameLogger.Log(LogCategory.Voice,
                    "⚠️ [AudioRecorder] 音声処理を強制無効化");
                GameLogger.Log(LogCategory.Voice,
                    $"   Realtime: {applyRealtimeProcessing} → false");
                GameLogger.Log(LogCategory.Voice,
                    $"   Final: {applyProcessing} → false");
                GameLogger.Log(LogCategory.Voice,
                    "   理由: テスト結果により処理なし(Raw)が最良と判明");
                GameLogger.Log(LogCategory.Voice,
                    "════════════════════════════════════════════════════");
            }

            applyRealtimeProcessing = false;
            applyProcessing = false;
        }

        void Update()
        {
            if (recordingMode == RecordingMode.PushToTalk)
            {
                HandlePushToTalk();
            }
            else if (recordingMode == RecordingMode.ContinuousVAD)
            {
                HandleContinuousVAD();
            }
        }

        void OnGUI()
        {
            if (showPTTStatus)
            {
                DrawUI();
            }
        }

        private void OnDestroy()
        {
            if (isRecording)
            {
                Microphone.End(microphoneDevice);
            }

            if (micClip != null)
            {
                Destroy(micClip);
            }
        }

        #endregion

        #region Initialization

        private void InitializeProcessing()
        {
            try
            {
                // VAD初期化
                vad = new Audio.VoiceActivityDetector(
                    sampleRate,
                    vadEnergyThreshold,
                    vadSilenceDuration,
                    vadMaxRecordingDuration
                );

                vad.OnSpeechStarted += OnVADSpeechStarted;
                vad.OnSpeechEnded += OnVADSpeechEnded;

                // リアルタイムプロセッサー初期化
                if (applyRealtimeProcessing)
                {
                    realtimeProcessor = new Audio.RealtimeAudioProcessor(sampleRate);
                    Log("Realtime processor initialized");
                }

                Log("VAD & Processing initialized");
            }
            catch (Exception e)
            {
                GameLogger.LogError(LogCategory.Voice,$"[AudioRecorder] Processing init failed: {e.Message}");
            }
        }

        #endregion

        #region Push-to-Talk Mode

        private void HandlePushToTalk()
        {
            bool keyPressed = CheckPTTInput();

            if (keyPressed && !isPTTPressed)
            {
                isPTTPressed = true;
                StartRecording();
            }
            else if (!keyPressed && isPTTPressed)
            {
                isPTTPressed = false;
                float[] audioData = StopRecording();

                if (audioData != null && audioData.Length > 0)
                {
                    if (applyProcessing)
                    {
                        audioData = Audio.AdvancedAudioFilters.ProcessForWakeWord(audioData, sampleRate);
                    }

                    // 保存
                    if (autoSaveRecordings)
                    {
                        SaveRecordingAutomatically(audioData);
                    }

                    OnAudioDataReady?.Invoke(audioData);
                }
            }
        }

        private bool CheckPTTInput()
        {
            if (useMouseButton)
            {
                return Input.GetMouseButton(mouseButtonIndex);
            }
            else
            {
                return Input.GetKey(pushToTalkKey);
            }
        }

        #endregion

        #region Continuous VAD Mode

        private void HandleContinuousVAD()
        {
            if (!isRecording)
            {
                StartContinuousRecording();
            }

            // マイクからチャンクを取得
            ProcessMicrophoneChunk();
        }

        private void StartContinuousRecording()
        {
            if (string.IsNullOrEmpty(microphoneDevice))
            {
                if (Microphone.devices.Length > 0)
                {
                    microphoneDevice = Microphone.devices[0];
                }
                else
                {
                    GameLogger.LogError(LogCategory.Voice,"[AudioRecorder] No microphone found");
                    return;
                }
            }

            micClip = Microphone.Start(microphoneDevice, true, 10, sampleRate);
            if (micClip == null)
            {
                GameLogger.LogError(LogCategory.Voice,"[AudioRecorder] Failed to start microphone");
                return;
            }

            lastMicPosition = 0;
            isRecording = true;

            Log("Continuous recording started (VAD mode)");
        }

        private void ProcessMicrophoneChunk()
        {
            if (micClip == null || !isRecording)
                return;

            int currentPosition = Microphone.GetPosition(microphoneDevice);

            if (currentPosition < lastMicPosition)
            {
                // バッファがループした
                lastMicPosition = 0;
            }

            int samplesToRead = currentPosition - lastMicPosition;

            if (samplesToRead >= chunkSize)
            {
                // チャンクを読み取り
                float[] chunk = new float[chunkSize];
                micClip.GetData(chunk, lastMicPosition);

                // リアルタイム処理適用
                if (applyRealtimeProcessing && realtimeProcessor != null)
                {
                    chunk = realtimeProcessor.ProcessChunk(chunk);
                }

                // VAD処理
                vad.ProcessChunk(chunk);

                lastMicPosition += chunkSize;
            }
        }

        private void OnVADSpeechStarted()
        {
            Log("VAD: Speech detected");
        }

        private void OnVADSpeechEnded(float[] audioData)
        {
            Log($"VAD: Speech ended ({audioData.Length} samples)");

            // 最終処理
            if (applyProcessing)
            {
                audioData = Audio.AdvancedAudioFilters.ProcessForWakeWord(audioData, sampleRate);
            }

            // 保存
            if (autoSaveRecordings)
            {
                SaveRecordingAutomatically(audioData);
            }

            OnAudioDataReady?.Invoke(audioData);
        }

        #endregion

        #region Recording Control (PTT用)

        public bool StartRecording()
        {
            if (isRecording)
            {
                GameLogger.LogWarning(LogCategory.Voice,"[AudioRecorder] Already recording");
                return false;
            }

            try
            {
                if (string.IsNullOrEmpty(microphoneDevice))
                {
                    if (Microphone.devices.Length > 0)
                    {
                        microphoneDevice = Microphone.devices[0];
                    }
                    else
                    {
                        GameLogger.LogError(LogCategory.Voice,"[AudioRecorder] No microphone found");
                        return false;
                    }
                }

                micClip = Microphone.Start(microphoneDevice, false, maxRecordSeconds, sampleRate);

                if (micClip == null)
                {
                    GameLogger.LogError(LogCategory.Voice,"[AudioRecorder] Failed to create microphone clip");
                    return false;
                }

                lastMicPosition = 0;
                isRecording = true;

                Log($"Recording started (device: {microphoneDevice ?? "default"}, rate: {sampleRate}Hz)");
                return true;
            }
            catch (Exception e)
            {
                GameLogger.LogError(LogCategory.Voice,$"[AudioRecorder] Failed to start recording: {e.Message}");
                return false;
            }
        }

        public float[] StopRecording()
        {
            if (!isRecording)
            {
                GameLogger.LogWarning(LogCategory.Voice,"[AudioRecorder] Not recording");
                return null;
            }

            try
            {
                int currentPosition = Microphone.GetPosition(microphoneDevice);
                Microphone.End(microphoneDevice);
                isRecording = false;

                if (micClip == null)
                {
                    GameLogger.LogError(LogCategory.Voice,"[AudioRecorder] AudioClip is null");
                    return null;
                }

                int recordedSamples = currentPosition;
                if (recordedSamples <= 0)
                {
                    recordedSamples = micClip.samples;
                }

                float[] audioData = new float[recordedSamples * micClip.channels];
                micClip.GetData(audioData, 0);

                Log($"Recording stopped. Captured {audioData.Length} samples ({audioData.Length / (float)sampleRate:F2}s)");

                // リアルタイム処理適用（PTTモード用）
                if (applyRealtimeProcessing && realtimeProcessor != null)
                {
                    audioData = realtimeProcessor.ProcessChunk(audioData);
                    Log("Realtime processing applied");
                }

                return audioData;
            }
            catch (Exception e)
            {
                GameLogger.LogError(LogCategory.Voice,$"[AudioRecorder] Failed to stop recording: {e.Message}");
                isRecording = false;
                return null;
            }
        }

        #endregion

        #region Public API

        public int SampleRate => sampleRate;
        public RecordingMode CurrentMode => recordingMode;
        public bool IsVADActive => vad != null && vad.IsDetecting;
        public bool IsMicrophoneActive => isRecording;

        /// <summary>
        /// 録音モードを動的に変更
        /// </summary>
        public void SetMode(RecordingMode mode)
        {
            if (isRecording)
            {
                GameLogger.LogWarning(LogCategory.Voice,
                    "[AudioRecorder] Cannot change mode while recording");
                return;
            }

            recordingMode = mode;
            Log($"Recording mode changed to: {mode}");
        }

        /// <summary>
        /// ContinuousVADモードでの常時リスニングを開始
        /// </summary>
        public void StartContinuousListening()
        {
            if (recordingMode != RecordingMode.ContinuousVAD)
            {
                GameLogger.LogWarning(LogCategory.Voice,
                    "[AudioRecorder] StartContinuousListening requires ContinuousVAD mode");
                return;
            }

            if (!isRecording)
            {
                StartContinuousRecording();
                Log("Continuous listening started");
            }
        }

        /// <summary>
        /// ContinuousVADモードでの常時リスニングを停止
        /// </summary>
        public void StopContinuousListening()
        {
            if (isRecording && recordingMode == RecordingMode.ContinuousVAD)
            {
                Microphone.End(microphoneDevice);
                isRecording = false;
                vad?.Reset();
                Log("Continuous listening stopped");
            }
        }

        #endregion

        #region Utilities

        private void Log(string message)
        {
            if (showDebugLog)
            {
                GameLogger.Log(LogCategory.Voice, $"[AudioRecorder] {message}");
            }
        }

        #endregion

        #region Legacy Compatibility

        // 既存のフィルタシステムとの互換性のため残す
        private float[] ApplyFilters(float[] audioData)
        {
            // 何もしない（新しいパイプラインに置き換え済み）
            return audioData;
        }

        #endregion
    }
}