# 着せ替え機能

## 1. 概要

犬に服、被り物、アクセサリーを着せ替える機能。Firebase経由でたっぷハウスと同期する。

---

## 2. 機能仕様

### 2.1 着せ替えカテゴリ

| カテゴリ | 説明 | 同時装着 |
|---------|------|---------|
| 服 | Tシャツ、ジャケット、ドレス等 | 1つ |
| 被り物 | 帽子、リボン、かぶりもの等 | 1つ |
| アクセサリー | 首輪、メガネ、バッグ等 | 2つまで |

### 2.2 アイテム管理

| 要素 | 説明 |
|------|------|
| 所持アイテム | ユーザーが所持しているアイテム |
| 装着アイテム | 現在犬が着用しているアイテム |
| ロックアイテム | 未解放のアイテム（将来機能） |

---

## 3. 画面設計

### 3.1 着せ替え画面

```
┌─────────────────────────────────────┐
│ ← 戻る      着せ替え         完了   │ ← ヘッダー
├─────────────────────────────────────┤
│                                     │
│                                     │
│         ┌───────────────┐           │
│         │               │           │
│         │      🐕       │           │ ← 犬（上部、画面の40%）
│         │    （3D）     │           │    回転操作可能
│         │               │           │
│         └───────────────┘           │
│                                     │
│   ┌─────────────────────────────┐   │
│   │ 🔄 回転       👁 プレビュー  │   │ ← コントロール
│   └─────────────────────────────┘   │
│                                     │
├─────────────────────────────────────┤
│   カテゴリタブ                       │
│   ┌──────┬──────┬──────┬──────┐     │ ← タブ
│   │ すべて│  服  │被り物│アクセ│     │
│   └──────┴──────┴──────┴──────┘     │
├─────────────────────────────────────┤
│   アイテムグリッド（スクロール可）    │
│   ┌─────┬─────┬─────┬─────┐         │
│   │なし │ 👕1 │ 👕2 │ 👕3 │         │ ← アイテム
│   ├─────┼─────┼─────┼─────┤         │    選択中は枠線
│   │ 👕4 │ 👕5 │ 👕6 │ 👕7 │         │
│   ├─────┼─────┼─────┼─────┤         │
│   │ 👕8 │ 🔒  │ 🔒  │ 🔒  │         │ ← ロックアイテム
│   └─────┴─────┴─────┴─────┘         │
│                                     │
└─────────────────────────────────────┘
```

### 3.2 UI要素仕様

| 要素 | サイズ | 備考 |
|------|--------|------|
| 犬表示エリア | 画面の40% | 3D表示、ピンチ操作で回転 |
| カテゴリタブ | 高さ50dp | 4タブ |
| アイテムセル | 80×80dp | サムネイル + 選択枠 |
| 「なし」セル | 80×80dp | 各カテゴリの先頭に配置 |

### 3.3 アイテムセル状態

```
通常状態:          選択状態:          ロック状態:
┌─────────┐       ┌─────────┐       ┌─────────┐
│         │       │ ▓▓▓▓▓▓▓ │       │         │
│   👕    │       │ ▓  👕  ▓ │       │   🔒    │
│         │       │ ▓▓▓▓▓▓▓ │       │         │
│ 赤Tシャツ │       │ 赤Tシャツ │       │  ？？？  │
└─────────┘       └─────────┘       └─────────┘
  枠線なし          青枠線            グレーアウト
```

---

## 4. 犬の3D表示

### 4.1 表示仕様

| 項目 | 仕様 |
|------|------|
| 位置 | 画面上部中央 |
| サイズ | 画面幅の60%、高さ40% |
| 背景 | 透明（グラデーション背景） |
| 操作 | 左右スワイプで回転 |
| ライティング | 正面光 + 環境光 |

### 4.2 回転操作

```csharp
public class DressUpDogView : MonoBehaviour
{
    [SerializeField] private Transform dogTransform;
    [SerializeField] private float rotateSpeed = 0.5f;

    private float currentRotation = 0f;

    private void OnDrag(Vector2 delta)
    {
        // 左右スワイプで犬を回転
        currentRotation += delta.x * rotateSpeed;
        dogTransform.rotation = Quaternion.Euler(0, currentRotation, 0);
    }

    public void ResetRotation()
    {
        currentRotation = 0f;
        dogTransform.rotation = Quaternion.identity;
    }
}
```

---

## 5. データ設計

### 5.1 DressUpItem ScriptableObject

```csharp
[CreateAssetMenu(fileName = "DressUpItem", menuName = "TapHouse/DressUp/Item")]
public class DressUpItem : ScriptableObject
{
    [Header("基本情報")]
    public string itemId;           // 一意のID
    public string displayName;       // 表示名
    public DressUpCategory category; // カテゴリ
    public Sprite thumbnail;         // サムネイル画像

    [Header("3Dモデル")]
    public GameObject prefab;        // 装着用Prefab
    public string attachBone;        // 装着先のボーン名

    [Header("入手条件")]
    public bool isDefault;           // デフォルトで所持
    public int unlockCondition;      // 解放条件（将来用）
}

public enum DressUpCategory
{
    Clothes,    // 服
    Headwear,   // 被り物
    Accessory   // アクセサリー
}
```

### 5.2 Firebase構造

```
users/{userId}/dressUp/
├── equipped/
│   ├── clothes: "item_tshirt_red"     # 装着中の服
│   ├── headwear: "item_hat_blue"      # 装着中の被り物
│   └── accessories: ["item_collar_gold"] # 装着中のアクセサリー
└── owned/
    ├── item_tshirt_red: true
    ├── item_tshirt_blue: true
    ├── item_hat_blue: true
    └── item_collar_gold: true
```

---

## 6. 装着システム

### 6.1 アーキテクチャ

```
┌─────────────────────────────────────────────────────────────┐
│                    着せ替えシステム                          │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌───────────────┐    ┌───────────────┐                    │
│  │ DressUpManager │←──│ DressUpUI     │                    │
│  │ (ロジック)      │    │ (表示)        │                    │
│  └───────┬───────┘    └───────────────┘                    │
│          │                                                  │
│          ↓                                                  │
│  ┌───────────────┐    ┌───────────────┐                    │
│  │DogDressUpModel│    │ Firebase同期  │                    │
│  │ (3Dモデル)     │←──│              │                    │
│  └───────────────┘    └───────────────┘                    │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 6.2 DressUpManager.cs

```csharp
using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace TapHouse.Pocket.DressUp
{
    public class DressUpManager : MonoBehaviour
    {
        [SerializeField] private DogDressUpModel dogModel;
        [SerializeField] private DressUpItemDatabase itemDatabase;

        // 現在の装着状態
        private string equippedClothes;
        private string equippedHeadwear;
        private List<string> equippedAccessories = new List<string>();

        // 一時的なプレビュー状態
        private string previewClothes;
        private string previewHeadwear;
        private List<string> previewAccessories = new List<string>();

        public async UniTask Initialize()
        {
            // Firebaseから現在の装着状態を読み込み
            await LoadEquippedItems();

            // 犬モデルに適用
            ApplyEquippedItems();
        }

        public void PreviewItem(DressUpItem item)
        {
            switch (item.category)
            {
                case DressUpCategory.Clothes:
                    previewClothes = item.itemId;
                    break;
                case DressUpCategory.Headwear:
                    previewHeadwear = item.itemId;
                    break;
                case DressUpCategory.Accessory:
                    // アクセサリーはトグル
                    if (previewAccessories.Contains(item.itemId))
                        previewAccessories.Remove(item.itemId);
                    else if (previewAccessories.Count < 2)
                        previewAccessories.Add(item.itemId);
                    break;
            }

            // プレビュー適用
            ApplyPreview();
        }

        public void ClearItem(DressUpCategory category)
        {
            switch (category)
            {
                case DressUpCategory.Clothes:
                    previewClothes = null;
                    break;
                case DressUpCategory.Headwear:
                    previewHeadwear = null;
                    break;
                case DressUpCategory.Accessory:
                    previewAccessories.Clear();
                    break;
            }

            ApplyPreview();
        }

        public async UniTask ConfirmChanges()
        {
            // プレビューを確定
            equippedClothes = previewClothes;
            equippedHeadwear = previewHeadwear;
            equippedAccessories = new List<string>(previewAccessories);

            // Firebaseに保存
            await SaveEquippedItems();
        }

        public void CancelChanges()
        {
            // プレビューをリセット
            previewClothes = equippedClothes;
            previewHeadwear = equippedHeadwear;
            previewAccessories = new List<string>(equippedAccessories);

            // 元の状態に戻す
            ApplyEquippedItems();
        }

        private void ApplyPreview()
        {
            dogModel.SetClothes(previewClothes);
            dogModel.SetHeadwear(previewHeadwear);
            dogModel.SetAccessories(previewAccessories);
        }

        private void ApplyEquippedItems()
        {
            dogModel.SetClothes(equippedClothes);
            dogModel.SetHeadwear(equippedHeadwear);
            dogModel.SetAccessories(equippedAccessories);
        }

        private async UniTask LoadEquippedItems()
        {
            // Firebase読み込み実装
        }

        private async UniTask SaveEquippedItems()
        {
            // Firebase保存実装
        }
    }
}
```

### 6.3 DogDressUpModel.cs

```csharp
using UnityEngine;
using System.Collections.Generic;

namespace TapHouse.Pocket.DressUp
{
    public class DogDressUpModel : MonoBehaviour
    {
        [Header("装着ポイント")]
        [SerializeField] private Transform clothesAttachPoint;
        [SerializeField] private Transform headwearAttachPoint;
        [SerializeField] private Transform[] accessoryAttachPoints;

        [Header("データベース")]
        [SerializeField] private DressUpItemDatabase itemDatabase;

        private GameObject currentClothes;
        private GameObject currentHeadwear;
        private List<GameObject> currentAccessories = new List<GameObject>();

        public void SetClothes(string itemId)
        {
            // 現在の服を削除
            if (currentClothes != null)
            {
                Destroy(currentClothes);
            }

            if (string.IsNullOrEmpty(itemId)) return;

            // 新しい服を装着
            var item = itemDatabase.GetItem(itemId);
            if (item != null && item.prefab != null)
            {
                currentClothes = Instantiate(item.prefab, clothesAttachPoint);
                currentClothes.transform.localPosition = Vector3.zero;
                currentClothes.transform.localRotation = Quaternion.identity;
            }
        }

        public void SetHeadwear(string itemId)
        {
            // 現在の被り物を削除
            if (currentHeadwear != null)
            {
                Destroy(currentHeadwear);
            }

            if (string.IsNullOrEmpty(itemId)) return;

            // 新しい被り物を装着
            var item = itemDatabase.GetItem(itemId);
            if (item != null && item.prefab != null)
            {
                currentHeadwear = Instantiate(item.prefab, headwearAttachPoint);
                currentHeadwear.transform.localPosition = Vector3.zero;
                currentHeadwear.transform.localRotation = Quaternion.identity;
            }
        }

        public void SetAccessories(List<string> itemIds)
        {
            // 現在のアクセサリーをすべて削除
            foreach (var acc in currentAccessories)
            {
                Destroy(acc);
            }
            currentAccessories.Clear();

            // 新しいアクセサリーを装着
            for (int i = 0; i < itemIds.Count && i < accessoryAttachPoints.Length; i++)
            {
                var item = itemDatabase.GetItem(itemIds[i]);
                if (item != null && item.prefab != null)
                {
                    var acc = Instantiate(item.prefab, accessoryAttachPoints[i]);
                    acc.transform.localPosition = Vector3.zero;
                    acc.transform.localRotation = Quaternion.identity;
                    currentAccessories.Add(acc);
                }
            }
        }
    }
}
```

---

## 7. アイテムデータベース

### 7.1 DressUpItemDatabase.cs

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace TapHouse.Pocket.DressUp
{
    [CreateAssetMenu(fileName = "DressUpItemDatabase", menuName = "TapHouse/DressUp/Database")]
    public class DressUpItemDatabase : ScriptableObject
    {
        [SerializeField] private List<DressUpItem> items = new List<DressUpItem>();

        public DressUpItem GetItem(string itemId)
        {
            return items.FirstOrDefault(i => i.itemId == itemId);
        }

        public List<DressUpItem> GetItemsByCategory(DressUpCategory category)
        {
            return items.Where(i => i.category == category).ToList();
        }

        public List<DressUpItem> GetAllItems()
        {
            return items;
        }

        public List<DressUpItem> GetDefaultItems()
        {
            return items.Where(i => i.isDefault).ToList();
        }
    }
}
```

### 7.2 初期アイテム一覧

| カテゴリ | アイテム名 | デフォルト |
|---------|-----------|----------|
| 服 | 赤Tシャツ | ○ |
| 服 | 青Tシャツ | ○ |
| 服 | ボーダーシャツ | ○ |
| 被り物 | 赤リボン | ○ |
| 被り物 | 青帽子 | ○ |
| 被り物 | 麦わら帽子 | ○ |
| アクセサリー | 金の首輪 | ○ |
| アクセサリー | 蝶ネクタイ | ○ |

---

## 8. Firebase同期

### 8.1 同期タイミング

| タイミング | 動作 |
|-----------|------|
| アプリ起動 | 装着状態を読み込み |
| 完了ボタン | 変更を保存 |
| バックグラウンド→復帰 | 最新状態を読み込み |

### 8.2 同期処理

```csharp
public class DressUpFirebaseSync : MonoBehaviour
{
    private DatabaseReference dressUpRef;

    public async UniTask<EquippedItems> LoadEquippedItems()
    {
        var snapshot = await dressUpRef.Child("equipped").GetValueAsync();

        if (!snapshot.Exists) return new EquippedItems();

        return new EquippedItems
        {
            clothes = snapshot.Child("clothes").Value?.ToString(),
            headwear = snapshot.Child("headwear").Value?.ToString(),
            accessories = ParseAccessories(snapshot.Child("accessories"))
        };
    }

    public async UniTask SaveEquippedItems(EquippedItems items)
    {
        var updates = new Dictionary<string, object>
        {
            { "equipped/clothes", items.clothes ?? "" },
            { "equipped/headwear", items.headwear ?? "" },
            { "equipped/accessories", items.accessories }
        };

        await dressUpRef.UpdateChildrenAsync(updates);
    }
}

public class EquippedItems
{
    public string clothes;
    public string headwear;
    public List<string> accessories = new List<string>();
}
```

---

## 9. テストケース

| # | テスト内容 | 期待結果 |
|---|-----------|---------|
| 1 | 服を選択 | プレビュー表示、選択枠表示 |
| 2 | 「なし」を選択 | アイテム非表示 |
| 3 | 完了ボタン | Firebase保存、メイン画面に反映 |
| 4 | 戻るボタン | 変更キャンセル、元の状態に戻る |
| 5 | 犬を回転 | スワイプで回転 |
| 6 | アクセサリー2つ装着 | 両方表示 |
| 7 | アクセサリー3つ目選択 | 装着されない（上限2） |
| 8 | アプリ再起動 | 装着状態が維持 |

---

## 10. 新規ファイル一覧

| ファイル | 役割 |
|----------|------|
| `Assets/Scripts/Pocket/DressUp/DressUpManager.cs` | 着せ替えロジック |
| `Assets/Scripts/Pocket/DressUp/DressUpItem.cs` | アイテムデータ |
| `Assets/Scripts/Pocket/DressUp/DressUpItemDatabase.cs` | アイテムデータベース |
| `Assets/Scripts/Pocket/DressUp/DogDressUpModel.cs` | 3Dモデル制御 |
| `Assets/Scripts/Pocket/DressUp/DressUpUI.cs` | UI制御 |
| `Assets/Scripts/Pocket/DressUp/DressUpFirebaseSync.cs` | Firebase同期 |
| `Assets/Resources/Pocket/DressUp/DressUpItemDatabase.asset` | データベースアセット |
