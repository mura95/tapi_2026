====================================
犬の育成システム - 拡張ガイド
====================================

最終更新: 2024-01-15

■ 目次

1. 概要
2. 性格（Personality）の追加方法
3. アクション（Action）の追加方法
4. パラメータ調整のコツ
5. よくある質問
6. トラブルシューティング

====================================

1. # 概要

このシステムでは、犬の性格とアクションを
ScriptableObject として追加できます。

■ 拡張のメリット

- コード変更不要
- Inspector で値を調整可能
- 何個でも追加可能
- バランス調整が簡単

■ 必要なファイル
Assets/Scripts/ - DogPersonalityData.cs - DogActionData.cs - DogActionEngine.cs - DogStateController.cs

Assets/Editor/ - DogAssetCreator.cs

==================================== 2. 性格（Personality）の追加方法
====================================

■ 基本手順

【手順 1】アセットを作成

1. Project ウィンドウで右クリック
2. Create > Dog > Personality
3. ファイル名を設定
   例: Personality_Playful（遊び好き）

【手順 2】基本情報を設定
▼ Inspector で設定

Personality Name:
└ 表示名（例: 遊び好き）

Description:
└ 説明文
└ 「よく遊びたがる、エネルギッシュな性格」

【手順 3】パラメータを設定

■ Love（愛情度）への影響

Love Increase Multiplier:
└ 愛情が上がる時の倍率
└ 1.0 = 標準
└ 1.5 = 1.5 倍（すぐ懐く）
└ 0.5 = 0.5 倍（なかなか懐かない）

Love Decrease Multiplier:
└ 愛情が下がる時の倍率
└ 1.0 = 標準
└ 1.5 = 1.5 倍（信頼を失いやすい）

■ Demand（要求度）への影響

Demand Increase Multiplier:
└ 要求度が上がる時の倍率
└ 1.0 = 標準
└ 1.4 = 1.4 倍（よく構ってほしがる）
└ 0.7 = 0.7 倍（手がかからない）

Demand Decrease Multiplier:
└ 要求度が下がる時の倍率
└ 1.0 = 標準
└ 1.2 = 1.2 倍（満足しやすい）

■ Hunger（空腹度）への影響

Hunger Progress Multiplier:
└ 空腹になる速度の倍率
└ 1.0 = 標準
└ 1.5 = 1.5 倍（すぐお腹が空く）
└ 0.8 = 0.8 倍（少食）

Feed Love Bonus:
└ 食事を与えた時の愛情ボーナス
└ 0 = ボーナスなし
└ 3 = +3（食いしん坊向け）

【手順 4】DogStateController に設定

1. Hierarchy で犬の GameObject を選択
2. Inspector > Dog State Controller
3. Personality 欄にドラッグ&ドロップ

【手順 5】テスト
Play モードで確認 - なでる → Love 上昇量を確認 - 時間経過 → Demand 上昇量を確認 - ごはん → Love 上昇量を確認

====================================
性格の設計例
====================================

■ 初心者向け（育てやすい）

◆ Friendly（人懐っこい）
Love Increase: 1.5
Love Decrease: 1.0
Demand Increase: 1.0
Demand Decrease: 1.0
Hunger Progress: 1.0
Feed Bonus: 0

特徴: すぐ懐く、標準的

◆ Calm（穏やか）
Love Increase: 1.0
Love Decrease: 0.8
Demand Increase: 0.7
Demand Decrease: 1.0
Hunger Progress: 0.9
Feed Bonus: 0

特徴: 手がかからない、落ち着いている

■ 中級者向け（特徴的）

◆ Active（活発）
Love Increase: 1.0
Love Decrease: 1.0
Demand Increase: 1.4
Demand Decrease: 1.2
Hunger Progress: 1.2
Feed Bonus: 0

特徴: よく遊びたがる、よく食べる

◆ Gluttonous（食いしん坊）
Love Increase: 1.0
Love Decrease: 1.0
Demand Increase: 1.0
Demand Decrease: 1.0
Hunger Progress: 1.5
Feed Bonus: 3

特徴: すぐお腹が空く、食事で大喜び

■ 上級者向け（難しい）

◆ Shy（恥ずかしがり）
Love Increase: 0.5
Love Decrease: 1.5
Demand Increase: 1.3
Demand Decrease: 0.8
Hunger Progress: 1.0
Feed Bonus: 0

特徴: なかなか懐かない、時間が必要

◆ Independent（独立心強い）
Love Increase: 0.8
Love Decrease: 0.7
Demand Increase: 0.6
Demand Decrease: 1.0
Hunger Progress: 1.0
Feed Bonus: 0

特徴: ツンデレ、一人でも平気

==================================== 3. アクション（Action）の追加方法
====================================

■ 基本手順

【手順 1】アセットを作成

1. Project ウィンドウで右クリック
2. Create > Dog > Action
3. ファイル名を設定
   例: Action_Brush（ブラッシング）

【手順 2】基本情報を設定

Action Name:
└ 表示名（例: ブラッシング）

Description:
└ 説明文
└ 「犬をブラッシングする。綺麗になって気持ちいい。」

【手順 3】基本効果を設定

▼ Base Effect

Love Change:
└ 愛情度の変化量
└ +2 = 愛情が 2 上がる
└ -1 = 愛情が 1 下がる

Demand Change:
└ 要求度の変化量
└ -20 = 要求度が 20 下がる（満足）
└ +10 = 要求度が 10 上がる

Feeds Hunger:
└ 空腹度を回復するか
└ true = 回復する（ごはん系）
└ false = 回復しない

Use Ideal Timing Bonus:
└ 理想的な時間ボーナスを使うか
└ true = 朝 8 時・夕方 18 時に+1 ボーナス
└ false = 使わない

Use Personality Feed Bonus:
└ 性格の食事ボーナスを使うか
└ true = 食いしん坊なら追加ボーナス
└ false = 使わない

【手順 4】性格による倍率を設定

Apply Personality To Love:
└ true = 性格の倍率を適用
└ false = 性格の影響を受けない

Apply Personality To Demand:
└ true = 性格の倍率を適用
└ false = 性格の影響を受けない

【手順 5】愛情レベルによる変動を設定

Scale With Love Level:
└ true = 愛情レベルで効果が変わる
└ false = 常に同じ効果

▼ true の場合

Love Low Multiplier:
└ 愛情 Low の時の倍率（0-33）
└ 例: 0.5（まだ慣れてないので効果薄い）

Love Medium Multiplier:
└ 愛情 Medium の時の倍率（34-66）
└ 例: 1.0（標準）

Love High Multiplier:
└ 愛情 High の時の倍率（67-100）
└ 例: 2.0（大好きなので効果大！）

【手順 6】DogStateController に設定

■ 既存のアクションを置き換える場合

1. Inspector > Dog State Controller
2. 該当の Action 欄にドラッグ

■ 新しいアクションを追加する場合

1. DogStateController.cs を開く
2. 以下を追加:

[SerializeField] private DogActionData actionBrush;

public void OnBrush()
{
\_actionEngine.ExecuteAction(actionBrush);
}

3. Inspector で設定

====================================
アクションの設計例
====================================

■ 基本アクション

◆ Pet（なでる）
Love: +1
Demand: -15
Feeds: false
Apply Personality: true/true
Scale With Love: true (0.5/1.0/2.0)

特徴: 基本的な愛情表現

◆ Feed（ごはん）
Love: +2
Demand: -20
Feeds: true
Ideal Timing: true
Personality Bonus: true
Apply Personality: true/true
Scale With Love: false

特徴: 空腹を満たす、時間ボーナスあり

■ 高度なアクション

◆ Play（遊ぶ）
Love: +3
Demand: -30
Feeds: false
Apply Personality: true/true
Scale With Love: true (0.3/1.0/1.5)

特徴: 大きく満足、愛情が低いと遊びたがらない

◆ Walk（散歩）
Love: +4
Demand: -40
Feeds: false
Apply Personality: true/true
Scale With Love: true (0.5/1.0/1.3)

特徴: 最も効果的、外に連れて行く

■ 特殊アクション

◆ Scold（叱る）
Love: -1
Demand: +5
Feeds: false
Apply Personality: true/true
Scale With Love: false

特徴: しつけ用、愛情が下がる

◆ Treat（高級おやつ）
Love: +5
Demand: -25
Feeds: false
Personality Bonus: true
Apply Personality: true/true
Scale With Love: true (0.8/1.0/1.5)

特徴: 特別なご褒美

==================================== 4. パラメータ調整のコツ
====================================

■ バランスの基準

▼ Love（愛情度）
標準的な上昇: 1-3
大きな上昇: 4-6
小さな減少: -1
大きな減少: -2 〜 -5

▼ Demand（要求度）
標準的な減少: -15 〜 -25
大きな減少: -30 〜 -50
標準的な上昇: +10 〜 +15

■ 性格の倍率の目安
すぐ懐く: 1.5 〜 2.0
標準: 1.0
なかなか懐かない: 0.5 〜 0.8
手がかからない: 0.6 〜 0.8
手がかかる: 1.3 〜 1.5

■ テストのポイント

1. 初期値から 10 分プレイ
2. Love/Demand の変化を記録
3. バランスが良いか確認
4. 調整 → 再テスト

==================================== 5. よくある質問
====================================

Q1: 新しい性格を追加したのに反映されない
A1: DogStateController の Personality 欄に
設定しましたか？設定後、Play モードで確認。

Q2: 倍率が効いていない気がする
A2: Apply Personality To Love/Demand が
true になっているか確認してください。

Q3: 複数の性格を組み合わせたい
A3: 現在のシステムでは 1 つのみ。
複数の性格を組み合わせた新しい性格を
作成してください。

Q4: アクションを増やしすぎるとバグる？
A4: ScriptableObject なので何個でも OK。
ただし、DogStateController に追加する場合は
コード編集が必要です。

Q5: 数値の意味がわからない
A5: Play モードでログを確認してください。
Console に詳細が表示されます。

==================================== 6. トラブルシューティング
====================================

■ 問題 1: Create > Dog が表示されない
原因: DogAssetCreator.cs が正しく配置されていない
解決: Assets/Editor/ フォルダに配置

■ 問題 2: 性格が反映されない
原因: DogStateController に設定していない
解決: Inspector で Personality に設定

■ 問題 3: コンパイルエラー
原因: 必要なファイルが不足
解決: すべてのファイルを確認

- DogPersonalityData.cs
- DogActionData.cs
- DogActionEngine.cs
- LoveManager.cs
- DemandManager.cs

■ 問題 4: 数値が変わらない
原因: Play モード中に変更した
解決: Play モードを停止してから変更

■ 問題 5: Hunger が動作しない
原因: HungerManager が別で動作している
解決: HungerManager は独立しています。
feedsHunger = true にしても、
HungerManager.IncreaseHungerState() を
呼ぶ必要があります。

====================================
関連ファイル
====================================

■ スクリプト
Assets/Scripts/ - DogPersonalityData.cs - DogActionData.cs - DogActionEngine.cs - DogStateController.cs - LoveManager.cs - DemandManager.cs - IDemandAdjuster.cs

■ エディタ拡張
Assets/Editor/ - DogAssetCreator.cs

■ ドキュメント
Assets/Documentation/ - README.txt - PersonalityGuide.txt（このファイル） - ActionGuide.txt

====================================
更新履歴
====================================

2024-01-15: 初版作成

- 基本手順を記載
- 設計例を追加
- FAQ を追加

====================================
