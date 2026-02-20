# ポケットメイン画面 構築手順

## 概要

たっぷポケットのメイン画面（PocketMain.unity）を構築するための手順書。

---

## 画面仕様

### レイアウト（2×2グリッド）

```
┌─────────────────────────────────────┐
│  たっぷポケット             [⚙設定] │ ← 設定はWebブラウザでmypageを開く
├─────────────────────────────────────┤
│                                     │
│   ┌─────────────┐ ┌─────────────┐   │
│   │             │ │             │   │
│   │  🐕 コール   │ │  👕 着替え   │   │
│   │             │ │             │   │
│   └─────────────┘ └─────────────┘   │
│                                     │
│   ┌─────────────┐ ┌─────────────┐   │
│   │             │ │             │   │
│   │  👟 歩数計   │ │ 🎮 犬と遊ぶ  │   │
│   │             │ │             │   │
│   └─────────────┘ └─────────────┘   │
│                                     │
└─────────────────────────────────────┘
```

### ボタン機能

| ボタン | 動作 |
|--------|------|
| コール | main.unityに遷移（犬を呼んでフル機能を使う） |
| 着替え | DressUp.unityに遷移 |
| 歩数計 | Pedometer.unityに遷移 |
| 犬と遊ぶ | MiniGames.unityに遷移 |
| 設定 | OSブラウザでTapHouseWebのmypageを開く |

### 自動帰還機能

**重要:** ポケットメイン画面に戻ると、犬は自動的にメイン機に帰還する。

```
main.unity（犬と遊んでいる）
        ↓
  戻るボタン等
        ↓
PocketMain.unity に遷移
        ↓
OnEnable() で ReturnDogToMain() 呼び出し
        ↓
犬がメイン機に自動帰還
```

---

## 前提条件

- Unity 6 (6000.2.8f1) がインストール済み
- 既存のたっぷハウスプロジェクトが動作する状態
- Firebase設定が完了している

---

## 構築手順

### Phase 1: スクリプトの作成

#### Step 1.1: AppConfig.cs の作成

`Assets/Scripts/Pocket/AppConfig.cs`:

```csharp
using UnityEngine;
using TapHouse.MultiDevice;

namespace TapHouse.Pocket
{
    public enum AppMode
    {
        TapHouse,   // たっぷハウス（メイン画面から開始）
        TapPocket   // たっぷポケット（ポケット画面から開始）
    }

    public class AppConfig : MonoBehaviour
    {
        public static AppConfig Instance { get; private set; }

        [Header("アプリモード")]
        [SerializeField] private AppMode appMode = AppMode.TapPocket;

        public AppMode Mode => appMode;
        public bool IsPocket => appMode == AppMode.TapPocket;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (IsPocket && DogLocationSync.Instance != null)
            {
                DogLocationSync.Instance.SetDeviceRole(DeviceRole.Sub);
            }
        }
    }
}
```

#### Step 1.2: PocketMainUI.cs の作成

`Assets/Scripts/UI/ModeSelect/PocketMainUI.cs`:

```csharp
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using TapHouse.MultiDevice;

namespace TapHouse.Pocket.UI
{
    /// <summary>
    /// ポケットメイン画面のUI制御
    /// </summary>
    public class PocketMainUI : MonoBehaviour
    {
        [Header("2×2グリッドボタン")]
        [SerializeField] private Button callButton;
        [SerializeField] private Button dressUpButton;
        [SerializeField] private Button pedometerButton;
        [SerializeField] private Button miniGameButton;

        [Header("ヘッダー")]
        [SerializeField] private Button settingsButton;

        [Header("コールボタン表示")]
        [SerializeField] private TMP_Text callButtonText;
        [SerializeField] private GameObject loadingIndicator;

        [Header("Mypage URL")]
        [SerializeField] private string mypageUrl = "https://your-taphouse-web-url.com/mypage";

        private void OnEnable()
        {
            // ポケットメイン画面に戻ったら犬を自動帰還
            ReturnDogToMainIfNeeded();
        }

        private void Start()
        {
            SetupButtons();
            SetupDogLocationSync();
        }

        private void OnDestroy()
        {
            if (DogLocationSync.Instance != null)
            {
                DogLocationSync.Instance.OnTransferStarted -= OnTransferStarted;
            }
        }

        #region Setup

        private void SetupButtons()
        {
            // コールボタン → main.unityに遷移
            callButton?.onClick.AddListener(OnCallButtonClicked);

            // 着せ替えボタン
            dressUpButton?.onClick.AddListener(() => {
                SceneManager.LoadScene("DressUp");
            });

            // 歩数計ボタン
            pedometerButton?.onClick.AddListener(() => {
                SceneManager.LoadScene("Pedometer");
            });

            // ミニゲームボタン
            miniGameButton?.onClick.AddListener(() => {
                SceneManager.LoadScene("MiniGames");
            });

            // 設定ボタン → Webブラウザでmypageを開く
            settingsButton?.onClick.AddListener(OnSettingsButtonClicked);
        }

        private void SetupDogLocationSync()
        {
            if (DogLocationSync.Instance == null)
            {
                Debug.LogWarning("DogLocationSync.Instance is null");
                return;
            }

            // ポケットは常にサブ機
            DogLocationSync.Instance.SetDeviceRole(DeviceRole.Sub);

            // イベント購読
            DogLocationSync.Instance.OnTransferStarted += OnTransferStarted;
        }

        #endregion

        #region Button Handlers

        private async void OnCallButtonClicked()
        {
            if (DogLocationSync.Instance == null)
            {
                Debug.LogError("DogLocationSync.Instance is null");
                // Firebaseなしでも遷移は許可
                SceneManager.LoadScene("main");
                return;
            }

            // ローディング表示
            SetLoading(true);

            // 犬を呼ぶ
            await DogLocationSync.Instance.RequestCallDog();

            // main.unityに遷移
            SceneManager.LoadScene("main");
        }

        private void OnSettingsButtonClicked()
        {
            // OSのブラウザでmypageを開く
            Application.OpenURL(mypageUrl);
        }

        #endregion

        #region Dog Auto Return

        /// <summary>
        /// ポケットメイン画面に戻ったら犬をメイン機に帰還
        /// </summary>
        private async void ReturnDogToMainIfNeeded()
        {
            if (DogLocationSync.Instance == null) return;

            if (DogLocationSync.Instance.HasDog)
            {
                Debug.Log("Returning dog to main device (auto return on PocketMain)");
                await DogLocationSync.Instance.ReturnDogToMain();
            }
        }

        #endregion

        #region Events

        private void OnTransferStarted(bool isEntering)
        {
            SetLoading(true);
        }

        #endregion

        #region UI Updates

        private void SetLoading(bool isLoading)
        {
            if (callButton != null)
            {
                callButton.interactable = !isLoading;
            }

            if (callButtonText != null)
            {
                callButtonText.text = isLoading ? "読み込み中..." : "コール";
            }

            if (loadingIndicator != null)
            {
                loadingIndicator.SetActive(isLoading);
            }
        }

        #endregion
    }
}
```

---

### Phase 2: シーン構築（Unity Editor）

#### Step 2.1: PocketMain シーンの作成

1. **File → New Scene** で新規シーン作成
2. **File → Save As** で `Assets/Scenes/PocketMain.unity` として保存

#### Step 2.2: Hierarchy構造

```
PocketMain (Scene)
├── Managers
│   ├── AppConfig           ← AppConfig.cs
│   ├── DogLocationSync     ← 既存
│   └── FirebaseManager     ← 既存
│
├── EventSystem
│
├── Canvas (Screen Space - Overlay)
│   ├── Header
│   │   ├── TitleText       ← "たっぷポケット"
│   │   └── SettingsButton  ← 右上、⚙アイコン
│   │
│   └── ButtonGrid (Grid Layout Group)
│       ├── CallButton      ← 🐕 コール
│       ├── DressUpButton   ← 👕 着替え
│       ├── PedometerButton ← 👟 歩数計
│       └── MiniGameButton  ← 🎮 犬と遊ぶ
│
├── Camera
│   └── Main Camera
│
└── PocketMainUI            ← PocketMainUI.cs
```

#### Step 2.3: Canvas設定

1. **Canvas** を選択
2. **Canvas Scaler**:
   - UI Scale Mode: `Scale With Screen Size`
   - Reference Resolution: `1080 x 1920`
   - Match: `0.5`

#### Step 2.4: ButtonGrid設定（Grid Layout Group）

1. `ButtonGrid` に **Grid Layout Group** を追加
2. 設定:
   - Cell Size: `450 x 450`
   - Spacing: `30, 30`
   - Start Corner: `Upper Left`
   - Start Axis: `Horizontal`
   - Child Alignment: `Middle Center`
   - Constraint: `Fixed Column Count`
   - Constraint Count: `2`

#### Step 2.5: ボタンサイズ

| ボタン | サイズ | 備考 |
|--------|--------|------|
| 4つのグリッドボタン | 450×450px | Grid Layout Groupで自動調整 |
| SettingsButton | 100×100px | 右上に配置 |

#### Step 2.6: フォント設定

| 要素 | フォントサイズ |
|------|--------------|
| タイトル | 48px |
| ボタンテキスト | 40px |

---

### Phase 3: 参照設定

#### Step 3.1: PocketMainUI の参照設定

1. `PocketMainUI` オブジェクトを選択
2. Inspector で各フィールドを設定:

| フィールド | 参照先 |
|-----------|--------|
| callButton | CallButton |
| dressUpButton | DressUpButton |
| pedometerButton | PedometerButton |
| miniGameButton | MiniGameButton |
| settingsButton | SettingsButton |
| callButtonText | CallButton内のTMP_Text |
| mypageUrl | TapHouseWebのmypageURL |

---

### Phase 4: ビルド設定

#### Step 4.1: シーンをビルドに追加

**File → Build Settings**:

```
Scenes In Build:
  0: Scenes/Splash
  1: Scenes/Login
  2: Scenes/ModeSelect      ← モード選択（ある場合）
  3: Scenes/PocketMain      ← 追加
  4: Scenes/main
  5: Scenes/DressUp         ← 後で作成
  6: Scenes/Pedometer       ← 後で作成
  7: Scenes/MiniGames       ← 後で作成
```

---

## 動作確認チェックリスト

### 基本動作

- [ ] シーンが正常に開く
- [ ] 4つのボタンが2×2グリッドで表示される
- [ ] ボタンサイズが適切（タップしやすい）

### コール機能

- [ ] コールボタンをタップ → main.unityに遷移
- [ ] main.unityで犬と遊べる

### 自動帰還

- [ ] main.unityからポケットメインに戻る → 犬がメイン機に自動帰還

### 設定ボタン

- [ ] 設定ボタンをタップ → OSブラウザでmypageが開く

### その他のボタン

- [ ] 着せ替えボタン → DressUp シーン（未実装なら警告）
- [ ] 歩数計ボタン → Pedometer シーン（未実装なら警告）
- [ ] 犬と遊ぶボタン → MiniGames シーン（未実装なら警告）

---

## 次のステップ

1. **DressUp.unity** シーンの作成 → [dress-up.md](./features/dress-up.md)
2. **Pedometer.unity** シーンの作成 → [pedometer.md](./features/pedometer.md)
3. **MiniGames.unity** シーンの作成 → [brain-training.md](./features/brain-training.md)

---

## ファイル一覧

| ファイル | 説明 |
|----------|------|
| `Assets/Scenes/PocketMain.unity` | ポケットメイン画面シーン |
| `Assets/Scripts/Pocket/AppConfig.cs` | アプリモード管理 |
| `Assets/Scripts/UI/ModeSelect/PocketMainUI.cs` | メイン画面UI制御 |

---

## 関連ドキュメント

| ドキュメント | 説明 |
|-------------|------|
| [pocket-overview.md](./pocket-overview.md) | 全体概要 |
| [call-dog.md](./features/call-dog.md) | コール機能詳細 |
| [ui-architecture.md](./ui-architecture.md) | UI設計詳細 |


まず、アプリを起動したときに、初期画面をこの.unityにしたいんですけど、それってどうやってやりますか？

Firebase認証確認 中は、 uiを表示しないようになっていますが、インジケーターを表示する方向に変えたい。