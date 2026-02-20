# スリープシステム仕様書

## 概要

犬（ペット）の睡眠サイクルを管理するシステム。夜間の睡眠と昼寝（お昼寝）の2種類の睡眠状態をサポートし、時刻に基づいて自動的に睡眠・起床を制御します。

## ファイル構成

| ファイル | 役割 |
|----------|------|
| `SleepController.cs` | メインコントローラー、初期化・状態管理 |
| `SleepController.Scheduling.cs` | スケジュール管理（睡眠・起床のタイミング制御） |
| `SleepController.Check.cs` | Overdue（遅延）検出と自動修正 |
| `SleepController.Nap.cs` | 昼寝機能の管理 |
| `TimeZoneProvider.cs` | タイムゾーンの一元管理 |

## 夜間睡眠

### 基本動作

1. **就寝時刻**（デフォルト: 22時）になると自動的に睡眠状態に移行
2. **起床時刻**（デフォルト: 6時）になると自動的に起床
3. アプリがバックグラウンドでも`AndroidAlarmScheduler`により起床

### 状態遷移

```
[idle] ---(就寝時刻)--> [sleeping] ---(起床時刻)--> [idle]
```

### 設定項目

| 項目 | PlayerPrefsキー | デフォルト値 | 説明 |
|------|-----------------|-------------|------|
| 就寝時刻 | `SleepHour` | 22 | 0-23時（最大23時） |
| 起床時刻 | `WakeHour` | 6 | 0-23時 |

### スケジュール方式

- **フォアグラウンド**: `Coroutine + WaitForSecondsRealtime`（軽量・信頼性高）
- **バックグラウンド/アプリ終了後**: `AndroidAlarmScheduler`（システムアラーム使用）

## 昼寝（お昼寝）

### 基本動作

1. 犬があくびをするとNapCount（眠気カウンター）が+1
2. NapCountが15に達し、昼寝時間帯内であれば昼寝開始
3. 昼寝終了時刻になると自動的に起床
4. 昼寝中にタッチすると50%の確率で起きる（NapCount-1）

### 状態遷移

```
[idle] ---(NapCount=15 & 昼寝時間帯)--> [napping] ---(終了時刻 or タッチ)--> [idle]
```

### 設定項目

| 項目 | PlayerPrefsキー | デフォルト値 | 説明 |
|------|-----------------|-------------|------|
| 昼寝開始時刻 | `NapSleepHour` | 13 | 昼寝可能な開始時刻 |
| 昼寝終了時刻 | `NapWakeHour` | 15 | 昼寝終了・NapCountリセット時刻 |
| NapCount | `NapCount` | 0 | 現在の眠気カウンター（0-15） |
| 最終リセット時刻 | `LastNapEndTime` | - | ISO 8601形式で保存 |

### NapCountリセット条件

毎日`NapWakeHour`（デフォルト15時）を超えるとNapCountが0にリセットされる。

判定ロジック:
```
前回リセット時刻 < 今日のNapWakeHour <= 現在時刻
```

## 時間帯の制約

### 重要な制約事項

- **昼寝と夜睡眠の時間帯は重複しないよう設計**
- 昼寝: 13時～15時（固定範囲）
- 夜睡眠: 22時～6時（設定可能、最大23時まで）
- `sleepHour`を15時以前に設定すると昼寝との競合が発生

### 時間帯判定ロジック

```csharp
bool isSleepTime = (now.Hour >= sleepHour || now.Hour < wakeHour);
```

## Overdue（遅延）検出

### 目的

アプリがバックグラウンドにあった場合など、予定時刻を過ぎてもスケジュールが実行されなかった場合の自動修正。

### 監視間隔

- チェック間隔: 60秒
- Overdue閾値: 10分

### 動作

1. 60秒ごとにスケジュール状態をチェック
2. 予定時刻から10分以上経過していれば強制実行
3. 重複実行防止のため、実行前にフラグをクリア

## Android固有の動作

### AlarmScheduler

- `AndroidAlarmScheduler`: Androidのシステムアラームを使用
- アプリ終了後も起床アラームが動作
- バッテリー最適化の除外設定を推奨

### バッテリー最適化

```java
// 設定画面を開いてバッテリー最適化を無効化
android.settings.REQUEST_IGNORE_BATTERY_OPTIMIZATIONS
```

## タイムゾーン管理

### TimeZoneProvider

アプリ全体で使用するタイムゾーンを一元管理するstaticクラス。

```csharp
// 現在時刻の取得（タイムゾーン変換済み）
DateTime now = TimeZoneProvider.Now;

// 現在のタイムゾーン
TimeZoneInfo tz = TimeZoneProvider.Current;
```

### Android対応

- システムタイムゾーン一覧の取得に失敗した場合、主要なタイムゾーンのハードコードリストをフォールバック
- 日本（Asia/Tokyo）を含む22個の主要タイムゾーンを保持

## 画面制御

### 睡眠時

- 画面輝度を下げる（`BrightnessUtil.SetBrightness.DoAction(-1f)`）
- 画面スリープを許可（`SleepTimeout.SystemSetting`）
- UIボタンを非表示

### 起床時

- 画面輝度を戻す（`BrightnessUtil.SetBrightness.DoAction(1.0f)`）
- 画面スリープを無効化（`SleepTimeout.NeverSleep`）
- UIボタンを表示

## Android電源管理（AndroidPowerService）

### ファイル

`Assets/Scripts/Sleep/Power/AndroidPowerService.cs`

### 概要

Androidの画面ON/OFF制御とWakeLock管理を行うサービス。

### 主要メソッド

| メソッド | 役割 | 呼び出し元 |
|----------|------|-----------|
| `TurnScreenOn()` | 画面を起動し、WakeLockを取得 | `WakeUp()` |
| `ClearKeepScreenOn()` | 画面スリープを許可し、WakeLockをリリース | `StartSleeping()` |
| `Release()` | WakeLockを完全に解放 | `OnDestroy()` |

### WakeLock管理

```
起床時: TurnScreenOn()
  ├─ FLAG_SHOW_WHEN_LOCKED 追加
  ├─ FLAG_TURN_SCREEN_ON 追加
  ├─ FLAG_KEEP_SCREEN_ON 追加
  └─ WakeLock acquire（SCREEN_BRIGHT_WAKE_LOCK | ACQUIRE_CAUSES_WAKEUP）

睡眠時: ClearKeepScreenOn()
  ├─ WakeLock release ← 重要：これがないと端末がスリープしない
  └─ FLAG_KEEP_SCREEN_ON クリア
```

### 重要な注意点

**WakeLockは必ずリリースが必要**

`TurnScreenOn()`で取得したWakeLockを`ClearKeepScreenOn()`でリリースしないと、端末が自動スリープしない問題が発生する。

```csharp
// ClearKeepScreenOn()の最初で必ずWakeLockをリリース
public void ClearKeepScreenOn()
{
    ReleaseWakeLock();  // ← これが必須
    // FLAG_KEEP_SCREEN_ONのクリア処理...
}
```

### フラグ定数

| フラグ | 値 | 説明 |
|--------|-----|------|
| `FLAG_KEEP_SCREEN_ON` | 0x00000080 | 画面を常時ONに保つ |
| `FLAG_SHOW_WHEN_LOCKED` | 0x00080000 | ロック画面上に表示 |
| `FLAG_TURN_SCREEN_ON` | 0x00200000 | 画面をONにする |
| `SCREEN_BRIGHT_WAKE_LOCK` | 0x0000000a | 画面を明るく保つWakeLock |
| `ACQUIRE_CAUSES_WAKEUP` | 0x10000000 | 取得時に画面を起動 |

## デバッグ機能

### 自動テストモード

`_autoTestMode = true`で有効化。3サイクルの睡眠→起床を自動実行。

```
テストサイクル:
1. 2分後にスリープ
2. 3分後に起床
3. 繰り返し（3回）
```

### ContextMenuコマンド

```csharp
[ContextMenu("Set Hunger State to Hungry")]
public void DebugSetHungerToHungry()
```

## 関連クラス

| クラス | 役割 |
|--------|------|
| `DogController` | 犬のアニメーション制御 |
| `FirebaseManager` | 状態のFirebase同期 |
| `MainUIButtons` | UIボタンの表示/非表示 |
| `BrightnessUtil` | 画面輝度制御 |
| `AndroidAlarmScheduler` | Androidシステムアラーム |
| `EditorAlarmScheduler` | エディタ用アラーム（Invoke使用） |
