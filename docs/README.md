# TapHouse ドキュメント

Unity製バーチャルペットシミュレーション（タップハウス）の技術ドキュメント集。

---

## 開発スケジュール

**[development-schedule.md](./development-schedule.md)** - 4週間の開発ロードマップ

| Week | フェーズ | 内容 |
|------|---------|------|
| 1 | Phase 1 | ログイン機能（メール認証、Googleサインイン） |
| 2 | Phase 2 | たっぷポケット構築（コール、着せ替え、歩数計） |
| 3 | Phase 3 | メタバース内散歩（Photon Fusion/Voice） |
| 4 | Phase 4 | 課金プラットフォーム（Unity IAP） |

---

## 目次

1. [ログインシステム](#1-ログインシステム)
2. [音声コマンドシステム](#2-音声コマンドシステム)
3. [リマインダー通知システム](#3-リマインダー通知システム)
4. [Firebase連携システム](#4-firebase連携システム)
5. [アニメーション・状態マシン](#5-アニメーション状態マシン)
6. [ゲームプレイ機能](#6-ゲームプレイ機能)
7. [状態管理システム](#7-状態管理システム)
8. [顔認識システム](#8-顔認識システム)
9. [タッチ・インタラクション](#9-タッチインタラクション)
10. [UIシステム](#10-uiシステム)
11. [ローカライゼーション](#11-ローカライゼーション)
12. [オーディオシステム](#12-オーディオシステム)
13. [犬の体型管理](#13-犬の体型管理)
14. [ユーティリティ](#14-ユーティリティ)
15. [メタバース散歩機能](#15-メタバース散歩機能)
16. [たっぷポケット（サブアプリ）](#16-たっぷポケットサブアプリ)

---

## 1. ログインシステム

ログイン・新規会員登録・ユーザー種別判別に関する機能。

### 詳細ドキュメント
- [README.md](./login/README.md) - ログインシステム概要
- [login-screen-design.md](./login/login-screen-design.md) - ログイン画面UI設計
- [registration-flow.md](./login/registration-flow.md) - 新規会員登録フロー
- [user-data-schema.md](./login/user-data-schema.md) - Firestoreユーザーデータ構造
- [pocket-browser-login.md](./login/pocket-browser-login.md) - たっぷポケット用ブラウザログイン

### 主な機能
- **新規会員登録**: Firebase Authでメールアドレス登録、メール認証送信
- **ログイン**: メール/パスワード認証、自動ログイン対応
- **アプリモード選択**: ハウス/ポケットをチェックボタンで選択
- **オプション登録**: 寄付者登録、たっぷハウス端末申込

### 画面構成
| 要素 | 説明 |
|------|------|
| 新規会員登録ボタン | 最上部、目立つ配置 |
| ログインフォーム | メール/パスワード入力 |
| アプリモード選択 | ラジオボタン（ハウス/ポケット） |
| 寄付者チェック | オプション |
| ハウス申込チェック | オプション |

### 関連スクリプト
```
Assets/Scripts/UI/Login/
├── LoginFormUI.cs           # ログインUI（既存・改修予定）
├── RegistrationUI.cs        # 新規会員登録UI（新規予定）
├── AppModeSelector.cs       # アプリモード選択（新規予定）
└── LoginPasswordToggle.cs   # パスワード表示切替（既存）
```

---

## 2. 音声コマンドシステム

音声認識により犬へのコマンドを実行するシステム。

### 詳細ドキュメント
- [voice-command-system.md](./voice-command/voice-command-system.md) - システム全体の仕様書
- [voice-command-setup-guide.md](./voice-command/voice-command-setup-guide.md) - Unity Editorセットアップ手順
- [audio-processing-noise-analysis.md](./voice-command/audio-processing-noise-analysis.md) - 音声処理パイプライン分析

### 主な機能
- **認識エンジン**: OpenAI Whisper API（オンライン）/ ローカルWhisperモデル（オフライン）
- **30種類のコマンド**: 日本語/英語対応
- **Wake Word検出**: 特定キーワードで音声入力開始
- **顔認識連携**: 顔検出時に自動で音声入力モード開始

### コマンドカテゴリ
| カテゴリ | コマンド例 | 数 |
|---------|-----------|-----|
| BasicCommands | おすわり、お手、おかわり、ふせ、立て、待て、よし | 7 |
| MovementCommands | おいで、回れ、ジャンプ | 3 |
| TrickCommands | バーン、ちんちん、ハイタッチ | 3 |
| CommunicationCommands | 吠えろ、静かに | 2 |
| PraiseCommands | よくできた、すごい、かわいい等 | 15 |

### 関連スクリプト
```
Assets/Scripts/VoiceCommandSystem/
├── VoiceCommandManager.cs      # オーケストレーター
├── VoiceCommandConfig.cs       # 設定（ScriptableObject）
├── AudioRecorder.cs            # マイク録音、VAD処理
├── Recognizers/                # 認識エンジン
└── Commands/DogCommands/       # コマンド実装
```

---

## 3. リマインダー通知システム

高齢者向けの服薬・食事・水分補給などのリマインダー通知機能。

### 詳細ドキュメント
- [reminder-system.md](./reminder/reminder-system.md) - システム仕様書
- [reminder-caregiver-app-spec.md](./reminder/reminder-caregiver-app-spec.md) - 見守りアプリ側仕様
- [reminder-notification-ui-implementation.md](./reminder/reminder-notification-ui-implementation.md) - UI実装ガイド
- [design-spec-reminder-notification-ui.md](./reminder/design-spec-reminder-notification-ui.md) - UIデザイン仕様

### 通知タイプ
| Type | 日本語 | メッセージ例 |
|------|--------|-------------|
| medication | 服薬 | ○○さん、お薬を服用してください |
| meal | 食事 | ○○さん、お食事の時間です |
| hydration | 水分補給 | ○○さん、お水を飲んでください |
| exercise | 運動 | ○○さん、体操の時間です |
| rest | 休憩 | ○○さん、休憩してください |
| appointment | 予定 | ○○さん、予定があります |

### 関連スクリプト
```
Assets/Scripts/Reminder/
├── ReminderType.cs              # 通知種類Enum
├── ReminderData.cs              # データモデル
├── ReminderManager.cs           # メイン制御
└── ReminderNotificationUI.cs    # 通知UI
```

---

## 4. Firebase連携システム

Firebase Realtime Database、Firestore、Firebase Authenticationを使用したクラウド同期機能。

### 詳細ドキュメント
- [firebase-integration.md](./firebase/firebase-integration.md) - 連携仕様書
- [login-system.md](./firebase/login-system.md) - 認証システム仕様
- [multi-device-dog-transfer.md](./firebase/multi-device-dog-transfer.md) - マルチデバイス機能

### 主な機能
- **認証**: メール/パスワードログイン、自動ログイン
- **状態同期**: Realtime Databaseでペット状態をリアルタイム同期
- **リモートアクション**: 他デバイスから犬のアクションをトリガー
- **マルチデバイス**: メイン機/サブ機間での犬の呼び出し・帰還（30分タイムアウト）

### データベース構造
```
users/{userId}/
├── state                 # ペット状態
├── feedLog/              # 餌やり記録
├── playLog/              # 遊びパラメータ
├── skillLogs/            # アクションログ
├── logs/                 # アクティビティログ
└── dogLocation/          # マルチデバイス位置情報
    ├── currentDeviceId
    ├── isMainDevice
    └── transferRequest/
```

### 関連スクリプト
```
Assets/Scripts/
├── FirebaseManager.cs          # Firebase連携
├── UI/Login/LoginFormUI.cs     # ログインUI
├── Utilities/SecurePlayerPrefs.cs  # 暗号化ストレージ
└── MultiDevice/                # マルチデバイス機能
```

---

## 5. アニメーション・状態マシン

UnityのAnimator StateMachineBehaviourを拡張した動的アニメーション制御システム。

### 詳細ドキュメント
- [state-machine-behaviour.md](./animation/state-machine-behaviour.md) - StateMachineBehaviour仕様
- [IdleWalkAnimation_Analysis.md](./animation/IdleWalkAnimation_Analysis.md) - IdleWalkAnimation分析

### EmotionState（感情状態）
```csharp
[Flags]
public enum EmotionState
{
    None = 0,
    Hungry = 1 << 0,    // 空腹
    Sleeping = 1 << 1,  // 睡眠中
    Crying = 1 << 2,    // 泣いている
    Happy = 1 << 3,     // 幸せ
    Angry = 1 << 4,     // 怒り
}
```

### 主な機能
- **感情に応じたアニメーション選択**: EmotionStateでフィルタリング
- **ScriptableObjectベースの設定**: アニメーショングループをデータドリブンで管理
- **サウンド同期**: DSP時間スケジューリングで完璧なオーディオ同期
- **ランダム歩行**: NavMesh上でランダムな目標地点へ移動

### 関連スクリプト
```
Assets/Scripts/StateMachineBehaviour/
├── DogBehaviourBase.cs         # 基底クラス
├── EmotionState.cs             # 感情状態Enum
├── IdleWalkAnimation.cs        # ランダム歩行制御
├── IdleWalkAnimationNavMesh.cs # NavMesh対応歩行
├── SittingBehaviour.cs         # 座りアニメーション
├── LyingBehaviour.cs           # 伏せアニメーション
└── BoredBehaviour.cs           # 退屈アニメーション
```

---

## 6. ゲームプレイ機能

犬との遊びや睡眠サイクルなど、ゲームプレイに関連する機能。

### 詳細ドキュメント
- [play-toy-system.md](./gameplay/play-toy-system.md) - ボール遊び機能仕様
- [sleep-system.md](./gameplay/sleep-system.md) - スリープシステム仕様

### ボール遊び機能
| アクション | 確率 | 説明 |
|------------|------|------|
| 噛みつき→投げ | 50% | 5秒間噛みついてから投げる |
| 直接投げ | 50% | 振って直接投げる |

**フリーズ防止策:**
- `isCoroutineRunning`フラグで再入防止
- 30秒タイムアウトウォッチャー
- ToyFetcherBaseは180秒タイムアウト

### スリープシステム
| 種類 | 時間帯 | 説明 |
|------|--------|------|
| 夜間睡眠 | 22:00～6:00 | 就寝・起床時刻は設定可能 |
| 昼寝 | 13:00～15:00 | 固定時間帯 |

**注意:** 昼寝と夜睡眠の時間帯は重複しないよう設計

### 関連スクリプト
```
Assets/Scripts/
├── PlayToy/
│   ├── PlayManager.cs          # おもちゃスポーン・投げ処理
│   ├── PlayToy.cs              # 遊びシーケンス制御
│   ├── ToyFetcherBase.cs       # フェッチ処理基底
│   ├── GetBall.cs              # ボールフェッチ
│   └── GetToy.cs               # おもちゃフェッチ
├── Sleep/
│   ├── SleepController.cs      # メイン制御
│   ├── SleepController.Scheduling.cs
│   ├── SleepController.Check.cs
│   └── SleepController.Nap.cs
└── SnackManager.cs             # おやつ機能
```

---

## 7. 状態管理システム

アプリ全体で共有される状態の管理。

### 詳細ドキュメント
- [global-state-management.md](./state-management/global-state-management.md) - 状態管理仕様

### 3つのメトリクス
| メトリクス | 範囲 | 管理クラス | 永続化 |
|-----------|------|-----------|--------|
| 愛情度 (Love) | 0-100 | `LoveManager.cs` | PlayerPrefs |
| 要求度 (Demand) | 0-100 | `DemandManager.cs` | なし |
| 空腹度 (Hunger) | 4段階 | `HungerManager.cs` | PlayerPrefs |

### PetState（ペット状態）
```csharp
public enum PetState
{
    idle,       // 通常状態
    feeding,    // 食事中
    sleeping,   // 夜間睡眠中
    ball,       // ボール遊び中
    snack,      // おやつ中
    napping,    // 昼寝中
    ready,      // UI操作待ち
    moving,     // 移動中
    toy,        // おもちゃ遊び中
    action,     // アクション実行中
}
```

### HungerState（空腹状態）
| 状態 | 経過時間 |
|------|----------|
| Full | 0～3時間 |
| MediumHigh | 3～6時間 |
| MediumLow | 6～9時間 |
| Hungry | 9時間以上 |

### 関連スクリプト
```
Assets/Scripts/
├── Dog/
│   ├── DogStateController.cs   # 中央オーケストレーター
│   ├── LoveManager.cs          # 愛情度管理
│   ├── DemandManager.cs        # 要求度管理
│   └── IDemandAdjuster.cs      # 要求調整インターフェース
├── HungerManager.cs            # 空腹度管理
├── Variables/GlobalVariables.cs # グローバル状態
└── Constants/PrefsKeys.cs      # PlayerPrefsキー定義
```

---

## 8. 顔認識システム

カメラでユーザーの顔を検出し、犬がユーザーに注目するアクションをトリガー。

### 詳細ドキュメント
- [face-detection-system.md](./detection/face-detection-system.md) - 顔認識仕様

### 処理フロー
```
カメラ映像取得
    ↓
グレースケール変換 + ダウンサンプリング
    ↓
動体検出チェック（FacePresenceDetector）
    ↓
CascadeClassifier.detectMultiScale()
    ↓
顔検出時 → DogController.LookAtUser()
```

### 関連スクリプト
```
Assets/Scripts/FaceRecognition/
├── FaceDetection.cs            # 顔検出メイン処理
└── FacePresenceDetector.cs     # 動体検出による最適化
```

---

## 9. タッチ・インタラクション

画面タッチによる犬との対話機能。TouchController.csの機能をタッチシステム、移動・アクション制御をインタラクションシステムとして分離。

### 詳細ドキュメント
- [touch-system.md](./touch/touch-system.md) - タッチ入力、なでなで、長押し機能（TouchController.cs）
- [interaction-system.md](./touch/interaction-system.md) - 移動・回転制御、DogControllerアクション

### タッチシステム（TouchController.cs）
- **入力検出**: タッチ/マウス入力、無効化条件チェック、Raycast判定
- **なでる**: 犬をタッチして撫でる（愛情度+1、タッチ終了時に吠える）
- **長押し（3秒）**: 犬が仰向けになる（LieOnBack状態）
- **アイコン表示**: タッチ位置にアイコン表示

### インタラクションシステム（TurnAndMoveHandler.cs / DogController.cs）
- **移動・回転制御**: TurnAndMoveHandlerによる状態マシン
- **アクション実行**: お手、おかわり、ダンス、バーン等

### タッチ無効化条件
| 条件 | 説明 |
|------|------|
| 犬不在（マルチデバイス） | `DogLocationSync.HasDog == false` |
| 昼寝中 | 眠い反応のみトリガー |
| リマインダー表示中 | 完全無効 |
| 睡眠中/食事中/おやつ中 | 完全無効 |

### 関連スクリプト
```
Assets/Scripts/
├── TouchController.cs          # タッチ入力処理、なでなで機能
├── TurnAndMoveHandler.cs       # 移動・回転制御（状態マシン）
└── DogController.cs            # Animatorパラメータ制御、アクション実行
```

---

## 10. UIシステム

メインUI、ポップアップ、設定画面など。

### メインUI
- **満腹ゲージ**: 空腹状態の視覚的表示
- **ご飯/遊び/おやつボタン**: アイテム選択ポップアップを開く
- **タブレットモード**: 高齢者向けの大きなボタン表示

### 設定画面
- **ユーザー名設定**: 表示名の変更
- **言語設定**: 日本語/英語切り替え
- **睡眠時間設定**: 就寝・起床時刻の変更
- **サブ機設定**: マルチデバイスのサブ機トグル
- **デバッグ画面**: 開発者向け状態表示

### ScriptableObjectイベント
```csharp
[Event ScriptableObject]
└── Raise(data) → 登録されたすべてのリスナーに通知

[EventListener Component]
└── OnEventRaised(data) → コールバック実行
```

### 関連スクリプト
```
Assets/Scripts/UI/
├── Main/
│   ├── MainUI.cs               # メインUI制御
│   ├── MainUIButtons.cs        # ボタン制御
│   ├── MainUIFullness.cs       # 満腹ゲージ
│   └── Popup/                  # 選択ポップアップ
├── Setting/
│   ├── DebugCanvasManager.cs   # デバッグ画面
│   └── DebugButton.cs          # デバッグボタン
├── Login/
│   └── LoginFormUI.cs          # ログインUI
└── Shape/                      # 体型調整UI

Assets/Scripts/ScriptableObject/Event/  # イベント定義
```

---

## 11. ローカライゼーション

日本語/英語のUI多言語対応。

### 主な機能
- **言語切り替え**: 設定画面から変更可能
- **LocalizedText**: Textコンポーネントにアタッチしてキーベースで翻訳
- **動的テキスト**: リマインダーメッセージ等の動的生成

### 関連スクリプト
```
Assets/Scripts/UI/
├── LocalizationManager.cs      # ローカライゼーション管理
├── LocalizedText.cs            # 翻訳テキストコンポーネント
└── LanguageSettingsUI.cs       # 言語設定UI
```

---

## 12. オーディオシステム

BGM、SE、音声の管理。

### 主な機能
- **DSP時間スケジューリング**: アニメーションとの完璧な同期
- **音量設定**: BGM/SE個別の音量調整
- **吠え声**: MaskLayerによるレイヤー別サウンド

### 関連スクリプト
```
Assets/Scripts/
├── Audio/GameAudioSettings.cs  # 音量設定（シングルトン）
├── AudioController.cs          # オーディオ制御
├── AudioAnimationPlayer.cs     # アニメーション同期再生
├── LayerBarkSound.cs           # 吠え声制御
└── MaskLayerSoundManager.cs    # レイヤーサウンド管理
```

---

## 13. 犬の体型管理

犬の体型（太さ）を管理するシステム。

### 主な機能
- **体型変化**: 餌やりで体型が変化
- **BlendShape制御**: 3Dモデルのブレンドシェイプで体型を表現
- **体型リセット**: 運動で体型をリセット

### 関連スクリプト
```
Assets/Scripts/Dog/
├── DogBodyShapeManager.cs      # 体型管理
└── Common/                     # 共通定義

Assets/Scripts/UI/Shape/        # 体型調整UI
```

---

## 14. ユーティリティ

共通で使用されるユーティリティクラス。

### タイムゾーン処理
- **TimeZoneProvider**: タイムゾーンの一元管理
- **TimeZoneUtility**: UTC⇔ローカル変換ヘルパー

### セキュリティ
- **SecurePlayerPrefs**: パスワード等の暗号化ストレージ

### ログ
- **Logging/**: デバッグログ出力ユーティリティ

### 関連スクリプト
```
Assets/Scripts/Utilities/
├── TimeZoneProvider.cs         # タイムゾーン管理
├── TimeZoneUtility.cs          # 時間変換ユーティリティ
├── SecurePlayerPrefs.cs        # 暗号化ストレージ
└── Logging/                    # ログユーティリティ
```

---

## 15. メタバース散歩機能

他のユーザーと一緒にバーチャル空間で犬を連れて散歩できるマルチプレイヤー機能。

### 詳細ドキュメント
- [metaverse_walk_overview.md](./metaverse_walk/metaverse_walk_overview.md) - 機能概要・フェーズ分け
- [metaverse_walk_trigger.md](./metaverse_walk/metaverse_walk_trigger.md) - 散歩トリガーシステム
- [metaverse_walk_scene.md](./metaverse_walk/metaverse_walk_scene.md) - メタバースシーン設計
- [metaverse_walk_network.md](./metaverse_walk/metaverse_walk_network.md) - マルチプレイヤー同期（Photon Fusion 2）
- [metaverse_walk_voice.md](./metaverse_walk/metaverse_walk_voice.md) - 音声通話（Photon Voice 2）

### 開発フェーズ

| フェーズ | 内容 | 技術 |
|---------|------|------|
| **フェーズ1 (MVP)** | シングルプレイヤー散歩 | Unity NavMesh |
| **フェーズ2** | マルチプレイヤー | Photon Fusion 2 |
| **フェーズ3** | 音声通話 | Photon Voice 2 |

### 主な機能
- **散歩トリガー**: 毎日10時に犬が「散歩に行きたい」とアピール
- **メタバース空間**: 公園風の3Dマップをアイソメトリック視点で歩く
- **犬の追従**: NavMeshAgentによる自然な追従
- **マルチプレイヤー**: 最大10人が同じ空間で散歩（フェーズ2）
- **近接音声通話**: 近づくと声が聞こえる（フェーズ3）

### 関連スクリプト
```
Assets/Scripts/
├── Metaverse/
│   ├── WalkScheduler.cs              # 散歩スケジュール管理
│   ├── WalkTriggerUI.cs              # トリガーUI
│   ├── MetaverseManager.cs           # シーン管理
│   ├── MetaversePlayerController.cs  # プレイヤー移動
│   ├── MetaverseDogFollower.cs       # 犬の追従
│   └── MetaverseCamera.cs            # カメラ制御
└── Network/                           # フェーズ2以降
    ├── PhotonNetworkManager.cs
    ├── NetworkPlayerController.cs
    └── VoiceChatManager.cs
```

---

## 16. たっぷポケット（サブアプリ）

高齢者向けのサブ端末アプリ。たっぷハウスと連携し、外出先でも犬と一緒に過ごせる。

### 詳細ドキュメント
- [pocket/README.md](./pocket/README.md) - ポケット機能の目次・概要
- [pocket/pocket-overview.md](./pocket/pocket-overview.md) - 全体コンセプト
- [pocket/build-configuration.md](./pocket/build-configuration.md) - ビルド分け方針
- [pocket/ui-architecture.md](./pocket/ui-architecture.md) - UIアーキテクチャ

### 主な機能

| 機能 | 説明 | ドキュメント |
|------|------|-------------|
| コール | メイン機から犬を呼ぶ | [call-dog.md](./pocket/features/call-dog.md) |
| 着せ替え | 犬に服・帽子を着せる | [dress-up.md](./pocket/features/dress-up.md) |
| 歩数計 | 犬とユーザーの歩数表示 | [pedometer.md](./pocket/features/pedometer.md) |
| 脳トレ | 神経衰弱ゲーム | [brain-training.md](./pocket/features/brain-training.md) |

### メタバース機能

| 機能 | 説明 | ドキュメント |
|------|------|-------------|
| 内蔵メタバース | ポケット版散歩機能 | [pocket-metaverse-overview.md](./pocket/metaverse/pocket-metaverse-overview.md) |
| 加速度歩行 | スマホを振って歩く | [accelerometer-walk.md](./pocket/metaverse/accelerometer-walk.md) |
| 通話（音量ブースト） | 高齢者向け音量増幅 | [pocket-voice-call.md](./pocket/metaverse/pocket-voice-call.md) |
| チャット | テキストチャット（後回し） | [chat-page.md](./pocket/metaverse/chat-page.md) |

### ビルド設定

| 項目 | 値 |
|------|-----|
| Define Symbol | `TAPHOUSE_POCKET` |
| 対応プラットフォーム | Android / iOS |
| ビルド設定 | [platform/build-settings.md](./pocket/platform/build-settings.md) |

### 関連スクリプト（予定）
```
Assets/Scripts/Pocket/
├── PocketManager.cs
├── UI/
├── CallDog/
├── DressUp/
├── Pedometer/
├── BrainTraining/
└── Metaverse/
```

---

## ドキュメントファイル一覧

### login/
| ファイル | 内容 |
|----------|------|
| README.md | ログインシステム概要 |
| login-screen-design.md | ログイン画面UI設計・画面遷移 |
| registration-flow.md | 新規会員登録フロー・メール認証 |
| user-data-schema.md | Firestoreユーザーデータ構造 |
| pocket-browser-login.md | たっぷポケット用ブラウザログイン |

### voice-command/
| ファイル | 内容 |
|----------|------|
| voice-command-system.md | システム全体の仕様書 |
| voice-command-setup-guide.md | セットアップ手順 |
| audio-processing-noise-analysis.md | 音声処理分析 |

### reminder/
| ファイル | 内容 |
|----------|------|
| reminder-system.md | システム仕様書 |
| reminder-caregiver-app-spec.md | 見守りアプリ仕様 |
| reminder-notification-ui-implementation.md | UI実装ガイド |
| design-spec-reminder-notification-ui.md | UIデザイン仕様 |

### firebase/
| ファイル | 内容 |
|----------|------|
| firebase-integration.md | Firebase連携仕様 |
| login-system.md | 認証システム仕様 |
| multi-device-dog-transfer.md | マルチデバイス機能 |

### animation/
| ファイル | 内容 |
|----------|------|
| state-machine-behaviour.md | StateMachineBehaviour仕様 |
| IdleWalkAnimation_Analysis.md | IdleWalkAnimation分析 |

### gameplay/
| ファイル | 内容 |
|----------|------|
| play-toy-system.md | ボール遊び機能仕様 |
| sleep-system.md | スリープシステム仕様 |

### state-management/
| ファイル | 内容 |
|----------|------|
| global-state-management.md | 状態管理仕様 |

### detection/
| ファイル | 内容 |
|----------|------|
| face-detection-system.md | 顔認識システム仕様 |

### touch/
| ファイル | 内容 |
|----------|------|
| touch-system.md | タッチ入力、なでなで、長押し機能（TouchController.cs） |
| interaction-system.md | 移動・回転制御、DogControllerアクション |

### metaverse_walk/
| ファイル | 内容 |
|----------|------|
| metaverse_walk_overview.md | 機能概要・フェーズ分け |
| metaverse_walk_trigger.md | 散歩トリガーシステム仕様 |
| metaverse_walk_scene.md | メタバースシーン設計 |
| metaverse_walk_network.md | マルチプレイヤー同期仕様 |
| metaverse_walk_voice.md | 音声通話仕様 |

### pocket/
| ファイル | 内容 |
|----------|------|
| README.md | ポケット機能の目次・概要 |
| pocket-overview.md | アプリ全体概要・コンセプト |
| build-configuration.md | ビルド分け実装方針 |
| ui-architecture.md | UIアーキテクチャ・画面設計 |

### pocket/features/
| ファイル | 内容 |
|----------|------|
| call-dog.md | コール機能（犬呼び込み） |
| dress-up.md | 着せ替え機能 |
| pedometer.md | 歩数計機能 |
| brain-training.md | 脳トレ機能（記憶ゲーム） |

### pocket/metaverse/
| ファイル | 内容 |
|----------|------|
| pocket-metaverse-overview.md | ポケット内蔵メタバース概要 |
| accelerometer-walk.md | 加速度センサー歩行連動 |
| pocket-voice-call.md | 通話機能（音量ブースト） |
| chat-page.md | チャットページ（実装後回し） |

### pocket/platform/
| ファイル | 内容 |
|----------|------|
| build-settings.md | Android/iOSビルド設定 |
