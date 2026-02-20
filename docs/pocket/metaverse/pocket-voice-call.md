# 通話機能（高齢者向け音量ブースト）

## 1. 概要

メタバース内での音声通話機能。高齢者の聴力に配慮した音量ブースト機能を搭載し、他のユーザーとの会話を支援する。

---

## 2. 高齢者向け音声課題

### 2.1 課題分析

```
┌─────────────────────────────────────────────────────────────┐
│                    高齢者の音声通話課題                      │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│   【聴力の問題】                                             │
│   ├── 高周波が聞こえにくい（子音が不明瞭）                  │
│   ├── 全体的な聴力低下                                     │
│   └── 雑音の中で会話を聞き分けにくい                       │
│                                                             │
│   【発話の問題】                                             │
│   ├── 声が小さい                                          │
│   ├── 滑舌が不明瞭                                        │
│   └── マイクから離れて話す                                 │
│                                                             │
│   【環境の問題】                                             │
│   ├── スマホのスピーカー音質                               │
│   ├── 周囲の雑音                                          │
│   └── ネットワーク遅延                                     │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 2.2 解決アプローチ

| 課題 | 解決策 |
|------|--------|
| 聴力低下 | 音量ブースト + イコライザー |
| 高周波が聞こえにくい | 低周波強調 |
| 声が小さい | AGC（自動ゲイン調整）強め |
| 雑音 | ノイズ抑制強め |
| 不明瞭な発話 | VAD感度調整 |

---

## 3. 音量ブースト機能

### 3.1 ElderlyVoiceEnhancer

```csharp
using UnityEngine;
using Photon.Voice.Unity;

namespace TapHouse.Pocket.Metaverse
{
    /// <summary>
    /// 高齢者向け音声強化機能
    /// </summary>
    public class ElderlyVoiceEnhancer : MonoBehaviour
    {
        [Header("音量設定")]
        [SerializeField, Range(1f, 3f)]
        private float volumeBoost = 2f;           // 音量ブースト倍率

        [Header("イコライザー")]
        [SerializeField]
        private bool enableEqualizer = true;

        [SerializeField, Range(0f, 2f)]
        private float lowFrequencyBoost = 1.5f;   // 低周波ブースト

        [SerializeField, Range(0f, 2f)]
        private float midFrequencyBoost = 1.2f;   // 中周波ブースト

        [SerializeField, Range(0f, 1f)]
        private float highFrequencyBoost = 0.8f;  // 高周波（下げる）

        [Header("Photon Voice")]
        [SerializeField]
        private Speaker speaker;

        private AudioSource audioSource;
        private AudioLowPassFilter lowPassFilter;
        private AudioHighPassFilter highPassFilter;

        private void Start()
        {
            audioSource = speaker.GetComponent<AudioSource>();

            // 音量ブースト適用
            ApplyVolumeBoost();

            // イコライザー設定
            if (enableEqualizer)
            {
                SetupEqualizer();
            }
        }

        private void ApplyVolumeBoost()
        {
            // 基本音量を上げる
            audioSource.volume = Mathf.Clamp(volumeBoost, 0f, 1f);

            // AudioMixerでさらにブースト
            // ※実際にはAudioMixerGroupを使用
        }

        private void SetupEqualizer()
        {
            // 簡易イコライザー（フィルターの組み合わせ）

            // 高周波をカット（聞こえにくい周波数を下げる）
            lowPassFilter = gameObject.AddComponent<AudioLowPassFilter>();
            lowPassFilter.cutoffFrequency = 4000f;  // 4kHz以上をカット
            lowPassFilter.lowpassResonanceQ = 1f;

            // 低周波をブースト（聞きやすい周波数を強調）
            // ※Unity標準では難しいため、AudioMixer使用を推奨
        }

        /// <summary>
        /// 音量ブースト設定（UI設定画面から）
        /// </summary>
        public void SetVolumeBoost(float boost)
        {
            volumeBoost = Mathf.Clamp(boost, 1f, 3f);
            ApplyVolumeBoost();
        }

        /// <summary>
        /// プリセット：聴力が少し低下
        /// </summary>
        public void ApplyMildHearingLossPreset()
        {
            volumeBoost = 1.5f;
            lowFrequencyBoost = 1.2f;
            midFrequencyBoost = 1.1f;
            highFrequencyBoost = 0.9f;
            ApplySettings();
        }

        /// <summary>
        /// プリセット：聴力が低下
        /// </summary>
        public void ApplyModerateHearingLossPreset()
        {
            volumeBoost = 2.0f;
            lowFrequencyBoost = 1.5f;
            midFrequencyBoost = 1.3f;
            highFrequencyBoost = 0.7f;
            ApplySettings();
        }

        /// <summary>
        /// プリセット：聴力がかなり低下
        /// </summary>
        public void ApplySevereHearingLossPreset()
        {
            volumeBoost = 2.5f;
            lowFrequencyBoost = 1.8f;
            midFrequencyBoost = 1.5f;
            highFrequencyBoost = 0.5f;
            ApplySettings();
        }

        private void ApplySettings()
        {
            ApplyVolumeBoost();
            // イコライザー設定も更新
        }
    }
}
```

---

## 4. Recorder設定（送信側）

### 4.1 高齢者向けRecorder設定

```csharp
using Photon.Voice.Unity;
using POpusCodec.Enums;

namespace TapHouse.Pocket.Metaverse
{
    public class ElderlyRecorderSettings : MonoBehaviour
    {
        [SerializeField] private Recorder recorder;

        public void ApplyElderlySettings()
        {
            // AGC（自動ゲイン調整）を強めに
            recorder.UseAGC = true;

            // ノイズ抑制を有効に
            recorder.UseNS = true;

            // エコーキャンセル
            recorder.UseAEC = true;

            // VAD感度を調整（小さい声も拾う）
            recorder.VoiceDetection = true;
            recorder.VoiceDetectionThreshold = 0.005f;  // 通常より低め

            // サンプリングレートとビットレート
            recorder.SamplingRate = SamplingRate.Sampling24000;  // 24kHz
            recorder.Bitrate = 30000;  // 30kbps

            // フレーム長
            recorder.FrameDuration = OpusCodec.FrameDuration.Frame20ms;
        }
    }
}
```

---

## 5. 音量調整UI

### 5.1 通話設定画面

```
┌─────────────────────────────────────┐
│ ← 戻る      通話設定                │
├─────────────────────────────────────┤
│                                     │
│   ┌─────────────────────────────┐   │
│   │   🔊 相手の声の音量         │   │
│   │                             │   │
│   │   小 ────────────●── 大     │   │ ← スライダー
│   │                             │   │
│   └─────────────────────────────┘   │
│                                     │
│   ┌─────────────────────────────┐   │
│   │   🎤 自分の声の感度         │   │
│   │                             │   │
│   │   弱 ──────●──────── 強     │   │
│   │                             │   │
│   └─────────────────────────────┘   │
│                                     │
│   ┌─────────────────────────────┐   │
│   │   聴力プリセット             │   │
│   │                             │   │
│   │   [標準] [少し聞こえにくい]  │   │ ← プリセットボタン
│   │   [聞こえにくい]            │   │
│   │                             │   │
│   └─────────────────────────────┘   │
│                                     │
│   ┌─────────────────────────────┐   │
│   │   [テスト通話]               │   │ ← 自分の声を確認
│   └─────────────────────────────┘   │
│                                     │
└─────────────────────────────────────┘
```

### 5.2 VoiceSettingsUI

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TapHouse.Pocket.Metaverse
{
    public class VoiceSettingsUI : MonoBehaviour
    {
        [Header("音量スライダー")]
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private TMP_Text volumeLabel;

        [Header("感度スライダー")]
        [SerializeField] private Slider sensitivitySlider;
        [SerializeField] private TMP_Text sensitivityLabel;

        [Header("プリセットボタン")]
        [SerializeField] private Button normalPresetButton;
        [SerializeField] private Button mildPresetButton;
        [SerializeField] private Button moderatePresetButton;

        [Header("テスト通話")]
        [SerializeField] private Button testButton;
        [SerializeField] private TMP_Text testStatusText;

        [Header("Components")]
        [SerializeField] private ElderlyVoiceEnhancer voiceEnhancer;
        [SerializeField] private ElderlyRecorderSettings recorderSettings;

        private void Start()
        {
            // スライダー設定
            volumeSlider.minValue = 1f;
            volumeSlider.maxValue = 3f;
            volumeSlider.value = 2f;

            sensitivitySlider.minValue = 0.001f;
            sensitivitySlider.maxValue = 0.02f;
            sensitivitySlider.value = 0.005f;

            // イベント登録
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);

            normalPresetButton.onClick.AddListener(() => ApplyPreset(0));
            mildPresetButton.onClick.AddListener(() => ApplyPreset(1));
            moderatePresetButton.onClick.AddListener(() => ApplyPreset(2));

            testButton.onClick.AddListener(StartTestCall);
        }

        private void OnVolumeChanged(float value)
        {
            voiceEnhancer.SetVolumeBoost(value);
            volumeLabel.text = $"{value:F1}倍";
        }

        private void OnSensitivityChanged(float value)
        {
            // VAD感度を設定
        }

        private void ApplyPreset(int preset)
        {
            switch (preset)
            {
                case 0:
                    // 標準
                    volumeSlider.value = 1.5f;
                    break;
                case 1:
                    voiceEnhancer.ApplyMildHearingLossPreset();
                    volumeSlider.value = 1.5f;
                    break;
                case 2:
                    voiceEnhancer.ApplyModerateHearingLossPreset();
                    volumeSlider.value = 2.0f;
                    break;
            }
        }

        private void StartTestCall()
        {
            // マイク入力を録音して再生
            StartCoroutine(TestCallCoroutine());
        }

        private System.Collections.IEnumerator TestCallCoroutine()
        {
            testStatusText.text = "録音中...3秒間話してください";

            // 3秒間録音
            yield return new WaitForSeconds(3f);

            testStatusText.text = "再生中...";

            // 録音を再生

            yield return new WaitForSeconds(3f);

            testStatusText.text = "";
        }
    }
}
```

---

## 6. 近接音声との連携

### 6.1 ProximityVoiceWithEnhancer

```csharp
using UnityEngine;
using Photon.Voice.Unity;

namespace TapHouse.Pocket.Metaverse
{
    /// <summary>
    /// 近接音声 + 高齢者向け音量ブースト
    /// </summary>
    public class ProximityVoiceWithEnhancer : MonoBehaviour
    {
        [Header("近接音声設定")]
        [SerializeField] private float maxHearingDistance = 10f;
        [SerializeField] private float fullVolumeDistance = 2f;

        [Header("高齢者向け設定")]
        [SerializeField] private float elderlyVolumeBoost = 2f;

        [Header("Components")]
        [SerializeField] private Speaker speaker;
        [SerializeField] private ElderlyVoiceEnhancer voiceEnhancer;

        private Transform localPlayer;
        private AudioSource audioSource;

        private void Start()
        {
            audioSource = speaker.GetComponent<AudioSource>();
            FindLocalPlayer();
        }

        private void Update()
        {
            if (localPlayer == null) return;

            UpdateVolumeByDistance();
        }

        private void UpdateVolumeByDistance()
        {
            float distance = Vector3.Distance(transform.position, localPlayer.position);

            // 基本の距離減衰
            float baseVolume = CalculateDistanceVolume(distance);

            // 高齢者向けブースト適用
            float boostedVolume = baseVolume * elderlyVolumeBoost;

            // 最大1.0にクランプ（後段のAudioMixerでさらにブースト）
            audioSource.volume = Mathf.Clamp01(boostedVolume);
        }

        private float CalculateDistanceVolume(float distance)
        {
            if (distance <= fullVolumeDistance)
            {
                return 1f;
            }
            else if (distance >= maxHearingDistance)
            {
                return 0f;
            }
            else
            {
                float t = (distance - fullVolumeDistance) / (maxHearingDistance - fullVolumeDistance);
                return 1f - t;
            }
        }

        private void FindLocalPlayer()
        {
            // ローカルプレイヤーを検索
        }
    }
}
```

---

## 7. 音声レベルインジケーター

### 7.1 入力レベル表示

```
┌─────────────────────────────────────┐
│                                     │
│   🎤 あなたの声                     │
│   ┌─────────────────────────────┐   │
│   │ ████████░░░░░░░░░░░░░░░░░  │   │ ← 入力レベル
│   └─────────────────────────────┘   │
│   「声が入力されています」          │
│                                     │
│   🔊 相手の声                       │
│   ┌─────────────────────────────┐   │
│   │ ██████████████░░░░░░░░░░░  │   │ ← 出力レベル
│   └─────────────────────────────┘   │
│                                     │
└─────────────────────────────────────┘
```

### 7.2 VoiceLevelIndicator

```csharp
using UnityEngine;
using UnityEngine.UI;
using Photon.Voice.Unity;

namespace TapHouse.Pocket.Metaverse
{
    public class VoiceLevelIndicator : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Slider inputLevelSlider;
        [SerializeField] private Slider outputLevelSlider;
        [SerializeField] private TMP_Text inputStatusText;

        [Header("Components")]
        [SerializeField] private Recorder recorder;

        private void Update()
        {
            UpdateInputLevel();
        }

        private void UpdateInputLevel()
        {
            // マイク入力レベルを取得
            float level = recorder.LevelMeter.CurrentAvgAmp;
            inputLevelSlider.value = level;

            // ステータス表示
            if (recorder.IsCurrentlyTransmitting)
            {
                inputStatusText.text = "声が入力されています";
                inputStatusText.color = Color.green;
            }
            else if (level > 0.01f)
            {
                inputStatusText.text = "もう少し大きな声で";
                inputStatusText.color = Color.yellow;
            }
            else
            {
                inputStatusText.text = "マイクに向かって話してください";
                inputStatusText.color = Color.gray;
            }
        }
    }
}
```

---

## 8. エラーハンドリング

### 8.1 マイク関連エラー

```csharp
public class VoiceErrorHandler : MonoBehaviour
{
    public void HandleMicrophoneError(string error)
    {
        if (error.Contains("permission"))
        {
            ShowPermissionError();
        }
        else if (error.Contains("device"))
        {
            ShowDeviceError();
        }
        else
        {
            ShowGenericError(error);
        }
    }

    private void ShowPermissionError()
    {
        PocketDialog.Show(
            "マイクの使用が許可されていません。\n設定アプリから許可してください。",
            "設定を開く",
            "キャンセル",
            onPositive: OpenSettings
        );
    }

    private void ShowDeviceError()
    {
        PocketDialog.Show(
            "マイクが見つかりません。\nミュート状態で開始します。",
            "OK"
        );
    }
}
```

---

## 9. テストケース

| # | テスト内容 | 期待結果 |
|---|-----------|---------|
| 1 | 音量ブースト設定 | 音量が増幅される |
| 2 | プリセット適用 | 設定値が一括変更 |
| 3 | テスト通話 | 自分の声が聞こえる |
| 4 | 入力レベル表示 | リアルタイムで更新 |
| 5 | 近接音声 | 距離に応じて音量変化 |
| 6 | マイク権限なし | ミュート状態で開始 |
| 7 | 聴力プリセット | イコライザー適用 |

---

## 10. 関連ドキュメント

| ドキュメント | 説明 |
|-------------|------|
| [../../metaverse_walk/metaverse_walk_voice.md](../../metaverse_walk/metaverse_walk_voice.md) | たっぷハウス版通話機能 |
| [pocket-metaverse-overview.md](./pocket-metaverse-overview.md) | ポケット版メタバース概要 |

---

## 11. 新規ファイル一覧

| ファイル | 役割 |
|----------|------|
| `Assets/Scripts/Pocket/Metaverse/ElderlyVoiceEnhancer.cs` | 音量ブースト |
| `Assets/Scripts/Pocket/Metaverse/ElderlyRecorderSettings.cs` | Recorder設定 |
| `Assets/Scripts/Pocket/Metaverse/VoiceSettingsUI.cs` | 設定UI |
| `Assets/Scripts/Pocket/Metaverse/ProximityVoiceWithEnhancer.cs` | 近接音声+ブースト |
| `Assets/Scripts/Pocket/Metaverse/VoiceLevelIndicator.cs` | レベル表示 |
| `Assets/Scripts/Pocket/Metaverse/VoiceErrorHandler.cs` | エラー処理 |
