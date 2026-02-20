# マルチデバイス犬転送システム

## 概要

複数デバイス間で犬を移動させる機能。メイン機がデフォルトで犬を表示し、サブ機は「呼ぶ」ボタンで犬を呼び出せる。

## 仕様

| 項目 | 仕様 |
|------|------|
| デフォルト | メイン機に犬がいる |
| 呼び出し | サブ機→Firebase→メイン機（犬が右へ退場）→サブ機（犬が左から登場） |
| コールバック | メイン機からもサブ機の犬を呼び戻せる |
| タイムアウト | 30分経過でメイン機に自動帰還 |
| 再起動時 | メイン機に戻る（メイン機は起動時に常に犬を持つ） |
| 設定画面 | サブ機設定トグルで切り替え |

---

## Firebase Realtime Database 構造

```
users/{userId}/
  └── dogLocation/
        ├── currentDeviceId: string     // 現在犬がいるデバイスID
        ├── isMainDevice: bool          // 現在メイン機にいるか
        ├── transferRequest/            // 転送リクエスト
        │     ├── requestingDeviceId: string
        │     ├── timestamp: number (Unix ms)
        │     └── type: "call" | "return"
        └── lastActivityTimestamp: number // 最終アクティビティ（タイムアウト用）
```

---

## UI仕様

### ボタン表示ロジック（メイン機・サブ機共通）

| 状態 | 表示されるボタン | TopLeftUI（空腹ゲージ） |
|------|-----------------|----------------------|
| 犬がいる | ご飯・遊ぶ・おやつ（3つ） | 表示 |
| 犬がいない | 呼ぶ（1つ） | 非表示 |

**重要**: 3つのボタンと呼ぶボタンは**同時に表示されない**（排他的）

### 呼ぶボタンデザイン
- 既存ボタン（ご飯・遊ぶ・おやつ）と同じデザイン
- アイコン + テキスト「呼ぶ」

---

## ファイル構成

### 新規作成ファイル

| ファイル | 説明 |
|----------|------|
| `Assets/Scripts/MultiDevice/DogLocationSync.cs` | Firebase同期マネージャー（シングルトン） |
| `Assets/Scripts/MultiDevice/DogTransferAnimation.cs` | 犬の入退場アニメーション制御 |

### 修正ファイル

| ファイル | 変更内容 |
|----------|----------|
| `Assets/Scripts/Constants/PrefsKeys.cs` | `IsSubDevice`, `DeviceId` キー追加 |
| `Assets/Scripts/DogController.cs` | `SetVisible(bool)` メソッド追加 |
| `Assets/Scripts/UI/Main/MainUIButtons.cs` | `callDogButton` 追加、`topLeftUI` 表示切り替え |
| `Assets/Scripts/UserNameSettings.cs` | `subDeviceToggle` 追加 |
| `Assets/Scripts/TouchController.cs` | 犬がいない時のタッチ処理スキップ |
| `Assets/Scripts/UI/Setting/DebugCanvasManager.cs` | デバイス役割表示追加 |

---

## クラス詳細

### DogLocationSync

Firebase Realtime Databaseを使用してデバイス間の犬の位置を同期するシングルトンクラス。

```csharp
namespace TapHouse.MultiDevice
{
    public enum DeviceRole { Main, Sub }

    public class DogLocationSync : MonoBehaviour
    {
        // シングルトン
        public static DogLocationSync Instance { get; }

        // プロパティ
        public DeviceRole CurrentRole { get; }  // 現在のデバイス役割
        public bool HasDog { get; }             // このデバイスに犬がいるか
        public string DeviceId { get; }         // デバイス固有ID

        // イベント
        public event Action<bool> OnDogPresenceChanged;  // 犬の有無が変わった時
        public event Action<bool> OnTransferStarted;     // 転送開始時

        // メソッド
        public void SetDeviceRole(DeviceRole role);  // 設定画面から呼ぶ
        public UniTask RequestCallDog();             // 犬を呼ぶリクエスト
        public UniTask ReturnDogToMain();            // 犬をメイン機に返す
        public void RecordActivity();                // アクティビティ記録（タイムアウトリセット）
    }
}
```

### DogTransferAnimation

犬のデバイス間転送時のアニメーションを制御するクラス。

```csharp
namespace TapHouse.MultiDevice
{
    public class DogTransferAnimation : MonoBehaviour
    {
        // 右側へ退場
        public UniTask ExitToRight();

        // 左側から登場
        public UniTask EnterFromLeft();

        // アニメーション中かどうか
        public bool IsAnimating { get; }
    }
}
```

---

## 処理フロー

### サブ機が犬を呼ぶ場合

```
[サブ機]                    [Firebase RTDB]              [メイン機]
    |                           |                            |
    | 1. 「呼ぶ」ボタン押下       |                            |
    | 2. transferRequest書込み   |                            |
    |-------------------------->|                            |
    |                           |--------------------------->|
    |                           | 3. ValueChangedで検知       |
    |                           |                            | 4. 犬を右へ退場
    |                           |<---------------------------|
    |                           | 5. currentDeviceId更新      |
    |<--------------------------|                            |
    | 6. ValueChangedで検知      |                            |
    | 7. 犬を左から登場          |                            |
```

### メイン機が再起動した場合

```
[サブ機]                    [Firebase RTDB]              [メイン機]
    |                           |                            |
    | 犬を持っている             |                            | 再起動
    |                           |<---------------------------|
    |                           | メイン機が起動時に           |
    |                           | dogLocation を上書き         |
    |<--------------------------|                            |
    | Firebase リスナーが検知    |                            |
    | 犬を右へ退場              |                            | 犬を表示
```

### 30分タイムアウト

```
[サブ機で30分経過]
    |
    | タイムアウト検知
    | ReturnDogToMain() 呼び出し
    |
    | 犬を右へ退場
    |
    | Firebase更新（isMainDevice: true）
    |
[メイン機がリスナーで検知]
    |
    | 犬を左から登場
```

---

## PlayerPrefs キー

| キー | 型 | 説明 |
|------|---|------|
| `IsSubDevice` | int | 0=メイン機、1=サブ機 |
| `DeviceId` | string | デバイス固有識別子 |

---

## Unity Editor 設定手順

### 1. DogLocationSync をシーンに追加

1. **Hierarchy** で右クリック → **Create Empty** → 名前を `DogLocationSync` に変更
2. **Add Component** → `DogLocationSync` を追加
3. フィールド設定：

| フィールド | 設定値 |
|-----------|--------|
| Dog Controller | シーン内の犬をドラッグ |
| Transfer Animation | 犬のDogTransferAnimationコンポーネント |
| Timeout Minutes | 30 |
| Enable Debug Log | ✓ |

### 2. DogTransferAnimation を犬に追加

1. 犬のGameObjectを選択
2. **Add Component** → `DogTransferAnimation` を追加
3. フィールド設定：

| フィールド | 設定値 |
|-----------|--------|
| Dog Transform | 自動設定 |
| Animator | 犬のAnimator |
| Dog Controller | 犬のDogController |
| Exit Speed | 3 |
| Enter Speed | 2.5 |
| Exit Position | (15, 0, 0) |
| Enter Start Position | (-15, 0, 0) |

### 3. 呼ぶボタンを追加

1. 既存のボタン（ご飯等）を複製
2. 名前を `CallDogButton` に変更
3. テキストを「呼ぶ」に変更
4. `MainUIButtons` の `Call Dog Button` フィールドに設定

### 4. サブ機トグルを追加

1. 設定パネル内に新しいトグル追加
2. ラベルを「サブ機」に変更
3. `FirebaseUIManager` の `Sub Device Toggle` フィールドに設定

---

## デバッグ方法

### ログ確認

`DogLocationSync` と `DogTransferAnimation` は `enableDebugLog = true` でログを出力。

```
[DogLocationSync] Device role changed to: Sub
[DogLocationSync] Sending call dog request...
[DogLocationSync] Dog exiting...
[DogTransferAnimation] Starting exit animation...
```

### Firebase Console

`users/{userId}/dogLocation` でリアルタイムにデータを確認可能。

### エディタでのテスト

1. Play開始
2. `Shift + 左クリック` で設定画面を開く
3. サブ機トグルをONにして保存
4. 「呼ぶ」ボタンが表示されることを確認

### タイムアウトのテスト

**方法1: Inspector設定**
1. `DogLocationSync` の Inspector を開く
2. `Debug Timeout Seconds` に短い値（例: 10秒）を設定
3. サブ機で犬を呼び、10秒後に自動で戻ることを確認

**方法2: ContextMenu**
1. サブ機で犬がいる状態で `DogLocationSync` を右クリック
2. `Force Timeout (Return Dog to Main)` を選択
3. 即座にメイン機に戻る

### DebugCanvasでの確認

DebugCanvasに以下の情報が表示される:
```
Device: MAIN (or SUB)
犬あり (or 犬なし)
Timeout: 29:45 (サブ機で犬がいる場合のみ)
```

---

## トラブルシューティング

### DogLocationSync.Instance が null

- シーンに `DogLocationSync` GameObjectがあるか確認
- Awake/Start の順序を確認

### 呼ぶボタンが表示されない

- `MainUIButtons.callDogButton` が設定されているか確認
- タブレットモードがONか確認
- `DogLocationSync.HasDog` がfalseか確認

### Firebase 同期されない

- ログイン状態を確認
- Firebase Realtime Database のルールを確認
- ネットワーク接続を確認

### タイムアウトが動作しない

- `DogLocationSync.timeoutMinutes` の値を確認
- サブ機で犬を持っている状態か確認

---

## 変更履歴

| 日付 | 変更内容 |
|------|----------|
| 2026-01-18 | 初版作成 |
| 2026-01-18 | デバッグ機能追加（タイムアウトテスト、DebugCanvas表示） |
| 2026-01-18 | TouchControllerに犬なし時のタッチ無効化を追加 |
| 2026-01-18 | TopLeftUI（空腹ゲージ）の表示切り替えを追加 |
