# コール機能（犬呼び込み）

## 1. 概要

メイン機（たっぷハウス）からサブ機（たっぷポケット）へ犬を呼び出す機能。**既存の`DogLocationSync`システムをそのまま使用**する。

**重要:** この機能は既に`Assets/Scripts/MultiDevice/`に完全実装済み。ポケットでは既存システムを呼び出すUIのみ追加する。

---

## 2. 既存システム概要

### 2.1 実装済みファイル

| ファイル | 役割 |
|----------|------|
| `Assets/Scripts/MultiDevice/DogLocationSync.cs` | Firebase同期マネージャー（シングルトン） |
| `Assets/Scripts/MultiDevice/DogTransferAnimation.cs` | 犬の入退場アニメーション制御 |

詳細: [docs/firebase/multi-device-dog-transfer.md](../../firebase/multi-device-dog-transfer.md)

### 2.2 DogLocationSync API

```csharp
namespace TapHouse.MultiDevice
{
    public enum DeviceRole { Main, Sub }

    public class DogLocationSync : MonoBehaviour
    {
        // シングルトン
        public static DogLocationSync Instance { get; }

        // プロパティ
        public DeviceRole CurrentRole { get; }  // 現在のデバイス役割
        public bool HasDog { get; }             // このデバイスに犬がいるか
        public string DeviceId { get; }         // デバイス固有ID
        public float RemainingTimeoutSeconds { get; } // タイムアウト残り時間

        // イベント
        public event Action<bool> OnDogPresenceChanged;  // 犬の有無が変わった時
        public event Action<bool> OnTransferStarted;     // 転送開始時（true=登場, false=退場）

        // メソッド
        public void SetDeviceRole(DeviceRole role);  // デバイス役割設定
        public UniTask RequestCallDog();             // 犬を呼ぶ
        public UniTask ReturnDogToMain();            // 犬をメイン機に返す
        public void RecordActivity();                // タイムアウトリセット
    }
}
```

### 2.3 転送フロー

```
┌─────────────────────────────────────────────────────────────┐
│                    犬の転送フロー                            │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  [ポケット(サブ機)]        [Firebase]        [ハウス(メイン機)]│
│        │                      │                     │       │
│  1. コールボタン押下          │                     │       │
│        │── RequestCallDog() ─→│                     │       │
│        │                      │── transferRequest ─→│       │
│        │                      │                     │ 2. 犬退場│
│        │                      │←─ currentDeviceId ──│       │
│  3. 犬登場 ←──────────────────│                     │       │
│        │                      │                     │       │
│  4. たっぷハウス画面に遷移     │                     │       │
│     （フル機能が使える）       │                     │       │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## 3. ポケットでの実装

### 3.1 ポケットメイン画面での使用

```csharp
using TapHouse.MultiDevice;

public class PocketMainUI : MonoBehaviour
{
    [SerializeField] private Button callButton;
    [SerializeField] private TMP_Text callButtonText;

    private void Start()
    {
        // 初期状態: ポケットは常にサブ機
        DogLocationSync.Instance.SetDeviceRole(DeviceRole.Sub);

        // イベント購読
        DogLocationSync.Instance.OnDogPresenceChanged += OnDogPresenceChanged;
        DogLocationSync.Instance.OnTransferStarted += OnTransferStarted;

        // ボタン設定
        callButton.onClick.AddListener(OnCallButtonClicked);

        // 初期UI更新
        UpdateUI();
    }

    private void OnDestroy()
    {
        if (DogLocationSync.Instance != null)
        {
            DogLocationSync.Instance.OnDogPresenceChanged -= OnDogPresenceChanged;
            DogLocationSync.Instance.OnTransferStarted -= OnTransferStarted;
        }
    }

    private async void OnCallButtonClicked()
    {
        // 既存APIを呼び出すだけ
        await DogLocationSync.Instance.RequestCallDog();
    }

    private void OnDogPresenceChanged(bool hasDog)
    {
        if (hasDog)
        {
            // 犬が来た → たっぷハウス画面に遷移
            SceneManager.LoadScene("main");
        }
        else
        {
            UpdateUI();
        }
    }

    private void OnTransferStarted(bool isEntering)
    {
        // 転送中はボタン無効化
        callButton.interactable = false;
        callButtonText.text = isEntering ? "呼び出し中..." : "帰還中...";
    }

    private void UpdateUI()
    {
        bool hasDog = DogLocationSync.Instance?.HasDog ?? false;
        callButton.interactable = !hasDog;
        callButtonText.text = hasDog ? "犬がいます" : "コール";
    }
}
```

### 3.2 ポケット起動時の設定

ポケットは起動時に自動的にサブ機として設定される：

```csharp
// AppConfig.cs で起動時に設定
public class AppConfig : MonoBehaviour
{
    [SerializeField] private AppMode appMode = AppMode.TapPocket;

    private void Start()
    {
        if (appMode == AppMode.TapPocket)
        {
            // ポケットは常にサブ機
            DogLocationSync.Instance?.SetDeviceRole(DeviceRole.Sub);
        }
    }
}
```

---

## 4. 仕様

### 4.1 動作仕様

| 項目 | 仕様 |
|------|------|
| デフォルト | メイン機（たっぷハウス）に犬がいる |
| コール | ポケットから呼ぶ → main.unityに遷移 |
| コール後 | たっぷハウス画面（main.unity）でフル機能が使える |
| 自動帰還 | ポケットメイン画面に戻ると犬がメイン機に自動帰還 |
| タイムアウト | 30分で自動的にメイン機に帰還 |
| メイン機再起動 | メイン機に犬が戻る |

**注意:** 「帰す」ボタンは実装しない。ポケットメイン画面（PocketMain.unity）に戻ると自動的に`ReturnDogToMain()`が呼ばれる。

### 4.2 Firebase構造（既存）

```
users/{userId}/dogLocation/
├── currentDeviceId: string     # 現在犬がいるデバイスID
├── isMainDevice: bool          # メイン機にいるか
├── transferRequest/            # 転送リクエスト
│     ├── requestingDeviceId: string
│     ├── timestamp: number
│     └── type: "call" | "return"
└── lastActivityTimestamp: number
```

---

## 5. UI設計

### 5.1 ポケットメイン画面でのコールボタン

```
┌─────────────────────────────────────┐
│                                     │
│         ┌───────────────┐           │
│         │               │           │
│         │   🐕 コール    │           │ ← 大きなボタン（100dp×100dp）
│         │  犬を呼ぶ      │           │
│         │               │           │
│         └───────────────┘           │
│                                     │
│     「犬をこちらに呼びましょう」     │
│                                     │
└─────────────────────────────────────┘

呼び出し中:
┌─────────────────────────────────────┐
│         ┌───────────────┐           │
│         │    🔄         │           │
│         │  呼び出し中... │           │ ← ボタン無効化
│         └───────────────┘           │
│     「犬が向かっています...」       │
└─────────────────────────────────────┘
```

### 5.2 犬が来た後（たっぷハウス画面）

コールで犬を呼んだ後は`main.unity`（たっぷハウス画面）に遷移：
- 餌やり、遊び、おやつなどフル機能が使える
- ポケットメイン画面に戻ると自動的に犬がメイン機に帰還
- タイムアウト（30分）で自動帰還

---

## 6. 新規実装が必要なもの

ポケットで新たに実装するのは**UIのみ**：

| ファイル | 内容 |
|----------|------|
| `PocketMainUI.cs` | コールボタンのOnClick処理 |
| `PocketMain.unity` | コールボタンのGameObject配置 |

既存の`DogLocationSync`と`DogTransferAnimation`は**変更不要**。

---

## 7. テストケース

| # | テスト内容 | 期待結果 |
|---|-----------|---------|
| 1 | コールボタンタップ | `RequestCallDog()`が呼ばれ、main.unityに遷移 |
| 2 | main.unityで餌やり | 正常に動作 |
| 3 | main.unityからポケットメインに戻る | 犬がメイン機に自動帰還 |
| 4 | 30分タイムアウト | 自動でメイン機に戻る |
| 5 | メイン機再起動 | ポケットの犬がメイン機に戻る |

---

## 8. 関連ドキュメント

| ドキュメント | 説明 |
|-------------|------|
| [docs/firebase/multi-device-dog-transfer.md](../../firebase/multi-device-dog-transfer.md) | マルチデバイスシステム詳細 |
| [DogLocationSync.cs](../../../Assets/Scripts/MultiDevice/DogLocationSync.cs) | 実装コード |
| [DogTransferAnimation.cs](../../../Assets/Scripts/MultiDevice/DogTransferAnimation.cs) | アニメーション実装 |
