# タスクリスト: ブラウザ再試行ロジックの実装

- [x] `CrawlerService.cs` のブラウザ候補取得をリスト化
    - [x] `GetBrowserPath` を `GetAllBrowserPaths` に改名し、`List<string>` を返すように変更
    - [x] 重複パスを排除するロジックを追加
- [x] `CrawlerService.cs` の初期化フローを改善
    - [x] `_executablePath` の代わりに `List<string> _availablePaths` を導入
    - [x] `EnsureBrowserAsync` で全候補を順番に `LaunchAsync` するループを実装
    - [x] 成功したブラウザを記録し、次回以降固定するように調整
- [x] 動作確認
    - [x] ビルドが通ることを確認
- [x] 修正内容のまとめ (walkthrough.md) の作成
