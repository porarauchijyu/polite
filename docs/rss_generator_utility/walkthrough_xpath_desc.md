# 説明文の追加と XPath サポートの導入完了報告

PolitePol の構成を参考に、ニュース項目の「説明文（概要）」の抽出機能、および CSS セレクタに加えてより精度の高い XPath を利用できる機能を実装しました。

## 実施内容

### 1. 説明文（Description）のサポート
- **データ構造**: `FeedItem` に `Description` を追加し、データベースの監視対象サイト設定に `DescriptionSelector` カラムを追加しました。
- **RSS 出力**: 取得した説明文を RSS フィードの `<description>` タグに出力するように [RssService.cs](file:///c:/Users/uchij/Desktop/polite/Services/RssService.cs) を更新しました。

### 2. XPath セレクタへの自動対応
[ExtractorService.cs](file:///c:/Users/uchij/Desktop/polite/Services/ExtractorService.cs) において、入力されたセレクタが `/` や `./` で始まる、あるいは `(` で始まる場合に、**自動的に XPath として処理**するロジックを導入しました。これにより、CSS では切り出しにくい複雑な階層構造（例: `li[1]` の値と `li[3]` の値を組み合わせる等）にも対応可能です。

### 3. CLI インターフェースの更新
- **サイト追加時**: `--add` コマンド実行時に、タイトル・リンクのセレクタに加えて「説明文のセレクタ」を入力できるようになりました。
- **保存処理**: 取得元のサイト名やコンテナ設定と併せて、説明文の設定も `targets.json` に正しく保存されます。

## 検証結果
- **ビルド確認**: `dotnet build` により、エラーなく正常にビルドできることを確認しました。
- **データベース移行**: アプリ起動時に SQLite データベースのスキーマが自動で更新（カラム追加）されることを確認しました。

## 使い方
新しくサイトを追加する際や、既存サイトを `--repair` する際に、説明文用の CSS または XPath を入力してください。
例（CSS）: `.news-text`
例（XPath）: `./div[@class='summary']`

これにより、作成される `feed.xml` に記事の概要が含まれるようになります。
