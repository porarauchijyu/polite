# ニュース公表日の抽出および表示機能の実装計画

ニュースが「いつ公表されたか」を取得し、`--read` コマンドで一覧表示する際にタイトルの先頭へ付加する機能を実装します。

## ユーザーレビューが必要な事項
> [!IMPORTANT]
> - `targets.json` およびデータベースの `Targets` テーブルに `DateSelector` カラムが追加されます。
> - `dateTime.TryParse` を使用して「2024年4月15日」や「2024.04.15」などの日本語フォーマットを解析しますが、特殊な記述（例：「昨日」「さっき」など）には対応できない場合があります。

## 変更内容

### 1. データ構造の拡張

#### [MODIFY] [DatabaseService.cs](file:///c:/Users/uchij/Desktop/polite/Services/DatabaseService.cs)
- `TargetConfig` クラスに `DateSelector` プロパティを追加。
- データベース移行ロジックを更新し、`Targets` テーブルに `DateSelector` カラムを追加。

### 2. 抽出ロジックの強化

#### [MODIFY] [ExtractorService.cs](file:///c:/Users/uchij/Desktop/polite/Services/ExtractorService.cs)
- `ExtractItems` に `dateSelector` 引数を追加。
- 指定されたセレクタから日付文字列を取得し、`DateTime` 型に変換するロジックを実装。
- 日本語文化圏 (`ja-JP`) の日付解析をサポート。

### 3. CLI および表示処理の更新

#### [MODIFY] [Program.cs](file:///c:/Users/uchij/Desktop/polite/Program.cs)
- `--add` および `--repair` のフローで、日付のセレクタを入力できるように更新。
- `HandleReadCommandAsync` において、`PublishedDate` が取得できている場合は `[2024/04/15] タイトル` の形式で出力するように変更。

## 検証計画
### 手動確認
- 京阪や大阪メトロのサイトで日付が入っている要素（例: `time` タグや `span.date`）をセレクタで指定し、正しく解析されるか確認。
- `--read` を実行し、コンソール出力の文頭に日付が付加されていることを確認。
- 日付が取得できない場合に `PublishedDate` が `DateTime.Now` にフォールバックされるか確認。
