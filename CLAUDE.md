# CLAUDE.md

このファイルはClaude Code (claude.ai/code) がこのリポジトリのコードを扱う際のガイダンスを提供します。

## ドキュメントの確認

**重要:** 各機能の詳細仕様は `docs/README.md` を参照してください。

機能の実装や調査を行う前に、[docs/README.md](./docs/README.md) で該当機能のドキュメントを確認してください。

**カバーされている機能:**
音声コマンド / リマインダー通知 / Firebase連携 / アニメーション / ゲームプレイ / 状態管理 / 顔認識 / タッチ操作 / UI / ローカライゼーション / オーディオ / 犬の体型管理 / ユーティリティ

## プロジェクト概要

Unity製バーチャルペットシミュレーション（タップハウス）- たまごっち風のAndroidアプリケーション。高度なAI行動、音声コマンド、Firebaseによるクラウド同期機能を備えた3D犬を特徴としています。

**Unityバージョン:** 6000.2.8f1 (Unity 6)
**対象プラットフォーム:** Android
**言語:** C#（日本語UIローカライゼーション）

## 開発コマンド

### プロジェクトを開く
```bash
# Unity Editorで開く
# File → Open Project → このディレクトリを選択
# または Unity Hub: Add → このディレクトリを選択
```

### Android向けビルド
1. Unity Editorを開く
2. File → Build Settings → Android
3. Switch Platform（まだAndroidでない場合）
4. Player Settingsで適切なkeystoreが設定されていることを確認
5. BuildまたはBuild and Run

### テストの実行
```bash
# Unity Editor内:
# Window → General → Test Runner
# PlayModeまたはEditModeを選択
# "Run All"をクリック、または特定のテストを選択
```

### パッケージ解決
```bash
# Google Play Services依存関係を解決:
# Unity Editor: Assets → Play Services Resolver → Android Resolver → Resolve
# これによりAssets/Plugins/Androidに.aarファイルがプルされる
```

## コアアーキテクチャ

### 状態管理システム

プロジェクトは`DogStateController.cs`を介した集中オーケストレーターパターンを使用し、3つの相互接続されたメトリクスを管理します：

| メトリクス | 範囲 | 管理クラス | 永続化 |
|-----------|------|-----------|--------|
| **愛情度 (Love)** | 0-100 | `LoveManager.cs` | PlayerPrefs |
| **要求度 (Demand)** | 0-100 | `DemandManager.cs` | なし（セッション毎に再計算） |
| **空腹度 (Hunger)** | 4段階 | `HungerManager.cs` | PlayerPrefs |

詳細: [docs/state-management/](./docs/state-management/)

### 主要システム一覧

| システム | 概要 | 詳細ドキュメント |
|---------|------|-----------------|
| **音声コマンド** | Whisper APIによる30種類の日本語/英語コマンド | [docs/voice-command/](./docs/voice-command/) |
| **リマインダー** | 高齢者向け服薬・食事通知 | [docs/reminder/](./docs/reminder/) |
| **Firebase連携** | クラウド同期、認証、マルチデバイス | [docs/firebase/](./docs/firebase/) |
| **アニメーション** | StateMachineBehaviour、感情状態 | [docs/animation/](./docs/animation/) |
| **ゲームプレイ** | ボール遊び、睡眠サイクル | [docs/gameplay/](./docs/gameplay/) |
| **顔認識** | OpenCVによる顔検出→注目アクション | [docs/detection/](./docs/detection/) |

### アクション実行フロー

すべてのプレイヤーインタラクションは`DogActionEngine.cs`を経由します：

```
プレイヤーアクション →
  DogActionEngine.ExecuteAction(DogActionData) →
    CalculateLoveChange() [性格 × 愛情レベル × タイミングボーナス]
    CalculateDemandChange() [性格 × 愛情レベルスケーリング]
    Firebaseにログ → ActionResultを返す
```

**キーパターン:** すべてのアクションは`DogActionData` ScriptableObjectとして定義（ハードコードではない）

### 主要アーキテクチャパターン

| パターン | 使用箇所 |
|---------|-------|
| **シングルトン** | VoiceCommandManager、GameAudioSettings |
| **ScriptableObject設定** | DogActionData、DogPersonalityData、VoiceCommandConfig、イベント定義 |
| **イベントシステム** | ScriptableObjectベースのパブリッシュ-サブスクライブ（疎結合） |
| **ストラテジー** | I_ToyThrowAngle（角度計算戦略） |
| **アジャスター/デコレーター** | IDemandAdjuster（プラガブルな要求調整ルール） |
| **オブザーバー** | Firebaseリスナー（ChildChanged、ChildAdded） |
| **ステートマシン** | StateMachineBehaviourスクリプト付きAnimatorベース |

### 時間ベースの更新

```
30秒ごと:
  DogStateController.StateCheck() → DemandManager.ApplyAllAdjusters()

1時間ごと（3600秒）:
  DogStateController.TickHourPassed() →
    DemandManager.TickHourPassed(loveLevel)
    LoveManager.CheckTimePenalty()
    Hungryの場合: ApplyHungerNeglectPenalty()
```

## 主要な依存関係

**外部パッケージ:**
- `com.cysharp.unitask` (2.5.10) - Async/awaitサポート
- `com.github.asus4.mediapipe` (2.17.0) - FaceManager用の顔検出
- `com.github.asus4.tflite` (2.17.0) - TensorFlow Lite推論
- `com.whisper.unity` (GitHubパッケージ) - ローカル音声認識
- `com.unity.ai.inference` (2.4.1) - Unity Sentis AIランタイム
- Firebase SDK - Authentication、Realtime Database、Firestore、Cloud Storage

**Googleサインイン:**
- 完全なセットアップ手順については`Assets/README.md`を参照
- Assetsフォルダに`google-services.json`が必要
- IDトークン用にWeb client IDを設定する必要あり
- KeystoreのSHA1フィンガープリントがコンソール設定と一致する必要あり

## よくあるタスク

### 新しい犬の性格を追加
1. `Resources/Dog/Personality/`に新しい`DogPersonalityData` ScriptableObjectを作成
2. 愛情増加/減少、要求増加/減少、空腹進行の倍率を設定
3. 適切なUI/ゲームロジックで参照

### 新しいアクションを追加
1. `Resources/Dog/Action/`に`DogActionData` ScriptableObjectを作成
2. 基本の愛情/要求/空腹効果を定義
3. 性格フラグと愛情レベル倍率を設定
4. UIアイコンとアニメーショントリガーを追加
5. UIボタンイベントで接続

### 新しい音声コマンドを追加
1. `VoiceCommandBase`を継承する新しいクラスを作成（`Assets/Scripts/VoiceCommandSystem/Commands/DogCommands/`内）
2. `Keywords`プロパティを日本語/英語のバリエーションでオーバーライド
3. `ExecuteAsync()`をコマンドロジックでオーバーライド
4. `VoiceCommandManager.cs`で`VoiceCommandRegistry.Register()`を介して登録

詳細: [docs/voice-command/voice-command-system.md](./docs/voice-command/voice-command-system.md)

### 状態しきい値の変更
`DogStateController.cs`を編集：
- 愛情/要求レベル: `MoodLevel.cs`/`DemandLevel.cs`の列挙型範囲を変更
- 時間間隔: `tickInterval`（30秒）または`hourInSeconds`（3600秒）を変更
- 空腹進行: `HungerManager.cs`の時間しきい値を変更

## ファイル構造リファレンス

```
Assets/Scripts/
├── DogStateController.cs           # 中央オーケストレーター
├── LoveManager.cs                  # 愛情度管理
├── DemandManager.cs                # 要求度管理
├── HungerManager.cs                # 空腹度管理
├── DogActionEngine.cs              # アクション処理
├── FirebaseManager.cs              # Firebase連携
├── VoiceCommandSystem/             # 音声コマンド
├── StateMachineBehaviour/          # アニメーション制御
├── Reminder/                       # リマインダー機能
├── MultiDevice/                    # マルチデバイス機能
└── Variables/GlobalVariables.cs    # グローバル状態

Assets/Resources/
├── Dog/Personality/                # 性格定義（ScriptableObject）
├── Dog/Action/                     # アクション定義（ScriptableObject）
└── UI/Item/                        # アイテムリスト
```

## コードスタイルに関する注意事項

- **コルーチンの安全性:** PlayToyシステムは適切な再入ガードとタイムアウトウォッチャーを示している - 新しいコルーチンにはこのパターンに従うこと
- **ScriptableObjectファースト:** データ駆動設計を優先 - ハードコーディングではなく設定用のScriptableObjectを作成
- **疎結合:** システム間通信にはScriptableObjectイベントを使用
- **プラットフォーム抽象化:** プラットフォーム固有の実装を持つインターフェース（IPowerService、IAlarmScheduler）を使用
- **時間処理:** 計算には常にUTCを使用し、表示時のみローカル時間に変換（`TimeZoneUtility.cs`を参照）
- **Firebaseタイムスタンプ:** 古いコマンド実行を防ぐために±10秒ウィンドウ内でタイムスタンプを検証
