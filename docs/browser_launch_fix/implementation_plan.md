# ブラウザ起動エラーの改善計画

セキュリティが厳しい環境でもブラウザを確実に起動できるよう、Puppeteer の起動オプションを強化し、実行環境への依存を減らす修正を行います。

## ユーザーレビューが必要な項目
- **UserDataDir の場所**: デフォルトではアプリケーション実行フォルダ内の `browser_data` を使用するようにします。これにより、システムのテンポラリフォルダへのアクセス制限を回避します。
- **Pipe = true**: 標準的な WebSocket 通信ではなくパイプ通信を使用します。これはセキュリティソフトによるローカルポートの制限を回避するために有効です。

## 提案される変更

### CrawlerService [Component]

#### [MODIFY] [CrawlerService.cs](file:///c:/Users/uchij/Desktop/polite/Services/CrawlerService.cs)
- `LaunchBrowserAsync` メソッドを修正し、以下のオプションを追加します：
    - `Pipe = true`: セキュリティやプロキシ環境での接続問題を回避。
    - `UserDataDir`: アプリケーションの作業ディレクトリ内に明示的に指定（`Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "browser_data")`）。
    - `Args` に以下のフラグを追加：
        - `--disable-gpu`: グラフィック関連のエラー回避。
        - `--disable-dev-shm-usage`: メモリ共有の制限回避。
        - `--disable-setuid-sandbox`: サンドボックス制限の緩和。
        - `--no-first-run`, `--no-default-browser-check`: 余計なダイアログを抑制。
- 起動失敗時の例外ハンドリングを強化し、エラー内容がより詳細にログへ残るようにします。

### AppSettings [Component]

#### [MODIFY] [AppSettings.cs](file:///c:/Users/uchij/Desktop/polite/Services/AppSettings.cs)
- 起動の安定性を高めるための追加設定（必要であれば）を追加。現時点では `Pipe` 通信をデフォルトにします。

## オープンな質問
- [IMPORTANT] ブラウザのパスは見つかっているが、具体的にどのようなエラー（アクセス拒否、プロセスが終了した、タイムアウトなど）が出ていますか？もしログがあれば教えていただけるとより確実な対策が打てます。

## 検証計画

### 自動テスト
- 修正後のコードで、以前起動に失敗していた環境での起動テストを依頼。

### 手動確認
- `browser_data` フォルダが正しく生成され、そこにブラウザのプロファイルが作られているか確認。
- デバッグコンソールで `[Crawler] ブラウザを起動中...` の後にエラーが出ずに正常にページ取得ができることを確認。
