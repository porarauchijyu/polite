using System;
using System.IO;
using System.Threading.Tasks;
using PuppeteerSharp;
using Newtonsoft.Json.Linq;

namespace RssGenerator.Services
{
    public class VisualSelectionResult
    {
        public string ContainerSelector { get; set; }
        public string TitleSelector { get; set; }
        public string DescriptionSelector { get; set; }
        public string DateSelector { get; set; }
        public string PageTitle { get; set; } // [NEW] ページのタイトル
        public bool IsCancelled { get; set; } = false;
    }

    public class VisualSelectorService
    {
        private readonly CrawlerService _crawler;

        public VisualSelectorService(CrawlerService crawler)
        {
            _crawler = crawler;
        }

        public async Task<VisualSelectionResult> SelectSelectorsAsync(string url)
        {
            _crawler.Headless = false;
            var tcs = new TaskCompletionSource<VisualSelectionResult>();

            try
            {
                var page = await _crawler.NewPageAsync();
                
                // C# 側のコールバックを公開 (JSON 文字列で受け取る)
                await page.ExposeFunctionAsync("onConfigDone", async (string json) =>
                {
                    try 
                    {
                        var results = Newtonsoft.Json.Linq.JObject.Parse(json);
                        var pageTitle = await page.EvaluateExpressionAsync<string>("document.title").ConfigureAwait(false);

                        var res = new VisualSelectionResult
                        {
                            ContainerSelector = results["container"]?.ToString(),
                            TitleSelector = results["title"]?.ToString(),
                            DescriptionSelector = results["desc"]?.ToString(),
                            DateSelector = results["date"]?.ToString(),
                            PageTitle = pageTitle
                        };
                        
                        // 返信を待たせたくないので、別スレッドで結果をセット
                        _ = Task.Run(() => tcs.TrySetResult(res));
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[VisualSelector] Bridge Error: {ex.Message}");
                        _ = Task.Run(() => tcs.TrySetResult(new VisualSelectionResult { IsCancelled = true }));
                        return false;
                    }
                }).ConfigureAwait(false);

                // ボット対策のウォームアップ
                try {
                    Uri uri = new Uri(url);
                    string root = $"{uri.Scheme}://{uri.Host}/";
                    await page.GoToAsync(root, new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.DOMContentLoaded }, Timeout = 10000 }).ConfigureAwait(false);
                    await Task.Delay(1500).ConfigureAwait(false);
                } catch { }

                await page.GoToAsync(url, new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.Networkidle2 }, Timeout = 30000 }).ConfigureAwait(false);
                
                // 動的コンテンツ (microCMS等) の読み込みを待つ
                await Task.Delay(3500).ConfigureAwait(false);

                // SelectorGadget.js を読み込んで注入
                string scriptPath = PathHelper.ResolvePath(Path.Combine("Scripts", "SelectorGadget.js"));
                if (File.Exists(scriptPath))
                {
                    string script = File.ReadAllText(scriptPath);
                    await page.AddScriptTagAsync(new AddTagOptions { Content = script }).ConfigureAwait(false);
                }
                else
                {
                    throw new FileNotFoundException("SelectorGadget.js が見つかりません。");
                }

                // ブラウザが閉じられた時のキャンセル処理
                page.Close += (sender, e) => tcs.TrySetResult(new VisualSelectionResult { IsCancelled = true });

                // ユーザーの操作を待機
                var finalResult = await tcs.Task;
                
                if (!finalResult.IsCancelled)
                {
                    await page.Browser.CloseAsync().ConfigureAwait(false);
                }

                return finalResult;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VisualSelector] Error: {ex.Message}");
                return new VisualSelectionResult { IsCancelled = true };
            }
            finally
            {
                _crawler.Headless = true; // 元に戻す
            }
        }
    }
}
