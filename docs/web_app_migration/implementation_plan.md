# RSSジェネレーター Webアプリ移行設計書 (Migration Plan)

本プロジェクトを、.NET Framework (WPF) から **Next.js + GitHub Actions** ベースの Web アプリケーションへ移行するための設計書です。

## 1. 概要
既存の RSS 生成機能を GitHub Actions 上で動作する Node.js スクリプトに移植し、取得したデータを GitHub Pages で公開するダッシュボードで閲覧可能にします。これにより、OS に依存せず、ブラウザだけで完結する RSS 運用環境を構築します。

## 2. システムアーキテクチャ

```mermaid
graph TD
    A[GitHub Actions (Cron)] --> B[Scraper (Node.js + Playwright)]
    B --> C[Update targets.json / feeds.xml / articles.json]
    C --> D[Next.js Static Build]
    D --> E[GitHub Pages (Public Web Site)]
    E --> F[User Browser]
```

## 3. 技術スタック

| 要素 | 技術 | 選定理由 |
| :--- | :--- | :--- |
| **Scraper** | Node.js + Playwright | GitHub Actions (Linux) でのブラウザ動作が非常に安定しているため。 |
| **Frontend** | Next.js (Static Export) | 高性能な UI を構築でき、GitHub Pages との相性が良いため。 |
| **Styling** | Vanilla CSS (CSS Modules) | 柔軟なカスタマイズとプレミアムな質感を実現するため。 |
| **Storage** | Git Repository (JSON/XML) | 無料かつ履歴管理が可能なため。 |

## 4. 各コンポーネントの設計

### 4.1. スクレイパー (Scraper Core)
*   **入力**: `targets.json` (既存のものをそのまま利用、または拡張)
*   **エンジン**: Playwright。ヘッドレス環境でのボット対策（Stealth 設定）を継承。
*   **出力**: 
    *   `public/feeds/*.xml`: 各サイトごとの RSS フィード
    *   `public/data/articles.json`: フロントエンド表示用の統合データ
*   **ロジック**: 既存の `InferenceService.js` (JavaScriptベースの推論エンジン) をそのまま再利用し、セレクタの自動推定を継続。

### 4.2. フロントエンド (Dashboard UI)
*   **ページ構成**:
    *   `Home`: 全記事の統合タイムライン (最新順)
    *   `Sites`: 登録サイト一覧とステータス確認
    *   `Settings`: `targets.json` の管理用 UI
*   **デザイン方針**:
    *   **プレミアム・ダークモード**: 深みのあるグレーと鮮やかなアクセントカラー。
    *   **インタラクティブ**: ホバーエフェクト、スムーズな遷移アニメーション。
    *   **レスポンシブ**: PC、タブレット、スマホのすべてで最適化。

### 4.3. 自動化フロー (GitHub Actions)
*   **スケジュール**: 1時間おきに実行。
*   **ワークフロー手順**:
    1.  `actions/checkout` (コード取得)
    2.  `actions/setup-node` (環境構築)
    3.  `npm install` (依存関係)
    4.  `npm run scrape` (スクレイピング実行 & ファイル更新)
    5.  `npm run build` (Next.js 静的ファイル生成)
    6.  `peaceiris/actions-gh-pages` (gh-pages ブランチへのデプロイ)

## 5. 移行の難易度とリスク
*   **難易度**: 中程度。C# から TypeScript へのロジック移植がメイン作業となります。
*   **リスク**:
    *   **サイト側の変更**: セレクタ推論が失敗する可能性があるが、既存のロジックを継承することでリスクを最小化。
    *   **GitHub Actions の制限**: 月間 2,000 分の無料枠内で十分収まる設計にします（1回数分程度）。

## 6. 実装フェーズ

### フェーズ 1: 基盤構築 (Web App Foundation)
*   Next.js プロジェクトの初期化
*   基本デザインシステム（CSS）の実装

### フェーズ 2: スクレイパー移植
*   Playwright によるクロール基盤の作成
*   C# 版 `ExtractorService` と `InferenceService` の移植

### フェーズ 3: ダッシュボード実装
*   記事一覧、サイト一覧画面の作成
*   RSS フィード出力機能の実装

### フェーズ 4: デプロイとテスト
*   GitHub Actions の設定
*   GitHub Pages での公開確認

---

## ユーザー様への確認事項
> [!IMPORTANT]
> 1. **ディレクトリ構成**: 新しい Web アプリは `web` フォルダ内に作成し、既存の .NET コードと共存させる形でよろしいでしょうか？それとも、完全に Web 版で置き換える（プロジェクト直下に配置する）形がよろしいでしょうか？
> 2. **デザイン**: ダークモードをデフォルトにしてもよろしいでしょうか？
