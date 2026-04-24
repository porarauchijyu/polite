# ブラウザサイズ最小化の完了報告

PuppeteerSharp が独自の Chromium をダウンロードしてディスク容量を圧迫（約 700MB）していた問題を、既存のシステムブラウザを活用する方式に変更することで解決しました。

## 変更内容

### 1. システムブラウザの自動検出ロジックの実装
[CrawlerService.cs](file:///c:/Users/uchij/Desktop/polite/Services/CrawlerService.cs) に、Windows のレジストリや標準パスから **Google Chrome** または **Microsoft Edge** の実行パスを自動で取得するロジックを追加しました。

### 2. PuppeteerSharp の起動設定変更
ブラウザ起動時に `ExecutablePath` を明示的に指定するように変更し、独自のダウンロード済み Chromium ではなく、検出したシステムブラウザを使用するようにしました。

### 3. フォールバック処理
万が一、システムに Chrome/Edge が見つからない場合は、従来通り最小限のバイナリを `runtimes` フォルダにダウンロードして継続動作するよう、安定性を確保しています。

## 検証結果
- **ビルド確認**: `dotnet build` により、コンパイルエラーがないことを確認しました。
- **ロジック確認**: レジストリキー (`HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\chrome.exe`) を優先し、非破壊なパス確認を行っています。

## 推奨される後続作業
> [!TIP]
> 既にダウンロードされている `bin/Debug/net48/runtimes` フォルダや、プロジェクト直下の `runtimes` フォルダは、手動で削除していただいて問題ありません（削除後もシステムブラウザで動作します）。これにより、**約 700MB 以上** の空き容量が確保されます。
