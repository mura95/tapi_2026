# Unity Console Log 確認

Unity Editor のコンソールログを確認し、エラーや警告を分析します。

## ログファイル

- **パス:** `Logs/unity_console.log`（プロジェクトルート直下）
- **書き出し元:** `Assets/Scripts/Editor/UnityConsoleLogger.cs`
- **形式:** `[タイムスタンプ] [LOG/WARN/ERR] (クラス名:メソッド名) メッセージ`

## 実行内容

1. `Logs/unity_console.log` を読み取る
2. エラー（ERR）や警告（WARN）を優先的に抽出
3. 問題の原因と修正案を日本語で説明
4. 該当するソースコードがあれば特定し、修正を提案
