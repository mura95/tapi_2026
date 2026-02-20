Stripeパッケージがまだインストールされていないので、TypeScriptエラーはnpm  
 install後に解消されます。これで実装完了です。

実装した内容をまとめます：

実装完了サマリー

Phase 1: 基盤

- package.json: @stripe/stripe-js 追加
- functions/package.json: stripe 追加
- firebase.json: マイページ・管理者ページ・API用のrewriteルール追加
- scripts/build.js: mypage.ts、admin.ts のエントリポイント追加
- src/shared/types.ts:
  共通型定義（UserData拡張、ポイント、サブスク、トランザクション）
- src/shared/firebase-client.ts: Firebase初期化の共通化
- src/shared/api.ts: API呼び出しユーティリティ
- public/css/common.css: 共通CSSコンポーネント
- public/css/mypage.css: マイページ専用CSS
- public/css/admin.css: 管理者ページ専用CSS

Phase 2: Firebase Functions

- functions/src/stripe.ts: Stripe Checkout/Portal/Webhook処理
- functions/src/points.ts: ポイント残高、パッケージ、サブスク状態、履歴取
- functions/src/admin.ts: 銀行振込申請の一覧/承認/却下
- functions/src/index.ts: APIルーター（stripeApi, pointsApi, adminApi）

Phase 3 & 4: フロントエンド

- public/mypage.html:
  マイページUI（モバイルファースト、ボトムナビ、サイドバー）
- src/mypage.ts:
  マイページロジック（タブ切り替え、ポイント購入、サブスク管理）
- public/admin.html: 管理者ページUI
- src/admin.ts: 管理者ロジック（振込承認/却下）

環境変数

- .env.example: STRIPE_PUBLISHABLE_KEY 追加
- functions/.env.example:
  STRIPE_SECRET_KEY、STRIPE_WEBHOOK_SECRET、ADMIN_UIDS 追加

次のステップ

1. npm install と cd functions && npm install で依存関係インストール
2. Stripeダッシュボードでプロダクト/価格を作成し、Firestoreに
   pointPackages/subscriptionPlans を登録
3. .env と functions/.env に実際のStripeキーを設定
4. Stripeダッシュボードでwebhookエンドポイントを登録
5. npm run build でビルド
6. npm run serve でローカルテスト
