# 犬と遊ぼう（ミニゲーム）

## 1. 概要

犬と一緒に遊べるミニゲーム集。犬が直接参加するゲームで、高齢者が楽しみながら犬との絆を深める。

---

## 2. コンセプト

### 2.1 基本方針

```
┌─────────────────────────────────────────────────────────────┐
│                    犬と遊ぼう コンセプト                     │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│   「犬と一緒に遊んで、絆を深めよう！」                       │
│                                                             │
│   ┌─────────────────────────────────────────────────┐      │
│   │                                                 │      │
│   │   🎾 ボールを投げる → 🐕 犬がキャッチ！        │      │
│   │   👋 手を振る → 🐕 犬がジャンプ！              │      │
│   │   🦴 おやつを隠す → 🐕 犬が探す！              │      │
│   │                                                 │      │
│   │   犬のリアクション = ゲームの楽しさ             │      │
│   │                                                 │      │
│   └─────────────────────────────────────────────────┘      │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 2.2 ゲームの特徴

| 特徴 | 説明 |
|------|------|
| 犬が主役 | 犬のリアクションがゲームのフィードバック |
| シンプル操作 | タップ・スワイプのみ |
| 短時間プレイ | 1ゲーム1-3分 |
| 愛情度連動 | 遊ぶと愛情度がアップ |

---

## 3. ゲーム一覧

### 3.1 ゲーム選択画面

```
┌─────────────────────────────────────┐
│ ← 戻る      犬と遊ぼう              │
├─────────────────────────────────────┤
│                                     │
│         ┌───────────────┐           │
│         │      🐕       │           │
│         │   ワクワク！   │           │ ← 犬が待機中
│         └───────────────┘           │
│                                     │
│   ┌─────────────────────────────┐   │
│   │   🎾 ボールキャッチ         │   │
│   │   ボールを投げてキャッチ！   │   │ ← ゲーム1
│   └─────────────────────────────┘   │
│                                     │
│   ┌─────────────────────────────┐   │
│   │   🦴 おやつ探し             │   │
│   │   隠れたおやつを見つけよう！ │   │ ← ゲーム2
│   └─────────────────────────────┘   │
│                                     │
│   ┌─────────────────────────────┐   │
│   │   🐾 まねっこダンス         │   │
│   │   犬のポーズをまねしよう！   │   │ ← ゲーム3
│   └─────────────────────────────┘   │
│                                     │
└─────────────────────────────────────┘
```

### 3.2 ゲーム詳細

| ゲーム | 操作 | 犬の動き | 難易度 |
|--------|------|---------|--------|
| ボールキャッチ | スワイプで投げる | 走ってキャッチ | 簡単 |
| おやつ探し | タップで選ぶ | 探して見つける | 普通 |
| まねっこダンス | タイミングタップ | ポーズを見せる | 普通 |

---

## 4. ボールキャッチ

### 4.1 ゲーム概要

犬にボールを投げてキャッチさせるゲーム。スワイプの方向と強さでボールの軌道が変わる。

### 4.2 画面設計

```
┌─────────────────────────────────────┐
│ [やめる]   ボールキャッチ   スコア:5│
├─────────────────────────────────────┤
│                                     │
│                                     │
│                    ★               │ ← ボーナスゾーン
│                                     │
│              🎾                     │ ← 投げたボール
│                    ↗               │
│                                     │
│                      🐕             │ ← 犬（走って追いかける）
│   ════════════════════════════════  │ ← 地面ライン
│                                     │
│   ┌─────────────────────────────┐   │
│   │  🎾  残り: ●●●●●           │   │ ← ボール残数
│   └─────────────────────────────┘   │
│                                     │
│   ┌─────────────────────────────┐   │
│   │   ↑ スワイプで投げる        │   │ ← 操作ヒント
│   └─────────────────────────────┘   │
│                                     │
└─────────────────────────────────────┘
```

### 4.3 ゲームルール

| ルール | 内容 |
|--------|------|
| 投げ方 | 画面下から上にスワイプ |
| 判定 | 犬がボールに追いつけばキャッチ成功 |
| ボーナス | 星ゾーンを通過で追加ポイント |
| 残数 | ボール5個でゲーム終了 |

### 4.4 スコアリング

| アクション | スコア | 愛情度 |
|-----------|--------|--------|
| キャッチ成功 | +10点 | +1 |
| ボーナスキャッチ | +20点 | +2 |
| ジャンプキャッチ | +30点 | +3 |
| ミス | 0点 | 0 |

### 4.5 実装

```csharp
public class BallCatchGame : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DogController dog;
    [SerializeField] private GameObject ballPrefab;
    [SerializeField] private Transform throwPoint;

    [Header("Settings")]
    [SerializeField] private int maxBalls = 5;
    [SerializeField] private float minThrowForce = 5f;
    [SerializeField] private float maxThrowForce = 15f;

    private int remainingBalls;
    private int score;
    private GameObject currentBall;

    public void OnSwipe(Vector2 direction, float force)
    {
        if (remainingBalls <= 0 || currentBall != null) return;

        // ボールを生成
        currentBall = Instantiate(ballPrefab, throwPoint.position, Quaternion.identity);

        // 投げる力を計算
        float throwForce = Mathf.Lerp(minThrowForce, maxThrowForce, force);
        Vector3 throwDirection = new Vector3(direction.x, direction.y, 1f).normalized;

        // ボールを投げる
        var rb = currentBall.GetComponent<Rigidbody>();
        rb.AddForce(throwDirection * throwForce, ForceMode.Impulse);

        // 犬を追いかけさせる
        dog.ChaseTarget(currentBall.transform);

        remainingBalls--;
    }

    private void OnBallCaught(bool isBonus, bool isJump)
    {
        int points = 10;
        int love = 1;

        if (isBonus) { points = 20; love = 2; }
        if (isJump) { points = 30; love = 3; }

        score += points;
        LoveManager.Instance.AddLove(love);

        // 犬の喜びアニメーション
        dog.PlayHappyAnimation();

        // ボール削除
        Destroy(currentBall);
        currentBall = null;
    }
}
```

---

## 5. おやつ探し

### 5.1 ゲーム概要

3つのカップの中に隠れたおやつを犬と一緒に探すゲーム。カップがシャッフルされた後、おやつの位置を当てる。

### 5.2 画面設計

```
┌─────────────────────────────────────┐
│ [やめる]   おやつ探し     スコア:3  │
├─────────────────────────────────────┤
│                                     │
│         ┌───────────────┐           │
│         │      🐕       │           │
│         │   クンクン... │           │ ← 犬がヒントをくれる
│         └───────────────┘           │
│                                     │
│                                     │
│   ┌─────┐    ┌─────┐    ┌─────┐    │
│   │     │    │     │    │     │    │
│   │ 🥤  │    │ 🥤  │    │ 🥤  │    │ ← 3つのカップ
│   │     │    │     │    │     │    │
│   └─────┘    └─────┘    └─────┘    │
│                                     │
│   ┌─────────────────────────────┐   │
│   │  「犬が気にしているカップは？」│   │ ← ヒント
│   └─────────────────────────────┘   │
│                                     │
│   ラウンド: 3/5                     │ ← 進行状況
│                                     │
└─────────────────────────────────────┘
```

### 5.3 ゲームルール

| ルール | 内容 |
|--------|------|
| カップ数 | 3個（難易度で増加） |
| シャッフル | 3-5回（難易度で増加） |
| ヒント | 犬が正解に近いカップを見つめる |
| ラウンド | 5ラウンドで終了 |

### 5.4 犬のヒント動作

| 犬の動作 | 意味 |
|---------|------|
| カップを見つめる | そのカップが怪しい |
| 尻尾を振る | 正解に近い |
| 鼻を鳴らす | 正解のカップ |

### 5.5 実装

```csharp
public class TreatSearchGame : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DogController dog;
    [SerializeField] private Cup[] cups;

    [Header("Settings")]
    [SerializeField] private int totalRounds = 5;
    [SerializeField] private float shuffleSpeed = 0.5f;

    private int currentRound;
    private int correctCupIndex;
    private int score;

    public async UniTask StartRound()
    {
        // おやつを隠す
        correctCupIndex = Random.Range(0, cups.Length);
        cups[correctCupIndex].HideTreat();

        // シャッフルアニメーション
        await ShuffleCups();

        // 犬にヒントを出させる
        dog.LookAt(cups[correctCupIndex].transform);
        dog.WagTail();
    }

    public void OnCupSelected(int index)
    {
        bool isCorrect = index == correctCupIndex;

        // カップを開ける
        cups[index].Open();

        if (isCorrect)
        {
            // 正解
            score++;
            dog.PlayHappyAnimation();
            dog.EatTreat();
            LoveManager.Instance.AddLove(2);
        }
        else
        {
            // 不正解
            cups[correctCupIndex].Open(); // 正解を見せる
            dog.PlaySadAnimation();
        }

        currentRound++;
        if (currentRound >= totalRounds)
        {
            EndGame();
        }
        else
        {
            _ = StartRound();
        }
    }
}
```

---

## 6. まねっこダンス

### 6.1 ゲーム概要

犬がポーズを見せるので、タイミングよくボタンを押してまねするリズムゲーム。

### 6.2 画面設計

```
┌─────────────────────────────────────┐
│ [やめる]   まねっこダンス  連続:5   │
├─────────────────────────────────────┤
│                                     │
│         ┌───────────────┐           │
│         │               │           │
│         │      🐕       │           │ ← 犬がポーズ
│         │   「お手！」   │           │
│         │               │           │
│         └───────────────┘           │
│                                     │
│   次のポーズ:                       │
│   ┌─────┬─────┬─────┬─────┐        │
│   │ お手 │おかわり│ ふせ │ジャンプ│        │ ← 次のポーズ表示
│   │  ↓  │     │     │     │        │
│   └─────┴─────┴─────┴─────┘        │
│                                     │
│   ┌─────────────────────────────┐   │
│   │                             │   │
│   │  ┌─────┐  ┌─────┐          │   │
│   │  │ 🐾  │  │ 🐾  │          │   │ ← タップボタン
│   │  │左手  │  │右手  │          │   │
│   │  └─────┘  └─────┘          │   │
│   │                             │   │
│   └─────────────────────────────┘   │
│                                     │
└─────────────────────────────────────┘
```

### 6.3 ゲームルール

| ルール | 内容 |
|--------|------|
| ポーズ種類 | お手、おかわり、ふせ、ジャンプ |
| タイミング | 犬がポーズした瞬間にタップ |
| 判定 | Perfect / Good / Miss |
| 連続ボーナス | 3連続成功でボーナス |

### 6.4 ポーズと対応ボタン

| ポーズ | ボタン |
|--------|--------|
| お手 | 左手ボタン |
| おかわり | 右手ボタン |
| ふせ | 両方同時 |
| ジャンプ | 上スワイプ |

---

## 7. 愛情度・スコア連携

### 7.1 ゲーム結果と愛情度

| ゲーム | 最大愛情度 | 条件 |
|--------|-----------|------|
| ボールキャッチ | +15 | 全キャッチ成功 |
| おやつ探し | +10 | 全問正解 |
| まねっこダンス | +10 | 全Perfect |

### 7.2 Firebase保存データ

```
users/{userId}/miniGames/
├── ballCatch/
│   ├── highScore: 150
│   ├── totalPlays: 25
│   └── lastPlayed: timestamp
├── treatSearch/
│   ├── highScore: 5
│   ├── totalPlays: 18
│   └── lastPlayed: timestamp
└── danceMimicry/
    ├── highScore: 30
    ├── bestCombo: 12
    ├── totalPlays: 10
    └── lastPlayed: timestamp
```

---

## 8. 高齢者向け配慮

### 8.1 操作配慮

| 配慮 | 実装 |
|------|------|
| 大きなボタン | 100dp以上 |
| 寛容な判定 | タイミング判定を緩く |
| 繰り返しプレイ | 何度でも遊べる |
| ゆっくりペース | スピード調整可能 |

### 8.2 視覚配慮

| 配慮 | 実装 |
|------|------|
| 高コントラスト | 背景と対象物を明確に |
| 大きな文字 | スコア表示など |
| 動きの予告 | 次のアクションを表示 |

---

## 9. テストケース

| # | テスト内容 | 期待結果 |
|---|-----------|---------|
| 1 | ゲーム選択画面表示 | 3つのゲームが表示 |
| 2 | ボールキャッチ開始 | ボールが投げられる |
| 3 | 犬がキャッチ成功 | スコア加算、愛情度アップ |
| 4 | おやつ探し正解 | 犬が喜ぶ |
| 5 | まねっこダンスPerfect | 連続カウント増加 |
| 6 | ゲーム終了 | スコア保存、Firebase同期 |

---

## 10. 新規ファイル一覧

| ファイル | 役割 |
|----------|------|
| `Assets/Scripts/Pocket/MiniGames/MiniGameManager.cs` | ゲーム管理 |
| `Assets/Scripts/Pocket/MiniGames/BallCatchGame.cs` | ボールキャッチ |
| `Assets/Scripts/Pocket/MiniGames/TreatSearchGame.cs` | おやつ探し |
| `Assets/Scripts/Pocket/MiniGames/DanceMimicryGame.cs` | まねっこダンス |
| `Assets/Scripts/Pocket/MiniGames/MiniGameScoreManager.cs` | スコア管理 |
| `Assets/Scripts/Pocket/MiniGames/UI/GameSelectUI.cs` | ゲーム選択UI |
| `Assets/Scripts/Pocket/MiniGames/UI/GameResultUI.cs` | 結果UI |
