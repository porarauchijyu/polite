# 説明文の追加と XPath サポート導入計画

PolitePol の柔軟性を参考に、各ニュース項目の「説明文（概要）」の抽出機能を実装し、CSS セレクタに加えてより精度の高い XPath も利用可能にします。

## ユーザーレビューが必要な事項
> [!IMPORTANT]
> - `targets.json` とデータベースのスキーマが更新されます（`DescriptionSelector` カラムの追加）。
> - 抽出された RSS (`feed.xml`) に `<description>` 項目が含まれるようになります。
> - セレクタが `/` または `./` で始まる、あるいは `(` で始まる場合は自動的に XPath として処理されます。

## 変更内容

### 1. データ構造の拡張 (DatabaseService & ExtractorService)

#### [MODIFY] [ExtractorService.cs](file:///c:/Users/uchij/Desktop/polite/Services/ExtractorService.cs)
- `FeedItem` クラスに `Description` プロパティを追加します。
- XPath か CSS かを判定して値を抽出するユーティリティメソッドを追加します。

#### [MODIFY] [DatabaseService.cs](file:///c:/Users/uchij/Desktop/polite/Services/DatabaseService.cs)
- `TargetConfig` クラスに `DescriptionSelector` プロパティを追加します。
- `Targets` テーブルに `DescriptionSelector` カラム、`FeedItems` テーブルに `Description` カラムを追加する移行処理を実装します。

### 2. 抽出・生成処理の更新

#### [MODIFY] [ExtractorService.cs](file:///c:/Users/uchij/Desktop/polite/Services/ExtractorService.cs)
- `ExtractItems` でタイトル・リンクに加え、説明文の抽出も行います。
- CSS セレクタだけでなく XPath によるノード選択に対応します。

#### [MODIFY] [RssService.cs](file:///c:/Users/uchij/Desktop/polite/Services/RssService.cs)
- 生成される RSS 項目の概要（Summary）に、抽出した説明文を設定します。

### 3.推論エンジンと CLI の更新

#### [MODIFY] [InferenceService.cs](file:///c:/Users/uchij/Desktop/polite/Services/InferenceService.cs)
- `InferenceResult` に `DescriptionSelector` （将来的な拡張用）を含められるようにします。

#### [MODIFY] [Program.cs](file:///c:/Users/uchij/Desktop/polite/Program.cs)
- `--add` や `--repair` 時、およびインポート時に新しいセレクタ項目を扱えるように更新します。

## 検証計画
### 手動確認
- `--add` コマンドで説明文のセレクタ（例: `.news-summary` や XPath）を指定し、正しく抽出されるか確認。
- `feed.xml` を開き、各項目に正しい概要が含まれているか確認。
- 前回の `ContainerSelector` と併用して、リスト内から正確に情報が抜けるかテスト。
