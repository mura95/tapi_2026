# Git Commit

変更をgitにコミットします。

## 手順

1. `git status` で変更ファイルを確認
2. `git diff --stat` で変更内容を確認
3. `git log --oneline -3` で最近のコミットスタイルを確認
4. 変更内容に基づいて適切なコミットメッセージを作成
5. ユーザーに確認後、コミットを実行

## ルール

- コミットメッセージは英語で、変更内容を簡潔に記述
- Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com> を末尾に追加
- pushは明示的に指示されない限り実行しない
