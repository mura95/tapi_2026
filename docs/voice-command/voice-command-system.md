# 音声コマンドシステム 仕様書

## 概要

音声認識により犬へのコマンドを実行するシステム。OpenAI Whisper APIまたはローカルWhisperモデルを使用した音声認識、Wake Word検出、30種類の日本語/英語コマンドをサポートしています。

## ファイル構成

### Core（コア）

| ファイル | 役割 |
|----------|------|
| `VoiceCommandManager.cs` | システム全体のオーケストレーター（シングルトン） |
| `VoiceCommandConfig.cs` | APIキー等の設定（ScriptableObject） |
| `VoiceInputDetector.cs` | 3本指タッチによる録音開始/終了検出 |
| `AudioRecorder.cs` | マイク録音、VAD、音声処理 |

### Recognizers（認識エンジン）

| ファイル | 役割 |
|----------|------|
| `IVoiceRecognizer.cs` | 認識エンジンのインターフェース |
| `RecognizerSelector.cs` | ネットワーク状態に応じた認識エンジン選択 |
| `OpenAIVoiceRecognizer.cs` | OpenAI Whisper API（オンライン） |
| `LocalWhisperRecognizer.cs` | ローカルWhisperモデル（オフライン） |

### Commands（コマンド）

| ファイル | 役割 |
|----------|------|
| `IVoiceCommand.cs` | コマンドインターフェース、コンテキスト定義 |
| `VoiceCommandBase.cs` | コマンド基底クラス |
| `VoiceCommandRegistry.cs` | コマンドの登録とマッチング実行 |

### DogCommands（犬コマンド実装）

| ファイル | コマンド数 |
|----------|-----------|
| `BasicCommands.cs` | 7個（おすわり、お手、おかわり、ふせ、立て、待て、よし） |
| `MovementCommands.cs` | 3個（おいで、回れ、ジャンプ） |
| `TrickCommands.cs` | 3個（バーン、ちんちん、ハイタッチ） |
| `CommunicationCommands.cs` | 2個（吠えろ、静かに） |
| `PraiseCommands.cs` | 15個（よくできた、すごい、かわいい等） |

### WakeWord（ウェイクワード）

| ファイル | 役割 |
|----------|------|
| `WakeWordManager.cs` | Wake Word統合管理 |
| `WakeWordDetector.cs` | Wake Word検出処理 |
| `WakeWordModelValidator.cs` | モデル検証 |

### Audio（音声処理）

| ファイル | 役割 |
|----------|------|
| `VoiceActivityDetector.cs` | 音声区間検出（VAD） |
| `RealtimeAudioProcessor.cs` | リアルタイム音声処理 |
| `AdvancedAudioFilters.cs` | ノイズ除去等のフィルター |

## システムフロー

### 全体フロー

```
[入力検出]
3本指タッチ / Rキー（デスクトップ）
    ↓
VoiceInputDetector.OnRecordingStarted
    ↓
[録音]
AudioRecorder.StartRecording()
    ↓
マイクから音声キャプチャ
    ↓
指を離す
    ↓
VoiceInputDetector.OnRecordingStopped
    ↓
AudioRecorder.StopRecording() → float[]
    ↓
[認識]
RecognizerSelector.RecognizeAsync()
    ↓
┌─────────────────────────────────────┐
│ オンライン時: OpenAI Whisper API    │
│ オフライン時: ローカルWhisper       │
└─────────────────────────────────────┘
    ↓
認識テキスト取得
    ↓
[コマンド実行]
VoiceCommandRegistry.ExecuteMatchingCommands()
    ↓
キーワードマッチング（部分一致）
    ↓
DogController.ActionBool(true) 等
```

## VoiceCommandManager（中央オーケストレーター）

### 初期化順序

```csharp
async Task InitializeSystem()
{
    // 1. DogController取得
    // 2. VoiceInputDetector初期化 + イベント購読
    // 3. AudioRecorder初期化
    // 4. RecognizerSelector初期化（OpenAI / Local）
    // 5. VoiceCommandRegistry初期化 + 全コマンド登録
}
```

### 設定パラメータ

| パラメータ | デフォルト値 | 説明 |
|------------|--------------|------|
| `enableVoiceCommand` | true | システム有効/無効 |
| `useOpenAI` | true | OpenAI API使用 |
| `useLocalWhisper` | true | ローカルWhisper使用 |
| `showDebugLog` | true | デバッグ表示 |

### APIキー設定（優先順位）

1. **VoiceCommandConfig**（ScriptableObject）- 最優先
2. **環境変数** `OPENAI_API_KEY`
3. **Inspectorで直接指定**（非推奨）

## VoiceInputDetector（入力検出）

### トリガー方式

| 環境 | トリガー |
|------|----------|
| Android/iOS | 3本指タッチ |
| Editor/Desktop | Rキー長押し |

### 設定パラメータ

| パラメータ | デフォルト値 | 説明 |
|------------|--------------|------|
| `requiredFingers` | 3 | 必要な指の本数 |
| `minimumHoldTime` | 0.3秒 | 最低保持時間（これより短いとキャンセル） |
| `vibrationFeedback` | true | バイブレーションフィードバック |
| `desktopTestKey` | R | デスクトップテスト用キー |

## AudioRecorder（録音）

### 録音モード

| モード | 説明 |
|--------|------|
| `PushToTalk` | キー押下で録音（デフォルト） |
| `ContinuousVAD` | 常時監視、音声検出で自動録音 |

### 設定パラメータ

| パラメータ | デフォルト値 | 説明 |
|------------|--------------|------|
| `sampleRate` | 16000 | サンプリングレート |
| `maxRecordSeconds` | 30 | 最大録音秒数 |
| `vadEnergyThreshold` | 0.01 | VAD音声検出しきい値 |
| `vadSilenceDuration` | 0.5秒 | 無音判定時間 |
| `vadMaxRecordingDuration` | 3秒 | VAD最大録音時間 |
| `autoSaveRecordings` | true | 録音の自動保存 |

### 音声処理パイプライン

```
マイク入力
    ↓
RealtimeAudioProcessor（リアルタイム処理）
    ↓
VoiceActivityDetector（音声区間検出）
    ↓
AdvancedAudioFilters.ProcessForWakeWord()
    ↓
処理済み音声データ
```

## RecognizerSelector（認識エンジン選択）

### 選択ロジック

```csharp
if (preferOnline && IsOnline() && openAIRecognizer.IsInitialized)
    → OpenAI Whisper API
else if (fallbackToLocal && localRecognizer.IsInitialized)
    → ローカルWhisper
```

### フォールバック機能

- 優先認識エンジンが失敗した場合、別の認識エンジンで再試行
- 例: OpenAI API失敗 → ローカルWhisperで再認識

## VoiceCommandRegistry（コマンド登録/実行）

### コマンドマッチング

```csharp
// 部分一致でキーワード検索
foreach (var command in commands)
{
    string matchedKeyword = command.Match(recognizedText);
    if (matchedKeyword != null)
        matches.Add(command);
}

// 優先度順にソート
matches.Sort((a, b) => b.Priority.CompareTo(a.Priority));

// 実行
foreach (var command in matches)
{
    if (command.CanExecute())
        command.Execute(context);
}
```

### VoiceCommandContext

コマンド実行時に渡されるコンテキスト情報：

| プロパティ | 説明 |
|------------|------|
| `RecognizedText` | 認識されたテキスト全体 |
| `MatchedKeyword` | マッチしたキーワード |
| `Confidence` | 信頼度スコア（0.0-1.0） |
| `RecognizerName` | 使用された認識エンジン名 |
| `Timestamp` | タイムスタンプ |

## コマンド一覧（30個）

### BasicCommands（基本コマンド）

| コマンド | キーワード例 | 優先度 |
|----------|--------------|--------|
| Sit | おすわり, 座れ, sit | 10 |
| Paw | おて, お手, paw | 10 |
| Okawari | おかわり, 左手 | 10 |
| Down | ふせ, 伏せ, down | 10 |
| Stand | たっち, 立って, stand | 10 |
| Wait | まて, 待て, wait | 15 |
| Okay | よし, ok, go | 15 |

### MovementCommands（移動コマンド）

| コマンド | キーワード例 | 優先度 |
|----------|--------------|--------|
| Come | おいで, こっち, come | 10 |
| Turn | 回れ, まわれ, turn | 10 |
| Jump | ジャンプ, 飛べ, jump | 10 |

### TrickCommands（トリックコマンド）

| コマンド | キーワード例 | 優先度 |
|----------|--------------|--------|
| Bang | バーン, 死んだふり | 10 |
| ChinChin | ちんちん, 二足立ち | 10 |
| HighFive | ハイタッチ, high five | 10 |

### CommunicationCommands（コミュニケーション）

| コマンド | キーワード例 | 優先度 |
|----------|--------------|--------|
| Bark | 吠えろ, ワン, bark | 10 |
| Quiet | 静かに, しずかに, quiet | 10 |

### PraiseCommands（褒め言葉）- 15種類

よくできた, すごい, えらい, 上手, 賢い, かわいい, 大好き, 好き, ラブラブ, 頑張れ, ファイト, できるよ, ご褒美, おやつ, 美味しい

## 新しいコマンドの追加方法

### 1. VoiceCommandBaseを継承

```csharp
public class NewCommand : VoiceCommandBase
{
    private DogController dogController;

    public override string CommandName => "NewCommand";
    public override string[] Keywords => new[]
    {
        "キーワード1", "キーワード2", "keyword"
    };
    public override string Description => "コマンドの説明";
    public override int Priority => 10;

    public NewCommand(DogController controller)
    {
        dogController = controller;
    }

    public override void Execute(VoiceCommandContext context)
    {
        LogExecution(context);
        if (dogController == null) return;

        // コマンド実行ロジック
        dogController.ActionBool(true);
    }

    public override bool CanExecute()
    {
        return dogController != null;
    }
}
```

### 2. VoiceCommandManagerで登録

`RegisterDogCommands()`メソッドに追加：

```csharp
commandRegistry.RegisterCommands(
    new NewCommand(dogController)
);
```

## Wake Wordシステム

### 概要

特定のキーワード（例: "ポチ"）を検出してコマンド受付を開始するシステム。

### 動作フロー

```
常時監視
    ↓
WakeWordDetector.ProcessAudioData()
    ↓
Wake Word検出
    ↓
WakeWordManager.OnWakeWordDetected()
    ↓
コマンドウィンドウを開く（5秒間）
    ↓
VoiceCommandManager.SetListeningEnabled(true)
    ↓
音声コマンド受付可能
```

### 設定パラメータ

| パラメータ | デフォルト値 | 説明 |
|------------|--------------|------|
| `enableWakeWord` | true | Wake Word有効/無効 |
| `requireWakeWordForCommands` | false | コマンド実行にWake Wordを必須とするか |
| `commandWindowDuration` | 5秒 | コマンド受付ウィンドウ時間 |

## VoiceActivityDetector（VAD）

### 音声区間検出ロジック

```csharp
float energy = CalculateRMS(chunk);  // RMSエネルギー計算
bool isSpeech = energy > energyThreshold;

if (isSpeech && !isDetecting)
    → 音声開始、バッファリング開始

if (!isSpeech && isDetecting)
    silenceTimer += chunkDuration;
    if (silenceTimer >= silenceDuration)
        → 音声終了、OnSpeechEnded発火
```

## OpenAI Whisper API

### リクエスト形式

```
POST https://api.openai.com/v1/audio/transcriptions
Content-Type: multipart/form-data

file: audio.wav
model: whisper-1
language: ja
response_format: json
```

### WAV変換

`float[]` → 16bit PCM WAV形式に変換してAPIに送信

## デバッグ機能

### OnGUI表示

- 録音中: 「🎙️ 録音中... (Xs)」（赤色）
- 認識中: 「⏳ 認識中...」（黄色）
- 成功: 「✅ 実行完了 (認識テキスト)」（緑色）
- 失敗: 「❌ 認識不可」（赤色）

### ログ出力

すべてのログは `LogCategory.Voice` カテゴリで出力

## トラブルシューティング

### 音声認識が動作しない

1. **マイク権限**: Android設定でマイク権限が許可されているか
2. **APIキー**: VoiceCommandConfigにAPIキーが設定されているか
3. **ネットワーク**: オンライン認識の場合、インターネット接続を確認

### コマンドが認識されない

1. **キーワード確認**: 登録されているキーワードを確認
2. **ログ確認**: 認識されたテキストをログで確認
3. **言語設定**: OpenAI APIは `language: ja` で日本語を指定

### 録音時間が短すぎる

- `minimumHoldTime`（0.3秒）より長く指を保持する
- VADモードの場合、`vadSilenceDuration`を調整

## 依存関係

### 外部パッケージ

- `com.whisper.unity` - ローカルWhisperモデル
- `com.unity.ai.inference` - Unity Sentis AIランタイム

### 必要な権限

- **Android**: `android.permission.RECORD_AUDIO`
