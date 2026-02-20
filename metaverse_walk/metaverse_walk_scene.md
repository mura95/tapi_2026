# メタバースシーン設計仕様書

## 1. 概要

メタバース散歩機能のメインシーン。公園風の3Dマップ上で**犬が先導し、プレイヤーが後ろからついていく**シングルプレイヤー体験（フェーズ1）。

**重要:** 犬が前、プレイヤーが後ろの追従関係。ユーザーは犬をタップ操作で誘導し、プレイヤー（自分）は自動で犬について行く。

---

## 2. シーン構成

### 2.1 ヒエラルキー

```
Metaverse (Scene)
├── --- Managers ---
│   ├── MetaverseManager
│   ├── WalkScheduler (DontDestroyOnLoad参照)
│   └── AudioManager
│
├── --- Environment ---
│   ├── Terrain
│   │   ├── Ground
│   │   ├── Paths
│   │   └── NavMeshSurface
│   ├── Props
│   │   ├── Trees
│   │   ├── Benches
│   │   ├── Flowers
│   │   └── Lamps
│   └── Lighting
│       ├── Directional Light
│       └── Ambient Settings
│
├── --- Characters ---
│   ├── Dog (先導役)
│   │   ├── DogModel (既存犬モデル)
│   │   ├── NavMeshAgent
│   │   └── MetaverseDogController (タッチで移動指示)
│   └── Player (追従役)
│       ├── PlayerModel (カプセル/アバター)
│       ├── NavMeshAgent
│       └── MetaversePlayerFollower (犬を追従)
│
├── --- Camera ---
│   ├── MetaverseCamera
│   │   ├── IsometricCameraController
│   │   └── CameraTarget (犬追従、プレイヤーも画面内)
│   └── UICamera
│
└── --- UI ---
    └── MetaverseCanvas
        ├── ExitButton (「散歩をやめる」)
        ├── Timer (オプション: 30分タイマー)
        └── MuteButton (フェーズ3用)
```

---

## 3. カメラ設定

### 3.1 アイソメトリック（俯瞰）カメラ

```csharp
public class MetaverseCamera : MonoBehaviour
{
    [Header("ターゲット")]
    [SerializeField] private Transform dog;      // 犬（メインターゲット）
    [SerializeField] private Transform player;   // プレイヤー（サブターゲット）

    [Header("カメラ設定")]
    [SerializeField] private float distance = 15f;       // カメラ距離
    [SerializeField] private float height = 10f;         // カメラ高さ
    [SerializeField] private float angle = 45f;          // 俯角（度）
    [SerializeField] private float rotationAngle = 45f;  // Y軸回転（度）

    [Header("追従設定")]
    [SerializeField] private float smoothSpeed = 5f;     // 追従スムーズさ

    private Vector3 offset;

    private void Start()
    {
        CalculateOffset();
    }

    private void CalculateOffset()
    {
        // アイソメトリック視点のオフセット計算
        float radAngle = rotationAngle * Mathf.Deg2Rad;
        float radPitch = angle * Mathf.Deg2Rad;

        offset = new Vector3(
            distance * Mathf.Sin(radAngle) * Mathf.Cos(radPitch),
            height,
            -distance * Mathf.Cos(radAngle) * Mathf.Cos(radPitch)
        );
    }

    private void LateUpdate()
    {
        if (dog == null) return;

        // 犬とプレイヤーの中間点をターゲットに
        Vector3 centerPoint = dog.position;
        if (player != null)
        {
            centerPoint = (dog.position + player.position) / 2f;
        }

        Vector3 desiredPosition = centerPoint + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.LookAt(centerPoint);
    }
}
```

### 3.2 カメラパラメータ

| パラメータ | 値 | 説明 |
|------------|-----|------|
| distance | 15 | カメラ距離 |
| height | 10 | カメラ高さ |
| angle | 45° | 俯角 |
| rotationAngle | 45° | Y軸回転（アイソメトリック） |
| smoothSpeed | 5 | 追従スムーズさ |
| Field of View | 35° | 遠近感を抑える |

### 3.3 ビジュアルイメージ

```
        カメラ位置
           ↙
          📷
           ＼
            ＼  45°
             ＼
              ↘
            🐕 ← 犬（前、先導）
             ↑
            🧑‍🦳 ← プレイヤー（後ろ、追従）
           ／    ＼
         ／        ＼
       ／  公園風景  ＼
     ─────────────────────
```

---

## 4. 犬の移動操作（先導役）

**重要:** ユーザーは画面をタップして**犬**を誘導する。プレイヤーは自動で犬について行く。

### 4.1 MetaverseDogController

```csharp
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 犬の移動を制御するコントローラー
/// ユーザーのタッチ入力を受けて犬を移動させる（先導役）
/// </summary>
public class MetaverseDogController : MonoBehaviour
{
    [Header("移動設定")]
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("入力設定")]
    [SerializeField] private LayerMask groundLayer;

    [Header("アニメーション")]
    [SerializeField] private Animator animator;
    private static readonly int SpeedParam = Animator.StringToHash("Speed");
    private static readonly int IsWalkingParam = Animator.StringToHash("IsWalking");

    private NavMeshAgent agent;
    private Camera mainCamera;
    private Vector3 targetPosition;
    private bool isMoving = false;

    public bool IsMoving => isMoving;
    public Vector3 TargetPosition => targetPosition;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
        mainCamera = Camera.main;
        targetPosition = transform.position;
    }

    private void Update()
    {
        HandleTouchInput();
        UpdateMovement();
        UpdateAnimation();
    }

    private void HandleTouchInput()
    {
        // タッチ入力（またはマウス）で犬を誘導
        if (Input.GetMouseButtonDown(0))
        {
            // UI上のタップは無視
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                return;

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
            {
                // NavMesh上の有効な位置かチェック
                if (NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, 1f, NavMesh.AllAreas))
                {
                    targetPosition = navHit.position;
                    agent.SetDestination(targetPosition);
                    isMoving = true;

                    // タップエフェクト表示（オプション）
                    // ShowTapEffect(hit.point);
                }
            }
        }
    }

    private void UpdateMovement()
    {
        // 目的地に到着したらisMovingをfalseに
        if (isMoving && !agent.pathPending)
        {
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                isMoving = false;
            }
        }

        // 移動方向に回転
        if (agent.velocity.sqrMagnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(agent.velocity.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void UpdateAnimation()
    {
        float speed = agent.velocity.magnitude;
        if (animator != null)
        {
            animator.SetFloat(SpeedParam, speed);
            animator.SetBool(IsWalkingParam, speed > 0.1f);
        }
    }
}
```

### 4.2 操作方法

| 操作 | 動作 |
|------|------|
| タップ | タップした地点へ**犬**が移動 |
| 長押し＋ドラッグ | 連続移動（オプション） |
| ピンチ | ズームイン/アウト（オプション） |

**ポイント:** プレイヤーを直接操作するのではなく、犬を誘導する。本物の散歩のように犬が前を歩く。

### 4.3 移動パラメータ

| パラメータ | 値 | 説明 |
|------------|-----|------|
| moveSpeed | 2.5 m/s | 犬の歩行速度 |
| rotationSpeed | 10 | 回転スムーズさ |
| stoppingDistance | 0.1m | 停止距離 |

---

## 5. プレイヤーの追従ロジック（追従役）

**重要:** プレイヤーは犬の後ろを自動で追従する。

### 5.1 MetaversePlayerFollower

```csharp
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// プレイヤーの追従を制御するコントローラー
/// 犬の後ろを自動で追従する（追従役）
/// </summary>
public class MetaversePlayerFollower : MonoBehaviour
{
    [Header("追従設定")]
    [SerializeField] private Transform dog;               // 犬（追従対象）
    [SerializeField] private float followDistance = 1.5f; // 犬との距離
    [SerializeField] private float stopDistance = 1f;     // 停止距離
    [SerializeField] private float catchUpSpeed = 4f;     // 追いつき速度
    [SerializeField] private float normalSpeed = 3f;      // 通常速度

    [Header("アニメーション")]
    [SerializeField] private Animator animator;
    private static readonly int SpeedParam = Animator.StringToHash("Speed");
    private static readonly int IsWalkingParam = Animator.StringToHash("IsWalking");

    private NavMeshAgent agent;
    private MetaverseDogController dogController;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = normalSpeed;

        if (dog != null)
        {
            dogController = dog.GetComponent<MetaverseDogController>();
        }
    }

    private void Update()
    {
        if (dog == null) return;

        FollowDog();
        UpdateAnimation();
    }

    private void FollowDog()
    {
        float distanceToDog = Vector3.Distance(transform.position, dog.position);

        if (distanceToDog > followDistance)
        {
            // 犬の後方に追従位置を計算
            Vector3 followPosition = GetFollowPosition();

            // NavMeshで有効な位置にサンプリング
            if (NavMesh.SamplePosition(followPosition, out NavMeshHit navHit, 2f, NavMesh.AllAreas))
            {
                agent.SetDestination(navHit.position);
            }

            // 距離が離れすぎたら速度アップ（犬に追いつく）
            if (distanceToDog > followDistance * 2)
            {
                agent.speed = catchUpSpeed;
            }
            else
            {
                agent.speed = normalSpeed;
            }
        }
        else if (distanceToDog < stopDistance)
        {
            // 犬に近すぎたら停止
            agent.ResetPath();
        }
    }

    private Vector3 GetFollowPosition()
    {
        // 犬の後方位置（リードを持っているイメージ）
        Vector3 dogForward = dog.forward;

        // 犬の真後ろに配置
        Vector3 offset = -dogForward * followDistance;
        return dog.position + offset;
    }

    private void UpdateAnimation()
    {
        float speed = agent.velocity.magnitude;

        if (animator != null)
        {
            animator.SetFloat(SpeedParam, speed);
            animator.SetBool(IsWalkingParam, speed > 0.1f);
        }
    }
}
```

### 5.2 追従パラメータ

| パラメータ | 値 | 説明 |
|------------|-----|------|
| followDistance | 1.5m | 犬との追従距離 |
| stopDistance | 1m | 停止距離 |
| normalSpeed | 3 m/s | 通常速度（犬より少し速い） |
| catchUpSpeed | 4 m/s | 追いつき速度 |

### 5.3 追従位置イメージ

```
    移動方向
       ↑
       │
       🐕 ← 犬（前、先導）
       │
       │  リード（見えない）
       │
       🧑‍🦳 ← プレイヤー（後ろ、追従）
```

### 5.4 なぜ犬が前なのか？

| 理由 | 説明 |
|------|------|
| **自然な散歩体験** | 実際の犬の散歩では、犬が前を歩くことが多い |
| **犬中心のUX** | Tappuは犬がメイン。犬を見ながら散歩する体験 |
| **操作の直感性** | 「犬を誘導する」という操作が自然 |
| **視認性** | カメラから犬がよく見える位置に |

---

## 6. UI デザイン

### 6.1 「散歩をやめる」ボタン

```
┌─────────────────────────────────────────┐
│ ╔══════════════╗                        │
│ ║ 散歩をやめる ║  [🎤]                  │  ← 左上
│ ╚══════════════╝                        │
│                                         │
│               🐕 ← 犬（前）             │
│               ↑                         │
│               🧑‍🦳 ← プレイヤー（後ろ）   │
│                                         │
│           ～ 公園風景 ～                │
│                                         │
└─────────────────────────────────────────┘
```

### 6.2 ボタン仕様

| ボタン | サイズ | 位置 | 背景色 |
|--------|--------|------|--------|
| 散歩をやめる | 160×50dp | 左上 (20, 20) | #FF5722 (オレンジ) |
| ミュート | 50×50dp | 右上 | #607D8B (グレー) |

### 6.3 ExitButton 実装

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;

public class ExitWalkButton : MonoBehaviour
{
    [SerializeField] private WalkScheduler walkScheduler;
    [SerializeField] private string mainSceneName = "main";

    public void OnExitButtonClicked()
    {
        ExitToMainScene().Forget();
    }

    private async UniTaskVoid ExitToMainScene()
    {
        // 散歩完了をマーク
        walkScheduler.MarkWalkCompleted();

        // PetStateを戻す
        GlobalVariables.Instance.SetPetState(PetState.idle);

        // フェードアウト（オプション）
        // await FadeManager.Instance.FadeOut(0.5f);

        // メインシーンへ戻る
        await SceneManager.LoadSceneAsync(mainSceneName);
    }
}
```

---

## 7. 30分タイマー（オプション）

### 7.1 WalkTimer

```csharp
using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class WalkTimer : MonoBehaviour
{
    [Header("設定")]
    [SerializeField] private float walkDurationMinutes = 30f;
    [SerializeField] private bool autoEndEnabled = false;

    [Header("UI")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private GameObject timerUI;

    [Header("イベント")]
    public UnityEvent OnTimeUp;

    private float remainingTime;
    private bool isRunning = false;

    private void Start()
    {
        remainingTime = walkDurationMinutes * 60f;
        isRunning = true;
    }

    private void Update()
    {
        if (!isRunning) return;

        remainingTime -= Time.deltaTime;

        UpdateTimerDisplay();

        if (remainingTime <= 0)
        {
            isRunning = false;
            if (autoEndEnabled)
            {
                OnTimeUp?.Invoke();
            }
        }
    }

    private void UpdateTimerDisplay()
    {
        if (timerText == null) return;

        int minutes = Mathf.FloorToInt(remainingTime / 60f);
        int seconds = Mathf.FloorToInt(remainingTime % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }
}
```

### 7.2 タイマー仕様

| 項目 | 仕様 |
|------|------|
| 初期値 | 30分 |
| 表示形式 | MM:SS |
| 終了時 | 通知表示（自動終了はオプション） |

---

## 8. 3Dマップ（Asset Store）

### 8.1 推奨アセット

| アセット名 | 価格 | 特徴 |
|-----------|------|------|
| Low Poly Nature Pack | 無料 | 軽量、モバイル対応 |
| Simple Stylized Nature Pack | $15~ | かわいいスタイル |
| Cute Park Pack | $20~ | 公園特化 |
| Low Poly Town Pack | $25~ | 街並み含む |

### 8.2 要件

| 要件 | 説明 |
|------|------|
| ポリゴン数 | 低ポリ（モバイル対応） |
| テクスチャ | 512×512以下推奨 |
| NavMesh対応 | 歩行可能な地形 |
| スタイル | アイソメトリック視点に適したフラットなデザイン |

### 8.3 マップ構成要素

```
公園マップ
├── 地面
│   ├── 芝生エリア
│   ├── 砂利道
│   └── 池（歩行不可）
├── オブジェクト
│   ├── 木（複数種）
│   ├── ベンチ
│   ├── 花壇
│   ├── 街灯
│   └── 噴水
└── 境界
    └── フェンス/植え込み
```

---

## 9. NavMesh 設定

### 9.1 NavMeshSurface 設定

```csharp
// NavMeshSurface コンポーネント設定
Agent Type: Humanoid
Include Layers: Ground, Walkable
Use Geometry: Physics Colliders
```

### 9.2 NavMeshAgent 設定

| キャラクター | Speed | Radius | Height |
|-------------|-------|--------|--------|
| プレイヤー | 3 | 0.3 | 1.8 |
| 犬 | 2.5-4 | 0.2 | 0.6 |

### 9.3 NavMeshObstacle

- 池、フェンスなど歩行不可エリアに配置
- Carve オプションを使用して動的な障害物対応

---

## 10. MetaverseManager

### 10.1 クラス設計

```csharp
using UnityEngine;
using Cysharp.Threading.Tasks;

public class MetaverseManager : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private MetaverseDogController dog;        // 犬（先導役）
    [SerializeField] private MetaversePlayerFollower player;    // プレイヤー（追従役）
    [SerializeField] private MetaverseCamera metaverseCamera;
    [SerializeField] private WalkScheduler walkScheduler;

    [Header("スポーン")]
    [SerializeField] private Transform spawnPoint;

    private void Start()
    {
        Initialize().Forget();
    }

    private async UniTaskVoid Initialize()
    {
        // 犬をスポーン位置に配置（犬が前）
        if (dog != null && spawnPoint != null)
        {
            dog.transform.position = spawnPoint.position;
            dog.transform.rotation = spawnPoint.rotation;
        }

        // プレイヤーを犬の後ろに配置
        if (player != null && dog != null)
        {
            Vector3 playerSpawnPos = dog.transform.position - dog.transform.forward * 1.5f;
            player.transform.position = playerSpawnPos;
            player.transform.rotation = dog.transform.rotation;
        }

        // カメラ初期化
        if (metaverseCamera != null)
        {
            // 初期位置にスナップ
        }

        // フェードイン
        // await FadeManager.Instance.FadeIn(0.5f);

        Debug.Log("[MetaverseManager] Initialized");
    }

    private void OnDestroy()
    {
        // クリーンアップ
    }
}
```

---

## 11. ライティング設定

### 11.1 昼間設定

| 項目 | 設定値 |
|------|--------|
| Directional Light Color | #FFF9E5 (暖かい白) |
| Directional Light Intensity | 1.0 |
| Ambient Color | #87CEEB (スカイブルー) |
| Shadow Type | Soft Shadows |
| Shadow Resolution | Medium |

### 11.2 パフォーマンス設定

| 項目 | 設定値 |
|------|--------|
| Realtime GI | OFF |
| Baked GI | ON (可能なら) |
| Shadow Distance | 30m |
| LOD Bias | 1.0 |

---

## 12. 音響設定

### 12.1 環境音

| サウンド | ループ | 音量 |
|----------|--------|------|
| 鳥のさえずり | Yes | 0.3 |
| 風の音 | Yes | 0.2 |
| 噴水の音 | Yes (3D) | 0.5 |

### 12.2 効果音

| イベント | サウンド |
|----------|----------|
| 歩行 | 足音（芝生/砂利） |
| 犬の歩行 | 犬の足音 |
| UI操作 | ボタンSE |

---

## 13. パフォーマンス最適化

### 13.1 描画最適化

| 技術 | 説明 |
|------|------|
| Occlusion Culling | 見えないオブジェクトを除外 |
| LOD | 距離に応じたモデル切り替え |
| Static Batching | 静的オブジェクトのバッチ処理 |
| GPU Instancing | 同一オブジェクトのインスタンシング |

### 13.2 目標フレームレート

| プラットフォーム | 目標FPS |
|-----------------|---------|
| Android (ミドル) | 30 FPS |
| Android (ハイエンド) | 60 FPS |

---

## 14. テストケース

| # | テスト内容 | 期待結果 |
|---|-----------|---------|
| 1 | シーン遷移 | 正常にMetaverseシーンがロードされる |
| 2 | 犬の移動 | タップした位置へ**犬**が移動する |
| 3 | プレイヤーの追従 | 犬の後ろをプレイヤーが追従する |
| 4 | カメラ追従 | 犬とプレイヤーの中間をカメラが追従 |
| 5 | 境界 | マップ外に移動できない |
| 6 | 散歩終了 | ボタンでメインシーンに戻る |
| 7 | パフォーマンス | 30FPS以上を維持 |
| 8 | 追従距離 | プレイヤーが犬から離れすぎない |

---

## 15. 関連ファイル

| ファイル | 役割 |
|----------|------|
| `MetaverseManager.cs` | シーン管理（新規作成） |
| `MetaverseDogController.cs` | 犬の移動制御・先導役（新規作成） |
| `MetaversePlayerFollower.cs` | プレイヤーの追従・追従役（新規作成） |
| `MetaverseCamera.cs` | カメラ制御（新規作成） |
| `ExitWalkButton.cs` | 終了ボタン（新規作成） |
| `WalkTimer.cs` | タイマー（新規作成、オプション） |
