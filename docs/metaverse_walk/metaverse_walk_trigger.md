# 散歩トリガーシステム仕様書

## 1. 概要

毎日10時に犬が「散歩に行きたい」とアピールし、ユーザーがボタンをタップするとメタバースシーンへ遷移するシステム。

---

## 2. システム構成

```
┌─────────────────────────────────────────────────────────┐
│                      WalkScheduler                       │
│  ┌─────────────────────────────────────────────────┐    │
│  │  毎フレーム時刻チェック                          │    │
│  │      ↓                                          │    │
│  │  10:00になったら                                │    │
│  │      ↓                                          │    │
│  │  WalkRequestState = Active                      │    │
│  └─────────────────────────────────────────────────┘    │
└───────────────────────┬─────────────────────────────────┘
                        │
                        ↓
┌─────────────────────────────────────────────────────────┐
│                    WalkTriggerUI                         │
│  ┌─────────────────────────────────────────────────┐    │
│  │  WalkRequestState監視                           │    │
│  │      ↓                                          │    │
│  │  Active時:                                      │    │
│  │    - 犬のアニメーション再生（鳴く）              │    │
│  │    - 「散歩に行く」ボタン表示                    │    │
│  │      ↓                                          │    │
│  │  ボタンタップ時:                                │    │
│  │    - PetState.walk に遷移                       │    │
│  │    - Metaverseシーンへロード                    │    │
│  └─────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────┘
```

---

## 3. WalkScheduler クラス設計

### 3.1 クラス図

```csharp
public class WalkScheduler : MonoBehaviour
{
    // 設定
    [SerializeField] private int walkHour = 10;          // 散歩開始時刻（時）
    [SerializeField] private int walkMinute = 0;         // 散歩開始時刻（分）
    [SerializeField] private int walkWindowMinutes = 60; // 散歩受付時間（分）

    // 状態
    public WalkRequestState CurrentState { get; private set; }

    // イベント
    public event Action<WalkRequestState> OnStateChanged;

    // メソッド
    public void CheckSchedule();
    public void ResetState();
    public bool IsWalkTime();
}

public enum WalkRequestState
{
    Inactive,   // 散歩時間外
    Active,     // 散歩要求中（ボタン表示）
    Walking,    // 散歩中
    Completed   // 本日の散歩完了
}
```

### 3.2 詳細実装

```csharp
using UnityEngine;
using System;

public class WalkScheduler : MonoBehaviour
{
    [Header("スケジュール設定")]
    [SerializeField] private int walkHour = 10;
    [SerializeField] private int walkMinute = 0;
    [SerializeField] private int walkWindowMinutes = 60;

    [Header("永続化キー")]
    private const string LAST_WALK_DATE_KEY = "LastWalkDate";

    public WalkRequestState CurrentState { get; private set; } = WalkRequestState.Inactive;
    public event Action<WalkRequestState> OnStateChanged;

    private void Start()
    {
        // 起動時に状態を確認
        CheckSchedule();
    }

    private void Update()
    {
        // Inactive状態のときのみ時刻チェック
        if (CurrentState == WalkRequestState.Inactive)
        {
            CheckSchedule();
        }
    }

    public void CheckSchedule()
    {
        // 本日すでに散歩済みかチェック
        if (HasWalkedToday())
        {
            SetState(WalkRequestState.Completed);
            return;
        }

        // 現在時刻が散歩時間内かチェック
        if (IsWalkTime())
        {
            SetState(WalkRequestState.Active);
        }
        else
        {
            SetState(WalkRequestState.Inactive);
        }
    }

    public bool IsWalkTime()
    {
        DateTime now = TimeZoneUtility.GetLocalNow();
        DateTime walkStart = new DateTime(now.Year, now.Month, now.Day, walkHour, walkMinute, 0);
        DateTime walkEnd = walkStart.AddMinutes(walkWindowMinutes);

        return now >= walkStart && now < walkEnd;
    }

    private bool HasWalkedToday()
    {
        string lastWalkDate = PlayerPrefs.GetString(LAST_WALK_DATE_KEY, "");
        string today = DateTime.Today.ToString("yyyy-MM-dd");
        return lastWalkDate == today;
    }

    public void MarkWalkCompleted()
    {
        string today = DateTime.Today.ToString("yyyy-MM-dd");
        PlayerPrefs.SetString(LAST_WALK_DATE_KEY, today);
        PlayerPrefs.Save();
        SetState(WalkRequestState.Completed);
    }

    public void StartWalk()
    {
        SetState(WalkRequestState.Walking);
    }

    public void ResetState()
    {
        SetState(WalkRequestState.Inactive);
    }

    private void SetState(WalkRequestState newState)
    {
        if (CurrentState != newState)
        {
            CurrentState = newState;
            OnStateChanged?.Invoke(newState);
        }
    }
}
```

---

## 4. WalkTriggerUI クラス設計

### 4.1 クラス図

```csharp
public class WalkTriggerUI : MonoBehaviour
{
    // 参照
    [SerializeField] private WalkScheduler walkScheduler;
    [SerializeField] private GameObject walkButton;
    [SerializeField] private DogController dogController;

    // メソッド
    private void OnWalkStateChanged(WalkRequestState state);
    public void OnWalkButtonClicked();
    private void PlayWalkRequestAnimation();
    private void ShowWalkButton();
    private void HideWalkButton();
    private void TransitionToMetaverse();
}
```

### 4.2 詳細実装

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;

public class WalkTriggerUI : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private WalkScheduler walkScheduler;
    [SerializeField] private GameObject walkButton;
    [SerializeField] private DogController dogController;
    [SerializeField] private Animator dogAnimator;

    [Header("アニメーション")]
    [SerializeField] private string walkRequestTrigger = "WalkRequest";

    [Header("シーン")]
    [SerializeField] private string metaverseSceneName = "Metaverse";

    private void OnEnable()
    {
        if (walkScheduler != null)
        {
            walkScheduler.OnStateChanged += OnWalkStateChanged;
        }
    }

    private void OnDisable()
    {
        if (walkScheduler != null)
        {
            walkScheduler.OnStateChanged -= OnWalkStateChanged;
        }
    }

    private void Start()
    {
        // 初期状態を反映
        if (walkScheduler != null)
        {
            OnWalkStateChanged(walkScheduler.CurrentState);
        }
    }

    private void OnWalkStateChanged(WalkRequestState state)
    {
        switch (state)
        {
            case WalkRequestState.Active:
                PlayWalkRequestAnimation();
                ShowWalkButton();
                break;

            case WalkRequestState.Walking:
            case WalkRequestState.Completed:
            case WalkRequestState.Inactive:
            default:
                HideWalkButton();
                break;
        }
    }

    private void PlayWalkRequestAnimation()
    {
        // 犬が鳴いて散歩をねだるアニメーション
        if (dogAnimator != null)
        {
            dogAnimator.SetTrigger(walkRequestTrigger);
        }

        // 吠え声を再生
        // AudioController.Instance?.PlayBark();
    }

    private void ShowWalkButton()
    {
        if (walkButton != null)
        {
            walkButton.SetActive(true);
        }
    }

    private void HideWalkButton()
    {
        if (walkButton != null)
        {
            walkButton.SetActive(false);
        }
    }

    public void OnWalkButtonClicked()
    {
        TransitionToMetaverse().Forget();
    }

    private async UniTaskVoid TransitionToMetaverse()
    {
        // 状態を散歩中に変更
        walkScheduler.StartWalk();

        // PetStateを変更
        GlobalVariables.Instance.SetPetState(PetState.walk);

        // フェードアウト（オプション）
        // await FadeManager.Instance.FadeOut(0.5f);

        // シーン遷移
        await SceneManager.LoadSceneAsync(metaverseSceneName);
    }
}
```

---

## 5. PetState 拡張

### 5.1 PetState enum への追加

```csharp
// GlobalVariables.cs または PetState.cs

public enum PetState
{
    idle,       // 通常状態
    feeding,    // 食事中
    sleeping,   // 夜間睡眠中
    ball,       // ボール遊び中
    snack,      // おやつ中
    napping,    // 昼寝中
    ready,      // UI操作待ち
    moving,     // 移動中
    toy,        // おもちゃ遊び中
    action,     // アクション実行中
    walk,       // 散歩中（メタバース） ← 追加
}
```

### 5.2 状態遷移ルール

```
                    ┌─────────────────┐
                    │      idle       │
                    └────────┬────────┘
                             │
                             │ 10時 & ボタンタップ
                             ↓
                    ┌─────────────────┐
                    │      walk       │
                    └────────┬────────┘
                             │
                             │ 「散歩をやめる」ボタン
                             ↓
                    ┌─────────────────┐
                    │      idle       │
                    └─────────────────┘
```

### 5.3 walk状態中の制約

| 制約 | 説明 |
|------|------|
| タッチ無効 | メインシーンのタッチ操作は無効 |
| リマインダー | 散歩中は通知を遅延（要検討） |
| 食事/おやつ | 散歩中は不可 |
| 睡眠 | 散歩中は遷移しない |

---

## 6. 犬のアニメーション・サウンド

### 6.1 散歩要求アニメーション

```
WalkRequestアニメーション
├── 犬がプレイヤーの方を向く
├── 尻尾を振る
├── 2-3回吠える
└── ジャンプしてアピール
```

### 6.2 Animatorパラメータ

| パラメータ | 型 | 説明 |
|------------|------|------|
| WalkRequest | Trigger | 散歩要求アニメーション開始 |
| IsWalkMode | Bool | 散歩モード中フラグ |

### 6.3 サウンド

| イベント | サウンド |
|----------|----------|
| 散歩要求 | 嬉しそうな吠え声 × 2-3回 |
| ボタン表示 | UI表示SE |
| シーン遷移 | 遷移SE |

---

## 7. UI デザイン

### 7.1 「散歩に行く」ボタン

```
┌─────────────────────────────────────────┐
│                                         │
│                  🐕                     │
│               ╔═══════════╗             │
│               ║ 散歩に行く ║             │
│               ╚═══════════╝             │
│                  ↑                      │
│              ボタン（犬の上に表示）       │
│                                         │
└─────────────────────────────────────────┘
```

### 7.2 ボタン仕様

| 項目 | 仕様 |
|------|------|
| サイズ | 200 × 60 dp |
| フォントサイズ | 24sp |
| 背景色 | #4CAF50 (緑) |
| 文字色 | #FFFFFF (白) |
| 角丸 | 12dp |
| アニメーション | 上下に軽くバウンス |

### 7.3 位置

```csharp
// 犬のWorld座標からScreen座標に変換し、少し上に配置
Vector3 dogScreenPos = Camera.main.WorldToScreenPoint(dog.transform.position);
walkButton.transform.position = dogScreenPos + new Vector3(0, 100, 0);
```

---

## 8. シーン遷移処理

### 8.1 遷移フロー

```
1. ボタンタップ
      ↓
2. WalkScheduler.StartWalk()
      ↓
3. GlobalVariables.SetPetState(PetState.walk)
      ↓
4. （オプション）フェードアウト
      ↓
5. SceneManager.LoadSceneAsync("Metaverse")
      ↓
6. Metaverseシーン初期化
```

### 8.2 データ引き継ぎ

| データ | 引き継ぎ方法 |
|--------|-------------|
| ユーザー名 | PlayerPrefs / GlobalVariables |
| 犬の見た目 | ScriptableObject参照 |
| PetState | GlobalVariables |

### 8.3 シーン構成

```
Scenes/
├── main (既存メインシーン)
└── Metaverse (新規)
```

---

## 9. 永続化

### 9.1 PlayerPrefs キー

| キー | 型 | 説明 |
|------|-----|------|
| `LastWalkDate` | string | 最後に散歩した日付 (yyyy-MM-dd) |

### 9.2 日付リセット

- 毎日0時に `Completed` → `Inactive` にリセット
- または、日付変更を検知してリセット

---

## 10. 設定可能パラメータ

| パラメータ | デフォルト値 | 説明 |
|------------|-------------|------|
| walkHour | 10 | 散歩開始時刻（時） |
| walkMinute | 0 | 散歩開始時刻（分） |
| walkWindowMinutes | 60 | 散歩受付時間（分） |

### 将来の拡張

- ユーザーが散歩時間を設定可能にする
- 複数の散歩時間を設定可能にする（朝・夕）

---

## 11. テストケース

| # | テスト内容 | 期待結果 |
|---|-----------|---------|
| 1 | 10時前にアプリ起動 | ボタン非表示 |
| 2 | 10時になる | ボタン表示、犬が鳴く |
| 3 | ボタンタップ | Metaverseシーンへ遷移 |
| 4 | 散歩後、メインに戻る | ボタン非表示（Completed） |
| 5 | 翌日10時 | 再度ボタン表示 |
| 6 | 11時（受付時間終了後） | ボタン非表示 |

---

## 12. 関連ファイル

| ファイル | 役割 |
|----------|------|
| `WalkScheduler.cs` | スケジュール管理（新規作成） |
| `WalkTriggerUI.cs` | UI制御（新規作成） |
| `GlobalVariables.cs` | PetState定義（既存、拡張） |
| `TimeZoneUtility.cs` | 時刻変換（既存） |

---

## 13. 依存関係

- `Cysharp.UniTask` - 非同期処理
- `TimeZoneUtility` - ローカル時刻取得
- `GlobalVariables` - グローバル状態管理
