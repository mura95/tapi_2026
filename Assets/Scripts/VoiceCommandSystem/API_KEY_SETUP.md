# API キーの安全な管理方法

このプロジェクトでは、OpenAI API キーを安全に管理するために以下の方法を使用します。

## ⚠️ 重要：GitHub に API キーをプッシュしないでください

`.gitignore`に以下のファイルが除外設定されています：

- `Assets/Resources/VoiceCommandConfig.asset`
- `.env`、`.env.local`
- `*.secret`

## 推奨方法：ScriptableObject を使用（最も簡単）

### 1. VoiceCommandConfig アセットを作成

1. Unity エディタで、プロジェクトウィンドウの`Assets/Resources/`フォルダを右クリック
2. `Create > VoiceCommand > Config` を選択
3. 作成されたアセットの名前を `VoiceCommandConfig` にする
4. Inspector で OpenAI API Key を入力

### 2. VoiceCommandManager に設定

1. Hierarchy で `VoiceCommandManager` オブジェクトを選択
2. Inspector で `Config` フィールドに作成した `VoiceCommandConfig` をドラッグ&ドロップ

### 3. 完了

✅ これで API キーはローカルにのみ保存され、GitHub にプッシュされません。

---

## 代替方法 1：環境変数を使用

### Windows（PowerShell）

```powershell
$env:OPENAI_API_KEY = "your-api-key-here"
```

### Windows（永続的）

```powershell
[System.Environment]::SetEnvironmentVariable('OPENAI_API_KEY', 'your-api-key-here', 'User')
```

### macOS / Linux

```bash
export OPENAI_API_KEY="your-api-key-here"
```

### macOS / Linux（永続的）

`~/.bashrc` または `~/.zshrc` に追加：

```bash
export OPENAI_API_KEY="your-api-key-here"
```

---

## 代替方法 2：.env ファイルを使用（今後実装予定）

プロジェクトルートに `.env` ファイルを作成：

```env
OPENAI_API_KEY=your-api-key-here
```

**注意：** `.env`ファイルは`.gitignore`に含まれているため、GitHub にプッシュされません。

---

## API キー読み込みの優先順位

1. **VoiceCommandConfig アセット**（最優先）
2. **環境変数** `OPENAI_API_KEY`
3. **Inspector の直接指定**（⚠️ 非推奨：GitHub にプッシュされる可能性あり）

---

## チーム開発の場合

### 配布用のテンプレートを作成

`VoiceCommandConfig_Template.asset` を作成して GitHub にプッシュ：

- API キーは空白にする
- 他の設定は含める

各開発者が：

1. テンプレートをコピーして `VoiceCommandConfig.asset` にリネーム
2. 自分の API キーを入力
3. `.gitignore`により自動的に除外される

---

## トラブルシューティング

### API キーが読み込まれない

- Console ログで「API key loaded from Config file」が表示されるか確認
- `VoiceCommandConfig.asset`が`Assets/Resources/`フォルダにあるか確認
- VoiceCommandManager の Inspector で`Config`フィールドが設定されているか確認

### 誤って API キーをコミットしてしまった場合

1. API キーを即座に無効化（OpenAI のダッシュボードで削除）
2. 新しい API キーを生成
3. Git の履歴から API キーを削除（`git filter-branch`または`BFG Repo-Cleaner`を使用）

---

## セキュリティのベストプラクティス

✅ **推奨**

- VoiceCommandConfig を使用
- API キーをコード外で管理
- `.gitignore`で API キーファイルを除外

❌ **非推奨**

- コードに API キーを直接記述
- Inspector で API キーを直接入力（Scene ファイルにコミットされる可能性）
- API キーをスクリーンショットに含める
