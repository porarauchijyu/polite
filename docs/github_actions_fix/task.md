# タスクリスト: GitHub Actions フィード生成修正

- [x] `PathHelper.cs` の改善
    - [x] `FindProjectRoot` メソッドの追加
    - [x] `ResolvePath` でルートディレクトリを優先するように変更
- [x] `Program.cs` の修正
    - [x] `--run` オプションの明示的な処理を追加
    - [x] 巡回完了後、常に `UpdateRssFeed` を呼び出すように変更
- [x] `RssService.cs` の修正
    - [x] `GenerateAllFeeds` 内のパス解決を `PathHelper` に統一
- [/] 動作確認
    - [ ] ローカルでの実行確認
    - [ ] フィードファイルがルートに生成/更新されることの確認
