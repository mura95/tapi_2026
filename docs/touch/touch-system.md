# タッチシステム 仕様書

## 概要

画面タッチによる犬との対話機能。タッチ入力の検出・状態管理、なでなで機能、長押しで仰向けなど、TouchController.csで実装される機能を管理します。

## ファイル構成

| ファイル | 役割 |
|----------|------|
| `TouchController.cs` | タッチ入力の検出、なでなで、長押し機能 |

**関連仕様書:** [interaction-system.md](./interaction-system.md) - 移動・回転制御、DogControllerアクション

---

## 処理フロー

### 全体フロー

```
タッチ入力検出（Update毎フレーム）
    ↓
┌──────────────────────────────────────┐
│ 入力取得                             │
│   ├── Input.touchCount > 0 ?         │
│   │     → Input.GetTouch(0).position │
│   └── または Input.mousePosition     │
└──────────────────────────────────────┘
    ↓
┌──────────────────────────────────────┐
│ 無効化条件チェック                   │
│   ├── マルチデバイス状態             │
│   ├── アプリ状態                     │
│   └── 犬の状態                       │
└──────────────────────────────────────┘
    ↓
[無効] → タッチ処理スキップ、状態リセット
[有効] → 続行
    ↓
┌──────────────────────────────────────┐
│ CheckHoldTouch() - 長押し判定        │
│   └── 3秒以上 → LieOnBack状態に移行  │
└──────────────────────────────────────┘
    ↓
┌──────────────────────────────────────┐
│ DefaultPetting() - なでなで処理      │
│   ├── Raycastで犬への接触判定        │
│   ├── 犬が遠い → 移動して戻す        │
│   └── タッチ終了 → 吠える            │
└──────────────────────────────────────┘
```

---

## タッチ無効化条件

以下の状態ではタッチ入力が無効化されます：

### マルチデバイス関連

| 条件 | 説明 | 処理 |
|------|------|------|
| `DogLocationSync.HasDog == false` | 犬がいない | 即座にリセット |
| `DogTransferAnimation.IsAnimating` | 転送アニメーション中 | 即座にリセット |

### アプリ状態関連

| 条件 | 説明 | 処理 |
|------|------|------|
| `GlobalVariables.CurrentState == reminder` | リマインダー表示中 | 即座にリセット |
| `GlobalVariables.CurrentState == napping` | 昼寝中 | 眠い反応のみトリガー |
| `GlobalVariables.CurrentState != idle` | idle以外 | スキップ |
| `GlobalVariables.IsInputUserName == true` | ユーザー名入力中 | スキップ |

### 犬の状態関連

| 条件 | 説明 | 処理 |
|------|------|------|
| `GetSleepBool() == true` | 夜間睡眠中 | スキップ |
| `GetSnackType() != 0` | おやつ中 | スキップ |
| `GetTransitionNo() == 3` | 特定トランジション中 | スキップ |

### 優先順位

```
リマインダー > 睡眠 > その他の状態 > タッチ
```

---

## タッチ状態管理

### 状態変数

| 変数 | 型 | 説明 |
|------|-----|------|
| `isTouching` | bool | 現在タッチ中か |
| `touchTimer` | float | タッチ継続時間 |
| `releaseTouchTimer` | float | タッチ離してからの時間 |
| `continuousTouch` | bool | なでなで継続中か |
| `shouldLogPetting` | bool | ログ送信フラグ |

### 状態遷移

```
[タッチなし]
    │
    ├── (タッチ開始)
    ▼
[タッチ中]
    │  ├── isTouching = true
    │  ├── touchTimer += Time.deltaTime
    │  └── releaseTouchTimer = 0
    │
    ├── (タッチ終了)
    ▼
[タッチなし]
    ├── isTouching = false
    ├── touchTimer = 0
    └── 終了処理（吠える等）
```

### 状態リセット

```csharp
private void ResetTouchState(string reason)
{
    continuousTouch = false;
    shouldLogPetting = false;
    touchTimer = 0f;
}
```

---

## Raycast判定

### 設定

| パラメータ | 値 | 説明 |
|-----------|-----|------|
| `targetLayerName` | `"dog"` | 対象レイヤー |
| `RaycastDistance` | 5.0 | 最大距離（ユニット） |
| `layerMask` | dog & ~UI | UIレイヤーを除外 |

### 処理フロー

```
スクリーン座標
    ↓
Camera.main.ScreenPointToRay(screenPosition)
    ↓
Physics.Raycast(ray, out hit, 5.0f, layerMask)
    ↓
[ヒット] → hit.collider が犬か確認
[ミス]   → なでなで無効
```

### 接触位置の正規化

```csharp
Vector3 localPointInObject = characterController.transform.InverseTransformPoint(hit.point);
float objectWidth = collider.bounds.size.x;
float normalizedX = (localPointInObject.x / objectWidth) + 0.5f;
// 結果: 0.0（左端）～ 1.0（右端）
```

この値は `PatFloat` パラメータとしてAnimatorに渡され、なでる位置に応じたアニメーションを制御します。

---

## なでなで機能（Petting）

### 処理フロー

```
タッチ開始
    ↓
犬の位置チェック（中心から離れすぎ？）
    ↓
[遠い: sqrMagnitude > 0.1f]
    └── TurnAndMoveHandler.StartTurnAndMove() で呼び戻し
    └── なでなでスキップ
    ↓
[近い] → 続行
    ↓
Raycast判定
    ↓
[犬にヒット]
    ├── 接触位置を正規化（0.0～1.0）
    ├── SetPatFloat() でアニメーション制御
    ├── MaskLayerManager.SetLayerWeight("face", 0)
    └── Petting(true) でなでなでアニメーション開始
    ↓
タッチ終了
    ├── Petting(false)
    ├── Firebase にログ送信
    ├── DogStateController.OnPet() で愛情度更新
    ├── MaskLayerManager.SetLayerWeight("Bark", 1, 3f)
    ├── LayerBarkTrigger() で吠える
    └── AttentionCount = 0
```

### Animatorパラメータ

| パラメータ | 型 | 説明 |
|-----------|-----|------|
| `isPetting` | bool | なでなで中フラグ |
| `PatFloat` | float | なでる位置（0.0=左端 ～ 1.0=右端） |

### 愛情度への影響

```csharp
// タッチ終了時
pettingManager.UpdateLog("skill");
_dogStateController.OnPet();
```

---

## 長押し機能（LieOnBack）

3秒以上の長押しで犬が仰向けになります。

### パラメータ

| 定数 | 値 | 説明 |
|------|-----|------|
| `MaxTouchDuration` | 3.0秒 | 長押し判定の閾値 |
| `ResetDelay` | 3.0秒 | 離した後の状態リセット遅延 |

### 状態遷移

```
[通常状態]
    │
    ├── (3秒長押し)
    ▼
[LieOnBack状態]
    │  ├── SetLieOnBackTrue()
    │  ├── UpdateTransitionState(4)
    │  └── UpdateLieOnBackStateLoop() 開始
    │
    ├── (タッチ継続) → ランダムアニメーション（0～2）
    │
    ├── (タッチ離す)
    │       ▼
    │   [待機アニメーション（type 2）]
    │       │
    │       ├── (3秒経過)
    │       ▼
    │   [通常状態に復帰]
    │       ├── UpdateTransitionState(0)
    │       ├── isInLieOnBackState = false
    │       └── Petting(false)
```

### LieOnBackType アニメーション

| 値 | アニメーション | 発生条件 |
|----|---------------|---------|
| 0 | ランダム動作1 | タッチ継続中 |
| 1 | ランダム動作2 | タッチ継続中 |
| 2 | 待機 | タッチ離し時 |

### アニメーションループ処理

```csharp
private IEnumerator UpdateLieOnBackStateLoop()
{
    while (isInLieOnBackState)
    {
        if (releaseTouchTimer > 0)
        {
            // タッチ離し中は待機アニメーション
            characterController.UpdateLieOnBackState(2);
        }
        else
        {
            // タッチ中はランダムアニメーション
            characterController.UpdateLieOnBackState(Random.Range(0, 3));
        }

        LieOnBackAnimationLength = characterController.GetCurrentAnimationLength();
        yield return new WaitForSeconds(LieOnBackAnimationLength);
    }
}
```

### Animatorパラメータ

| パラメータ | 型 | 説明 |
|-----------|-----|------|
| `LieOnBackBool` | bool | 仰向け状態フラグ |
| `LieOnBackType` | int | アニメーション種類（0-2） |
| `TransitionNo` | int | 状態遷移番号（4=仰向け） |

---

## なでなでアイコン

タッチ位置にアイコンを表示する機能。

### 設定

| フィールド | 説明 |
|-----------|------|
| `pettingIcon` | タッチ位置に表示するImage |
| `pettingIconRectTransform` | アイコンのRectTransform |
| `canvasRectTransform` | 親CanvasのRectTransform |

### 処理

```csharp
// タッチ位置をCanvas座標に変換
if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
    canvasRectTransform,
    screenPosition,
    canvas.worldCamera,
    out var localPoint))
{
    pettingIconRectTransform.anchoredPosition = localPoint;
    pettingIcon.enabled = true;
}
```

### 重要：アイコン表示とRaycastの関係

**アイコンが表示されていても、犬に触れているとは限らない**

```
処理フロー:

CheckHoldTouch()
    └── タッチがあればアイコン表示（位置に関係なく）
            ↓
DefaultPetting()
    └── Raycast判定（犬にヒットした時のみなでなで有効）

結果:
├── アイコン表示 ＋ 犬にヒット → なでなで有効 ✓
└── アイコン表示 ＋ 犬にミス → なでなで無効 ✗（アイコンは表示される）
```

### アイコン表示されるが反応しない原因

| 原因 | 詳細 | 対処 |
|------|------|------|
| **犬が中心から離れている** | `sqrMagnitude > 0.1f`（約0.32m以上） | 犬が自動で中心に移動 |
| **Raycastが犬に当たらない** | タッチ位置と犬の位置がズレている | 犬の真上をタッチ |
| **レイヤー設定ミス** | 犬が`dog`レイヤーにない | レイヤー確認 |
| **コライダーが小さい** | 視覚的なメッシュよりコライダーが小さい | コライダー調整 |

---

## エッジケース処理

### タッチ中に状態が変わった場合

```csharp
// タッチ中にリマインダーが来た場合
if (GlobalVariables.CurrentState == PetState.reminder)
{
    pettingIcon.enabled = false;
    ResetTouchState("Reminder state");
    return;  // タッチ処理をスキップ
}
```

### マルチタッチ処理

**仕様:** 最初の1本目の指のみ有効。2本目以降は無視。

```csharp
// 常に最初のタッチのみ使用
Vector2 screenPosition = Input.touchCount > 0
    ? Input.GetTouch(0).position  // ← 常にインデックス0
    : (Vector2)Input.mousePosition;
```

### 画面外へのスワイプアウト

**仕様:** タッチ終了として処理（リセット）

- Unityの`TouchPhase.Canceled`として検出される
- `Input.touchCount`が0になるため、自動的にタッチ終了処理が走る

### アプリがバックグラウンドになった場合

**仕様:** タッチ状態を全リセット

- `OnApplicationPause(true)` でタッチ状態をリセットすべき
- 現状は `Input.touchCount` が0になることで自動リセット

---

## マルチデバイス対応

### 犬不在時の処理

```csharp
if (!DogLocationSync.Instance.HasDog)
{
    pettingIcon.enabled = false;
    ResetTouchState("Dog not present (multi-device)");
    return;
}
```

### 転送アニメーション中の処理

```csharp
if (_dogTransferAnimation.IsAnimating)
{
    pettingIcon.enabled = false;
    ResetTouchState("Dog transfer animation in progress");
    return;
}
```

---

## 昼寝中の特殊処理

昼寝中（`napping`）にタッチすると、眠い反応をトリガーします。

```csharp
if (GlobalVariables.CurrentState == PetState.napping)
{
    // 眠い反応中はタッチを無視
    if (_sleepController.IsInSleepyReaction())
    {
        return;
    }

    // タッチ開始時のみ反応をトリガー
    if (currentTouchInput && !isTouching)
    {
        _sleepController.OnPetTouched();
    }
}
```

---

## パフォーマンス最適化

### 現状の問題点

#### 1. 毎フレームのGetComponent呼び出し

```csharp
// ❌ 現在の実装（毎フレーム呼び出し）
Collider collider = characterController.GetComponent<Collider>();
```

**推奨修正:**
```csharp
// ✅ Start()でキャッシュ
private Collider _dogCollider;

void Start()
{
    _dogCollider = characterController.GetComponent<Collider>();
}
```

**影響度:** 中

---

#### 2. Camera.mainの毎フレームアクセス

```csharp
// ❌ 現在の実装
Ray ray = Camera.main.ScreenPointToRay(screenPosition);
```

**推奨修正:**
```csharp
// ✅ Start()でキャッシュ
private Camera _mainCamera;

void Start()
{
    _mainCamera = Camera.main;
}
```

**影響度:** 中

---

#### 3. デバッグログの文字列アロケーション

```csharp
// ❌ 現在の実装
LogDebug($"[LieOnBack Loop] Animation length: {LieOnBackAnimationLength:F2}s");
```

**推奨修正:**
```csharp
// ✅ Conditional属性でリリースビルドから除外
[System.Diagnostics.Conditional("UNITY_EDITOR")]
private void LogDebug(string message) { ... }
```

**影響度:** 低〜中

---

#### 4. FindObjectOfType の使用

```csharp
// ❌ 現在の実装
_dogTransferAnimation = FindObjectOfType<DogTransferAnimation>();
```

**推奨修正:**
```csharp
// ✅ SerializeFieldで直接参照
[SerializeField] private DogTransferAnimation _dogTransferAnimation;
```

**影響度:** 低

---

### 最適化優先度

| 優先度 | 項目 | 理由 |
|--------|------|------|
| **高** | Colliderキャッシュ | 毎フレーム呼び出し |
| **高** | Camera.mainキャッシュ | 毎フレーム呼び出し |
| **中** | デバッグログ最適化 | GC発生源 |
| **低** | FindObjectOfType置換 | Start()で1回のみ |

---

### Raycastの最適化（検討事項）

| 方法 | メリット | デメリット |
|------|---------|-----------|
| 現状維持 | 精度が高い | 毎フレームコスト |
| N フレームおき | コスト削減 | 反応が若干遅れる |
| 移動量閾値 | タッチ移動時のみ | 実装複雑化 |

**推奨:** 現状維持（犬1体のみでLayerMask指定済みのため軽量）

---

## デバッグ

### デバッグログ有効化

```csharp
[SerializeField] private bool enableDebugLog = true;
```

### ログ出力例

```
[TouchController] [Touch State Changed] false -> true
[TouchController] [Petting Start] Hit at normalized X: 0.45
[TouchController] [Touch End] Triggering bark and resetting
[TouchController] [LieOnBack] Triggered after 3.05s hold
[TouchController] [LieOnBack Loop] Animation length: 2.30s
[TouchController] [LieOnBack] Exiting after 3.00s release delay
```

---

## 関連コンポーネント

| コンポーネント | 役割 |
|---------------|------|
| `DogController` | Animatorパラメータ制御（Petting, LieOnBack等） |
| `TurnAndMoveHandler` | 犬が遠い時の呼び戻し |
| `MaskLayerManager` | Animatorレイヤーウェイト制御 |
| `FirebaseManager` | なでなでログの送信 |
| `DogStateController` | 愛情度更新（OnPet） |
| `DogLocationSync` | マルチデバイス状態管理 |
| `DogTransferAnimation` | 転送アニメーション状態 |
| `SleepController` | 昼寝反応トリガー |
| `GlobalVariables` | アプリ状態管理 |
