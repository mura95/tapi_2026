# マルチプレイヤー同期仕様書（フェーズ2）

## 1. 概要

Photon Fusion 2 を使用したマルチプレイヤー同期機能。最大10人が同じ空間で犬を連れて散歩できる。

---

## 2. Photon Fusion 2 概要

### 2.1 選定理由

| 比較項目 | Photon Fusion 2 | Photon PUN 2 |
|---------|-----------------|--------------|
| ネットワークトポロジ | 分散権限/サーバー権限 | クライアント権限 |
| 同期精度 | 高（Tick-based） | 中 |
| 予測/補間 | 組み込み | 手動実装 |
| 最新技術 | ✓ | レガシー |
| モバイル最適化 | ✓ | ✓ |

### 2.2 Fusion のモード

| モード | 説明 | 使用ケース |
|--------|------|-----------|
| **Shared Mode** | P2Pベースの分散権限 | 本プロジェクト推奨 |
| Host Mode | 1台がホスト | 小規模ゲーム |
| Server Mode | 専用サーバー | 大規模・競技性 |

**本機能では Shared Mode を使用**

---

## 3. システム構成

### 3.1 アーキテクチャ

```
┌─────────────────────────────────────────────────────────┐
│                     Photon Cloud                         │
│  ┌───────────────────────────────────────────────────┐  │
│  │              Fusion Shared Server                  │  │
│  │   ┌─────────────────────────────────────────┐     │  │
│  │   │           Room "WalkRoom_001"           │     │  │
│  │   │  ┌──────┐ ┌──────┐     ┌──────┐        │     │  │
│  │   │  │User A│ │User B│ ... │User J│        │     │  │
│  │   │  └──────┘ └──────┘     └──────┘        │     │  │
│  │   │     (Max 10 players per room)          │     │  │
│  │   └─────────────────────────────────────────┘     │  │
│  └───────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
```

### 3.2 コンポーネント構成

```
Metaverse (Scene)
├── NetworkRunner (Photon)
│   └── PhotonNetworkManager
├── NetworkPlayer (Spawned)
│   ├── NetworkObject
│   ├── NetworkRigidbody (オプション)
│   ├── NetworkPlayerController
│   └── PlayerNameTag
├── NetworkDog (Spawned)
│   ├── NetworkObject
│   ├── NetworkDogController
│   └── NetworkAnimator
└── UI
    └── PlayerList (接続中プレイヤー一覧)
```

---

## 4. セットアップ

### 4.1 パッケージインストール

```
1. Unity Package Manager → Add package from git URL
   URL: https://github.com/photon-fusion-sdk/fusion.git

2. または Photon Dashboard からダウンロード
   https://dashboard.photonengine.com/
```

### 4.2 App ID 設定

```csharp
// PhotonAppSettings.asset
App Id Fusion: [Photon Dashboardから取得]
App Version: 1.0
Fixed Region: jp (日本)
```

### 4.3 サーバーリージョン（重要）

Photon Cloud は世界各地にサーバーがあり、リージョンを指定可能。

#### 利用可能なリージョン

| リージョン | コード | 場所 | 日本からの遅延目安 |
|-----------|--------|------|------------------|
| **Japan** | `jp` | 東京 | **10-30ms** |
| Korea | `kr` | ソウル | 30-50ms |
| Asia | `asia` | シンガポール | 50-80ms |
| US West | `usw` | サンノゼ | 100-150ms |
| US East | `us` | ワシントンDC | 150-200ms |
| EU | `eu` | アムステルダム | 200-250ms |
| South America | `sa` | サンパウロ | 250-300ms |
| Australia | `au` | メルボルン | 100-150ms |

#### 日本向け推奨設定

```csharp
// PhotonAppSettings.asset
Fixed Region: jp    // ← 日本リージョン固定推奨

// または、コードで指定
runner.StartGame(new StartGameArgs {
    // ...
    CustomPhotonAppSettings = new AppSettings {
        FixedRegion = "jp"
    }
});
```

**推奨:**
- 日本国内ユーザーのみ → `jp` リージョン固定で遅延最小化
- 海外ユーザーも含む → 自動選択（Best Region）を使用

#### リージョン選択の影響

| 設定 | メリット | デメリット |
|------|---------|-----------|
| **固定（jp）** | 遅延が安定、低遅延 | 海外からは遅延大 |
| **自動選択** | 各ユーザーに最適 | 国をまたぐルームで遅延差 |

### 4.4 必要なアセット

| アセット | 説明 |
|----------|------|
| PhotonAppSettings | Photon設定 |
| NetworkPlayer Prefab | ネットワークプレイヤー |
| NetworkDog Prefab | ネットワーク犬 |

---

## 5. PhotonNetworkManager

### 5.1 クラス設計

```csharp
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public class PhotonNetworkManager : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("設定")]
    [SerializeField] private NetworkRunner runnerPrefab;
    [SerializeField] private NetworkPrefabRef playerPrefab;
    [SerializeField] private NetworkPrefabRef dogPrefab;
    [SerializeField] private int maxPlayersPerRoom = 10;

    [Header("スポーン")]
    [SerializeField] private Transform[] spawnPoints;

    private NetworkRunner runner;
    private Dictionary<PlayerRef, NetworkObject> spawnedPlayers = new();

    public event Action<PlayerRef> OnPlayerJoined;
    public event Action<PlayerRef> OnPlayerLeft;

    public static PhotonNetworkManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public async UniTask<bool> ConnectToRoom(string roomName = null)
    {
        // NetworkRunnerを生成
        runner = Instantiate(runnerPrefab);
        runner.AddCallbacks(this);

        // ルーム名を生成（指定がなければ自動マッチング）
        string finalRoomName = roomName ?? $"WalkRoom_{UnityEngine.Random.Range(1, 100)}";

        // 接続開始
        var sceneRef = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
        var startGameArgs = new StartGameArgs
        {
            GameMode = GameMode.Shared,
            SessionName = finalRoomName,
            PlayerCount = maxPlayersPerRoom,
            Scene = sceneRef,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        };

        var result = await runner.StartGame(startGameArgs);

        if (result.Ok)
        {
            Debug.Log($"[Photon] Connected to room: {finalRoomName}");
            return true;
        }
        else
        {
            Debug.LogError($"[Photon] Failed to connect: {result.ShutdownReason}");
            return false;
        }
    }

    public void Disconnect()
    {
        if (runner != null)
        {
            runner.Shutdown();
        }
    }

    // INetworkRunnerCallbacks 実装
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsSharedModeMasterClient)
        {
            // プレイヤーをスポーン
            Vector3 spawnPos = GetSpawnPosition(player);
            NetworkObject playerObj = runner.Spawn(playerPrefab, spawnPos, Quaternion.identity, player);
            spawnedPlayers[player] = playerObj;

            // 犬をスポーン
            Vector3 dogSpawnPos = spawnPos - Vector3.forward;
            runner.Spawn(dogPrefab, dogSpawnPos, Quaternion.identity, player);
        }

        OnPlayerJoined?.Invoke(player);
        Debug.Log($"[Photon] Player joined: {player}");
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (spawnedPlayers.TryGetValue(player, out NetworkObject playerObj))
        {
            runner.Despawn(playerObj);
            spawnedPlayers.Remove(player);
        }

        OnPlayerLeft?.Invoke(player);
        Debug.Log($"[Photon] Player left: {player}");
    }

    private Vector3 GetSpawnPosition(PlayerRef player)
    {
        int index = player.PlayerId % spawnPoints.Length;
        return spawnPoints[index].position;
    }

    // その他のコールバック（最小実装）
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
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
}
```

---

## 6. NetworkPlayer

### 6.1 Prefab 構成

```
NetworkPlayer (Prefab)
├── NetworkObject
├── NetworkTransform
├── NetworkPlayerController
├── PlayerModel
│   ├── Mesh
│   └── Animator
├── NameTagCanvas
│   └── PlayerNameTag
└── Collider
```

### 6.2 NetworkPlayerController

```csharp
using Fusion;
using UnityEngine;
using UnityEngine.AI;

public class NetworkPlayerController : NetworkBehaviour
{
    [Header("移動設定")]
    [SerializeField] private float moveSpeed = 3f;

    [Header("同期データ")]
    [Networked] public Vector3 TargetPosition { get; set; }
    [Networked] public NetworkString<_32> PlayerName { get; set; }

    private NavMeshAgent agent;
    private Camera mainCamera;

    public override void Spawned()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
        mainCamera = Camera.main;

        if (HasInputAuthority)
        {
            // ローカルプレイヤーの初期化
            string userName = PlayerPrefs.GetString("UserName", "Guest");
            PlayerName = userName;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (HasInputAuthority)
        {
            HandleInput();
        }

        // すべてのクライアントで移動を適用
        if (agent != null && TargetPosition != Vector3.zero)
        {
            agent.SetDestination(TargetPosition);
        }
    }

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                if (NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, 1f, NavMesh.AllAreas))
                {
                    TargetPosition = navHit.position;
                }
            }
        }
    }
}
```

---

## 7. NetworkDog

### 7.1 Prefab 構成

```
NetworkDog (Prefab)
├── NetworkObject
├── NetworkTransform
├── NetworkDogController
├── DogModel
│   ├── Mesh
│   └── Animator
├── NetworkAnimator
└── NavMeshAgent
```

### 7.2 NetworkDogController

```csharp
using Fusion;
using UnityEngine;
using UnityEngine.AI;

public class NetworkDogController : NetworkBehaviour
{
    [Header("追従設定")]
    [SerializeField] private float followDistance = 1.5f;
    [SerializeField] private float stopDistance = 1f;
    [SerializeField] private float speed = 2.5f;

    [Header("同期データ")]
    [Networked] public NetworkBehaviourId OwnerId { get; set; }

    private NavMeshAgent agent;
    private NetworkPlayerController owner;

    public override void Spawned()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = speed;
    }

    public void SetOwner(NetworkPlayerController playerController)
    {
        owner = playerController;
        OwnerId = playerController.Id;
    }

    public override void FixedUpdateNetwork()
    {
        if (owner == null) return;

        float distance = Vector3.Distance(transform.position, owner.transform.position);

        if (distance > followDistance)
        {
            Vector3 followPos = owner.transform.position - owner.transform.forward * followDistance;
            if (NavMesh.SamplePosition(followPos, out NavMeshHit navHit, 2f, NavMesh.AllAreas))
            {
                agent.SetDestination(navHit.position);
            }
        }
        else if (distance < stopDistance)
        {
            agent.ResetPath();
        }
    }
}
```

---

## 8. ルーム管理

### 8.1 自動マッチング

```csharp
public async UniTask<bool> AutoJoinRoom()
{
    // 空きのあるルームを検索
    var sessionList = await GetAvailableRooms();

    foreach (var session in sessionList)
    {
        if (session.PlayerCount < maxPlayersPerRoom)
        {
            return await ConnectToRoom(session.Name);
        }
    }

    // 空きがなければ新規ルーム作成
    return await ConnectToRoom();
}

private async UniTask<List<SessionInfo>> GetAvailableRooms()
{
    // Photon Lobby経由でセッション一覧を取得
    // ...
    return new List<SessionInfo>();
}
```

### 8.2 ルーム設定

| 設定 | 値 | 説明 |
|------|-----|------|
| Max Players | 10 | 最大プレイヤー数 |
| Is Visible | true | ロビーに表示 |
| Is Open | true | 参加可能 |
| Room TTL | 300秒 | 空になってからの保持時間 |

---

## 9. 同期データ

### 9.1 プレイヤー同期データ

| データ | 型 | 同期タイミング |
|--------|-----|----------------|
| Position | Vector3 | Every Tick |
| Rotation | Quaternion | Every Tick |
| TargetPosition | Vector3 | On Change |
| PlayerName | string | On Spawn |
| AnimationState | int | On Change |

### 9.2 犬同期データ

| データ | 型 | 同期タイミング |
|--------|-----|----------------|
| Position | Vector3 | Every Tick |
| Rotation | Quaternion | Every Tick |
| OwnerId | NetworkBehaviourId | On Spawn |
| AnimationState | int | On Change |

---

## 10. プレイヤー名タグUI

### 10.1 PlayerNameTag

```csharp
using Fusion;
using UnityEngine;
using TMPro;

public class PlayerNameTag : NetworkBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Canvas canvas;

    private Camera mainCamera;
    private NetworkPlayerController playerController;

    public override void Spawned()
    {
        mainCamera = Camera.main;
        playerController = GetComponentInParent<NetworkPlayerController>();

        // ローカルプレイヤーは名前を非表示（オプション）
        if (HasInputAuthority)
        {
            canvas.gameObject.SetActive(false);
        }
    }

    private void LateUpdate()
    {
        if (mainCamera == null) return;

        // カメラの方を向く
        canvas.transform.LookAt(canvas.transform.position + mainCamera.transform.forward);

        // 名前を更新
        if (playerController != null)
        {
            nameText.text = playerController.PlayerName.ToString();
        }
    }
}
```

### 10.2 名前タグ仕様

| 項目 | 仕様 |
|------|------|
| 位置 | プレイヤーの頭上 (Y + 2m) |
| サイズ | 動的（距離に応じて） |
| フォント | 24sp、白、黒縁 |
| 表示条件 | 他プレイヤーのみ |

---

## 11. 接続フロー

```
┌──────────────┐
│ シーン遷移    │
└──────┬───────┘
       │
       ↓
┌──────────────┐
│ NetworkRunner │
│ 初期化        │
└──────┬───────┘
       │
       ↓
┌──────────────┐
│ ルーム検索/   │
│ 作成          │
└──────┬───────┘
       │
       ↓
┌──────────────┐
│ 接続完了      │
│ OnPlayerJoined│
└──────┬───────┘
       │
       ↓
┌──────────────┐
│ Player/Dog   │
│ スポーン      │
└──────────────┘
```

---

## 12. エラーハンドリング

### 12.1 接続エラー

```csharp
public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
{
    switch (reason)
    {
        case NetConnectFailedReason.Timeout:
            ShowError("接続がタイムアウトしました");
            break;
        case NetConnectFailedReason.ServerFull:
            ShowError("ルームが満員です");
            break;
        default:
            ShowError($"接続に失敗しました: {reason}");
            break;
    }

    // メインシーンに戻る
    ReturnToMainScene().Forget();
}
```

### 12.2 切断処理

```csharp
public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
{
    Debug.Log($"[Photon] Shutdown: {shutdownReason}");

    if (shutdownReason != ShutdownReason.Ok)
    {
        ShowError("接続が切断されました");
        ReturnToMainScene().Forget();
    }
}
```

---

## 13. パフォーマンス最適化

### 13.1 Interest Management（AOI）

```csharp
// 遠くのプレイヤーは更新頻度を下げる
[Networked, Accuracy(0.01f)]
public Vector3 Position { get; set; }
```

### 13.2 帯域幅最適化

| 技術 | 説明 |
|------|------|
| Delta Compression | 変更差分のみ送信 |
| Quantization | 精度を下げて圧縮 |
| Tick Rate調整 | 30 tick/sec で十分 |

### 13.3 推奨設定

| 設定 | 値 |
|------|-----|
| Tick Rate | 30 |
| Send Rate | 15-30 |
| Position Accuracy | 0.01 |
| Rotation Accuracy | 1.0 |

---

## 14. テストケース

| # | テスト内容 | 期待結果 |
|---|-----------|---------|
| 1 | 2人同時接続 | 両者が互いを確認できる |
| 2 | 10人同時接続 | 全員が同じ空間にいる |
| 3 | 移動同期 | 他プレイヤーの移動が滑らかに見える |
| 4 | 犬の追従 | 各プレイヤーの犬が正しく追従 |
| 5 | 名前表示 | 他プレイヤーの名前が表示される |
| 6 | 切断時 | 切断したプレイヤーが消える |
| 7 | 再接続 | 再接続後に正しくスポーン |

---

## 15. 料金・制限

### 課金モデル（重要）

Photon は **CCU（同時接続数）ベースの課金** であり、**接続時間は課金に影響しない**。

| 要素 | 課金への影響 |
|------|------------|
| **同時接続数（CCU）** | 直接影響（プラン上限で判定） |
| **接続時間** | **影響なし**（1分でも1時間でも同じ） |
| **メッセージ量** | 60GB/月超過で追加課金 |

```
例: 100 CCUプラン
├── 10時に80人接続 → OK
├── 11時に50人接続 → OK
├── 10時に120人接続 → NG（超過、新規接続拒否）
└── 各ユーザーが30分接続 → 時間は課金に影響なし
```

### Photon Fusion 無料枠

| 項目 | 無料枠 |
|------|--------|
| CCU (同時接続) | 100 |
| Messages/月 | 60GB |
| Room数 | 無制限 |

### 有料プラン

| プラン | CCU | 月額 |
|--------|-----|------|
| Plus | 500 | $95 |
| Pro | 1000 | $175 |
| Enterprise | カスタム | 要相談 |

### CCU超過時の挙動

| プラン | CCU超過時 |
|--------|----------|
| Free | 新規接続が拒否される（既存接続は維持） |
| 有料 | バースト課金（追加CCU自動購入）または拒否を選択可 |

---

## 16. 関連ファイル

| ファイル | 役割 |
|----------|------|
| `PhotonNetworkManager.cs` | 接続・ルーム管理（新規作成） |
| `NetworkPlayerController.cs` | ネットワークプレイヤー（新規作成） |
| `NetworkDogController.cs` | ネットワーク犬（新規作成） |
| `PlayerNameTag.cs` | 名前タグUI（新規作成） |

---

## 17. 参考リンク

- [Photon Fusion 2 公式ドキュメント](https://doc.photonengine.com/fusion/)
- [Photon Dashboard](https://dashboard.photonengine.com/)
- [Fusion サンプルプロジェクト](https://doc.photonengine.com/fusion/current/samples/)
