using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Firebase.Database;
using Firebase.Auth;
using Cysharp.Threading.Tasks;
using TapHouse.Logging;

namespace TapHouse.MultiDevice
{
    /// <summary>
    /// デバイスの役割（メイン機 or サブ機）
    /// </summary>
    public enum DeviceRole
    {
        Main,   // メイン機（デフォルトで犬がいる）
        Sub     // サブ機（呼び出しボタン表示）
    }

    /// <summary>
    /// 転送リクエストのタイプ
    /// </summary>
    public enum TransferRequestType
    {
        Call,   // 犬を呼ぶ
        Return  // 犬を返す（メイン機に戻す）
    }

    /// <summary>
    /// 複数デバイス間での犬の位置を同期するマネージャー
    /// Firebase Realtime Database を使用してデバイス間の状態を同期
    /// </summary>
    public class DogLocationSync : MonoBehaviour
    {
        #region Singleton
        public static DogLocationSync Instance { get; private set; }
        #endregion

        #region Inspector Fields
        [Header("References")]
        [SerializeField] private DogController _dogController;
        [SerializeField] private DogTransferAnimation _transferAnimation;

        [Header("Settings")]
        [SerializeField] private float timeoutMinutes = 30f;
        [SerializeField] private bool enableDebugLog = true;

        [Header("Debug/Test")]
        [Tooltip("シングルデバイステスト用：メイン機がなくても犬を呼べる")]
        [SerializeField] private bool singleDeviceTestMode = false;

        [Tooltip("テスト用：タイムアウトを短く設定（秒単位）。0=通常のtimeoutMinutesを使用")]
        [SerializeField] private float debugTimeoutSeconds = 0f;
        #endregion

        #region Public Properties
        /// <summary>現在のデバイス役割</summary>
        public DeviceRole CurrentRole { get; private set; } = DeviceRole.Main;

        /// <summary>このデバイスに犬がいるか</summary>
        public bool HasDog { get; private set; } = true;

        /// <summary>このデバイスの固有ID</summary>
        public string DeviceId { get; private set; }

        /// <summary>Firebase接続状態</summary>
        public bool IsConnected => FirebaseManager.IsConnected;

        /// <summary>タイムアウトまでの残り時間（秒）</summary>
        public float RemainingTimeoutSeconds
        {
            get
            {
                if (CurrentRole != DeviceRole.Sub || !HasDog) return -1f;
                float timeoutSec = debugTimeoutSeconds > 0 ? debugTimeoutSeconds : timeoutMinutes * 60f;
                float elapsed = (float)(DateTime.UtcNow - _lastActivityTime).TotalSeconds;
                return Mathf.Max(0f, timeoutSec - elapsed);
            }
        }
        #endregion

        #region Events
        /// <summary>犬の存在状態が変更された時に発火</summary>
        public event Action<bool> OnDogPresenceChanged;

        /// <summary>転送が開始された時に発火</summary>
        public event Action<bool> OnTransferStarted; // true = entering, false = exiting
        #endregion

        #region Private Fields
        private string _userId;
        private DatabaseReference _dogLocationRef;
        private DatabaseReference _transferRequestRef;
        private EventHandler<ValueChangedEventArgs> _locationChangedHandler;
        private EventHandler<ValueChangedEventArgs> _transferRequestHandler;
        private CancellationTokenSource _timeoutCts;
        private DateTime _lastActivityTime;
        private bool _isTransferring;
        private const int REQUEST_TIMEOUT_SECONDS = 10;
        #endregion

        #region Firebase Paths
        private string DogLocationPath => $"users/{_userId}/dogLocation";
        private string CurrentDeviceIdPath => $"{DogLocationPath}/currentDeviceId";
        private string IsMainDevicePath => $"{DogLocationPath}/isMainDevice";
        private string TransferRequestPath => $"{DogLocationPath}/transferRequest";
        private string LastActivityPath => $"{DogLocationPath}/lastActivityTimestamp";
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // デバイスIDを取得または生成
            DeviceId = GetOrCreateDeviceId();

            // 起動時はサブ機設定をクリア（常にメイン機として起動）
            ClearSubDeviceSettingOnStartup();

            // PlayerPrefsからデバイス役割を読み込み（常にMainになる）
            LoadDeviceRole();

            Log($"DogLocationSync initialized. DeviceId: {DeviceId}, Role: {CurrentRole}");
        }

        /// <summary>
        /// 起動時にサブ機設定をクリア（常にメイン機として起動するため）
        /// </summary>
        private void ClearSubDeviceSettingOnStartup()
        {
            if (PlayerPrefs.HasKey(PrefsKeys.IsSubDevice))
            {
                PlayerPrefs.DeleteKey(PrefsKeys.IsSubDevice);
                PlayerPrefs.Save();
                Log("Cleared IsSubDevice setting on startup - starting as Main device");
            }
        }
        private async void Start()
        {
            Log("Start() called");

            // コンポーネント参照の取得
            if (_dogController == null)
            {
                _dogController = FindObjectOfType<DogController>();
                Log($"DogController found: {_dogController != null}");
            }
            if (_transferAnimation == null)
            {
                _transferAnimation = FindObjectOfType<DogTransferAnimation>();
                Log($"DogTransferAnimation found: {_transferAnimation != null}");
            }

            // Firebase認証を待つ
            Log("Waiting for Firebase Auth...");
            await WaitForFirebaseAuth();

            // Firebaseリスナーをセットアップ
            Log("Setting up Firebase listeners...");
            SetupFirebaseListeners();

            // 初期状態を設定
            Log("Initializing dog presence...");
            await InitializeDogPresence();

            // タイムアウトチェックを開始
            Log("Starting timeout check...");
            StartTimeoutCheck();

            Log("Start() completed");
        }

        private void OnDestroy()
        {
            // リスナーの解除
            CleanupFirebaseListeners();

            // タイムアウトキャンセル
            _timeoutCts?.Cancel();
            _timeoutCts?.Dispose();

            if (Instance == this)
                Instance = null;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// デバイス役割を設定（設定画面から呼ばれる）
        /// </summary>
        public async void SetDeviceRole(DeviceRole role)
        {
            Log($"SetDeviceRole called: {CurrentRole} -> {role}");

            if (CurrentRole == role)
            {
                Log("Role unchanged, skipping");
                return;
            }

            CurrentRole = role;
            PlayerPrefs.SetInt(PrefsKeys.IsSubDevice, role == DeviceRole.Sub ? 1 : 0);
            PlayerPrefs.Save();

            Log($"Device role changed to: {role}");

            // 役割変更時の初期化
            await InitializeDogPresence();
            Log("InitializeDogPresence completed after role change");
        }

        /// <summary>
        /// 犬を呼ぶリクエストを送信
        /// </summary>
        public async UniTask RequestCallDog()
        {
            Log($"RequestCallDog called. HasDog={HasDog}, IsTransferring={_isTransferring}, IsConnected={IsConnected}, UserId={_userId}");

            if (_isTransferring)
            {
                Log("Transfer already in progress, ignoring request");
                return;
            }

            if (HasDog)
            {
                Log("Dog is already here, ignoring call request");
                return;
            }

            // シングルデバイステストモード：メイン機がなくても犬を呼べる
            if (singleDeviceTestMode)
            {
                Log("Single device test mode: calling dog directly");
                await CallDogDirectly();
                return;
            }

            if (string.IsNullOrEmpty(_userId))
            {
                LogError("UserId is null - Firebase not initialized");
                return;
            }

            if (!IsConnected)
            {
                LogError("Firebase not connected, cannot send request");
                return;
            }

            Log("Sending call dog request...");

            try
            {
                var requestData = new TransferRequestData
                {
                    requestingDeviceId = DeviceId,
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    type = "call"
                };

                string json = JsonUtility.ToJson(requestData);
                Log($"Request JSON: {json}");
                Log($"TransferRequestPath: {TransferRequestPath}");

                await FirebaseDatabase.DefaultInstance
                    .GetReference(TransferRequestPath)
                    .SetRawJsonValueAsync(json);

                Log("Call dog request sent successfully - waiting for main device to respond");
            }
            catch (Exception ex)
            {
                LogError($"Failed to send call request: {ex.Message}");
                LogError($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// シングルデバイステスト用：直接犬を呼ぶ
        /// </summary>
        private async UniTask CallDogDirectly()
        {
            Log("Calling dog directly (test mode)");

            // Firebaseを更新（このデバイスに犬がいる状態に）
            if (!string.IsNullOrEmpty(_userId) && IsConnected)
            {
                await UpdateDogLocation(DeviceId, false);
            }

            // 犬を登場させる
            await EnterDog();
        }

        /// <summary>
        /// 犬をメイン機に返すリクエストを送信
        /// </summary>
        public async UniTask ReturnDogToMain()
        {
            if (_isTransferring)
            {
                Log("Transfer already in progress, ignoring request");
                return;
            }

            if (!HasDog)
            {
                Log("Dog is not here, cannot return");
                return;
            }

            if (CurrentRole == DeviceRole.Main)
            {
                Log("Already main device, cannot return");
                return;
            }

            Log("Returning dog to main device...");

            // 犬を退場させる
            await ExitDog();

            // Firebaseを更新（メイン機に戻す）
            await UpdateDogLocationToMain();
        }

        /// <summary>
        /// アクティビティを記録（タイムアウトリセット）
        /// </summary>
        public void RecordActivity()
        {
            _lastActivityTime = DateTime.UtcNow;

            if (HasDog && IsConnected)
            {
                _ = UpdateLastActivityAsync();
            }
        }

        /// <summary>
        /// テスト用：即座にタイムアウトを発動して犬をメイン機に返す
        /// </summary>
        [ContextMenu("Force Timeout (Return Dog to Main)")]
        public void ForceTimeout()
        {
            if (CurrentRole == DeviceRole.Sub && HasDog)
            {
                Log("ForceTimeout called - returning dog to main device");
                _ = ReturnDogToMain();
            }
            else
            {
                Log($"ForceTimeout skipped - Role: {CurrentRole}, HasDog: {HasDog}");
            }
        }
        #endregion

        #region Firebase Setup
        private async UniTask WaitForFirebaseAuth()
        {
            int maxWait = 50; // 5秒
            int waited = 0;

            while (FirebaseAuth.DefaultInstance?.CurrentUser == null && waited < maxWait)
            {
                await UniTask.Delay(100);
                waited++;
            }

            var user = FirebaseAuth.DefaultInstance?.CurrentUser;
            if (user != null)
            {
                _userId = user.UserId;
                Log($"Firebase Auth ready. UserId: {_userId}");
            }
            else
            {
                LogError("Firebase Auth timeout - user not logged in");
            }
        }

        private void SetupFirebaseListeners()
        {
            if (string.IsNullOrEmpty(_userId))
            {
                LogError("Cannot setup listeners - userId is null");
                return;
            }

            // 犬の位置変更リスナー
            _dogLocationRef = FirebaseDatabase.DefaultInstance.GetReference(DogLocationPath);
            _locationChangedHandler = (sender, args) =>
            {
                if (args.DatabaseError != null)
                {
                    LogError($"Location listener error: {args.DatabaseError.Message}");
                    return;
                }
                OnDogLocationChanged(args.Snapshot);
            };
            _dogLocationRef.ValueChanged += _locationChangedHandler;

            // 転送リクエストリスナー
            _transferRequestRef = FirebaseDatabase.DefaultInstance.GetReference(TransferRequestPath);
            _transferRequestHandler = (sender, args) =>
            {
                if (args.DatabaseError != null)
                {
                    LogError($"Transfer request listener error: {args.DatabaseError.Message}");
                    return;
                }
                OnTransferRequestChanged(args.Snapshot);
            };
            _transferRequestRef.ValueChanged += _transferRequestHandler;

            Log("Firebase listeners setup complete");
        }

        private void CleanupFirebaseListeners()
        {
            if (_dogLocationRef != null && _locationChangedHandler != null)
            {
                _dogLocationRef.ValueChanged -= _locationChangedHandler;
            }
            if (_transferRequestRef != null && _transferRequestHandler != null)
            {
                _transferRequestRef.ValueChanged -= _transferRequestHandler;
            }
            Log("Firebase listeners cleaned up");
        }
        #endregion

        #region Firebase Handlers
        private void OnDogLocationChanged(DataSnapshot snapshot)
        {
            if (!snapshot.Exists) return;

            var currentDeviceId = snapshot.Child("currentDeviceId").Value?.ToString();
            var isMainDevice = snapshot.Child("isMainDevice").Value is bool b && b;

            Log($"Dog location changed - DeviceId: {currentDeviceId}, IsMain: {isMainDevice}");

            // 自分のデバイスに犬が来たかチェック
            bool shouldHaveDog = currentDeviceId == DeviceId ||
                                 (isMainDevice && CurrentRole == DeviceRole.Main);

            if (shouldHaveDog != HasDog && !_isTransferring)
            {
                _ = HandleDogPresenceChange(shouldHaveDog);
            }
        }

        private void OnTransferRequestChanged(DataSnapshot snapshot)
        {
            if (!snapshot.Exists || !snapshot.HasChildren) return;

            var requestingDeviceId = snapshot.Child("requestingDeviceId").Value?.ToString();
            var timestamp = snapshot.Child("timestamp").Value;
            var type = snapshot.Child("type").Value?.ToString();

            if (string.IsNullOrEmpty(requestingDeviceId) || timestamp == null) return;

            // 自分からのリクエストは無視
            if (requestingDeviceId == DeviceId) return;

            // タイムスタンプ検証
            long requestTimestamp = Convert.ToInt64(timestamp);
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (Math.Abs(now - requestTimestamp) > REQUEST_TIMEOUT_SECONDS * 1000) return;

            Log($"Transfer request received - From: {requestingDeviceId}, Type: {type}");

            // 犬を持っている場合、リクエストに応じて転送
            if (HasDog && type == "call")
            {
                _ = HandleCallRequest(requestingDeviceId);
            }
        }

        private async UniTask HandleCallRequest(string targetDeviceId)
        {
            if (_isTransferring) return;

            Log($"Handling call request from: {targetDeviceId}");

            // 犬を退場させる
            await ExitDog();

            // Firebaseを更新
            await UpdateDogLocation(targetDeviceId, false);

            // リクエストをクリア
            await ClearTransferRequest();
        }

        private async UniTask HandleDogPresenceChange(bool shouldHaveDog)
        {
            if (_isTransferring) return;

            if (shouldHaveDog && !HasDog)
            {
                // 犬が来る
                await EnterDog();
            }
            else if (!shouldHaveDog && HasDog)
            {
                // 犬が去る（他デバイスからの呼び出し）
                await ExitDog();
            }
        }
        #endregion

        #region Dog Transfer Animation
        private async UniTask EnterDog()
        {
            _isTransferring = true;
            OnTransferStarted?.Invoke(true);

            Log("Dog entering...");

            if (_transferAnimation != null)
            {
                _transferAnimation.EnterFromLeft();
                // アニメーション完了を待つ
                while (_transferAnimation.IsAnimating)
                {
                    await UniTask.Yield();
                }
            }
            else if (_dogController != null)
            {
                _dogController.SetVisible(true);
            }

            HasDog = true;
            _lastActivityTime = DateTime.UtcNow;
            OnDogPresenceChanged?.Invoke(true);

            _isTransferring = false;
            Log("Dog entered successfully");
        }

        private async UniTask ExitDog()
        {
            _isTransferring = true;
            OnTransferStarted?.Invoke(false);

            Log("Dog exiting...");

            if (_transferAnimation != null)
            {
                _transferAnimation.ExitToRight();
                // アニメーション完了を待つ
                while (_transferAnimation.IsAnimating)
                {
                    await UniTask.Yield();
                }
            }
            else if (_dogController != null)
            {
                _dogController.SetVisible(false);
            }

            HasDog = false;
            OnDogPresenceChanged?.Invoke(false);

            _isTransferring = false;
            Log("Dog exited successfully");
        }
        #endregion

        #region Firebase Updates
        private async UniTask InitializeDogPresence()
        {
            Log($"InitializeDogPresence called. UserId={_userId}, CurrentRole={CurrentRole}");

            if (string.IsNullOrEmpty(_userId))
            {
                LogError("InitializeDogPresence: UserId is null, skipping Firebase update");
                // ローカル状態だけ設定
                if (CurrentRole == DeviceRole.Main)
                {
                    HasDog = true;
                    if (_dogController != null) _dogController.SetVisible(true);
                }
                else
                {
                    HasDog = false;
                    if (_dogController != null) _dogController.SetVisible(false);
                }
                OnDogPresenceChanged?.Invoke(HasDog);
                return;
            }

            try
            {
                if (CurrentRole == DeviceRole.Main)
                {
                    // メイン機は起動時に常に犬を持つ
                    HasDog = true;
                    if (_dogController != null) _dogController.SetVisible(true);
                    await UpdateDogLocationToMain();
                    Log("Main device: dog presence set to true");
                }
                else
                {
                    // サブ機は起動時は犬なし
                    HasDog = false;
                    if (_dogController != null) _dogController.SetVisible(false);
                    Log("Sub device: dog presence set to false");
                }

                OnDogPresenceChanged?.Invoke(HasDog);
                Log($"Initial dog presence: {HasDog}, OnDogPresenceChanged invoked");
            }
            catch (Exception ex)
            {
                LogError($"InitializeDogPresence failed: {ex.Message}");
            }
        }

        private async UniTask UpdateDogLocation(string deviceId, bool isMainDevice)
        {
            try
            {
                var updates = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "currentDeviceId", deviceId },
                    { "isMainDevice", isMainDevice },
                    { "lastActivityTimestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }
                };

                await FirebaseDatabase.DefaultInstance
                    .GetReference(DogLocationPath)
                    .UpdateChildrenAsync(updates);

                Log($"Dog location updated - DeviceId: {deviceId}, IsMain: {isMainDevice}");
            }
            catch (Exception ex)
            {
                LogError($"Failed to update dog location: {ex.Message}");
            }
        }

        private async UniTask UpdateDogLocationToMain()
        {
            // メイン機のデバイスIDを取得するか、isMainDevice=trueに設定
            await UpdateDogLocation(DeviceId, CurrentRole == DeviceRole.Main);
        }

        private async UniTask ClearTransferRequest()
        {
            try
            {
                await FirebaseDatabase.DefaultInstance
                    .GetReference(TransferRequestPath)
                    .RemoveValueAsync();
                Log("Transfer request cleared");
            }
            catch (Exception ex)
            {
                LogError($"Failed to clear transfer request: {ex.Message}");
            }
        }

        private async UniTask UpdateLastActivityAsync()
        {
            try
            {
                await FirebaseDatabase.DefaultInstance
                    .GetReference(LastActivityPath)
                    .SetValueAsync(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            }
            catch (Exception ex)
            {
                LogError($"Failed to update last activity: {ex.Message}");
            }
        }
        #endregion

        #region Timeout Management
        private void StartTimeoutCheck()
        {
            _timeoutCts?.Cancel();
            _timeoutCts = new CancellationTokenSource();
            _ = TimeoutCheckLoop(_timeoutCts.Token);
        }

        private async UniTask TimeoutCheckLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                // デバッグ用タイムアウトが設定されている場合は短い間隔でチェック
                float checkInterval = debugTimeoutSeconds > 0 ? 1f : 60f; // 1秒 or 1分
                await UniTask.Delay(TimeSpan.FromSeconds(checkInterval), cancellationToken: token);

                if (token.IsCancellationRequested) break;

                // サブ機で犬を持っている場合、タイムアウトチェック
                if (CurrentRole == DeviceRole.Sub && HasDog)
                {
                    var elapsed = DateTime.UtcNow - _lastActivityTime;
                    float timeoutSec = debugTimeoutSeconds > 0 ? debugTimeoutSeconds : timeoutMinutes * 60f;

                    if (elapsed.TotalSeconds >= timeoutSec)
                    {
                        string timeoutDesc = debugTimeoutSeconds > 0
                            ? $"{debugTimeoutSeconds} seconds (debug)"
                            : $"{timeoutMinutes} minutes";
                        Log($"Timeout reached ({timeoutDesc}). Returning dog to main device.");
                        await ReturnDogToMain();
                    }
                }
            }
        }
        #endregion

        #region Utilities
        private string GetOrCreateDeviceId()
        {
            var deviceId = PlayerPrefs.GetString(PrefsKeys.DeviceId, "");
            if (string.IsNullOrEmpty(deviceId))
            {
                deviceId = SystemInfo.deviceUniqueIdentifier;
                if (deviceId == SystemInfo.unsupportedIdentifier)
                {
                    deviceId = Guid.NewGuid().ToString();
                }
                PlayerPrefs.SetString(PrefsKeys.DeviceId, deviceId);
                PlayerPrefs.Save();
            }
            return deviceId;
        }

        private void LoadDeviceRole()
        {
            int isSubDevice = PlayerPrefs.GetInt(PrefsKeys.IsSubDevice, 0);
            CurrentRole = isSubDevice == 1 ? DeviceRole.Sub : DeviceRole.Main;
        }

        private void Log(string message)
        {
            if (enableDebugLog)
            {
                GameLogger.Log(LogCategory.Firebase, $"[DogLocationSync] {message}");
            }
        }

        private void LogError(string message)
        {
            GameLogger.LogError(LogCategory.Firebase, $"[DogLocationSync] {message}");
        }
        #endregion

        #region Data Classes
        [Serializable]
        private class TransferRequestData
        {
            public string requestingDeviceId;
            public long timestamp;
            public string type;
        }
        #endregion
    }
}
