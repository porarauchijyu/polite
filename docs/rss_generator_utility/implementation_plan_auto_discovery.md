# 監視対象セレクタ自動推論機能（Auto-Discovery）の実装計画

## 概要
ユーザーが `targets.json` を手動で編集・解析する手間を省くため、URLを入力するだけで最適な `TitleSelector` と `LinkSelector` を自動的に推論し、設定に追加する機能を実装します。

## ユーザーレビュー要求事項
> [!IMPORTANT]
> **推論の精度**
> 100%の精度を保証することは難しいため、推論結果をユーザーに提示し、承認を得てから `targets.json` に保存するフローとします。
> 
> **実行環境**
> 引き続き PuppeteerSharp を使用し、ブラウザ上で JavaScript を実行して構造解析を行います。

## 提案される変更

### 1. 推論エンジンの新規追加
#### [NEW] [InferenceService.cs](file:///c:/Users/uchij/Desktop/polite/Services/InferenceService.cs)
- ページ内の全リンクを走査し、ニュース項目と思われる「クラスタ」を特定するロジックを実装します。
- スコアリング（項目数、日付の有無、文字数、URLパターン）に基づき、最適なセレクタを算出します。

### 2. クローラー機能の拡張
#### [MODIFY] [CrawlerService.cs](file:///c:/Users/uchij/Desktop/polite/Services/CrawlerService.cs)
- ブラウザ内でカスタム JavaScript を実行し、その結果を取得するメソッドを追加します。

### 3. コマンドライン・インターフェースの更新
#### [MODIFY] [Program.cs](file:///c:/Users/uchij/Desktop/polite/Program.cs)
- 新しいモード（例：`--add [URL]`）を追加します。
- 推論されたセレクタを表示し、ユーザーが `y` を入力した場合に `targets.json` を更新します。

---

## オープンな質問
> [!IMPORTANT]
> 1. 推論機能は「コマンドライン引数（`--add URL`）」として実装してよろしいでしょうか？
> 2. それとも、アプリ起動時に「URLを入力してください」と対話的に聞く形式がよろしいでしょうか？

## 検証計画
### 自動テスト
- 代表的なニュースサイト（大阪メトロ、JR西日本、Yahoo!ニュース等）で、正しいセレクタが推論されるかを確認。

### 手動確認
- 新しいURLに対して `--add` コマンドを実行し、`targets.json` が正しく更新され、その後の巡回で記事が取得できることを確認。
