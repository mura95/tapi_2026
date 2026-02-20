# 音声処理パイプライン - ノイズ問題分析

## 現在の音声処理フロー

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           音声処理パイプライン                               │
└─────────────────────────────────────────────────────────────────────────────┘

┌──────────────┐
│  マイク入力   │  Unity Microphone API
│  (16kHz)     │  AudioRecorder.cs
└──────┬───────┘
       │
       ▼
┌──────────────────────────────────────────┐
│  リアルタイム処理 (applyRealtimeProcessing)│
│  RealtimeAudioProcessor.cs               │
│  ┌────────────────────────────────────┐  │
│  │ 1. Pre-emphasis (係数: 0.97)      │  │
│  │ 2. Highpass Filter (300Hz)        │  │
│  │ 3. Lowpass Filter (3400Hz)        │  │
│  └────────────────────────────────────┘  │
└──────┬───────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────┐
│  VAD (Voice Activity Detection)          │
│  VoiceActivityDetector.cs                │
│  ┌────────────────────────────────────┐  │
│  │ - RMSエネルギー計算                │  │
│  │ - しきい値: 0.01                   │  │
│  │ - 無音判定: 0.5秒                  │  │
│  │ - 最小録音: 0.3秒                  │  │
│  │ - 最大録音: 3秒                    │  │
│  └────────────────────────────────────┘  │
└──────┬───────────────────────────────────┘
       │ 音声検出時
       ▼
┌──────────────────────────────────────────┐
│  最終処理 (applyProcessing)               │
│  AdvancedAudioFilters.cs                 │
│  ┌────────────────────────────────────┐  │
│  │ 1. リサンプリング（必要時）        │  │
│  │ 2. Pre-emphasis (係数: 0.97) ⚠️    │  │
│  │ 3. Highpass Filter (300Hz) ⚠️      │  │
│  │ 4. Lowpass Filter (3400Hz) ⚠️      │  │
│  │ 5. Spectral Subtraction           │  │
│  │    (最初0.3秒をノイズとして除去)   │  │
│  │ 6. RMS正規化 (-20dB)              │  │
│  └────────────────────────────────────┘  │
└──────┬───────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────┐
│  WAVファイル保存                          │
│  AudioRecorder_FileIO.cs                 │
└──────────────────────────────────────────┘
```

---

## 問題点の特定

### 問題1: フィルターの二重適用 ⚠️ 重大

| 処理 | リアルタイム処理 | 最終処理 | 結果 |
|------|-----------------|----------|------|
| Pre-emphasis | ✅ 適用 | ✅ 適用 | **2回適用** → 高周波が過度に強調 |
| Highpass (300Hz) | ✅ 適用 | ✅ 適用 | **2回適用** → 低周波が過度にカット |
| Lowpass (3400Hz) | ✅ 適用 | ✅ 適用 | **2回適用** → 高周波が過度にカット |

**影響**: 音声が不自然に歪む、ノイズが増幅される可能性

### 問題2: スペクトラルサブトラクションの誤動作

```
現在の動作:
┌─────────────────────────────────────────┐
│ 録音データ: [0.0s ─────────────── 1.5s] │
│              ↑                          │
│         最初0.3秒を                      │
│         ノイズプロファイルとして使用      │
└─────────────────────────────────────────┘
```

**問題**: VADが音声を検出してから録音を開始するため、最初の0.3秒には**すでに音声が含まれている**可能性が高い。

→ 音声の一部がノイズとして扱われ、除去されてしまう

### 問題3: VADしきい値が低すぎる

| 設定 | 現在の値 | 問題 |
|------|---------|------|
| energyThreshold | 0.01 | 環境ノイズでも反応する可能性 |

**影響**: ノイズだけの録音が生成される

### 問題4: ノイズゲートがない

現在、しきい値以下の音量でも録音データに含まれる。
ノイズゲートで小さなノイズを完全にカットする処理がない。

---

## 現在の設定値一覧

### AudioRecorder.cs

```csharp
[SerializeField] private int sampleRate = 16000;
[SerializeField] private bool applyRealtimeProcessing = true;  // リアルタイム処理
[SerializeField] private bool applyProcessing = true;          // 最終処理
[SerializeField] private float vadEnergyThreshold = 0.01f;
[SerializeField] private float vadSilenceDuration = 0.5f;
[SerializeField] private float vadMaxRecordingDuration = 3f;
[SerializeField] private int chunkSize = 1024;
```

### RealtimeAudioProcessor.cs

```csharp
private const float PRE_EMPHASIS_COEF = 0.97f;
private const float LOW_FREQ_HZ = 300f;
private const float HIGH_FREQ_HZ = 3400f;
```

### AdvancedAudioFilters.cs

```csharp
private const float PRE_EMPHASIS_COEF = 0.97f;
private const float NOISE_DURATION_SEC = 0.3f;  // ノイズプロファイル用
private const float RMS_TARGET_DB = -20f;
// Bandpass: 300Hz - 3400Hz
```

### VoiceActivityDetector.cs

```csharp
private const float energyThreshold = 0.01f;
private const float silenceDuration = 0.5f;
private const float maxRecordingDuration = 3f;
private const float minRecordingDuration = 0.3f;
```

---

## 改善案

### 案A: 二重処理の解消（推奨）

リアルタイム処理と最終処理の役割を明確に分離：

```
リアルタイム処理（VAD用）:
- 軽量なフィルタリングのみ
- VAD判定の精度向上が目的

最終処理（API送信用）:
- 完全なフィルタリング
- ノイズ除去
- 正規化
```

**具体的な変更**:
1. `applyRealtimeProcessing = false` に設定
2. または、最終処理で二重適用を避けるロジックを追加

### 案B: ノイズプロファイルの改善

VAD検出前のデータをノイズプロファイルとして使用：

```
改善後:
┌─────────────────────────────────────────────────┐
│ [ノイズ区間] ─ VAD検出 ─ [音声区間]              │
│      ↑                                          │
│ この部分をノイズプロファイルに                    │
└─────────────────────────────────────────────────┘
```

### 案C: ノイズゲートの追加

```csharp
// しきい値以下の音量を完全にゼロに
float noiseGateThreshold = 0.005f;
for (int i = 0; i < samples.Length; i++)
{
    if (Mathf.Abs(samples[i]) < noiseGateThreshold)
        samples[i] = 0f;
}
```

### 案D: VADしきい値の調整

```csharp
// 現在: 0.01（敏感すぎる）
// 推奨: 0.02〜0.03（環境に応じて調整）
vadEnergyThreshold = 0.02f;
```

---

## テスト用設定

ノイズ問題を切り分けるため、以下の設定でテストを推奨：

### テスト1: 処理なし（生音声）

```
AudioRecorder:
  applyRealtimeProcessing = false
  applyProcessing = false
```

### テスト2: リアルタイム処理のみ

```
AudioRecorder:
  applyRealtimeProcessing = true
  applyProcessing = false
```

### テスト3: 最終処理のみ

```
AudioRecorder:
  applyRealtimeProcessing = false
  applyProcessing = true
```

### テスト4: 両方（現在の設定）

```
AudioRecorder:
  applyRealtimeProcessing = true
  applyProcessing = true
```

---

## ファイル一覧

| ファイル | 役割 |
|----------|------|
| `AudioRecorder.cs` | 録音制御、設定管理 |
| `AudioRecorder_FileIO.cs` | WAV保存 |
| `RealtimeAudioProcessor.cs` | リアルタイムフィルタリング |
| `AdvancedAudioFilters.cs` | 最終処理パイプライン |
| `VoiceActivityDetector.cs` | 音声検出 |

---

## テスト結果 ✅

**2024年テスト実施結果:**

| パターン | 結果 |
|---------|------|
| 01_Raw_NoProcessing | **✅ 最良** |
| 02_RealtimeOnly | ノイズあり |
| 03_FinalOnly | ノイズあり |
| 04_Both_CurrentSetting | ノイズあり |

**結論:** 音声処理（Pre-emphasis、Bandpass Filter、Spectral Subtraction）はWhisper APIの認識精度を下げていた。

**対応:**
- `AudioRecorder.cs`の設定を変更:
  - `applyRealtimeProcessing = false`
  - `applyProcessing = false`
- Whisper APIは生の音声データで最も良く動作する

---

## 次のステップ

1. [x] テスト1〜4を実行して、どの処理がノイズの原因か特定 → **処理なしが最良**
2. [x] 二重処理の解消 → **両方の処理をOFFに設定**
3. [ ] ~~ノイズプロファイル取得方法の改善~~ → 不要
4. [ ] ~~VADしきい値の最適化~~ → 現状維持（必要に応じて調整）
