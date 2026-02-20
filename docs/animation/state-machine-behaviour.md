# StateMachineBehaviour システム仕様書

## 概要

UnityのAnimator StateMachineBehaviourを拡張し、アニメーションステートに応じた動的な振る舞い（アニメーション選択、サウンド再生、移動制御）を実装するシステム。

## ファイル構成

| ファイル | 役割 |
|----------|------|
| `DogBehaviourBase.cs` | 基底クラス。アニメーション・サウンド同期処理 |
| `EmotionState.cs` | 感情状態のFlags列挙型 |
| `IdleWalkAnimation.cs` | ランダム歩行の移動・回転制御 |
| `SittingBehaviour.cs` | 座りアニメーション用（DogBehaviourBase継承） |
| `LyingBehaviour.cs` | 伏せアニメーション用（DogBehaviourBase継承） |
| `BoredBehaviour.cs` | 退屈アニメーション用（DogBehaviourBase継承） |

---

## EmotionState（感情状態）

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

**特徴:**
- Flags属性により複数状態の組み合わせが可能
- 現在は `Hungry` フラグのみがアクティブに使用されている
- `GlobalVariables.CurrentHungerState` から動的に判定

---

## DogBehaviourBase（基底クラス）

### 責務

1. **アニメーショングループの管理** - ScriptableObject（DogAnimationGroupBase）から設定を読み込み
2. **ループ/単発アニメーションの識別** - ステートパスでループか単発かを判定
3. **感情に応じたアニメーション選択** - EmotionStateでフィルタリング
4. **サウンドとアニメーションの同期再生** - DSP時間でスケジュール

### アニメーション制御フロー

```
OnStateEnter()
    ↓
ループアニメーション判定
    ├── true（ループ）
    │   ├── 感情状態を取得
    │   ├── 感情に合う単発アニメーションを選択
    │   └── _idleTime カウントダウン開始
    │
    └── false（単発）
        └── 戻り先のループアニメーションを設定

OnStateUpdate()（ループ時のみ）
    ↓
_idleTime カウントダウン
    ↓ (0以下になったら)
PlayAnimationAndSound()
    ├── DSP時間で0.1秒後にサウンド再生をスケジュール
    └── 0.1秒待機後にアニメーション遷移トリガー
```

### 主要プロパティ

| プロパティ | 型 | 説明 |
|-----------|-----|------|
| `dogAnimationGroup` | DogAnimationGroupBase | アニメーション設定ScriptableObject |
| `_isLoopAnimation` | bool | 現在のステートがループか |
| `_idleTime` | float | 次のアニメーション再生までの待機時間 |
| `SyncDelay` | double | サウンド同期用遅延（0.1秒） |

### サウンド同期の仕組み

```csharp
// DSP時間でスケジュールすることで正確な同期を実現
var startTime = AudioSettings.dspTime + SyncDelay;
_audioSource.PlayScheduled(startTime);
await Task.Delay((int)(SyncDelay * 1000));
animator.SetInteger(_transitionAnimationTypeHash, animationType);
```

---

## IdleWalkAnimation（歩行制御）

### 責務

ランダムな目標位置を選択し、回転→移動→正面向き直しの一連のシーケンスを制御する。

### ステートマシン

```
State.Idle
    ↓ (OnStateEnter)
State.RotatingToTarget
    ↓ (目標方向への回転完了)
State.MovingToTarget
    ↓ (目標位置に到達)
State.FinalizingRotation
    ↓ (カメラ方向への回転完了)
State.Idle
```

### パラメータ設定

| パラメータ | デフォルト値 | 説明 |
|-----------|-------------|------|
| `minPosition` | (-0.3, 0, -4.0) | 移動範囲の最小座標 |
| `maxPosition` | (0.3, 0, 0.5) | 移動範囲の最大座標 |
| `minDistance` | 2.0 | 現在位置からの最小移動距離 |
| `retryCount` | 5 | ランダム位置生成のリトライ回数 |
| `moveSpeed` | 1.5 | 移動速度 |
| `acceleration` | 0.5 | 加速度 |
| `rotationSpeed` | 180 | 回転速度（度/秒） |
| `targetProximity` | 0.3 | 到達判定距離 |

### 目標位置選択アルゴリズム

```
GetRandomTargetPosition()
    ↓
1. retryCount回ランダム候補を生成
2. Z座標に応じてX座標の範囲を調整（奥に行くほど広く）
3. 0.2単位でスナップ
4. minDistance以上離れていることを確認
5. 後方（177°以上）でない候補を優先
6. 条件に合う候補がなければフォールバック
```

**後方移動の抑制:**
- 角度が177°以上の方向は「後方」と判定
- 後方でない候補を優先的に選択
- すべて後方の場合のみフォールバックで後方候補を使用

### Animatorパラメータ

| パラメータ | 型 | 説明 |
|-----------|-----|------|
| `walk_x` | float | 左右の回転量（BlendTree用） |
| `walk_y` | float | 前進速度（BlendTree用） |
| `angle` | float | 回転角度（-1〜1に正規化） |
| `turning` | bool | 回転中フラグ |
| `TransitionNo` | int | 遷移先指定 |

### GlobalVariables連携

```csharp
// 移動開始時
GlobalVariables.CurrentState = PetState.moving;

// 睡眠中はスキップ
if (GlobalVariables.CurrentState == PetState.sleeping)
{
    animator.SetInteger("TransitionNo", 1);
    return;
}

// 移動完了時
GlobalVariables.CurrentState = PetState.idle;
```

### UIボタン制御

```csharp
// 移動開始時にボタン非表示
_mainUIButtons.UpdateButtonVisibility(false);

// 移動完了時にボタン表示
_mainUIButtons.UpdateButtonVisibility(true);
```

---

## 派生クラス（空実装）

### SittingBehaviour / LyingBehaviour / BoredBehaviour

```csharp
public class SittingBehaviour : DogBehaviourBase { }
public class LyingBehaviour : DogBehaviourBase { }
public class BoredBehaviour : DogBehaviourBase { }
```

これらは `DogBehaviourBase` の機能をそのまま継承し、Animator の異なるステートに割り当てるためのマーカークラス。各ステートに対応する `DogAnimationGroupBase` ScriptableObject を Inspector で設定することで、ステートごとに異なるアニメーションセットを使用可能。

---

## DogAnimationGroupBase（ScriptableObject）

アニメーション設定を保持するScriptableObject。

### 構造

```csharp
public class DogAnimationGroupBase : ScriptableObject
{
    // 待機時間の範囲
    public RangeFloat waitLoopTime;

    // 遷移用Animatorパラメータ名
    public string transitionAnimationTypeName;

    // ループアニメーションのステートパス
    public List<string> loopAnimationStateFullPaths;

    // 単発アニメーション一覧
    public List<DogAnimationOption> oneShotAnimations;

    // ループアニメーション一覧
    public List<DogAnimationOption> loopAnimations;
}
```

### DogAnimationOption

```csharp
public class DogAnimationOption
{
    public string stateName;          // ステート名
    public int animationType;         // 遷移パラメータ値
    public AudioClip audioClip;       // 再生するサウンド
    public EmotionState emotionState; // 対応する感情状態
}
```

### アニメーション選択

| メソッド | 説明 |
|---------|------|
| `GetRandomOneShotAnimation()` | 単発アニメーションからランダム選択 |
| `GetRandomOneShotAnimationByEmotion(EmotionState)` | 感情でフィルタリングしてランダム選択 |
| `GetRandomLoopAnimation()` | ループアニメーションからランダム選択 |

---

## 処理フロー図

### 通常のアニメーションサイクル

```
[Animator State Enter]
         ↓
    DogBehaviourBase.OnStateEnter()
         ↓
    IsLoopAnimation() チェック
         ↓
    ┌────────────────────────────────┐
    │ ループアニメーション           │
    │  └─ GetEmotionState()          │
    │  └─ GetRandomOneShotAnimation()│
    │  └─ WaitLoopTime 設定          │
    └────────────────────────────────┘
         ↓
    OnStateUpdate() 毎フレーム
         ↓
    _idleTime カウントダウン
         ↓ (0以下)
    PlayAnimationAndSound()
         ↓
    [次のステートへ遷移]
```

### 歩行サイクル

```
[Idle State から歩行開始]
         ↓
    IdleWalkAnimation.OnStateEnter()
         ↓
    GetRandomTargetPosition()
         ↓
    ┌─────────────────────────────────┐
    │ State.RotatingToTarget          │
    │  └─ RotateTowardsTarget()       │
    │  └─ 回転完了まで待機            │
    └─────────────────────────────────┘
         ↓
    ┌─────────────────────────────────┐
    │ State.MovingToTarget            │
    │  └─ MoveAndRotateTowardsTarget()│
    │  └─ 目標到達まで移動            │
    └─────────────────────────────────┘
         ↓
    ┌─────────────────────────────────┐
    │ State.FinalizingRotation        │
    │  └─ カメラ方向へ回転            │
    │  └─ 回転完了まで待機            │
    └─────────────────────────────────┘
         ↓
    ResetState()
         ↓
    [Idle State へ復帰]
```

---

## デバッグ機能

### IdleWalkAnimation

```csharp
[SerializeField] private bool enableDebugLog = false;
```

- `enableDebugLog` を true にするとコンソールに詳細ログ出力
- Unity Editor でのみ `TargetVisualizer` で目標位置を可視化

### TargetVisualizer（Editor専用）

```csharp
#if UNITY_EDITOR
    ShowTarget(animator, targetPosition);
    HideTarget(animator);
#endif
```

---

## 安全機構

### タイムアウト（IdleWalkAnimation）

```csharp
[SerializeField] private float timeoutSeconds = 30f;
private float _stateStartTime = 0f;

// OnStateUpdate内でチェック
if (_currentState != State.Idle && Time.time - _stateStartTime > timeoutSeconds)
{
    Debug.LogWarning($"[IdleWalkAnimation] Timeout after {timeoutSeconds}s - forcing reset");
    ResetState(animator);
    return;
}
```

30秒経過しても歩行が完了しない場合、強制的にリセットして無限ループを防止。

### OnStateExit クリーンアップ（IdleWalkAnimation）

```csharp
public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
{
    if (_currentState != State.Idle)
    {
        // Animatorパラメータをリセット
        animator.SetFloat("walk_x", 0);
        animator.SetFloat("walk_y", 0);
        animator.SetBool("turning", false);

        // UIボタンを再表示
        _mainUIButtons?.UpdateButtonVisibility(true);

        // グローバル状態をリセット
        if (GlobalVariables.CurrentState == PetState.moving)
            GlobalVariables.CurrentState = PetState.idle;
    }
}
```

Animatorが外部からステートを強制終了した場合でも、状態が残らないようクリーンアップ。

### ゼロベクトル対策（IdleWalkAnimation）

```csharp
Vector3 horizontalDirection = new Vector3(cameraDirection.x, 0f, cameraDirection.z);

// ゼロベクトルチェック（犬がカメラ真下付近にいる場合）
if (horizontalDirection.sqrMagnitude < 0.0001f)
{
    ResetState(animator);  // 最終回転をスキップ
}
else
{
    finalRotationEnd = Quaternion.LookRotation(horizontalDirection.normalized);
}
```

犬がカメラ真下にいる場合、`LookRotation(Vector3.zero)` の警告を回避。

---

## 注意事項

1. **サウンド同期の精度**: DSP時間を使用しているため、Time.deltaTimeよりも正確な同期が可能
2. **async/await使用**: `PlayAnimationAndSound()` は async Task だが、fire-and-forget で呼び出し
3. **GlobalVariables依存**: 睡眠状態や空腹状態の判定に GlobalVariables を参照
4. **UI連携**: 移動中はUIボタンを非表示にする制御が含まれる
5. **後方移動抑制**: 犬が後ろ向きに歩かないようアルゴリズムで制御
