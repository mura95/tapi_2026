using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace TapHouse.UI
{
    /// <summary>
    /// 共通ローディングオーバーレイ
    /// 各シーンで使用できる統一されたローディング表示
    /// </summary>
    public class LoadingOverlay : MonoBehaviour
    {
        #region Singleton (Optional)
        private static LoadingOverlay _instance;
        public static LoadingOverlay Instance => _instance;
        #endregion

        [Header("UI References")]
        [Tooltip("ローディング表示用のパネル。未設定の場合はこのGameObject自体を使用")]
        [SerializeField] private GameObject overlayPanel;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Transform spinnerTransform;

        [Header("Spinner Settings")]
        [SerializeField] private float rotateSpeed = 200f;
        [SerializeField] private bool useAnimationCurve = false;
        [SerializeField] private float curveLoopDuration = 1.5f;
        [SerializeField] private AnimationCurve speedCurve = new AnimationCurve(
            new Keyframe(0f, 0.3f, 0f, 2f),
            new Keyframe(0.5f, 1.2f, 0f, 0f),
            new Keyframe(1f, 0.3f, -2f, 0f)
        );

        [Header("Settings")]
        [SerializeField] private bool preventSleepWhileLoading = true;
        [SerializeField] private bool hideOnStart = true;
        [SerializeField] private bool enableDebugLog = false;

        private bool _isShowing;
        private int _originalSleepTimeout;
        private Action _onHideCallback;

        public bool IsShowing => _isShowing;

        /// <summary>
        /// 表示対象のGameObject（overlayPanelが未設定の場合は自身）
        /// </summary>
        private GameObject TargetPanel => overlayPanel != null ? overlayPanel : gameObject;

        private void Awake()
        {
            // シングルトン設定（オプション）
            if (_instance == null)
            {
                _instance = this;
                Log("LoadingOverlay instance set");
            }

            _originalSleepTimeout = Screen.sleepTimeout;
        }

        private void Start()
        {
            if (hideOnStart)
            {
                HideImmediate();
                Log("Hidden on start");
            }
        }

        private void Update()
        {
            if (_isShowing && spinnerTransform != null)
            {
                RotateSpinner();
            }
        }

        private void OnDestroy()
        {
            // スリープ設定を復元
            Screen.sleepTimeout = _originalSleepTimeout;

            if (_instance == this)
            {
                _instance = null;
            }
        }

        #region Public Methods

        /// <summary>
        /// ローディング表示
        /// </summary>
        public void Show(string message = "読み込み中…")
        {
            Log($"Show called with message: {message}");

            _isShowing = true;

            // パネルを表示
            TargetPanel.SetActive(true);
            Log($"TargetPanel ({TargetPanel.name}) SetActive(true)");

            SetMessage(message);

            if (preventSleepWhileLoading)
            {
                Screen.sleepTimeout = SleepTimeout.NeverSleep;
            }
        }

        /// <summary>
        /// ローディング非表示
        /// </summary>
        public void Hide()
        {
            Log("Hide called");

            _isShowing = false;

            TargetPanel.SetActive(false);

            if (preventSleepWhileLoading)
            {
                Screen.sleepTimeout = _originalSleepTimeout;
            }

            _onHideCallback?.Invoke();
            _onHideCallback = null;
        }

        /// <summary>
        /// 即座に非表示（アニメーションなし）
        /// </summary>
        public void HideImmediate()
        {
            _isShowing = false;
            TargetPanel.SetActive(false);
        }

        /// <summary>
        /// メッセージを更新
        /// </summary>
        public void SetMessage(string message)
        {
            if (messageText != null)
            {
                messageText.text = message;
            }
            else
            {
                Log("Warning: messageText is null");
            }
        }

        /// <summary>
        /// 非表示時のコールバックを設定
        /// </summary>
        public void SetOnHideCallback(Action callback)
        {
            _onHideCallback = callback;
        }

        #endregion

        #region Private Methods

        private void RotateSpinner()
        {
            if (spinnerTransform == null) return;

            float speed = rotateSpeed;

            if (useAnimationCurve)
            {
                float t = (Time.time % curveLoopDuration) / curveLoopDuration;
                speed *= speedCurve.Evaluate(t);
            }

            spinnerTransform.Rotate(0, 0, -speed * Time.deltaTime);
        }

        private void Log(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[LoadingOverlay] {message}");
            }
        }

        #endregion

        #region Static Helper Methods

        /// <summary>
        /// シーン内のLoadingOverlayを検索して表示
        /// </summary>
        public static void ShowLoading(string message = "読み込み中…")
        {
            var overlay = FindOverlay();
            if (overlay != null)
            {
                overlay.Show(message);
            }
            else
            {
                Debug.LogWarning("[LoadingOverlay] ShowLoading: No LoadingOverlay found in scene!");
            }
        }

        /// <summary>
        /// シーン内のLoadingOverlayを検索して非表示
        /// </summary>
        public static void HideLoading()
        {
            var overlay = FindOverlay();
            if (overlay != null)
            {
                overlay.Hide();
            }
            else
            {
                Debug.LogWarning("[LoadingOverlay] HideLoading: No LoadingOverlay found in scene!");
            }
        }

        private static LoadingOverlay FindOverlay()
        {
            if (_instance != null) return _instance;

            // アクティブなものを検索
            var overlay = FindObjectOfType<LoadingOverlay>();
            if (overlay != null) return overlay;

            // 非アクティブなものも検索
            var allOverlays = Resources.FindObjectsOfTypeAll<LoadingOverlay>();
            foreach (var o in allOverlays)
            {
                // シーン内のオブジェクトのみ（プレハブは除外）
                if (o.gameObject.scene.isLoaded)
                {
                    return o;
                }
            }

            return null;
        }

        #endregion
    }
}
