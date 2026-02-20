using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VoiceCommandSystem.Core;
using TapHouse.Logging;

namespace VoiceCommandSystem.Debug
{
    /// <summary>
    /// 顔認識音声入力システムのデバッグビュー
    /// 全ての音声コマンド関連デバッグ情報を統合表示
    ///
    /// 使用方法:
    /// 1. DebugCanvas内にFaceVoiceDebugPanelを作成
    /// 2. このコンポーネントをDebugCanvasに追加
    /// 3. UI参照を設定
    /// 4. 他のOnGUI表示をオフにする（showDebugGUI = false）
    /// </summary>
    public class VoiceCommandDebugView : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("FaceVoiceDebugPanel オブジェクト")]
        [SerializeField] private GameObject debugPanel;
        [SerializeField] private RawImage cameraPreview;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Image faceIndicator;
        [SerializeField] private Image micIndicator;

        [Header("Indicator Colors")]
        [SerializeField] private Color activeColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color inactiveColor = new Color(0.5f, 0.5f, 0.5f);
        [SerializeField] private Color recordingColor = new Color(0.9f, 0.2f, 0.2f);
        [SerializeField] private Color processingColor = new Color(0.9f, 0.9f, 0.2f);

        [Header("References")]
        [SerializeField] private FacePresenceDetector faceDetector;
        [SerializeField] private FaceTriggeredVoiceInput faceTriggeredInput;
        [SerializeField] private VoiceCommandManager voiceCommandManager;
        [SerializeField] private AudioRecorder audioRecorder;

        [Header("Camera Preview Settings")]
        [Tooltip("カメラプレビューを表示するかどうか")]
        [SerializeField] private bool showCameraPreview = true;
        [Tooltip("プレビュー最大幅")]
        [SerializeField] private float maxPreviewWidth = 180f;
        [Tooltip("プレビュー最大高さ")]
        [SerializeField] private float maxPreviewHeight = 135f;

        [Header("Toggle")]
        [SerializeField] private KeyCode toggleKey = KeyCode.F1;
        [SerializeField] private bool startVisible = false;

        [Header("Disable Other Debug Displays")]
        [Tooltip("有効にすると他のOnGUI表示を自動的にオフにする")]
        [SerializeField] private bool disableOtherDebugGUI = true;

        private bool isVisible = false;
        private bool isFacePresent = false;
        private bool isMicActive = false;
        private bool isProcessing = false;
        private WebCamTexture webCamTexture;

        // 認識結果
        private string lastRecognizedText = "";
        private string lastCommandResult = "";
        private float resultDisplayEndTime = 0f;

        private void Start()
        {
            FindReferences();
            SubscribeToEvents();

            isVisible = startVisible;
            if (debugPanel != null)
            {
                debugPanel.SetActive(isVisible);
            }

            // 他のOnGUI表示をオフにする
            if (disableOtherDebugGUI)
            {
                DisableOtherDebugDisplays();
            }

            SetupCameraPreview();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            // トグルキーでデバッグパネルの表示/非表示を切り替え
            if (Input.GetKeyDown(toggleKey))
            {
                ToggleDebugPanel();
            }

            if (isVisible)
            {
                UpdateStatusText();
                UpdateIndicators();
                UpdateCameraPreview();
            }
        }

        #region Setup

        private void FindReferences()
        {
            if (faceDetector == null)
                faceDetector = FindObjectOfType<FacePresenceDetector>();

            if (faceTriggeredInput == null)
                faceTriggeredInput = FindObjectOfType<FaceTriggeredVoiceInput>();

            if (voiceCommandManager == null)
                voiceCommandManager = VoiceCommandManager.Instance;

            if (audioRecorder == null)
                audioRecorder = FindObjectOfType<AudioRecorder>();
        }

        private void SubscribeToEvents()
        {
            if (faceDetector != null)
            {
                faceDetector.onFacePresenceChanged.AddListener(OnFacePresenceChanged);
            }

            if (faceTriggeredInput != null)
            {
                faceTriggeredInput.OnVoiceInputStarted += OnVoiceInputStarted;
                faceTriggeredInput.OnVoiceInputEnded += OnVoiceInputEnded;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (faceDetector != null)
            {
                faceDetector.onFacePresenceChanged.RemoveListener(OnFacePresenceChanged);
            }

            if (faceTriggeredInput != null)
            {
                faceTriggeredInput.OnVoiceInputStarted -= OnVoiceInputStarted;
                faceTriggeredInput.OnVoiceInputEnded -= OnVoiceInputEnded;
            }
        }

        private void DisableOtherDebugDisplays()
        {
            // VoiceInputDetectorのOnGUIをオフ
            var inputDetector = FindObjectOfType<VoiceInputDetector>();
            if (inputDetector != null)
            {
                var field = typeof(VoiceInputDetector).GetField("showDebugGUI",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(inputDetector, false);
                }
            }

            // AudioRecorderのOnGUIをオフ
            if (audioRecorder != null)
            {
                var field = typeof(AudioRecorder).GetField("showPTTStatus",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(audioRecorder, false);
                }
            }

            // VoiceCommandManagerのOnGUIをオフ
            if (voiceCommandManager != null)
            {
                var field = typeof(VoiceCommandManager).GetField("showDebugLog",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(voiceCommandManager, false);
                }
            }

            GameLogger.Log(LogCategory.Voice, "[VoiceCommandDebugView] Disabled other OnGUI displays");
        }

        private void SetupCameraPreview()
        {
            if (!showCameraPreview || cameraPreview == null)
                return;

            // FacePresenceDetectorからWebCamTextureを取得
            if (faceDetector != null && faceDetector.webCamHelper != null)
            {
                webCamTexture = faceDetector.webCamHelper.GetWebCamTexture();
                if (webCamTexture != null)
                {
                    cameraPreview.texture = webCamTexture;
                    AdjustCameraPreviewAspectRatio();
                    GameLogger.Log(LogCategory.Voice, "[VoiceCommandDebugView] Camera preview setup complete");
                }
            }
        }

        /// <summary>
        /// カメラプレビューのアスペクト比を維持してサイズ調整
        /// </summary>
        private void AdjustCameraPreviewAspectRatio()
        {
            if (cameraPreview == null || webCamTexture == null)
                return;

            // WebCamTextureのサイズを取得（初期化されていない場合は待機）
            if (webCamTexture.width <= 16 || webCamTexture.height <= 16)
                return;

            float sourceAspect = (float)webCamTexture.width / webCamTexture.height;

            // 最大サイズ内に収まるようにアスペクト比を維持してサイズ計算
            float targetWidth = maxPreviewWidth;
            float targetHeight = targetWidth / sourceAspect;

            if (targetHeight > maxPreviewHeight)
            {
                targetHeight = maxPreviewHeight;
                targetWidth = targetHeight * sourceAspect;
            }

            RectTransform rt = cameraPreview.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.sizeDelta = new Vector2(targetWidth, targetHeight);
            }

            // フロントカメラの場合は水平反転
            if (faceDetector != null && faceDetector.requestedIsFrontFacing)
            {
                cameraPreview.rectTransform.localScale = new Vector3(-1, 1, 1);
            }

            GameLogger.Log(LogCategory.Voice,
                $"[VoiceCommandDebugView] Preview adjusted: {webCamTexture.width}x{webCamTexture.height} -> {targetWidth:F0}x{targetHeight:F0}");
        }

        #endregion

        #region Event Handlers

        private void OnFacePresenceChanged(bool isPresent)
        {
            isFacePresent = isPresent;
        }

        private void OnVoiceInputStarted()
        {
            isMicActive = true;
        }

        private void OnVoiceInputEnded(float[] audioData)
        {
            // 録音終了 - 処理開始
            isProcessing = true;
        }

        /// <summary>
        /// 認識結果を設定（外部から呼び出し）
        /// </summary>
        public void SetRecognitionResult(string recognizedText, string commandResult, bool success)
        {
            lastRecognizedText = recognizedText ?? "";
            lastCommandResult = commandResult ?? "";
            resultDisplayEndTime = Time.time + 5f; // 5秒間表示
            isProcessing = false;
        }

        #endregion

        #region UI Updates

        private void UpdateStatusText()
        {
            if (statusText == null) return;

            string triggerMode = voiceCommandManager != null
                ? voiceCommandManager.CurrentTriggerMode.ToString()
                : "N/A";

            string faceStatus = GetFaceStatus();
            string micStatus = GetMicStatus();
            string vadStatus = GetVADStatus();
            string recognitionStatus = GetRecognitionStatus();

            statusText.text = $"<b>FaceVoice Debug</b>\n" +
                             $"Mode: {triggerMode}\n" +
                             $"顔: {faceStatus}\n" +
                             $"Mic: {micStatus}\n" +
                             $"VAD: {vadStatus}\n" +
                             $"{recognitionStatus}";
        }

        private string GetFaceStatus()
        {
            if (isFacePresent)
            {
                return "<color=#00FF00>検出中</color>";
            }
            return "<color=#888888>未検出</color>";
        }

        private string GetMicStatus()
        {
            if (faceTriggeredInput != null && faceTriggeredInput.IsMicrophoneActive)
            {
                return "<color=#00FF00>ON</color>";
            }
            if (audioRecorder != null && audioRecorder.IsMicrophoneActive)
            {
                return "<color=#00FF00>ON</color>";
            }
            return "<color=#888888>OFF</color>";
        }

        private string GetVADStatus()
        {
            if (audioRecorder != null && audioRecorder.IsVADActive)
            {
                return "<color=#FF4444>録音中</color>";
            }
            if (audioRecorder != null && audioRecorder.IsMicrophoneActive)
            {
                return "<color=#00FF00>待機中</color>";
            }
            return "<color=#888888>停止</color>";
        }

        private string GetRecognitionStatus()
        {
            if (isProcessing)
            {
                return "<color=#FFFF00>認識中...</color>";
            }

            if (Time.time < resultDisplayEndTime && !string.IsNullOrEmpty(lastRecognizedText))
            {
                string result = $"「{lastRecognizedText}」";
                if (!string.IsNullOrEmpty(lastCommandResult))
                {
                    result += $"\n{lastCommandResult}";
                }
                return result;
            }

            return "<color=#888888>待機中</color>";
        }

        private void UpdateIndicators()
        {
            // 顔インジケータ
            if (faceIndicator != null)
            {
                faceIndicator.color = isFacePresent ? activeColor : inactiveColor;
            }

            // マイクインジケータ
            if (micIndicator != null)
            {
                bool micActive = (faceTriggeredInput != null && faceTriggeredInput.IsMicrophoneActive) ||
                                 (audioRecorder != null && audioRecorder.IsMicrophoneActive);
                bool vadActive = audioRecorder != null && audioRecorder.IsVADActive;

                if (isProcessing)
                {
                    micIndicator.color = processingColor;
                }
                else if (vadActive)
                {
                    micIndicator.color = recordingColor;
                }
                else if (micActive)
                {
                    micIndicator.color = activeColor;
                }
                else
                {
                    micIndicator.color = inactiveColor;
                }
            }
        }

        private void UpdateCameraPreview()
        {
            // WebCamTextureがまだ設定されていない場合、再試行
            if (showCameraPreview && cameraPreview != null)
            {
                if (webCamTexture == null)
                {
                    SetupCameraPreview();
                }
                // アスペクト比の再調整（カメラ初期化後）
                else if (webCamTexture.width > 16 && cameraPreview.rectTransform.sizeDelta.x < 10)
                {
                    AdjustCameraPreviewAspectRatio();
                }
            }

            // FaceTriggeredInputの状態を更新
            if (faceTriggeredInput != null)
            {
                isMicActive = faceTriggeredInput.IsMicrophoneActive;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// デバッグパネルの表示/非表示を切り替え
        /// </summary>
        public void ToggleDebugPanel()
        {
            isVisible = !isVisible;
            if (debugPanel != null)
            {
                debugPanel.SetActive(isVisible);
            }

            GameLogger.Log(LogCategory.Voice,
                $"[VoiceCommandDebugView] Debug panel {(isVisible ? "shown" : "hidden")}");
        }

        /// <summary>
        /// デバッグパネルを表示
        /// </summary>
        public void ShowDebugPanel()
        {
            isVisible = true;
            if (debugPanel != null)
            {
                debugPanel.SetActive(true);
            }
        }

        /// <summary>
        /// デバッグパネルを非表示
        /// </summary>
        public void HideDebugPanel()
        {
            isVisible = false;
            if (debugPanel != null)
            {
                debugPanel.SetActive(false);
            }
        }

        /// <summary>
        /// カメラプレビューの表示/非表示を切り替え
        /// </summary>
        public void SetCameraPreviewEnabled(bool enabled)
        {
            showCameraPreview = enabled;
            if (cameraPreview != null)
            {
                cameraPreview.gameObject.SetActive(enabled);
            }
        }

        #endregion
    }
}
