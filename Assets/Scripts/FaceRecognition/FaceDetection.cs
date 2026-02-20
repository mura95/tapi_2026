using UnityEngine;
using System;
using TapHouse.Logging;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// 超軽量：カメラ映像は描画せず、1秒ごとに「人(顔)が居る/居ない」を判定して通知するだけ。
/// - WebCamTextureToMatHelper のイベントを購読
/// - 例外は段階ごとに捕捉＆ログ
/// - 動体検出で無駄な顔探索を回避
/// </summary>
[RequireComponent(typeof(WebCamTextureToMatHelper))]
public class FacePresenceDetector : MonoBehaviour
{
    [Header("Camera")]
    public WebCamTextureToMatHelper webCamHelper;
    [Tooltip("優先：フロントカメラ")] public bool requestedIsFrontFacing = true;
    [Tooltip("要求解像度（実際は端末依存）")] public int requestedWidth = 640;
    public int requestedHeight = 480;
    [Tooltip("省電力のため低FPS推奨")] public int requestedFPS = 10;

    [Header("Detection Model")]
    [Tooltip("StreamingAssets 内のカスケードパス")]
    public string cascadeFilename = "lbpcascade_frontalface.xml";
    [Tooltip("最小顔サイズ（縮小画像の高さ比）")]
    [Range(0.05f, 0.6f)]
    public float minFaceRatio = 0.2f;

    [Header("Performance")]
    [Tooltip("検出用ダウンサンプル幅（小さいほど軽い）")]
    public int detectWidth = 240;
    [Tooltip("検出間隔(ms) 例: 2000で0.5Hz")]
    public int detectIntervalMs = 2000;
    [Tooltip("モーション差分のON判定しきい値（画素数）")]
    public int motionPixelThreshold = 500;

    [Header("Events")]
    [Tooltip("顔の有無が変化したら true/false を通知")]
    public UnityEvent<bool> onFacePresenceChanged;

    [SerializeField]
    private FirebaseManager _firebaseManager;
    [SerializeField] private DogController _dogController;
    [SerializeField] private int attentionMax = 5;

    // 内部
    private CascadeClassifier cascade;
    private Mat rgba, gray, graySmall, prevGraySmall, diffSmall;
    private MatOfRect faces = new MatOfRect();
    private float nextDetectTimeMs;
    private float scaleToSmall = 1f;
    private bool isInitialized = false;
    private bool lastPresence = false;

    // ログを出し過ぎないためのスロットル
    private float nextLogTime = 0f;
    private const float logIntervalSec = 1f;

    void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 30;
    }

    void Start()
    {
        if (webCamHelper == null) webCamHelper = GetComponent<WebCamTextureToMatHelper>();

        // イベント購読（Initialize前に）
        webCamHelper.onInitialized.AddListener(OnWebCamTextureToMatHelperInitialized);
        webCamHelper.onDisposed.AddListener(OnWebCamTextureToMatHelperDisposed);
        webCamHelper.onErrorOccurred.AddListener(OnWebCamTextureToMatHelperErrorOccurred);

        // カメラ要求
        webCamHelper.requestedIsFrontFacing = requestedIsFrontFacing;
        webCamHelper.requestedWidth = requestedWidth;
        webCamHelper.requestedHeight = requestedHeight;
        webCamHelper.requestedFPS = requestedFPS;

#if UNITY_ANDROID && !UNITY_EDITOR
        webCamHelper.avoidAndroidFrontCameraLowLightIssue = true;
#endif

        // カスケード読込
        string cascadePath = Utils.getFilePath(cascadeFilename);
        if (string.IsNullOrEmpty(cascadePath))
        {
            GameLogger.LogError(LogCategory.Face, $"Cascade not found: {cascadeFilename}. Place it under Assets/StreamingAssets/OpenCVForUnity/");
        }
        else
        {
            cascade = new CascadeClassifier(cascadePath);
            if (cascade.empty())
            {
                GameLogger.LogError(LogCategory.Face, "Failed to load CascadeClassifier.");
                cascade.Dispose(); cascade = null;
            }
            else
            {
                GameLogger.Log(LogCategory.Face, $"Cascade loaded successfully from {cascadePath}");
            }
        }

        webCamHelper.Initialize();
    }

    public void OnWebCamTextureToMatHelperInitialized()
    {
        isInitialized = false;
        try
        {
            // 1) Matの取得
            try
            {
                rgba = webCamHelper.GetMat();
                if (rgba == null || rgba.empty())
                    throw new InvalidOperationException("rgba mat is null or empty.");
                gray = new Mat(rgba.rows(), rgba.cols(), CvType.CV_8UC1);
                GameLogger.Log(LogCategory.Face, $"[Init-1] rgba={rgba.cols()}x{rgba.rows()} gray OK");
            }
            catch (Exception e)
            {
                GameLogger.LogError(LogCategory.Face, $"[Init-1] Mat init failed: {e}");
                GameLogger.LogException(LogCategory.Face, e);
                return;
            }

            // 2) 縮小用Mat準備（検出は縮小画像でのみ実施）
            try
            {
                int smallW = Mathf.Min(detectWidth, rgba.cols());
                int smallH = Mathf.RoundToInt(rgba.rows() * (smallW / (float)rgba.cols()));
                if (smallW <= 0 || smallH <= 0)
                    throw new ArgumentOutOfRangeException($"smallW/H invalid: {smallW}x{smallH}");
                scaleToSmall = smallW / (float)rgba.cols();
                graySmall = new Mat(smallH, smallW, CvType.CV_8UC1);
                prevGraySmall = new Mat(smallH, smallW, CvType.CV_8UC1);
                diffSmall = new Mat(smallH, smallW, CvType.CV_8UC1);
                GameLogger.Log(LogCategory.Face, $"[Init-2] graySmall={smallW}x{smallH} scale={scaleToSmall}");
            }
            catch (Exception e)
            {
                GameLogger.LogError(LogCategory.Face, $"[Init-2] Small mats init failed: {e}");
                GameLogger.LogException(LogCategory.Face, e);
                return;
            }

            nextDetectTimeMs = Time.realtimeSinceStartup * 1000f + detectIntervalMs;
            isInitialized = true;
            GameLogger.Log(LogCategory.Face, $"WebCamTextureToMatHelper initialized. Frame size: {rgba.cols()}x{rgba.rows()}");
        }
        catch (Exception e)
        {
            GameLogger.LogError(LogCategory.Face, $"[Init-FATAL] Unhandled exception: {e}");
            GameLogger.LogException(LogCategory.Face, e);
            isInitialized = false;
        }
    }

    public void OnWebCamTextureToMatHelperDisposed()
    {
        Release(ref gray);
        Release(ref graySmall);
        Release(ref prevGraySmall);
        Release(ref diffSmall);
        Release(ref rgba);
        faces?.Dispose(); faces = null;
        isInitialized = false;
        GameLogger.Log(LogCategory.Face, "WebCamTextureToMatHelper disposed.");
    }

    public void OnWebCamTextureToMatHelperErrorOccurred(WebCamTextureToMatHelper.ErrorCode errorCode)
    {
        GameLogger.LogError(LogCategory.Face, $"WebCamTextureToMatHelper error: {errorCode}");
    }

    void Update()
    {
        if (!webCamHelper.IsPlaying() || !webCamHelper.DidUpdateThisFrame()) return;
        if (!isInitialized || cascade == null) return;
        if (!ShouldProcessFrame()) return;

        // 入力取得 → グレースケール化 → 縮小
        rgba = webCamHelper.GetMat();
        Imgproc.cvtColor(rgba, gray, Imgproc.COLOR_RGBA2GRAY);
        Imgproc.resize(gray, graySmall, graySmall.size(), 0, 0, Imgproc.INTER_AREA);

        bool performDetect = false;
        float nowMs = Time.realtimeSinceStartup * 1000f;

        // 1秒に1回だけ検出（かつ動きがある時に限定）
        if (nowMs >= nextDetectTimeMs)
        {
            if (!prevGraySmall.empty())
            {
                Core.absdiff(graySmall, prevGraySmall, diffSmall);
                Imgproc.threshold(diffSmall, diffSmall, 8, 255, Imgproc.THRESH_BINARY);
                int changed = Core.countNonZero(diffSmall);
                if (changed >= motionPixelThreshold)
                    performDetect = true;
            }
            else
            {
                performDetect = true;
            }

            graySmall.copyTo(prevGraySmall);
            nextDetectTimeMs = nowMs + detectIntervalMs;
        }

        if (!performDetect) return;

        // 顔検出（縮小画像のみ）
        Imgproc.equalizeHist(graySmall, graySmall);
        int minSize = Mathf.RoundToInt(minFaceRatio * graySmall.rows());
        if (minSize < 16) minSize = 16;

        cascade.detectMultiScale(
            graySmall,
            faces,
            1.1, 3, 0,
            new Size(minSize, minSize),
            new Size()
        );

        bool presence = faces != null && faces.toArray().Length > 0;
        if (presence != lastPresence)
        {
            lastPresence = presence;
            onFacePresenceChanged?.Invoke(presence);
            LogThrottled(presence ? "Face: PRESENT" : "Face: ABSENT");
            if (presence)
            {
                if (_dogController != null)
                {
                    _dogController.ActionBool(true);
                }
                GlobalVariables.AttentionCount++;
                if (_firebaseManager != null)
                {
                    StartCoroutine(LogFaceEventCoroutine());
                }
            }
        }
    }

    private IEnumerator LogFaceEventCoroutine()
    {
        var task = _firebaseManager.UpdateLog("face");
        while (!task.IsCompleted)
        {
            yield return null;
        }
        if (task.Exception != null)
        {
            GameLogger.LogError(LogCategory.Face, task.Exception.ToString());
        }
    }

    void OnDestroy()
    {
        webCamHelper?.Dispose();
        if (cascade != null) { cascade.Dispose(); cascade = null; }
    }

    private void Release(ref Mat m)
    {
        if (m != null) { m.Dispose(); m = null; }
    }

    private void LogThrottled(string msg)
    {
        float t = Time.realtimeSinceStartup;
        if (t >= nextLogTime)
        {
            GameLogger.Log(LogCategory.Face, msg);
            nextLogTime = t + logIntervalSec;
        }
    }

    private bool ShouldProcessFrame()
    {
        return
            GlobalVariables.AttentionCount < attentionMax
            && _dogController != null
            && _dogController.GetTransitionNo() != 3
            && _dogController.GetTransitionNo() != 4
            && GlobalVariables.CurrentState == PetState.idle
            && !_dogController.GetIsAction()
            && _dogController.GetSnackType() == 0;
    }
}
