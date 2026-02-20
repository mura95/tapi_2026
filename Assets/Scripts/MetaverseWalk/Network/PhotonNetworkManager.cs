using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TapHouse.MetaverseWalk.Core;

namespace TapHouse.MetaverseWalk.Network
{
    /// <summary>
    /// Photon Fusion 2の接続・ルーム管理・プレイヤースポーンを担当
    /// Shared Modeで動作し、各クライアントが自身のエンティティを管理する
    ///
    /// [変更履歴]
    /// - P1-3: バックグラウンド復帰処理追加（OnApplicationPause）
    /// - P1-4: 再接続ロジック強化（指数バックオフ、Rejoin、エラーハンドラー統合）
    /// - P2-2: 接続タイムアウト処理追加
    /// </summary>
    public class PhotonNetworkManager : MonoBehaviour, INetworkRunnerCallbacks
    {
        [Header("Photon設定")]
        [SerializeField] private NetworkRunner runnerPrefab;
        [SerializeField] private NetworkPrefabRef networkDogPrefab;
        [SerializeField] private NetworkPrefabRef networkPlayerPrefab;
        [SerializeField] private int maxPlayersPerRoom = NetworkConstants.MAX_PLAYERS_PER_ROOM;

        [Header("スポーン")]
        [SerializeField] private Transform[] spawnPoints;

        [Header("デバッグ")]
        [SerializeField] private bool debugMode = false;

        private NetworkRunner runner;
        private readonly Dictionary<PlayerRef, SpawnedEntities> spawnedEntities = new();
        private bool isDisconnecting = false;
        private bool isReconnecting = false;
        private bool isConnecting = false;

        // バックグラウンド復帰用
        private float pausedAtTime;
        private string lastRoomName;
        private bool hasEverConnected = false;

        public static PhotonNetworkManager Instance { get; private set; }

        public bool IsConnected => runner != null && runner.IsRunning;
        public int PlayerCount => runner != null && runner.IsRunning ? runner.SessionInfo.PlayerCount : 0;
        public string RoomName => runner != null && runner.IsRunning ? runner.SessionInfo.Name : null;
        public bool IsReconnecting => isReconnecting;

        // イベント
        public event Action<int> OnPlayerCountChanged;
        public event Action<bool, string> OnConnectionStatusChanged;

        // デバッグ用統計
        public int DebugReconnectAttempts { get; private set; }
        public float DebugLastPauseDuration { get; private set; }

        private struct SpawnedEntities
        {
            public NetworkObject Player;
            public NetworkObject Dog;
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        #region 接続・切断

        /// <summary>
        /// Photonルームに接続（自動マッチング）
        /// 時間バケット+連番方式で同じ時間帯のプレイヤーを同じルームにマッチング
        /// 満員の場合は次の連番ルームを試行（最大MAX_ROOM_JOIN_ATTEMPTS回）
        /// </summary>
        public async UniTask<bool> ConnectToRoomAsync(string roomName = null, CancellationToken ct = default)
        {
            if (IsConnected)
            {
                Log("Already connected");
                return true;
            }

            // 同時接続試行を防止
            if (isConnecting)
            {
                Log("Connection already in progress, skipping");
                return false;
            }

            // ネットワーク到達可能性チェック
            if (!MetaverseErrorHandler.IsNetworkAvailable())
            {
                MetaverseErrorHandler.RaiseError(MetaverseErrorCode.E001_NoNetwork);
                return false;
            }

            isConnecting = true;
            OnConnectionStatusChanged?.Invoke(false, "接続中...");

            try
            {
                // 指定ルーム名がある場合はそのまま試行
                if (roomName != null)
                {
                    return await TryJoinRoomWithTimeout(roomName, ct);
                }

                // 自動マッチング: 時間バケットベースのルーム名を順番に試行
                string timeBucket = GetTimeBucket();
                for (int i = 0; i < NetworkConstants.MAX_ROOM_JOIN_ATTEMPTS; i++)
                {
                    ct.ThrowIfCancellationRequested();

                    string candidateRoom = $"{NetworkConstants.ROOM_NAME_PREFIX}{timeBucket}_{i}";
                    Log($"Trying room: {candidateRoom} (attempt {i + 1}/{NetworkConstants.MAX_ROOM_JOIN_ATTEMPTS})");

                    bool success = await TryJoinRoomWithTimeout(candidateRoom, ct);
                    if (success)
                    {
                        lastRoomName = candidateRoom;
                        return true;
                    }

                    Log($"Room {candidateRoom} failed, trying next...");
                }

                MetaverseErrorHandler.RaiseError(MetaverseErrorCode.E002_RoomSearchTimeout);
                return false;
            }
            finally
            {
                isConnecting = false;
            }
        }

        /// <summary>
        /// タイムアウト付きルーム接続
        /// </summary>
        private async UniTask<bool> TryJoinRoomWithTimeout(string roomName, CancellationToken ct)
        {
            using var timeoutCts = new CancellationTokenSource(NetworkConstants.CONNECTION_TIMEOUT_MS);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

            try
            {
                return await TryJoinRoom(roomName, linkedCts.Token);
            }
            catch (OperationCanceledException)
            {
                if (timeoutCts.IsCancellationRequested)
                {
                    Log($"Connection timeout for room: {roomName}");
                    CleanupRunner();
                }
                return false;
            }
        }

        /// <summary>
        /// 指定ルーム名への接続を試行
        /// </summary>
        private async UniTask<bool> TryJoinRoom(string roomName, CancellationToken ct = default)
        {
            // 既存のrunnerがあればクリーンアップ
            CleanupRunner();

            runner = Instantiate(runnerPrefab);
            runner.AddCallbacks(this);

            // NetworkSceneManagerDefaultはrunnerのGameObjectに配置
            // （複数runner間でのコンポーネント共有を防止）
            var sceneManager = runner.gameObject.GetComponent<NetworkSceneManagerDefault>()
                               ?? runner.gameObject.AddComponent<NetworkSceneManagerDefault>();

            var sceneRef = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
            var startGameArgs = new StartGameArgs
            {
                GameMode = GameMode.Shared,
                SessionName = roomName,
                PlayerCount = maxPlayersPerRoom,
                Scene = sceneRef,
                SceneManager = sceneManager,
            };

            var result = await runner.StartGame(startGameArgs);

            if (result.Ok)
            {
                Log($"Connected to room: {roomName}");
                hasEverConnected = true;
                MetaverseErrorHandler.ClearError();
                OnConnectionStatusChanged?.Invoke(true, "接続中");
                return true;
            }
            else
            {
                Log($"Failed to join room {roomName}: {result.ShutdownReason}");
                CleanupRunner();
                return false;
            }
        }

        /// <summary>
        /// UTC基準の時間バケット文字列を生成
        /// 例: "20260217_1030"（30分単位）
        /// </summary>
        private string GetTimeBucket()
        {
            var utcNow = DateTime.UtcNow;
            int bucketMinute = (utcNow.Minute / NetworkConstants.ROOM_TIME_BUCKET_MINUTES)
                               * NetworkConstants.ROOM_TIME_BUCKET_MINUTES;
            return $"{utcNow:yyyyMMdd}_{utcNow.Hour:D2}{bucketMinute:D2}";
        }

        /// <summary>
        /// リトライ付き接続（指数バックオフ）
        /// 初回: 即接続、リトライ: 2秒→4秒→8秒の待機
        /// </summary>
        public async UniTask<bool> ConnectWithRetryAsync(string roomName = null, CancellationToken ct = default)
        {
            isReconnecting = false;

            for (int i = 0; i <= NetworkConstants.MAX_RECONNECT_ATTEMPTS; i++)
            {
                ct.ThrowIfCancellationRequested();

                if (i > 0)
                {
                    isReconnecting = true;
                    DebugReconnectAttempts = i;

                    // 指数バックオフ: 2秒 → 4秒 → 8秒
                    int delayMs = NetworkConstants.RECONNECT_BASE_INTERVAL_MS * (1 << (i - 1));
                    Log($"Reconnect attempt {i}/{NetworkConstants.MAX_RECONNECT_ATTEMPTS} (waiting {delayMs}ms)");
                    OnConnectionStatusChanged?.Invoke(false, $"つなぎなおしています... ({i}/{NetworkConstants.MAX_RECONNECT_ATTEMPTS})");

                    await UniTask.Delay(delayMs, cancellationToken: ct);
                }

                bool success = await ConnectToRoomAsync(roomName, ct);
                if (success)
                {
                    isReconnecting = false;
                    DebugReconnectAttempts = 0;
                    return true;
                }
            }

            isReconnecting = false;
            MetaverseErrorHandler.RaiseError(MetaverseErrorCode.E008_ReconnectFailed);
            return false;
        }

        /// <summary>
        /// 切断
        /// </summary>
        public void Disconnect()
        {
            if (isDisconnecting) return;
            isDisconnecting = true;

            Log("Disconnecting...");

            if (runner != null)
            {
                runner.Shutdown();
            }
        }

        private void CleanupRunner()
        {
            if (runner != null)
            {
                Destroy(runner.gameObject);
                runner = null;
            }
        }

        #endregion

        #region バックグラウンド復帰 (P1-3)

        /// <summary>
        /// アプリがバックグラウンドに行った / 復帰した時の処理
        /// Android: Doze Mode等でネットワーク切断される場合がある
        /// </summary>
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                // バックグラウンドに入った
                pausedAtTime = Time.realtimeSinceStartup;
                lastRoomName = RoomName;
                Log($"App paused. Room: {lastRoomName}");
            }
            else
            {
                // 一度も接続していない場合は無視
                // （Play Mode開始時のドメインリロードによる誤検知を防止）
                if (!hasEverConnected)
                {
                    Log("App resumed but never connected yet, ignoring (initial startup)");
                    return;
                }

                // フォアグラウンドに復帰
                float pauseDuration = Time.realtimeSinceStartup - pausedAtTime;
                DebugLastPauseDuration = pauseDuration;
                Log($"App resumed. Pause duration: {pauseDuration:F1}s");

                HandleBackgroundReturn(pauseDuration).Forget();
            }
        }

        /// <summary>
        /// バックグラウンド復帰時の処理
        /// 閾値以内: 再接続試行、閾値超過: メインシーンへ戻る
        /// </summary>
        private async UniTaskVoid HandleBackgroundReturn(float pauseDurationSec)
        {
            if (pauseDurationSec > NetworkConstants.BACKGROUND_REJOIN_THRESHOLD_SEC)
            {
                // 長時間離脱 → 散歩終了
                Log($"Background timeout ({pauseDurationSec:F1}s > {NetworkConstants.BACKGROUND_REJOIN_THRESHOLD_SEC}s)");
                MetaverseErrorHandler.RaiseError(
                    MetaverseErrorCode.E007_BackgroundTimeout,
                    $"Paused for {pauseDurationSec:F1}s");
                return;
            }

            // 短時間離脱 → 接続が切れていたら再接続を試みる
            if (!IsConnected && !isReconnecting)
            {
                Log($"Connection lost during background ({pauseDurationSec:F1}s). Attempting rejoin...");
                OnConnectionStatusChanged?.Invoke(false, "つなぎなおしています...");

                // 元のルーム名があればそこにRejoin試行
                string targetRoom = lastRoomName;
                bool success = await ConnectWithRetryAsync(targetRoom);

                if (!success)
                {
                    // 再接続失敗 → エラーはConnectWithRetryAsync内で発行済み
                    Log("Rejoin failed after background return");
                }
            }
            else
            {
                Log("Still connected after background return");
            }
        }

        #endregion

        #region スポーン

        private Vector3 GetSpawnPosition(PlayerRef player)
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                return Vector3.zero;
            }
            int index = player.PlayerId % spawnPoints.Length;
            return spawnPoints[index].position;
        }

        private Quaternion GetSpawnRotation(PlayerRef player)
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                return Quaternion.identity;
            }
            int index = player.PlayerId % spawnPoints.Length;
            return spawnPoints[index].rotation;
        }

        private void SpawnLocalEntities(NetworkRunner activeRunner, PlayerRef player)
        {
            Vector3 spawnPos = GetSpawnPosition(player);
            Quaternion spawnRot = GetSpawnRotation(player);

            // NavMeshにスナップ（地下スポーン防止）
            if (NavMesh.SamplePosition(spawnPos, out NavMeshHit navHit, 5f, NavMesh.AllAreas))
            {
                spawnPos = navHit.position;
            }

            // プレイヤーをスポーン（操作対象、基準位置に配置）
            NetworkObject playerObj = activeRunner.Spawn(
                networkPlayerPrefab,
                spawnPos,
                spawnRot,
                player
            );

            // 犬をスポーン（追従役、プレイヤーの前方に配置）
            Vector3 dogSpawnPos = spawnPos + spawnRot * Vector3.forward * MetaverseConstants.SPAWN_OFFSET_FORWARD;
            NetworkObject dogObj = activeRunner.Spawn(
                networkDogPrefab,
                dogSpawnPos,
                spawnRot,
                player
            );

            spawnedEntities[player] = new SpawnedEntities
            {
                Player = playerObj,
                Dog = dogObj
            };

            // NetworkSpawnManagerに初期設定を委譲
            var spawnManager = FindFirstObjectByType<NetworkSpawnManager>();
            if (spawnManager != null)
            {
                spawnManager.SetupLocalEntities(dogObj, playerObj);
            }
            else
            {
                Debug.LogError("[PhotonNetworkManager] NetworkSpawnManager not found! UI will not be linked.");
            }

            Debug.Log($"[PhotonNetworkManager] Spawned local entities at {spawnPos}");
        }

        #endregion

        #region ログ

        private void Log(string message)
        {
            // テスト段階: 常にログ出力（debugMode関係なく）
            // TODO: プロダクションではdebugModeで制御
            Debug.Log($"[PhotonNetworkManager] {message}");
        }

        #endregion

        #region INetworkRunnerCallbacks

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            Log($"Player joined: {player}");

            // ローカルプレイヤーの場合、自分のエンティティをスポーン
            // コールバック引数のrunnerを使用（this.runnerとの不一致を防止）
            if (player == runner.LocalPlayer)
            {
                SpawnLocalEntities(runner, player);
            }

            OnPlayerCountChanged?.Invoke(runner.SessionInfo.PlayerCount);
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            Log($"Player left: {player}");

            // 自分のエンティティの場合はDespawn
            if (spawnedEntities.TryGetValue(player, out var entities))
            {
                if (entities.Dog != null) runner.Despawn(entities.Dog);
                if (entities.Player != null) runner.Despawn(entities.Player);
                spawnedEntities.Remove(player);
            }

            OnPlayerCountChanged?.Invoke(runner.SessionInfo.PlayerCount);
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            Log($"Shutdown: {shutdownReason}");
            spawnedEntities.Clear();
            CleanupRunner();
            isDisconnecting = false;

            if (shutdownReason != ShutdownReason.Ok)
            {
                // ShutdownReasonをエラーコードに変換して発行
                var errorCode = MetaverseErrorHandler.FromShutdownReason(shutdownReason);
                if (errorCode != MetaverseErrorCode.None)
                {
                    MetaverseErrorHandler.RaiseError(errorCode, $"ShutdownReason: {shutdownReason}");
                }
                OnConnectionStatusChanged?.Invoke(false, "切断されました");
            }
        }

        public void OnConnectedToServer(NetworkRunner runner)
        {
            Log("Connected to server");
            OnConnectionStatusChanged?.Invoke(true, "接続中");
        }

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            Log($"Disconnected: {reason}");
            OnConnectionStatusChanged?.Invoke(false, "切断されました");

            // 意図的な切断でなければ再接続を試みる
            if (!isDisconnecting && !isReconnecting)
            {
                MetaverseErrorHandler.RaiseError(
                    MetaverseErrorCode.E004_Disconnected,
                    $"NetDisconnectReason: {reason}");
            }
        }

        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
            var errorCode = MetaverseErrorHandler.FromConnectFailedReason(reason);
            Log($"Connect failed: {reason} -> {errorCode}");
            MetaverseErrorHandler.RaiseError(errorCode, $"ConnectFailed: {reason}");
        }

        // 必須コールバック（最小実装）
        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

        #endregion

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
