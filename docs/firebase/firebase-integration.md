# Firebase連携システム ドキュメント

## 概要

TapHouseアプリケーションはFirebase Realtime Database、Firestore、Firebase Authenticationを使用してクラウド同期機能を実装しています。

### 関連ファイル

| ファイル | 役割 |
|---------|------|
| `FirebaseManager.cs` | Firebase連携の中央オーケストレーター |
| `LoginFormUI.cs` | 認証UI・ログイン処理 |
| `UserNameSettings.cs` | ユーザー設定・ログアウト管理 |
| `SecurePlayerPrefs.cs` | 暗号化されたローカルストレージ |
| `PrefsKeys.cs` | PlayerPrefsキーの一元管理 |

---

## 1. データベース構造

### Realtime Database

```
users/
└── {userId}/
    ├── state                 (String)    現在のペット状態
    ├── feedLog/
    │   ├── timestamp        (Long)       Unix時間（秒）
    │   ├── feedScale        (Float)      餌の量（1.0 = 通常）
    │   └── success          (Boolean)    成功フラグ
    ├── playLog/
    │   ├── angle            (Float)      投げ角度（度）
    │   ├── speed            (Float)      投げ速度
    │   ├── timestamp        (Long)       Unix時間（秒）
    │   └── success          (Boolean)    成功フラグ
    └── skillLogs/
        └── {log-id}/
            ├── action       (String)     アクション名
            └── timestamp    (Long)       Unix時間（秒）
```

### Firestore

```
users/
└── {userId}/
    └── logs/                             アクティビティログコレクション
        └── {doc-id}/
            ├── action       (String)     アクション説明
            └── timestamp    (Timestamp)  サーバータイムスタンプ
```

---

## 2. 認証フロー

### ログインシーケンス

```
アプリ起動
    │
    ├─→ LoginFormUI.Start()
    │   │
    │   ├─→ SecurePlayerPrefs.MigratePlaintextPassword()
    │   │   （古い平文パスワードを暗号化形式に移行）
    │   │
    │   ├─→ FirebaseAuth.CurrentUserを確認
    │   │   │
    │   │   ├─→ [ユーザーあり] → メインシーンへ遷移
    │   │   │
    │   │   └─→ [ユーザーなし] → 自動ログイン試行
    │   │       │
    │   │       ├─→ [保存された認証情報あり]
    │   │       │   └─→ SignInWithEmailAndPasswordAsync()
    │   │       │
    │   │       └─→ [認証情報なし] → ログインUI表示
    │   │
    │   └─→ [ログイン成功]
    │       ├─→ 認証情報を保存（パスワードは暗号化）
    │       └─→ メインシーンへ遷移
```

### 認証情報の保存

| キー | 暗号化 | 説明 |
|-----|--------|------|
| Email | なし | メールアドレス |
| Password | AES-256 | パスワード（デバイス固有キーで暗号化） |
| UserId | なし | Firebase UID |
| DisplayName | なし | 表示名 |

### パスワード暗号化（SecurePlayerPrefs）

- **暗号化方式:** AES-256 (CBC mode)
- **キー生成:** `SystemInfo.deviceUniqueIdentifier` をSHA256でハッシュ化
- **IV:** 毎回ランダム生成、暗号文の先頭に付加
- **エンコーディング:** Base64

---

## 3. リアルタイムリスナー

### 接続状態監視

```csharp
// .info/connected を監視
FirebaseDatabase.DefaultInstance
    .GetReference(".info/connected")
    .ValueChanged += OnConnectionStateChanged;
```

- `FirebaseManager.IsConnected` プロパティを更新
- オフライン時はFirebase操作をスキップ

### ペット状態変更リスナー

**監視パス:** `users/{userId}/` の `playLog` と `feedLog`

| イベント | アクション | 検証 |
|---------|----------|------|
| playLog変更 | `PlayManager.ThrowToy(speed, angle)` | タイムスタンプ ±10秒 |
| feedLog変更 | `EatAnimationController.AnimeEating(scale)` | タイムスタンプ ±10秒 |

### リモートアクションリスナー

**監視パス:** `users/{userId}/skillLogs`

**対応アクション:**

| action値 | 実行メソッド |
|----------|-------------|
| `ote` | `DogController.ActionRPaw()` |
| `okawari` | `DogController.ActionLPaw()` |
| `dance` | `DogController.ActionDance()` |
| `bang` | `DogController.ActionDang()` |
| `settings` | `DogController.ActionStand()` |
| `lie_down` | `DogController.ActionLieDown()` |
| `high_dance` | `DogController.ActionHighDance()` |

### タイムスタンプ検証

```csharp
long unixTimeNow = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
long logUnixTime = Convert.ToInt64(snapshot.Child("timestamp").Value);
if (Math.Abs(unixTimeNow - logUnixTime) > 10) return; // 10秒以上古い/先の場合は拒否
```

**目的:** 古いコマンドやリプレイ攻撃を防止

---

## 4. 書き込み操作

### 操作一覧

| 操作 | メソッド | パス | タイムアウト |
|------|---------|------|-------------|
| ペット状態更新 | `UpdatePetState()` | `state` | 10秒 |
| アクティビティログ | `UpdateLog()` | `logs/` (Firestore) | 10秒 |
| 空腹状態同期 | `UpdateHungerStateWithTimestamp()` | `feedLog` | 10秒 |
| 表示名取得 | `GetDisplayNameAsync()` | Firestore | 10秒 |

### Fire-and-Forgetパターン

```csharp
if (!_isConnected) {
    GameLogger.Log("[Firebase] Offline - state will sync when reconnected");
    return;
}
_ = UpdatePetStateAsync(stateName); // awaitしない
```

---

## 5. ゲームシステムとの連携

### 呼び出し箇所

| ファイル | 呼び出し | 状態値 |
|---------|---------|--------|
| `EatAnimationController.cs` | `UpdatePetState()` / `UpdateLog("feed")` | `idle` |
| `GetBall.cs` | `UpdatePetState()` / `UpdateLog("play")` | `ball` |
| `TouchController.cs` | `UpdateLog("skill")` | - |
| `SleepController.cs` | `UpdatePetState()` | `sleeping` / `idle` |
| `SleepController.Nap.cs` | `UpdatePetState()` | `napping` |

---

## 6. オフライン対応

| コンポーネント | オフライン動作 |
|---------------|---------------|
| ペット状態更新 | Firebase SDKが自動キュー、再接続時に同期 |
| アクティビティログ | スキップ（重要度低） |
| 接続監視 | `.info/connected` で自動監視 |
| 空腹状態 | ローカルのPlayerPrefsにフォールバック |
| 表示名 | "Admin" をデフォルト値として返却 |

---

## 7. エラーハンドリング

### Null安全性

```csharp
_firebaseManager?.UpdateLog("action");  // Null条件演算子
_firebaseManager?.UpdatePetState("state");
```

### 例外処理パターン

```csharp
try {
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
    var task = FirebaseDatabase.DefaultInstance.GetReference(statePath).SetValueAsync(stateName);
    var completedTask = await Task.WhenAny(task, Task.Delay(-1, cts.Token));

    if (completedTask != task) {
        GameLogger.LogWarning("[Firebase] UpdatePetState timeout - will retry on reconnect");
        return;
    }
    if (task.IsFaulted) {
        GameLogger.LogWarning($"[Firebase] UpdatePetState failed: {task.Exception?.Message}");
    }
} catch (OperationCanceledException) {
    GameLogger.LogWarning("[Firebase] UpdatePetState cancelled");
} catch (Exception ex) {
    GameLogger.LogWarning($"[Firebase] UpdatePetState error: {ex.Message}");
}
```

---

## 8. データフロー図

### ユーザーアクション → Firebase

```
ユーザーアクション（なでる、餌やり、遊び）
    │
    ├─→ DogController / EatAnimationController / PlayManager
    │   │
    │   ├─→ GameLogger.Log()
    │   │
    │   └─→ FirebaseManager.UpdatePetState("state_name")
    │       │
    │       ├─→ [オフライン] → スキップ（ローカル状態は更新）
    │       │
    │       └─→ [オンライン] → UpdatePetStateAsync()
    │           │
    │           └─→ FirebaseDatabase.SetValueAsync(basePath/state)
    │               （10秒タイムアウト、リトライなし）
    │
    └─→ FirebaseManager.UpdateLog("action")
        │
        ├─→ [オフライン] → スキップ
        │
        └─→ [オンライン] → FirebaseFirestore.AddAsync(logs)
            （10秒タイムアウト、リトライなし）
```

### リモートアクション → ローカル実行

```
Firebaseコンソール / 外部API
    │
    ├─→ skillLogs/{log-id} に書き込み
    │   {action: "ote", timestamp: 1234567890}
    │
    └─→ playLog に書き込み
        {angle: 45.0, speed: 10.0, timestamp: 1234567890}

FirebaseManager リアルタイムリスナー
    │
    ├─→ .ChildAdded skillLogsPath
    │   │
    │   └─→ タイムスタンプ検証（±10秒）
    │       │
    │       ├─→ [有効] → ExecuteAction(action)
    │       │   └─→ DogController.Action*()
    │       │
    │       └─→ [無効] → スキップ
    │
    └─→ .ChildChanged basePath (playLog/feedLog)
        │
        └─→ タイムスタンプ検証
            ├─→ [playLog有効] → PlayManager.ThrowToy(speed, angle)
            └─→ [feedLog有効] → EatAnimationController.AnimeEating(scale)
```

---

## 9. セキュリティ考慮事項

### 実装済みのセキュリティ対策

1. **パスワード暗号化** - AES-256、デバイス固有キー
2. **認証** - Firebase AuthによるHTTPS通信
3. **タイムスタンプ検証** - ±10秒ウィンドウでリプレイ攻撃防止
4. **デバイスバインディング** - 暗号化キーはdeviceUniqueIdentifierから生成

### 潜在的なリスク

| リスク | 説明 | 推奨対策 |
|-------|------|---------|
| 固定タイムアウト | 全操作が10秒固定、遅いネットワークで失敗 | 指数バックオフの実装 |
| 汎用エラーメッセージ | ログイン失敗原因が分からない | エラー種別の細分化 |
| トークン更新なし | 長時間セッションで無効化の可能性 | トークン監視の実装 |
| リスナー未解除 | シーン再読み込みでメモリリーク | OnDestroyでの解除 |

---

## 10. ローカル永続化

### 保存データ一覧

| キー | 型 | 用途 | 暗号化 |
|-----|---|------|-------|
| Email | String | 自動ログイン | なし |
| Password | String | 自動ログイン | AES-256 |
| UserId | String | ユーザー識別 | なし |
| DisplayName | String | UI表示 | なし |
| SleepHour | Int | 睡眠スケジュール | なし |
| WakeHour | Int | 起床スケジュール | なし |
| HungerState | Int | 空腹レベル | なし |
| LastEatTime | Long | 空腹トラッキング | なし |
| LastInteractionTime | Binary | 愛情度ペナルティ計算 | なし |

---

## 11. 改善提案

### 優先度高

1. **トークン更新監視の実装**
   - `FirebaseAuth.CurrentUser?.IsValid` を監視
   - トークン期限切れ時の再認証フロー追加

2. **指数バックオフリトライの実装**
   - 1秒 → 2秒 → 4秒 → 8秒 → 16秒（最大）
   - ネットワーク一時障害への耐性向上

3. **リスナー解除の実装**
   ```csharp
   void OnDestroy() {
       FirebaseDatabase.DefaultInstance
           .GetReference(".info/connected")
           .ValueChanged -= OnConnectionStateChanged;
       // 他のリスナーも同様に解除
   }
   ```

4. **認証エラーメッセージの細分化**
   - ネットワークエラー
   - 認証情報エラー
   - アカウント未登録

### 優先度中

5. **サーバータイムスタンプの使用**
   - クライアント時刻のずれに依存しない検証

6. **Firestoreログのクリーンアップ**
   - 90日以上前のログを自動削除
   - Cloud Functionsでの定期実行

7. **同時書き込み対策**
   - トランザクション使用
   - またはパス `feedLog/{timestamp}` で上書き防止

---

## 12. デバッグ方法

### Firebaseコンソールでの確認

1. Firebase Console → Realtime Database → `users/{userId}`
2. 状態値、ログ、タイムスタンプを直接確認可能

### ログ確認

```csharp
GameLogger.Log(LogCategory.Firebase, "メッセージ");
GameLogger.LogWarning(LogCategory.Firebase, "警告");
GameLogger.LogError(LogCategory.Firebase, "エラー");
```

### テスト用リモートアクション

Firebase Consoleから直接skillLogsにエントリを追加:
```json
{
  "action": "dance",
  "timestamp": 1234567890  // 現在のUnix時間
}
```

---

## 変更履歴

| 日付 | 変更内容 |
|------|---------|
| 2026-01-13 | 初版作成 |
