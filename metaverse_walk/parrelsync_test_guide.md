# ParrelSync マルチプレイテスト手順

ParrelSyncを使って2つのUnity Editorで同じルームに接続し、犬・プレイヤーが正しく同期されることを検証するための手順書。

## 前提条件

- ParrelSync v1.5.2 がインストール済み
- Photon Fusion 2 の AppID が設定済み（`Assets/Photon/Fusion/Resources/PhotonAppSettings.asset`）
- `MetaverseTestBootstrap` と `MetaverseDebugOverlay` が実装済み（`Assets/Scripts/MetaverseWalk/Debug/`）

## 関連コンポーネント

| コンポーネント | ファイル | 役割 |
|---------------|---------|------|
| MetaverseTestBootstrap | `Assets/Scripts/MetaverseWalk/Debug/MetaverseTestBootstrap.cs` | PetState自動設定、ParrelSyncクローン検出、表示名の自動区別 |
| MetaverseDebugOverlay | `Assets/Scripts/MetaverseWalk/Debug/MetaverseDebugOverlay.cs` | IMGUI デバッグオーバーレイ（接続状態・ルーム名・プレイヤー数等） |

---

## Phase 1: シーンにデバッグコンポーネントを配置

1. **Unity Editorでプロジェクトを開く**

2. **Metaverse.unityを開く**
   - `Assets/Scenes/Metaverse.unity` をダブルクリック

3. **デバッグ用GameObjectを作成**
   - Hierarchyウィンドウで右クリック → `Create Empty`
   - 名前を `DebugBootstrap` に変更

4. **2つのコンポーネントをアタッチ**
   - `DebugBootstrap` を選択した状態で Inspectorの `Add Component` をクリック
   - `MetaverseTestBootstrap` を検索してアタッチ
   - もう一度 `Add Component` → `MetaverseDebugOverlay` を検索してアタッチ

5. **シーンを保存**
   - `Ctrl + S`

> **この時点でのInspector確認ポイント:**
> - `MetaverseTestBootstrap`: Host Player Name = `Host_Player`, Clone Player Name = `Clone_Player`
> - `MetaverseDebugOverlay`: Show Overlay = ON, Toggle Key = `F1`

---

## Phase 2: 単体動作確認（ParrelSync前）

まず1つのEditorだけで正常起動するか確認する。

1. **Playボタンを押す**

2. **Consoleウィンドウを確認** (`Window → General → Console`)
   以下のログが出ていれば `MetaverseTestBootstrap` が正常動作:
   ```
   [MetaverseTestBootstrap] Set PetState to walk (direct play mode)
   [MetaverseTestBootstrap] Original editor - DisplayName: 'Host_Player'
   [MetaverseTestBootstrap] Initialized - IsClone: false, DisplayName: Host_Player
   ```

3. **画面左上のデバッグオーバーレイを確認**
   以下の情報が表示される:
   ```
   FPS: XX.X
   ParrelSync: Original
   Name: Host_Player
   Status: Connected (緑色) または Disconnected (赤色)
   Room: WalkRoom_XXXXXXXX_XXXX_X
   Players: 1
   PetState: walk
   ```
   - `F1`キーでオーバーレイの表示/非表示を切り替え可能

4. **接続失敗した場合のフォールバック確認**
   - `Status: Disconnected` で犬とプレイヤーが画面に表示されていれば、シングルプレイヤーフォールバックが正常動作
   - `Status: Connected` であれば、Photon Fusionサーバーに接続成功

5. **Playを停止**

> **トラブルシューティング:**
> - オーバーレイが表示されない → `DebugBootstrap` の `MetaverseDebugOverlay` で `Show Overlay` がONか確認
> - コンパイルエラー → `Console` のErrorタブを確認
> - `PetState: idle` のまま → `MetaverseTestBootstrap` の Awake() が MetaverseManager の Start() より前に実行されているか確認

---

## Phase 3: Script Execution Order の確認（必要な場合）

`MetaverseTestBootstrap.Awake()` は `MetaverseManager.Start()` より先に実行される必要がある。通常 `Awake` → `Start` の順で実行されるので問題ないが、念のため確認:

1. `Edit → Project Settings → Script Execution Order`
2. 特に設定がなければデフォルトのままでOK（`Awake` は全オブジェクトで `Start` より先に呼ばれる）

---

## Phase 4: ParrelSyncクローンの作成

1. **メニューバー → `ParrelSync` → `Clones Manager`**

2. **Clones Managerウィンドウが開く**
   - まだクローンがない場合: `Create new clone` ボタンをクリック
   - 既にクローンがある場合: そのまま使える

3. **クローン作成を待つ**
   - プロジェクトのシンボリックリンクが作成される
   - `Library` フォルダのコピーが行われるため、少し時間がかかる（初回のみ）

4. **`Open in New Editor` をクリック**
   - 2つ目のUnity Editorが起動する
   - 起動に数分かかる場合がある

> **注意:** クローン側のEditorは元のプロジェクトとスクリプト・アセットを共有している。片方でスクリプトを変更すると、もう片方も自動的にリコンパイルされる。

---

## Phase 5: マルチプレイテスト実行

1. **両方のEditorで `Assets/Scenes/Metaverse.unity` を開く**
   - オリジナル側: 既に開いているはず
   - クローン側: `Assets/Scenes/Metaverse.unity` をダブルクリック

2. **オリジナルEditorで Play を押す**
   - Console で以下を確認:
     ```
     [MetaverseTestBootstrap] Original editor - DisplayName: 'Host_Player'
     [MetaverseTestBootstrap] Connection: Connected - 接続中
     [MetaverseTestBootstrap] Player count: 1
     ```
   - オーバーレイで `Status: Connected (緑)` を確認

3. **クローンEditorで Play を押す**（オリジナルがConnectedになった後）
   - Console で以下を確認:
     ```
     [MetaverseTestBootstrap] Clone detected - DisplayName set to 'Clone_Player'
     [MetaverseTestBootstrap] Connection: Connected - 接続中
     [MetaverseTestBootstrap] Player count: 2
     ```

4. **両方のオーバーレイを比較確認**

   | 項目 | オリジナル | クローン |
   |------|-----------|---------|
   | ParrelSync | Original | Clone |
   | Name | Host_Player | Clone_Player |
   | Status | Connected (緑) | Connected (緑) |
   | Room | 同じルーム名 | 同じルーム名 |
   | Players | 2 | 2 |

---

## Phase 6: 同期検証

両方Connectedになったら、以下を順番に確認する。

### 6-1. スポーン確認
- 両方のEditorで犬とプレイヤーが地面の上に表示されているか
- 地面に埋まっていないか（NavMeshスナップが機能しているか）

### 6-2. 移動同期テスト
- **オリジナル側** で移動ボタン（前進/左/右）を操作
- **クローン側** のGame画面でオリジナルの犬が動いているか確認
- 逆も実施: クローン側で操作 → オリジナル側で確認

### 6-3. プレイヤー追従確認
- 犬を移動させると、プレイヤー（飼い主）が後ろから追従するか

### 6-4. 名前タグ確認
- 相手のプレイヤーに名前タグが表示されているか
- オリジナル側から見たクローンの名前が `Clone_Player` と表示されるか

### 6-5. 切断テスト
- **クローン側のPlayを停止**
- オリジナル側で:
  - `Players: 1` に減少するか
  - エラーなく動作を継続するか
  - Consoleに `Player left` 系のログが出るか

---

## Phase 7: 接続失敗フォールバックテスト

Photon接続が失敗した場合の動作確認:

1. **インターネット接続を切断**（Wi-Fiオフなど）
2. **Metaverse.unityでPlayを押す**
3. 以下を確認:
   - Consoleに `Failed to connect to Photon, falling back to single player` が出る
   - 犬とプレイヤーがローカルスポーンされる
   - 移動ボタンで犬を操作できる
4. **インターネット接続を復元**

---

## よくある問題と対処法

| 症状 | 原因と対処 |
|------|-----------|
| クローンが同じルームに入らない | 時間バケットが30分単位のため、30分の境界をまたいだ場合に起きる。両方のPlayを同じ30分枠内で押す |
| `PhotonNetworkManager: Not Found` とオーバーレイに表示 | シーンに `PhotonNetworkManager` オブジェクトがない。Metaverse.unityを正しく開いているか確認 |
| クローン側でコンパイルエラー | クローンEditorを再起動して再コンパイルさせる |
| 犬が地面に埋まる | SpawnPointのY座標を確認し、NavMeshが正しくベイクされているか `Window → AI → Navigation` で確認 |
| 接続が `Disconnected` のまま | Photon App IDが設定されているか `Assets/Photon/Fusion/Resources/PhotonAppSettings.asset` を確認 |
| 名前空間エラー (`TapHouse.MetaverseWalk.Debug`) | デバッグコンポーネントの名前空間が `TapHouse.MetaverseWalk.DebugTools` になっているか確認（`Debug` は `UnityEngine.Debug` と衝突する） |

---

## 検証チェックリスト

- [ ] 両クライアントが同じ時間バケットのルームに自動マッチング
- [ ] 犬・プレイヤーのスポーンが地上（NavMesh上）に配置
- [ ] 移動操作が相手側に反映（位置・アニメーション同期）
- [ ] 片方を停止（Play停止）→ もう片方が正常に動作継続
- [ ] 接続失敗時にシングルプレイヤーモードへフォールバック
- [ ] プレイヤー名が Host_Player / Clone_Player で区別されている
- [ ] デバッグオーバーレイが正しく情報を表示
