# たっぷポケット ブラウザログイン

## 1. 概要

たっぷポケット（スマホアプリ）では、OSのブラウザを使用してログイン・新規登録を行う。認証完了後、Deep Linkでアプリに戻り、Firebase Authでサインインする。

---

## 2. 認証フロー

```
┌─────────────────────────────────────────────────────────────┐
│              たっぷポケット 認証フロー                       │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌──────────────┐                                          │
│  │ アプリ起動    │                                          │
│  │ (Unity)      │                                          │
│  └──────┬───────┘                                          │
│         │                                                   │
│         ▼                                                   │
│  ┌──────────────┐                                          │
│  │ 認証状態確認  │                                          │
│  └──────┬───────┘                                          │
│         │                                                   │
│    ┌────┴────┐                                             │
│    │         │                                             │
│ 認証済み  未認証                                            │
│    │         │                                             │
│    ▼         ▼                                             │
│ ┌─────┐  ┌──────────────┐                                  │
│ │メイン│  │ モード選択    │ ← 「たっぷポケット」ボタン       │
│ │ 画面 │  │ (Unity)      │                                  │
│ └─────┘  └──────┬───────┘                                  │
│                 │ ボタン押下                                │
│                 ▼                                          │
│         ┌──────────────┐                                   │
│         │ OSブラウザ起動 │                                   │
│         │ Application  │                                   │
│         │ .OpenURL()   │                                   │
│         └──────┬───────┘                                   │
│                │                                           │
│  ══════════════╪═══════════════════════════════════════    │
│      アプリ外   │   OSブラウザ                              │
│  ══════════════╪═══════════════════════════════════════    │
│                ▼                                           │
│         ┌──────────────┐                                   │
│         │ Webログイン   │ Firebase Hosting                  │
│         │ ページ       │ https://taphouse.web.app/login    │
│         └──────┬───────┘                                   │
│                │                                           │
│       ┌────────┼────────┐                                  │
│       ▼        ▼        ▼                                  │
│    ┌─────┐ ┌─────┐ ┌─────────┐                            │
│    │ログ │ │新規 │ │Google   │                            │
│    │イン │ │登録 │ │サインイン│                            │
│    └──┬──┘ └──┬──┘ └────┬────┘                            │
│       │       │         │                                  │
│       └───────┴─────────┘                                  │
│                │                                           │
│                ▼                                           │
│         ┌──────────────┐                                   │
│         │ Firebase     │                                   │
│         │ Functions    │                                   │
│         │ カスタムトークン│                                  │
│         │ 生成          │                                   │
│         └──────┬───────┘                                   │
│                │                                           │
│                ▼                                           │
│         ┌──────────────┐                                   │
│         │ Deep Link    │                                   │
│         │ でアプリに戻る │                                   │
│         │ taphouse://  │                                   │
│         │ auth?token=xx│                                   │
│         └──────┬───────┘                                   │
│                │                                           │
│  ══════════════╪═══════════════════════════════════════    │
│      アプリ内   │   Unity                                   │
│  ══════════════╪═══════════════════════════════════════    │
│                ▼                                           │
│         ┌──────────────┐                                   │
│         │ Deep Link受信 │                                   │
│         │ DeepLinkHandler│                                  │
│         └──────┬───────┘                                   │
│                │                                           │
│                ▼                                           │
│         ┌──────────────┐                                   │
│         │ Firebase Auth │                                   │
│         │ SignInWith   │                                   │
│         │ CustomToken  │                                   │
│         └──────┬───────┘                                   │
│                │                                           │
│                ▼                                           │
│         ┌──────────────┐                                   │
│         │ PocketMain   │                                   │
│         │ .unity へ遷移 │                                   │
│         └──────────────┘                                   │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## 3. 技術構成

### 3.1 必要なコンポーネント

| コンポーネント | 役割 | 技術 |
|---------------|------|------|
| Webログインページ | ログイン/登録UI | Firebase Hosting + HTML/JS |
| Firebase Auth (Web) | Web側の認証 | Firebase JS SDK |
| Firebase Functions | カスタムトークン生成 | Node.js |
| Deep Link | アプリへのリダイレクト | Android intent-filter |
| DeepLinkHandler | Deep Link受信 | Application.deepLinkActivated |
| PocketAuthManager | 認証フロー管理 | Firebase Auth Unity SDK |

### 3.2 URL設計

| 用途 | URL |
|------|-----|
| Webログインページ | `https://taphouse.web.app/login` |
| 認証成功時のDeep Link | `taphouse://auth?token={customToken}` |
| 認証キャンセル時 | `taphouse://auth?cancelled=true` |
| 認証エラー時 | `taphouse://auth?error={errorCode}` |

---

## 4. ファイル構成

### 4.1 Unity側

```
Assets/Scripts/UI/Login/
├── DeepLinkHandler.cs      # Deep Link受信・パース
├── PocketAuthManager.cs    # ブラウザ認証管理
├── LoginFormUI.cs          # 既存：メールログインUI
├── LoginPasswordToggle.cs  # 既存
└── StrictAlphanumericValidator.cs  # 既存

Assets/Scripts/UI/ModeSelect/
├── ModeSelectManager.cs    # モード選択・ブラウザ起動
└── AppModeManager.cs       # 既存

Assets/Plugins/Android/
└── AndroidManifest.xml     # Deep Link intent-filter追加済み
```

### 4.2 Web側（別リポジトリ）

**[TapHouseWeb](../../../TapHouseWeb/)** リポジトリで管理

```
TapHouseWeb/
├── firebase.json           # Firebase Hosting + Functions設定
├── .firebaserc             # プロジェクト設定
├── package.json            # ルート（Hosting用）
├── src/
│   └── auth.ts             # 認証ロジック（TypeScript）
├── public/
│   ├── index.html          # ログインページ
│   └── css/
│       └── style.css       # スタイル
├── dist/                   # ビルド出力（Hosting）
└── functions/
    ├── package.json
    └── src/
        └── index.ts        # createCustomToken関数（TypeScript）
```

詳細仕様: [TapHouseWeb/docs/browser-login-spec.md](../../../TapHouseWeb/docs/browser-login-spec.md)

---

## 5. Unity実装詳細

### 5.1 DeepLinkHandler.cs

シングルトンパターンでDeep Linkを受信し、パラメータを解析してイベントを発火する。

```csharp
public class DeepLinkHandler : MonoBehaviour
{
    public event Action<string> OnTokenReceived;
    public event Action OnCancelled;
    public event Action<string> OnError;

    void Awake()
    {
        Application.deepLinkActivated += OnDeepLinkActivated;

        // 起動時のDeep Linkをチェック
        if (!string.IsNullOrEmpty(Application.absoluteURL))
        {
            OnDeepLinkActivated(Application.absoluteURL);
        }
    }

    private void OnDeepLinkActivated(string url)
    {
        var uri = new Uri(url);
        if (uri.Scheme != "taphouse" || uri.Host != "auth") return;

        var query = ParseQuery(uri.Query);

        if (query.TryGetValue("token", out var token))
            OnTokenReceived?.Invoke(token);
        else if (query.TryGetValue("cancelled", out _))
            OnCancelled?.Invoke();
        else if (query.TryGetValue("error", out var error))
            OnError?.Invoke(error);
    }
}
```

### 5.2 PocketAuthManager.cs

ブラウザ起動とカスタムトークン認証を管理する。

```csharp
public class PocketAuthManager : MonoBehaviour
{
    private const string LOGIN_URL = "https://taphouse.web.app/login";

    public void OpenBrowserLogin()
    {
        Application.OpenURL(LOGIN_URL);
    }

    public async Task<bool> SignInWithCustomToken(string token)
    {
        var auth = FirebaseAuth.DefaultInstance;
        await auth.SignInWithCustomTokenAsync(token);
        return auth.CurrentUser != null;
    }
}
```

### 5.3 ModeSelectManager.cs

モード選択画面でブラウザログインを呼び出す。

```csharp
public void OnTapPocketSelected()
{
    AppModeManager.Save(AppMode.TapPocket);
    ShowStatus("ブラウザでログイン中…");
    _pocketAuth.OpenBrowserLogin();
}
```

### 5.4 AndroidManifest.xml（Deep Link設定）

```xml
<intent-filter>
    <action android:name="android.intent.action.VIEW" />
    <category android:name="android.intent.category.DEFAULT" />
    <category android:name="android.intent.category.BROWSABLE" />
    <data android:scheme="taphouse" android:host="auth" />
</intent-filter>
```

---

## 6. Web実装詳細

Web側の実装は **[TapHouseWeb](../../../TapHouseWeb/)** リポジトリで管理。

| ドキュメント | 内容 |
|------------|------|
| [browser-login-spec.md](../../../TapHouseWeb/docs/browser-login-spec.md) | 完成仕様書 |
| [api-spec.md](../../../TapHouseWeb/docs/api-spec.md) | Firebase Functions API仕様 |
| [setup-guide.md](../../../TapHouseWeb/docs/setup-guide.md) | セットアップ・デプロイ手順 |

### 6.1 概要

- **認証ロジック**: `src/auth.ts` (TypeScript)
- **Functions API**: `functions/src/index.ts` (TypeScript)
- **ログインページ**: `public/index.html`

認証成功時はカスタムトークンを生成し、Deep Linkでアプリに戻る：

```
taphouse://auth?token={customToken}
```

---

## 7. デプロイ手順

詳細は [TapHouseWeb/docs/setup-guide.md](../../../TapHouseWeb/docs/setup-guide.md) を参照。

```bash
# TapHouseWebリポジトリに移動
cd TapHouseWeb

# 依存関係インストール
npm run install:all

# ビルド＆デプロイ
npm run deploy
```

---

## 8. セキュリティ考慮事項

| リスク | 対策 |
|--------|------|
| トークン傍受 | カスタムトークンは短命（1時間）、1回限り使用 |
| Deep Linkなりすまし | IDトークン検証をサーバー側で実施 |
| CSRF | Firebase AuthのCSRF保護を使用 |
| フィッシング | Firebase Hostingの正規ドメインのみ使用 |

---

## 9. エラーハンドリング

| 状況 | 対応 |
|------|------|
| ブラウザが開けない | エラーメッセージ表示、リトライ |
| 認証キャンセル | モード選択画面に戻る |
| トークン期限切れ | 再度ブラウザログインを促す |
| ネットワークエラー | リトライボタン表示 |

---

## 10. テスト項目

- [ ] メールログインでアプリに戻れる
- [ ] Googleログインでアプリに戻れる
- [ ] 新規登録後アプリに戻れる
- [ ] 認証キャンセルで正しく処理される
- [ ] トークン期限切れ時のエラーハンドリング
- [ ] オフライン時の挙動

---

## 11. 設定が必要な項目

デプロイ前に以下の設定を行う必要がある。
詳細は [TapHouseWeb/docs/setup-guide.md](../../../TapHouseWeb/docs/setup-guide.md) を参照。

### Unity側（このリポジトリ）

- **AndroidManifest.xml**: Deep Link設定（設定済み）
- **DeepLinkHandler.cs**: シーン内に配置
- **PocketAuthManager.cs**: ModeSelectManagerから参照

### Web側（TapHouseWebリポジトリ）

1. **src/auth.ts**
   - `firebaseConfig`にFirebaseプロジェクトの設定値を入力

2. **.firebaserc**
   - `"default": "taphouse-app"` を実際のプロジェクトIDに変更

3. **Firebase Console**
   - Authentication > Sign-in method で Email/Password と Google を有効化
   - Firestore Database を有効化
   - Hosting をセットアップ
