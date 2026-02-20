# メタバース散歩 マルチプレイヤー Editorセットアップ手順

## 前提条件

- Photon Fusion 2 SDKがインポート済み（`Assets/Photon/` に存在）
- Photon DashboardでFusionアプリを作成し、App IDを取得済み
- `PhotonAppSettings.asset` にApp IDとFixed Region (`jp`) を設定済み

上記が未完了の場合は [metaverse_walk_network.md](./metaverse_walk_network.md) のセクション4を参照。

---

## 現在のシーン構成（変更前）

```
Metaverse (Scene)
├── MetaverseManager          ... MetaverseManager スクリプト
├── MetaverseCamera           ... MetaverseCamera スクリプト、MainCameraタグ
├── shiba_null                ... 犬モデル（Prefabインスタンス）+ MetaverseDogController
├── Player                    ... カプセルメッシュ + NavMeshAgent + MetaversePlayerFollower
├── DogSpawnPoint             ... Position (0, 0, 0)
├── PlayerSpawnPoint          ... Position (0, 0, -1.5)
├── Directional Light
├── Ground_01                 ... 地面 + NavMeshSurface
├── 環境オブジェクト各種      ... 木、岩、花など
├── EventSystem
├── MetaverseCanvas
│   ├── HUD                   ... MetaverseHUD スクリプト
│   │   ├── MovementButtons   ... MovementButtonUI スクリプト
│   │   │   ├── LeftButton
│   │   │   ├── RightButton
│   │   │   └── ForwardButton
│   │   └── ExitButton        ... ExitWalkButton スクリプト
│   ├── PlayerCountPanel
│   ├── ConnectionStatusPanel
│   ├── DebugTextPanel
│   └── ConfirmDialog
└── （その他環境オブジェクト）
```

---

## ステップ3: NetworkDog Prefabの作成

### 3-1. Prefabを複製する

1. **Project** ウィンドウを開く
2. 以下のパスに移動する:
   ```
   Assets/Resources/MetaverseWalk/
   ```
3. `shiba_null` を**右クリック** → **Duplicate**
4. 複製されたファイルの名前を **`NetworkDog`** に変更する

> 既に `NetworkDog.prefab` が存在している場合は、既存のものを使ってください。
> その場合は上書きせず、次の「3-2」に進んでください。

### 3-2. Prefab編集モードに入る

1. `NetworkDog` を**ダブルクリック**する
   - ヒエラルキーが Prefab 編集モードに切り替わる
   - 画面上部に「◀ NetworkDog」のようなパンくずリストが表示される

### 3-3. NetworkObject を追加する

1. ヒエラルキーで **NetworkDog（ルート）** を選択する
2. Inspector の一番下にある **Add Component** ボタンをクリック
3. 検索欄に `NetworkObject` と入力する
4. **Fusion > NetworkObject** を選択して追加する

```
┌─────────────────────────────────────────┐
│ Inspector - NetworkDog                  │
├─────────────────────────────────────────┤
│ ☑ NetworkObject                         │
│   Flags: (デフォルトのまま)              │
│                                         │
│ [Add Component]                         │
└─────────────────────────────────────────┘
```

### 3-4. NetworkTransform を追加する

1. 再び **Add Component** をクリック
2. 検索欄に `NetworkTransform` と入力
3. **Fusion > NetworkTransform** を選択して追加
4. 追加された NetworkTransform の設定を以下のように変更:

| 項目 | 設定値 |
|------|--------|
| Sync Position | ☑ ON |
| Sync Rotation | ☑ ON |
| Sync Scale | ☐ OFF（デフォルト） |
| Position Accuracy | `0.01` |
| Rotation Accuracy | `1` |

```
┌─────────────────────────────────────────┐
│ ☑ NetworkTransform                      │
│   Sync Position: ☑                      │
│   Sync Rotation: ☑                      │
│   Position Accuracy: 0.01               │
│   Rotation Accuracy: 1                  │
└─────────────────────────────────────────┘
```

### 3-5. NetworkDogController を追加する

1. **Add Component** をクリック
2. 検索欄に `NetworkDogController` と入力
3. **TapHouse > MetaverseWalk > Network > NetworkDogController** を選択
4. `Animator` フィールドを設定する:
   - ヒエラルキーで犬モデルの子オブジェクト（Animatorが付いている階層）を探す
   - そのAnimatorコンポーネントを `Animator` フィールドにドラッグ&ドロップ

```
┌─────────────────────────────────────────┐
│ ☑ NetworkDogController                  │
│   Animator: [犬のAnimatorをドラッグ]     │
└─────────────────────────────────────────┘
```

> **確認:** この時点で `MetaverseDogController` が既にアタッチされていることを確認。
> 元の `shiba_null` から複製しているので、通常は付いています。

### 3-6. Prefab編集モードを閉じる

1. ヒエラルキー上部の **◀** 矢印をクリックして Prefab 編集モードを終了する
2. 保存ダイアログが出たら **Save** をクリック

### 3-7. 完成後の NetworkDog Prefab 構成

```
NetworkDog (Prefab)
├── NetworkObject          ... ← 新規追加
├── NetworkTransform       ... ← 新規追加
├── NetworkDogController   ... ← 新規追加
├── MetaverseDogController ... （元から存在）
├── Animator               ... （元から存在）
├── Rigidbody              ... （元から存在）
├── Collider               ... （元から存在）
└── 犬モデルの子オブジェクト群
```

---

## ステップ4: NetworkPlayer Prefabの作成

### 4-1. シーン内の Player を Prefab化する

1. `Assets/Scenes/Metaverse.unity` を開く
2. ヒエラルキーで **Player** を選択する
3. **Player** を **Project** ウィンドウの以下のフォルダにドラッグ&ドロップ:
   ```
   Assets/Resources/MetaverseWalk/
   ```
4. ダイアログが出たら **Original Prefab** を選択
5. 作成されたPrefabの名前を **`NetworkPlayer`** に変更する

> 既に `NetworkPlayer.prefab` が存在している場合は、既存のものに上書きしてよいか確認してから進めてください。

### 4-2. Prefab編集モードに入る

1. `NetworkPlayer` を**ダブルクリック**してPrefab編集モードに入る

### 4-3. NetworkObject を追加する

1. ルートの **NetworkPlayer** を選択
2. **Add Component** → `NetworkObject` を追加（ステップ3-3と同じ手順）

### 4-4. NetworkTransform を追加する

1. **Add Component** → `NetworkTransform` を追加
2. 設定:

| 項目 | 設定値 |
|------|--------|
| Position Accuracy | `0.01` |
| Rotation Accuracy | `1` |

### 4-5. NetworkPlayerController を追加する

1. **Add Component** → 検索 `NetworkPlayerController`
2. **TapHouse > MetaverseWalk > Network > NetworkPlayerController** を選択
3. 設定（後で名前タグを作った後に再設定する箇所あり）:

| フィールド | 現時点の設定 |
|-----------|-------------|
| Animator | なし（Playerにアニメーターがあればドラッグ） |
| Name Tag | なし（次のステップで作成後に設定） |

### 4-6. 名前タグUIを作成する

プレイヤーの頭上に他ユーザーの名前を表示するUIを作ります。

#### 4-6-1. Canvas を作成

1. ヒエラルキーで **NetworkPlayer（ルート）** を**右クリック**
2. **UI** → **Canvas** を選択
3. 作成されたCanvasの名前を **`NameTagCanvas`** に変更

#### 4-6-2. Canvas のコンポーネントを設定

NameTagCanvas を選択して Inspector を設定:

**Canvas コンポーネント:**

| 項目 | 設定値 |
|------|--------|
| Render Mode | **World Space** |

> Render Mode を World Space に変更すると、RectTransform が手動設定可能になります。

**RectTransform:**

| 項目 | 設定値 |
|------|--------|
| Pos X | `0` |
| Pos Y | `2` |
| Pos Z | `0` |
| Width | `200` |
| Height | `50` |
| Scale X | `0.01` |
| Scale Y | `0.01` |
| Scale Z | `0.01` |

```
┌─────────────────────────────────────────┐
│ RectTransform                           │
│   Pos X: 0   Pos Y: 2   Pos Z: 0      │
│   Width: 200  Height: 50               │
│   Scale: 0.01, 0.01, 0.01              │
└─────────────────────────────────────────┘
```

**Canvas Scaler** は削除してOK（World Spaceでは不要）。

#### 4-6-3. テキストを追加

1. **NameTagCanvas** を**右クリック**
2. **UI** → **Text - TextMeshPro** を選択
3. 「TMP Importer」ダイアログが出たら **Import TMP Essentials** をクリック（初回のみ）
4. 作成された TextMeshPro オブジェクトの名前を **`NameText`** に変更

#### 4-6-4. NameText の設定

NameText を選択して Inspector を設定:

**RectTransform:**

| 項目 | 設定値 |
|------|--------|
| Anchor | Stretch-Stretch (四隅いっぱい) |
| Left / Right / Top / Bottom | すべて `0` |

**TextMeshPro - Text (UI) コンポーネント:**

| 項目 | 設定値 |
|------|--------|
| Text | `Player` （テスト用。実行時に自動設定される） |
| Font Size | `36` |
| Alignment | 中央揃え（水平: Center、垂直: Middle） |
| Color | 白 (`#FFFFFF`) |

**Outline の追加（見やすくするため）:**

1. NameText の Inspector で **Material Preset** の横にある設定アイコンをクリック
2. または TextMeshPro コンポーネントの下部にある **Outline** セクションを展開
3. 設定:

| 項目 | 設定値 |
|------|--------|
| Outline Color | 黒 (`#000000`) |
| Outline Thickness | `0.2` |

#### 4-6-5. PlayerNameTag スクリプトを追加

1. **NameTagCanvas** を選択
2. **Add Component** → 検索 `PlayerNameTag`
3. **TapHouse > MetaverseWalk > Network > PlayerNameTag** を選択
4. Inspector で設定:

| フィールド | 設定方法 |
|-----------|---------|
| Canvas | `NameTagCanvas` 自身をドラッグ |
| Name Text | 子オブジェクトの `NameText` をドラッグ |
| Max Display Distance | `15` |

```
┌─────────────────────────────────────────┐
│ ☑ PlayerNameTag                         │
│   Canvas: NameTagCanvas                 │
│   Name Text: NameText                   │
│   Max Display Distance: 15              │
└─────────────────────────────────────────┘
```

### 4-7. NetworkPlayerController の設定を完了する

1. ヒエラルキーで **NetworkPlayer（ルート）** を選択
2. NetworkPlayerController コンポーネントの `Name Tag` フィールドに、先ほど作成した **NameTagCanvas の PlayerNameTag** をドラッグ

```
┌─────────────────────────────────────────┐
│ ☑ NetworkPlayerController               │
│   Animator: (あればドラッグ)             │
│   Name Tag: NameTagCanvas > PlayerNameTag│
└─────────────────────────────────────────┘
```

### 4-8. Prefab編集モードを閉じる

1. **◀** 矢印をクリックして Prefab 編集モードを終了
2. **Save** をクリック

### 4-9. 完成後の NetworkPlayer Prefab 構成

```
NetworkPlayer (Prefab)
├── NetworkObject              ... ← 新規追加
├── NetworkTransform           ... ← 新規追加
├── NetworkPlayerController    ... ← 新規追加
├── MetaversePlayerFollower    ... （元から存在）
├── NavMeshAgent               ... （元から存在）
├── MeshRenderer               ... （元から存在、カプセル）
├── MeshFilter                 ... （元から存在）
└── NameTagCanvas              ... ← 新規追加（子オブジェクト）
    └── NameText               ... ← 新規追加（TextMeshPro）
```

---

## ステップ5: Prefabを Fusion に登録する

Photon Fusion は、ネットワークでSpawnするPrefabを事前に登録する必要があります。

1. **Project** ウィンドウで検索欄に `NetworkProjectConfig` と入力
   - パス: `Assets/Photon/Fusion/Resources/NetworkProjectConfig.asset`
2. クリックして Inspector に表示する
3. Inspector 内の **Prefab Table** セクションを探す
4. **Rebuild Object Table** ボタンをクリック

```
┌─────────────────────────────────────────┐
│ NetworkProjectConfig                    │
│                                         │
│ Prefab Table                            │
│   [Rebuild Object Table]  ← クリック   │
│                                         │
│   登録済みPrefab:                       │
│     - NetworkDog          ← 確認       │
│     - NetworkPlayer       ← 確認       │
│     - (その他のFusion Prefab...)        │
└─────────────────────────────────────────┘
```

> **確認:** リストに `NetworkDog` と `NetworkPlayer` が含まれていることを確認してください。
> もし表示されない場合は、Prefabのルートに `NetworkObject` が正しく追加されているか確認してください。

---

## ステップ6: NetworkRunner Prefab を作成する

PhotonNetworkManager がネットワーク接続時に使う NetworkRunner の Prefab を作ります。

### 6-1. Metaverseシーンを開く

1. `Assets/Scenes/Metaverse.unity` を開く（既に開いていればそのまま）

### 6-2. 空のGameObjectを作成する

1. ヒエラルキーの空白を**右クリック** → **Create Empty**
2. 名前を **`NetworkRunnerPrefab`** に変更
3. Position を `(0, 0, 0)` に設定

### 6-3. NetworkRunner コンポーネントを追加する

1. **NetworkRunnerPrefab** を選択
2. **Add Component** → 検索 `NetworkRunner`
3. **Fusion > NetworkRunner** を選択

```
┌─────────────────────────────────────────┐
│ ☑ NetworkRunner                         │
│   (設定はデフォルトのままでOK)           │
└─────────────────────────────────────────┘
```

### 6-4. Prefab化する

1. ヒエラルキーの **NetworkRunnerPrefab** を Project ウィンドウの以下にドラッグ:
   ```
   Assets/Resources/MetaverseWalk/
   ```
2. Prefabが作成されたことを確認

### 6-5. シーンから削除する

1. ヒエラルキーの **NetworkRunnerPrefab** を**右クリック** → **Delete**

> Prefabとして保存されたので、シーン上には不要です。

---

## ステップ7: シーンに PhotonNetworkManager を配置する

### 7-1. 空のGameObjectを作成する

1. ヒエラルキーの空白を**右クリック** → **Create Empty**
2. 名前を **`PhotonNetworkManager`** に変更

### 7-2. スクリプトを追加する

1. **Add Component** → 検索 `PhotonNetworkManager`
2. **TapHouse > MetaverseWalk > Network > PhotonNetworkManager** を選択

### 7-3. Inspector を設定する

| フィールド | 設定方法 |
|-----------|---------|
| Runner Prefab | Projectから `Assets/Resources/MetaverseWalk/NetworkRunnerPrefab` をドラッグ |
| Network Dog Prefab | Projectから `Assets/Resources/MetaverseWalk/NetworkDog` をドラッグ |
| Network Player Prefab | Projectから `Assets/Resources/MetaverseWalk/NetworkPlayer` をドラッグ |
| Max Players Per Room | `10` |
| Spawn Points | 下のステップ8で設定 |
| Debug Mode | `☑ ON`（開発中はON推奨） |

```
┌─────────────────────────────────────────────────┐
│ ☑ PhotonNetworkManager                          │
│                                                  │
│ Photon設定                                       │
│   Runner Prefab: NetworkRunnerPrefab             │
│   Network Dog Prefab: NetworkDog                 │
│   Network Player Prefab: NetworkPlayer           │
│   Max Players Per Room: 10                       │
│                                                  │
│ スポーン                                         │
│   Spawn Points:                                  │
│     Size: 0  ← (ステップ8で設定)                 │
│                                                  │
│ デバッグ                                         │
│   Debug Mode: ☑                                  │
└─────────────────────────────────────────────────┘
```

---

## ステップ8: スポーンポイントを作成する

複数プレイヤーが同じ位置にSpawnしないよう、複数のスポーンポイントを配置します。

### 8-1. スポーンポイントを5つ作成する

1. ヒエラルキーで**右クリック** → **Create Empty** を5回繰り返す
2. 以下の名前と位置に設定:

| 名前 | Position X | Position Y | Position Z |
|------|-----------|-----------|-----------|
| `SpawnPoint_01` | `0` | `0` | `0` |
| `SpawnPoint_02` | `5` | `0` | `0` |
| `SpawnPoint_03` | `-5` | `0` | `0` |
| `SpawnPoint_04` | `0` | `0` | `5` |
| `SpawnPoint_05` | `0` | `0` | `-5` |

> **ヒント:** 位置はマップの歩ける範囲内に配置してください。
> 上記はデフォルト値です。実際のマップに合わせて調整してください。

### 8-2. PhotonNetworkManager に登録する

1. ヒエラルキーで **PhotonNetworkManager** を選択
2. Inspector の `Spawn Points` の **Size** を `5` に変更
3. 各要素にスポーンポイントをドラッグ:

```
┌─────────────────────────────────────────┐
│ Spawn Points                            │
│   Size: 5                               │
│   Element 0: SpawnPoint_01              │
│   Element 1: SpawnPoint_02              │
│   Element 2: SpawnPoint_03              │
│   Element 3: SpawnPoint_04              │
│   Element 4: SpawnPoint_05              │
└─────────────────────────────────────────┘
```

> **ヒント:** 複数のオブジェクトを一度にドラッグすることもできます。
> ヒエラルキーで SpawnPoint_01〜05 を Shift+クリックで全選択し、
> Inspector の Spawn Points 配列にまとめてドラッグします。

---

## ステップ9: シーンに NetworkSpawnManager を配置する

### 9-1. 空のGameObjectを作成する

1. ヒエラルキーで**右クリック** → **Create Empty**
2. 名前を **`NetworkSpawnManager`** に変更

### 9-2. スクリプトを追加する

1. **Add Component** → 検索 `NetworkSpawnManager`
2. **TapHouse > MetaverseWalk > Network > NetworkSpawnManager** を選択

### 9-3. Inspector を設定する

| フィールド | 設定方法 |
|-----------|---------|
| Movement Button UI | ヒエラルキーから `MetaverseCanvas > HUD > MovementButtons` をドラッグ |
| Metaverse Camera | ヒエラルキーから `MetaverseCamera` をドラッグ |
| Debug Mode | `☑ ON` |

```
┌─────────────────────────────────────────────────┐
│ ☑ NetworkSpawnManager                            │
│                                                  │
│ UI参照                                           │
│   Movement Button UI: MovementButtons            │
│                                                  │
│ カメラ参照                                       │
│   Metaverse Camera: MetaverseCamera              │
│                                                  │
│ デバッグ                                         │
│   Debug Mode: ☑                                  │
└─────────────────────────────────────────────────┘
```

> **注意:** `MovementButtons` にドラッグするとき、そのGameObjectに
> `MovementButtonUI` スクリプトが付いていることを確認してください。
> Inspector に「MovementButtonUI」コンポーネントが表示されていればOKです。

---

## ステップ10: 既存オブジェクトの調整

マルチプレイヤーモードでは犬とプレイヤーがネットワーク経由でSpawnされるため、
シーンに直接配置されている既存のオブジェクトを調整します。

### 10-1. 既存の犬を非アクティブにする

1. ヒエラルキーで **shiba_null** を選択
2. Inspector 左上の**チェックボックスを外す**（非アクティブ化）

```
┌─────────────────────────────────────────┐
│ □ shiba_null          ← チェックを外す │
│   Transform                             │
│   ...                                   │
└─────────────────────────────────────────┘
```

### 10-2. 既存のPlayerを非アクティブにする

1. ヒエラルキーで **Player** を選択
2. Inspector 左上の**チェックボックスを外す**

```
┌─────────────────────────────────────────┐
│ □ Player              ← チェックを外す │
│   Transform                             │
│   ...                                   │
└─────────────────────────────────────────┘
```

> **理由:** マルチプレイヤーモードでは `PhotonNetworkManager` が
> `NetworkDog` と `NetworkPlayer` のPrefabを動的にSpawnします。
> シーンに直接配置された犬とプレイヤーは不要になります。

### 10-3. MetaverseManager の設定を確認する

1. ヒエラルキーで **MetaverseManager** を選択
2. Inspector で以下を確認:

| フィールド | 設定値 |
|-----------|--------|
| Enable Multiplayer | `☑ ON` |
| Dog Spawn Point | DogSpawnPoint（そのまま） |
| Player Spawn Point | PlayerSpawnPoint（そのまま） |
| Debug Mode | `☑ ON`（開発中） |

```
┌─────────────────────────────────────────────────┐
│ ☑ MetaverseManager                               │
│                                                  │
│ シーン設定                                       │
│   Main Scene Name: main                          │
│                                                  │
│ スポーン設定                                     │
│   Dog Spawn Point: DogSpawnPoint                 │
│   Player Spawn Point: PlayerSpawnPoint           │
│                                                  │
│ マルチプレイヤー                                 │
│   Enable Multiplayer: ☑  ← ONであること確認     │
│                                                  │
│ デバッグ                                         │
│   Debug Mode: ☑                                  │
└─────────────────────────────────────────────────┘
```

### 10-4. MetaverseCamera の参照をクリアする

マルチプレイヤーモードではカメラのターゲットは実行時に自動設定されます。

1. ヒエラルキーで **MetaverseCamera** を選択
2. MetaverseCamera スクリプトの以下のフィールドを **None** に設定:
   - `Dog`: None（フィールドの値を右クリック → Delete、またはフィールド右の ◎ → None）
   - `Player`: None

```
┌─────────────────────────────────────────┐
│ ☑ MetaverseCamera                       │
│   Dog: None (Transform)     ← 空にする │
│   Player: None (Transform)  ← 空にする │
│   Distance: 15                          │
│   Height: 10                            │
│   Pitch Angle: 45                       │
│   Yaw Angle: 45                         │
│   Smooth Speed: 5                       │
└─────────────────────────────────────────┘
```

> **理由:** `NetworkSpawnManager` が実行時に `SetTargets()` を呼び出して
> ローカルプレイヤーの犬とプレイヤーを自動設定します。

### 10-5. MovementButtons の犬参照をクリアする

1. ヒエラルキーで `MetaverseCanvas > HUD > MovementButtons` を選択
2. `MovementButtonUI` コンポーネントの `Dog Controller` を **None** に設定

```
┌─────────────────────────────────────────┐
│ ☑ MovementButtonUI                      │
│   Dog Controller: None      ← 空にする │
│   Forward Button: ForwardButton         │
│   Left Button: LeftButton               │
│   Right Button: RightButton             │
└─────────────────────────────────────────┘
```

> **理由:** `NetworkSpawnManager` が実行時に `SetDogController()` を呼び出して
> ローカルでSpawnされた犬のコントローラーを自動設定します。

---

## ステップ11: シーンを保存する

1. **Ctrl + S** でシーンを保存
2. 保存後のヒエラルキー確認:

```
Metaverse (Scene)
├── MetaverseManager          ... enableMultiplayer: ☑
├── PhotonNetworkManager      ... ← 新規追加
├── NetworkSpawnManager       ... ← 新規追加
├── MetaverseCamera           ... Dog: None, Player: None
├── □ shiba_null              ... ← 非アクティブ
├── □ Player                  ... ← 非アクティブ
├── DogSpawnPoint
├── PlayerSpawnPoint
├── SpawnPoint_01             ... ← 新規追加
├── SpawnPoint_02             ... ← 新規追加
├── SpawnPoint_03             ... ← 新規追加
├── SpawnPoint_04             ... ← 新規追加
├── SpawnPoint_05             ... ← 新規追加
├── Directional Light
├── Ground_01
├── 環境オブジェクト各種
├── EventSystem
└── MetaverseCanvas
    ├── HUD (MetaverseHUD)
    │   ├── MovementButtons   ... Dog Controller: None
    │   │   ├── LeftButton
    │   │   ├── RightButton
    │   │   └── ForwardButton
    │   └── ExitButton
    ├── PlayerCountPanel
    ├── ConnectionStatusPanel
    ├── DebugTextPanel
    └── ConfirmDialog
```

---

## ステップ12: 動作テスト

### 12-1. シングルプレイヤーテスト（まず動作確認）

Photon接続なしでも動作することを確認します。

1. MetaverseManager の **Enable Multiplayer** を一時的に `☐ OFF`
2. `shiba_null` と `Player` を一時的に**再アクティブ化**（チェックを入れる）
3. MetaverseCamera の `Dog` に `shiba_null`、`Player` に `Player` を再設定
4. MovementButtons の `Dog Controller` に `shiba_null` の MetaverseDogController を再設定
5. **▶ Play** ボタンを押す
6. 移動ボタンで犬が動き、プレイヤーが追従し、カメラが追いかけることを確認
7. **▶ Play** を停止
8. 上記の一時変更を元に戻す:
   - Enable Multiplayer: `☑ ON`
   - shiba_null / Player: 非アクティブ
   - Camera / MovementButtons: None

### 12-2. マルチプレイヤーテスト（Photon接続）

1. **▶ Play** ボタンを押す
2. Console ウィンドウ（Window → General → Console）で以下のログを確認:
   ```
   [MetaverseManager] Connecting to Photon (multiplayer mode)...
   [PhotonNetworkManager] Connected to room: WalkRoom_XXX
   [NetworkSpawnManager] Player follower linked to dog
   [NetworkSpawnManager] Camera targets set
   [NetworkSpawnManager] Movement UI linked to dog controller
   [NetworkSpawnManager] Local entities setup complete
   ```
3. 犬とプレイヤーがSpawnされていることを確認
4. 移動ボタンで犬を操作できることを確認
5. HUD に「1人」「接続中」が表示されることを確認

### 12-3. トラブルシューティング

| 症状 | 確認すること |
|------|-------------|
| 犬が表示されない | PhotonNetworkManager の Network Dog Prefab が設定されているか |
| プレイヤーが表示されない | PhotonNetworkManager の Network Player Prefab が設定されているか |
| 移動ボタンが反応しない | NetworkSpawnManager の Movement Button UI が設定されているか |
| カメラが動かない | NetworkSpawnManager の Metaverse Camera が設定されているか |
| 「接続失敗」と表示される | PhotonAppSettings の App ID が正しいか / インターネット接続があるか |
| Console に赤エラー | エラーメッセージの内容を確認して対処 |

### 12-4. 2クライアントテスト（ParrelSync推奨）

2つのUnity Editorインスタンスで同時テストします。

1. **ParrelSync** をインストール:
   - Window → Package Manager → **+** → Add package from git URL
   - URL: `https://github.com/VeriorPies/ParrelSync.git?path=/ParrelSync`
   - **Add** をクリック
2. メニュー: **ParrelSync** → **Clones Manager**
3. **Create New Clone** をクリック（初回は数分かかる）
4. **Open in New Editor** をクリック
5. 両方のEditorで Metaverse シーンを開いて **▶ Play**
6. 確認項目:
   - 両方のEditorで犬とプレイヤーが2組表示される
   - 片方で犬を動かすと、もう片方でその犬が動いて見える
   - 相手の頭上に名前タグが表示される
   - 片方が退出しても、もう片方は正常に動作する

---

## 参考: 完成後のProject構成

```
Assets/Resources/MetaverseWalk/
├── shiba_null.prefab          ... 元の犬モデル（シングルプレイヤー用）
├── NetworkDog.prefab          ... ネットワーク対応犬（マルチプレイヤー用）
├── NetworkPlayer.prefab       ... ネットワーク対応プレイヤー（マルチプレイヤー用）
└── NetworkRunnerPrefab.prefab ... Photon NetworkRunner

Assets/Scripts/MetaverseWalk/Network/
├── NetworkConstants.cs
├── PhotonNetworkManager.cs
├── NetworkDogController.cs
├── NetworkPlayerController.cs
├── NetworkSpawnManager.cs
└── PlayerNameTag.cs
```

---

## 関連ドキュメント

| ファイル | 内容 |
|----------|------|
| [metaverse_walk_overview.md](./metaverse_walk_overview.md) | 機能全体の概要 |
| [metaverse_walk_scene.md](./metaverse_walk_scene.md) | メタバースシーン設計 |
| [metaverse_walk_network.md](./metaverse_walk_network.md) | マルチプレイヤー同期仕様 |
| [metaverse_walk_trigger.md](./metaverse_walk_trigger.md) | 散歩トリガーシステム |
| [metaverse_walk_voice.md](./metaverse_walk_voice.md) | 音声通話仕様（フェーズ3） |
