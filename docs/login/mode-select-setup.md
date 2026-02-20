# ModeSelect.unity セットアップ手順

## 1. シーン作成

1. Unity Editor で `File → New Scene → Basic (Built-in)`
2. `File → Save As` で `Assets/Scenes/ModeSelect.unity` として保存

---

## 2. UI構成

### 2.1 Canvas作成

1. Hierarchy で右クリック → `UI → Canvas`
2. Canvas の設定:
   - Canvas Scaler → UI Scale Mode: `Scale With Screen Size`
   - Reference Resolution: `1080 x 1920`
   - Match: `0.5`

### 2.2 ModeSelectUI オブジェクト作成

Canvas 配下に以下の構成で作成:

```
Canvas
└── ModeSelectUI (空のGameObject)
    ├── Title (TextMeshPro)
    ├── Subtitle (TextMeshPro)
    ├── TapHouseButton (Button)
    │   ├── Icon (Image)
    │   ├── ButtonTitle (TextMeshPro)
    │   └── ButtonDesc (TextMeshPro)
    └── TapPocketButton (Button)
        ├── Icon (Image)
        ├── ButtonTitle (TextMeshPro)
        └── ButtonDesc (TextMeshPro)
```

### 2.3 テキスト設定

| オブジェクト | テキスト内容 |
|-------------|-------------|
| Title | たっぷへようこそ！ |
| Subtitle | どちらで使いますか？ |
| TapHouseButton/ButtonTitle | たっぷハウス |
| TapHouseButton/ButtonDesc | 置き型タブレットで使う方 |
| TapPocketButton/ButtonTitle | たっぷポケット |
| TapPocketButton/ButtonDesc | スマホで持ち歩いて使う方 |

### 2.4 ボタンサイズ（高齢者向け）

| 項目 | 推奨値 |
|------|--------|
| ボタン高さ | 200px以上 |
| ボタン幅 | 親の90% |
| ボタン間余白 | 40px |
| タイトルフォントサイズ | 48 |
| 説明フォントサイズ | 32 |

---

## 3. ModeSelectManager設定

### 3.1 空のGameObject作成

1. Hierarchy で右クリック → `Create Empty`
2. 名前を `ModeSelectManager` に変更

### 3.2 スクリプトアタッチ

1. `ModeSelectManager` オブジェクトを選択
2. Inspector で `Add Component`
3. `ModeSelectManager` スクリプトを検索してアタッチ

### 3.3 参照設定

Inspector で以下を設定:

| フィールド | 設定するオブジェクト |
|-----------|---------------------|
| Mode Select UI | ModeSelectUI |
| Tap House Button | TapHouseButton |
| Tap Pocket Button | TapPocketButton |
| Login Scene Name | `login` |
| Main Scene Name | `Main` |
| Pocket Main Scene Name | `PocketMain` |

---

## 4. Build Settings

### 4.1 シーン追加

1. `File → Build Settings`
2. `Add Open Scenes` で ModeSelect.unity を追加
3. ModeSelect.unity をドラッグして**一番上（index 0）**に配置

### 4.2 シーン順序

```
0: Scenes/ModeSelect    ← 起動シーン
1: Scenes/login
2: Scenes/Main
3: Scenes/PocketMain    ← 未作成の場合は後で追加
```

---

## 5. 動作確認

### 5.1 テスト手順

1. ModeSelect.unity を開く
2. Play ボタンで実行
3. 確認項目:
   - [ ] Firebase未ログイン状態 → モード選択UIが表示される
   - [ ] 「たっぷハウス」押下 → login シーンへ遷移
   - [ ] 「たっぷポケット」押下 → OSブラウザでログインページが開く

### 5.2 ログイン済みテスト

1. login シーンでログイン実行
2. アプリを再起動（Play停止→再Play）
3. 確認項目:
   - [ ] 自動的に Main シーンへ遷移する

---

## 6. ファイル構成

```
Assets/
├── Scenes/
│   └── ModeSelect.unity          ← 新規作成
├── Scripts/
│   └── UI/
│       ├── ModeSelect/
│       │   ├── AppModeManager.cs     ← 新規作成済み
│       │   └── ModeSelectManager.cs  ← 新規作成済み
│       └── Login/
│           ├── DeepLinkHandler.cs    ← たっぷポケット用
│           └── PocketAuthManager.cs  ← たっぷポケット用
└── Plugins/Android/
    └── AndroidManifest.xml       ← Deep Link設定
```

---

## 7. 注意事項

- PocketMain.unity は Phase 2 で作成予定。現時点では存在しなくてもOK
- たっぷポケットのブラウザログインは実装済み。詳細は [pocket-browser-login.md](./pocket-browser-login.md) を参照
- Firebase の初期化は `FirebaseAuth.DefaultInstance` で自動的に行われる
