# RSSジェネレーター・ユーティリティ再作成計画

## 概要
現在のプロジェクトを整理し、より堅牢で使いやすいRSS生成システムとして再構築します。
特に、欠落している `targets.json` の作成と、スクレイピング精度の向上、コンソール出力の改善（ユーザー体験の向上）に焦点を当てます。

## ユーザーレビュー要求事項
> [!IMPORTANT]
> **技術スタックの維持**
> .NET Framework 4.8 および C# 7.3 を引き続き使用しつつ、コードの品質を「プレミアム」レベルまで引き上げます。
> 
> **Web UIへのアップグレード（提案）**
> もしコンソールアプリではなく、React/Next.js 等を使用したモダンなWeb管理画面が必要な場合はお知らせください。今回は既存のコンソールアプリの再構築を優先します。

## 提案される変更

### 1. プロジェクト構成の整理
- 依存関係の最新化
- エラーハンドリングの強化

### 2. 設定ファイルの再作成
#### [NEW] [targets.json](file:///c:/Users/uchij/Desktop/polite/targets.json)
- Osaka Metro, 阪急電鉄, Yahoo! ニュースの初期設定を含めます。

### 3. ロジックの改善
#### [MODIFY] [CrawlerService.cs](file:///c:/Users/uchij/Desktop/polite/Services/CrawlerService.cs)
- ブラウザのインスタンス管理を最適化し、リソースリークを防ぎます。
- タイムアウトやリトライ処理を追加します。

#### [MODIFY] [ExtractorService.cs](file:///c:/Users/uchij/Desktop/polite/Services/ExtractorService.cs)
- セレクターの柔軟性を高め、タイトルとリンクが正しくペアになるように改善します。

#### [MODIFY] [Program.cs](file:///c:/Users/uchij/Desktop/polite/Program.cs)
- コンソール出力を色付けし、進捗状況を視覚的にわかりやすくします。

## オープンな質問
> [!IMPORTANT]
> 1. 現在の .NET Framework 4.8 構成を維持してよろしいでしょうか？
> 2. `task.md` 等のドキュメントを作成して進めてもよろしいでしょうか？（ユーザー送信ルールに従い確認）

## 検証計画
### 手動確認
- `targets.json` を編集してサイトを追加。
- 実行時に各サイトから正しくデータが抽出され、`feed.xml` が更新されることを確認。
- 重複した記事が RSS に追加されないことを確認。
