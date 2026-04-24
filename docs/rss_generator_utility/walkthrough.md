# 依存ファイル集約 (runtimes フォルダー化) 完了報告

実行ファイル（EXE）周辺を整理し、すべての依存ファイルを `runtimes` フォルダーへ集約する構成への変更が完了しました。

## 実施した主な変更

### 1. アセンブリ探索パスの設定
- **ファイル**: [App.config](file:///c:/Users/uchij/Desktop/polite/App.config)
- **内容**: `<probing privatePath="runtimes" />` を追加。これにより、プログラム起動時に `runtimes` サブフォルダー内の DLL を自動的に読み込むようになります。

### 2. ブラウザ保存場所の統合
- **ファイル**: [CrawlerService.cs](file:///c:/Users/uchij/Desktop/polite/Services/CrawlerService.cs)
- **内容**: Puppeteer のブラウザ保存先を `bin/runtimes/Chrome` 等に変更。実行ディレクトリ直下にブラウザフォルダーが生成されるのを防ぎます。

### 3. ビルドプロセスの自動整理
- **ファイル**: [RssGenerator.csproj](file:///c:/Users/uchij/Desktop/polite/RssGenerator.csproj)
- **内容**: ビルド後に `*.dll` や `x64/x86` フォルダー、`Chrome` フォルダーを `runtimes` 内へ自動移動するビルドターゲット `OrganizeOutput` を追加しました。

## 動作確認の結果
- **ビルド出力**: `bin/Debug/net48` 直下が EXE と設定ファイルのみになり、極めてクリーンな状態であることを確認しました。
- **実行確認**: `.\RssGenerator.exe --help` を実行し、`runtimes` 内の DLL が正しくロードされ、ヘルプメッセージが表示されることを確認しました。
- **ブラウザ起動**: 大阪メトロの解析テストを通じ、`runtimes` 内のブラウザが正常に起動・動作することを確認しました。

---
> [!TIP]
> 今後、新しい NuGet パッケージを追加した場合も、ビルド時に自動的に `runtimes` へ整理されます。実行ファイルを配布する際は、EXE と `runtimes` フォルダーをセットで配布してください。
