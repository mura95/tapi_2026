# 顔認識システム 仕様書

## 概要

カメラでユーザーの顔を検出し、犬がユーザーに注目するアクションをトリガーするシステム。OpenCV for Unityを使用したカスケード分類器ベースの顔検出を実装しています。

## ファイル構成

| ファイル | 役割 |
|----------|------|
| `FaceDetection.cs` | 顔検出（動体検出による最適化付き） |
| `DogController.cs` | 顔検出時のアクション実行 |
| `MaskLayerManager.cs` | 顔認識時のAnimatorレイヤー制御 |
| `GlobalVariables.cs` | AttentionCountによる飽き管理 |

## システムフロー

### 全体フロー

```
カメラ映像取得
    ↓
WebCamTextureToMatHelper（OpenCV Mat変換）
    ↓
グレースケール変換 + ダウンサンプリング
    ↓
[FacePresenceDetector] 動体検出チェック
    ↓
CascadeClassifier.detectMultiScale()（顔検出）
    ↓
顔検出時: DogController.ActionBool(true)
    ↓
犬が注目アクション実行
    ↓
GlobalVariables.AttentionCount++
```

## FacePresenceDetector

### 機能

- **動体検出による最適化**: 画面に動きがないときは顔検出をスキップ
- **UnityEventによる通知**: `onFacePresenceChanged`イベントで顔の有無を通知
- **Firebaseログ**: 顔検出イベントをFirebaseに記録

### 設定パラメータ

| パラメータ | デフォルト値 | 説明 |
|------------|--------------|------|
| `detectWidth` | 240 | 検出用ダウンサンプル幅 |
| `detectIntervalMs` | 2000 | 検出間隔（ミリ秒） |
| `motionPixelThreshold` | 500 | 動き検出しきい値（画素数） |
| `minFaceRatio` | 0.2 | 最小顔サイズ（縮小画像の高さ比） |

### 動体検出ロジック

```csharp
// 前フレームとの差分計算
Core.absdiff(graySmall, prevGraySmall, diffSmall);
Imgproc.threshold(diffSmall, diffSmall, 8, 255, Imgproc.THRESH_BINARY);
int changed = Core.countNonZero(diffSmall);

// 動きがあるときのみ顔検出を実行
if (changed >= motionPixelThreshold)
    performDetect = true;
```

## アクション実行

### DogController.ActionBool()

顔検出時に呼ばれるメソッド。犬を注目アクションに遷移させます。

```csharp
public void ActionBool(bool value = false)
{
    // 制御条件チェック
    if (GetTransitionNo() == 3 ||                    // 特定状態では無視
        GlobalVariables.CurrentHungerState == HungerState.Hungry ||  // 空腹時は無視
        GetSleepBool())                              // 睡眠中は無視
        return;

    // 犬が離れている場合は移動してから実行
    if (Vector3.Distance(transform.position, Vector3.zero) > 0.5f && value)
    {
        StartCoroutine(WaitForMoveToComplete(value));
    }
    else
    {
        // faceレイヤーのウェイト設定（15秒後に自動リセット）
        _maskLayerManager.SetLayerWeight("face", value ? 1 : 0, 15f);
        // Animatorのアクションフラグ設定
        animator.SetBool("ActionBool", value);
    }
}
```

### MaskLayerManager

Animatorの`face`レイヤーを制御し、顔認識時の特別なアニメーション表現を実現します。

```csharp
public void SetLayerWeight(string layerName, float weight, float duration = 0f)
{
    int layerIndex = animator.GetLayerIndex(layerName);
    animator.SetLayerWeight(layerIndex, weight);

    // 指定時間後に自動リセット
    if (duration > 0f)
        StartCoroutine(ResetLayerWeightAfterTime(layerIndex, duration));
}
```

## 飽き管理システム（AttentionCount）

### 概要

犬が同じ刺激に対して「飽きる」ことをシミュレートするシステム。顔認識によるアクションは`AttentionCount`がリセットされるまで最大5回まで実行されます。

### リセット条件

| トリガー | ファイル | 動作 |
|----------|----------|------|
| タッチ操作 | `TouchController.cs` | `AttentionCount = 0` |
| 食事完了 | `EatAnimationController.cs` | `AttentionCount = 0` |
| 食事開始 | `EatAnimationController.cs` | `AttentionCount = 10`（検出停止） |

### 処理条件チェック

```csharp
// FacePresenceDetector
private bool ShouldProcessFrame()
{
    return GlobalVariables.AttentionCount < attentionMax  // 飽きていない
        && _dogController != null
        && _dogController.GetTransitionNo() != 3         // 特定状態以外
        && _dogController.GetTransitionNo() != 4
        && GlobalVariables.CurrentState == PetState.idle // アイドル状態
        && !_dogController.GetIsAction()                  // アクション中でない
        && _dogController.GetSnackType() == 0;           // おやつ中でない
}
```

## パフォーマンス最適化

### CPU負荷軽減策

1. **ダウンサンプリング**: 640x480 → 約213x160（1/3倍）で検出処理
2. **フレームスキップ**: 5フレームに1回のみ検出実行
3. **動体検出ゲート**: 画面に動きがないときは顔検出をスキップ
4. **低FPS設定**: カメラは10FPSで動作

### メモリ管理

```csharp
// Matオブジェクトは再利用（毎フレームnewしない）
private Mat grayMat;
private Mat graySmall;
private MatOfRect faces = new MatOfRect();

// 破棄時は適切にDispose
void OnDestroy()
{
    grayMat?.Dispose();
    graySmall?.Dispose();
    faces?.Dispose();
    faceCascade?.Dispose();
}
```

## 依存関係

### 外部パッケージ

- **OpenCV for Unity**: カスケード分類器による顔検出
- `WebCamTextureToMatHelper`: カメラ映像のMat変換ヘルパー

### カスケードファイル

- **配置場所**: `Assets/StreamingAssets/OpenCVForUnity/`
- **ファイル名**: `lbpcascade_frontalface.xml`
- **種類**: LBP（Local Binary Pattern）ベースの正面顔検出器

## Androidパーミッション

```csharp
#if UNITY_ANDROID
if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
{
    Permission.RequestUserPermission(Permission.Camera);
    return;
}
#endif
```

## シーケンス図

```
[カメラ] ──映像取得──► [WebCamHelper] ──Mat変換──► [FacePresenceDetector]
                                                         │
                                                         ▼
                                                  グレースケール変換
                                                         │
                                                         ▼
                                                   ダウンサンプリング
                                                         │
                                                         ▼
                                              ┌─── 動体検出チェック ───┐
                                              │                        │
                                              ▼                        ▼
                                          動きあり                  動きなし
                                              │                        │
                                              ▼                        ▼
                                       顔検出実行                    スキップ
                                              │
                                              ▼
                                     ┌─── 顔検出結果 ───┐
                                     │                  │
                                     ▼                  ▼
                                  検出あり            検出なし
                                     │
                                     ▼
                        DogController.ActionBool(true)
                                     │
                                     ▼
                         MaskLayerManager.SetLayerWeight("face", 1)
                                     │
                                     ▼
                         animator.SetBool("ActionBool", true)
                                     │
                                     ▼
                              注目アニメーション再生
                                     │
                                     ▼
                          GlobalVariables.AttentionCount++
```

## トラブルシューティング

### 顔検出が動作しない

1. **カスケードファイルの確認**: `StreamingAssets/OpenCVForUnity/`に`lbpcascade_frontalface.xml`があるか
2. **カメラパーミッション**: Android設定でカメラ権限が許可されているか
3. **AttentionCount**: `attentionMax`（5）に達していないか

### 検出精度が低い

1. **照明条件**: 十分な明るさがあるか
2. **minFaceRatio**: 値を下げて小さい顔も検出
3. **minNeighbors**: 値を下げて検出感度を上げる（誤検出増加に注意）

### パフォーマンスが悪い

1. **downscaleFactor**: 値を上げて処理画像を小さく
2. **detectionFrameSkip**: 値を上げて検出頻度を下げる
3. **detectIntervalMs**: 値を上げて検出間隔を長く
