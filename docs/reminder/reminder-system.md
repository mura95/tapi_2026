# リマインダー通知システム

## 概要

高齢者向けの服薬・食事・水分補給などのリマインダー通知機能。
見守りアプリから登録された時間になると、犬が吠えて通知する。

## 通知の種類

| Type | 日本語 | メッセージ例 |
|------|--------|-------------|
| medication | 服薬 | ○○さん、お薬を服用してください |
| meal | 食事 | ○○さん、お食事の時間です |
| hydration | 水分補給 | ○○さん、お水を飲んでください |
| exercise | 運動 | ○○さん、体操の時間です |
| rest | 休憩 | ○○さん、休憩してください |
| appointment | 予定 | ○○さん、予定があります |

---

## ファイル構成

```
Assets/Scripts/Reminder/
├── ReminderType.cs              # 通知種類のEnum（6種類）
├── ReminderData.cs              # データモデル
├── ReminderManager.cs           # メイン制御クラス
└── ReminderNotificationUI.cs    # 通知UIコントローラー

Assets/Resources/UI/
└── ReminderNotificationUI.prefab  # 通知UIプレハブ（要作成）
```

---

## Firestore データ構造

### リマインダー登録データ
**Collection:** `users/{userId}/reminders`
**Document:** `{reminderId}`

```json
{
  "id": "reminder_001",
  "type": "medication",
  "displayName": "朝のお薬",
  "times": [
    { "hour": 8, "minute": 0 },
    { "hour": 12, "minute": 0 },
    { "hour": 20, "minute": 0 }
  ],
  "daysOfWeek": [1, 2, 3, 4, 5],
  "enabled": true,
  "createdAt": 1705200000000,
  "updatedAt": 1705200000000
}
```

### フィールド説明

| フィールド | 型 | 説明 |
|-----------|---|------|
| id | string | リマインダーID |
| type | string | 通知種類（medication, meal, hydration, exercise, rest, appointment） |
| displayName | string | 表示名（見守りアプリで設定） |
| times | array | 通知時間の配列（複数設定可能） |
| daysOfWeek | array | 曜日指定（0=日, 1=月, ..., 6=土）。空配列=毎日 |
| enabled | boolean | 有効/無効フラグ |

### リマインダーログ
**Collection:** `users/{userId}/reminderLog`
**Document:** 自動生成ID

```json
{
  "reminderId": "reminder_001",
  "type": "medication",
  "scheduledHour": 8,
  "scheduledMinute": 0,
  "triggeredAt": "2024-01-14T08:00:15Z",
  "completedAt": "2024-01-14T08:05:30Z",
  "responseTimeSeconds": 315,
  "success": true
}
```

| フィールド | 型 | 説明 |
|-----------|---|------|
| reminderId | string | リマインダーID |
| type | string | 通知種類 |
| scheduledHour | int | 予定時刻（時） |
| scheduledMinute | int | 予定時刻（分） |
| triggeredAt | Timestamp | 通知開始時刻 |
| completedAt | Timestamp | 完了/タイムアウト時刻 |
| responseTimeSeconds | int | 応答時間（秒） |
| success | boolean | `true`: 完了ボタン押下、`false`: タイムアウト |

### 応答時間の活用例

| 分析項目 | 説明 |
|---------|------|
| 平均応答時間 | 普段の反応速度を把握 |
| 時間帯別傾向 | 朝は早い、夕方は遅い等のパターン |
| 異常検知 | 通常より大幅に遅い場合にアラート |
| 完了率 | success=true の割合を計算 |

---

## 通知フロー

```
60秒ポーリング
    │
    ├─→ 状態チェック（sleeping/napping/reminder中はスキップ）
    │
    ├─→ 曜日チェック（daysOfWeek）
    │
    ├─→ 時間チェック（許容範囲: ±1分）
    │
    ├─→ 今日通知済みか確認（PlayerPrefs）
    │
    └─→ 通知トリガー
        │
        ├─→ GlobalVariables.CurrentState = PetState.reminder
        │
        ├─→ タイムアウトコルーチン開始（60分）
        │
        ├─→ 犬を中央(0,0,0)に移動
        │
        ├─→ 吠えアニメーション（ActionBark）
        │
        ├─→ 吠え音ループ開始
        │
        └─→ ReminderNotificationUI表示
            │
            ├─→ 完了ボタン押下
            │   ├─→ 音停止
            │   ├─→ Firebase書き込み（success: true）
            │   ├─→ State = idle
            │   └─→ UI破棄
            │
            └─→ 60分タイムアウト
                ├─→ 音停止
                ├─→ Firebase書き込み（success: false）
                ├─→ State = idle
                └─→ UI破棄
```

---

## 状態制御

### PetState.reminder

`PetState.reminder` 状態中は以下の動作が制限される:

| 動作 | 制限状況 |
|------|---------|
| タッチ反応 | 無効（TouchController.cs） |
| 歩き回り | 無効（IdleWalkAnimation.cs） |
| transitionNo変化 | 無効 |
| 完了ボタン | 有効 |

### チェック箇所

```csharp
// TouchController.cs
if (GlobalVariables.CurrentState == PetState.reminder)
{
    // タッチ無効
    return;
}

// IdleWalkAnimation.cs
if (GlobalVariables.CurrentState == PetState.reminder)
{
    // 歩き回り無効
    return;
}
```

---

## データ読み込み

### 方式: 起動時 + アプリ復帰時

```csharp
void Start()
{
    LoadRemindersOnce();  // 起動時
}

void OnApplicationPause(bool pause)
{
    if (!pause)
    {
        LoadRemindersOnce();  // 復帰時
    }
}
```

リアルタイムリスナーは使用しない（コスト削減）。

### 将来の拡張

Cloud Functions + FCM で見守りアプリからの変更をリアルタイム反映:

```
見守りアプリ → Firebase更新 → Cloud Functions → FCM → タップハウス → 変数更新
```

---

## 重複通知防止

### PlayerPrefsキー形式

```
Reminder_{reminderId}_{yyyyMMdd}_{HHmm}
```

例: `Reminder_reminder_001_20240114_0800`

同じ日の同じ時間には再通知しない。

---

## シーン統合手順

### 1. ReminderManagerをシーンに追加

```
Main Scene
└── ReminderManager (GameObject)
    └── ReminderManager (Component)
        ├── _dogController: [DogControllerへの参照]
        ├── _turnAndMoveHandler: [TurnAndMoveHandlerへの参照]
        ├── _barkAudioSource: [AudioSourceへの参照]
        └── _barkClip: [吠え音AudioClip]
```

### 2. UI Prefab作成

`Assets/Resources/UI/ReminderNotificationUI.prefab`:

```
ReminderNotificationUI (Canvas)
├── Background (Image - 半透明黒、Button付き)
└── Panel (Image - 白背景)
    ├── _iconImage (Image)
    ├── _typeText (TextMeshProUGUI)
    ├── _messageText (TextMeshProUGUI)
    └── _completeButton (Button)
        └── Text (TextMeshProUGUI - "完了")
```

Canvas設定:
- Render Mode: Screen Space - Overlay
- Sort Order: 1000

---

## 設定パラメータ

| パラメータ | デフォルト | 説明 |
|-----------|-----------|------|
| _checkIntervalSeconds | 60 | チェック間隔（秒） |
| _reminderToleranceMinutes | 1 | 時間許容範囲（分） |
| _timeoutMinutes | 60 | タイムアウト時間（分） |

---

## テスト方法

### 1. Firestoreにテストデータを登録

Firebase Console → Firestore Database → `users/{userId}/reminders` コレクション → ドキュメント追加:

```json
{
  "id": "test_reminder",
  "type": "medication",
  "displayName": "テスト服薬",
  "times": [{ "hour": 14, "minute": 30 }],
  "daysOfWeek": [],
  "enabled": true,
  "createdAt": 1705200000000,
  "updatedAt": 1705200000000
}
```

### 2. タイムアウトテスト

`_timeoutMinutes` を1分に設定してテスト。

### 3. 状態制約テスト

reminder状態中に画面タッチ → 反応しないことを確認。

### 4. ログ確認

Firebase Console → Firestore Database → `users/{userId}/reminderLog` コレクション:
- `success: true` または `success: false` が記録されていることを確認

---

## 関連ファイル

| ファイル | 役割 |
|---------|------|
| `DogController.cs` | ActionBark()、TurnAndMoveHandler |
| `SleepController.cs` | 時間ポーリングパターンの参考 |
| `FoodSelectionUI.cs` | ポップアップUIパターンの参考 |
| `GlobalVariables.cs` | PetState管理 |
| `FirebaseManager.cs` | Firebase読み書きパターンの参考 |

---

## 残り作業（TODO）

### 必須

| 作業 | 説明 | 優先度 |
|------|------|--------|
| UI Prefab作成 | `Assets/Resources/UI/ReminderNotificationUI.prefab` を作成 | 高 |
| シーン統合 | ReminderManagerをメインシーンに追加、参照設定 | 高 |
| 吠え音AudioClip | リマインダー用の吠え音を設定 | 高 |
| Firebaseテスト | テストデータを登録して動作確認 | 高 |

### 推奨

| 作業 | 説明 | 優先度 |
|------|------|--------|
| アイコン素材 | 通知種類ごとのアイコン画像を作成 | 中 |
| ローカライズ対応 | メッセージをLocalizationManagerで管理 | 中 |
| 音量設定 | GameAudioSettingsにリマインダー音量を追加 | 低 |

---

## 拡張可能な機能

### 1. Cloud Functions連携（リアルタイム更新）

**目的**: 見守りアプリでリマインダーを変更したとき、即座にタップハウスに反映

**実装方針**:
```
見守りアプリ
    │
    └─→ Firebase reminders 更新
        │
        └─→ Cloud Functions (onWrite トリガー)
            │
            └─→ FCM プッシュ通知
                │
                └─→ タップハウスアプリ
                    │
                    └─→ LoadRemindersOnce() 再実行
```

**必要なもの**:
- Cloud Functions (Node.js)
- FCM (Firebase Cloud Messaging)
- Android FCM受信実装

---

### 2. スヌーズ機能

**目的**: 完了ボタンの代わりに「5分後に再通知」

**データ構造追加**:
```json
{
  "snoozeEnabled": true,
  "snoozeMinutes": 5
}
```

**UI追加**:
- 「完了」ボタンの横に「あとで」ボタン

**実装**:
```csharp
public void OnSnoozeClicked()
{
    StopBarkLoop();
    CloseNotificationUI();

    // スヌーズ後に再通知
    StartCoroutine(SnoozeAndRetrigger(_activeReminder, snoozeMinutes));
}
```

---

### 3. 通知履歴画面

**目的**: 過去の通知履歴を見守りアプリまたはタップハウスで確認

**データ構造変更**:
```
users/{userId}/reminderLogs/{logId}  ← 複数ログを保持
```

**表示内容**:
- 日時
- 種類
- 完了/未完了
- 完了までの所要時間

---

### 4. カスタムメッセージ

**目的**: 見守り側が任意のメッセージを設定可能

**データ構造追加**:
```json
{
  "customMessage": "血圧のお薬を飲んでね"
}
```

**実装**:
```csharp
string message = string.IsNullOrEmpty(reminder.customMessage)
    ? GetMessageForType(reminder.GetReminderType(), userName)
    : reminder.customMessage;
```

---

### 5. 通知音のカスタマイズ

**目的**: 通知種類ごとに異なる音を設定

**データ構造追加**:
```json
{
  "soundType": "gentle"  // gentle, normal, urgent
}
```

**実装**:
```csharp
[SerializeField] private AudioClip _gentleBarkClip;
[SerializeField] private AudioClip _normalBarkClip;
[SerializeField] private AudioClip _urgentBarkClip;

private AudioClip GetBarkClip(string soundType)
{
    return soundType switch
    {
        "gentle" => _gentleBarkClip,
        "urgent" => _urgentBarkClip,
        _ => _normalBarkClip
    };
}
```

---

### 6. 見守り側への即時通知

**目的**: タイムアウト時に見守りアプリへプッシュ通知

**フロー**:
```
タイムアウト発生
    │
    └─→ Firebase reminderLog 書き込み (success: false)
        │
        └─→ Cloud Functions (onWrite トリガー)
            │
            └─→ 見守りアプリへFCM送信
                │
                └─→ 「○○さんがお薬を飲み忘れています」
```

---

### 7. 繰り返しパターンの拡張

**目的**: 毎週/隔週/毎月などの柔軟なスケジュール

**データ構造追加**:
```json
{
  "repeatPattern": "weekly",
  "repeatInterval": 1,
  "startDate": "2024-01-01",
  "endDate": "2024-12-31"
}
```

---

### 8. 複数ユーザー対応

**目的**: 1台のタブレットで複数の高齢者を管理

**データ構造**:
```
devices/{deviceId}/users/
├── user_1/reminders/
└── user_2/reminders/
```

**UI**:
- 起動時にユーザー選択画面
- メッセージに「田中さん」「山田さん」など表示

---

### 9. 統計・レポート機能

**目的**: 服薬遵守率などを見守りアプリで確認

**集計項目**:
- 日別/週別/月別の完了率
- 平均応答時間
- タイムアウト回数

**Cloud Functions**:
```javascript
// 毎日0時に集計
exports.dailyStats = functions.pubsub
  .schedule('0 0 * * *')
  .onRun(async (context) => {
    // reminderLogsを集計してstatsに書き込み
  });
```

---

### 10. StateRestriction共通化（将来）

**目的**: sleeping/napping/reminderの状態制約を一元管理

**実装**:
```csharp
public static class StateRestriction
{
    private static readonly HashSet<PetState> _restrictedStates = new()
    {
        PetState.sleeping,
        PetState.napping,
        PetState.reminder,
    };

    public static bool IsRestricted =>
        _restrictedStates.Contains(GlobalVariables.CurrentState);
}
```

**適用**:
- 既存の個別チェックをStateRestriction.IsRestrictedに置き換え
- 新しい制約状態が増えても1箇所の変更で済む

---

## 変更履歴

| 日付 | 変更内容 |
|------|---------|
| 2026-01-14 | 初版作成 |
| 2026-01-14 | 残り作業・拡張機能セクション追加 |
| 2026-01-15 | Realtime Database → Firestore に変更 |
| 2026-01-15 | 応答時間フィールド追加（triggeredAt, completedAt, responseTimeSeconds） |
