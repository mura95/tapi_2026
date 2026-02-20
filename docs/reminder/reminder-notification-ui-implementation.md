# ReminderNotificationUI 実装ガイド

## 概要

高齢者向けリマインダー通知UIの Unity 実装ガイド。
服薬・食事・水分補給などの時間になると、犬が吠えて通知し、この画面が表示される。

**関連ドキュメント:**
- [デザイン仕様書](./design-spec-reminder-notification-ui.md)

---

## 完成イメージ

```
┌─────────────────────────────────────────┐
│  [満腹ゲージ]              [ご飯][遊び] │
│                            [おかし]     │
│                                         │
│      ┌─────────────────────────┐       │
│      │   💊 服薬               │       │
│      │                         │       │
│      │   田中さん、お薬を      │       │
│      │   服用してください      │       │
│      │                         │       │
│      │      [ 完了 ]           │       │
│      └─────────────────────────┘       │
│                                         │
│            🐕 (吠えている犬)            │
│                                         │
└─────────────────────────────────────────┘
```

---

## Unity階層構造

```
ReminderNotificationUI (Canvas)
├── NotificationPanel (Image)
│   ├── IconImage (Image)
│   ├── TypeText (TextMeshPro)
│   ├── MessageText (TextMeshPro)
│   └── CompleteButton (Button)
│       └── Text (TextMeshPro)
```

---

## 詳細設定

### 1. Canvas

**作成:** Hierarchy → 右クリック → UI → Canvas

| コンポーネント | 設定項目 | 値 |
|--------------|----------|-----|
| Canvas | Render Mode | Screen Space - Overlay |
| Canvas | Sort Order | 100 |
| Canvas Scaler | UI Scale Mode | Scale With Screen Size |
| Canvas Scaler | Reference Resolution | 1080 x 1920 |
| Canvas Scaler | Screen Match Mode | Match Width Or Height |
| Canvas Scaler | Match | 0.5 |

---

### 2. NotificationPanel

**作成:** Canvas下 → 右クリック → UI → Image

#### RectTransform

| 設定項目 | 値 |
|----------|-----|
| Anchor | Middle Center |
| Pivot | (0.5, 0.5) |
| Pos X | 0 |
| Pos Y | 200 |
| Width | 600 |
| Height | 400 |

#### Image

| 設定項目 | 値 |
|----------|-----|
| Source Image | 角丸Sprite（後述） |
| Color | #FFF8E7（クリーム色） |
| Image Type | Sliced |
| Raycast Target | ✓ |

#### Shadow（Add Component → UI → Effects → Shadow）

| 設定項目 | 値 |
|----------|-----|
| Effect Color | #00000033 |
| Effect Distance | (4, -4) |
| Use Graphic Alpha | ✓ |

---

### 3. IconImage

**作成:** NotificationPanel下 → 右クリック → UI → Image

#### RectTransform

| 設定項目 | 値 |
|----------|-----|
| Anchor | Top Center |
| Pivot | (0.5, 0.5) |
| Pos X | -80 |
| Pos Y | -60 |
| Width | 64 |
| Height | 64 |

#### Image

| 設定項目 | 値 |
|----------|-----|
| Source Image | （タイプに応じて動的設定） |
| Color | White |
| Preserve Aspect | ✓ |

---

### 4. TypeText

**作成:** NotificationPanel下 → 右クリック → UI → Text - TextMeshPro

#### RectTransform

| 設定項目 | 値 |
|----------|-----|
| Anchor | Top Center |
| Pivot | (0, 0.5) |
| Pos X | -30 |
| Pos Y | -60 |
| Width | 200 |
| Height | 80 |

#### TextMeshPro

| 設定項目 | 値 |
|----------|-----|
| Font Asset | Noto Sans JP または 游ゴシック |
| Font Style | Bold |
| Font Size | 56 |
| Color | #4A3728（ダークブラウン） |
| Alignment | Left / Middle |

---

### 5. MessageText

**作成:** NotificationPanel下 → 右クリック → UI → Text - TextMeshPro

#### RectTransform

| 設定項目 | 値 |
|----------|-----|
| Anchor | Middle Center |
| Pivot | (0.5, 0.5) |
| Pos X | 0 |
| Pos Y | 0 |
| Width | 520 |
| Height | 150 |

#### TextMeshPro

| 設定項目 | 値 |
|----------|-----|
| Font Asset | Noto Sans JP または 游ゴシック |
| Font Style | Normal |
| Font Size | 42 |
| Color | #4A3728（ダークブラウン） |
| Alignment | Center / Middle |
| Text Wrapping | Enabled |

---

### 6. CompleteButton

**作成:** NotificationPanel下 → 右クリック → UI → Button - TextMeshPro

#### RectTransform

| 設定項目 | 値 |
|----------|-----|
| Anchor | Bottom Center |
| Pivot | (0.5, 0.5) |
| Pos X | 0 |
| Pos Y | 70 |
| Width | 240 |
| Height | 72 |

#### Image

| 設定項目 | 値 |
|----------|-----|
| Source Image | 角丸Sprite |
| Color | #E67E22（オレンジ） |
| Image Type | Sliced |

#### Button

| 状態 | Color Multiplier |
|------|------------------|
| Normal | #FFFFFF |
| Highlighted | #F5F5F5 |
| Pressed | #C8C8C8 |
| Disabled | #C8C8C8 |

#### 子テキスト（TextMeshPro）

| 設定項目 | 値 |
|----------|-----|
| Text | 完了 |
| Font Style | Bold |
| Font Size | 40 |
| Color | #FFFFFF（白） |
| Alignment | Center / Middle |

---

## アイコン一覧

| タイプ | 英語キー | 日本語 | 推奨アイコン | 推奨色 |
|--------|----------|--------|-------------|--------|
| 服薬 | medication | 服薬 | カプセル/錠剤 | #E74C3C |
| 食事 | meal | 食事 | お椀/箸 | #E67E22 |
| 水分補給 | hydration | 水分補給 | コップ/水滴 | #3498DB |
| 運動 | exercise | 運動 | 体操する人 | #27AE60 |
| 休憩 | rest | 休憩 | 椅子/Zzz | #9B59B6 |
| 予定 | appointment | 予定 | カレンダー | #E74C3C |

**アイコン保存先:** `Assets/Resources/UI/ReminderIcons/`

---

## スクリプト実装

### ReminderNotificationUI.cs

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ReminderNotificationUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image _iconImage;
    [SerializeField] private TextMeshProUGUI _typeText;
    [SerializeField] private TextMeshProUGUI _messageText;
    [SerializeField] private Button _completeButton;

    [Header("Icon Sprites")]
    [SerializeField] private Sprite _medicationIcon;
    [SerializeField] private Sprite _mealIcon;
    [SerializeField] private Sprite _hydrationIcon;
    [SerializeField] private Sprite _exerciseIcon;
    [SerializeField] private Sprite _restIcon;
    [SerializeField] private Sprite _appointmentIcon;

    private System.Action _onComplete;

    private void Awake()
    {
        _completeButton.onClick.AddListener(OnCompleteClicked);
        gameObject.SetActive(false);
    }

    public void Show(string type, string userName)
    {
        // アイコン設定
        _iconImage.sprite = GetIconForType(type);

        // タイプラベル設定
        _typeText.text = GetTypeLabel(type);

        // メッセージ設定
        _messageText.text = GetMessage(type, userName);

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void SetOnCompleteCallback(System.Action callback)
    {
        _onComplete = callback;
    }

    private void OnCompleteClicked()
    {
        _onComplete?.Invoke();
        Hide();
    }

    private Sprite GetIconForType(string type)
    {
        return type switch
        {
            "medication" => _medicationIcon,
            "meal" => _mealIcon,
            "hydration" => _hydrationIcon,
            "exercise" => _exerciseIcon,
            "rest" => _restIcon,
            "appointment" => _appointmentIcon,
            _ => _medicationIcon
        };
    }

    private string GetTypeLabel(string type)
    {
        return type switch
        {
            "medication" => "服薬",
            "meal" => "食事",
            "hydration" => "水分補給",
            "exercise" => "運動",
            "rest" => "休憩",
            "appointment" => "予定",
            _ => "お知らせ"
        };
    }

    private string GetMessage(string type, string userName)
    {
        string name = string.IsNullOrEmpty(userName) ? "" : $"{userName}さん、";

        return type switch
        {
            "medication" => $"{name}お薬を服用してください",
            "meal" => $"{name}お食事の時間です",
            "hydration" => $"{name}お水を飲んでください",
            "exercise" => $"{name}体操の時間です",
            "rest" => $"{name}休憩してください",
            "appointment" => $"{name}予定があります",
            _ => $"{name}お知らせです"
        };
    }
}
```

---

## ReminderManagerとの連携

### ReminderManager.cs（抜粋）

```csharp
public class ReminderManager : MonoBehaviour
{
    [SerializeField] private ReminderNotificationUI _notificationUI;
    [SerializeField] private DogController _dogController;

    private string _userName;

    private void ShowReminder(ReminderData reminder)
    {
        // 犬を吠えさせる
        _dogController?.ActionBark();

        // 通知UIを表示
        _notificationUI.SetOnCompleteCallback(() => OnReminderComplete(reminder));
        _notificationUI.Show(reminder.type, _userName);
    }

    private void OnReminderComplete(ReminderData reminder)
    {
        // Firebaseに完了を記録
        MarkReminderAsCompleted(reminder.id);

        // 犬の吠えを止める
        _dogController?.ActionBool(false);
    }
}
```

---

## 角丸Sprite作成方法

### 方法1: 画像を用意

1. 白い角丸四角形（256x256px、角丸32px）のPNG画像を作成
2. `Assets/Sprites/UI/` に配置
3. Import Settings:
   - Texture Type: Sprite (2D and UI)
   - Sprite Mode: Single
4. Sprite Editor → Border設定（9-slice）:
   - Left: 32, Right: 32, Top: 32, Bottom: 32

### 方法2: Unity標準Spriteを使用

- `UI/Skin/UISprite` など既存の角丸Spriteを使用

---

## カラーパレット

| 用途 | カラーコード | 色名 |
|------|-------------|------|
| パネル背景 | #FFF8E7 | クリーム |
| テキスト | #4A3728 | ダークブラウン |
| ボタン通常 | #E67E22 | オレンジ |
| ボタン押下 | #D35400 | ダークオレンジ |
| 影 | #00000033 | 黒33%透過 |

---

## Prefab化手順

1. 設定完了後、Canvasを選択
2. `Assets/Prefabs/UI/` にドラッグ&ドロップ
3. Prefab名: `ReminderNotificationUI.prefab`
4. シーン上のインスタンスは削除
5. ReminderManagerの `_notificationUI` フィールドにPrefabを参照
   - または、ReminderManagerがInstantiateで生成

---

## テスト方法

### エディタでのテスト

```csharp
// DebugCanvasManager.cs に追加
[ContextMenu("Test Reminder UI")]
private void TestReminderUI()
{
    var ui = FindObjectOfType<ReminderNotificationUI>();
    if (ui != null)
    {
        ui.Show("medication", "田中");
    }
}
```

### 確認項目

| # | テストケース | 期待結果 |
|---|-------------|----------|
| 1 | 服薬リマインダー表示 | カプセルアイコン + 「服薬」+ メッセージ |
| 2 | 食事リマインダー表示 | お椀アイコン + 「食事」+ メッセージ |
| 3 | 完了ボタン押下 | UI非表示 + コールバック実行 |
| 4 | 名前なしで表示 | 「お薬を服用してください」（敬称なし） |
| 5 | 犬の吠えと連動 | 表示時に吠え、完了時に停止 |

---

## トラブルシューティング

### UIが表示されない

1. Canvas の Sort Order を確認（100以上推奨）
2. `gameObject.SetActive(true)` が呼ばれているか確認
3. NotificationPanel の Color が透明でないか確認

### テキストが表示されない

1. TextMeshPro の Font Asset が設定されているか確認
2. 日本語フォント（Noto Sans JP等）がインポートされているか確認
3. Font Atlas に日本語文字が含まれているか確認

### ボタンが反応しない

1. Button の Interactable が ✓ になっているか確認
2. EventSystem がシーンに存在するか確認
3. Raycast Target が有効か確認

---

## 変更履歴

| 日付 | 変更内容 |
|------|---------|
| 2026-01-16 | 初版作成 |
