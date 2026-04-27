using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Xml;
using System.Text;

namespace RssGenerator.Services
{
    public class RssService
    {
        public void GenerateRss(List<FeedItem> items, string filePath, string title = "Custom RSS Feed", string description = "RSS Generator Utility による自動生成フィード")
        {
            var feed = new SyndicationFeed(
                title,
                description,
                new Uri("https://github.com/porarauchijyu/polite"),
                "RSSGenerator",
                DateTime.Now
            );

            var syndicationItems = new List<SyndicationItem>();

            foreach (var item in items)
            {
                var sItem = new SyndicationItem(
                    item.Title,
                    item.Description,
                    new Uri(item.Link),
                    item.Link,
                    new DateTimeOffset(item.PublishedDate)
                );
                sItem.Summary = new TextSyndicationContent(item.Description);
                syndicationItems.Add(sItem);
            }

            feed.Items = syndicationItems;

            var settings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                Indent = true,
                NewLineHandling = NewLineHandling.Replace
            };

            // ディレクトリが存在しない場合は作成
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var writer = XmlWriter.Create(filePath, settings))
            {
                Rss20FeedFormatter formatter = new Rss20FeedFormatter(feed);
                formatter.WriteTo(writer);
            }
        }

        public void GenerateAllFeeds(DatabaseService db)
        {
            // 1. 全体まとめフィードの生成
            Console.WriteLine("[RSS] 全体まとめフィードを生成中...");
            var allItems = db.GetLatestItems(100);
            GenerateRss(allItems, "feed.xml");

            // 2. サイト別フィードの生成
            var targets = db.GetTargets();
            
            // カレントディレクトリに feeds フォルダを作成 (GitHub Actions等でルートに保存されるように)
            string feedsDir = "feeds";
            if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RssGenerator.exe")))
            {
                // 実行ファイルと同じ階層に既にあるか、開発環境等での挙動を維持
                // ただし、相対パスで "feeds" と指定すればカレントディレクトリが優先される
            }
            
            if (!Directory.Exists(feedsDir)) Directory.CreateDirectory(feedsDir);

            foreach (var target in targets)
            {
                string safeName = GetSafeFileName(target.Name ?? target.Url);
                string fileName = $"feed_{safeName}.xml";
                string filePath = Path.Combine(feedsDir, fileName);

                Console.WriteLine($"[RSS] 個別フィードを生成中: {target.Name ?? target.Url} -> {fileName}");
                var targetItems = db.GetLatestItemsByTarget(target.Id, 100);
                
                GenerateRss(
                    targetItems, 
                    filePath, 
                    target.Name ?? "Custom Feed", 
                    $"{target.Name ?? target.Url} の最新ニュース"
                );
            }
        }

        private string GetSafeFileName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "unknown";
            
            // URLっぽい場合はドメイン名などを抽出
            if (name.StartsWith("http"))
            {
                try { name = new Uri(name).Host; } catch { }
            }

            var invalidChars = Path.GetInvalidFileNameChars();
            var safeName = new string(name.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());
            
            return safeName.Trim('_');
        }
    }
}
