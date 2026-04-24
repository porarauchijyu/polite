# ブラウザ検出機能の強化と手動設定の導入

「ブラウザが見つかりませんでした」というエラーが発生している問題に対し、ブラウザ検出ロジックのロバスト性を向上させ、ユーザーが手動でパスを指定できる仕組みを導入します。

## ユーザーレビューが必要な項目

> [!IMPORTANT]
> **settings.json の導入**
> 実行ファイルと同じディレクトリに `settings.json` を生成し、そこにブラウザのパスなどを保存できるようにします。ユーザーは必要に応じてこのファイルを直接編集してブラウザを指定できるようになります。

## 提案される変更点

### 設定管理の導入

#### [NEW] [AppSettings.cs](file:///c:/Users/uchij/Desktop/polite/Services/AppSettings.cs)
設定情報を保持するデータクラスと、その読み書きを行うロジックを実装します。

#### [NEW] [settings.json](file:///c:/Users/uchij/Desktop/polite/settings.json)
デフォルト設定を含む JSON ファイルを作成します。

---

### ブラウザ検出ロジックの改善

#### [MODIFY] [CrawlerService.cs](file:///c:/Users/uchij/Desktop/polite/Services/CrawlerService.cs)
- `InitializeAsync` で `settings.json` の `BrowserPath` を最優先でチェックするように変更。
- `GetBrowserPaths` に、より広範な検索（環境変数 PATH からの検索など）を追加。
- ブラウザ起動失敗時のログに、試行したパスとエラーメッセージを詳細に出力するように改善。

#### [MODIFY] [MainWindow.xaml.cs](file:///c:/Users/uchij/Desktop/polite/MainWindow.xaml.cs)
- 起動時やブラウザエラー発生時に、より詳細な情報をログ（GUI上のテキストエリア）に出力するように調整。

## 公開されている質問
- 特にありません。現在の構成で、エラーの詳細を可視化することで、なぜ「他の環境」で失敗しているかの原因特定が容易になります。

## 検証計画

### 自動テスト / 手動検証
- [ ] ブラウザの自動検出が動作することを確認（レジストリ/標準パス）。
- [ ] `settings.json` に無効なパスを書いた場合、エラーログに詳細が表示されることを確認。
- [ ] `settings.json` に有効なパスを書いた場合、それが最優先で使われることを確認。
- [ ] `settings.json` が存在しない場合、デフォルト値で自動生成されることを確認。
