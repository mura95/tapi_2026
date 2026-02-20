using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Firebase.Auth;
using TMPro;
using TapHouse.UI;

/// <summary>
/// 起動時のモード選択画面を管理
/// - ログイン済み: 保存モードに応じて即遷移
/// - 未ログイン: モード選択UIを表示
/// </summary>
public class ModeSelectManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject modeSelectUI;
    [SerializeField] private Button tapHouseButton;
    [SerializeField] private Button tapPocketButton;

    [Header("Loading")]
    [SerializeField] private LoadingOverlay loadingOverlay;

    [Header("Scene Names")]
    [SerializeField] private string loginSceneName = "login";
    [SerializeField] private string mainSceneName = "Main";
    [SerializeField] private string pocketMainSceneName = "PocketMain";

#if UNITY_EDITOR
    [Header("Debug（Editorのみ）")]
    [Tooltip("オンにすると、ログイン済みでも自動遷移せずモード選択画面を表示")]
    [SerializeField] private bool skipAutoRedirect = false;

    [Tooltip("デバッグ用UIパネル（Deep Linkシミュレート用）")]
    [SerializeField] private GameObject debugPanel;
    [SerializeField] private Button debugSimulateSuccessButton;
    [SerializeField] private Button debugSimulateCancelButton;
    [SerializeField] private Button debugSimulateErrorButton;
    [SerializeField] private TMP_InputField debugTokenInput;
#endif

    private FirebaseAuth _auth;
    private PocketAuthManager _pocketAuth;

    async void Start()
    {
        // UIを非表示で開始
        if (modeSelectUI != null)
        {
            modeSelectUI.SetActive(false);
        }

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

        // 認証確認中はローディング表示
        ShowLoading("読み込み中…");

        // ボタンイベント登録
        if (tapHouseButton != null)
        {
            tapHouseButton.onClick.AddListener(OnTapHouseSelected);
        }
        if (tapPocketButton != null)
        {
            tapPocketButton.onClick.AddListener(OnTapPocketSelected);
        }

#if UNITY_EDITOR
        SetupDebugButtons();
#endif

        await Task.Yield();

        _auth = FirebaseAuth.DefaultInstance;

        // PocketAuthManagerのセットアップ
        _pocketAuth = PocketAuthManager.Instance;
        _pocketAuth.OnAuthCompleted += OnPocketAuthCompleted;
        _pocketAuth.OnAuthCancelled += OnPocketAuthCancelled;
        _pocketAuth.OnAuthError += OnPocketAuthError;

        bool shouldAutoRedirect = _auth.CurrentUser != null;

#if UNITY_EDITOR
        if (skipAutoRedirect)
        {
            shouldAutoRedirect = false;
        }
#endif

        if (shouldAutoRedirect)
        {
            // ログイン済み → 保存モードで即遷移
            await RedirectByAppMode();
        }
        else
        {
            // 未ログイン or デバッグモード → モード選択UIを表示
            HideLoading();
            ShowModeSelectUI();
        }
    }

    void OnDestroy()
    {
        if (_pocketAuth != null)
        {
            _pocketAuth.OnAuthCompleted -= OnPocketAuthCompleted;
            _pocketAuth.OnAuthCancelled -= OnPocketAuthCancelled;
            _pocketAuth.OnAuthError -= OnPocketAuthError;
        }
    }

    private void ShowModeSelectUI()
    {
        if (modeSelectUI != null)
        {
            modeSelectUI.SetActive(true);
        }
    }

    private void HideModeSelectUI()
    {
        if (modeSelectUI != null)
        {
            modeSelectUI.SetActive(false);
        }
    }

    private async Task RedirectByAppMode()
    {
        var mode = AppModeManager.Load();
        string targetScene = mode == AppMode.TapPocket ? pocketMainSceneName : mainSceneName;
        await LoadSceneAsync(targetScene);
    }

    /// <summary>
    /// たっぷハウスボタン押下
    /// </summary>
    public void OnTapHouseSelected()
    {
        AppModeManager.Save(AppMode.TapHouse);
        SceneManager.LoadScene(loginSceneName);
    }

    /// <summary>
    /// たっぷポケットボタン押下
    /// </summary>
    public void OnTapPocketSelected()
    {
        AppModeManager.Save(AppMode.TapPocket);

        // ブラウザでログインページを開く
        ShowLoading("ブラウザでログイン中…");
        HideModeSelectUI();
        _pocketAuth.OpenBrowserLogin();
    }

    private void OnPocketAuthCompleted()
    {
        HideLoading();
        // PocketAuthManagerがPocketMainシーンに遷移済み
    }

    private void OnPocketAuthCancelled()
    {
        HideLoading();
        ShowModeSelectUI();
    }

    private void OnPocketAuthError(string error)
    {
        HideLoading();
        ShowModeSelectUI();
    }

    private void ShowLoading(string message)
    {
        if (loadingOverlay != null)
        {
            loadingOverlay.Show(message);
        }
    }

    private void HideLoading()
    {
        if (loadingOverlay != null)
        {
            loadingOverlay.Hide();
        }
    }

    private async Task LoadSceneAsync(string sceneName)
    {
        var op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone)
        {
            await Task.Yield();
        }
    }

#if UNITY_EDITOR
    #region Debug Methods

    private void SetupDebugButtons()
    {
        // デバッグパネルの表示（skipAutoRedirectがオンの時のみ）
        if (debugPanel != null)
        {
            debugPanel.SetActive(skipAutoRedirect);
        }

        // デバッグボタンのイベント登録
        if (debugSimulateSuccessButton != null)
        {
            debugSimulateSuccessButton.onClick.AddListener(OnDebugSimulateSuccess);
        }
        if (debugSimulateCancelButton != null)
        {
            debugSimulateCancelButton.onClick.AddListener(OnDebugSimulateCancel);
        }
        if (debugSimulateErrorButton != null)
        {
            debugSimulateErrorButton.onClick.AddListener(OnDebugSimulateError);
        }
    }

    /// <summary>
    /// [Debug] 認証成功をシミュレート
    /// </summary>
    private void OnDebugSimulateSuccess()
    {
        string token = "debug_test_token";

        if (debugTokenInput != null && !string.IsNullOrEmpty(debugTokenInput.text))
        {
            token = debugTokenInput.text;
        }

        ShowLoading("デバッグ: 認証シミュレート中…");
        DeepLinkHandler.Instance.SimulateTokenReceived(token);
    }

    /// <summary>
    /// [Debug] 認証キャンセルをシミュレート
    /// </summary>
    private void OnDebugSimulateCancel()
    {
        DeepLinkHandler.Instance.SimulateCancelled();
    }

    /// <summary>
    /// [Debug] 認証エラーをシミュレート
    /// </summary>
    private void OnDebugSimulateError()
    {
        DeepLinkHandler.Instance.SimulateError("debug_test_error");
    }

    #endregion
#endif
}
