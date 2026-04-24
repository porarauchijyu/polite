# 推論エンジンおよび抽出ロジックの強化計画

現在の「リンク単体」ベースの解析から、「ニュース項目のまとまり（コンテナ）」を意識した解析にアップグレードします。これにより、大阪メトロ以外の多様なサイト構造への対応力を高め、誤検出を減らします。

## ユーザーレビューが必要な事項
> [!IMPORTANT]
> - `targets.json` のスキーマに `ContainerSelector` が追加されます。
> - データベースの `Targets` テーブルにもカラムが追加されます。
> - 既存の登録サイトについては、再度 `--repair` を実行することで新しいコンテナベースの抽出が有効になります。

## 変更内容

### 1. 推論ロジックの強化 (InferenceService)

#### [MODIFY] [InferenceService.cs](file:///c:/Users/uchij/Desktop/polite/Services/InferenceService.cs)
- JavaScriptエンジンを更新し、ニュース項目（リンク等）の「共通の親要素」を特定するロジックを追加します。
- 項目の「密度」や「構造の繰り返し」を検証し、最も確からしいリストコンテナを `ContainerSelector` として返却します。

### 2. データ構造と永続化の更新 (DatabaseService & TargetConfig)

#### [MODIFY] [DatabaseService.cs](file:///c:/Users/uchij/Desktop/polite/Services/DatabaseService.cs)
- `TargetConfig` クラスに `ContainerSelector` プロパティを追加します。
- SQLite の `Targets` テーブルに `ContainerSelector` カラムを追加する移行処理（ALTER TABLE）を実装します。
- `RegisterTarget` および `GetTargets` メソッドで新フィールドを処理できるよう更新します。

### 3. 抽出ロジックの改善 (ExtractorService)

#### [MODIFY] [ExtractorService.cs](file:///c:/Users/uchij/Desktop/polite/Services/ExtractorService.cs)
- `ExtractItems` で `containerSelector` を受け取るように変更します。
- コンテナが指定されている場合、まずそのコンテナを特定し、その「配下のみ」からアイテムを抽出することで、ページ内の関係ないリンク（フッターやサイドバー）の混入を防ぎます。

### 4. CLIインターフェースの更新 (Program.cs)

#### [MODIFY] [Program.cs](file:///c:/Users/uchij/Desktop/polite/Program.cs)
- `--add` および `--repair` の処理フローを更新し、推論された `ContainerSelector` をユーザーに提示・保存するようにします。
- `targets.json` への読み書き処理に `ContainerSelector` を含めます。

## 検証計画
### 自動テスト
- `dotnet build` でのコンパイル確認。

### 手動確認
- 新しいサイトを `--add` し、コンテナセレクタが正しく推論されるか確認。
- 大阪メトロ等の既存サイトで `--repair` を実行し、構造が正しく保存されるか確認。
- 抽出時に無関係な領域（ヘッダー・フッター）のリンクが除外されているか確認。
