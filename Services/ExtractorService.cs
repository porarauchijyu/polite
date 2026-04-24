using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;
using Fizzler.Systems.HtmlAgilityPack;

namespace RssGenerator.Services
{
    public class FeedItem
    {
        public string Title { get; set; }
        public string Link { get; set; }
        public string Description { get; set; } // [NEW] 記事の概要/説明文
        public DateTime PublishedDate { get; set; }
        public string SourceTitle { get; set; } // 取得元サイト名
    }

    public class ExtractorService
    {
        public List<FeedItem> ExtractItems(string html, string baseUrl, string titleSelector, string linkSelector, string containerSelector = null, string descriptionSelector = null, string dateSelector = null)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var items = new List<FeedItem>();
            var root = doc.DocumentNode;

            // --- 新ロジック: アイテムコンテナ（枠）ベースの抽出 ---
            if (!string.IsNullOrEmpty(containerSelector))
            {
                var itemContainers = SelectNodes(root, containerSelector);
                Console.WriteLine($"[Extractor] {itemContainers.Count} 個のアイテムコンテナを検出しました: {containerSelector}");

                foreach (var container in itemContainers)
                {
                    try
                    {
                        // 1. リンクの取得 (コンテナ自体、またはコンテナ内の指定要素)
                        HtmlNode linkNode = SelectNode(container, linkSelector);
                        if (linkNode == null && container.Name.ToLower() == "a") linkNode = container; // コンテナ自体が<a>の場合
                        
                        string absoluteLink = null;
                        if (linkNode != null)
                        {
                            string href = linkNode.GetAttributeValue("href", "");
                            if (!string.IsNullOrEmpty(href) && !href.StartsWith("#") && !href.StartsWith("javascript:"))
                            {
                                absoluteLink = NormalizeUrl(baseUrl, href);
                            }
                        }

                        // 2. タイトルの取得 (コンテナ内の指定要素)
                        string title = GetValue(container, titleSelector);
                        if (string.IsNullOrWhiteSpace(title) && linkNode != null) title = linkNode.InnerText.Trim();
                        if (string.IsNullOrWhiteSpace(title)) title = container.InnerText.Trim(); // フォールバック

                        // 3. 説明文の取得
                        string description = GetValue(container, descriptionSelector);

                        // 4. 日付の取得
                        DateTime pubDate = DateTime.Now;
                        if (!string.IsNullOrEmpty(dateSelector))
                        {
                            string dateStr = GetValue(container, dateSelector);
                            pubDate = ParseDate(dateStr);
                        }

                        if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(absoluteLink))
                        {
                            if (!items.Any(i => i.Link == absoluteLink))
                            {
                                items.Add(new FeedItem
                                {
                                    Title = Cleaner.CleanTitle(title),
                                    Link = absoluteLink,
                                    Description = Cleaner.CleanTitle(description),
                                    PublishedDate = pubDate
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Error] 個別アイテム抽出中にエラー: {ex.Message}");
                    }
                }
            }
            // --- 旧ロジック: リンクベース抽出 (互換性維持) ---
            else
            {
                var linkNodes = SelectNodes(root, linkSelector);
                Console.WriteLine($"[Extractor] {linkNodes.Count} 個のリンク要素を検出しました。");

                foreach (var node in linkNodes)
                {
                    try
                    {
                        HtmlNode linkElement = node.Name.ToLower() == "a" ? node : node.QuerySelector("a");
                        if (linkElement == null) continue;

                        string relativeLink = linkElement.GetAttributeValue("href", "");
                        if (string.IsNullOrEmpty(relativeLink) || relativeLink.StartsWith("#") || relativeLink.StartsWith("javascript:")) continue;

                        string absoluteLink = NormalizeUrl(baseUrl, relativeLink);
                        string title = GetValue(node, titleSelector);
                        if (string.IsNullOrWhiteSpace(title)) title = linkElement.InnerText.Trim();

                        string description = GetValue(node, descriptionSelector);

                        DateTime pubDate = DateTime.Now;
                        if (!string.IsNullOrEmpty(dateSelector))
                        {
                            string dateStr = GetValue(node, dateSelector);
                            pubDate = ParseDate(dateStr);
                        }

                        if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(absoluteLink))
                        {
                            if (!items.Any(i => i.Link == absoluteLink))
                            {
                                items.Add(new FeedItem { Title = Cleaner.CleanTitle(title), Link = absoluteLink, Description = Cleaner.CleanTitle(description), PublishedDate = pubDate });
                            }
                        }
                    }
                    catch (Exception ex) { Console.WriteLine($"[Error] アイテム抽出中にエラー: {ex.Message}"); }
                }
            }

            return items;
        }

        private DateTime ParseDate(string dateStr)
        {
            if (string.IsNullOrWhiteSpace(dateStr)) return DateTime.Now;

            // 数字以外の文字（年月日など）を正規化
            string normalized = dateStr.Replace("年", "/").Replace("月", "/").Replace("日", "").Replace(".", "/").Trim();

            if (DateTime.TryParse(normalized, System.Globalization.CultureInfo.GetCultureInfo("ja-JP"), System.Globalization.DateTimeStyles.None, out DateTime result))
            {
                return result;
            }
            
            return DateTime.Now;
        }

        private HtmlNode SelectNode(HtmlNode root, string selector)
        {
            if (string.IsNullOrEmpty(selector)) return null;
            if (IsXPath(selector))
            {
                return root.SelectSingleNode(selector);
            }
            return root.QuerySelector(selector);
        }

        private IList<HtmlNode> SelectNodes(HtmlNode root, string selector)
        {
            if (string.IsNullOrEmpty(selector)) return new List<HtmlNode>();
            if (IsXPath(selector))
            {
                var nodes = root.SelectNodes(selector);
                return nodes != null ? nodes.ToList() : new List<HtmlNode>();
            }
            return root.QuerySelectorAll(selector).ToList();
        }

        private string GetValue(HtmlNode node, string selector)
        {
            var target = SelectNode(node, selector);
            return target?.InnerText?.Trim();
        }

        private bool IsXPath(string selector)
        {
            return selector.StartsWith("/") || selector.StartsWith("./") || selector.StartsWith("(");
        }

        private string NormalizeUrl(string baseUrl, string relativeUrl)
        {
            if (string.IsNullOrEmpty(relativeUrl)) return null;
            if (Uri.IsWellFormedUriString(relativeUrl, UriKind.Absolute)) return relativeUrl;

            try
            {
                var baseUri = new Uri(baseUrl);
                var absoluteUri = new Uri(baseUri, relativeUrl);
                return absoluteUri.ToString();
            }
            catch
            {
                return relativeUrl;
            }
        }
    }

    public static class Cleaner
    {
        public static string CleanTitle(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            // 改行や余計な空白を除去
            return text.Replace("\r", "").Replace("\n", " ").Replace("\t", " ").Trim();
        }
    }
}

