using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using Firebase.Firestore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Firebase.Auth;
using UnityEngine.SceneManagement;
using TapHouse.Logging;

public class FirebaseManager : MonoBehaviour
{
    private string basePath => string.IsNullOrEmpty(userId) ? "" : $"users/{userId}/";
    private string statePath => $"{basePath}state";
    private string feedLogPath => $"{basePath}feedLog";
    private string skillLogsPath => $"{basePath}skillLogs";
    private string LogsPath => $"{basePath}logs";
    private FirebaseAuth auth;
    private string userId;
    [SerializeField] public DogController characterController;
    [SerializeField] public EatAnimationController eating;
    [SerializeField] public PlayManager _playManager;
    [SerializeField] private HungerManager hungerManager;

    private const string META_URL =
  "https://firebasestorage.googleapis.com/v0/b/pichipichipet.firebasestorage.app/o/releases%2FappMeta.json?alt=media&token=7108795c-fd8c-42be-a4ca-f8e48a8297e1";

    // オフライン対応
    private static bool _isConnected = true;
    private const int REQUEST_TIMEOUT_SECONDS = 10;

    // リスナー参照（OnDestroyで解除するため保持）
    private DatabaseReference _connectionRef;
    private DatabaseReference _basePathRef;
    private Firebase.Database.Query _skillLogsQuery;
    private EventHandler<ValueChangedEventArgs> _connectionHandler;
    private EventHandler<ChildChangedEventArgs> _childChangedHandler;
    private EventHandler<ChildChangedEventArgs> _childAddedHandler;

    /// <summary>
    /// Firebase接続状態（他クラスから参照可能）
    /// </summary>
    public static bool IsConnected => _isConnected;

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;

        if (auth.CurrentUser != null)
        {
            userId = auth.CurrentUser.UserId;
            GameLogger.Log(LogCategory.Firebase,$"Logged in as: {userId}");
        }
        else
        {
            GameLogger.LogError(LogCategory.Firebase,"User is not logged in. Redirecting to login...");
            RedirectToLogin();
            return;
        }

        if (string.IsNullOrEmpty(userId))
        {
            GameLogger.LogError(LogCategory.Firebase,"UserId is null or empty. Cannot proceed with Firebase operations.");
            return;
        }

        // Firebase接続状態の監視を開始
        StartConnectionMonitoring();

        HandleChildAdded(skillLogsPath, HandleGeneralLog);
        HandleChildValueChanges();
        UpdatePetState("idle");
        StartCoroutine(UpdateHungerStateWithTimestamp());
    }

    /// <summary>
    /// Firebase Realtime Databaseの接続状態を監視
    /// </summary>
    private void StartConnectionMonitoring()
    {
        _connectionRef = FirebaseDatabase.DefaultInstance.GetReference(".info/connected");
        _connectionHandler = (sender, args) =>
        {
            if (args.DatabaseError != null)
            {
                GameLogger.LogWarning(LogCategory.Firebase,$"[Firebase] Connection monitor error: {args.DatabaseError.Message}");
                return;
            }

            bool wasConnected = _isConnected;
            _isConnected = args.Snapshot.Value != null && (bool)args.Snapshot.Value;

            if (wasConnected != _isConnected)
            {
                GameLogger.Log(LogCategory.Firebase,$"[Firebase] Connection state changed: {(_isConnected ? "ONLINE" : "OFFLINE")}");
            }
        };
        _connectionRef.ValueChanged += _connectionHandler;
    }

    private void RedirectToLogin()
    {
        SceneManager.LoadScene("login");
        GameLogger.Log(LogCategory.Firebase,"Redirecting to login screen...");
    }

    /// <summary>
    /// ユーザーにエラーを通知（多言語対応）
    /// </summary>
    private void NotifyUser(string messageKey)
    {
        string message = messageKey;
        if (LocalizationManager.Instance != null)
        {
            // エラーメッセージキーから翻訳を取得
            message = LocalizationManager.Instance.GetText(messageKey);
        }
        GameLogger.LogError(LogCategory.Firebase,message);
        // TODO: UIでユーザーに表示する処理を追加
    }

    /// <summary>
    /// ログを記録（オフライン時はスキップ、クリティカルではないため）
    /// </summary>
    public async Task UpdateLog(string action)
    {
        // オフライン時はログ記録をスキップ（ログはクリティカルではない）
        if (!_isConnected)
        {
            GameLogger.Log(LogCategory.Firebase,$"[Firebase] Offline - skipping log: {action}");
            return;
        }

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(REQUEST_TIMEOUT_SECONDS));

            FirebaseFirestore firestore = FirebaseFirestore.DefaultInstance;
            CollectionReference logsCollection = firestore.Collection(LogsPath);
            Dictionary<string, object> newLog = new Dictionary<string, object>
            {
                { "action", action },
                { "timestamp", Firebase.Firestore.Timestamp.FromDateTime(DateTime.UtcNow) }
            };

            var addTask = logsCollection.AddAsync(newLog);
            var completedTask = await Task.WhenAny(addTask, Task.Delay(-1, cts.Token));

            if (completedTask == addTask && !addTask.IsFaulted)
            {
                GameLogger.Log(LogCategory.Firebase,$"[Firebase] Log recorded: {action}");
            }
            else
            {
                GameLogger.LogWarning(LogCategory.Firebase,$"[Firebase] Log recording timeout or failed: {action}");
            }
        }
        catch (OperationCanceledException)
        {
            GameLogger.LogWarning(LogCategory.Firebase,$"[Firebase] Log recording cancelled: {action}");
        }
        catch (Exception ex)
        {
            GameLogger.LogWarning(LogCategory.Firebase,$"[Firebase] Failed to log: {ex.Message}");
        }
    }

    /// <summary>
    /// ペット状態を更新（オフライン時はローカルのみ更新、復旧時に自動同期）
    /// </summary>
    public void UpdatePetState(string stateName)
    {
        if (!Enum.TryParse(stateName, out PetState state))
        {
            GameLogger.LogError(LogCategory.Firebase,$"Invalid state name: {stateName}");
            return;
        }

        // ローカル状態は即座に更新
        GlobalVariables.CurrentState = state;

        // オフライン時はFirebase書き込みをスキップ（SDKのオフラインキャッシュに任せる）
        if (!_isConnected)
        {
            GameLogger.Log(LogCategory.Firebase,$"[Firebase] Offline - state '{stateName}' will sync when reconnected");
            return;
        }

        // Fire-and-forget で書き込み（タイムアウト付き）
        _ = UpdatePetStateAsync(stateName);
    }

    private async Task UpdatePetStateAsync(string stateName)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(REQUEST_TIMEOUT_SECONDS));

            var task = FirebaseDatabase.DefaultInstance
                .GetReference(statePath)
                .SetValueAsync(stateName);

            // タイムアウト付きで待機
            var completedTask = await Task.WhenAny(task, Task.Delay(-1, cts.Token));

            if (completedTask != task)
            {
                GameLogger.LogWarning(LogCategory.Firebase,$"[Firebase] UpdatePetState timeout - will retry on reconnect");
                return;
            }

            if (task.IsFaulted)
            {
                GameLogger.LogWarning(LogCategory.Firebase,$"[Firebase] UpdatePetState failed: {task.Exception?.Message}");
            }
        }
        catch (OperationCanceledException)
        {
            GameLogger.LogWarning(LogCategory.Firebase,"[Firebase] UpdatePetState cancelled due to timeout");
        }
        catch (Exception ex)
        {
            GameLogger.LogWarning(LogCategory.Firebase,$"[Firebase] UpdatePetState error: {ex.Message}");
        }
    }

    private void HandleChildValueChanges()
    {
        _basePathRef = FirebaseDatabase.DefaultInstance.GetReference(basePath);
        _childChangedHandler = (sender, args) =>
        {
            if (args.DatabaseError != null)
            {
                GameLogger.LogError(LogCategory.Firebase,$"Database error: {args.DatabaseError.Message}");
                return;
            }

            var successValue = args.Snapshot.Child("success").Value;
            long unixTimeNow = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long logUnixTime = Convert.ToInt64(args.Snapshot.Child("timestamp").Value);

            if (Math.Abs(unixTimeNow - logUnixTime) > 10) return;

            if (successValue is bool success && success)
            {
                if (args.Snapshot.Key == "playLog")
                {
                    float angle = float.Parse(args.Snapshot.Child("angle").Value.ToString());
                    float speed = float.Parse(args.Snapshot.Child("speed").Value.ToString());

                    GameLogger.Log(LogCategory.Firebase,$"angle : {angle}, speed : {speed}");
                    if (_playManager != null)
                    {
                        _playManager.ThrowToy(speed, angle);
                    }
                }
                else if (args.Snapshot.Key == "feedLog")
                {
                    float scale = float.Parse(args.Snapshot.Child("feedScale").Value.ToString());
                    if (eating != null) StartCoroutine(eating.AnimeEating(scale));
                    hungerManager.UpdateLastEatTime(logUnixTime);
                }
            }
        };
        _basePathRef.ChildChanged += _childChangedHandler;
    }

    private void HandleChildAdded(string path, Action<DataSnapshot> handleLog)
    {
        _skillLogsQuery = FirebaseDatabase.DefaultInstance.GetReference(path).LimitToLast(1);
        _childAddedHandler = (sender, args) =>
        {
            if (args.DatabaseError != null)
            {
                GameLogger.LogError(LogCategory.Firebase,$"Error received adding child: {args.DatabaseError.Message}");
                return;
            }

            if (args.Snapshot.Exists && args.Snapshot.HasChildren)
            {
                handleLog(args.Snapshot);
            }
        };
        _skillLogsQuery.ChildAdded += _childAddedHandler;
    }

    private void HandleGeneralLog(DataSnapshot snapshot)
    {
        if (snapshot.Child("action").Value == null || snapshot.Child("timestamp").Value == null)
        {
            GameLogger.LogError(LogCategory.Firebase,"Action or timestamp value is missing in the snapshot.");
            return;
        }

        string actionValue = snapshot.Child("action").Value.ToString();
        long logUnixTime = Convert.ToInt64(snapshot.Child("timestamp").Value);

        if (characterController == null)
        {
            GameLogger.LogError(LogCategory.Firebase,"CharacterController is not initialized.");
            return;
        }

        if (IsTimestampWithinRange(logUnixTime))
        {
            ExecuteAction(actionValue);
        }
    }

    private void ExecuteAction(string actionValue)
    {
        switch (actionValue)
        {
            case "petting":
                break;
            case "ote":
                characterController.ActionRPaw();
                break;
            case "okawari":
                characterController.ActionLPaw();
                break;
            case "dance":
                characterController.ActionDance();
                break;
            case "bang":
                characterController.ActionDang();
                break;
            case "settings":
                characterController.ActionStand();
                break;
            case "lie_down":
                characterController.ActionLieDown();
                break;
            case "high_dance":
                characterController.ActionHighDance();
                break;
            case "snack":
                GameLogger.Log(LogCategory.Firebase,$"Action '{actionValue}' is not supported.");
                break;
            default:
                GameLogger.Log(LogCategory.Firebase,$"Action '{actionValue}' is not supported.");
                break;
        }
    }

    private bool IsTimestampWithinRange(long logUnixTime)
    {
        long unixTimeNow = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return Math.Abs(unixTimeNow - logUnixTime) <= 10;
    }

    /// <summary>
    /// 空腹状態をFirebaseから取得（タイムアウト付き）
    /// </summary>
    public IEnumerator UpdateHungerStateWithTimestamp()
    {
        // オフライン時はスキップ（ローカルのPlayerPrefsを使用）
        if (!_isConnected)
        {
            GameLogger.Log(LogCategory.Firebase,"[Firebase] Offline - using local hunger state");
            yield break;
        }

        var feedLogReference = FirebaseDatabase.DefaultInstance.GetReference(feedLogPath);
        var task = feedLogReference.GetValueAsync();

        float elapsed = 0f;
        float timeout = REQUEST_TIMEOUT_SECONDS;

        // タイムアウト付きで待機
        while (!task.IsCompleted && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!task.IsCompleted)
        {
            GameLogger.LogWarning(LogCategory.Firebase,"[Firebase] UpdateHungerState timeout - using local state");
            yield break;
        }

        if (task.Exception != null)
        {
            GameLogger.LogWarning(LogCategory.Firebase,$"[Firebase] Error fetching hunger state: {task.Exception.Message}");
            yield break;
        }

        DataSnapshot snapshot = task.Result;
        if (snapshot.Exists && snapshot.HasChildren)
        {
            long logUnixTime = Convert.ToInt64(snapshot.Child("timestamp").Value);
            hungerManager.UpdateLastEatTime(logUnixTime);
            GameLogger.Log(LogCategory.Firebase,"[Firebase] Hunger state synced from server");
        }
        else
        {
            GameLogger.Log(LogCategory.Firebase,"[Firebase] No feed log found - using local state");
        }
    }

    /// <summary>
    /// 表示名を取得（オフライン時はデフォルト値を返す）
    /// </summary>
    public async Task<string> GetDisplayNameAsync()
    {
        const string defaultName = "Admin";

        // オフライン時はデフォルト値を返す
        if (!_isConnected)
        {
            GameLogger.Log(LogCategory.Firebase,"[Firebase] Offline - using default display name");
            return defaultName;
        }

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(REQUEST_TIMEOUT_SECONDS));

            FirebaseFirestore firestore = FirebaseFirestore.DefaultInstance;
            DocumentReference docRef = firestore.Collection("users").Document(userId);

            var task = docRef.GetSnapshotAsync();
            var completedTask = await Task.WhenAny(task, Task.Delay(-1, cts.Token));

            if (completedTask != task)
            {
                GameLogger.LogWarning(LogCategory.Firebase,"[Firebase] GetDisplayName timeout");
                return defaultName;
            }

            DocumentSnapshot snapshot = task.Result;
            if (snapshot.Exists && snapshot.ContainsField("displayName"))
            {
                string displayName = snapshot.GetValue<string>("displayName");
                GameLogger.Log(LogCategory.Firebase,$"[Firebase] DisplayName retrieved: {displayName}");
                return displayName;
            }
            else
            {
                GameLogger.Log(LogCategory.Firebase,"[Firebase] DisplayName not found - using default");
                return defaultName;
            }
        }
        catch (OperationCanceledException)
        {
            GameLogger.LogWarning(LogCategory.Firebase,"[Firebase] GetDisplayName cancelled");
            return defaultName;
        }
        catch (Exception ex)
        {
            GameLogger.LogWarning(LogCategory.Firebase,$"[Firebase] Failed to retrieve displayName: {ex.Message}");
            return defaultName;
        }
    }

    /// <summary>
    /// ペットの名前を取得（オフライン時はデフォルト値を返す）
    /// </summary>
    public async Task<string> GetPetNameAsync()
    {
        const string defaultName = "わんちゃん";

        if (!_isConnected)
        {
            GameLogger.Log(LogCategory.Firebase, "[Firebase] Offline - using default pet name");
            return defaultName;
        }

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(REQUEST_TIMEOUT_SECONDS));

            FirebaseFirestore firestore = FirebaseFirestore.DefaultInstance;
            DocumentReference docRef = firestore.Collection("users").Document(userId);

            var task = docRef.GetSnapshotAsync();
            var completedTask = await Task.WhenAny(task, Task.Delay(-1, cts.Token));

            if (completedTask != task)
            {
                GameLogger.LogWarning(LogCategory.Firebase, "[Firebase] GetPetName timeout");
                return defaultName;
            }

            DocumentSnapshot snapshot = task.Result;
            if (snapshot.Exists && snapshot.ContainsField("petName"))
            {
                string petName = snapshot.GetValue<string>("petName");
                GameLogger.Log(LogCategory.Firebase, $"[Firebase] PetName retrieved: {petName}");
                return petName;
            }
            else
            {
                GameLogger.Log(LogCategory.Firebase, "[Firebase] PetName not found - using default");
                return defaultName;
            }
        }
        catch (OperationCanceledException)
        {
            GameLogger.LogWarning(LogCategory.Firebase, "[Firebase] GetPetName cancelled");
            return defaultName;
        }
        catch (Exception ex)
        {
            GameLogger.LogWarning(LogCategory.Firebase, $"[Firebase] Failed to retrieve petName: {ex.Message}");
            return defaultName;
        }
    }

    //     public void TryCheckForUpdate()
    //     {
    // #if UNITY_ANDROID && !UNITY_EDITOR
    //         using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
    //         var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
    //         var enumUpdate = new AndroidJavaClass("com.github.javiersantos.appupdater.enums.UpdateFrom");
    //         var enumDisp = new AndroidJavaClass("com.github.javiersantos.appupdater.enums.Display");
    //
    //         new AndroidJavaObject("com.github.javiersantos.appupdater.AppUpdater", activity)
    //             .Call<AndroidJavaObject>("setUpdateFrom", enumUpdate.GetStatic<AndroidJavaObject>("JSON"))
    //             .Call<AndroidJavaObject>("setUpdateJSON", META_URL)
    //             .Call<AndroidJavaObject>("setDisplay", enumDisp.GetStatic<AndroidJavaObject>("DIALOG"))
    //             .Call("start");
    // #else
    //         GameLogger.Log(LogCategory.Firebase,"Update check is not supported on this platform.");
    // #endif
    //     }

    /// <summary>
    /// シーン終了時にFirebaseリスナーを解除（メモリリーク防止）
    /// </summary>
    private void OnDestroy()
    {
        // 接続監視リスナーの解除
        if (_connectionRef != null && _connectionHandler != null)
        {
            _connectionRef.ValueChanged -= _connectionHandler;
        }

        // ChildChangedリスナーの解除
        if (_basePathRef != null && _childChangedHandler != null)
        {
            _basePathRef.ChildChanged -= _childChangedHandler;
        }

        // ChildAddedリスナーの解除
        if (_skillLogsQuery != null && _childAddedHandler != null)
        {
            _skillLogsQuery.ChildAdded -= _childAddedHandler;
        }

        GameLogger.Log(LogCategory.Firebase, "[Firebase] Listeners unregistered on destroy");
    }
}