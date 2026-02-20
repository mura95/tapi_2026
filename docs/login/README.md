# ログインシステム

ログイン・新規会員登録・ユーザー種別判別に関するドキュメント。

---

## アーキテクチャ概要

たっぷハウスとたっぷポケットで**ログイン方式が異なる**。

```
┌─────────────────────────────────────────────────────────────┐
│                    ログインアーキテクチャ                    │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│                    ┌─────────────────┐                      │
│                    │   アプリ起動    │                      │
│                    │ ModeSelect.unity│                      │
│                    └────────┬────────┘                      │
│                             │                               │
│                    Firebase初期化・認証確認                  │
│                             │                               │
│              ┌──────────────┴──────────────┐                │
│        ログイン済み                    未ログイン            │
│        → 即遷移                    → モード選択UI表示       │
│              │                             │                │
│              │              ┌──────────────┴──────────────┐ │
│              │              ▼                             ▼ │
│              │   ┌─────────────────┐    ┌─────────────────┐│
│              │   │   たっぷハウス   │    │  たっぷポケット ││
│              │   │   （タブレット） │    │   （スマホ）    ││
│              │   └────────┬────────┘    └────────┬────────┘│
│              │            │                      │         │
│              │            ▼                      ▼         │
│              │   ┌─────────────────┐    ┌─────────────────┐│
│              │   │ Unity内ログイン │    │ OSブラウザログイン││
│              │   │ login.unity    │    │ Firebase Hosting││
│              │   └────────┬────────┘    └────────┬────────┘│
│              │            │                      │         │
│              └────────────┴──────────┬───────────┘         │
│                                      ▼                     │
│                             ┌─────────────────┐            │
│                             │  Firebase Auth  │            │
│                             │  + Firestore    │            │
│                             └─────────────────┘            │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## 方式の違い

| 項目 | たっぷハウス | たっぷポケット |
|------|-------------|---------------|
| **デバイス** | タブレット（専用端末） | スマホ |
| **ログイン場所** | Unity内（login.unity） | OSブラウザ（Firebase Hosting） |
| **理由** | Kioskモード対応のため | セキュリティ・UX向上のため |
| **認証方法** | メール/パスワード | メール/パスワード + Google |
| **新規登録** | Unity内 | OSブラウザ |

---

## なぜ方式を分けるのか？

### たっぷハウス（Unity内ログイン）
- タブレットを**Kioskモード**または**ホームアプリ設定**で専用端末化
- OSブラウザに遷移できない/させたくない
- WebViewはセキュリティリスクがある
- 現状のログイン画面で十分

### たっぷポケット（OSブラウザログイン）
- スマホユーザーに馴染みのあるUX
- OSブラウザが最もセキュア（フィッシング対策等）
- Googleサインインがスムーズ
- パスワードマネージャー対応

---

## ドキュメント一覧

### Unity側（このリポジトリ）

| ファイル | 内容 |
|----------|------|
| [mode-selection.md](./mode-selection.md) | モード選択画面（起動時の分岐） |
| [mode-select-setup.md](./mode-select-setup.md) | ModeSelect.unity セットアップ手順 |
| [pocket-browser-login.md](./pocket-browser-login.md) | たっぷポケット用ブラウザログイン（Unity側） |
| [registration-flow.md](./registration-flow.md) | 新規会員登録フロー |
| [user-data-schema.md](./user-data-schema.md) | Firestoreユーザーデータ構造 |

### Web側（別リポジトリ）

**[TapHouseWeb](../../../TapHouseWeb/)** で管理

| ファイル | 内容 |
|----------|------|
| [TapHouseWeb/docs/browser-login-spec.md](../../../TapHouseWeb/docs/browser-login-spec.md) | ブラウザログイン完成仕様書 |
| [TapHouseWeb/docs/api-spec.md](../../../TapHouseWeb/docs/api-spec.md) | Firebase Functions API仕様 |
| [TapHouseWeb/docs/setup-guide.md](../../../TapHouseWeb/docs/setup-guide.md) | セットアップ・デプロイ手順 |

---

## Firebase連携

| サービス | 用途 |
|----------|------|
| **Firebase Auth** | メール/パスワード認証、Google認証 |
| **Firebase Hosting** | ポケット用Webログインページ |
| **Firestore** | ユーザー情報保存 |
| **Firebase Functions** | カスタムトークン生成API |
| **FCM** | プッシュ通知トークン管理 |

---

## 関連スクリプト

### モード選択・起動
```
Assets/Scripts/UI/ModeSelect/
├── ModeSelectManager.cs     # 起動処理・モード選択UI
└── AppModeManager.cs        # モード保存・読込
```

### たっぷハウス（Unity内）
```
Assets/Scripts/UI/Login/
├── LoginFormUI.cs           # ログインUI
└── LoginPasswordToggle.cs   # パスワード表示切替
```

### たっぷポケット（ブラウザ連携）
```
Assets/Scripts/UI/Login/
├── PocketAuthManager.cs     # ブラウザ認証管理
└── DeepLinkHandler.cs       # Deep Link受信処理
```

### Web側（別リポジトリ）

```
TapHouseWeb/
├── src/auth.ts              # 認証ロジック（TypeScript）
├── public/
│   ├── index.html           # ログインページ
│   └── css/style.css        # スタイル
└── functions/src/index.ts   # カスタムトークン生成API
```
