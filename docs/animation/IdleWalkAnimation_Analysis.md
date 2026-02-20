# IdleWalkAnimationNavMesh ドキュメント

## 概要

`IdleWalkAnimationNavMesh.cs`は`StateMachineBehaviour`を継承したクラスで、犬がアイドル状態から歩行するアニメーションを制御します。NavMeshによるパス検証機能を備えています。

**ファイル:** `Assets/Scripts/StateMachineBehaviour/IdleWalkAnimationNavMesh.cs`

---

## 状態遷移図

```
OnStateEnter
    │
    ▼
┌─────────────────┐
│ 目標位置を決定   │  GetRandomTargetPositionWithNavMesh()
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ 角度差をチェック  │  現在の向き vs 目標方向
└────────┬────────┘
         │
    ┌────┴────┐
    │         │
角度差 > 0.3°  角度差 ≤ 0.3°
    │         │
    ▼         ▼
┌───────────────┐  ┌───────────────┐
│RotatingToTarget│  │MovingToTarget │
│ (目標方向に回転)│  │ (移動開始)     │
└───────┬───────┘  └───────┬───────┘
        │                  │
        └────────┬─────────┘
                 │
                 ▼ (移動完了)
        ┌───────────────────┐
        │FinalizingRotation │
        │ (カメラ方向に回転)  │
        └─────────┬─────────┘
                  │
                  ▼ (回転完了)
        ┌───────────────────┐
        │    ResetState     │
        │    (終了処理)      │
        └───────────────────┘
```

---

## 内部状態 (State enum)

| 状態 | 説明 |
|------|------|
| `Idle` | 待機状態（処理なし） |
| `RotatingToTarget` | 目標位置の方向に回転中 |
| `MovingToTarget` | 目標位置に向かって移動中 |
| `FinalizingRotation` | カメラ方向に回転中（最終処理） |

---

## LateUpdate回転制御

アニメーションクリップがルートのrotationキーフレームを持っている場合、`OnStateUpdate`内で`transform.rotation`を設定しても上書きされる問題があります。

### 解決策: DogControllerのLateUpdate経由で回転適用

```
処理フロー:
OnStateUpdate
  └─ _dogController.SetPendingRotation(rotation)
       └─ _pendingRotation に保存
            ↓
アニメーション評価（ここで回転が上書きされる）
            ↓
LateUpdate
  └─ transform.rotation = _pendingRotation.Value
       └─ 最終的に正しい回転が適用される
```

---

## 設定パラメータ

| パラメータ | デフォルト値 | 説明 |
|-----------|-------------|------|
| minPosition | (-0.3, 0, -4.0) | 移動範囲の最小座標 |
| maxPosition | (0.3, 0, 0.5) | 移動範囲の最大座標 |
| minDistance | 2.0 | 最低移動距離 |
| retryCount | 5 | 目標位置探索のリトライ回数 |
| moveSpeed | 1.5 | 移動速度 |
| acceleration | 0.5 | 加速度 |
| rotationSpeed | 180 | 回転速度（度/秒） |
| targetProximity | 0.3 | 到着判定距離 |
| rotationThreshold | 0.3 | 回転開始の角度しきい値 |
| smoothingSpeedX | 60 | walk_xパラメータのスムージング速度 |
| smoothingSpeedY | 60 | walk_yパラメータのスムージング速度 |
| timeoutSeconds | 30 | タイムアウト時間 |

---

## スキップ条件

以下の状態では歩行処理をスキップ:
- `PetState.sleeping` - 睡眠中
- `PetState.reminder` - リマインダー表示中

---

## NavMesh検証

目標位置の決定時にNavMeshを使用して到達可能性をチェック:

1. `NavMesh.SamplePosition` - 候補位置をNavMesh上に補正
2. `NavMesh.CalculatePath` - 到達可能なパスが存在するかチェック
3. `NavMeshPathStatus.PathComplete` - パスが完全に到達可能かを確認

---

## 主要メソッド

| メソッド | 説明 |
|---------|------|
| `Initialize()` | 初回初期化（OnStateEnterで呼び出し） |
| `OnStateEnter()` | 状態に入った時の処理 |
| `OnStateUpdate()` | 毎フレームの更新処理 |
| `OnStateExit()` | 状態を抜ける時のクリーンアップ |
| `RotateTowardsTarget()` | 目標方向への回転処理 |
| `MoveAndRotateTowardsTarget()` | 移動と回転の同時処理 |
| `ResetState()` | 状態のリセット |
| `GetRandomTargetPositionWithNavMesh()` | NavMesh検証付き目標位置の取得 |
