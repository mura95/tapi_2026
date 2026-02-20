# 加速度センサー歩行連動

## 1. 概要

スマートフォンの加速度センサーを使用して、実際に歩く/振る動作をメタバース内の移動に連動させる機能。高齢者が実際に体を動かしながらメタバースを楽しめる。

---

## 2. コンセプト

### 2.1 歩行連動の仕組み

```
┌─────────────────────────────────────────────────────────────┐
│                    加速度センサー歩行連動                    │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│   実世界                       メタバース                   │
│                                                             │
│   ┌─────────────┐             ┌─────────────┐              │
│   │             │             │             │              │
│   │  👤 歩く    │ ─────────→ │  👤 歩く    │              │
│   │  📱         │  加速度     │  🐕         │              │
│   │             │  センサー   │             │              │
│   └─────────────┘             └─────────────┘              │
│                                                             │
│   【検出方法】                                               │
│   - スマホを持って歩く → 上下の加速度変化を検出             │
│   - スマホを左右に振る → 左右の加速度変化を検出             │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 2.2 操作パターン

| 操作 | 動作 | 対象 |
|------|------|------|
| 歩く（スマホを持って） | 前進 | 歩ける人向け |
| 左右に振る | 前進 | 座ったままの人向け |
| 傾ける | 方向転換 | 共通 |

---

## 3. 加速度センサーの使用

### 3.1 Unityでの取得

```csharp
using UnityEngine;

public class AccelerometerReader : MonoBehaviour
{
    // 加速度を取得
    public Vector3 GetAcceleration()
    {
        return Input.acceleration;
    }

    // ローパスフィルター適用
    public Vector3 GetFilteredAcceleration(float alpha = 0.8f)
    {
        Vector3 raw = Input.acceleration;
        filteredAcceleration = Vector3.Lerp(filteredAcceleration, raw, alpha);
        return filteredAcceleration;
    }

    private Vector3 filteredAcceleration;
}
```

### 3.2 歩行検出の原理

```
加速度の変化パターン（歩行時）

    Y軸加速度
      ↑
    1.2 ┤     *       *
    1.0 ┤   *   *   *   *
    0.8 ┤  *     * *     *
    0.6 ┤ *               *
      ──┼─────────────────→ 時間
        │ 足が   足が   足が
        │ 着地   離地   着地

    歩行 = 加速度のピークが周期的に発生
```

---

## 4. 実装

### 4.1 AccelerometerWalkController.cs

```csharp
using UnityEngine;
using System.Collections.Generic;

namespace TapHouse.Pocket.Metaverse
{
    public class AccelerometerWalkController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float stepThreshold = 0.3f;      // 歩行検出閾値
        [SerializeField] private float stepCooldown = 0.3f;       // 連続検出防止
        [SerializeField] private float moveSpeed = 2f;            // 移動速度
        [SerializeField] private float turnSensitivity = 60f;     // 回転感度

        [Header("Filter")]
        [SerializeField] private float lowPassAlpha = 0.8f;       // ローパスフィルター係数

        [Header("Debug")]
        [SerializeField] private bool enableDebugLog = false;

        // 状態
        private Vector3 filteredAcceleration;
        private float lastStepTime;
        private float lastMagnitude;
        private bool isWalking;

        // イベント
        public event System.Action OnStepDetected;
        public event System.Action<bool> OnWalkingStateChanged;

        // 出力
        public Vector3 MoveDirection { get; private set; }
        public float TurnAngle { get; private set; }
        public bool IsWalking => isWalking;
        public int StepCount { get; private set; }

        private void Update()
        {
            UpdateAcceleration();
            DetectStep();
            UpdateMovement();
        }

        private void UpdateAcceleration()
        {
            // ローパスフィルターで高周波ノイズを除去
            Vector3 raw = Input.acceleration;
            filteredAcceleration = Vector3.Lerp(filteredAcceleration, raw, lowPassAlpha * Time.deltaTime * 10);
        }

        private void DetectStep()
        {
            // 重力成分を除去した加速度の大きさ
            Vector3 gravity = filteredAcceleration.normalized;
            Vector3 linearAcceleration = Input.acceleration - gravity;
            float magnitude = linearAcceleration.magnitude;

            // クールダウン中は検出しない
            if (Time.time - lastStepTime < stepCooldown)
            {
                lastMagnitude = magnitude;
                return;
            }

            // ピーク検出（前フレームより大きく、閾値を超えている）
            if (magnitude > stepThreshold && magnitude > lastMagnitude && lastMagnitude < stepThreshold)
            {
                // 歩行検出
                lastStepTime = Time.time;
                StepCount++;
                OnStepDetected?.Invoke();

                if (!isWalking)
                {
                    isWalking = true;
                    OnWalkingStateChanged?.Invoke(true);
                }

                if (enableDebugLog)
                {
                    Debug.Log($"[AccelerometerWalk] Step detected! Count: {StepCount}, Magnitude: {magnitude:F2}");
                }
            }

            lastMagnitude = magnitude;

            // 一定時間歩行が検出されなければ停止
            if (isWalking && Time.time - lastStepTime > 1.0f)
            {
                isWalking = false;
                OnWalkingStateChanged?.Invoke(false);
            }
        }

        private void UpdateMovement()
        {
            if (!isWalking)
            {
                MoveDirection = Vector3.zero;
                return;
            }

            // 前進方向
            MoveDirection = transform.forward * moveSpeed;

            // 傾きによる方向転換
            // X軸の傾き = 左右の傾き
            float tilt = filteredAcceleration.x;
            TurnAngle = tilt * turnSensitivity;
        }

        /// <summary>
        /// 感度調整（設定画面から呼び出し）
        /// </summary>
        public void SetSensitivity(float sensitivity)
        {
            // 0.5 ~ 1.5 の範囲
            stepThreshold = 0.3f / sensitivity;
        }

        /// <summary>
        /// キャリブレーション（現在の姿勢を基準にする）
        /// </summary>
        public void Calibrate()
        {
            // 現在の加速度を基準値として記録
            filteredAcceleration = Input.acceleration;
        }

        /// <summary>
        /// 歩数リセット
        /// </summary>
        public void ResetStepCount()
        {
            StepCount = 0;
        }
    }
}
```

### 4.2 MetaversePlayerController との連携

```csharp
using UnityEngine;

namespace TapHouse.Pocket.Metaverse
{
    public class PocketMetaversePlayerController : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private CharacterController characterController;
        [SerializeField] private AccelerometerWalkController walkController;

        [Header("Settings")]
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float turnSpeed = 90f;

        private void Update()
        {
            // 加速度センサーからの入力で移動
            if (walkController.IsWalking)
            {
                // 前進
                Vector3 move = transform.forward * moveSpeed * Time.deltaTime;
                characterController.Move(move);

                // 方向転換
                float turn = walkController.TurnAngle * Time.deltaTime;
                transform.Rotate(0, turn, 0);
            }
        }
    }
}
```

---

## 5. 歩行検出アルゴリズム

### 5.1 ピーク検出法

```
┌─────────────────────────────────────────────────────────────┐
│                    ピーク検出アルゴリズム                    │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│   加速度                                                    │
│      ↑                                                      │
│      │     *       *       *                               │
│   閾値├─────────────────────────── threshold               │
│      │   *   *   *   *   *   *                             │
│      │  *     * *     * *     *                            │
│      │ *               *       *                           │
│      └─────────────────────────────→ 時間                  │
│            ↑       ↑       ↑                               │
│          Step1   Step2   Step3                             │
│                                                             │
│   【検出条件】                                               │
│   1. 加速度が閾値を超える                                   │
│   2. 前フレームより加速度が大きい（上昇中）                 │
│   3. 前フレームの加速度が閾値未満（谷から山へ）             │
│   4. 前回検出から一定時間経過（クールダウン）               │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 5.2 ノイズ対策

| 対策 | 実装 |
|------|------|
| ローパスフィルター | 高周波ノイズを除去 |
| クールダウン | 連続検出を防止（0.3秒） |
| 閾値調整 | 感度設定で調整可能 |
| 重力除去 | 線形加速度のみを使用 |

---

## 6. キャリブレーション

### 6.1 初期キャリブレーション

```
┌─────────────────────────────────────┐
│         キャリブレーション           │
├─────────────────────────────────────┤
│                                     │
│   ┌─────────────────────────────┐   │
│   │                             │   │
│   │      📱                     │   │
│   │      ↑                      │   │
│   │   スマホを楽な姿勢で        │   │
│   │   持ってください            │   │
│   │                             │   │
│   └─────────────────────────────┘   │
│                                     │
│   ┌─────────────────────────────┐   │
│   │        [準備OK]             │   │
│   └─────────────────────────────┘   │
│                                     │
│   ヒント: 座ったままでも          │
│   振ることで歩けます              │
│                                     │
└─────────────────────────────────────┘
```

### 6.2 キャリブレーション処理

```csharp
public class AccelerometerCalibrator : MonoBehaviour
{
    [SerializeField] private AccelerometerWalkController walkController;

    public void StartCalibration()
    {
        StartCoroutine(CalibrateCoroutine());
    }

    private IEnumerator CalibrateCoroutine()
    {
        // 3秒間の平均を取る
        List<Vector3> samples = new List<Vector3>();

        for (int i = 0; i < 30; i++)
        {
            samples.Add(Input.acceleration);
            yield return new WaitForSeconds(0.1f);
        }

        // 平均を計算
        Vector3 average = Vector3.zero;
        foreach (var sample in samples)
        {
            average += sample;
        }
        average /= samples.Count;

        // キャリブレーション適用
        walkController.Calibrate();
    }
}
```

---

## 7. 設定画面

### 7.1 感度設定

```
┌─────────────────────────────────────┐
│         歩行感度設定                 │
├─────────────────────────────────────┤
│                                     │
│   感度                              │
│   ┌─────────────────────────────┐   │
│   │ 低い ────────●────── 高い   │   │ ← スライダー
│   └─────────────────────────────┘   │
│                                     │
│   ┌─────────────────────────────┐   │
│   │   [テスト歩行]               │   │ ← テストボタン
│   │                             │   │
│   │   検出した歩数: 0           │   │
│   └─────────────────────────────┘   │
│                                     │
│   ヒント:                           │
│   - 検出されない場合は感度を上げる  │
│   - 誤検出が多い場合は感度を下げる  │
│                                     │
└─────────────────────────────────────┘
```

### 7.2 操作モード選択

```csharp
public enum WalkMode
{
    Walk,      // 実際に歩く
    Shake,     // 左右に振る
    Touch      // タッチ操作（フォールバック）
}
```

---

## 8. フォールバック

### 8.1 加速度センサーが使えない場合

```csharp
public class WalkInputManager : MonoBehaviour
{
    [SerializeField] private AccelerometerWalkController accelerometerWalk;
    [SerializeField] private TouchWalkController touchWalk;

    private void Start()
    {
        // 加速度センサーの利用可否チェック
        if (!SystemInfo.supportsAccelerometer)
        {
            // タッチ操作にフォールバック
            accelerometerWalk.enabled = false;
            touchWalk.enabled = true;

            ShowFallbackMessage();
        }
    }

    private void ShowFallbackMessage()
    {
        PocketToast.Show(
            "加速度センサーが利用できないため、タッチ操作に切り替えます",
            ToastType.Info
        );
    }
}
```

### 8.2 タッチ操作（フォールバック）

```csharp
public class TouchWalkController : MonoBehaviour
{
    public bool IsWalking { get; private set; }
    public Vector3 MoveDirection { get; private set; }

    private void Update()
    {
        if (Input.touchCount > 0 || Input.GetMouseButton(0))
        {
            IsWalking = true;

            // タッチ位置から方向を計算
            Vector2 touchPos = Input.touchCount > 0
                ? Input.GetTouch(0).position
                : (Vector2)Input.mousePosition;

            // 画面中央からの相対位置で方向決定
            Vector2 screenCenter = new Vector2(Screen.width / 2, Screen.height / 2);
            Vector2 direction = (touchPos - screenCenter).normalized;

            MoveDirection = new Vector3(direction.x, 0, direction.y);
        }
        else
        {
            IsWalking = false;
            MoveDirection = Vector3.zero;
        }
    }
}
```

---

## 9. 高齢者向け配慮

### 9.1 安全性

| 配慮 | 実装 |
|------|------|
| 座ったまま操作可能 | 振るモード対応 |
| 過度な動きを要求しない | 感度調整で対応 |
| 疲労警告 | 10分ごとに休憩を促す |
| 緊急停止 | 画面タップで即座に停止 |

### 9.2 疲労警告

```csharp
public class FatigueWarning : MonoBehaviour
{
    [SerializeField] private float warningIntervalMinutes = 10f;

    private float startTime;

    private void Start()
    {
        startTime = Time.time;
    }

    private void Update()
    {
        float elapsed = (Time.time - startTime) / 60f;

        if (elapsed >= warningIntervalMinutes)
        {
            ShowFatigueWarning();
            startTime = Time.time;
        }
    }

    private void ShowFatigueWarning()
    {
        PocketDialog.Show(
            "10分経ちました。少し休憩しませんか？",
            "続ける",
            "休憩する"
        );
    }
}
```

---

## 10. テストケース

| # | テスト内容 | 期待結果 |
|---|-----------|---------|
| 1 | 歩行検出 | 歩くとキャラクターが前進 |
| 2 | 振り検出 | 振るとキャラクターが前進 |
| 3 | 傾き検出 | 傾けると方向転換 |
| 4 | 感度調整 | 設定変更で検出感度が変化 |
| 5 | キャリブレーション | 姿勢リセット |
| 6 | フォールバック | センサーなしでタッチ操作 |
| 7 | 歩数カウント | 正確に歩数をカウント |
| 8 | 停止検出 | 動きが止まるとキャラクター停止 |

---

## 11. 新規ファイル一覧

| ファイル | 役割 |
|----------|------|
| `Assets/Scripts/Pocket/Metaverse/AccelerometerWalkController.cs` | 加速度歩行制御 |
| `Assets/Scripts/Pocket/Metaverse/AccelerometerCalibrator.cs` | キャリブレーション |
| `Assets/Scripts/Pocket/Metaverse/TouchWalkController.cs` | タッチフォールバック |
| `Assets/Scripts/Pocket/Metaverse/WalkInputManager.cs` | 入力管理 |
| `Assets/Scripts/Pocket/Metaverse/FatigueWarning.cs` | 疲労警告 |
