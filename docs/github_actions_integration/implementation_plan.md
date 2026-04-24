# GitHub Actions 自動定期巡回 実装計画

本アプリを GitHub Actions で定期的に実行し、RSSフィードを自動更新する仕組みを構築します。

## ユーザーレビューが必要な項目
- **リポジトリの構成**: 実行ファイルをビルドした状態でコミットするか、アクション内でビルドするかを選択できます。今回は「アクション内でビルド」し、最新のコードで実行する構成にします。
- **実行頻度**: デフォルトでは「1時間おき」に設定しますが、調整可能です。

## 提案される変更

### GitHub Actions Workflow [NEW]

#### [NEW] [rss-generator-run.yml](file:///c:/Users/uchij/Desktop/polite/.github/workflows/rss-generator-run.yml)
- 以下のステップを含むワークフローを作成します：
    1. リポジトリのチェックアウト。
    2. .NET Framework ビルド環境のセットアップ。
    3. 依存関係の復元とビルド。
    4. アプリの実行（`RssGenerator.exe --run`）。
    5. 生成された `feed.xml` と `feed.sqlite` などをリポジトリにプッシュ。

### CrawlerService [Component]

#### [MODIFY] [CrawlerService.cs](file:///c:/Users/uchij/Desktop/polite/Services/CrawlerService.cs)
- CI環境（GitHub Actions）を検知した場合、強制的に `Headless = true` で起動するように調整します。

## オープンな質問
- [IMPORTANT] GitHub 上でこのプロジェクトをどのように管理されていますか？（プライベートリポジトリを推奨します）
- [IMPORTANT] 実行ファイルをすでに GitHub にアップロードされていますか？まだの場合は、このワークフローでビルドからすべて行えるように設定します。

## 検証計画

### 自動テスト
- GitHub Actions のコンソール上でログを確認し、ブラウザの起動とサイトの巡回が正常に行われているか確認。

### 手動確認
- 自動実行後にリポジトリ内の `feed.xml` や `feed.sqlite` が更新されていることを確認。
