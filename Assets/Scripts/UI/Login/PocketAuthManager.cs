using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Firebase.Auth;
using TapHouse.Logging;

/// <summary>
/// たっぷポケット用のブラウザ認証管理
/// OSブラウザでログイン後、Deep Linkでカスタムトークンを受け取り認証する
/// </summary>
public class PocketAuthManager : MonoBehaviour
{
    private const string LOGIN_URL = "https://pichipichipet.web.app/";
    private const string POCKET_MAIN_SCENE = "PocketMain";

    /// <summary>認証完了時</summary>
    public event Action OnAuthCompleted;

    /// <summary>認証キャンセル時</summary>
    public event Action OnAuthCancelled;

    /// <summary>認証エラー時</summary>
    public event Action<string> OnAuthError;

    private FirebaseAuth _auth;
    private bool _isAuthenticating;

    private static PocketAuthManager _instance;

    /// <summary>
    /// シングルトンインスタンス
    /// </summary>
    public static PocketAuthManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("PocketAuthManager");
                _instance = go.AddComponent<PocketAuthManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    async void Start()
    {
        await Task.Yield();
        _auth = FirebaseAuth.DefaultInstance;
        SetupDeepLinkListeners();
    }

    void OnDestroy()
    {
        if (_instance == this)
        {
            CleanupDeepLinkListeners();
            _instance = null;
        }
    }

    private void SetupDeepLinkListeners()
    {
        var handler = DeepLinkHandler.Instance;
        handler.OnTokenReceived += HandleTokenReceived;
        handler.OnCancelled += HandleCancelled;
        handler.OnError += HandleError;
    }

    private void CleanupDeepLinkListeners()
    {
        if (DeepLinkHandler.Instance != null)
        {
            var handler = DeepLinkHandler.Instance;
            handler.OnTokenReceived -= HandleTokenReceived;
            handler.OnCancelled -= HandleCancelled;
            handler.OnError -= HandleError;
        }
    }

    /// <summary>
    /// ブラウザでログインページを開く
    /// </summary>
    public void OpenBrowserLogin()
    {
        if (_isAuthenticating) return;

        _isAuthenticating = true;
        Application.OpenURL(LOGIN_URL);
    }

    /// <summary>
    /// カスタムトークンでサインイン
    /// </summary>
    public async Task<bool> SignInWithCustomToken(string token)
    {
        if (_auth == null)
        {
            _auth = FirebaseAuth.DefaultInstance;
        }

        try
        {
            var result = await _auth.SignInWithCustomTokenAsync(token);

            if (result.User != null)
            {

                // ユーザー情報を保存
                PlayerPrefs.SetString(PrefsKeys.UserId, result.User.UserId);
                if (!string.IsNullOrEmpty(result.User.Email))
                {
                    PlayerPrefs.SetString(PrefsKeys.Email, result.User.Email);
                }
                if (!string.IsNullOrEmpty(result.User.DisplayName))
                {
                    PlayerPrefs.SetString(PrefsKeys.DisplayName, result.User.DisplayName);
                }
                PlayerPrefs.Save();

                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            GameLogger.LogException(LogCategory.Firebase, ex);
            Debug.LogError($"[PocketAuthManager] Sign in failed: {ex.Message}");
            return false;
        }
    }

    private async void HandleTokenReceived(string token)
    {
#if UNITY_EDITOR
        // デバッグ用トークンの場合はFirebase認証をスキップ
        if (token.StartsWith("debug_"))
        {
            _isAuthenticating = false;
            OnAuthCompleted?.Invoke();
            await LoadSceneAsync(POCKET_MAIN_SCENE);
            return;
        }
#endif

        var success = await SignInWithCustomToken(token);
        _isAuthenticating = false;

        if (success)
        {
            OnAuthCompleted?.Invoke();
            await LoadSceneAsync(POCKET_MAIN_SCENE);
        }
        else
        {
            OnAuthError?.Invoke("sign_in_failed");
        }
    }

    private void HandleCancelled()
    {
        _isAuthenticating = false;
        OnAuthCancelled?.Invoke();
    }

    private void HandleError(string error)
    {
        Debug.LogError($"[PocketAuthManager] Auth error: {error}");
        _isAuthenticating = false;
        OnAuthError?.Invoke(error);
    }

    private async Task LoadSceneAsync(string sceneName)
    {
        var op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone)
        {
            await Task.Yield();
        }
    }

    /// <summary>
    /// 認証中かどうか
    /// </summary>
    public bool IsAuthenticating => _isAuthenticating;

    /// <summary>
    /// 認証状態をリセット
    /// </summary>
    public void ResetAuthState()
    {
        _isAuthenticating = false;
    }

    /// <summary>
    /// イベントリスナーをクリア
    /// </summary>
    public void ClearListeners()
    {
        OnAuthCompleted = null;
        OnAuthCancelled = null;
        OnAuthError = null;
    }
}
