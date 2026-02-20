# Git Push

変更をリモートリポジトリにプッシュします。

## 手順

1. `git status` でコミット済みかを確認
2. `git log origin/main..HEAD --oneline` でプッシュ対象のコミットを表示
3. ユーザーに確認後、`git push` を実行

## 注意

- force pushは明示的に指示されない限り実行しない
- main/masterへの直接pushは確認を求める
