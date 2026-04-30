# GitHub Actions でのフィード生成問題の修正計画

GitHub Actions の実行が完了しても `feed.xml` 等が生成（更新）されない問題を修正します。

## 現状の分析
1. **更新がないと生成されない**: `Program.cs` の現在のロジックでは、新しい記事が1件も見つからなかった場合に `UpdateRssFeed` をスキップしています。CI環境（GitHub Actions）では、リポジトリを常に最新状態（ファイルが存在する状態）に保つ必要があるため、更新の有無にかかわらずフィードを生成すべきです。
2. **保存場所の不一致**: `PathHelper.ResolvePath` が、ファイルが存在しない場合にデフォルトで実行ファイルのあるディレクトリ（`bin/...`）を返します。GitHub Actions では、ビルドされたバイナリが深い階層にあるため、ファイルがそこに作成されると、リポジトリルートでの `git add` に引っかからず、結果として生成されていないように見えます。

## 修正内容

### 1. `Program.cs` のロジック修正
- `hasGlobalUpdates` の有無にかかわらず、通常の巡回実行時には必ず `UpdateRssFeed` を呼び出すように変更します。
- 引数なし、または `--run` 指定時の挙動を整理します。

### 2. `PathHelper.cs` の改善
- ファイルが存在しない場合でも、プロジェクトのルート（`targets.json` が存在するディレクトリ）を特定し、そこを優先的な保存先とするように改善します。

### 3. `RssService.cs` の小修正
- `GenerateAllFeeds` 内で、相対パス指定だけでなく `PathHelper` を通してパスを解決するようにし、保存場所を確実に制御します。

## 実施手順

### [MODIFY] [Program.cs](file:///c:/Users/uchij/Desktop/polite/Program.cs)
- `MainAsync` 内の巡回後のフィード更新処理を、`hasGlobalUpdates` が `false` でも実行するように変更。
- 明示的な `--run` オプションのハンドリング（現在はフォールスルー）。

### [MODIFY] [PathHelper.cs](file:///c:/Users/uchij/Desktop/polite/Services/PathHelper.cs)
- プロジェクトルートを検索する `FindProjectRoot()` ヘルパーメソッドの追加。
- `ResolvePath` で、ファイル未存在時のデフォルトをプロジェクトルートに変更。

### [MODIFY] [RssService.cs](file:///c:/Users/uchij/Desktop/polite/Services/RssService.cs)
- `GenerateAllFeeds` 内の `"feed.xml"` 指定を `PathHelper.ResolvePath("feed.xml")` に変更。

## 検証計画
- ローカル環境で実行し、`feed.xml` が正しく更新されることを確認。
- GitHub Actions を模した環境（ルートから bin 内の exe を実行）で、ファイルがルートに生成されるか確認。
