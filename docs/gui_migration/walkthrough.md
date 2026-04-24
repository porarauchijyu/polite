# 修正内容の確認：RSS Generator Utility GUI 化 & ビジュアルセレクタ追加

RSS Generator Utility を従来の CLI インターフェースから、モダンな WPF GUI アプリケーションへと大幅にアップグレードしました。これにより、自動推論が難しいサイトでも、実際のサイト画面を見ながら直感的に抽出対象を指定できるようになりました。

## 実施した主な変更

### 1. プレミアムな GUI ダッシュボードの構築
- **MainWindow.xaml**: ダークモードを基調とした洗練されたデザインのダッシュボードを新規作成しました。
- **サイト一覧管理**: 登録されているニュースサイトをカード形式で一覧表示し、個別の巡回や設定の修復、削除がボタン一つで行えます。
- **ログエリア**: 巡回中の処理状況（新着情報の取得数など）を下部にリアルタイムで表示します。

### 2. ビジュアル・セレクタ (Visual Selector) モードの実装
- **ブラウザ連携**: PuppeteerSharp を利用し、画面表示ありのブラウザを制御。
- **ポイント＆クリック選択**: ブラウザ内に専用ツール（SelectorGadget.js）を注入。要素をマウスオーバーで強調表示し、クリックするだけで「ニュースの枠」「タイトル」「日付」「概要」のセレクタをキャプチャします。
- **ハイブリッド・アプローチ**: サイト追加時にはまず自動推論（InferenceService）を試み、必要に応じてワンクリックでビジュアルモードへ移行できます。

### 3. ハイブリッド起動 (CLI & GUI) の維持
- **Program.cs**: 起動時の引数を解析し、引数がない場合は GUI を開き、引数がある場合は従来の CLI コマンド（自動巡回など）を実行する「ハイブリッド仕様」を実装しました。これにより、既存のバッチ処理（タスクスケジューラ等）を壊すことなく GUI の恩恵を受けられます。

---

## 変更ファイル

### 基盤・UI
- [RssGenerator.csproj](file:///c:/Users/uchij/Desktop/polite/RssGenerator.csproj): WPF の有効化、WinExe への出力変更。
- [App.xaml](file:///c:/Users/uchij/Desktop/polite/App.xaml) / [App.xaml.cs](file:///c:/Users/uchij/Desktop/polite/App.xaml.cs): WPF エントリポイント。
- [MainWindow.xaml](file:///c:/Users/uchij/Desktop/polite/MainWindow.xaml) / [MainWindow.xaml.cs](file:///c:/Users/uchij/Desktop/polite/MainWindow.xaml.cs): メイン画面.
- [AddSiteWindow.xaml](file:///c:/Users/uchij/Desktop/polite/AddSiteWindow.xaml) / [AddSiteWindow.xaml.cs](file:///c:/Users/uchij/Desktop/polite/AddSiteWindow.xaml.cs): 新規サイト追加・解析画面.

### サービス・スクリプト
- [VisualSelectorService.cs](file:///c:/Users/uchij/Desktop/polite/Services/VisualSelectorService.cs): ブラウザとの通信・制御。
- [SelectorGadget.js](file:///c:/Users/uchij/Desktop/polite/Scripts/SelectorGadget.js): ブラウザ内のセレクタ抽出ツール。
- [CrawlerService.cs](file:///c:/Users/uchij/Desktop/polite/Services/CrawlerService.cs): 非ヘッドレスモード対応。

---

## 検証結果
- **GUI 起動**: `RssGenerator.exe` を引数なしで実行すると、プレミアムデザインのウィンドウが立ち上がることを確認。
- **自動推論 & ビジュアル選択**: URL を入力して「解析」後、自動でセレクタが提案され、必要に応じてブラウザを開いて手動指定できることを確認。
- **JSON同期**: 設定を保存すると、`targets.json` に正しく書き出されることを確認。
