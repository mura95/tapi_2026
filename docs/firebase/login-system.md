# ログインシステム仕様書

## 概要

Firebase Authenticationを使用したメール/パスワード認証システム。自動ログイン機能を備え、認証情報をPlayerPrefsに保存して次回起動時に自動的にログインを試行する。

## ファイル構成

| ファイル | 役割 |
|----------|------|
| `LoginFormUI.cs` | ログイン画面のUI制御、認証処理 |
| `LoginPasswordToggle.cs` | パスワード表示/非表示の切り替え |
| `FirebaseUIManager.cs` | 設定画面、ログアウト処理（UserNameSettings.cs） |
| `SecurePlayerPrefs.cs` | パスワード暗号化ストレージ |
| `PrefsKeys.cs` | PlayerPrefsキーの一元管理 |

## 認証フロー

### 初回起動時

```
アプリ起動
    ↓
LoginFormUI.Start()
    ↓
Firebase.CurrentUser チェック
    ↓ (null)
PlayerPrefs に保存された認証情報チェック
    ↓ (なし)
ログインフォーム表示
    ↓
ユーザーがメール/パスワード入力
    ↓
SignInWithEmailAndPasswordAsync()
    ↓ (成功)
PlayerPrefs に認証情報を平文保存 ⚠️
    ↓
Main シーンへ遷移
```

### 2回目以降（自動ログイン）

```
アプリ起動
    ↓
LoginFormUI.Start()
    ↓
Firebase.CurrentUser チェック
    ↓ (null - セッション切れ)
PlayerPrefs に保存された認証情報チェック
    ↓ (あり)
TryAutoLogin() → SignInWithEmailAndPasswordAsync()
    ↓ (成功)
Main シーンへ遷移
```

### ログアウト

```
設定画面でログアウトボタン押下
    ↓
FirebaseUIManager.Logout()
    ↓
firebaseAuth.SignOut()
    ↓
PlayerPrefs から認証情報を削除
    ↓
login シーンへ遷移
```

## 保存されるデータ

| キー | 型 | 内容 | セキュリティ |
|------|-----|------|-------------|
| `Email` | string | メールアドレス | ⚠️ 平文 |
| `Password` | string | パスワード | ⚠️ **平文（危険）** |
| `UserId` | string | Firebase UID | 平文 |
| `DisplayName` | string | 表示名 | 平文 |
| `LastLoginTime` | string | 最終ログイン日時 | 平文 |

## 問題点と評価

### セキュリティ問題（重大）

| 問題 | 深刻度 | 説明 |
|------|--------|------|
| **パスワード平文保存** | 🔴 Critical | PlayerPrefsは暗号化されておらず、root権限で読み取り可能 |
| **自動ログインの脆弱性** | 🟠 High | 端末紛失時に認証情報が漏洩 |
| **セッション管理なし** | 🟡 Medium | Firebase IDトークンの有効期限管理がない |

### 設計上の問題（すべて解決済み）

| 問題 | 状況 | 対応 |
|------|------|------|
| **LoginManager未使用** | ✅ 解決 | 削除済み |
| **キー定義の分散** | ✅ 解決 | `PrefsKeys.cs` で一元化 |
| **エラーハンドリング不足** | ✅ 解決 | オフライン対応実装 |

### Android固有の問題

```
PlayerPrefsの保存場所:
/data/data/jp.co.pichipichi.petdisplay/shared_prefs/jp.co.pichipichi.petdisplay.v2.playerprefs.xml

root権限があれば読み取り可能:
<string name="Password">user_password_here</string>
```

## 推奨される改善

### 1. パスワードを保存しない（推奨）

Firebase AuthはIDトークンをキャッシュするため、パスワード保存は不要。

```csharp
// 改善後
void SaveAutoLogin(string email, string userId)
{
    PlayerPrefs.SetString(EMAIL_KEY, email);
    PlayerPrefs.SetString(USER_ID_KEY, userId);
    // パスワードは保存しない
}

bool TryAutoLogin()
{
    // Firebase の CurrentUser を確認するだけ
    return firebaseAuth.CurrentUser != null;
}
```

### 2. パスワード暗号化（実装済み）

```csharp
// SecurePlayerPrefs を使用（AES-256暗号化）
SecurePlayerPrefs.SetString("Password", password);
```

`Assets/Scripts/Utilities/SecurePlayerPrefs.cs` で実装済み。

### 3. セッション維持について

Firebase SDKは自動的にトークンを更新するため、**毎日アプリを使用していればセッション切れは発生しません**。

| 条件 | 結果 |
|------|------|
| 毎日スリープ/起床 | ✅ Firebase書き込みでトークン自動更新 |
| 1年以上未使用 | ⚠️ Refresh Token期限切れ |
| パスワード変更 | ⚠️ 再ログイン必要 |

## 代替認証方式の検討

| 方式 | メリット | デメリット |
|------|----------|------------|
| **Firebase ID Token のみ** | パスワード保存不要、セキュア | トークン期限切れ時の再認証必要 |
| **Biometric認証** | 高セキュリティ、UX良好 | デバイス依存、実装複雑 |
| **Google Sign-In** | パスワード管理不要 | Google依存 |
| **Anonymous Auth → Link** | 初回簡単、後でアカウント紐付け | 実装複雑 |

## 現在のコード評価

### LoginFormUI.cs

```
設計: ★★★☆☆ (3/5)
- async/awaitを適切に使用
- UIステートマシンが明確
- エラーハンドリングが基本的

セキュリティ: ★☆☆☆☆ (1/5)
- パスワード平文保存は致命的

保守性: ★★★☆☆ (3/5)
- キー定義が散在
- ログアウト処理が別ファイル
```


## 実装優先度

1. ✅ **Critical**: パスワードの暗号化 → `SecurePlayerPrefs.cs` で実装済み
2. ✅ **High**: キー定義の集約 → `PrefsKeys.cs` で実装済み
3. 🟡 **Medium**: LoginManagerの削除/統合
4. 🟢 **Low**: Biometric認証の追加検討

---

## オフライン対応（実装済み）

`FirebaseManager.cs` にオフライン対応を実装済み。

### 接続状態監視

```csharp
// Firebase接続状態の監視
FirebaseDatabase.DefaultInstance
    .GetReference(".info/connected")
    .ValueChanged += OnConnectionChanged;

// 他クラスから参照可能
public static bool IsConnected => _isConnected;
```

### 動作仕様

| 状態 | 動作 |
|------|------|
| **オンライン** | 通常通りFirebase読み書き（10秒タイムアウト） |
| **オフライン** | ローカル状態を即座に更新、Firebase書き込みはスキップ |
| **復旧時** | Firebase Realtime DBのオフラインキャッシュが自動同期 |

### タイムアウト保護

すべてのFirebaseリクエストに10秒のタイムアウトを設定：

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
var completedTask = await Task.WhenAny(task, Task.Delay(-1, cts.Token));
```

これにより、WiFi切断時でもアプリがハングすることはありません。
