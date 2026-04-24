# 修復計画: targets.json の同期と初期化の改善

`targets.json` が空の状態で作成されたり、データが消失したりする問題を修正します。また、DB の新しい列（セレクタ等）と `targets.json` が正しく同期されるようにプログラムを改善します。

## 修正が必要な点
- **デシリアライズの安全確保**: `targets.json` が空（0バイト）や存在しない場合に `JsonConvert.DeserializeObject` が `null` を返し、その後の処理でクラッシュする箇所を修正します。
- **DB との完全同期**: `LoadConfigurationAsync` で `ContainerSelector` や `DescriptionSelector` などの新しいフィールドが無視されていたため、起動時に DB の値が `null` で上書きされてしまう問題を修正します。
- **書き込み処理の堅牢化**: JSON 書き込み時に例外が発生してファイルが破損しないよう、安全な書き込みを心がけます。

## 提案する変更内容

### [Program.cs](file:///c:/Users/uchij/Desktop/polite/Program.cs)

#### [MODIFY] `LoadConfigurationAsync`
- `targets.json` から全てのフィールド（`ContainerSelector`, `DescriptionSelector`, `DateSelector`）を読み込むように修正します。
- `?? new List<TargetConfig>()` を使用して `null` 参照を防ぎます。

#### [MODIFY] `HandleAddCommandAsync`, `HandleRepairAllCommandAsync`, `HandleImportCommandAsync`
- JSON 読み込み時の `null` チェックを厳格化します。
- `?? new List<TargetConfig>()` を追加します。

#### [NEW] `HandleSyncCommandAsync` (オプション)
- DB の最新情報を `targets.json` に強制的に書き出すコマンドを追加し、不整合を解消できるようにします。

## 検証計画
- `targets.json` を削除した状態でプログラムを実行し、警告が出ることを確認。
- 新しいサイトを `--add` し、`targets.json` に全てのセレクタが保存されることを確認。
- 既存の `targets.json` に手動で `DescriptionSelector` を書き込み、起動時に DB に正しく反映されるか確認。

## ユーザーへの質問
1. 「空で出来上がる」というのは、具体的にどのコマンド（例: `--add`, `--repair-all`）を実行したとき、あるいはどのタイミングで発生しますか？
2. `targets.json` を削除してから実行した場合、自動的に作成されることを期待されていますか？（現在は自動作成は特定のコマンド実行時のみです）
