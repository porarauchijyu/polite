# ブラウザファイルサイズの最小化計画

PuppeteerSharp がデフォルトでダウンロードする Chromium は約 700MB と非常に大きいため、システムのインストール済みブラウザ（Chrome または Edge）を優先的に使用するように変更し、アプリのフットプリントを最小化します。

## ユーザーレビューが必要な事項
> [!IMPORTANT]
> この変更により、実行環境に **Google Chrome** または **Microsoft Edge** がインストールされている必要があります。一般的な Windows 環境であれば Edge が標準搭載されていますが、特殊な環境では動作しなくなる可能性があります。

## 変更内容

### CrawlerService の改善

#### [MODIFY] [CrawlerService.cs](file:///c:/Users/uchij/Desktop/polite/Services/CrawlerService.cs)
- システムのレジストリや標準パスから `chrome.exe` または `msedge.exe` を自動検出するロジックを追加します。
- `InitializeAsync` での `BrowserFetcher.DownloadAsync()` を停止（またはオプション化）します。
- `Puppeteer.LaunchAsync` 時に `ExecutablePath` を指定するようにします。

## 導入後のクリーンアップ
既存の `bin/Debug/net48/runtimes` フォルダ（約 720MB）は、手動またはスクリプトで削除することで容量を大幅に削減できます。

## 検証計画
### 手動確認
- アプリケーションを起動し、システムのブラウザを使用して正常にクローリングができることを確認します。
- `runtimes` フォルダを削除した状態で動作することを確認します。
