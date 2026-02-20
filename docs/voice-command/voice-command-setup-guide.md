# 音声コマンドシステム - Unity Editor セットアップガイド

## 概要

このガイドでは、音声コマンドシステムの新しいトリガー方式（顔認識 + 音量検出）をUnity Editorで設定する手順を説明します。

---

## 1. 必要なコンポーネント一覧

| コンポーネント | 役割 | 配置場所 |
|---------------|------|----------|
| `FacePresenceDetector` | 顔検出（OpenCV） | 既存（シーン内） |
| `FaceTriggeredVoiceInput` | 顔認識→マイク制御 | **新規追加** |
| `VoiceCommandManager` | 音声コマンド統括 | 既存（シーン内） |
| `AudioRecorder` | 録音・VAD処理 | 既存（VoiceCommandManagerと同じGO） |
| `VoiceCommandDebugView` | デバッグUI | **新規追加（オプション）** |

---

## 2. FaceTriggeredVoiceInput のセットアップ

### 手順 2.1: コンポーネントの追加

1. **Hierarchyウィンドウ**で`VoiceCommandManager`オブジェクトを選択
2. **Inspectorウィンドウ**で「Add Component」をクリック
3. 「FaceTriggeredVoiceInput」を検索して追加

### 手順 2.2: 参照の設定

Inspectorで以下のフィールドを設定：

| フィールド | 設定内容 |
|-----------|----------|
| **Face Detector** | シーン内の`FacePresenceDetector`オブジェクトをドラッグ |
| **Audio Recorder** | 同じGameObject上の`AudioRecorder`（自動検出される場合あり） |
| **Enable Face Trigger** | ✅ チェック（本番用） |
| **Activation Delay** | `0.5`（顔検出後0.5秒でマイク起動） |
| **Deactivation Grace Period** | `2.0`（顔消失後2秒でマイク停止） |
| **Show Debug Log** | ✅ チェック（開発中） |

```
┌─────────────────────────────────────────────┐
│ FaceTriggeredVoiceInput (Script)            │
├─────────────────────────────────────────────┤
│ 参照                                        │
│   Face Detector: [FacePresenceDetector]     │
│   Audio Recorder: [AudioRecorder]           │
│                                             │
│ 設定                                        │
│   ☑ Enable Face Trigger                     │
│   Activation Delay: 0.5                     │
│   Deactivation Grace Period: 2.0            │
│                                             │
│ デバッグ                                    │
│   ☑ Show Debug Log                          │
└─────────────────────────────────────────────┘
```

---

## 3. VoiceCommandManager の設定

### 手順 3.1: TriggerModeの設定

`VoiceCommandManager`コンポーネントのInspectorで：

| フィールド | 設定内容 |
|-----------|----------|
| **Trigger Mode** | `FaceTriggered`（本番用）または`Both`（デバッグ併用） |
| **Face Triggered Input** | 手順2で追加した`FaceTriggeredVoiceInput`をドラッグ |

### TriggerModeの選択肢

| モード | 動作 | 用途 |
|--------|------|------|
| `DebugOnly` | 3本指タッチ/Rキーのみ | テスト専用 |
| `FaceTriggered` | 顔認識+音量のみ | **本番用（推奨）** |
| `Both` | 両方有効 | デバッグ＋本番同時テスト |

```
┌─────────────────────────────────────────────┐
│ VoiceCommandManager (Script)                │
├─────────────────────────────────────────────┤
│ トリガーモード                              │
│   Trigger Mode: [FaceTriggered ▼]           │
│                                             │
│ 顔認識トリガー                              │
│   Face Triggered Input: [FaceTriggered...] │
└─────────────────────────────────────────────┘
```

---

## 4. デバッグビューのセットアップ（カメラプレビュー付き）

カメラ映像と検出状態を画面に表示するデバッグUIを作成します。

### 手順 4.1: 既存のDebugCanvasを使用

シーン内に既にある`DebugCanvas`を使用します。

### 手順 4.2: FaceVoiceDebugPanel の作成

DebugCanvas内に以下の構造を作成：

```
DebugCanvas (既存)
├── DebugPanel (既存)
└── FaceVoiceDebugPanel (新規) ← 追加
    ├── CameraPreview (RawImage)
    ├── StatusText (TextMeshPro)
    ├── FaceIndicator (Image)
    └── MicIndicator (Image)
```

#### 4.2.1: FaceVoiceDebugPanel
1. DebugCanvas内で右クリック → **UI** → **Panel**
2. 名前を「FaceVoiceDebugPanel」に変更
3. **RectTransform**設定：
   - Anchor: 右上
   - Pivot: (1, 1)
   - Pos X: -10, Pos Y: -10
   - Width: 200, Height: 250
4. **Image**コンポーネント：
   - Color: 黒（Alpha: 0.8）

#### 4.2.2: CameraPreview
1. FaceVoiceDebugPanel内で右クリック → **UI** → **Raw Image**
2. 名前を「CameraPreview」に変更
3. **RectTransform**設定：
   - Anchor: 上中央
   - Pos Y: -10
   - Width: 160, Height: 120

#### 4.2.3: StatusText
1. FaceVoiceDebugPanel内で右クリック → **UI** → **Text - TextMeshPro**
2. 名前を「StatusText」に変更
3. **RectTransform**設定：
   - Anchor: 上中央
   - Pos Y: -140
   - Width: 180, Height: 100
4. **TextMeshPro**設定：
   - Font Size: 12
   - Alignment: Left
   - Color: 白

#### 4.2.4: FaceIndicator / MicIndicator
1. FaceVoiceDebugPanel内で右クリック → **UI** → **Image**
2. それぞれ「FaceIndicator」「MicIndicator」と命名
3. **RectTransform**設定：
   - Width: 20, Height: 20
   - 横並びに配置（下部）
4. **Image**設定：
   - Color: グレー（初期状態）
   - Image Type: Simple

### 手順 4.3: VoiceCommandDebugView コンポーネントの追加

1. 「DebugCanvas」オブジェクト（または`FaceVoiceDebugPanel`）を選択
2. 「Add Component」→「VoiceCommandDebugView」
3. Inspectorで参照を設定：

| フィールド | 設定内容 |
|-----------|----------|
| **Debug Panel** | FaceVoiceDebugPanel オブジェクト |
| **Camera Preview** | CameraPreview (RawImage) |
| **Status Text** | StatusText (TextMeshPro) |
| **Face Indicator** | FaceIndicator (Image) |
| **Mic Indicator** | MicIndicator (Image) |
| **Face Detector** | シーン内のFacePresenceDetector |
| **Face Triggered Input** | FaceTriggeredVoiceInput |
| **Voice Command Manager** | VoiceCommandManager |
| **Audio Recorder** | AudioRecorder |
| **Show Camera Preview** | ✅ チェック |
| **Toggle Key** | F1（お好みで変更） |
| **Start Visible** | ✅ チェック（開発中は表示） |

```
┌─────────────────────────────────────────────┐
│ VoiceCommandDebugView (Script)              │
├─────────────────────────────────────────────┤
│ UI References                               │
│   Debug Panel: [FaceVoiceDebugPanel]        │
│   Camera Preview: [CameraPreview]           │
│   Status Text: [StatusText]                 │
│   Face Indicator: [FaceIndicator]           │
│   Mic Indicator: [MicIndicator]             │
│                                             │
│ Indicator Colors                            │
│   Active Color: [緑]                        │
│   Inactive Color: [グレー]                  │
│   Recording Color: [赤]                     │
│                                             │
│ References                                  │
│   Face Detector: [FacePresenceDetector]     │
│   Face Triggered Input: [FaceTriggered...]  │
│   Voice Command Manager: [VoiceCommand...]  │
│   Audio Recorder: [AudioRecorder]           │
│                                             │
│ Camera Preview Settings                     │
│   ☑ Show Camera Preview                     │
│   Preview Width: 160                        │
│   Preview Height: 120                       │
│                                             │
│ Toggle                                      │
│   Toggle Key: F1                            │
│   ☑ Start Visible                           │
└─────────────────────────────────────────────┘
```

---

## 5. 動作確認

### チェックリスト

| # | 確認項目 | 期待結果 |
|---|---------|----------|
| 1 | アプリ起動 | デバッグパネルが表示される |
| 2 | カメラプレビュー | 自分の顔が映る |
| 3 | 顔を映す | 「顔検出: 検出中」と表示、Face Indicatorが緑に |
| 4 | 0.5秒待機 | 「マイク: ON (顔検出)」と表示、Mic Indicatorが緑に |
| 5 | 声を出す | 「VAD: 音声検出中」と表示 |
| 6 | コマンド発話（例：「おすわり」） | 犬がお座りする |
| 7 | 顔を隠す | 2秒後に「マイク: OFF」と表示 |
| 8 | F1キー | デバッグパネルの表示/非表示が切り替わる |

### コンソールログ確認

正常動作時のログ例：
```
[FaceTriggeredVoiceInput] Face detected - will activate microphone after delay
[FaceTriggeredVoiceInput] Microphone ACTIVATED (face present)
[AudioRecorder] Continuous recording started (VAD mode)
[AudioRecorder] VAD: Speech detected
[AudioRecorder] VAD: Speech ended (24000 samples)
[VoiceCommandManager] 🎙️ Face-triggered recording received: 24000 samples
[VoiceCommandManager] ✅ 実行完了 (おすわり)
```

---

## 6. トラブルシューティング

### 問題：カメラプレビューが表示されない

**原因と対処：**
1. WebCamTextureがまだ初期化されていない
   → アプリ起動後数秒待つ
2. FacePresenceDetectorへの参照が未設定
   → Inspectorで設定を確認
3. カメラ権限が許可されていない（Android）
   → アプリ設定でカメラ権限を許可

### 問題：顔検出されてもマイクがONにならない

**原因と対処：**
1. `enableFaceTrigger`がfalse
   → FaceTriggeredVoiceInputのInspectorで確認
2. TriggerModeが`DebugOnly`
   → VoiceCommandManagerで`FaceTriggered`または`Both`に変更
3. AudioRecorderへの参照が未設定
   → Inspectorで設定を確認

### 問題：声を出してもコマンドが実行されない

**原因と対処：**
1. VADのしきい値が高すぎる
   → AudioRecorderの`vadEnergyThreshold`を`0.005`に下げる
2. OpenAI APIキーが未設定
   → VoiceCommandConfigまたは環境変数を確認
3. コマンドが認識リストにない
   → VoiceCommandRegistryのログを確認

---

## 7. 本番リリース前の設定

| 項目 | 開発時 | 本番時 |
|------|--------|--------|
| TriggerMode | Both | **FaceTriggered** |
| Show Debug Log | ✅ | ❌ |
| Debug Panel Start Visible | ✅ | ❌ |
| VoiceInputDetector.enableDebugTrigger | ✅ | ❌ |

---

## 8. 階層構造サマリー

```
シーン
├── FaceDetectionManager
│   └── FacePresenceDetector (既存)
│       └── WebCamTextureToMatHelper
│
├── VoiceCommandSystem
│   └── VoiceCommandManager (既存)
│       ├── VoiceInputDetector (RequireComponent)
│       ├── AudioRecorder (RequireComponent)
│       └── FaceTriggeredVoiceInput ← 新規追加
│
├── WakeWordSystem (スタンドアロン・バックアップ用)
│   └── WakeWordManager
│       └── WakeWordDetector
│   ※ VoiceCommandManagerとの連携は切断済み
│
└── UI
    └── DebugCanvas (既存)
        ├── DebugPanel (既存)
        └── FaceVoiceDebugPanel ← 新規追加
            ├── CameraPreview (RawImage)
            ├── StatusText (TextMeshPro)
            ├── FaceIndicator (Image)
            └── MicIndicator (Image)
```

---

## クイックスタート（最小構成）

デバッグUIなしで最速でセットアップする場合：

1. `VoiceCommandManager`オブジェクトに`FaceTriggeredVoiceInput`を追加
2. `FaceTriggeredVoiceInput`の`Face Detector`にシーン内の`FacePresenceDetector`を設定
3. `VoiceCommandManager`の`Trigger Mode`を`FaceTriggered`に変更
4. `VoiceCommandManager`の`Face Triggered Input`に追加したコンポーネントを設定
5. 再生して顔を映し、コマンドを発話してテスト
