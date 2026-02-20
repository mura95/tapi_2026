# ボール遊び機能 仕様書

## 概要

犬がボールやロープなどのおもちゃで遊ぶ機能。ユーザーがおもちゃを選択すると、犬がおもちゃを咥えて投げ、追いかけて取ってくるという一連の動作を行います。

## ファイル構成

| ファイル | 役割 |
|----------|------|
| `PlayManager.cs` | おもちゃのスポーンと投げ処理の管理 |
| `PlayToy.cs` | 遊びのシーケンス制御（噛む・振る・投げる） |
| `ToyFetcherBase.cs` | おもちゃ取得処理の基底クラス |
| `GetBall.cs` | ボール用のフェッチ処理 |
| `GetToy.cs` | その他おもちゃ用のフェッチ処理 |

## 遊びのフロー

### 全体フロー

```
ユーザーがおもちゃ選択
    ↓
PlayManager.SpawnToy()
    ↓
PlayToy.DoPlayCoroutine()
    ↓
[50%] BiteSequence → ThrowSequence
[50%] ThrowSequence
    ↓
PlayManager.ThrowToy()
    ↓
ToyFetcherBase.FetchToy()
    ↓
犬がおもちゃを追いかける
    ↓
おもちゃを咥えて戻る
    ↓
初期位置に帰還
```

## PlayToy（遊びシーケンス）

### アクション選択

| アクション | 確率 | 説明 |
|------------|------|------|
| 噛みつき→投げ | 50% | 5秒間噛みついてから投げる |
| 直接投げ | 50% | 振って直接投げる |

### BiteSequence（噛みつきシーケンス）

1. `SetBite(true)` - 噛みつきアニメーション開始
2. おもちゃを口元に配置（`toyBiteLocalPosition`）
3. 5秒間（`biteTime`）待機
4. `SetBite(false)` - 噛みつきアニメーション終了
5. ThrowSequenceへ移行

### ThrowSequence（投げシーケンス）

1. `SetSwing(true)` - 振りアニメーション開始
2. おもちゃを投げ位置に配置（`toyThrowMouthLocalPos`）
3. 1±0.2秒（`throwTiming`±`throwTimingVariation`）待機
4. `ExecuteThrow()` - おもちゃを投げる
5. 1秒（`chaseWaitTime`）待機
6. `SetSwing(false)` - 振りアニメーション終了

### 投げ角度戦略

`I_ToyThrowAngle`インターフェースを実装した戦略クラスを使用：

| クラス | 説明 |
|--------|------|
| `AngleSetForCenter` | 画面中央に向かって投げる |
| `AngleSetForPoint` | 指定したポイントに向かって投げる |

### 設定パラメータ

| パラメータ | デフォルト値 | 説明 |
|------------|-------------|------|
| `biteToyProbability` | 0.5 | 噛みつきアクションの確率 |
| `biteTime` | 5秒 | 噛みつき時間 |
| `throwTiming` | 1秒 | 投げまでの待機時間 |
| `throwTimingVariation` | 0.2秒 | 投げタイミングのランダム変動 |
| `chaseWaitTime` | 1秒 | 投げ後の待機時間 |
| `minThrowSpeed` | 0.5 | 最小投げ速度 |
| `maxThrowSpeed` | 1.5 | 最大投げ速度 |

## ToyFetcherBase（おもちゃ取得）

### 基本動作

1. `FetchToy()`でおもちゃ追跡開始
2. NavMeshAgentを有効化して目標に移動
3. おもちゃに到達したら咥える
4. 初期位置に戻る
5. `OnReachedReturn()`で終了処理

### 移動設定

| パラメータ | デフォルト値 | 説明 |
|------------|-------------|------|
| `runMaxSpeed` | 5 | 追いかけ時の最大速度 |
| `returnSpeed` | 4 | 戻り時の速度 |

### 安全制限

| パラメータ | デフォルト値 | 説明 |
|------------|-------------|------|
| `maxToyDistance` | 50ユニット | おもちゃの最大距離 |
| `maxPlayTime` | 180秒 | 遊び時間の最大値 |

これらの制限を超えると自動的に遊びを終了して戻ります。

### NavMesh制御

```csharp
// NavMeshAgentの有効/無効
BeNaviMesh = true/false;

// NavMesh上に再配置
EnsureAgentOnNavMesh();
```

### アニメーション更新

`UpdateMovementAnimation()`でNavMeshAgentの速度に基づいてアニメーションを更新：

```csharp
Vector3 localVelocity = transform.InverseTransformDirection(agent.velocity);
float normalizedSpeedX = Mathf.Clamp(Mathf.Abs(localVelocity.x) / runMaxSpeed, 0, 1);
float normalizedSpeedZ = Mathf.Clamp(Mathf.Abs(localVelocity.z) / runMaxSpeed, 0, 1);
dogController.StartMoving(normalizedSpeedX, normalizedSpeedZ);
```

## フリーズ防止策

### PlayToy

1. **再入防止フラグ**: `isCoroutineRunning`で二重実行を防止
2. **タイムアウト監視**: `TimeoutWatcher()`で30秒後に強制キャンセル
3. **キャンセルフラグ**: `isCancelled`で途中キャンセル対応
4. **try-finally**: `ExecuteThrow()`でアニメーション状態を確実にリセット

### ToyFetcherBase

1. **距離チェック**: 50ユニット以上離れたら終了
2. **時間チェック**: 180秒以上経過したら終了
3. **NavMesh再配置**: `EnsureAgentOnNavMesh()`で確実にNavMesh上に配置

## おもちゃの種類

### GetBall

ボール専用のフェッチ処理。`ToyFetcherBase`を継承。

### GetToy

ロープなどその他のおもちゃ用。`ToyFetcherBase`を継承。

## 状態管理

### PetState

| 状態 | 説明 |
|------|------|
| `idle` | 通常状態 |
| `playing` | 遊び中 |

### GlobalVariables

- `AttentionCount`: 注目カウンター（遊び終了時にリセット）

## デバッグ機能

### PlayToyデバッグ設定

| 設定 | 説明 |
|------|------|
| `enableBiteAnimation` | 噛みつきアニメーションの有効/無効 |
| `enableSwingAnimation` | 振りアニメーションの有効/無効 |
| `enableDetailedLogs` | 詳細ログの出力 |

### ログ出力

`[PlayToy]`プレフィックスで詳細なログを出力：

```
[PlayToy] DoPlayCoroutine: START
[PlayToy] Random=0.342, Threshold=0.500
[PlayToy] Action: BITE
[PlayToy] === BITE SEQUENCE START ===
...
```

## Firebase連携

遊び終了時にFirebaseにログを送信：

```csharp
firebaseManager?.UpdatePetState("idle");
dogStateController?.OnPlay();
```

## 関連クラス

| クラス | 役割 |
|--------|------|
| `DogController` | 犬のアニメーション制御 |
| `PlayManager` | おもちゃのスポーンと投げ管理 |
| `TurnAndMoveHandler` | 回転と移動のハンドリング |
| `MatchRotation` | おもちゃの回転を犬に合わせる |
| `MainUIButtons` | UIボタン表示制御 |
| `MainUICloseButton` | 閉じるボタン制御 |

## 注意事項

1. **NavMeshの配置**: 遊びエリアにNavMeshがベイクされている必要がある
2. **おもちゃのRigidbody**: おもちゃにはRigidbodyコンポーネントが必要
3. **マウス位置**: `AngleSetForPoint`使用時はカメラの設定が正しい必要がある
4. **アニメーション同期**: 噛みつき・振りのアニメーションはDogControllerで管理
