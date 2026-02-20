using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Firebase;
using Firebase.Auth;
using TapHouse.Logging;

public class LoginFormUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] TMP_InputField emailInput;
    [SerializeField] TMP_InputField passwordInput;
    [SerializeField] Button          loginButton;
    [SerializeField] TMP_Text        errorMessageText;
    [SerializeField] TMP_Text        loadingText;
    [SerializeField] GameObject      spinnerRoot;

    [Header("Spinner Settings")]
    [SerializeField] Transform       spinnerImage;
    [SerializeField] float           spinnerSpeed = 180f;
    [SerializeField] float           curveLoopDuration = 1.5f;
    [SerializeField] AnimationCurve  speedCurve = new AnimationCurve(
        new Keyframe(0f, 0.3f, 0f, 2f),
        new Keyframe(0.5f, 1.2f, 0f, 0f),
        new Keyframe(1f, 0.3f, -2f, 0f)
    );

    bool          isLoggingIn;
    bool          isSpinning;
    FirebaseAuth  firebaseAuth;
    int           originalSleepTimeout;

    enum UIState { Booting, Ready, Authenticating }
    UIState current;

    async void Start()
    {
        originalSleepTimeout = Screen.sleepTimeout;
        ShowLoadingText();
        SetState(UIState.Booting, "読み込み中 …");
        await Task.Yield();

        // 平文パスワードを暗号化形式に移行
        SecurePlayerPrefs.MigratePlaintextPassword();

        firebaseAuth = FirebaseAuth.DefaultInstance;
        ClearErrorMessage();

        // 既存セッションがあれば即遷移
        if (firebaseAuth.CurrentUser != null)
        {
            await TransitionMainScene();
            return;
        }

        // 自動ログイン判定
        if (TryAutoLogin())
        {
            return;
        }

        // UI を有効化
        HideLoadingText();
        EnableLoginUI(true);
        SetState(UIState.Ready, "");

        loginButton.onClick.AddListener(OnEmailLogin);
    }

    void Update()
    {
        if (isSpinning && spinnerImage != null)
        {
            float t = (Time.time % curveLoopDuration) / curveLoopDuration;
            float speedMultiplier = speedCurve.Evaluate(t);
            spinnerImage.Rotate(0f, spinnerSpeed * speedMultiplier * Time.deltaTime, 0f);
        }
    }

    void OnDestroy()
    {
        Screen.sleepTimeout = originalSleepTimeout;
    }

    #region UI Helper
    void ShowLoadingText()
    {
        if (loadingText == null) return;
        loadingText.text = "読み込み中 …";
        loadingText.gameObject.SetActive(true);
        if (spinnerRoot != null) spinnerRoot.SetActive(true);

        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        isSpinning = true;
    }
    void HideLoadingText()
    {
        if (loadingText == null) return;
        loadingText.text = "";
        loadingText.gameObject.SetActive(false);
        if (spinnerRoot != null) spinnerRoot.SetActive(false);

        Screen.sleepTimeout = originalSleepTimeout;
        isSpinning = false;
    }

    void EnableLoginUI(bool enable)
    {
        emailInput.gameObject.SetActive(enable);
        passwordInput.gameObject.SetActive(enable);
        loginButton.gameObject.SetActive(enable);
        errorMessageText.gameObject.SetActive(enable);
        loginButton.interactable = enable;
    }
    void DisplayError(string msg)
    {
        errorMessageText.text = msg;
        errorMessageText.gameObject.SetActive(true);
    }
    void ClearErrorMessage() => DisplayError("");
    #endregion

    #region Auto-login
    bool TryAutoLogin()
    {
        if (PlayerPrefs.HasKey(PrefsKeys.Email) && SecurePlayerPrefs.HasKey(PrefsKeys.Password))
        {
            OnEmailLogin(PlayerPrefs.GetString(PrefsKeys.Email),
                         SecurePlayerPrefs.GetString(PrefsKeys.Password));
            return true;
        }
        return false;
    }
    #endregion

    #region Email Login
    public void OnEmailLogin() =>
        OnEmailLogin(emailInput.text, passwordInput.text);

    async void OnEmailLogin(string email, string password)
    {
        if (isLoggingIn) return;
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            DisplayError("メールとパスワードを入力してください。");
            return;
        }

        isLoggingIn = true;
        ShowLoadingText();
        EnableLoginUI(false);
        SetState(UIState.Authenticating, "認証中 …");

        try
        {
            var result = await firebaseAuth.SignInWithEmailAndPasswordAsync(email, password);
            SaveAutoLogin(email, password, result.User.UserId);
            await TransitionMainScene();
            // シーン遷移成功時はスリープ防止を維持したまま（OnDestroyで復元）
            return;
        }
        catch (Exception ex)
        {
            GameLogger.LogException(LogCategory.Firebase, ex);
            DisplayError("ログインに失敗しました。");
            HideLoadingText();
            EnableLoginUI(true);
            SetState(UIState.Ready, "");
            isLoggingIn = false;
        }
    }
    #endregion

    void SaveAutoLogin(string email, string pass, string userId)
    {
        PlayerPrefs.SetString(PrefsKeys.Email, email);
        SecurePlayerPrefs.SetString(PrefsKeys.Password, pass);
        PlayerPrefs.SetString(PrefsKeys.UserId, userId);
        PlayerPrefs.Save();
    }

    async Task TransitionMainScene()
    {
        ShowLoadingText();
        SetState(UIState.Authenticating, "ロード中 …");
        var op = SceneManager.LoadSceneAsync("Main");
        while (!op.isDone)
        {
            await Task.Yield();
        }
    }

    void SetState(UIState next, string labelText)
    {
        current = next;
        bool showSpinner = next != UIState.Ready;

        if (spinnerRoot != null) spinnerRoot.SetActive(showSpinner);
        loadingText.gameObject.SetActive(showSpinner);
        loadingText.text = labelText;
        EnableLoginUI(next == UIState.Ready);
    }
}
