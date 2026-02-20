using UnityEngine;
using System.Threading.Tasks;
using VoiceCommandSystem.Commands;
using VoiceCommandSystem.Commands.DogCommands;
using VoiceCommandSystem.Recognizers;
using VoiceCommandSystem.Debug;
using TapHouse.Logging;

namespace VoiceCommandSystem.Core
{
    /// <summary>
    /// 音声コマンドのトリガーモード
    /// </summary>
    public enum TriggerMode
    {
        /// <summary>VoiceInputDetectorのみ（デバッグ用：3本指タッチ/Rキー）</summary>
        DebugOnly,
        /// <summary>顔認識トリガー（本番用）</summary>
        FaceTriggered,
        /// <summary>両方有効</summary>
        Both
    }

    /// <summary>
    /// 音声コマンドシステムのメインマネージャー（Wake Word対応版）
    /// </summary>
    [RequireComponent(typeof(VoiceInputDetector))]
    [RequireComponent(typeof(AudioRecorder))]
    public class VoiceCommandManager : MonoBehaviour
    {
        #region Singleton
        private static VoiceCommandManager instance;
        public static VoiceCommandManager Instance => instance;

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        #endregion

        [Header("機能設定")]
        [SerializeField] private bool enableVoiceCommand = true;

        [Header("API設定")]
        [Tooltip("APIキー設定ファイル（Resources/VoiceCommandConfig.asset）")]
        [SerializeField] private VoiceCommandConfig config;

        [Header("OpenAI API 設定（非推奨：直接指定）")]
        [Tooltip("⚠️ 非推奨：Configファイルを使用してください")]
        [SerializeField] private bool useOpenAI = true;
        [Tooltip("⚠️ GitHubにプッシュしないでください！環境変数またはConfigファイルを使用")]
        [SerializeField] private string openAIApiKey = "";

        [Header("ローカルWhisper設定")]
        [SerializeField] private bool useLocalWhisper = true;

        [Header("参照")]
        [SerializeField] private DogController dogController;

        [Header("トリガーモード")]
        [Tooltip("音声コマンドのトリガー方式を選択")]
        [SerializeField] private TriggerMode triggerMode = TriggerMode.FaceTriggered;

        [Header("顔認識トリガー")]
        [SerializeField] private FaceTriggeredVoiceInput faceTriggeredInput;

        [Header("デバッグ")]
        [SerializeField] private bool showDebugLog = true;
        [Tooltip("デバッグビュー（自動検出）")]
        [SerializeField] private VoiceCommandDebugView debugView;

        // コンポーネント（事前アタッチ）
        private VoiceInputDetector inputDetector;
        private AudioRecorder audioRecorder;
        private RecognizerSelector recognizerSelector;
        private VoiceCommandRegistry commandRegistry;

        // 状態
        private bool isInitialized = false;
        private bool isProcessing = false;
        private string resultMessage = "";
        private float resultDisplayEndTime = 0f;

        async void Start()
        {
            if (!enableVoiceCommand)
            {
                GameLogger.Log(LogCategory.Voice,"[VoiceCommandManager] Voice command system is disabled");
                enabled = false;
                return;
            }

            await InitializeSystem();
        }

        private async Task InitializeSystem()
        {
            GameLogger.Log(LogCategory.Voice,"========================================");
            GameLogger.Log(LogCategory.Voice,"[VoiceCommandManager] Initializing Voice Command System");
            GameLogger.Log(LogCategory.Voice,"========================================");

            try
            {
                // DogControllerの取得
                if (dogController == null)
                {
                    dogController = FindObjectOfType<DogController>();
                    if (dogController == null)
                    {
                        GameLogger.LogWarning(LogCategory.Voice,"[VoiceCommandManager] DogController not found");
                    }
                }

                // 1. VoiceInputDetector の取得（RequireComponentで自動追加）
                InitializeInputDetector();

                // 2. AudioRecorder の取得（RequireComponentで自動追加）
                InitializeAudioRecorder();

                // 3. 認識エンジンの初期化
                await InitializeRecognizers();

                // 4. コマンドレジストリの初期化
                InitializeCommandRegistry();

                // 5. FaceTriggeredVoiceInputの初期化
                InitializeFaceTriggeredInput();

                // 6. トリガーモードの適用
                ApplyTriggerMode();

                isInitialized = true;
                GameLogger.Log(LogCategory.Voice,"[VoiceCommandManager] ✅ Initialization completed");
                GameLogger.Log(LogCategory.Voice,$"[VoiceCommandManager] TriggerMode: {triggerMode}");
                GameLogger.Log(LogCategory.Voice,"========================================");
            }
            catch (System.Exception e)
            {
                GameLogger.LogError(LogCategory.Voice,$"[VoiceCommandManager] ❌ Initialization failed: {e.Message}");
                enabled = false;
            }
        }

        private void InitializeInputDetector()
        {
            inputDetector = GetComponent<VoiceInputDetector>();
            if (inputDetector == null)
            {
                GameLogger.LogError(LogCategory.Voice,"[VoiceCommandManager] VoiceInputDetector not found!");
                return;
            }

            inputDetector.OnRecordingStarted += HandleRecordingStarted;
            inputDetector.OnRecordingStopped += HandleRecordingStopped;

            GameLogger.Log(LogCategory.Voice,"[VoiceCommandManager] VoiceInputDetector initialized");
        }

        private void InitializeAudioRecorder()
        {
            audioRecorder = GetComponent<AudioRecorder>();
            if (audioRecorder == null)
            {
                GameLogger.LogError(LogCategory.Voice,"[VoiceCommandManager] AudioRecorder not found!");
                return;
            }

            GameLogger.Log(LogCategory.Voice,"[VoiceCommandManager] AudioRecorder initialized");
        }

        private async Task InitializeRecognizers()
        {
            // 1. Configファイルから読み込み（最優先）
            if (config != null && config.HasApiKey())
            {
                openAIApiKey = config.OpenAIApiKey;
                useOpenAI = config.UseOpenAI;
                useLocalWhisper = config.UseLocalWhisper;
                GameLogger.Log(LogCategory.Voice,"[VoiceCommandManager] API key loaded from Config file");
            }
            // 2. 環境変数から読み込み（次の優先度）
            else if (string.IsNullOrEmpty(openAIApiKey))
            {
                openAIApiKey = System.Environment.GetEnvironmentVariable("OPENAI_API_KEY");
                if (!string.IsNullOrEmpty(openAIApiKey))
                {
                    GameLogger.Log(LogCategory.Voice,"[VoiceCommandManager] API key loaded from environment variable");
                }
            }

            if (useOpenAI && string.IsNullOrEmpty(openAIApiKey))
            {
                GameLogger.LogWarning(LogCategory.Voice,"[VoiceCommandManager] ⚠️ OpenAI API key not set. Please create a VoiceCommandConfig asset or set OPENAI_API_KEY environment variable.");
                useOpenAI = false;
            }

            recognizerSelector = new RecognizerSelector(
                openAIApiKey: useOpenAI ? openAIApiKey : null,
                preferOnline: useOpenAI,
                fallbackToLocal: useLocalWhisper
            );

            bool success = await recognizerSelector.InitializeAsync();

            if (!success)
            {
                GameLogger.LogError(LogCategory.Voice,"[VoiceCommandManager] Failed to initialize recognizer");
                throw new System.Exception("No recognizer available");
            }

            GameLogger.Log(LogCategory.Voice,"[VoiceCommandManager] Recognizers initialized");
        }

        private void InitializeCommandRegistry()
        {
            commandRegistry = new VoiceCommandRegistry();
            RegisterDogCommands();

            if (showDebugLog)
            {
                commandRegistry.LogAllCommands();
            }

            GameLogger.Log(LogCategory.Voice,"[VoiceCommandManager] CommandRegistry initialized");
        }

        private void InitializeFaceTriggeredInput()
        {
            // FaceTriggeredVoiceInputが設定されていない場合は検索
            if (faceTriggeredInput == null)
            {
                faceTriggeredInput = FindObjectOfType<FaceTriggeredVoiceInput>();
            }

            if (faceTriggeredInput != null)
            {
                faceTriggeredInput.OnVoiceInputEnded += HandleFaceTriggeredRecordingEnded;
                GameLogger.Log(LogCategory.Voice,"[VoiceCommandManager] FaceTriggeredVoiceInput initialized");
            }
            else if (triggerMode == TriggerMode.FaceTriggered || triggerMode == TriggerMode.Both)
            {
                GameLogger.LogWarning(LogCategory.Voice,
                    "[VoiceCommandManager] FaceTriggeredVoiceInput not found but TriggerMode requires it!");
            }
        }

        private void ApplyTriggerMode()
        {
            // VoiceInputDetector（デバッグ用）の制御
            bool enableDebug = triggerMode == TriggerMode.DebugOnly || triggerMode == TriggerMode.Both;
            if (inputDetector != null)
            {
                inputDetector.SetDebugTriggerEnabled(enableDebug);
            }

            // FaceTriggeredVoiceInputの制御
            bool enableFace = triggerMode == TriggerMode.FaceTriggered || triggerMode == TriggerMode.Both;
            if (faceTriggeredInput != null)
            {
                faceTriggeredInput.SetEnabled(enableFace);
            }

            GameLogger.Log(LogCategory.Voice,
                $"[VoiceCommandManager] TriggerMode applied: {triggerMode} " +
                $"(Debug: {enableDebug}, Face: {enableFace})");
        }

        private async void HandleFaceTriggeredRecordingEnded(float[] audioData)
        {
            if (!isInitialized)
            {
                GameLogger.LogWarning(LogCategory.Voice,"[VoiceCommandManager] System not initialized");
                return;
            }

            GameLogger.Log(LogCategory.Voice,
                $"[VoiceCommandManager] 🎙️ Face-triggered recording received: {audioData.Length} samples");

            await ProcessAudioAsync(audioData);
        }

        private void RegisterDogCommands()
        {
            if (dogController == null)
            {
                GameLogger.LogWarning(LogCategory.Voice,"[VoiceCommandManager] DogController is null");
                return;
            }

            // BasicCommands
            commandRegistry.RegisterCommands(
                new SitCommand(dogController),
                new PawCommand(dogController),
                new OkawariCommand(dogController),
                new DownCommand(dogController),
                new StandCommand(dogController),
                new WaitCommand(dogController),
                new OkayCommand(dogController)
            );

            // MovementCommands
            commandRegistry.RegisterCommands(
                new ComeCommand(dogController),
                new TurnCommand(dogController),
                new JumpCommand(dogController)
            );

            // TrickCommands
            commandRegistry.RegisterCommands(
                new BangCommand(dogController),
                new ChinChinCommand(dogController),
                new HighFiveCommand(dogController)
            );

            // CommunicationCommands
            commandRegistry.RegisterCommands(
                new BarkCommand(dogController),
                new QuietCommand(dogController)
            );

            // PraiseCommands
            commandRegistry.RegisterCommands(
                new GoodBoyCommand(dogController),
                new GreatCommand(dogController),
                new WellDoneCommand(dogController),
                new AmazingCommand(dogController),
                new SmartCommand(dogController),
                new CuteCommand(dogController),
                new LoveCommand(dogController),
                new LikeCommand(dogController),
                new LoveLoveCommand(dogController),
                new CheerCommand(dogController),
                new FightCommand(dogController),
                new YouCanDoItCommand(dogController),
                new RewardCommand(dogController),
                new TreatCommand(dogController),
                new YummyCommand(dogController)
            );

            GameLogger.Log(LogCategory.Voice,"[VoiceCommandManager] All dog commands registered (30 commands)");
        }

        private void HandleRecordingStarted()
        {
            if (!isInitialized || isProcessing)
            {
                GameLogger.LogWarning(LogCategory.Voice,"[VoiceCommandManager] Cannot start recording");
                return;
            }

            GameLogger.Log(LogCategory.Voice,"[VoiceCommandManager] 🎙️ Recording started (Debug trigger)");

            bool success = audioRecorder.StartRecording();
            if (!success)
            {
                GameLogger.LogError(LogCategory.Voice,"[VoiceCommandManager] Failed to start recording");
            }
        }

        private async void HandleRecordingStopped()
        {
            if (!isInitialized)
            {
                GameLogger.LogWarning(LogCategory.Voice,"[VoiceCommandManager] System not initialized");
                return;
            }

            GameLogger.Log(LogCategory.Voice,"[VoiceCommandManager] 🛑 Recording stopped - processing...");

            float[] audioData = audioRecorder.StopRecording();

            if (audioData == null || audioData.Length == 0)
            {
                GameLogger.LogWarning(LogCategory.Voice,"[VoiceCommandManager] No audio data captured");
                return;
            }

            await ProcessAudioAsync(audioData);
        }

        private async Task ProcessAudioAsync(float[] audioData)
        {
            if (isProcessing) return;

            isProcessing = true;
            resultMessage = "";

            try
            {
                string recognizedText = await recognizerSelector.RecognizeAsync(
                    audioData,
                    audioRecorder.SampleRate
                );

                if (string.IsNullOrEmpty(recognizedText))
                {
                    resultMessage = "❌ 認識不可\n再度試して";
                    resultDisplayEndTime = Time.time + 2f;
                    NotifyDebugView("", "認識不可", false);
                    return;
                }

                // コマンド実行
                int executedCount = commandRegistry.ExecuteMatchingCommands(
                    recognizedText,
                    recognizerSelector.RecognizerName,
                    confidence: 1.0f
                );

                if (executedCount == 0)
                {
                    resultMessage = $"「{recognizedText}」\n\nコマンドが見つかりません";
                    resultDisplayEndTime = Time.time + 3f;
                    NotifyDebugView(recognizedText, "コマンドなし", false);
                }
                else
                {
                    resultMessage = $"✅ 実行完了\n({recognizedText})";
                    resultDisplayEndTime = Time.time + 2f;
                    NotifyDebugView(recognizedText, $"実行完了 ({executedCount}件)", true);
                }
            }
            catch (System.Exception e)
            {
                resultMessage = "❌ エラーが発生しました\nもう一度お試しください";
                resultDisplayEndTime = Time.time + 2f;
                NotifyDebugView("", $"エラー: {e.Message}", false);
            }
            finally
            {
                isProcessing = false;
            }
        }

        private void NotifyDebugView(string recognizedText, string result, bool success)
        {
            // デバッグビューが未設定の場合は検索
            if (debugView == null)
            {
                debugView = FindObjectOfType<VoiceCommandDebugView>();
            }

            if (debugView != null)
            {
                debugView.SetRecognitionResult(recognizedText, result, success);
            }
        }

        public void SetApiKey(string apiKey)
        {
            openAIApiKey = apiKey;
            GameLogger.Log(LogCategory.Voice,"[VoiceCommandManager] API key updated");
        }

        public void SetEnabled(bool enabled)
        {
            enableVoiceCommand = enabled;
            this.enabled = enabled;

            if (inputDetector != null)
            {
                inputDetector.enabled = enabled;
            }

            GameLogger.Log(LogCategory.Voice,$"[VoiceCommandManager] {(enabled ? "ENABLED" : "DISABLED")}");
        }

        public void SetTriggerMode(TriggerMode mode)
        {
            triggerMode = mode;
            ApplyTriggerMode();
            GameLogger.Log(LogCategory.Voice,$"[VoiceCommandManager] TriggerMode changed to: {mode}");
        }

        public TriggerMode CurrentTriggerMode => triggerMode;

        public void RegisterCommand(IVoiceCommand command)
        {
            commandRegistry?.RegisterCommand(command);
        }

        public bool UnregisterCommand(IVoiceCommand command)
        {
            return commandRegistry?.UnregisterCommand(command) ?? false;
        }

        public bool IsInitialized => isInitialized;
        public bool IsProcessing => isProcessing;
        public AudioRecorder AudioRecorder => audioRecorder;

        void OnDestroy()
        {
            if (inputDetector != null)
            {
                inputDetector.OnRecordingStarted -= HandleRecordingStarted;
                inputDetector.OnRecordingStopped -= HandleRecordingStopped;
            }

            if (faceTriggeredInput != null)
            {
                faceTriggeredInput.OnVoiceInputEnded -= HandleFaceTriggeredRecordingEnded;
            }

            recognizerSelector?.Dispose();

            GameLogger.Log(LogCategory.Voice,"[VoiceCommandManager] Cleaned up");
        }

        void OnGUI()
        {
            if (!showDebugLog || !isInitialized) return;

            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 20;
            style.alignment = TextAnchor.MiddleCenter;

            // 背景
            Texture2D bgTexture = new Texture2D(1, 1);
            bgTexture.SetPixel(0, 0, new Color(0, 0, 0, 0.8f));
            bgTexture.Apply();
            style.normal.background = bgTexture;
            style.padding = new RectOffset(5, 5, 5, 5);

            string displayText = "";

            // 処理中の表示
            if (isProcessing)
            {
                style.normal.textColor = Color.yellow;
                displayText = "⏳ 認識中...";
            }
            // 結果の表示（2秒間）
            else if (Time.time < resultDisplayEndTime)
            {
                // メッセージの内容で色を変える
                if (resultMessage.Contains("✅"))
                {
                    style.normal.textColor = Color.green;
                }
                else if (resultMessage.Contains("❌"))
                {
                    style.normal.textColor = Color.red;
                }
                else
                {
                    style.normal.textColor = Color.yellow;
                }
                displayText = resultMessage;
            }
            else
            {
                return;
            }

            // 中央に表示
            GUI.Label(new Rect(
                Screen.width / 2 - 250,
                Screen.height / 2 - 50,
                500, 100
            ), displayText, style);
        }
    }
}