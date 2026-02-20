# グローバル状態管理 仕様書

## 概要

本プロジェクトでは、アプリ全体で共有される状態を以下の2つの方法で管理しています：

1. **GlobalVariables** - ランタイム中のみ有効なメモリ上の状態
2. **PlayerPrefs** - 永続化されるローカルストレージ

---

## GlobalVariables（静的変数）

`Assets/Scripts/Variables/GlobalVariables.cs`

### 変数一覧

| 変数名 | 型 | デフォルト値 | 説明 | 使用箇所 |
|--------|-----|-------------|------|----------|
| `IsInputUserName` | bool | false | ユーザー名入力中フラグ | UserNameSettings, TouchController |
| `CurrentHungerState` | HungerState | Hungry | 現在の空腹状態 | HungerManager, DogStateController, UI |
| `AttentionCount` | int | 0 | 飽き（注目）カウンター | FaceManager, TouchController, PlayToy |
| `isMoving` | bool | false | 移動中フラグ | - |
| `CurrentState` | PetState | idle | 現在のペット状態 | 全システム共通 |
| `CurrentStateIndex` | int | 0 | CurrentStateの数値表現（読み取り専用） | - |
| `CurrentHungerStateIndex` | int | - | CurrentHungerStateの数値表現（読み取り専用） | UI |

### PetState（ペット状態）

```csharp
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
}
```

### HungerState（空腹状態）

```csharp
public enum HungerState
{
    Full,       // 満腹
    MediumHigh, // やや満腹
    MediumLow,  // やや空腹
    Hungry      // 空腹
}
```

---

## PlayerPrefs（永続化データ）

### ユーザー・認証関連

| キー | 型 | デフォルト | 説明 | 管理クラス |
|------|-----|-----------|------|------------|
| `DisplayName` | string | - | 表示名 | UserNameSettings |
| `UserId` | string | - | ユーザーID | LoginFormUI, UserNameSettings |
| `Email` | string | - | メールアドレス | LoginFormUI, UserNameSettings |
| `Password` | string | - | パスワード | LoginFormUI, UserNameSettings |
| `LastLoginDate` | string | - | 最終ログイン日時 | UserNameSettings |

### 睡眠・スケジュール関連

| キー | 型 | デフォルト | 説明 | 管理クラス |
|------|-----|-----------|------|------------|
| `SleepHour` | int | 22 | 就寝時刻（0-23） | SleepController, UserNameSettings |
| `WakeHour` | int | 6 | 起床時刻（0-23） | SleepController, DailyWakeScheduler |
| `NapSleepHour` | int | 13 | 昼寝開始時刻 | SleepController.Nap, UserNameSettings |
| `NapWakeHour` | int | 15 | 昼寝終了時刻 | SleepController.Nap, UserNameSettings |
| `NapCount` | int | 0 | 眠気カウンター（0-15） | SleepController.Nap |
| `LastNapEndTime` | string | "" | 最終昼寝リセット時刻（ISO 8601） | SleepController.Nap |
| `NextWakeEpochMs` | string | "" | 次回起床時刻（Unix epoch ms） | AndroidAlarmScheduler, EditorAlarmScheduler |

### 空腹・食事関連

| キー | 型 | デフォルト | 説明 | 管理クラス |
|------|-----|-----------|------|------------|
| `HungerState` | int | 3 (Hungry) | 空腹状態（enum値） | HungerManager |
| `LastEatTime` | string | - | 最終食事時刻（Unix epoch sec） | HungerManager |

### 愛情・インタラクション関連

| キー | 型 | デフォルト | 説明 | 管理クラス |
|------|-----|-----------|------|------------|
| `LastInteractionTime` | string | - | 最終インタラクション時刻（Binary DateTime） | LoveManager |

### 体型・外見関連

| キー | 型 | デフォルト | 説明 | 管理クラス |
|------|-----|-----------|------|------------|
| `DogBodyScale` | float | 1.0 | 犬の体型スケール | DogBodyShapeManager |
| `LastHungryCheck` | string | - | 最終空腹チェック時刻（Binary DateTime） | DogBodyShapeManager |
| `ConsecutiveFeedings` | int | 0 | 連続満腹回数 | DogBodyShapeManager |
| `DogCoat` | int | 0 (Brown) | 犬の毛色 | DogMaterialSwitcher |

### タイムゾーン関連

| キー | 型 | デフォルト | 説明 | 管理クラス |
|------|-----|-----------|------|------------|
| `timezoneId` | string | - | タイムゾーンID | TimeZoneProvider |
| `SelectedTimeZoneId` | string | - | 選択されたタイムゾーンID | TimeZoneUtility |
| `TimezoneIndex` | int | 0 | タイムゾーンドロップダウンのインデックス | UserNameSettings |

### UI・設定関連

| キー | 型 | デフォルト | 説明 | 管理クラス |
|------|-----|-----------|------|------------|
| `TabletMode` | int | 0 | タブレットモード（0/1） | UserNameSettings |
| `Language` | int | - | 言語設定 | LocalizationManager |

---

## Android SharedPreferences

Android固有の永続化（Java側で管理）

| ファイル名 | キー | 型 | 説明 |
|-----------|------|-----|------|
| `tap_alarm_cfg` | `wake_hour` | int | 起床時刻（時） |
| `tap_alarm_cfg` | `wake_min` | int | 起床時刻（分） |
| `tap_alarm_cfg` | `tz_id` | string | タイムゾーンID |
| `tap_alarm_cfg` | `tz_offset_min` | int | UTCオフセット（分） |
| `tap_alarm_cfg` | `next_epoch_ms` | long | 次回アラーム時刻（epoch ms） |

---

## 状態の依存関係

```
GlobalVariables.CurrentState
    ├── idle → 通常操作可能
    ├── sleeping/napping → タッチ制限、UI非表示
    ├── feeding/snack → 食事アニメーション中
    ├── ball/toy → 遊びシーケンス中
    ├── moving → 移動中
    ├── ready → UI選択待ち
    └── action → ボイスコマンド等実行中

GlobalVariables.CurrentHungerState
    ├── Full → 満腹アニメーション
    ├── MediumHigh/MediumLow → 通常
    └── Hungry → 空腹アニメーション、体型減少
```

---

## Firebase Realtime Database

リモート同期される状態（参考）

| パス | 説明 |
|------|------|
| `users/{userId}/state` | ペット状態 |
| `users/{userId}/feedLog` | 食事ログ |
| `users/{userId}/playLog` | 遊びログ |
| `users/{userId}/skillLogs` | リモートアクションログ |

---

## 注意事項

### PlayerPrefsの制限

1. **サイズ制限**: 大量のデータ保存には不向き
2. **同期なし**: デバイス間で共有されない

### GlobalVariablesの注意点

1. **スレッドセーフではない**: メインスレッド以外からのアクセスに注意
2. **永続化されない**: アプリ終了時に消失
3. **依存関係が不明確**: どこからでもアクセス可能なため追跡困難

---

## 実装済みの改善

### 1. キー定義の一元管理（実装済み）

`Assets/Scripts/Constants/PrefsKeys.cs`

```csharp
public static class PrefsKeys
{
    public const string Email = "Email";
    public const string Password = "Password";
    public const string SleepHour = "SleepHour";
    // ... 全キーを一元管理
}
```

### 2. パスワード暗号化（実装済み）

`Assets/Scripts/Utilities/SecurePlayerPrefs.cs`

```csharp
// 暗号化して保存
SecurePlayerPrefs.SetString(PrefsKeys.Password, password);

// 復号して取得
string password = SecurePlayerPrefs.GetString(PrefsKeys.Password);

// 削除
SecurePlayerPrefs.DeleteKey(PrefsKeys.Password);
```

**特徴:**
- AES-256-CBC暗号化
- デバイス固有のキー生成（SystemInfo.deviceUniqueIdentifier使用）
- 自動マイグレーション機能（平文→暗号化）

### 3. 自動マイグレーション

ログイン画面起動時に古い平文パスワードを自動的に暗号化形式に移行:

```csharp
// LoginFormUI.Start()で呼び出し
SecurePlayerPrefs.MigratePlaintextPassword();
```

---

## 今後の改善提案

### 推奨される追加改善策

```csharp
// 1. 型安全なラッパー
public class GameSettings
{
    public int SleepHour
    {
        get => PlayerPrefs.GetInt(PrefsKeys.SleepHour, 22);
        set => PlayerPrefs.SetInt(PrefsKeys.SleepHour, value);
    }
}

// 2. 状態管理のイベント化
public class PetStateManager
{
    public event Action<PetState> OnStateChanged;

    private PetState _state;
    public PetState State
    {
        get => _state;
        set
        {
            if (_state != value)
            {
                _state = value;
                OnStateChanged?.Invoke(value);
            }
        }
    }
}
```
