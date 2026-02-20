using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TapHouse.MultiDevice;
using TapHouse.UI;

namespace TapHouse.Pocket.UI
{
    /// <summary>
    /// ポケットメイン画面のUI制御
    /// 2x2グリッド: コール、着替え、歩数計、犬と遊ぶ
    /// </summary>
    public class PocketMainUI : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject mainUIPanel;

        [Header("2x2 Grid Buttons")]
        [SerializeField] private Button callButton;
        [SerializeField] private Button dressUpButton;
        [SerializeField] private Button pedometerButton;
        [SerializeField] private Button miniGameButton;

        [Header("Header")]
        [SerializeField] private Button settingsButton;

        [Header("Loading")]
        [SerializeField] private LoadingOverlay loadingOverlay;

        [Header("Button Animation")]
        [SerializeField] private float buttonPressScale = 0.9f;
        [SerializeField] private float buttonAnimDuration = 0.1f;

        [Header("Mypage URL")]
        [SerializeField] private string mypageUrl = "https://your-taphouse-web-url.com/mypage";

        [Header("Scene Names")]
        [SerializeField] private string mainSceneName = "main";
        [SerializeField] private string dressUpSceneName = "DressUp";
        [SerializeField] private string pedometerSceneName = "Pedometer";
        [SerializeField] private string miniGamesSceneName = "MiniGames";

        private bool _isLoading = false;

        private void Start()
        {
            SetupButtons();

            // LoadingOverlayが未設定の場合、シーンから検索
            if (loadingOverlay == null)
            {
                loadingOverlay = FindObjectOfType<LoadingOverlay>();

                // 非アクティブなオブジェクトも検索
                if (loadingOverlay == null)
                {
                    var allOverlays = Resources.FindObjectsOfTypeAll<LoadingOverlay>();
                    foreach (var overlay in allOverlays)
                    {
                        if (overlay.gameObject.scene.isLoaded)
                        {
                            loadingOverlay = overlay;
                            break;
                        }
                    }
                }
            }

            if (loadingOverlay == null)
            {
                Debug.LogWarning("[PocketMainUI] LoadingOverlay not found");
            }
        }

        private void SetupButtons()
        {
            // コールボタン → サブ機設定後、main.unityに遷移
            if (callButton != null)
            {
                callButton.onClick.AddListener(() => OnCallButtonPressed());
            }

            // 着替えボタン
            if (dressUpButton != null)
            {
                dressUpButton.onClick.AddListener(() => OnButtonPressed(dressUpButton, dressUpSceneName));
            }

            // 歩数計ボタン
            if (pedometerButton != null)
            {
                pedometerButton.onClick.AddListener(() => OnButtonPressed(pedometerButton, pedometerSceneName));
            }

            // ミニゲームボタン
            if (miniGameButton != null)
            {
                miniGameButton.onClick.AddListener(() => OnButtonPressed(miniGameButton, miniGamesSceneName));
            }

            // 設定ボタン → Webブラウザでmypageを開く
            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(() =>
                {
                    StartCoroutine(AnimateButtonPress(settingsButton, () =>
                    {
                        if (!string.IsNullOrEmpty(mypageUrl))
                        {
                            Application.OpenURL(mypageUrl);
                        }
                    }));
                });
            }
        }

        /// <summary>
        /// コールボタン押下時の処理
        /// </summary>
        private void OnCallButtonPressed()
        {
            if (_isLoading) return;

            // マルチデバイスシステム: サブ機として設定
            if (DogLocationSync.Instance != null)
            {
                DogLocationSync.Instance.SetDeviceRole(DeviceRole.Sub);
            }

            StartCoroutine(AnimateButtonAndLoadScene(callButton, mainSceneName));
        }

        /// <summary>
        /// ボタン押下時の処理
        /// </summary>
        private void OnButtonPressed(Button button, string sceneName)
        {
            if (_isLoading) return;

            StartCoroutine(AnimateButtonAndLoadScene(button, sceneName));
        }

        /// <summary>
        /// ボタンアニメーション後にシーン読み込み
        /// </summary>
        private IEnumerator AnimateButtonAndLoadScene(Button button, string sceneName)
        {
            _isLoading = true;

            // ボタン押下アニメーション
            yield return StartCoroutine(AnimateButtonPressCoroutine(button));

            // シーンが存在するか確認
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogWarning($"[PocketMainUI] Scene '{sceneName}' not found in build settings");
                _isLoading = false;
                yield break;
            }

            // メインUIを非表示
            HideMainUI();

            // ローディング表示
            ShowLoading();

            // 非同期でシーン読み込み
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

            // 読み込み完了まで待機
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
        }

        /// <summary>
        /// ボタン押下アニメーション（アクション付き）
        /// </summary>
        private IEnumerator AnimateButtonPress(Button button, System.Action onComplete)
        {
            yield return StartCoroutine(AnimateButtonPressCoroutine(button));
            onComplete?.Invoke();
        }

        /// <summary>
        /// ボタン押下アニメーション本体
        /// </summary>
        private IEnumerator AnimateButtonPressCoroutine(Button button)
        {
            if (button == null) yield break;

            Transform buttonTransform = button.transform;
            Vector3 originalScale = buttonTransform.localScale;
            Vector3 pressedScale = originalScale * buttonPressScale;

            // 縮小
            float elapsed = 0f;
            while (elapsed < buttonAnimDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / buttonAnimDuration;
                buttonTransform.localScale = Vector3.Lerp(originalScale, pressedScale, t);
                yield return null;
            }
            buttonTransform.localScale = pressedScale;

            // 戻す
            elapsed = 0f;
            while (elapsed < buttonAnimDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / buttonAnimDuration;
                buttonTransform.localScale = Vector3.Lerp(pressedScale, originalScale, t);
                yield return null;
            }
            buttonTransform.localScale = originalScale;
        }

        /// <summary>
        /// ローディング表示
        /// </summary>
        private void ShowLoading()
        {
            if (loadingOverlay != null)
            {
                loadingOverlay.Show("読み込み中…");
            }
        }

        /// <summary>
        /// ローディング非表示
        /// </summary>
        private void HideLoading()
        {
            if (loadingOverlay != null)
            {
                loadingOverlay.Hide();
            }
        }

        /// <summary>
        /// メインUIを非表示
        /// </summary>
        private void HideMainUI()
        {
            if (mainUIPanel != null)
            {
                mainUIPanel.SetActive(false);
            }
        }

        /// <summary>
        /// メインUIを表示
        /// </summary>
        private void ShowMainUI()
        {
            if (mainUIPanel != null)
            {
                mainUIPanel.SetActive(true);
            }
        }
    }
}
