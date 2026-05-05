# タスクリスト：RSSジェネレーター Webアプリ移行

## フェーズ 1: 基盤構築 (Web App Foundation)
- [ ] `web` フォルダの作成と Next.js プロジェクトの初期化
- [ ] プレミアム・デザインシステムの構築 (CSS 共通変数、リセット)
- [ ] 基本レイアウトコンポーネント (Header, Footer, Layout) の作成

## フェーズ 2: スクレイパー移植 (Scraper Porting)
- [ ] Playwright 環境の構築と基本的なクローラーの実装
- [ ] 既存の `targets.json` 読み込みロジックの移植
- [ ] 推論エンジン (`InferenceService.js`) の統合
- [ ] RSS (XML) および記事データ (JSON) の出力機能の実装

## フェーズ 3: ダッシュボード実装 (UI Development)
- [ ] Home 画面 (統合記事フィード) の実装
- [ ] Sites 画面 (登録サイト一覧) の実装
- [ ] 記事の検索・フィルタリング機能の実装

## フェーズ 4: デプロイと自動化 (DevOps)
- [ ] GitHub Actions ワークフローの作成 (`.github/workflows/deploy.yml`)
- [ ] 定期実行スケジュールのテスト
- [ ] GitHub Pages での公開確認

## フェーズ 5: ブラッシュアップ
- [ ] ローディングアニメーション、スケルトンスクリーンの追加
- [ ] パフォーマンス最適化
- [ ] 最終的なバグ修正と動作確認
