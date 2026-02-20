# インタラクションシステム 仕様書

## 概要

犬の移動・回転制御と各種アクションの実行を管理。TurnAndMoveHandlerによる移動システムと、DogControllerによるAnimator制御を担当します。

## ファイル構成

| ファイル | 役割 |
|----------|------|
| `TurnAndMoveHandler.cs` | 犬の回転・移動制御 |
| `DogController.cs` | Animatorパラメータ制御、アクション実行 |

**関連仕様書:** [touch-system.md](./touch-system.md) - タッチ入力、なでなで、長押し機能

---

## 移動・回転制御（TurnAndMoveHandler）

犬を指定位置に移動させるシステム。

### 状態マシン

```
[Idle] ──(StartTurnAndMove呼び出し)──> [TurningToFront]
                                              │
                                              ▼
                                       カメラ方向に回転
                                              │
                                   (回転完了 or 閾値以下)
                                              │
                                              ▼
                                       [MovingToTarget]
                                              │
                                       目標位置へ移動
                                              │
                                   (目標到達 or 近接)
                                              │
                                              ▼
                                          [Idle]
                                     GlobalState = idle
```

### パラメータ

| フィールド | デフォルト値 | 説明 |
|-----------|-------------|------|
| `rotationSpeed` | 1.0 | 回転速度 |
| `moveSpeed` | 1.0 | 移動速度 |
| `targetProximity` | 1.0 | 目標到達判定距離 |
| `rotationThreshold` | 5° | 回転完了判定角度 |

### Animatorパラメータ

| パラメータ | 型 | 説明 |
|-----------|-----|------|
| `TurnBool` | bool | 回転中フラグ |
| `TurnFloat` | float | 回転方向（0.0=左, 0.5=正面, 1.0=右） |
| `WalkBool` | bool | 歩行中フラグ |
| `walk_x` | float | 歩行方向X成分 |
| `walk_y` | float | 歩行方向Z成分 |

### 使用例

```csharp
// 中心位置（0,0,0）に移動、速度1.5
turnAndMoveHandler.StartTurnAndMove(new Vector3(0, 0, 0), 1.5f);

// 特定位置に移動、PetStateを変更しない
turnAndMoveHandler.StartTurnAndMove(targetPos, 1.0f, PetState.idle);
```

### TouchControllerからの呼び出し

```csharp
// 犬が中心から離れすぎている場合（なでなで時）
if (characterController.transform.position.sqrMagnitude > 0.1f)
{
    characterController.Petting(false);
    pettingIcon.enabled = false;
    turnAndMoveHandler.StartTurnAndMove(new Vector3(0, 0, 0), 1.5f);
    return;
}
```

---

## DogController アクション

各種アクションのトリガーメソッド。すべて中心位置への移動を伴います。

### アクション一覧

| メソッド | Animatorトリガー | 説明 |
|---------|-----------------|------|
| `ActionRPaw()` | `PawRStart` | 右お手 |
| `ActionLPaw()` | `PawLStart` | 左お手（おかわり） |
| `ActionDance()` | `DanceStart` | ダンス |
| `ActionDang()` | `BangStart` | バーン（死んだふり） |
| `ActionStand()` | `StandStart` | 立て |
| `ActionHighDance()` | `HighDanceStart` | ハイダンス |
| `ActionLieDown()` | - | ふせ（TransitionNo=1） |
| `ActionBark()` | `BarkStart` | 吠える |

### なでなで関連メソッド

TouchControllerから呼び出されるメソッド。

| メソッド | 説明 |
|---------|------|
| `Petting(bool)` | なでなで状態の設定 |
| `SetPatFloat(float)` | なでる位置の設定 |
| `LayerBarkTrigger()` | レイヤー付き吠えるトリガー |

### 長押し関連メソッド

TouchControllerから呼び出されるメソッド。

| メソッド | 説明 |
|---------|------|
| `SetLieOnBackTrue()` | 仰向け状態開始 |
| `UpdateLieOnBackState(int)` | 仰向けアニメーション種類設定 |
| `UpdateTransitionState(int)` | 状態遷移番号設定 |
| `GetCurrentAnimationLength()` | 現在のアニメーション長さ取得 |

---

## Animatorパラメータ一覧

### なでなで関連

| パラメータ | 型 | 説明 |
|-----------|-----|------|
| `isPetting` | bool | なでなで中 |
| `PatFloat` | float | なでる位置（0.0～1.0） |

### 長押し関連

| パラメータ | 型 | 説明 |
|-----------|-----|------|
| `LieOnBackBool` | bool | 仰向け状態 |
| `LieOnBackType` | int | 仰向けアニメーション種類（0-2） |

### 状態関連

| パラメータ | 型 | 説明 |
|-----------|-----|------|
| `TransitionNo` | int | 状態遷移番号 |
| `ActionBool` | bool | アクション実行中 |
| `SleepBool` | bool | 睡眠中 |
| `EatingBool` | bool | 食事中 |
| `snackType` | int | おやつ種類 |

### 遊び関連

| パラメータ | 型 | 説明 |
|-----------|-----|------|
| `BiteBool` | bool | 噛みつき中 |
| `SwingBool` | bool | 振り中 |

---

## TransitionNo 一覧

| 値 | 状態 | 説明 |
|----|------|------|
| 0 | 通常 | 標準状態 |
| 1 | ふせ | ActionLieDown() |
| 3 | 特定トランジション | タッチ無効 |
| 4 | 仰向け | LieOnBack |

---

## 関連コンポーネント

| コンポーネント | 役割 |
|---------------|------|
| `TouchController` | タッチ入力、なでなで、長押し処理 |
| `MaskLayerManager` | Animatorレイヤーウェイト制御 |
| `Animator` | アニメーション制御 |
