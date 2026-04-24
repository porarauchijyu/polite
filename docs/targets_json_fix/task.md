# タスクリスト: targets.json 同期問題の修正

- [x] `Program.cs` の `LoadConfigurationAsync` を改善
    - [x] 全てのフィールド（セレクタ等）を読み込むように修正
    - [x] JSON デシリアライズ時の `null` チェックを追加
- [x] 各コマンド（`--add`, `--repair-all`, `--import`）の JSON 読み込みを安全にする
    - [x] `?? new List<TargetConfig>()` の追加
- [x] DB の内容を `targets.json` に書き出す同期ユーティリティの実装（または既存コマンドの強化）
- [x] CSV インポート処理 （`HandleImportCommandAsync`）の改善
    - [x] BOM（UTF-8）の除去処理を追加
    - [x] インポート前の DB 同期処理を追加
    - [x] 既存エントリの更新（マージ）をサポート
- [/] パス解決の安定化とファイル配置の修正
    - [ ] `targets.json` をルートに移動・復元
    - [ ] `Program.cs` でパス解決ロジックを改善
    - [ ] `DatabaseService.cs` でパス解決ロジックを改善
- [ ] 最終動作確認
