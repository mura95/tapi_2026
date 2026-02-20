# 見守りアプリ リマインダー機能 仕様書

## 概要

見守りアプリ（Android）において、子どもが高齢の親のためにリマインダー通知を設定・確認する機能の仕様。

---

## 画面構成

```
見守りアプリ
├── ホーム画面
│   └── ペット状態確認画面
├── リマインダー履歴画面
│   └── 本日のリマインダー一覧（時間順）
├── 設定画面
│   └── リマインダー設定セクション
│       ├── リマインダー一覧
│       ├── 追加ボタン
│       └── リマインダー追加/編集画面
└── その他設定
```

**注意**: リマインダーがFirestoreに1件も登録されていない場合、ホーム画面にリマインダー履歴へのリンク/アイコンは表示しない。

---

## 1. リマインダー履歴画面

### 概要

本日のリマインダーを時間順に一覧表示し、完了/未完了の状態がわかる画面。
ペット状態確認画面と同様のシンプルな一覧形式。

### 表示内容

```
本日のリマインダー（2026年1月15日）

08:00  💊 朝のお薬        ✅ 完了（2分15秒）
10:00  💧 水分補給        ✅ 完了（5分30秒）
12:00  💊 昼のお薬        ⏳ 未完了
15:00  💧 水分補給        ─  予定
20:00  💊 夜のお薬        ─  予定
```

### 状態の種類

| 状態 | 表示 | 説明 |
|------|------|------|
| 完了 | ✅ 完了（応答時間） | success=true のログあり |
| 未完了 | ⏳ 未完了 | success=false のログあり（タイムアウト） |
| 予定 | ─ 予定 | まだ時間になっていない |
| 通知中 | 🔔 通知中 | 現在通知中（ログなし、時間経過済み） |

### 表示ロジック

1. Firestoreから本日の `reminders`（enabled=true）を取得
2. 各リマインダーの `times` を展開し、時間順にソート
3. 各時間について `reminderLog` を確認し、状態を決定
4. 時間順に一覧表示

---

## 2. 設定画面 - リマインダー設定セクション

### 概要

既存の設定画面内にリマインダー設定セクションを追加。

### 構成

```
設定画面
├── （既存の設定項目）
├── ─────────────────
├── リマインダー設定
│   ├── 💊 朝のお薬（毎日 8:00, 12:00, 20:00）  ON
│   ├── 💧 水分補給（毎日 10:00, 15:00）        ON
│   ├── 🏃 体操（月火水木金 9:00）              OFF
│   └── ＋ リマインダーを追加
└── （他の設定項目）
```

### リマインダー一覧の表示

- 各リマインダーをタップ → 編集画面へ遷移
- ON/OFFスイッチで即時有効/無効切り替え

---

## 3. リマインダー追加/編集画面

### 入力項目

| 項目 | 説明 |
|------|------|
| 種類 | 6種類から選択（服薬、食事、水分補給、運動、休憩、予定） |
| 表示名 | 任意の名前（最大20文字） |
| 通知時間 | 1〜10件の時間を設定 |
| 繰り返し | 毎日 or 曜日選択 |

### 種類（type）

| 値 | 表示 |
|----|------|
| medication | 服薬 |
| meal | 食事 |
| hydration | 水分補給 |
| exercise | 運動 |
| rest | 休憩 |
| appointment | 予定 |

### 曜日マッピング

```
日=0, 月=1, 火=2, 水=3, 木=4, 金=5, 土=6
空配列 = 毎日
```

### 削除

編集画面下部に「このリマインダーを削除」ボタンを配置。

---

## バリデーションルール

| 項目 | ルール |
|------|--------|
| 表示名 | 必須、20文字以内 |
| 通知時間 | 1件以上必須、重複不可 |
| 曜日選択時 | 1つ以上選択必須 |

### 件数制限

| 項目 | 上限 |
|------|------|
| リマインダー数 | 20件 |
| 1リマインダーあたりの通知時間 | 10件 |

---

## Firestore データ構造

### リマインダー設定
**Collection:** `users/{userId}/reminders`

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
  "daysOfWeek": [],
  "enabled": true,
  "createdAt": "Timestamp",
  "updatedAt": "Timestamp"
}
```

### リマインダーログ（読み取り専用）
**Collection:** `users/{userId}/reminderLog`

※ タップハウスから自動書き込み

```json
{
  "reminderId": "reminder_001",
  "type": "medication",
  "scheduledHour": 8,
  "scheduledMinute": 0,
  "triggeredAt": "2024-01-14T08:00:15Z",
  "completedAt": "2024-01-14T08:03:30Z",
  "responseTimeSeconds": 195,
  "success": true
}
```

| フィールド | 説明 |
|-----------|------|
| triggeredAt | 通知開始時刻 |
| completedAt | 完了/タイムアウト時刻 |
| responseTimeSeconds | 応答時間（秒） |
| success | true=完了, false=タイムアウト |

---

## Firestore 操作（Kotlin）

### リマインダー一覧取得

```kotlin
firestore.collection("users").document(userId)
    .collection("reminders")
    .get()
```

### リマインダー追加

```kotlin
val reminder = hashMapOf(
    "id" to reminderId,
    "type" to "medication",
    "displayName" to "朝のお薬",
    "times" to listOf(
        hashMapOf("hour" to 8, "minute" to 0)
    ),
    "daysOfWeek" to listOf<Int>(),
    "enabled" to true,
    "createdAt" to FieldValue.serverTimestamp(),
    "updatedAt" to FieldValue.serverTimestamp()
)

firestore.collection("users").document(userId)
    .collection("reminders").document(reminderId)
    .set(reminder)
```

### リマインダー更新

```kotlin
firestore.collection("users").document(userId)
    .collection("reminders").document(reminderId)
    .update(
        "displayName", newName,
        "times", newTimes,
        "updatedAt", FieldValue.serverTimestamp()
    )
```

### ON/OFF切り替え

```kotlin
firestore.collection("users").document(userId)
    .collection("reminders").document(reminderId)
    .update(
        "enabled", isEnabled,
        "updatedAt", FieldValue.serverTimestamp()
    )
```

### リマインダー削除

```kotlin
firestore.collection("users").document(userId)
    .collection("reminders").document(reminderId)
    .delete()
```

### 本日のログ取得（履歴画面用）

```kotlin
val todayStart = // 本日0:00のTimestamp

firestore.collection("users").document(userId)
    .collection("reminderLog")
    .whereGreaterThanOrEqualTo("completedAt", todayStart)
    .get()
```

### リマインダー存在チェック（アイコン表示判定）

```kotlin
firestore.collection("users").document(userId)
    .collection("reminders")
    .limit(1)
    .get()
    .addOnSuccessListener { snapshot ->
        val hasReminders = !snapshot.isEmpty
        // hasReminders が true の場合のみアイコン表示
    }
```

---

## Kotlin データクラス

```kotlin
import com.google.firebase.Timestamp

data class ReminderTime(
    val hour: Int = 0,
    val minute: Int = 0
)

data class Reminder(
    val id: String = "",
    val type: String = "medication",
    val displayName: String = "",
    val times: List<ReminderTime> = emptyList(),
    val daysOfWeek: List<Int> = emptyList(),
    val enabled: Boolean = true,
    val createdAt: Timestamp? = null,
    val updatedAt: Timestamp? = null
)

data class ReminderLog(
    val reminderId: String = "",
    val type: String = "",
    val scheduledHour: Int = 0,
    val scheduledMinute: Int = 0,
    val triggeredAt: Timestamp? = null,
    val completedAt: Timestamp? = null,
    val responseTimeSeconds: Int = 0,
    val success: Boolean = false
)

enum class ReminderType(val value: String, val displayName: String) {
    MEDICATION("medication", "服薬"),
    MEAL("meal", "食事"),
    HYDRATION("hydration", "水分補給"),
    EXERCISE("exercise", "運動"),
    REST("rest", "休憩"),
    APPOINTMENT("appointment", "予定")
}
```

### 応答時間フォーマット

```kotlin
fun formatResponseTime(seconds: Int): String {
    return when {
        seconds < 60 -> "${seconds}秒"
        seconds < 3600 -> {
            val min = seconds / 60
            val sec = seconds % 60
            "${min}分${sec}秒"
        }
        else -> {
            val hour = seconds / 3600
            val min = (seconds % 3600) / 60
            "${hour}時間${min}分"
        }
    }
}
```

---

## 将来の拡張

| 機能 | 説明 |
|------|------|
| プッシュ通知連携 | 設定変更をFCMでタップハウスに即時反映 |
| 異常検知アラート | 応答が遅い場合に見守り側にプッシュ通知 |
| 統計・グラフ表示 | 完了率・応答時間の推移 |
| カスタムメッセージ | 通知時のメッセージをカスタマイズ |

---

## 変更履歴

| 日付 | 変更内容 |
|------|---------|
| 2026-01-15 | 初版作成 |
| 2026-01-15 | 応答時間フィールドを追加 |
| 2026-01-15 | 仕様簡素化（履歴画面を本日一覧のみに、設定画面を既存画面内に統合） |
