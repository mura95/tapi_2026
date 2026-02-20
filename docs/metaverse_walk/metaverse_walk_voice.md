# 音声通話仕様書（フェーズ3）

## 1. 概要

Photon Voice 2 を使用した近接音声通話機能。他のユーザーに近づくと声が聞こえ、距離に応じて音量が変化する。

---

## ⚠️ 重要：音声品質と会話成立に関する懸念事項

### 本機能の実現可能性について

音声通話機能は技術的には実装可能だが、**実際に高齢者同士の会話が成立するかは検証が必要**。以下の懸念点を事前に把握し、対策を講じる必要がある。

### 主な懸念点

| 懸念 | 詳細 | リスクレベル |
|------|------|------------|
| **マイク音質** | スマホ内蔵マイクの品質差が大きい | 高 |
| **環境ノイズ** | 高齢者の生活環境は静かとは限らない | 中 |
| **遅延（レイテンシ）** | 会話のテンポが崩れる可能性 | 高 |
| **エコー・ハウリング** | スピーカー音がマイクに入る | 中 |
| **聴力の個人差** | 高齢者は聴力低下していることが多い | 高 |
| **発話の明瞭さ** | 声が小さい、滑舌が悪い場合 | 中 |

### 詳細分析

#### 1. マイク品質の問題

```
┌─────────────────────────────────────────────────────────┐
│                    マイク品質の現実                      │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  高品質端末（iPhone、高級Android）                       │
│  └── 比較的クリアな音声が期待できる                      │
│                                                         │
│  低〜中品質端末（格安スマホ、タブレット）                 │
│  └── マイク感度が低い、ノイズが多い                      │
│  └── 高齢者向けタブレットは特に注意                      │
│                                                         │
│  【対策】                                                │
│  ├── AGC（自動ゲイン調整）を有効にする                   │
│  ├── ノイズ抑制を強めに設定                             │
│  └── 外部マイク/ヘッドセット推奨をUI表示                │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

#### 2. 遅延（レイテンシ）の問題

| 遅延レベル | 時間 | 会話への影響 |
|-----------|------|------------|
| 良好 | 50-100ms | 自然な会話が可能 |
| 許容範囲 | 100-200ms | やや違和感あるが会話可能 |
| 問題あり | 200-400ms | 会話のテンポが崩れる |
| 会話困難 | 400ms以上 | 話が被る、沈黙が発生 |

**Photon Voice 2 の実績値:**
- 国内同士: 50-150ms（良好〜許容範囲）
- 海外同士: 150-300ms（許容範囲〜問題あり）

**モバイル回線での追加遅延:**
- 4G: +20-50ms
- 不安定な回線: +100ms以上

#### 3. 高齢者特有の課題

```
┌─────────────────────────────────────────────────────────┐
│                  高齢者向け音声通話の課題                 │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  【聴力の問題】                                          │
│  ├── 高周波が聞こえにくい（子音が聞き取れない）          │
│  ├── 音量を上げても明瞭度は上がらない                   │
│  └── 対策: 低周波を強調するイコライザー                 │
│                                                         │
│  【発話の問題】                                          │
│  ├── 声が小さい                                        │
│  ├── 滑舌が不明瞭                                      │
│  └── 対策: AGC強め、VAD感度調整                        │
│                                                         │
│  【操作の問題】                                          │
│  ├── ミュート解除を忘れる                              │
│  ├── 端末を口から離して話す                            │
│  └── 対策: 音声入力レベルインジケーター表示             │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

### 会話成立のための必須機能

| 機能 | 目的 | 優先度 |
|------|------|--------|
| **音声レベルインジケーター** | 自分の声が入力されているか確認 | 必須 |
| **接続状態表示** | 相手と繋がっているか確認 | 必須 |
| **音量調整スライダー** | 相手の声の音量を調整 | 必須 |
| **プッシュ・トゥ・トーク（オプション）** | ボタン押下中のみ送信 | 推奨 |
| **テスト通話機能** | 自分の声を確認できる | 推奨 |

### リスク軽減策

```csharp
// 推奨設定（高齢者向け）
public class ElderlyVoiceSettings
{
    // AGC（自動ゲイン調整）を強めに
    public bool UseAGC = true;
    public float AGCTargetLevel = 0.8f;  // 通常より高め

    // ノイズ抑制を有効に
    public bool UseNoiseSuppression = true;
    public int NoiseSuppressionLevel = 2;  // 0-3、高めに設定

    // エコーキャンセルを有効に
    public bool UseAEC = true;

    // 音量を大きめに
    public float DefaultSpeakerVolume = 0.9f;

    // VAD感度を調整（小さい声も拾う）
    public float VADThreshold = 0.005f;  // 通常より低め
}
```

### テスト・検証計画

| フェーズ | 内容 | 参加者 |
|---------|------|--------|
| **内部テスト** | 開発環境での基本動作確認 | 開発者2-3名 |
| **品質テスト** | 様々な端末・回線での音質確認 | テスター5名 |
| **ユーザーテスト** | 高齢者による実地テスト | 高齢者5-10名 |
| **負荷テスト** | 10人同時通話でのテスト | テスター10名 |

### テスト時の確認項目

- [ ] 声がクリアに聞こえるか
- [ ] 遅延を感じないか
- [ ] エコーやハウリングが発生しないか
- [ ] 複数人同時に話しても聞き分けられるか
- [ ] ミュート/ミュート解除が直感的にわかるか
- [ ] 音量調整が簡単にできるか
- [ ] 長時間（30分）使用しても問題ないか

### 代替案・フォールバック

会話品質が許容レベルに達しない場合の代替案:

| 代替案 | 説明 | 難易度 |
|--------|------|--------|
| **定型メッセージ送信** | 「こんにちは」「また会いましょう」等のボタン | 低 |
| **エモート/ジェスチャー** | 手を振る、お辞儀等のアニメーション | 低 |
| **音声を録音→送信** | プッシュトゥトークで録音し、非同期送信 | 中 |
| **別の音声サービス利用** | Agora、Discord等の検討 | 高 |

### 結論と推奨事項

1. **フェーズ3の前に必ずPoC（概念実証）を行う**
   - 高齢者5名以上で実際の会話をテスト
   - 許容可能な品質かを判断

2. **品質が不十分な場合**
   - エモート/定型メッセージでの非音声コミュニケーションに切り替え
   - 音声は「Nice to have」として位置づけ直す

3. **品質向上のための投資**
   - 外部マイク/ヘッドセットの推奨
   - 高品質端末の推奨リスト作成

---

## 2. Photon Voice 2 概要

### 2.1 主な機能

| 機能 | 説明 |
|------|------|
| リアルタイム音声 | 低遅延のボイスチャット |
| 音声エンコード | Opus コーデック |
| エコーキャンセル | 内蔵AEC |
| ノイズ抑制 | 内蔵NS |
| 3Dサウンド | 空間音声対応 |

### 2.2 Fusion 2 との統合

Photon Voice 2 は Photon Fusion 2 とシームレスに統合可能。同じ NetworkRunner を共有。

---

## 3. システム構成

### 3.1 アーキテクチャ

```
┌─────────────────────────────────────────────────────────┐
│                     Photon Cloud                         │
│  ┌─────────────────────┐  ┌─────────────────────┐       │
│  │   Fusion Server     │  │   Voice Server      │       │
│  │   (位置同期)         │  │   (音声ルーティング) │       │
│  └──────────┬──────────┘  └──────────┬──────────┘       │
│             │                        │                   │
│             └────────────┬───────────┘                   │
│                          │                               │
└──────────────────────────┼───────────────────────────────┘
                           │
              ┌────────────┼────────────┐
              ↓            ↓            ↓
           ┌─────┐     ┌─────┐     ┌─────┐
           │UserA│ ←→ │UserB│ ←→ │UserC│
           │ 🎤  │     │ 🎤  │     │ 🎤  │
           └─────┘     └─────┘     └─────┘

              近づくと音量アップ、離れると音量ダウン
```

### 3.2 コンポーネント構成

```
NetworkPlayer (Prefab)
├── NetworkObject
├── NetworkPlayerController
├── PhotonVoiceView
│   ├── Recorder (マイク入力)
│   └── Speaker (音声出力)
└── ProximityVoiceController
```

---

## 4. セットアップ

### 4.1 パッケージインストール

```
1. Unity Package Manager → Add package from git URL
   URL: https://github.com/photon-voice-sdk/voice.git

2. Photon Voice と Fusion の統合パッケージ
   URL: https://github.com/photon-fusion-sdk/fusion-voice-integration.git
```

### 4.2 App ID 設定

```csharp
// PhotonAppSettings.asset
App Id Voice: [Photon Dashboardから取得]
// Fusionと別のApp IDが必要
```

### 4.3 マイク権限（Android）

```xml
<!-- AndroidManifest.xml -->
<uses-permission android:name="android.permission.RECORD_AUDIO" />
```

---

## 5. VoiceChatManager

### 5.1 クラス設計

```csharp
using Photon.Voice.Unity;
using Photon.Voice.Fusion;
using UnityEngine;
using System;

public class VoiceChatManager : MonoBehaviour
{
    [Header("コンポーネント")]
    [SerializeField] private VoiceConnection voiceConnection;
    [SerializeField] private Recorder recorder;

    [Header("設定")]
    [SerializeField] private bool autoConnect = true;
    [SerializeField] private bool startMuted = false;

    public bool IsMuted { get; private set; }
    public bool IsConnected => voiceConnection != null && voiceConnection.Client.IsConnected;

    public event Action<bool> OnMuteStateChanged;

    public static VoiceChatManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (autoConnect)
        {
            Connect();
        }

        if (startMuted)
        {
            SetMute(true);
        }
    }

    public void Connect()
    {
        if (voiceConnection != null)
        {
            voiceConnection.ConnectUsingSettings();
        }
    }

    public void Disconnect()
    {
        if (voiceConnection != null)
        {
            voiceConnection.Client.Disconnect();
        }
    }

    public void SetMute(bool muted)
    {
        IsMuted = muted;

        if (recorder != null)
        {
            recorder.TransmitEnabled = !muted;
        }

        OnMuteStateChanged?.Invoke(muted);
        Debug.Log($"[VoiceChat] Mute: {muted}");
    }

    public void ToggleMute()
    {
        SetMute(!IsMuted);
    }
}
```

---

## 6. Recorder 設定

### 6.1 Recorder コンポーネント

```csharp
// Recorder設定（Inspector）
public class RecorderSettings
{
    public MicrophoneType MicrophoneType = MicrophoneType.Unity;
    public SamplingRate SamplingRate = SamplingRate.Sampling24000;
    public OpusCodec.FrameDuration FrameDuration = OpusCodec.FrameDuration.Frame20ms;
    public int Bitrate = 30000;  // 30kbps
    public bool VoiceDetection = true;
    public float VoiceDetectionThreshold = 0.01f;
}
```

### 6.2 推奨設定

| 設定 | 値 | 説明 |
|------|-----|------|
| Sampling Rate | 24000 Hz | モバイル向け最適 |
| Frame Duration | 20ms | 低遅延 |
| Bitrate | 30000 | 音質と帯域のバランス |
| Voice Detection | ON | 発話時のみ送信 |
| VAD Threshold | 0.01 | 感度 |

---

## 7. 近接音声（Proximity Voice）

### 7.1 ProximityVoiceController

```csharp
using Photon.Voice.Unity;
using UnityEngine;

public class ProximityVoiceController : MonoBehaviour
{
    [Header("近接音声設定")]
    [SerializeField] private float maxHearingDistance = 10f;  // 聞こえる最大距離
    [SerializeField] private float fullVolumeDistance = 2f;   // フル音量の距離
    [SerializeField] private AnimationCurve volumeCurve;       // 音量カーブ

    [Header("コンポーネント")]
    [SerializeField] private Speaker speaker;

    private Transform localPlayer;
    private AudioSource audioSource;

    private void Start()
    {
        audioSource = speaker.GetComponent<AudioSource>();

        // ローカルプレイヤーを検索
        FindLocalPlayer();

        // デフォルトの音量カーブを設定
        if (volumeCurve == null || volumeCurve.length == 0)
        {
            volumeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        }
    }

    private void FindLocalPlayer()
    {
        // Fusionのローカルプレイヤーを検索
        var players = FindObjectsOfType<NetworkPlayerController>();
        foreach (var player in players)
        {
            if (player.HasInputAuthority)
            {
                localPlayer = player.transform;
                break;
            }
        }
    }

    private void Update()
    {
        if (localPlayer == null)
        {
            FindLocalPlayer();
            return;
        }

        UpdateVolume();
    }

    private void UpdateVolume()
    {
        if (audioSource == null) return;

        // ローカルプレイヤーとの距離を計算
        float distance = Vector3.Distance(transform.position, localPlayer.position);

        // 距離に応じて音量を調整
        float volume = CalculateVolume(distance);
        audioSource.volume = volume;

        // 3Dサウンド位置を更新
        audioSource.spatialBlend = 1f;  // 完全3Dサウンド
    }

    private float CalculateVolume(float distance)
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
            // fullVolumeDistance〜maxHearingDistanceの範囲を0〜1に正規化
            float t = (distance - fullVolumeDistance) / (maxHearingDistance - fullVolumeDistance);
            return volumeCurve.Evaluate(t);
        }
    }
}
```

### 7.2 距離と音量の関係

```
音量
 1.0 ├────────┐
     │        │
     │        │
 0.5 │        ╲
     │         ╲
     │          ╲
 0.0 ├──────────┴─────────────
     0    2m   │         10m    距離
              fullVolume  maxHearing
```

### 7.3 パラメータ

| パラメータ | デフォルト値 | 説明 |
|------------|-------------|------|
| maxHearingDistance | 10m | 聞こえる最大距離 |
| fullVolumeDistance | 2m | フル音量の距離 |
| volumeCurve | EaseOut | 減衰カーブ |

---

## 8. Speaker 設定

### 8.1 Speaker コンポーネント

```csharp
// Speaker設定（3Dサウンド）
public class SpeakerSettings
{
    public float SpatialBlend = 1f;        // 3Dサウンド
    public float MinDistance = 1f;
    public float MaxDistance = 10f;
    public AudioRolloffMode RolloffMode = AudioRolloffMode.Linear;
}
```

### 8.2 AudioSource 設定

| 設定 | 値 |
|------|-----|
| Spatial Blend | 1.0 (3D) |
| Doppler Level | 0 |
| Spread | 0 |
| Min Distance | 1 |
| Max Distance | 10 |
| Rolloff Mode | Linear または Custom |

---

## 9. ミュートボタンUI

### 9.1 MuteButtonUI

```csharp
using UnityEngine;
using UnityEngine.UI;

public class MuteButtonUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button muteButton;
    [SerializeField] private Image buttonIcon;
    [SerializeField] private Sprite mutedSprite;
    [SerializeField] private Sprite unmutedSprite;

    [Header("色設定")]
    [SerializeField] private Color mutedColor = Color.red;
    [SerializeField] private Color unmutedColor = Color.white;

    private void Start()
    {
        muteButton.onClick.AddListener(OnMuteButtonClicked);
        VoiceChatManager.Instance.OnMuteStateChanged += UpdateUI;

        // 初期状態を反映
        UpdateUI(VoiceChatManager.Instance.IsMuted);
    }

    private void OnDestroy()
    {
        if (VoiceChatManager.Instance != null)
        {
            VoiceChatManager.Instance.OnMuteStateChanged -= UpdateUI;
        }
    }

    private void OnMuteButtonClicked()
    {
        VoiceChatManager.Instance.ToggleMute();
    }

    private void UpdateUI(bool isMuted)
    {
        if (isMuted)
        {
            buttonIcon.sprite = mutedSprite;
            buttonIcon.color = mutedColor;
        }
        else
        {
            buttonIcon.sprite = unmutedSprite;
            buttonIcon.color = unmutedColor;
        }
    }
}
```

### 9.2 UI デザイン

```
┌───────────────────────────────────────┐
│ [散歩やめる]              [🎤]        │
│                                       │
│              ↑                        │
│         ミュートボタン                 │
│         タップでON/OFF                 │
│                                       │
└───────────────────────────────────────┘

ミュート時:    [🎤] (赤、取り消し線)
ミュート解除時: [🎤] (白)
```

### 9.3 ボタン仕様

| 項目 | 仕様 |
|------|------|
| サイズ | 50×50 dp |
| 位置 | 右上 (margin: 20dp) |
| アイコン | マイクアイコン |
| ミュート時 | 赤色、斜線 |
| デフォルト | ミュート解除 |

---

## 10. マイク権限リクエスト

### 10.1 Android 権限

```csharp
using UnityEngine;
using UnityEngine.Android;

public class MicrophonePermission : MonoBehaviour
{
    public void RequestMicrophonePermission(System.Action<bool> callback)
    {
#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            PermissionCallbacks callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += (permission) =>
            {
                callback?.Invoke(true);
            };
            callbacks.PermissionDenied += (permission) =>
            {
                callback?.Invoke(false);
            };

            Permission.RequestUserPermission(Permission.Microphone, callbacks);
        }
        else
        {
            callback?.Invoke(true);
        }
#else
        callback?.Invoke(true);
#endif
    }
}
```

### 10.2 権限リクエストフロー

```
散歩開始
    ↓
マイク権限チェック
    ↓
┌─────────────────┐     ┌─────────────────┐
│ 権限なし         │ →  │ 権限リクエスト   │
└─────────────────┘     └────────┬────────┘
                                 │
              ┌──────────────────┴──────────────────┐
              ↓                                     ↓
    ┌─────────────────┐              ┌─────────────────┐
    │ 許可             │              │ 拒否             │
    │ 音声通話有効     │              │ ミュート状態で開始│
    └─────────────────┘              └─────────────────┘
```

---

## 11. 音声品質設定

### 11.1 品質プリセット

| プリセット | Bitrate | Sampling | 用途 |
|-----------|---------|----------|------|
| Low | 16kbps | 16000Hz | 帯域制限時 |
| Medium | 30kbps | 24000Hz | 標準（推奨） |
| High | 64kbps | 48000Hz | 高品質 |

### 11.2 推奨設定（モバイル）

```csharp
// モバイル向け最適設定
recorder.SamplingRate = POpusCodec.Enums.SamplingRate.Sampling24000;
recorder.FrameDuration = OpusCodec.FrameDuration.Frame20ms;
recorder.Bitrate = 30000;
```

---

## 12. エコーキャンセル

### 12.1 設定

```csharp
// Recorder設定
recorder.UseAEC = true;
recorder.UseNS = true;  // ノイズ抑制
recorder.UseAGC = true; // 自動ゲイン調整
```

### 12.2 高齢者向け配慮

| 配慮 | 実装 |
|------|------|
| 音量大きめ | デフォルトボリューム 0.8 |
| クリアな音声 | ノイズ抑制強め |
| 遅延最小化 | Frame Duration 20ms |

---

## 13. デバッグ機能

### 13.1 VoiceDebugUI

```csharp
using UnityEngine;
using TMPro;

public class VoiceDebugUI : MonoBehaviour
{
    [SerializeField] private TMP_Text debugText;
    [SerializeField] private Recorder recorder;
    [SerializeField] private bool showDebug = false;

    private void Update()
    {
        if (!showDebug) return;

        string status = $"Voice Status:\n";
        status += $"Connected: {VoiceChatManager.Instance.IsConnected}\n";
        status += $"Muted: {VoiceChatManager.Instance.IsMuted}\n";
        status += $"Transmitting: {recorder.IsCurrentlyTransmitting}\n";
        status += $"Level: {recorder.LevelMeter.CurrentAvgAmp:F3}\n";

        debugText.text = status;
    }
}
```

---

## 14. エラーハンドリング

### 14.1 接続エラー

```csharp
voiceConnection.Client.StateChanged += (state) =>
{
    switch (state)
    {
        case ClientState.ConnectedToMasterServer:
            Debug.Log("[Voice] Connected to master");
            break;
        case ClientState.Disconnected:
            Debug.LogWarning("[Voice] Disconnected");
            break;
    }
};
```

### 14.2 マイクエラー

```csharp
if (Microphone.devices.Length == 0)
{
    Debug.LogError("[Voice] No microphone found");
    // ミュート状態で開始
    VoiceChatManager.Instance.SetMute(true);
}
```

---

## 15. パフォーマンス最適化

### 15.1 帯域幅削減

| 技術 | 効果 |
|------|------|
| VAD (Voice Activity Detection) | 発話時のみ送信 |
| 低Bitrate | 帯域削減 |
| 距離カットオフ | 遠くの音声を無視 |

### 15.2 CPU最適化

```csharp
// 遠くのプレイヤーの音声処理をスキップ
if (distance > maxHearingDistance * 1.5f)
{
    speaker.enabled = false;
}
else
{
    speaker.enabled = true;
}
```

---

## 16. テストケース

| # | テスト内容 | 期待結果 |
|---|-----------|---------|
| 1 | 近距離で会話 | フル音量で聞こえる |
| 2 | 遠距離で会話 | 聞こえない |
| 3 | 距離を変えながら会話 | 音量が滑らかに変化 |
| 4 | ミュートON | 相手に声が聞こえない |
| 5 | ミュートOFF | 相手に声が聞こえる |
| 6 | マイク権限拒否 | ミュート状態で動作 |
| 7 | 10人同時通話 | 混信なく会話できる |

---

## 17. 料金・制限

### Photon Voice 無料枠

| 項目 | 無料枠 |
|------|--------|
| CCU (同時接続) | 20 |
| 帯域/月 | 3GB |

### 有料プラン

| プラン | CCU | 月額 |
|--------|-----|------|
| 100 CCU | 100 | $95 |
| 500 CCU | 500 | $200 |

**注意:** Voice の CCU 制限は Fusion より厳しいため、ボトルネックになりやすい

---

## 18. 関連ファイル

| ファイル | 役割 |
|----------|------|
| `VoiceChatManager.cs` | 音声通話管理（新規作成） |
| `ProximityVoiceController.cs` | 近接音声制御（新規作成） |
| `MuteButtonUI.cs` | ミュートボタン（新規作成） |
| `MicrophonePermission.cs` | マイク権限（新規作成） |

---

## 19. 参考リンク

- [Photon Voice 2 公式ドキュメント](https://doc.photonengine.com/voice/)
- [Photon Voice + Fusion 統合ガイド](https://doc.photonengine.com/voice/current/fusion-integration/)
- [Proximity Voice サンプル](https://doc.photonengine.com/voice/current/samples/proximity-voice)
