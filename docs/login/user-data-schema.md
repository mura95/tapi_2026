# Firestoreユーザーデータ構造

## 1. 概要

ユーザー情報はFirestoreの`users`コレクションに保存する。

---

## 2. データスキーマ

### 2.1 コレクション構造

```
Firestore
└── users (collection)
    └── {userId} (document)
        ├── accountCreatedTime: Timestamp
        ├── accountType: string
        ├── displayName: string
        ├── email: string
        ├── fcmToken: string
        ├── lastLoginTime: Timestamp
        ├── notificationSettings: map
        │   ├── adminNotification: boolean
        │   ├── userNotification: boolean
        │   └── watchNotification: boolean
        ├── petName: string
        ├── userId: string
        ├── appMode: string
        ├── isDonor: boolean
        └── hasAppliedDevice: boolean
```

### 2.2 フィールド詳細

| フィールド | 型 | 必須 | 説明 | 例 |
|------------|-----|:----:|------|-----|
| `accountCreatedTime` | Timestamp | ○ | アカウント作成日時 | 2025年1月4日 5:51:17 UTC+9 |
| `accountType` | string | ○ | アカウント種別 | "PARENT" |
| `displayName` | string | ○ | 表示名 | "たっちゃん" |
| `email` | string | ○ | メールアドレス | "misaki@pichipichi.co.jp" |
| `fcmToken` | string | - | FCMプッシュ通知トークン | "eHX_r-0eRgKeUHtC..." |
| `lastLoginTime` | Timestamp | ○ | 最終ログイン日時 | 2026年1月16日 18:34:28 UTC+9 |
| `notificationSettings` | map | ○ | 通知設定 | (下記参照) |
| `petName` | string | - | ペットの名前 | "ぽち" |
| `userId` | string | ○ | Firebase Auth UID | "C0yMWJgQ6wOQjkr8reGXUCS0tjN2" |
| `appMode` | string | ○ | アプリモード | "TapHouse" or "TapPocket" |
| `isDonor` | boolean | - | 寄付者フラグ | false |
| `hasAppliedDevice` | boolean | - | 端末申込済みフラグ | false |

### 2.3 notificationSettings詳細

| フィールド | 型 | デフォルト | 説明 |
|------------|-----|----------|------|
| `adminNotification` | boolean | true | 管理者からの通知 |
| `userNotification` | boolean | true | ユーザー向け通知 |
| `watchNotification` | boolean | true | 見守り通知 |

---

## 3. accountType値

| 値 | 説明 |
|-----|------|
| `PARENT` | 親（見守る側） |
| `CHILD` | 子（見守られる側・高齢者） |
| `CAREGIVER` | 介護者 |
| `ADMIN` | 管理者 |

---

## 4. appMode値

| 値 | 説明 | 遷移先シーン |
|-----|------|-------------|
| `TapHouse` | たっぷハウス（置き型端末） | main.unity |
| `TapPocket` | たっぷポケット（スマホアプリ） | PocketMain.unity |

---

## 5. サンプルドキュメント

```json
{
  "accountCreatedTime": "2025-01-04T05:51:17+09:00",
  "accountType": "PARENT",
  "displayName": "たっちゃん",
  "email": "misaki@pichipichi.co.jp",
  "fcmToken": "eHX_r-0eRgKeUHtCdCJ_Rc:APA91bHZCHQ5rVAOQ_IApVzb-o5rAGWIyrwcmuEocq9uRZzJhMx9-FDRRFNyPuKcfn2Mf3lGfto6M45BDZgZyn1c5m15NMWQrPOSYQWPCnE1f56MVZX806U",
  "lastLoginTime": "2026-01-16T18:34:28+09:00",
  "notificationSettings": {
    "adminNotification": true,
    "userNotification": true,
    "watchNotification": true
  },
  "petName": "ぽち",
  "userId": "C0yMWJgQ6wOQjkr8reGXUCS0tjN2",
  "appMode": "TapHouse",
  "isDonor": false,
  "hasAppliedDevice": true
}
```

---

## 6. Realtime DBとの使い分け

### 6.1 Firestore（ユーザー情報・ログ）

| データ | コレクション | 用途 |
|--------|-------------|------|
| ユーザー情報 | `users/{userId}` | アカウント情報、設定 |
| 見守りログ | `watchLogs/{logId}` | 見守り履歴 |
| 通知情報 | `notifications/{notificationId}` | プッシュ通知履歴 |
| リマインダー | `users/{userId}/reminders/{reminderId}` | リマインダー設定 |

### 6.2 Realtime DB（リアルタイム同期）

| データ | パス | 用途 |
|--------|------|------|
| 犬の状態 | `users/{userId}/state` | idle, feeding, sleeping等 |
| ゲームログ | `users/{userId}/playLog` | ボール遊び等のパラメータ |
| 着せ替え | `users/{userId}/costumes` | 装備中のアイテム |
| デバイス位置 | `users/{userId}/dogLocation` | マルチデバイス |

---

## 7. 読み書きの例

### 7.1 ユーザー情報取得

```csharp
// 擬似コード
public async Task<UserData> GetUserDataAsync(string userId)
{
    var doc = await FirebaseFirestore.DefaultInstance
        .Collection("users")
        .Document(userId)
        .GetSnapshotAsync();

    if (doc.Exists)
    {
        return doc.ConvertTo<UserData>();
    }
    return null;
}
```

### 7.2 最終ログイン更新

```csharp
// 擬似コード
public async Task UpdateLastLoginAsync(string userId)
{
    await FirebaseFirestore.DefaultInstance
        .Collection("users")
        .Document(userId)
        .UpdateAsync(new Dictionary<string, object>
        {
            { "lastLoginTime", FieldValue.ServerTimestamp }
        });
}
```

### 7.3 FCMトークン更新

```csharp
// 擬似コード
public async Task UpdateFcmTokenAsync(string userId, string token)
{
    await FirebaseFirestore.DefaultInstance
        .Collection("users")
        .Document(userId)
        .UpdateAsync(new Dictionary<string, object>
        {
            { "fcmToken", token }
        });
}
```

---

## 8. セキュリティルール（Firestore）

```javascript
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {

    // ユーザーは自分のドキュメントのみ読み書き可能
    match /users/{userId} {
      allow read, write: if request.auth != null
                         && request.auth.uid == userId;
    }

    // 見守りログは関連ユーザーのみアクセス可能
    match /watchLogs/{logId} {
      allow read: if request.auth != null;
      allow write: if request.auth != null
                   && request.resource.data.userId == request.auth.uid;
    }
  }
}
```

---

## 9. インデックス

| コレクション | フィールド | 順序 | 用途 |
|-------------|-----------|------|------|
| `users` | `accountType`, `accountCreatedTime` | ASC, DESC | 管理画面でのユーザー一覧 |
| `users` | `isDonor`, `accountCreatedTime` | ASC, DESC | 寄付者一覧 |
| `watchLogs` | `userId`, `timestamp` | ASC, DESC | ユーザー別ログ取得 |
